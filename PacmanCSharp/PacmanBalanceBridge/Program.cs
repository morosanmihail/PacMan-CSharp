using PacmanGameLogic.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanBalanceBridge
{
    class Program
    {
        static void Main(string[] args)
        {
            //args[1] will have XML

            //read XML. Decide on params from that

            //args[2] will have rand seed
            var GR = new GameRunner();

            string HostName = "";
            int NumberOfGames = 1;
            string AIAgentToUse = "";
            int RandSeed = 0;
            List<double> ParamsToSend = new List<double>();

            var RunResults = GR.RunGamesOnline(HostName, NumberOfGames, AIAgentToUse, RandSeed, ParamsToSend);
        }
    }
}
