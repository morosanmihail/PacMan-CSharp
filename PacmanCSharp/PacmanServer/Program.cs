using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pacman.GameLogic;
using Pacman.GameLogic.RemoteControl;
using PacmanAI;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PacmanServer
{
    class Program
    {
        public static string HostName = "";
        public static int Port = 5672;
        public static string Username = "guest";
        public static string Password = "guest";

        public static bool UseAMQP = false;
        public static string AMQPURL = "";

        public static void LoadParameters(string[] args)
        {
            if (args.Length > 0)
            {
                UseAMQP = args[0].Equals("y");

                if (UseAMQP)
                {
                    AMQPURL = args[1];
                }
                else
                {
                    HostName = args[1];

                    if (args.Length > 2)
                    {
                        Username = args[2];
                        Password = args[3];
                    }
                }
            }
            else
            {
                Console.Write("Use AMQP? (y/n)");
                UseAMQP = Console.ReadKey().KeyChar == 'y';
                Console.WriteLine();

                if (UseAMQP)
                {
                    Console.Write("AMQP URL: ");
                    AMQPURL = Console.ReadLine();
                }
                else
                {
                    Console.Write("Host Name: ");
                    HostName = Console.ReadLine();

                    Console.Write("User Name: ");
                    Username = Console.ReadLine();

                    Console.Write("Password: ");
                    Password = Console.ReadLine();
                }
            }
        }

        private static int gamesPlayed = 0;
        private static int totalScore = 0;
        private static GameState gs;
        private static int gamesToPlay = 100;
        private static long longestGame = 0;
        
        private static int highestScore = 0;
        private static int lowestScore = int.MaxValue;

        private static int maxPillsEaten = 0;
        private static int minPillsEaten = int.MaxValue;
        private static int pillsEatenTotal = 0;

        private static int maxGhostsEaten = 0;
        private static int minGhostsEaten = int.MaxValue;
        private static int totalGhostsEaten = 0;

        private static long lastMs = 0;
        private static long ms = 0;
        
        public static string Results = "";

        public static List<double> Scores = new List<double>();
        public static List<double> GameLengths = new List<double>();
        public static List<int> PillsEaten = new List<int>();
        public static List<int> PowerPillsEaten = new List<int>();
        public static List<int> GhostsEaten = new List<int>();

        public static void Reset()
        {
            gamesPlayed = 0;
            totalScore = 0;
            longestGame = 0;
            highestScore = 0;
            lowestScore = int.MaxValue;
            maxPillsEaten = 0;
            minPillsEaten = int.MaxValue;
            pillsEatenTotal = 0;
            maxGhostsEaten = 0;
            minGhostsEaten = int.MaxValue;
            totalGhostsEaten = 0;

            Results = "";

            Scores = new List<double>();
            GameLengths = new List<double>();
            PillsEaten = new List<int>();
            PowerPillsEaten = new List<int>();
            GhostsEaten = new List<int>();
        }

        static BasePacman GetNewController(GameData CustomData)
        {
            switch (CustomData.AIToUse)
            {
                case "LucPacScripted":
                    LucPacScripted.REMAIN_QUIET = true;
                    return new LucPacScripted();
                case "MMPac":
                    return new MMPac.MMPac(CustomData.EvolvedValues);
                case "MMLocPac":
                    //if (CustomData.EvolvedValues.Count < 25)
                    //    return new MMPac.MMLocPac("NeuralNetworkLocPac.nn");
                    //else
                        return new MMPac.MMLocPac(CustomData.EvolvedNeuralNet);
                case "MMLocPacMemory":
                    //if (CustomData.EvolvedValues.Count < 25)
                    //    return new MMPac.MMLocPacMemory("NeuralNetworkLocPac.nn");
                    //else
                        return new MMPac.MMLocPacMemory(CustomData.EvolvedNeuralNet, CustomData.EvolvedAStarValues, CustomData.NNInput, CustomData.NNHidden, CustomData.NNOutput);
                case "LucPac":
                    LucPac.REMAIN_QUIET = true;
                    return new LucPac();
                default:
                    return (BasePacman)Activator.CreateInstance(Type.GetType(CustomData.AIToUse), new object[] { });
            }
        }

        public static void RunGameLinear(GameData CustomData)
        {
            Reset();
            try
            {
                // Set the new count of games that we want to simulate.
                gamesToPlay = CustomData.GamesToPlay;
                

                // Get some strange invocation error here.
                // tryLoadController(_agentName);
                
                BasePacman controller = null;

                if(CustomData.EvolvedValues.Count < 25 && CustomData.EvolvedValues.Count > 0)
                {
                    gs = new GameState(CustomData.EvolvedValues, CustomData.RandomSeed);
                } else
                {
                    gs = new GameState(CustomData.RandomSeed);
                }

                //BasePacman controller = new TestPac();

                controller = GetNewController(CustomData);

                /*if(CustomData.AIToUse.Equals("LucPacScripted"))
                {
                    controller = new LucPacScripted();
                    LucPacScripted.REMAIN_QUIET = true;
                }
                if (CustomData.AIToUse.Equals("MMPac"))
                {
                    //use CustomData.NeuralNetwork in constructor
                    controller = new MMPac.MMPac(CustomData.MapData.EvolvedValues);
                }
                if (CustomData.AIToUse.Equals("MMLocPac"))
                {
                    //use CustomData.NeuralNetwork in constructor
                    if (CustomData.MapData.EvolvedValues.Count < 25)
                        controller = new MMPac.MMLocPac("NeuralNetworkLocPac.nn");
                    else
                        controller = new MMPac.MMLocPac(CustomData.MapData.EvolvedValues);
                }
                if(CustomData.AIToUse.Equals("LucPac"))
                {
                    controller = new LucPac();
                    LucPac.REMAIN_QUIET = true;
                }*/

                
                gs.GameOver += new EventHandler(GameOverHandler);
                gs.StartPlay();

                gs.Controller = controller;

                Stopwatch watch = new Stopwatch();
                int percentage = -1;
                int lastUpdate = 0;
                int lastGamesPlayed = 0;
                watch.Start();
                while (gamesPlayed < gamesToPlay)
                {
                    int newPercentage = (int)Math.Floor(((float)gamesPlayed / gamesToPlay) * 100);
                    if (newPercentage != percentage || gamesPlayed - lastUpdate >= 100)
                    {
                        lastUpdate = gamesPlayed;
                        percentage = newPercentage;
                        Console.Clear();
                        /*Console.Write("Current parameter set: ");
                        foreach(var X in CustomData.MapData.EvolvedValues)
                        {
                            Console.Write(X + ", ");
                        }*/
                        Console.WriteLine();
                        Console.WriteLine("Simulating ... " + percentage + "% (" + gamesPlayed + " : " + gamesToPlay + ")");
                        //Console.WriteLine(" - Elapsed: " + formatSeconds((watch.ElapsedMilliseconds / 1000.0) + "") + "s, Estimated total: " + formatSeconds(((watch.ElapsedMilliseconds / 1000.0) / percentage * 100) + "") + "s");
                        Console.WriteLine(" - Current best: " + highestScore);
                        Console.WriteLine(" - Current worst: " + lowestScore);
                        if (gamesPlayed > 0)
                        {
                            Console.WriteLine(" - Current avg.: " + (totalScore / gamesPlayed));
                        }
                        /*for (int i = scores.Count - 1; i >= 0 && i > scores.Count - 100; i--)
                        {
                            Console.Write(scores[i] + ",");
                        }*/
                    }
                    // update gamestate
                    Direction direction = controller.Think(gs);
                    gs.Pacman.SetDirection(direction);

                    // update game
                    gs.Update();
                    ms += GameState.MSPF;

                    if(lastGamesPlayed != gamesPlayed)
                    {
                        //Game finished, recreate controller
                        controller = GetNewController(CustomData);
                        gs.Controller = controller;

                        lastGamesPlayed = gamesPlayed;
                    } else
                    {
                        if(ms - lastMs > 3600000)
                        {
                            gs.InvokeGameOver();
                        }
                    }
                }
                watch.Stop();

                // shut down controller
                controller.SimulationFinished();

                JArray o = JArray.FromObject(Scores);
                JArray JGameLengths = JArray.FromObject(GameLengths);
                JArray JPowerPills = JArray.FromObject(PowerPillsEaten);

                JArray JPills = JArray.FromObject(PillsEaten);
                JArray JGhosts = JArray.FromObject(GhostsEaten);

                Dictionary<string, object> collection = new Dictionary<string, object>()
                {
                    {"Scores", o},
                    {"Lengths", JGameLengths},
                    {"PowerPills", JPowerPills },
                    {"Pills", JPills },
                    {"Ghosts", JGhosts }
                };

                JObject Result = new JObject(
                    new JProperty("metrics",
                        JObject.FromObject(collection)
                    )
                );

                Results = Result.ToString();

                // output results
                Console.Clear();
                long seconds = ms / 1000;
                Console.WriteLine("Games played: " + gamesPlayed);
                Console.WriteLine("Avg. score: " + (totalScore / gamesPlayed));
                Console.WriteLine("Highest score: " + highestScore + " points");
                Console.WriteLine("Lowest score: " + lowestScore + " points");
                Console.WriteLine("Max Pills Eaten: " + maxPillsEaten);
                Console.WriteLine("Min Pills Eaten: " + minPillsEaten);
                Console.WriteLine("Average Pills Eaten: " + pillsEatenTotal / gamesPlayed);
                Console.WriteLine("Max Ghosts Eaten: " + maxGhostsEaten);
                Console.WriteLine("Min Ghosts Eaten: " + minGhostsEaten);
                Console.WriteLine("Average Ghosts Eaten: " + totalGhostsEaten / gamesPlayed);
                Console.WriteLine("Longest game: " + ((float)longestGame / 1000.0f) + " seconds");
                Console.WriteLine("Total simulated time: " + (seconds / 60 / 60 / 24) + "d " + ((seconds / 60 / 60) % 24) + "h " + ((seconds / 60) % 60) + "m " + (seconds % 60) + "s");
                Console.WriteLine("Avg. simulated time pr. game: " + ((float)ms / 1000.0f / gamesPlayed) + " seconds");
                Console.WriteLine("Simulation took: " + (watch.ElapsedMilliseconds / 1000.0f) + " seconds");
                //Console.WriteLine("Speed: " + (ms / watch.ElapsedMilliseconds) + " (" + ((ms / watch.ElapsedMilliseconds) / 60) + "m " + ((ms / watch.ElapsedMilliseconds) % 60) + " s) simulated seconds pr. second");
                Console.WriteLine("For a total of: " + gamesPlayed / (watch.ElapsedMilliseconds / 1000.0f) + " games pr. second");

            }
            catch (Exception e)
            {
                // Log error.
                Console.WriteLine("Error happened. " + e.Message);
            }
            //return Results;
        }

        static void Main(string[] args)
        {
            LoadParameters(args);

            ConnectionFactory factory = null;
            if (UseAMQP)
            {
                factory = new ConnectionFactory()
                {
                    Uri = AMQPURL,
                };
            } else
            {
                factory = new ConnectionFactory()
                {
                    HostName = HostName,
                    UserName = Username,
                    Password = Password,

                };
            }

            do
            {
                //On error, try to reconnect to server. Wait 5 seconds between reconnect attempts
                Console.WriteLine("Connecting to host " + HostName);
                try
                {
                    using (var connection = factory.CreateConnection())
                    using (var channel = connection.CreateModel())
                    {
                        channel.QueueDeclare(queue: "rpc_queue",
                                             durable: false,
                                             exclusive: false,
                                             autoDelete: false,
                                             arguments: null);
                        channel.BasicQos(0, 1, false);
                        var consumer = new QueueingBasicConsumer(channel);
                        channel.BasicConsume(queue: "rpc_queue",
                                             noAck: false,
                                             consumer: consumer);
                        Console.WriteLine(" [x] Awaiting RPC requests");

                        while (true)
                        {
                            string response = null;
                            var ea = (BasicDeliverEventArgs)consumer.Queue.Dequeue();

                            var body = ea.Body;
                            var props = ea.BasicProperties;
                            var replyProps = channel.CreateBasicProperties();
                            replyProps.CorrelationId = props.CorrelationId;

                            try
                            {
                                var message = Encoding.UTF8.GetString(body);

                                //int n = int.Parse(message);
                                Console.WriteLine(" [.] RunGame()");

                                /*var serializer = new XmlSerializer(typeof(RPCData));
                                RPCData customData;

                                using (TextReader reader = new StringReader(message))
                                {
                                    customData = (RPCData)serializer.Deserialize(reader);
                                }*/
                                dynamic JResults = JsonConvert.DeserializeObject(message);

                                var Base = new double[9] { 3.0, 2.8, 2.8, 2.8, 2.8, 1.5, 1.5, 1.5, 1.5 };
                                List<double> Params = new List<double>(Base);

                                //RPCData customData = new RPCData();
                                GameData customData = new GameData();
                                customData.AIToUse = JResults.custom.AIAgent;
                                customData.GamesToPlay = JResults.custom.NumberOfGames;
                                
                                foreach (var Param in JResults.parameters)
                                {
                                    if ((bool)Param.enabled == true)
                                    {
                                        if (((string)Param.name).Equals("neuralnet"))
                                        {
                                            customData.EvolvedNeuralNet = new List<double>(Param.value.ToObject<List<double>>());
                                            //break;
                                        }
                                        else
                                        {
                                            if (((string)Param.name).Equals("astar"))
                                            {
                                                customData.EvolvedAStarValues = new List<double>(Param.value.ToObject<List<double>>());
                                            }
                                            else
                                            {
                                                Params[(int)Param.custom.index] += (double)Param.value;
                                            }
                                        }
                                    }
                                }

                                customData.NNInput = JResults.custom.NNInput;
                                customData.NNHidden = JResults.custom.NNHidden;
                                customData.NNOutput = JResults.custom.NNOutput;

                                customData.EvolvedValues = Params;

                                customData.RandomSeed = (int)JResults.randomseed;

                                RunGameLinear(customData);
                                
                                response = Program.Results;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(" [.] " + e.Message);
                                response = "";
                            }
                            finally
                            {
                                var responseBytes = Encoding.UTF8.GetBytes(response);
                                channel.BasicPublish(exchange: "",
                                                     routingKey: props.ReplyTo,
                                                     basicProperties: replyProps,
                                                     body: responseBytes);
                                channel.BasicAck(deliveryTag: ea.DeliveryTag,
                                                 multiple: false);
                            }

                            Console.WriteLine(" [x] Awaiting RPC requests");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }

                Thread.Sleep(5000);
            } while (true);
        }

        private static void GameOverHandler(object sender, EventArgs args)
        {
            longestGame = Math.Max(longestGame, ms - lastMs);

            GameLengths.Add(ms - lastMs);

            highestScore = Math.Max(highestScore, gs.Pacman.Score);
            lowestScore = Math.Min(lowestScore, gs.Pacman.Score);

            totalScore += gs.Pacman.Score;

            maxPillsEaten = Math.Max(gs.m_PillsEaten, maxPillsEaten);
            minPillsEaten = Math.Min(gs.m_PillsEaten, minPillsEaten);

            PillsEaten.Add(gs.m_PillsEaten);
            PowerPillsEaten.Add(gs.m_PowerPillsEaten);

            maxGhostsEaten = Math.Max(gs.m_GhostsEaten, maxGhostsEaten);
            minGhostsEaten = Math.Min(gs.m_GhostsEaten, minGhostsEaten);

            GhostsEaten.Add(gs.m_GhostsEaten);
            
            pillsEatenTotal += gs.m_PillsEaten;

            totalGhostsEaten += gs.m_GhostsEaten;

            //scores.Add(gs.Pacman.Score);
            //totalScore += gs.Pacman.Score;
            gamesPlayed++;
            lastMs = ms;

            //Results += gs.m_GhostsEaten + "," + gs.m_PillsEaten + "," + gs.Pacman.Score + "\n";

            Scores.Add(gs.Pacman.Score);
        }
    }

    public class GameData
    {
        public int GamesToPlay;
        public string AIToUse;
        public int RandomSeed;
        public List<double> EvolvedValues;
        public List<double> EvolvedAStarValues;
        public List<double> EvolvedNeuralNet;
        public int NNInput, NNHidden, NNOutput;
    }
}
