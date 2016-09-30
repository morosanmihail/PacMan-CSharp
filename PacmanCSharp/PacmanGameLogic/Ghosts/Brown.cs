using System;
using System.Collections.Generic;
using System.Text;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Pacman.GameLogic.Ghosts
{
    [Serializable()]
	public class Brown : Ghost
	{
		public const int StartX = 127, StartY = 118;
		private const int firstWaitToEnter = 20, secondWaitToEnter = 30;

		public Brown(int x, int y, GameState gameState, double Speed, double FleeSpeed)
			: base(x, y, gameState) {
			this.name = "Brown";
			this.drawOffset = Height * 3;
			ResetPosition();
            this.Speed = Speed;
            this.FleeSpeed = FleeSpeed;
        }

		public override void PacmanDead() {
			waitToEnter = secondWaitToEnter;
			ResetPosition();
		}

		public override void ResetPosition() {
			x = StartX;
			y = StartY;
			waitToEnter = firstWaitToEnter;
			direction = Direction.Up;
			base.ResetPosition();
		}

		public override void Move() {
			MoveRandom();
			base.Move();
		}

        public new Brown Clone()
        {
            Brown _temp = (Brown)this.MemberwiseClone();
            _temp.Node = Node.Clone();

            return _temp;
        }
    }
}
