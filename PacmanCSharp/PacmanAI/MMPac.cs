using Accord.Neuro;
using Accord.Neuro.ActivationFunctions;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Pacman.GameLogic;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMPac
{
    public class EvolutionWeights
    {
        private ActivationNetwork network;
        public EvolutionWeights(ActivationNetwork network)
        {
            this.network = network;
        }

        public void SetWeights(List<double> NNWeights)
        {
            int Current = 0;
            
            foreach (ActivationLayer layer in network.Layers)
            {
                foreach (ActivationNeuron neuron in layer.Neurons)
                {
                    for (int i = 0; i < neuron.Weights.Length; i++)
                    {
                        neuron.Weights[i] = NNWeights[Current];
                        
                        Current++;
                    }
                    neuron.Threshold = NNWeights[Current];
                    Current++;
                }
            }
            
        }

        public void SetWeightsRandom()
        {
            new GaussianWeights(network).Randomize();
            //((ActivationNetwork)network).UpdateVisibleWeights();
        }

        public List<double> TeachNetwork(List<double> Input, List<double> Output)
        {
            var teacher = new BackPropagationLearning(network)
            {
                LearningRate = 0.1f,
                Momentum = 0.9f
            };
            /*var teacher = new Accord.Neuro.Learning.DeepNeuralNetworkLearning(network)
            {
                Algorithm = (ann, i) => new ParallelResilientBackpropagationLearning(ann),
                LayerIndex = network.Layers.Length - 1,
            };*/

            //double[][] inputs, outputs;
            //Main.Database.Training.GetInstances(out inputs, out outputs);

            // Start running the learning procedure
            //for (int i = 0; i < Epochs && !shouldStop; i++)
            {
                teacher.Run(Input.ToArray(), Output.ToArray());
                //double error = teacher.RunEpoch(inputs, outputs);
            }

            //network.UpdateVisibleWeights();

            return new List<double>(network.Compute(Input.ToArray()));
        }

        public void TeachNetworkByEpochs(List<List<double>> Input, List<int> Output, int Epochs)
        {
            var teacher = new BackPropagationLearning(network)
            {
                LearningRate = 0.1f,
                Momentum = 0.9f
            };
            /*var teacher = new Accord.Neuro.Learning.DeepNeuralNetworkLearning(network)
            {
                Algorithm = (ann, i) => new ParallelResilientBackpropagationLearning(ann),
                LayerIndex = network.Layers.Length - 1,
            };*/

            double[][] inputs, outputs;
            //Main.Database.Training.GetInstances(out inputs, out outputs);

            inputs = new double[Input.Count][];
            outputs = new double[Output.Count][];

            for (int i=0; i < Input.Count; i++)
            {
                inputs[i] = Input[i].ToArray();
                outputs[i] = new double[6] { 0, 0, 0, 0, 0, 0 };
                outputs[i][Output[i]] = 1;    
            }

            // Start running the learning procedure
            for (int i = 0; i < Epochs; i++)
            {
                //teacher.Run(Input.ToArray(), Output.ToArray());
                double error = teacher.RunEpoch(inputs, outputs);
            }

            //network.UpdateVisibleWeights();

        }

        public void SaveWeights(string filename)
        {
            network.Save(filename);
        }

        public ActivationNetwork LoadWeightsFromFile(string filename)
        {
            network = (ActivationNetwork)Network.Load(filename);
            return network;
        }
    }

    public class MMPac : BasePacman
    {
        public ActivationNetwork Network;

        static int InputCount = 12;
        static int OutputCount = 1;

        EvolutionWeights EvoWeights;

        //List<double> PreviousOutput = new List<double>();

        public MMPac(List<double> NNWeights)
            : base("MMPac")
        {
            Network = new ActivationNetwork(new BernoulliFunction(), InputCount, 5, 5, OutputCount);

            EvoWeights = new EvolutionWeights(Network);
            EvoWeights.SetWeights(NNWeights);
            //Network.UpdateVisibleWeights();

            //for (int i = 0; i < OutputCount; i++) PreviousOutput.Add(0);
        }

        public MMPac(string LoadFromFile = "")
            : base("MMPac")
        {
            if (LoadFromFile.Length > 0)
            {
                EvoWeights = new EvolutionWeights(null);
                Network = EvoWeights.LoadWeightsFromFile(LoadFromFile);
            }
            else
            {
                Network = new DeepBeliefNetwork(new BernoulliFunction(), InputCount, OutputCount);
            }

            //for (int i = 0; i < OutputCount; i++) PreviousOutput.Add(0);
        }

        public void SaveWeights(string filename)
        {
            EvoWeights.SaveWeights(filename);
        }

        public List<double> GenerateInput(GameState gs, Node P)
        {
            List<double> input = new List<double>();
            //int PacX = gs.Pacman.Node.X;
            //int PacY = gs.Pacman.Node.Y;
            //Node P = gs.Pacman.Node;

            var PathtoPac = P.ShortestPath[gs.Pacman.Node.X, gs.Pacman.Node.Y];
            input.Add((PathtoPac != null ? PathtoPac.Distance : 2000));

            //IsJunction
            input.Add(P.PossibleDirections.Count > 2 ? 1 : -1);

            foreach (var G in gs.Ghosts)
            {
                var Path = P.ShortestPath[G.Node.X, G.Node.Y];
                input.Add((Path != null ? Path.Distance : 2000));
                //input.Add(Path != null ? ((double)Path.Direction) : 1);
                input.Add((G.Fleeing && G.Entered) ? 1 : -1);

                //input.Add(Normalize(PacX - G.Node.X, 32)); input.Add(Normalize(PacY - G.Node.Y, 32));

                //Distance to ghost
                //input.Add(Normalize(Math.Abs(PacX - G.Node.X) + Math.Abs(PacY - G.Node.Y), 64));
            }

            var NearestPowerPill = StateInfo.NearestPowerPill(P, gs);
            if (NearestPowerPill.Target != null)
            {
                var Path = P.ShortestPath[NearestPowerPill.Target.X, NearestPowerPill.Target.Y];
                input.Add((Path != null ? Path.Distance : 2000));
                //input.Add(Path != null ? ((double)Path.Direction) : 1);
                //input.Add(Normalize(PacX - NearestPowerPill.Target.X, 32));
                //input.Add(Normalize(PacY - NearestPowerPill.Target.Y, 32));
            }
            else
            {
                input.Add(2000);
                //input.Add(1);
            }

            if (P.Type == Node.NodeType.Pill)
            {
                input.Add(0);
            }
            else
            {
                NearestPowerPill = StateInfo.NearestPill(P, gs);
                if (NearestPowerPill.Target != null)
                {
                    var Path = P.ShortestPath[NearestPowerPill.Target.X, NearestPowerPill.Target.Y];
                    input.Add((Path != null ? Path.Distance : 2000));
                    //input.Add(Path != null ? ((double)Path.Direction) : 1);
                    //input.Add(Normalize(PacX - NearestPowerPill.Target.X, 32));
                    //input.Add(Normalize(PacY - NearestPowerPill.Target.Y, 32));
                }
                else
                {
                    input.Add(2000);
                    //input.Add(0);
                }
            }



            /*if (PrevOutput == null)
                input.AddRange(PreviousOutput);
            else
                input.AddRange(PrevOutput);
                */
            return input;
        }

        public override Direction Think(GameState gs)
        {
            double bestScore = 0;
            Direction bestDir = Direction.None;
            List<Direction> possible = gs.Pacman.PossibleDirections();

            foreach(var Dir in possible)
            {
                var Node = gs.Pacman.Node.GetNeighbour(Dir);
                List<double> input = GenerateInput(gs, Node);

                double output = Network.Compute(input.ToArray())[0];

                if (output > bestScore)
                {
                    bestScore = output;
                    bestDir = Dir;
                }
            }

            return bestDir;
        }
    }
}
