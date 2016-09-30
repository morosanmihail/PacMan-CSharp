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
		private uint fastTimer;
		private uint frames = 0;
		private static Image image;
		private Graphics g;
		private TimerEventHandler tickHandler;
		private Keys lastKey = Keys.None;
		private static Image sprites;		
		private GameState gameState;
		private BasePacman controller;
		private int readIndex = 0;
		private Point mousePos = new Point(0, 0);
		private bool step = false;
		// sector train data collection
		private const bool collectSectorData = false;
		private DateTime lastCollection = DateTime.Now;
		private bool collectionInProgress = false;
		
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
            Console.WriteLine(" 4 - SimRandom");
            int Selection = int.Parse(Console.ReadKey().KeyChar.ToString());

            switch(Selection)
            {
                case 1:
                    gameState.Controller = new LucPacScripted();
                    break;
                case 2:
                    gameState.Controller = new LucPac();
                    break;
                case 3:
                    gameState.Controller = new MMPac.MMLocPac("NeuralNetworkLocPac.nn");
                    break;
                default:
                    gameState.Controller = new SimGreedyRandom();
                    break;
            }

            controller = gameState.Controller;

            int myData = 0; // dummy data
			tickHandler = new TimerEventHandler(tick);
			fastTimer = timeSetEvent(50, 50, tickHandler, ref myData, 1);			
		}

		private void gameOverHandler(object sender, EventArgs args) {
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
			if( controller != null ) {
				if( collectSectorData && controller.Name == "SectorPac" ) {
					if( sectorDrawing != null ) {
						controller.Draw(g, sectorDrawing);
					}
				}
				controller.Draw(g);
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

		private void tick(uint id, uint msg, ref int userCtx, int rsv1, int rsv2) {
			if( controller != null ) {

			}

			//Picture.SuspendLayout(); // I doubt this helps here ... (how to stop unfinished drawings being shown ...)

			int livesLeft = gameState.Pacman.Lives;
			int score = gameState.Pacman.Score;

			if( step ) {
				gameState.ResumePlay();
			}

			// update pacman
			if( gameState.Started ) {
				if( controller != null ) {
					Direction thinkDirection = controller.Think(gameState);
					if( thinkDirection != Direction.None ) {
						gameState.Pacman.SetDirection(thinkDirection);
					}
				}
			}

			drawScreen(score, livesLeft,null);

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

		public delegate void TimerEventHandler(uint id, uint msg, ref int userCtx,
			int rsv1, int rsv2);
	}
}