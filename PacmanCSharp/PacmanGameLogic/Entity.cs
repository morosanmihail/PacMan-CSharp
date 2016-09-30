using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.GameLogic
{
    [Serializable()]
	public abstract class Entity
    {
        public const int Width = 14;
		public const int Height = 14;

		protected double x, y;
		public double Speed = 0.0f;		
		protected Direction direction;
		protected Direction lastNoneDirection;
		
		public Direction NextDirection;
        public GameState GameState;

		public Node Node;
        Point lastPosition = new Point(-1000, 0);

        public Direction Direction
        {
            get { return direction; }
        }

        
		//private Point lastPosition; // for use with setPosition
		public void SetPosition(int x, int y) {
            Node = GameState.Map.GetNode(x, y);
			this.x = x;
			this.y = y;
		}


		public void SetRoadPosition(int x, int y) {
            Node = GameState.Map.GetNodeNonWall(x, y);
			this.x = x;
			this.y = y;
			if (lastPosition.X != -1000) {
				if (Math.Abs(x - lastPosition.X) > 4.0) {
					if (x < lastPosition.X) {
						this.direction = Direction.Left;
						//Console.WriteLine("left");
					} else {
						this.direction = Direction.Right;
						//Console.WriteLine("right");
					}
					//Console.WriteLine("X trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
				}
				if (Math.Abs(y - lastPosition.Y) > 4.0) {
					if (y < lastPosition.Y) {
						this.direction = Direction.Up;
						//Console.WriteLine("up");
					} else {
						this.direction = Direction.Down;
						//Console.WriteLine("down");
					}
					//Console.WriteLine("Y trigger: " + x + " : " + lastPosition.X + " ... " + y + " : " + lastPosition.Y);
				}
			}
			lastPosition = new Point(x, y);
		}
		
		public int X { get { return (int)Math.Round(x); } }
		public int Y { get { return (int)Math.Round(y); } }
		public int ImgX { get { return X - 7; } }
		public int ImgY { get { return Y - 7; } }
		public double Xf { get { return x; } }
		public double Yf { get { return y; } }

		public Entity(int x, int y, GameState GameState) {
			this.direction = Direction.Left;
			this.NextDirection = Direction.Left;
			this.x = x;
			this.y = y;
			this.GameState = GameState;
			this.Node = GameState.Map.GetNode(x, y);			
		}

		protected bool checkDirection(Direction checkDirection){
			switch( checkDirection ) {
				case Direction.Up:
					if(Node.Up.Type != Node.NodeType.Wall ) {
						return true;
					}
					break;
				case Direction.Down:
					if(Node.Down.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
				case Direction.Left:
					if(Node.Left.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
				case Direction.Right:
					if(Node.Right.Type != Node.NodeType.Wall ) {						
						return true;
					}
					break;
			}
			return false;
		}

		protected bool setNextDirection() {
			if( NextDirection == direction )
				return false;
			switch( NextDirection ) {
				case Direction.Up: 
					if(Node.Up.Type != Node.NodeType.Wall ) { 
						direction = NextDirection; 
						this.x = Node.CenterX; 
						this.y = Node.CenterY;
                        Node = Node.Up; 
						return true; 
					}
					break;
				case Direction.Down: 
					if(Node.Down.Type != Node.NodeType.Wall ) { 
						direction = NextDirection; 
						this.x = Node.CenterX; 
						this.y = Node.CenterY;
                        Node = Node.Down; 
						return true; 
					} 
					break;
				case Direction.Left: 
					if( Node.Left.Type != Node.NodeType.Wall ) { 
						direction = NextDirection; 
						this.x = Node.CenterX; 
						this.y = Node.CenterY; 
						Node = Node.Left; 
						return true; 
					} 
					break;
				case Direction.Right: 
					if( Node.Right.Type != Node.NodeType.Wall ) { 
						direction = NextDirection; 
						this.x = Node.CenterX; 
						this.y = Node.CenterY; 
						Node = Node.Right; 
						return true; 
					} 
					break;
			}
			return false;
		}

		protected virtual void ProcessNode() { }

        protected virtual void ProcessNodeSimulated() { }

        public virtual void MoveSimulated()
        {
            double curSpeed = Speed;
            Ghosts.Ghost ghost = this as Ghosts.Ghost;
            if (ghost != null)
            {
                if (!ghost.Entered)
                {
                    // going back speed
                }
                else if (GameState.Map.Tunnels[Node.Y] && (Node.X <= 1 || Node.X >= Map.Width - 2))
                {
                    curSpeed = ghost.TunnelSpeed; // Ghosts.Ghost.TunnelSpeed;
                }
                else if (!ghost.Chasing)
                {
                    curSpeed = ghost.FleeSpeed; // Ghosts.Ghost.FleeSpeed;
                }
            }

            //Console.WriteLine(direction + " << going " + Node.X + "," + Node.Y + ", " + Node.Type + " ::: " + X + "," + Y + " : " + Node.CenterX + "," + Node.CenterY);
            switch (direction)
            {
                case Direction.Up:
                    if (this.y > Node.CenterY)
                    { // move towards target Node if we haven't reached it yet
                        this.y -= curSpeed;
                    }
                    if (this.y <= Node.CenterY)
                    {
                        ProcessNodeSimulated();
                        if (!setNextDirection())
                        { // try to change direction
                            if (Node.Up.Type == Node.NodeType.Wall)
                            {
                                this.y = Node.CenterY;
                            }
                            else
                            {
                                Node = Node.Up;
                            }
                        };
                    }
                    break;
                case Direction.Down:
                    if (this.y < Node.CenterY)
                    {
                        this.y += curSpeed;
                    }
                    if (this.y >= Node.CenterY)
                    {
                        ProcessNodeSimulated();
                        if (!setNextDirection())
                        {
                            if (Node.Down.Type == Node.NodeType.Wall)
                            {
                                this.y = Node.CenterY;
                            }
                            else
                            {
                                Node = Node.Down;
                            }
                        };
                    }
                    break;
                case Direction.Left:
                    if (Node.X == Map.Width - 1 && this.x < 10)
                    { // check wrapping round // buggy on map 3
                        this.x -= curSpeed;
                        if (this.x < 0)
                            this.x = GameState.Map.PixelWidth + this.x;
                    }
                    else
                    {
                        if (this.x > Node.CenterX)
                        {
                            this.x -= curSpeed;
                        }
                        if (this.x <= Node.CenterX)
                        {
                            ProcessNodeSimulated();
                            if (!setNextDirection())
                            {
                                if (Node.Left.Type == Node.NodeType.Wall)
                                {
                                    this.x = Node.CenterX;
                                }
                                else
                                {
                                    Node = Node.Left;
                                }
                            };
                        }
                    }
                    break;
                case Direction.Right:
                    if (Node.X == 0 && this.x > GameState.Map.PixelWidth - 10)
                    { // check wrapping round // buggy on map 3
                        this.x += curSpeed;
                        if (this.x > GameState.Map.PixelWidth)
                            this.x = this.x - GameState.Map.PixelWidth;
                    }
                    else
                    {
                        if (this.x < Node.CenterX)
                        {
                            this.x += curSpeed;
                        }
                        if (this.x >= Node.CenterX)
                        {
                            ProcessNodeSimulated();
                            if (!setNextDirection())
                            {
                                if (Node.Right.Type == Node.NodeType.Wall)
                                {
                                    this.x = Node.CenterX;
                                }
                                else
                                {
                                    Node = Node.Right;
                                }
                            };
                        }
                    }
                    break;
                case Direction.None:
                    setNextDirection();
                    break;
            }
        }

		public virtual void Move() {
            double curSpeed = Speed;
			Ghosts.Ghost ghost = this as Ghosts.Ghost;
			if( ghost != null ){
				if( !ghost.Entered ) {
					// going back speed
				} 
				else if( GameState.Map.Tunnels[Node.Y] && (Node.X <= 1 || Node.X >= Map.Width - 2) ) {
                    curSpeed = ghost.TunnelSpeed; // Ghosts.Ghost.TunnelSpeed;
				} else if( !ghost.Chasing ) {
                    curSpeed = ghost.FleeSpeed; // Ghosts.Ghost.FleeSpeed;
				}
			}

			//Console.WriteLine(direction + " << going " + Node.X + "," + Node.Y + ", " + Node.Type + " ::: " + X + "," + Y + " : " + Node.CenterX + "," + Node.CenterY);
			switch( direction ) {				
				case Direction.Up:
					if( this.y > Node.CenterY ) { // move towards target Node if we haven't reached it yet
						this.y -= curSpeed;
					}
					if( this.y <= Node.CenterY ) {
						ProcessNode();
						if( !setNextDirection() ) { // try to change direction
							if( Node.Up.Type == Node.NodeType.Wall ) { 
								this.y = Node.CenterY;
							} else { 
								Node = Node.Up;
							}
						};	
					}
					break;
				case Direction.Down:
					if( this.y < Node.CenterY ) {
						this.y += curSpeed;
					}
					if( this.y >= Node.CenterY ) {
						ProcessNode();
						if( !setNextDirection() ) {
							if( Node.Down.Type == Node.NodeType.Wall ) {
								this.y = Node.CenterY;
							} else {
								Node = Node.Down;
							}
						};							
					}
					break;
				case Direction.Left:
					if( Node.X == Map.Width - 1 && this.x < 10 ) { // check wrapping round // buggy on map 3
						this.x -= curSpeed;
						if( this.x < 0 )
							this.x = GameState.Map.PixelWidth + this.x;
					} else {
						if( this.x > Node.CenterX ) {
							this.x -= curSpeed;
						}
						if( this.x <= Node.CenterX ) {
							ProcessNode();
							if( !setNextDirection() ){
								if( Node.Left.Type == Node.NodeType.Wall ) {
									this.x = Node.CenterX;
								} else {
									Node = Node.Left;
								}
							};
						}
					}
					break;
				case Direction.Right:					
					if( Node.X == 0 && this.x > GameState.Map.PixelWidth - 10 ) { // check wrapping round // buggy on map 3
						this.x += curSpeed;
						if( this.x > GameState.Map.PixelWidth )
							this.x = this.x - GameState.Map.PixelWidth;
					} else {
						if( this.x < Node.CenterX ) {
							this.x += curSpeed;
						}
						if( this.x >= Node.CenterX ) {
							ProcessNode();
							if( !setNextDirection() ) {
								if( Node.Right.Type == Node.NodeType.Wall ) {
									this.x = Node.CenterX;
								} else {
									Node = Node.Right;
								}
							};
						}
					}
					break;
				case Direction.None:
					setNextDirection();
					break;
			}
		}

		public float Distance(Entity entity) {
			return (float)Math.Sqrt(Math.Pow(X - entity.X, 2) + Math.Pow(Y - entity.Y, 2));
		}

		public bool IsBelow(Entity entity){
			if( Y <= entity.Y ) 
				return true;
			return false;
		}

		public bool IsAbove(Entity entity) {
			if( Y >= entity.Y )
				return true;
			return false;
		}

		public bool IsLeft(Entity entity) {
			if( X >= entity.X )
				return true;
			return false;
		}

		public bool IsRight(Entity entity) {
			if( X <= entity.X )
				return true;
			return false;
		}

		public List<Direction> PossibleDirections() {
			List<Direction> possible = new List<Direction>();
            /*if (this.GameState.Pacman.Lives < 0)
                return possible;
			*/
            if( Node.Up.Type != Node.NodeType.Wall ) possible.Add(Direction.Up);
			if( Node.Down.Type != Node.NodeType.Wall ) possible.Add(Direction.Down);
			if( Node.Left.Type != Node.NodeType.Wall ) possible.Add(Direction.Left);
			if( Node.Right.Type != Node.NodeType.Wall ) possible.Add(Direction.Right);
			return possible;
		}

		public bool PossibleDirection(Direction d) {
			switch(d){
				case Direction.Up: return Node.Up.Type != Node.NodeType.Wall;
				case Direction.Down: return Node.Down.Type != Node.NodeType.Wall;
				case Direction.Left: return Node.Left.Type != Node.NodeType.Wall;
				case Direction.Right: return Node.Right.Type != Node.NodeType.Wall;
			}
			return false;
		}

		public Direction InverseDirection(Direction d) {
			switch( d ) {
				case Direction.Up: return Direction.Down;
				case Direction.Down: return Direction.Up;
				case Direction.Left: return Direction.Right;
				case Direction.Right: return Direction.Left;
			}
			return Direction.None;
		}

		public virtual void Draw(Graphics g, Image sprites) {
			g.DrawRectangle(new Pen(Brushes.Red), new Rectangle(ImgX, ImgY, Width, Height));
		}
	}

	public enum Direction { Up = 0, Down = 1, Left = 2, Right = 3, None = 5, Stall = 4 };
}
