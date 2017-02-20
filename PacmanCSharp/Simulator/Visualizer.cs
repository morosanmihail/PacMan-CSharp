using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Pacman.GameLogic.Ghosts;
using Pacman.GameLogic;
using System.Xml;
using System.Runtime.Serialization;
using System.Linq;
using PacmanAI;
using PacmanAI.MMMCTSCode;

namespace Pacman.Simulator
{
    [Serializable()]
	public partial class Visualizer : Form
	{
		private static string path = System.Reflection.Assembly.GetExecutingAssembly().EscapedCodeBase;
		private static string WorkingDir = Path.GetDirectoryName(path.Substring(path.IndexOf("///") + 3)).Replace("%23", "#").Replace("%20"," ");
        //private static string PacmanAIDir = "C:\\Users\\Luc\\Documents\\Visual Studio 2008\\Projects\\PacmanProject\\MsPacmanController\\bin\\Controllers";

        private static string PacmanAIDir = System.IO.Path.Combine(Environment.CurrentDirectory,@"Controllers");

		private uint fastTimer;
		private uint frames = 0;
		private static Image image;
		private Graphics g;
		private TimerEventHandler tickHandler;
		private Keys lastKey = Keys.None;
		private static Image sprites;		
		private GameState gameState;
		//private BasePacman controller;
		private byte[] gameStream;
		private int readIndex = 0;
		private Point mousePos = new Point(0, 0);
		private bool step = false;
		// sector train data collection
		private const bool collectSectorData = false;
		private DateTime lastCollection = DateTime.Now;
		private bool collectionInProgress = false;
		private bool collectionInputCalled = false;

        // The name of the agent that is going to be loaded in
        public static string AgentName { get; set; }

        // Return the image that is being used for the diplay of the game
        public static Image RenderingImage
        {
            get { return image; }
            set { image = value; }
        }

        public static Image RenderingSprites
        {
            get { return sprites; }
            set { sprites = value; }
        }

        int Selection = 1;

        BasePacman GetResetController()
        {
            switch (Selection)
            {
                case 1:
                    return new LucPacScripted();
                case 2:
                    return new LucPac();
                case 3:

                    return new MMPac.MMLocPac("NeuralNetworkLocPac.nn");
                case 6:
                    return new UncertainAgent();
                case 7:
                    return new LucPacMCTS();
                default:
                    return new SimGreedyRandom();
            }
        }
		
		public Visualizer() {			
			InitializeComponent();
			KeyDown += new KeyEventHandler(keyDownHandler);
			Picture.MouseDown += new MouseEventHandler(mouseDownHandler);
			Picture.MouseMove += new MouseEventHandler(mouseMoveHandler);
			// load images
			sprites = Util.LoadImage("sprites.png");
			// initialize graphics
			image = new Bitmap(Picture.Width, Picture.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			g = Graphics.FromImage(image);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			//g.ScaleTransform(2.0f, 2.0f);	
			// gamestate
			gameState = new GameState();
			this.Width = gameState.Map.PixelWidth + 7; // screenplayer test
			this.Height = gameState.Map.PixelHeight + 49; // screenplayer test
			//gameState.PacmanMortal = false;
			gameState.StartPlay();
			gameState.GameOver += new EventHandler(gameOverHandler);

            // IMPORTANT SETTINGS AREA //
            //tryLoadController("LucPac");

            //gameState.Controller = tryLoadController("LucPac");
            

            //gameState.Controller = tryLoadNNController("MMPac", NNValues);

            Console.WriteLine("Choose an AI agent to control Pacman:");
            Console.WriteLine(" 1 - LucPacScripted");
            Console.WriteLine(" 2 - LucPac (MCTS)");
            Console.WriteLine(" 3 - MMLocPac (Evolved Neural Network) from .nn file");
            Console.WriteLine(" 5 - SimRandom");
            Console.WriteLine(" 6 - UncertainAgent");
            Console.WriteLine(" 7 - LucPacMCTS2");

            Selection = int.Parse(Console.ReadKey().KeyChar.ToString());

            gameState.Controller = GetResetController();

            //gameState.Controller = tryLoadNNController("MMPac", "NeuralNetwork.nn");
            //gameState.Controller = tryLoadController("LucPacScripted");

            //
            //
            //gameState.Controller = new MMMCTS();

            //gameState.Controller = new MMPac.MMPac(NNValues);
            //((MMPac.MMPac)gameState.Controller).SaveWeights("NeuralNetwork.nn");

            //
            //controller = gameState.Controller;

            int myData = 0; // dummy data
			tickHandler = new TimerEventHandler(tick);
			fastTimer = timeSetEvent(50, 50, tickHandler, ref myData, 1);	
			//			
		}

		public void SetGameState(GameState gameState){			
			this.gameState = gameState;
			gameState.Replay = true;
			foreach( Ghost g in gameState.Ghosts ) {
				g.Enabled = false;
			}
		}

		private void tryLoadStream(string name) {
			try {
				gameStream = File.ReadAllBytes(Path.Combine(WorkingDir, name + ".dat"));
				foreach( Ghost g in gameState.Ghosts ) {
					g.Enabled = false;
				}
				gameState.Replay = true;
			} catch( IOException ) {
				Console.WriteLine("Could not find stream: " + name + ".dat");
			}
		}

        private static byte[] LoadBytes(string filename) {
			using( FileStream input = File.OpenRead(filename) ) {
				byte[] bytes = new byte[input.Length];
				input.Read(bytes, 0, bytes.Length);
				return bytes;
			}
		}

		private void gameOverHandler(object sender, EventArgs args) {


            gameState.Controller = GetResetController();
            //Console.WriteLine("Game over!");
        }

		private void mouseMoveHandler(object sender, MouseEventArgs e) {
			mousePos = new Point(e.X, e.Y);
		}

		private void mouseDownHandler(object sender, MouseEventArgs args) {

		}

		private void keyDownHandler(object sender, KeyEventArgs args) {
			if( collectionInProgress ){
				if( args.KeyCode == Keys.Q ) {
					skip = true;
				} else {
					acceptKey = args.KeyCode + "";
					try {
						danger = Int16.Parse(args.KeyValue + "") - 48;
					} catch { }
				}
			}			
			if( args.KeyCode == Keys.Left || args.KeyCode == Keys.Right ||
				args.KeyCode == Keys.Up || args.KeyCode == Keys.Down ) {
				lastKey = args.KeyCode;
				switch( args.KeyCode ) {
					case Keys.Up: gameState.Pacman.SetDirection(Direction.Up); break;
					case Keys.Down: gameState.Pacman.SetDirection(Direction.Down); break;
					case Keys.Left: gameState.Pacman.SetDirection(Direction.Left); break;
					case Keys.Right: gameState.Pacman.SetDirection(Direction.Right); break;				
				}
			}
			if( args.KeyCode == Keys.Space ) {
				if( gameState.Started ) {
					gameState.PausePlay();
				} else {
					gameState.ResumePlay();
				}
			}
			if( args.KeyCode == Keys.R ) {
				gameState.ReverseGhosts();
			}
			if( args.KeyCode == Keys.S ) {
				step = true;
			}
		}

		private void drawScreen(int score, int livesLeft, int[] sectorDrawing) {
			// clear screen
			g.Clear(Color.Black);
			// draw maze, pills
			gameState.Map.Draw(g);			
			// draw score
			g.DrawString(score + "", new Font("Arial", 9.0f, FontStyle.Bold), Brushes.Yellow, new Point(70, 252), new StringFormat(StringFormatFlags.DirectionRightToLeft));
			g.DrawString("Points", new Font("Arial", 9.0f, FontStyle.Bold), Brushes.Yellow, new Point(70, 252));
			// draw lives
			string lives = "Lives"; if( livesLeft == 1 ) lives = "Life";
			g.DrawString(livesLeft + " " + lives, new Font("Arial", 9.0f, FontStyle.Bold), Brushes.Yellow, new Point(130, 252));
			// draw time
			long time = gameState.ElapsedTime / 1000;
			string seconds = (time % 60) + ""; seconds = seconds.PadLeft(2, '0');
			g.DrawString((time / 60) + ":" + seconds, new Font("Arial", 9.0f, FontStyle.Bold), Brushes.Yellow, new Point(190, 252));
			// draw mousePos
			/*try {
				Node mouseNode = gameState.Map.GetNode(mousePos.X, mousePos.Y);
				g.DrawEllipse(new Pen(Brushes.White, 1.0f), new Rectangle(mouseNode.CenterX - 6, mouseNode.CenterY - 6, 13, 13));
			} catch { }*/
			// draw dangerestimates from sectorpac (test)
			if( gameState.Controller != null ) {
				if( collectSectorData && gameState.Controller.Name == "SectorPac" ) {
					if( sectorDrawing != null ) {
                        gameState.Controller.Draw(g, sectorDrawing);
					}
				}
                gameState.Controller.Draw(g);
			}
			// draw pacman			
			gameState.Pacman.Draw(g, sprites);
			// draw ghosts
			foreach( Ghost ghost in gameState.Ghosts ) {
				ghost.Draw(g, sprites);
			}
			// set picture
			try {
				Picture.Image = image;
			} catch { }
		}

		private int danger = -1;
		private bool skip = false;
		private string acceptKey = "";
		private void collectDangerData(Object o) {
			using( StreamWriter sw = new StreamWriter(File.Open("predictionDangerTrainingData.txt", FileMode.Append)) ) {
				foreach( Direction possible in gameState.Pacman.PossibleDirections() ) {
					Console.Write("Input danger (0-9) for direction " + possible + ": ");
					while( (danger < 0 || danger > 9) && !skip ) {
						System.Threading.Thread.Sleep(100);
					}
					if( skip ) {
						Console.WriteLine("Skipped");						
					} else {
						Console.WriteLine(danger + "");
						sw.Write(possible + ";");
						sw.Write(danger + ";");
                        gameState.Controller.WriteData(sw, (int)possible);
						sw.WriteLine("");
					}
					skip = false;
					danger = -1;
				}
			}
			lastCollection = DateTime.Now;
			collectionInProgress = false;
			collectionInputCalled = false;

			/*
			bool accepted = false;
			while( !accepted ) {
				int[] dangerColors = new int[] { -2, -2, -2, -2, -2, -2 };
				Console.WriteLine("Input danger as 0 (very safe) -> 9 (extreme danger) for the blue area: ");
				for( ; collectIndex < 6; collectIndex++ ) {
					dangerColors[collectIndex] = -1;
					drawScreen(0, 0, dangerColors);
					danger = -1;
					while( danger < 0 || danger > 9 ) {
						System.Threading.Thread.Sleep(100);
					}
					Console.Write(danger + ", ");
					dangerColors[collectIndex] = danger;
				}
				drawScreen(0, 0, dangerColors);
				Console.WriteLine("\nIs this correct (y/n)? ");
				acceptKey = "";
				while( acceptKey != "Y" && acceptKey != "N" ) {
					System.Threading.Thread.Sleep(100);
				}
				if( acceptKey == "Y" ) {
					accepted = true;
				}
				collectIndex = 0;
				using( StreamWriter sw = new StreamWriter(File.Open("sectorTrainingData.txt", FileMode.Append)) ) {
					for( int i = 0; i < 6; i++ ) {
						sw.Write(i + ";");
						sw.Write(dangerColors[i] + ";");
						controller.WriteData(sw,i);
						sw.WriteLine("");
					}
				}
			}
			lastCollection = DateTime.Now;
			collectionInProgress = false;
			collectionInputCalled = false;
			*/ 
		}

		private void tick(uint id, uint msg, ref int userCtx, int rsv1, int rsv2) {
			int livesLeft = gameState.Pacman.Lives;
			int score = gameState.Pacman.Score;

			if( step ) {
				gameState.ResumePlay();
			}

			// update pacman
			if( gameState.Started ) {
				if(gameState.Controller != null ) {
					Direction thinkDirection = gameState.Controller.Think(gameState);
					if( thinkDirection != Direction.None ) {
						gameState.Pacman.SetDirection(thinkDirection);
					}
				} else if( gameStream != null ) {
					if( readIndex == gameStream.Length ) {
						readIndex = 0;
						gameState.InvokeGameOver();
					}
					gameState.Pacman.SetPosition((int)gameStream[readIndex++], (int)gameStream[readIndex++], (Direction)gameStream[readIndex++]);
					livesLeft = (int)gameStream[readIndex++];
					score = (int)gameStream[readIndex++] * 255 + (int)gameStream[readIndex++];
					foreach( Ghost ghost in gameState.Ghosts ) {
						ghost.SetPosition((int)gameStream[readIndex++], (int)gameStream[readIndex++],
							((int)gameStream[readIndex++]) == 1 ? true : false,
							((int)gameStream[readIndex++]) == 1 ? true : false,
							(Direction)gameStream[readIndex++],
							((int)gameStream[readIndex++]) == 1 ? true : false);
					}
				}
			}

			drawScreen(score, livesLeft,null);
			
			// draw shortest path
		/*	if( startNode != null && endNode != null ) {
				startNode.Draw(g, Brushes.Red);
				endNode.Draw(g, Brushes.Red);
				if( true ) {
					List<Node> path = gameState.Map.GetRoute(startNode.X, startNode.Y, endNode.X, endNode.Y);
					if( path != null ) {
						foreach( Node n in path ) {
							n.Draw(g, Brushes.Red);
						}
					}
				}
			}*/
			// update game state
			if( gameState.Started ) {
				gameState.Update();
			}

			if( step ) {
				gameState.PausePlay();
				step = false;
			}
			// set picture
			//Picture.ResumeLayout();
			frames++;
		}

		[DllImport("Winmm.dll")]
		private static extern int timeGetTime();

		[DllImport("winmm.dll")]
		private static extern uint timeGetDevCaps(out TimeCaps timeCaps, int size);

		struct TimeCaps
		{
			public uint minimum;
			public uint maximum;

			public TimeCaps(uint minimum, uint maximum) {
				this.minimum = minimum;
				this.maximum = maximum;
			}
		}

		[DllImport("WinMM.dll", SetLastError = true)]
		private static extern uint timeSetEvent(int msDelay, int msResolution,
					TimerEventHandler handler, ref int userCtx, int eventType);

		[DllImport("WinMM.dll", SetLastError = true)]
		static extern uint timeKillEvent(uint timerEventId);

		public delegate void TimerEventHandler(uint id, uint msg, ref int userCtx,
			int rsv1, int rsv2);
	}
}