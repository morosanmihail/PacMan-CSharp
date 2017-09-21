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
        public static bool IsFile = false;
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                RunFolder = args[0];
                if (args.Length > 1)
                {
                    SaveFolder = args[1];
                } else
                {
                    FileAttributes attr = File.GetAttributes(RunFolder);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        //it's a directory
                        SaveFolder = RunFolder;
                    } else
                    {
                        SaveFolder = Path.GetDirectoryName(RunFolder);
                        IsFile = true;
                    }

                    
                }
            }
            else
            {
                Console.WriteLine("You have to supply 2 arguments: runFolder and saveFolder");
                Console.ReadKey();
                return;
            }

            string latestfile = "";

            if (!IsFile)
            {
                var files = Directory.GetFiles(RunFolder);
                latestfile = files.OrderBy(s => s).Last(s => s.EndsWith("xml"));
            } else
            {
                latestfile = RunFolder;
            }

            var doc = new XmlDocument();
            doc.Load(latestfile);
            
            string XPath = "//d4p1:BalanceGA[1]/df:Vector";

            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("df", "http://schemas.datacontract.org/2004/07/SharpGenetics.BaseClasses");
            nsmgr.AddNamespace("d4p1", "http://schemas.datacontract.org/2004/07/GeneticAlgorithm.GeneticAlgorithm");
            nsmgr.AddNamespace("d6p1", "http://schemas.microsoft.com/2003/10/Serialization/Arrays");

            var Root = doc.DocumentElement;
            var Node = Root.SelectSingleNode(XPath, nsmgr);

            //var Node = doc.DocumentElement.ChildNodes[2].ChildNodes[0].ChildNodes[9].ChildNodes[0].ChildNodes[7];
            
            List<double> Weights = new List<double>();
            int WeightCount = Node.ChildNodes.Count - 5;

            for(int i=0;i<WeightCount;i++)
            {
                Weights.Add(double.Parse(Node.ChildNodes[i].InnerText));
            }

            List<int> AStarWeights = new List<int>();
            for(int i=WeightCount;i<WeightCount + 5;i++)
            {
                AStarWeights.Add(int.Parse(Node.ChildNodes[i].InnerText));
            }

            //TODO change this to saving to other object type

            var NN = new MMLocPac(Weights, AStarWeights);

            SaveLocPacToFile sv = new SaveLocPacToFile(NN);

            //Save sv to file

            NN.SaveWeights(Path.Combine(SaveFolder, "neuralnetwork.nn"));
        }
    }
}
