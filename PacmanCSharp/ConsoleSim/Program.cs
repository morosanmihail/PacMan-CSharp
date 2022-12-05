using System;
using System.Collections.Generic;
using System.Linq;
using Pacman.GameLogic;
using System.Diagnostics;
using System.IO;
using Accord.Statistics;
using PacmanGameLogic.Automation;
using Accord.Statistics.Distributions.Univariate;

namespace PacmanAI
{
	class Program
	{
		private static int gamesPlayed = 0;
		private static int totalScore = 0;
		private static GameState gs;
		private static int gamesToPlay = 100;
		private static long longestGame = 0;

        #region Members
        private static int highestScore = 0;
		private static int lowestScore = int.MaxValue;

        private static int maxPillsEaten = 0;
        private static int minPillsEaten = int.MaxValue;
        private static int pillsEatenTotal = 0;
        
        private static int maxGhostsEaten = 0;
        private static int minGhostsEaten = int.MaxValue;
        private static int totalGhostsEaten = 0;
        #endregion

        private static long lastMs = 0;
		private static long ms = 0;
		private static List<double> scores = new List<double>();
        private static bool m_RemainQuiet = false;

        /// <summary>
        /// Take in the launch arguments that are generated when the program is
        /// launched for the first time
        /// </summary>
        /// <param name="args">The arguments to deal with</param>
        static void HandleArguments(string[] args)
        {
            // If arguments have been specified then determine what
            if (args.Length > 0)
            {
                // Loop through the arguments and do something based on them.
                for (int i = 0; i < args.Length; i++)
                {
                    // Based on what arguments are used then do something
                    switch (args[i])
                    {
                        // How many games do we want simulated?
                        case "-c":
                            if ((i + 1) < args.Length)
                            {
                                int _result = 0;
                                if (int.TryParse(args[i + 1], out _result))
                                {
                                    gamesToPlay = _result;
                                }
                                else
                                {
                                    // Inform ourselves that the number was not found in the argument
                                    Console.WriteLine("Number after the -c argument was not recognised.");
                                }
                            }
                            break;

                        // Called for when we want the agent to be quiet and no log output whatsoever.
                        case "-q":
                            m_RemainQuiet = true;
                        break;
                    }
                }
            }
        }

        private static void GameOverParallelHandler(object sender, EventArgs args)
        {
            GameState GS = sender as GameState;

            GS.PausePlay();

            highestScore = Math.Max(highestScore, GS.Pacman.Score);
            lowestScore = Math.Min(lowestScore, GS.Pacman.Score);

            totalScore += GS.Pacman.Score;
            gamesPlayed++;
        }

        static void Main(string[] args) {

            Process _process = Process.GetCurrentProcess();
            bool _takearguments = false;

            // How many arguments have been stored in the game.
            if (args.Length > 0)
            {
                Console.WriteLine("Arguments found.");
                _takearguments = true;
                HandleArguments(args);
            }

            string HostName = "localhost";

            if (!_takearguments)
            {
                Console.WriteLine("Host name:");
                HostName = Console.ReadLine();

                Console.Clear();
                Console.WriteLine("How many games do you wish to simulate?");
                string _count = Console.ReadLine();
                int _result = 0;
                
                // Determine that the value that has been inputted is
                // in fact valid.
                while (!int.TryParse(_count,out _result))
                {
                    Console.Clear();
                    Console.WriteLine("Please try again: ");
                    _count = Console.ReadLine();

                }

                // Set the new count of games that we want to simulate.
                gamesToPlay = _result;

                Console.Clear();
                string _consoleoutput = "";

                while (_consoleoutput != "n" && _consoleoutput != "y")
                {
                    // Determine if we want to log output to be silence while we do this
                    Console.WriteLine("Silence output?");
                    _consoleoutput = Console.ReadLine();
                }

                if (_consoleoutput == "n")
                {
                    m_RemainQuiet = false;
                }
                else if (_consoleoutput == "y")
                {
                    m_RemainQuiet = true;
                }
            }

            //RunGamesParallel();

            //return;

            // Get some strange invocation error here.
            // tryLoadController(_agentName);

			gs = new GameState(125);
			gs.GameOver += new EventHandler(GameOverHandler);
			gs.StartPlay();

            BasePacman controller = new LucPacScripted();

            Console.WriteLine("Choose an AI agent to control Pacman:");
            Console.WriteLine(" 1 - LucPacScripted");
            Console.WriteLine(" 2 - LucPac (MCTS)");
            Console.WriteLine(" 3 - MMLocPac (Evolved Neural Network) from .nn file");
            Console.WriteLine(" 5 - SimRandom");
            int Selection = int.Parse(Console.ReadKey().KeyChar.ToString());

            switch (Selection)
            {
                case 1:
                    controller = new LucPacScripted();
                    break;
                case 2:
                    controller = new LucPac();
                    break;
                case 3:
                    controller = new MMPac.MMLocPac("NeuralNetworkLocPac.nn");
                    break;
                default:
                    controller = new RandomPac();
                    break;
            }


            var GR = new GameRunner();

            var Base = new double[9] { 3.0, 2.8, 2.8, 2.8, 2.8, 1.5, 1.5, 1.5, 1.5 };
            var Params = new double[9] {0.07,0.01,0.02,-0.16,0.06,-0.05,0,0.06,-0.09 };
            //var Params = new double[9] { -0.17, 0.01, 0.02, -0.16, 0.06, -0.05, 0, 0.06, -0.09 };

           // Params = Params.Add(Base);

            string TestAgent = "PacmanAI.UncertainAgent,PacmanAI";

            var GRR = GR.RunGames(gamesToPlay, controller, gameParameters: Params.ToList());

            var NewScores = new List<double>();
            NewScores.AddRange(GRR.scores);
            for (int i = 0; i < NewScores.Count; i++)
                NewScores[i] += 9000;
            NewScores.AddRange(GRR.scores);

            var ZeroScores = new List<double>();
            for(int i=0;i<100;i++)
            {
                ZeroScores.Add(0);
            }

            WriteOutputFiles(GRR, controller);

            Console.WriteLine("Done - " + GRR.scores.Average() + " " + GRR.gamesPlayed);
            Console.WriteLine("Scores over 1600: " + GRR.scores.Where(s => s >= 1600).Count());

            Console.WriteLine("Done (Altered) - " + NewScores.Average() + " " + GRR.gamesPlayed);
            Console.WriteLine("Scores over 1500 (Altered): " + NewScores.Where(s => s >= 1500).Count());

            /*Console.WriteLine("Evaluation score via distribution evaluation: " + new DistributionWeightEvaluation(null).CalculateFitnessScore(GRR.scores, 5000, 1));
            Console.WriteLine("Evaluation score via average evaluation: " + new AccurateThresholdEvaluation(null).CalculateFitnessScore(GRR.scores, 5000, 1));


            Console.WriteLine("Evaluation score via distribution evaluation (All zeroes): " + new DistributionWeightEvaluation(null).CalculateFitnessScore(ZeroScores, 1500, 1));
            Console.WriteLine("Evaluation score via average evaluation (All zeroes): " + new AccurateThresholdEvaluation(null).CalculateFitnessScore(ZeroScores, 1500, 1));

            Console.WriteLine("Evaluation score via distribution evaluation (Altered scores): " + new DistributionWeightEvaluation(null).CalculateFitnessScore(NewScores, 5000, 1));
            Console.WriteLine("Evaluation score via average evaluation (Altered scores): " + new AccurateThresholdEvaluation(null).CalculateFitnessScore(NewScores, 5000, 1));

            var gaussianDist = new Accord.Statistics.Distributions.Univariate.NormalDistribution(2000, 10);

            Console.WriteLine("Evaluation score via distribution evaluation (Gaussian scores): " + new DistributionWeightEvaluation(null).CalculateFitnessScore(gaussianDist.Generate(100).ToList(), 2000, 1));
            Console.WriteLine("Evaluation score via average evaluation (Gaussian scores): " + new AccurateThresholdEvaluation(null).CalculateFitnessScore(gaussianDist.Generate(100).ToList(), 2000, 1));
            */
            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution rdb = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(GRR.scores.ToArray(),25);
            Accord.Controls.DataSeriesBox.Show("Pacman score distribution", rdb.ProbabilityDensityFunction, new Accord.DoubleRange(-2000, 12000));
            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution rdb2 = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(NewScores.ToArray());
            Accord.Controls.DataSeriesBox.Show("Pacman score distribution (Altered)", rdb2.ProbabilityDensityFunction, new Accord.DoubleRange(-2000, 12000));

            double[] coef = { 4,1 };
            var skewNormal = new Mixture<NormalDistribution>(coef, new NormalDistribution(2000, 1500), new NormalDistribution(7000, 1500));// new SkewNormalDistribution(4500, 3000, 7.2);
            Accord.Controls.DataSeriesBox.Show("Skew Normal", skewNormal.ProbabilityDensityFunction, new Accord.DoubleRange(-2000, 12000));

            Console.ReadKey();

            return;

            // DEFINE CONTROLLER //
            //BasePacman controller = new MMMCTSCode.MMMCTS();
            //BasePacman controller = new RandomPac();
            //BasePacman controller = new LucPacScripted();
            //BasePacman controller = new LucPac();
            //BasePacman controller = new MMPac.MMPac("NeuralNetwork.nn");
            //BasePacman controller = new MMPac.MMPac(Weights);
            //BasePacman controller = new MMPac.MMLocPac("NeuralNetworkLocPac.nn");

            // Turn off the logging
            if (controller.GetType() == typeof(LucPac) && m_RemainQuiet)
            {
                LucPac.REMAIN_QUIET = true;
            }

            if (controller.GetType() == typeof(LucPacScripted) && m_RemainQuiet)
            {
                LucPacScripted.REMAIN_QUIET = true;
            }

			//BasePacman controller = new SmartDijkstraPac();
			gs.Controller = controller;

			Stopwatch watch = new Stopwatch();
			int percentage = -1;
			int lastUpdate = 0;
			watch.Start();
			while( gamesPlayed < gamesToPlay ) {
				int newPercentage = (int)Math.Floor(((float)gamesPlayed / gamesToPlay) * 100);
				if( newPercentage != percentage || gamesPlayed - lastUpdate >= 100 ) {
					lastUpdate = gamesPlayed;
					percentage = newPercentage;
					Console.Clear();
					Console.WriteLine("Simulating ... " + percentage + "% (" + gamesPlayed + " : " + gamesToPlay + ")");
					Console.WriteLine(" - Elapsed: " + (watch.ElapsedMilliseconds / 1000.0) + "ms");
					Console.WriteLine(" - Current best: " + highestScore);
					Console.WriteLine(" - Current worst: " + lowestScore);
					if( gamesPlayed > 0 ) {
						Console.WriteLine(" - Current avg.: " + (totalScore / gamesPlayed));
					}
				}
				// update gamestate
				Direction direction = controller.Think(gs);
				gs.Pacman.SetDirection(direction);
				
				// update game
				gs.Update();
				ms += GameState.MSPF;
			}
			watch.Stop();

			// shut down controller
			controller.SimulationFinished();

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
			Console.WriteLine("Speed: " + (ms / watch.ElapsedMilliseconds) + " (" + ((ms / watch.ElapsedMilliseconds) / 60) + "m " + ((ms / watch.ElapsedMilliseconds) % 60) + " s) simulated seconds pr. second");
			Console.WriteLine("For a total of: " + gamesPlayed / (watch.ElapsedMilliseconds / 1000.0f) + " games pr. second");
            Console.WriteLine();

            //Calculate standard deviation
            double mean = totalScore / gamesPlayed;
            double totalsqdif = 0;
            foreach(var val in scores)
            {
                totalsqdif += (val - mean) * (val - mean);
            }
            double variance = totalsqdif / gamesPlayed;
            double stddev = Math.Sqrt(variance);
            Console.WriteLine("Standard deviation of: " + stddev);

            Console.WriteLine("Standard deviation of (Accord): " + scores.ToArray().StandardDeviation());

            //Generates a distribution from existing data
            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution db = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(scores.ToArray());
            
            //Calculates standard deviation
            Console.WriteLine("Standard deviation of (Accord 2): " + db.StandardDeviation);


            double[] sample =
                //{ 1000, 960, 1000, 960, 1000, 600, 100, 1000, 1500};
                { 2000, 2500, 2100, 9000, 1900, 2000, 150, 2100};
            //{ 60000, 70000, 80000, 90000, 40000, 100000, 200000, 15000, 500000, 44444 }; 
            //scores.Take(20).ToArray();

            double[] sample2 =
                scores.Take(100).ToArray();

            //Shapiro Wilk test to see if distribution is normal
            var swT = new Accord.Statistics.Testing.ShapiroWilkTest(scores.ToArray());
            Console.WriteLine("Shapiro Wilk Test on all scores: Statistic - " + swT.Statistic + " , PValue - " + swT.PValue + " , Significant - " + swT.Significant);

            var normalDist = new Accord.Statistics.Distributions.Univariate.NormalDistribution(950, 1200);
            var swT2 = new Accord.Statistics.Testing.ShapiroWilkTest(normalDist.Generate(1000));
            Console.WriteLine("Shapiro Wilk Test on normal dist: Statistic - " + swT2.Statistic + " , PValue - " + swT2.PValue + " , Significant - " + swT2.Significant);


            //Accord.Statistics.Testing.KolmogorovSmirnovTest ks = new Accord.Statistics.Testing.KolmogorovSmirnovTest(sample, db);

            //Console.WriteLine("KS Test: Statistic - " + ks.Statistic + " , PValue - " + ks.PValue + " , Significant - " + ks.Significant);

            //Probability that the given scores were sampled from the previous distribution
            Accord.Statistics.Testing.ZTest ts = new Accord.Statistics.Testing.ZTest(sample, totalScore / gamesPlayed);
                /*sample.Average(), 
                db.StandardDeviation,
                sample.Length,
                totalScore / gamesPlayed);
                */
            Console.WriteLine("Z Test: Statistic - " + ts.Statistic + " , PValue - " + ts.PValue + " , Significant - " + ts.Significant);


            Accord.Statistics.Testing.ZTest ts2 = new Accord.Statistics.Testing.ZTest(sample2, totalScore / gamesPlayed);
                /*sample2.Average(), 
                //Accord.Statistics.Tools.StandardDeviation(sample2.ToArray()),
                db.StandardDeviation,
                sample2.Length,
                totalScore / gamesPlayed);
                */
            Console.WriteLine("Z Test 2: Statistic - " + ts2.Statistic + " , PValue - " + ts2.PValue + " , Significant - " + ts2.Significant);

            //% of values that are between given ranges
            Console.WriteLine("Distribution function 0 - 1000: " + db.DistributionFunction(0, 1000));
            Console.WriteLine("Distribution function 1000 - 11000: " + db.DistributionFunction(1000, 11000));
            Console.WriteLine("Distribution function 0 - 500: " + db.DistributionFunction(0, 500));
            Console.WriteLine("Distribution function 1500 - 11000: " + db.DistributionFunction(1500, 11000));

            //MannWhitneyWilcoxon test on whether 2 samples are from the same distribution - high P value = likely same distribution
            Accord.Statistics.Testing.MannWhitneyWilcoxonTest mwTest = new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(scores.ToArray(), sample2);
            Console.WriteLine("MWW Test: Statistic - " + mwTest.Statistic + " , PValue - " + mwTest.PValue + " , Significant - " + mwTest.Significant);


            Accord.Statistics.Testing.MannWhitneyWilcoxonTest mwTest2 = new Accord.Statistics.Testing.MannWhitneyWilcoxonTest(normalDist.Generate(1000), scores.ToArray());
            Console.WriteLine("MWW Test 2 (actual scores versus normal dist): Statistic - " + mwTest2.Statistic + " , PValue - " + mwTest2.PValue + " , Significant - " + mwTest2.Significant);

            //Accord.Controls.HistogramBox.Show(scores.ToArray());


            //Guess what distribution this is
            var analysis = new Accord.Statistics.Analysis.DistributionAnalysis(scores.ToArray());

            // Compute the analysis
            analysis.Compute();

            // Get the most likely distribution (first)
            var mostLikely = analysis.GoodnessOfFit[0];
            
            var result = mostLikely.Distribution.ToString();
            Console.WriteLine(result);

            //Plots the distributions
            Accord.Controls.DataSeriesBox.Show("Pacman score distribution", db.ProbabilityDensityFunction, new Accord.DoubleRange(-2000, highestScore));

            Accord.Controls.DataSeriesBox.Show("Normal distribution", normalDist.ProbabilityDensityFunction, new Accord.DoubleRange(-2000, highestScore));

            Accord.Controls.DataSeriesBox.Show("Gamma distribution", mostLikely.Distribution.ProbabilityFunction, new Accord.DoubleRange(-2000, highestScore));



            //Calculate some CDF related malarkey
            //top 20 scores - 1 empirical
            //next 20 scores - 2nd empirical
            //calculate cdf of both
            //calculate cdf of cumulative

            int games1 = 80;
            int games2 = 20;

            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution edb = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(scores.GetRange(0,games1).ToArray());
            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution edb2 = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(scores.GetRange(games1, games2).ToArray());
            Accord.Statistics.Distributions.Univariate.EmpiricalDistribution edbC = new Accord.Statistics.Distributions.Univariate.EmpiricalDistribution(scores.GetRange(0, games1 + games2).ToArray());

            var cdf1 = edb.DistributionFunction(800);
            var cdf2 = edb2.DistributionFunction(800);
            var cdfC = edbC.DistributionFunction(800);

            Console.WriteLine("CDF1 = " + cdf1 + ", CDF2 = " + cdf2 + ", Guess = " + (cdf1 * games1 + cdf2 * games2) / (games1 + games2) + ", Actual = " + cdfC);


            //Convolution
            var ScoresA = scores.GetRange(0, games1).ToArray();
            var ScoresB = scores.GetRange(games1, games1).ToArray();

            //var Convolution = ScoresA.Convolve(ScoresB);
            double[] Convolution = new double[games1];
            Accord.Math.Transforms.FourierTransform2.Convolve(ScoresA, ScoresB, Convolution);

            Console.ReadLine();
		}

        private static string GetOutputFileName(string outputFileIdentifier, int sampleSize, BasePacman algorithm, DateTime generatedAt)
        {
	        Directory.CreateDirectory("output");
			return string.Format("output/{0}_(size-{1})_{2}_{3:yyyy-MM-dd_hh-mm-ss}.csv",
				outputFileIdentifier,
				sampleSize,
				algorithm.Name,
				generatedAt);
		}

        private static void WriteOutputFiles(GameRunnerResults result, BasePacman controller)
        {
	        Console.WriteLine("Start writing output files");
	        var generationTime = DateTime.Now;

	        var aggregatedFileName = GetOutputFileName("aggregated", result.gamesPlayed, controller, generationTime);
	        var aggregatedStatsStream = new StreamWriter(File.Open(aggregatedFileName, FileMode.Create));
	        aggregatedStatsStream.WriteLine(
		        "gamesPlayed,longestGame,highestScore,lowestScore,maxPillsEaten,minPillsEaten,avgPillsEaten,maxGhostsEaten,minGhostsEaten,avgGhostsEaten");
	        aggregatedStatsStream.Write("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
		        result.gamesPlayed,
		        result.longestGame,
		        result.highestScore,
		        result.lowestScore,
		        result.maxPillsEaten,
		        result.minPillsEaten,
		        result.avgPillsEaten,
		        result.maxGhostsEaten,
		        result.minGhostsEaten,
		        result.avgGhostsEaten);
	        aggregatedStatsStream.Close();

	        var scoresFileName = GetOutputFileName("scores", result.gamesPlayed, controller, generationTime);
	        var scoresStream = new StreamWriter(File.Open(scoresFileName, FileMode.Create));
	        scoresStream.WriteLine("score");
	        result.scores.ForEach(scoresStream.WriteLine);
	        scoresStream.Close();

	        Console.WriteLine("Finished writing output files");
        }

        private static void GameOverHandler(object sender, EventArgs args) {
			if( ms - lastMs > longestGame )
				longestGame = ms - lastMs;
			if( gs.Pacman.Score > highestScore ) {
				highestScore = gs.Pacman.Score;
			}
			if( gs.Pacman.Score < lowestScore ) {
				lowestScore = gs.Pacman.Score;
			}
            
            totalScore += gs.Pacman.Score;

            if (gs.m_PillsEaten > maxPillsEaten)
            {
                maxPillsEaten = gs.m_PillsEaten;
            }

            if (gs.m_PillsEaten < minPillsEaten)
            {
                minPillsEaten = gs.m_PillsEaten;
            }

            /// GHOSTS EATEN
            if (gs.m_GhostsEaten > maxGhostsEaten)
            {
                maxGhostsEaten = gs.m_GhostsEaten;
            }

            if (gs.m_GhostsEaten < minGhostsEaten)
            {
                minGhostsEaten = gs.m_GhostsEaten;
            }

            // Total up the amount of pills that have been eaten.
            pillsEatenTotal += gs.m_PillsEaten;
            
            totalGhostsEaten += gs.m_GhostsEaten;

			scores.Add(gs.Pacman.Score);
			gamesPlayed++;
			lastMs = ms;
		}
	}
}