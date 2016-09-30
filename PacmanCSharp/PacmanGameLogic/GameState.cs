using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.ComponentModel;

using System.Reflection;

// Required for cloning the object information that we are after
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Pacman.GameLogic.Ghosts;

namespace Pacman.GameLogic
{
	public class GameState : ICloneable
	{
        [CategoryAttribute("The level number"),DescriptionAttribute("The level number that the controller is on")]
		private int level = 0;
		public int Level { get { return level; } }

		public static Random Random = new Random();

        // ** NOTE ** //
        // These were originally read only.
        // Had to remove them because of cloning purposes.
       
        [CategoryAttribute("Pacman Object"),DescriptionAttribute("The object responsible for Ms. Pacman within the gamestate")]
        public Pacman Pacman;

        [CategoryAttribute("Red Ghost"), DescriptionAttribute("The red ghost within the maze")] 
		public Red Red;

        [CategoryAttribute("The pink ghost within the maze"), DescriptionAttribute("The object responsible for Ms. Pacman within the gamestate")]
		public Pink Pink;
    
		public Blue Blue;
       
		public Brown Brown;
    
        public Ghost[] Ghosts = new Ghost[4];

        [NonSerialized()]
		public BasePacman Controller;

        [CategoryAttribute("MSPF"),DescriptionAttribute("MSPF description")]
		public const int MSPF = 40;

        [CategoryAttribute("Timer"),DescriptionAttribute("The in game timer ticker")]
		public long Timer = 0;

        [CategoryAttribute("Elapsed time"), DescriptionAttribute("Time elapsed since the simulation started")]
		public long ElapsedTime = 0;
		public long Frames = 0;

        // Used for determining whether during simulation there was a failure.
        [CategoryAttribute("GameoverCount"), DescriptionAttribute("The amount of times that the agent lost the game")]
        public int m_GameOverCount = 0;

        [NonSerialized()]
		public static Image[] Mazes = new Image[1];

        private static Map[] maps = new Map[1];

        private Map map;
        private bool started = false;

        #region Properties
        public Map Map { get { return map; } set { map = value; } }
        public bool Started { get { return started; } }
        #endregion

        private const int reversalTime1 = 5000, reversalTime2 = 25000; // estimates
		private int reversal1 = reversalTime1, reversal2 = reversalTime2;		

		// settings
        [CategoryAttribute("PacmanMortal"),DescriptionAttribute("Was Pacman considered invincible during this game state?")]
		public bool PacmanMortal = true;
		
        [CategoryAttribute("NaturalReversals"),DescriptionAttribute("Will the ghosts return back to their normal state afterwards?")]
        public bool NaturalReversals = true;
		public bool Replay = false;
		
        [CategoryAttribute("AutomaticLevelChange"),DescriptionAttribute("Has the level changed")]
        public bool AutomaticLevelChange = true;

        [field: NonSerializedAttribute()]
		public event EventHandler GameOver;

        public event EventHandler PillsEaten;

        public event EventHandler GhostEaten;

        public int CloneTimeBegin = 0;
        public int CloneTimeEnd = 0;

        // Store the amount of time in total that has been consumed
        public int MCTSTimeTotal = 0;
        public int MCTSGamesTotal = 0;

        [field: NonSerializedAttribute()]
        public event EventHandler PacmanDead = new EventHandler(delegate(object sender, EventArgs e) {  });

        // For registering how many pills have been eaten within the environment.
        public int m_PillsEaten = 0;
        public int m_GhostsEaten = 0;

        public int m_TotalRoundScore = 0;

        #region Constructors
        public GameState(List<double> EvolvedValues, int RandomSeed) {
			loadMazes();
			map = maps[Level];
			// default position ... find out where
			Pacman = new Pacman(Pacman.StartX, Pacman.StartY, this, EvolvedValues[0]);
			Ghosts[0] = Red = new Red(Red.StartX, Red.StartY, this, EvolvedValues[1], EvolvedValues[5]);
			Ghosts[1] = Pink = new Pink(Pink.StartX, Pink.StartY, this, EvolvedValues[2], EvolvedValues[6]);
			Ghosts[2] = Blue = new Blue(Blue.StartX, Blue.StartY, this, EvolvedValues[3], EvolvedValues[7]);
			Ghosts[3] = Brown = new Brown(Brown.StartX, Brown.StartY, this, EvolvedValues[4], EvolvedValues[8]);

            Random = new Random(RandomSeed);
		}

        public GameState(int RandomSeed = 0)
        {
            loadMazes();
            map = maps[Level];
            // default position ... find out where
            Pacman = new Pacman(Pacman.StartX, Pacman.StartY, this, 3.0f);
            Ghosts[0] = Red = new Red(Red.StartX, Red.StartY, this, 2.8f, 1.5f);
            Ghosts[1] = Pink = new Pink(Pink.StartX, Pink.StartY, this, 2.8f, 1.5f);
            Ghosts[2] = Blue = new Blue(Blue.StartX, Blue.StartY, this, 2.8f, 1.5f);
            Ghosts[3] = Brown = new Brown(Brown.StartX, Brown.StartY, this, 2.8f, 1.5f);

            if(RandomSeed != 0)
            {
                Random = new Random(RandomSeed);
            }
        }
        #endregion

        #region Methods
        private void loadMazes(){
			for( int i = 0; i < 1; i++ ) {
                if (Mazes[i] == null || maps[i] == null)
                {
                    Mazes[i] = Util.LoadImage("ms_pacman_maze" + (i + 1) + ".gif");
                    maps[i] = new Map((Bitmap)Mazes[i]);
                }
			}
		}

        // Simple function that increases the count in which the 
        // the controller has died.
        private void IncreaseGameOverCount()
        {
            m_GameOverCount++;
        }

		public void StartPlay() {
			started = true;
			Timer = 0;
			Frames = 0;
		}

		public void PausePlay() {
			started = false;
		}

		public void ResumePlay() {
			started = true;
		}

		public void ReverseGhosts() {
			foreach( Ghost g in Ghosts ) {
				g.Reversal();
			}
		}

        public static Direction InverseDirection(Direction pDirection)
        {
            switch (pDirection)
            {
                case Direction.Up: return Direction.Down;
                case Direction.Down: return Direction.Up;
                case Direction.Left: return Direction.Right;
                case Direction.Right: return Direction.Left;
            }
            return Direction.None;
        }

        /// <summary>
        /// Move the Pacman agent in the given direction
        /// </summary>
        /// <param name="pDirection">The direction that we want the Pacman agent to move in</param>
        public void AdvanceGame(Direction pDirection)
        {
            if (Pacman.Lives == -1)
            {
                InvokeGameOver(true);
                return;
            }
            // Set the direction of PacMan within this gamestate.
            // Expectedly meant to be cloned
            Pacman.SetDirection(pDirection);

            if (!started)
            {
                return;
            }
            Frames++;
            Timer += MSPF;
            ElapsedTime += MSPF;
            // change level
            // TODO: use levels instead of just mazes
            if (Map.PillsLeft == 0 && AutomaticLevelChange)
            {
                level = Level + 1; // test for screenplayer
                if (Level > maps.Length - 1) level = 0;
                map = maps[Level];
                map.Reset();
                resetTimes();
                Pacman.ResetPosition();
                foreach (Ghost g in Ghosts)
                {
                    g.ResetPosition();
                }
                if (Controller != null)
                {
                    Controller.LevelCleared();
                }
                return;
            }
            // ghost reversals
            if (NaturalReversals)
            {
                bool ghostFleeing = false;
                foreach (Ghost g in Ghosts)
                {
                    if (!g.Chasing)
                    {
                        ghostFleeing = true;
                        break;
                    }
                }
                if (ghostFleeing)
                {
                    reversal1 += MSPF;
                    reversal2 += MSPF;
                }
                else
                {
                    if (Timer > reversal1)
                    {
                        reversal1 += 1200000; // 20 min
                        ReverseGhosts();
                    }
                    if (Timer > reversal2)
                    {
                        reversal2 = Int32.MaxValue;
                        ReverseGhosts();
                    }
                }
            }
            if (Replay)
            {
                // do nothing
            }
            else
            {
                // move
                Pacman.MoveSimulated();
                foreach (Ghost g in Ghosts)
                {
                    g.Move();
                    // check collisions				
                    if (g.Distance(Pacman) < 4.0f)
                    {
                        if (g.Chasing)
                        {
                            if (PacmanMortal)
                            {
                                resetTimes();
                                Pacman.Die();
                                PacmanDead(this, null);
                                foreach (Ghost g2 in Ghosts)
                                {
                                    g2.PacmanDead();
                                }
                                //if (Pacman.Lives == -1)
                                //{
                                //    InvokeGameOver(true);
                                //}
                                break;
                            }
                        }
                        else if (g.Entered)
                        {
                            Pacman.EatGhost();
                            g.Eaten();
                        }
                    }
                }
            }
        }

        // Generate a random move based on the simrandompac.
        public Direction GenerateRandomMove()
        {
            List<Direction> possible = Pacman.PossibleDirections();
            if (possible.Count > 0)
            {
                int select = Random.Next(0, possible.Count); //new Random().Next(0, possible.Count);
                if (possible[select] != Pacman.InverseDirection(Pacman.Direction))
                    return possible[select];
            }
            return Direction.None;
        }

        /// <summary>
        /// Used for the MCTS implementation so that we can see how the controller behaves
        /// </summary>
        public void UpdateSimulated()
        {
            if (Pacman.Lives == -1)
            {
                InvokeGameOver(true);
                return;
            }
            // Get the next direction from the controller that we want to
            // simulate from
            Direction _nextDirection = GenerateRandomMove();
            Pacman.SetDirection(_nextDirection);

            if (!started)
            {
                return;
            }
            Frames++;
            Timer += MSPF;
            ElapsedTime += MSPF;
            // change level
            // TODO: use levels instead of just mazes
            if (Map.PillsLeft == 0 && AutomaticLevelChange)
            {
                level = Level + 1; // test for screenplayer
                if (Level > maps.Length - 1) level = 0;

                map = maps[Level];
                map.Reset();
                
                resetTimes();
                Pacman.ResetPosition();
                foreach (Ghost g in Ghosts)
                {
                    g.ResetPosition();
                }
                if (Controller != null)
                {
                    Controller.LevelCleared();
                }
                return;
            }
            // ghost reversals
            if (NaturalReversals)
            {
                bool ghostFleeing = false;
                foreach (Ghost g in Ghosts)
                {
                    if (!g.Chasing)
                    {
                        ghostFleeing = true;
                        break;
                    }
                }
                if (ghostFleeing)
                {
                    reversal1 += MSPF;
                    reversal2 += MSPF;
                }
                else
                {
                    if (Timer > reversal1)
                    {
                        reversal1 += 1200000; // 20 min
                        ReverseGhosts();
                    }
                    if (Timer > reversal2)
                    {
                        reversal2 = Int32.MaxValue;
                        ReverseGhosts();
                    }
                }
            }
            if (Replay)
            {
                // do nothing
            }
            else
            {
                // move
                Pacman.MoveSimulated();
                foreach (Ghost g in Ghosts)
                {
                    g.Move();
                    // check collisions				
                    if (g.Distance(Pacman) < 4.0f)
                    {
                        if (g.Chasing)
                        {
                            if (PacmanMortal)
                            {
                                resetTimes();
                                Pacman.Die();
                                PacmanDead(this, null);
                                foreach (Ghost g2 in Ghosts)
                                {
                                    g2.PacmanDead();
                                }
                                //if (Pacman.Lives <= -1)
                                //{
                                //    InvokeGameOver(true);
                                //}

                                break;
                            }
                        }
                        else if (g.Entered)
                        {
                            Pacman.EatGhost();
                            g.Eaten();
                        }
                    }
                }
            }
        }

		public void Update() {
			if( !started ) {
				return;
			}
			Frames++;
			Timer += MSPF;
			ElapsedTime += MSPF;
			// change level
			// TODO: use levels instead of just mazes
			if( Map.PillsLeft == 0 && AutomaticLevelChange ) {
				level = Level + 1; // test for screenplayer
				if( Level > maps.Length - 1 ) level = 0;
				map = maps[Level];
				map.Reset();
				resetTimes();
				Pacman.ResetPosition();
				foreach( Ghost g in Ghosts ) {
					g.ResetPosition();
				}
				if( Controller != null ) {
					Controller.LevelCleared();
				}
				return;
			}
			// ghost reversals
			if( NaturalReversals ) {
				bool ghostFleeing = false;
				foreach( Ghost g in Ghosts ) {
					if( !g.Chasing ) {
						ghostFleeing = true;
						break;
					}
				}
				if( ghostFleeing ) {
					reversal1 += MSPF;
					reversal2 += MSPF;
				} else {
					if( Timer > reversal1 ) {
						reversal1 += 1200000; // 20 min
						ReverseGhosts();
					}
					if( Timer > reversal2 ) {
						reversal2 = Int32.MaxValue;
						ReverseGhosts();
					}
				}
			}
			if( Replay ) {
				// do nothing
			}
			else {
				// move
				Pacman.Move();
				foreach( Ghost g in Ghosts ) {
					g.Move();
					// check collisions				
					if( g.Distance(Pacman) < 4.0f ) {
						if( g.Chasing ) {
							if( PacmanMortal ) {
								resetTimes();

                                // Signal to the controller that we have restarted
                                // Store the states at the same time
                                Controller.Restart(this);
								
                                Pacman.Die();								
								PacmanDead(this, null);
								foreach( Ghost g2 in Ghosts ) {
									g2.PacmanDead();
								}
								if( Pacman.Lives == -1 ) {
									InvokeGameOver();
								}
								break;
							}
						} else if( g.Entered ) {
							Pacman.EatGhost();
							g.Eaten();
						}
					}
				}
			}
		}

		public void InvokeGameOver(bool Simulated = false) {
            m_GameOverCount++;

            GameOver?.Invoke(this, null);
            m_TotalRoundScore = 0;
            m_PillsEaten = 0;
            m_GhostsEaten = 0;
			ElapsedTime = 0;
			level = 0;
			map = maps[Level];
            if (!Simulated)
            {
                map.Reset();
                Pacman.Reset();
                foreach (Ghost g in Ghosts)
                {
                    g.PacmanDead();
                }
            }		
		}

		private void resetTimes() {
			Timer = 0;
			reversal1 = reversalTime1; 
			reversal2 = reversalTime2;
        }
        #endregion


        // Save the output of the game state image so that we can
        // examine the simulations that were made.
        public void SaveGameStateImage(Graphics g)
        {

        }

        /// <summary>
        /// Return whether or not there is a ghost at the provided coordinates
        /// </summary>
        /// <param name="x">The x coordinate</param>
        /// <param name="y">The y coordinate</param>
        /// <returns>Returns whether or not there is a ghost at the provided position</returns>
        public bool GhostIsAt(int x, int y)
        {
            foreach (var ghost in Ghosts)
            {
                if (ghost.Node.X == x && 
                    ghost.Node.Y == y)
                    return true;
            }

            return false;
        }

        #region ICloneable Members

        /** CLONE ALL THE THINGS! **/
        public object Clone()
        {
            CloneTimeBegin = Environment.TickCount;

            // Create a new copy of the object that we want
            GameState _temp = (GameState)this.MemberwiseClone();
            _temp.Map = (Map)this.Map.Clone();
            _temp.Pacman = (Pacman)this.Pacman.Clone(); // Grab a copy of pacman while we're at it 
            _temp.Pacman.GameState = _temp;

            _temp.GameOver = delegate(object sender, EventArgs e) { };
    
            // Genereate the maps
            //_temp.maps = new Map[1];
            //_temp.maps[0] = (Map)maps[0].Clone();

            // Commenting this out for testing purposes

            //for (int i = 0; i < maps.Length; i++)
            //{
            //    _temp.maps[i] = (Map)maps[i].Clone();
            //}

            // Generate the new array of ghosts
            _temp.Ghosts = new Ghost[4];
            _temp.Ghosts[0] = _temp.Red = (Red)Red.Clone();
            _temp.Ghosts[1] = _temp.Pink = (Pink)Pink.Clone();
            _temp.Ghosts[2] = _temp.Blue = (Blue)Blue.Clone();
            _temp.Ghosts[3] = _temp.Brown = (Brown)Brown.Clone();
            
            // Assign the newly generated game state manually
            // to the objects.
            _temp.Brown.GameState = _temp;
            _temp.Pink.GameState = _temp;
            _temp.Blue.GameState = _temp;
            _temp.Red.GameState = _temp;

            // Generate a random controller for the future simulations
            _temp.Controller = new SimRandomPac();

            // Loop through the ghosts and assign the game state appropriately
            for (int i = 0; i < _temp.Ghosts.Length; i++)
            {
                _temp.Ghosts[i].GameState = _temp;
            }

            CloneTimeEnd = Environment.TickCount;

            return _temp;
        }

        //public object DeepCopy()
        //{
        //    return CloneObject(this);
        //}

        //public object CloneObject(object o)
        //{
        //    Type type = o.GetType();
        //    object new_obj = Activator.CreateInstance(type);

        //    foreach (FieldInfo info in type.GetFields())
        //    {
        //        if (info.IsValueType == true)
        //        {
                  
        //            info.SetValue(new_obj, info.GetValue(o));
        //        }
        //        else
        //        {
        //            object cloned = CloneObject(info.GetValue(o));
        //            info.SetValue(new_obj, cloned);
        //        }
        //    }

        //    return new_obj;
        //}

        #endregion
    }
}
