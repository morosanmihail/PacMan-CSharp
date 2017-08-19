using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Pacman.Simulator
{
	class Program
	{
		private static Visualizer visualizer;
		private static Thread visualizerThread;

        // For argument based modifications ot the game state
        //private static bool TAKEN_ARGUMENTS = false;

        // For checking that the ghosts placed in the arguments are valid.
        private readonly static string[] GHOST_ALLOWED = { "bl", "br", "p", "r" };

		static void Main(string[] args) {
			Console.WriteLine("Simulator started");
            Console.WriteLine("Finding arguments...");
            //Console.WriteLine("Press Enter to exit");

            string Agent = "";
            string AgentFile = "";

            if (args.Length > 1)
            {
                Agent = args[0];
                AgentFile = args[1];
            }

            startVisualizer(Agent, AgentFile);
						
			while( true ) {
				string input = Console.ReadLine();
				switch(input){
					case "":
						//visualizerThread.Abort(); // buggy ... catch and close down gracefully
						//System.Threading.Thread.CurrentThread.Abort();
						break;
					case "restart":
					case "r":
						// support this
						break;
				}

			}
		}

		private static void startVisualizer(string Agent, string AgentFile) {
			visualizerThread = new System.Threading.Thread(delegate() {
				visualizer = new Visualizer(Agent, AgentFile);
                
                System.Windows.Forms.Application.Run(visualizer);
			});
			visualizerThread.Start();
		}
	}
}
