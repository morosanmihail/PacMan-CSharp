using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PacmanGameLogic.Automation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanBalanceBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            string HostName = "";
            int NumberOfGames = 1;
            string AIAgentToUse = "";
            int RandSeed = 0;
            var Base = new double[9] { 3.0, 2.8, 2.8, 2.8, 2.8, 1.5, 1.5, 1.5, 1.5 };
            List<double> Params = new List<double>(Base);

            if (args == null || args.Length < 3)
            {
                Console.WriteLine("Exited due to lack of arguments");
                return;
            }

            string PathSpecs = args[0];
            RandSeed = int.Parse(args[1]);
            var ReceivedParams = args[2].Split(',').Select(a => double.Parse(a)).ToList();

            dynamic Specs = JsonConvert.DeserializeObject(File.ReadAllText(PathSpecs));

            AIAgentToUse = Specs.custom.AIAgent;
            NumberOfGames = Specs.custom.NumberOfGames;

            int i = 0;
            foreach(var Parameter in Specs.parameters)
            {
                Params[(int)Parameter.custom.index] += ReceivedParams[i];
                i++;
            }

            var GR = new GameRunner();
            
            var RunResults = GR.RunGamesOnline(HostName, NumberOfGames, AIAgentToUse, RandSeed, Params);

            //Generate metrics

            //string MetricScores = JsonConvert.SerializeObject(RunResults.scores);
            JArray o = JArray.FromObject(RunResults.scores);

            Dictionary<string, object> collection = new Dictionary<string, object>()
            {
                {"Scores", o},
                {"OtherMetric", 1200}
            };

            JObject Result = new JObject(
                new JProperty("metrics",
                    JObject.FromObject(collection)
                    /*new JArray(
                        new JObject(
                            new JProperty("Scores", o)
                        ),
                        new JObject(
                            new JProperty("OtherMetric", 1200)
                            )
                   // ) */
                )
            );

            Console.WriteLine("BEGIN METRICS");
            Console.WriteLine(Result.ToString());
            Console.WriteLine("END METRICS");
        }
    }
}
