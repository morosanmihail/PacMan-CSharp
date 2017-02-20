using Pacman.GameLogic;
using Pacman.GameLogic.RemoteControl;
using PacmanAI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PacmanGameLogic.Automation
{
    public class GameRunnerResults
    {
        public int gamesPlayed = 0;
        public int totalScore = 0;
        public int gamesToPlay = 100;
        public long longestGame = 0;

        public int highestScore = 0;
        public int lowestScore = int.MaxValue;

        public int maxPillsEaten = 0;
        public int minPillsEaten = int.MaxValue;
        public int avgPillsEaten = 0;
        public int pillsEatenTotal = 0;

        public int maxGhostsEaten = 0;
        public int minGhostsEaten = int.MaxValue;
        public int avgGhostsEaten = 0;
        public int totalGhostsEaten = 0;

        public List<double> scores = new List<double>();
    }

    public class GameRunner
    {
        GameRunnerResults grr;
        GameState gs;

        string GetRemoteGameResults(RPCData sendData, string HostName)
        {
            try
            {
                string message = "";

                XmlSerializer xmlSerializer = new XmlSerializer(sendData.GetType());

                using (StringWriter textWriter = new StringWriter())
                {
                    xmlSerializer.Serialize(textWriter, sendData);
                    message = textWriter.ToString();
                }

                RPCClient rpcClient = null;
                while (rpcClient == null)
                {
                    try
                    {
                        rpcClient = new RPCClient(HostName);
                    }
                    catch
                    {
                        rpcClient = null;
                    }
                }
                var response = rpcClient.CallRunGame(message);

                rpcClient.Close();

                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(" ERROR: " + e.Message);
                return "0,0,0,0"; //RunResults[i]
            }
        }

        public GameRunnerResults RunGamesOnline(string HostName, int gamesToPlay, string controller, int RandomSeed = 0, List<double> gameParameters = null)
        {
            var grr2 = new GameRunnerResults();

            RPCData sendData = new RPCData();
            MapData md = new MapData(gameParameters);
            sendData.MapData = md;
            sendData.GamesToPlay = gamesToPlay;
            sendData.AIToUse = controller;
            sendData.Parallel = false;
            sendData.MaxScore = 0;
            sendData.RandomSeed = RandomSeed;

            var Results = GetRemoteGameResults(sendData, HostName);

            grr2.scores = new List<double>();
            if (gamesToPlay > 0)
            {
                //foreach (var Results in RunResults)
                {
                    if (Results != null)
                    {
                        var lines = Results.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Length > 1)
                            {
                                var result = line.Split(',');
                                int Ghosts = int.Parse(result[0]);
                                int Pills = int.Parse(result[1]);
                                int Score = int.Parse(result[2]);
                                grr2.totalGhostsEaten += Ghosts;
                                grr2.pillsEatenTotal += Pills;
                                //grr.TotalGames[Level]++;
                                grr2.totalScore += Score;

                                grr2.scores.Add(Score);
                            }
                        }
                    }
                }
            }

            grr2.gamesPlayed = gamesToPlay;

            return grr2;
        }

        public GameRunnerResults RunGames(int gamesToPlay, BasePacman controller, int RandomSeed = 0, List<double> gameParameters = null)
        {
            grr = new GameRunnerResults();

            gs = gameParameters == null ? new GameState(RandomSeed) : new GameState(gameParameters, RandomSeed);

            gs.GameOver += new EventHandler(GameOverHandler);
            gs.StartPlay();

            // Turn off the logging
            if (controller.GetType() == typeof(LucPac))
            {
                LucPac.REMAIN_QUIET = true;
            }

            if (controller.GetType() == typeof(LucPacScripted))
            {
                LucPacScripted.REMAIN_QUIET = true;
            }

            gs.Controller = controller;

            Stopwatch watch = new Stopwatch();
            int percentage = -1;
            int lastUpdate = 0;
            watch.Start();
            while (grr.gamesPlayed < gamesToPlay)
            {
                int newPercentage = (int)Math.Floor(((float)grr.gamesPlayed / gamesToPlay) * 100);
                if (newPercentage != percentage || grr.gamesPlayed - lastUpdate >= 100)
                {
                    lastUpdate = grr.gamesPlayed;
                    percentage = newPercentage;
                }
                // update gamestate
                Direction direction = controller.Think(gs);
                gs.Pacman.SetDirection(direction);

                // update game
                gs.Update();
            }
            watch.Stop();

            // shut down controller
            controller.SimulationFinished();

            return grr;
        }

        private void GameOverHandler(object sender, EventArgs args)
        {
            if (gs.Pacman.Score > grr.highestScore)
            {
                grr.highestScore = gs.Pacman.Score;
            }
            if (gs.Pacman.Score < grr.lowestScore)
            {
                grr.lowestScore = gs.Pacman.Score;
            }

            grr.totalScore += gs.Pacman.Score;

            if (gs.m_PillsEaten > grr.maxPillsEaten)
            {
                grr.maxPillsEaten = gs.m_PillsEaten;
            }

            if (gs.m_PillsEaten < grr.minPillsEaten)
            {
                grr.minPillsEaten = gs.m_PillsEaten;
            }

            /// GHOSTS EATEN
            if (gs.m_GhostsEaten > grr.maxGhostsEaten)
            {
                grr.maxGhostsEaten = gs.m_GhostsEaten;
            }

            if (gs.m_GhostsEaten < grr.minGhostsEaten)
            {
                grr.minGhostsEaten = gs.m_GhostsEaten;
            }

            // Total up the amount of pills that have been eaten.
            grr.pillsEatenTotal += gs.m_PillsEaten;

            grr.totalGhostsEaten += gs.m_GhostsEaten;

            grr.scores.Add(gs.Pacman.Score);
            grr.gamesPlayed++;
        }
    }
}
