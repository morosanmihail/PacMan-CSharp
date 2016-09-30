using Pacman.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanAI.MMMCTSCode
{
    public class Node
    {
        Direction Move;
        Node Parent;
        List<Node> Children;
        int Wins = 0;
        int Visits = 0;
        List<Direction> UntriedMoves;

        static GameState StartState;

        public Node(Direction move = Direction.None, Node Parent = null, GameState state = null)
        {
            this.Move = move;
            this.Parent = Parent;
            Children = new List<Node>();
            UntriedMoves = state.Pacman.PossibleDirections();
            UntriedMoves.Add(Direction.Stall);
            //UntriedMoves = new List<Direction>() { Direction.Up, Direction.Down, Direction.Left, Direction.Right, Direction.Stall };
            Visits = 0;
        }

        public Node UCTSelectChild()
        {
            Children = Children.OrderByDescending(c => (c.Wins / c.Visits + Math.Sqrt(2 * Math.Log(Visits) / c.Visits))).ToList();
            return Children[0];
        }

        public Node AddChild(Direction m, GameState s)
        {
            Node n = new Node(m, this, s);
            UntriedMoves.Remove(m);
            Children.Add(n);
            return n;
        }

        public void Update(int Score)
        {
            Visits++;
            Wins += Score;
        }

        public static int EvaluateNode(GameState pGameState)
        {
            // The score weighting for the MCTS tree in question
            int _score = 0;

            int _livesremaining = pGameState.Pacman.Lives;

            // Determine whether or not the lives remaining has changed.
            if (_livesremaining == -1)
            {
                _score -= TreeNode.DEATH_PENALTY;
            }

            // Determine whether or not the pacman got onto the next level.
            if (pGameState.Map.PillsLeft == 0)
            {
                _score += TreeNode.COMPLETE_REWARD;
            }

            _score += (- StartState.Pacman.Score + pGameState.Pacman.Score);

            return _score;
        }

        public static GameState SimulateGame(GameState pGameState, BasePacman pController, int pSteps)
        {
            int _currentlevel = pGameState.Level;
            int _gameoverCount = pGameState.m_GameOverCount;
            GameState _gameStateCloned = pGameState; // (GameState) pGameState.Clone();

            // Set the random controller to the game state that we are focusing on
            _gameStateCloned.Controller = pController;

            // Loop through the maximum amount of steps and then perform the 
            // simulation on the game state
            while (pSteps-- > 0
                   && _gameStateCloned.Level == _currentlevel
                   && _gameoverCount == _gameStateCloned.m_GameOverCount)
            {
                _gameStateCloned.UpdateSimulated();
            }

            // SaveStateAsImage(_gameStateCloned, LucPac.INSTANCE, "_simulatedgame");

            return _gameStateCloned;
        }

        public static Direction UCT(GameState rootstate, int itermax, int goforwardintime, int rolloutsize)
        {
            StartState = rootstate;
            Random rnd = new Random();
            Node rootnode = new Node(Direction.None, null, rootstate); //Maybe clone here?
            for (int i = 0; i < itermax; i++)
            {
                Node node = rootnode;
                GameState state = (GameState)rootstate.Clone();
                //Select
                while (node.UntriedMoves.Count == 0 && node.Children.Count > 0)
                {
                    node = node.UCTSelectChild();
                    
                    for (int y = 0; y < goforwardintime; y++)
                    {
                        if (state.Pacman.Lives < 0)
                            break;
                        state.AdvanceGame(node.Move);
                    }
                }
                //Expand
                if (node.UntriedMoves.Count > 0)
                {
                    Direction m = node.UntriedMoves[rnd.Next(0, node.UntriedMoves.Count)];
                    if(state.Pacman.Lives >= 0)
                        state.AdvanceGame(m);
                    node = node.AddChild(m, state);
                }
                //Rollout
                for (int y = 0; y < rolloutsize; y++)
                {
                    var pd = state.Pacman.PossibleDirections();
                    if (pd.Count == 0)
                        break;

                    var move = pd[rnd.Next(0, pd.Count)];
                    
                    for (int z = 0; z< goforwardintime;z++)
                    {
                        if (state.Pacman.Lives < 0)
                            break;
                        state.AdvanceGame(move);
                    }
                }
                //Backpropagate
                while (node != null)
                {
                    node.Update(EvaluateNode(state));
                    node = node.Parent;
                }
            }

            var Sorted = rootnode.Children.OrderByDescending(c => c.Visits).ToList();
            //var Sorted = rootnode.Children.OrderByDescending(c => c.Wins).ToList();
            return Sorted[0].Move;
        }
    }

    public class MMMCTS : BasePacman
    {
        int lastUpdate = 0;
        Direction lastUCT = Direction.None;
        public MMMCTS() : base("MMMCTS")
        {
        }

        public override Direction Think(GameState gs)
        {
            //if (lastUpdate == 0)
            {
                lastUCT = Node.UCT(gs, 22, 3, 7);
                //lastUpdate = 2;
            }

            //lastUpdate--;
            return lastUCT;
        }
    }
}
