using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TF2Raffler
{
    class Program
    {

        static void Main(string[] args)
        {
            using (MySqlConnection conn = new MySqlConnection())
            {

                conn.ConnectionString = "Server=127.0.0.1;Database=playerranks;User=user;";
                MySqlCommand cmd;
                conn.Open();
                try
                {
                    cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT * FROM players ORDER BY points DESC"; //Ensure results are ordered
                    var results = cmd.ExecuteReader();

                    if (results == null)
                        throw new Exception("Couldn't execute the query.");
                    if (!results.HasRows || results.FieldCount == 0)
                        throw new Exception("Didn't retrieve the table properly.");

                    var entries = new List<Entry>();
                    float totalScore = 0;

                    Console.WriteLine("Reading table data...");

                    while (results.Read())
                    {
                        var id = results.GetString("steamid");
                        var nick = results.GetString("nickname");
                        var score = results.GetFloat("points");

                        entries.Add(new Entry()
                        {
                            steamID = id,
                            nickname = nick,
                            points = score
                        });
                        totalScore += score;
                    }
                    
                    if (entries.Count == 0)
                        throw new Exception("Parsed 0 entries.");
                    if (totalScore == 0)
                        throw new Exception("Score amount to 0??? Check this ASAP");

                    Console.WriteLine($"Table read.\nSelect how many winners ({entries.Count} available entries)?");
                    int winnerAmount = int.Parse(Console.ReadLine());

                    var now = DateTime.Now;
                    Random rng = new Random(Guid.NewGuid().GetHashCode()); //GUID'd it
                    int totalScoreUpped = (int)(totalScore * 100); //Get the decimals up to roll an integer random

                    var winners = new List<Entry>(winnerAmount);

                    for (int i = 0; i < winnerAmount && i < entries.Count; )
                    {
                        float winnerBracket = (rng.Next(totalScoreUpped) + 100) / 100f; //Roll a bracket (getting the decimals back)
                        float accumulated = 0; //current bracket
                        for (int j = entries.Count - 1; j >= 0; j--) //Iterate from lowest score
                        {
                            accumulated += entries[j].points;

                            if (accumulated > winnerBracket) //first score found above bracket is our winner
                            {
                                var winner = entries[j];
                                if (winners.Contains(winner)) //if entry already been chosen
                                    break; //reroll
                                else
                                {
                                    winners.Add(winner); //next winner retrieval
                                    i++;
                                    break;
                                }
                            }
                        }
                    }

                    winners.RemoveAll(winner => winner == null); //Remove nulls if winnerAmount > entryCount

                    if (winners.Count == 0)
                        throw new Exception("No winner? Check your code boi");

                    Console.WriteLine("Selected " + winners.Count + ". Creating html...");

                    //Create html for display
                    var htmlBuilder = new StringBuilder();
                    htmlBuilder.AppendLine("<!DOCTYPE html><html><body>");
                    htmlBuilder.AppendLine($"<h1>UNAGI Raffles - {now.ToString("yyyy/MM/dd HH:mm:ss")}</h1>");
                    htmlBuilder.AppendLine($"<h2>Winners:</h2>");

                    foreach(var winner in winners) //List winners
                        htmlBuilder.AppendLine($"<h4>With {(winner.points / totalScore * 100).ToString("0.00")}% chance of winning: {winner.nickname} with {winner.points} points! Congratulations!</h4>");

                    htmlBuilder.AppendLine($"<p>{entries.Count} players participated and racked a total of {totalScore} points.</p>");
                    htmlBuilder.AppendLine($"<table border=\"1\" style=\"width:100%\"><tr><th>#</th><th>Name</th><th>Score</th><th>Chance of Winning</th></tr>");

                    for (int i = 0; i < entries.Count; i++)
                        htmlBuilder.AppendLine($"<tr><td>{i+1}</td><td>{entries[i].nickname}</td><td>{entries[i].points}</td><td>{(entries[i].points / totalScore * 100).ToString("0.00")}%</td></tr>");

                    htmlBuilder.AppendLine("</body></html>");

                    File.WriteAllText($"raffle-{now.ToString("yyyyMMddHHmmss")}.html", htmlBuilder.ToString());

                    Console.Write("HTML created. Press any key to exit...");
                    Console.ReadKey();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                finally
                {
                    if (conn.State == System.Data.ConnectionState.Open)
                        conn.Close();
                }
            }
        }

        internal class Entry
        {
            public string steamID, nickname;
            public float points;
        }
    }
}
