using MMPac;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NeuralNetworkRunToFile
{ 
    class Program
    {
        public static string RunFolder = "";
        public static string SaveFolder = "";
        static void Main(string[] args)
        {
            if (args.Length > 1)
            {
                RunFolder = args[0];
                SaveFolder = args[1];
            }
            else
            {
                Console.WriteLine("You have to supply 2 arguments: runFolder and saveFolder");
                Console.ReadKey();
                return;
            }

            var files = Directory.GetFiles(RunFolder);
            var latestfile = files.OrderBy(s => s).Last();

            var doc = new XmlDocument();
            doc.Load(latestfile);
            

            var Node = doc.DocumentElement.ChildNodes[2].ChildNodes[0].ChildNodes[9].ChildNodes[0].ChildNodes[6];

            List<double> Weights = new List<double>();

            foreach(dynamic X in Node.ChildNodes)
            {
                Weights.Add(double.Parse(X.InnerText));
            }

            var NN = new MMLocPac(Weights);
            NN.SaveWeights(Path.Combine(SaveFolder, "NeuralNetworkLocPac.nn"));
        }
    }
}
