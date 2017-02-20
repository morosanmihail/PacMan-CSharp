using Pacman.GameLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PacmanAI
{
    public class UncertainAgent : BasePacman
    {
        private LucPac SmartAgent = new LucPac();
        private LucPacScripted DumbAgent = new LucPacScripted();
        private bool UseSmart;

        public UncertainAgent() : base("UncertainAgent")
        {
            UseSmart = GameState.Random.NextDouble() > 0.5f;
            LucPac.REMAIN_QUIET = true;
            LucPacScripted.REMAIN_QUIET = true;

            Console.WriteLine("I am being " + (UseSmart ? "smart" : "dumb") + " this time.");
        }

        public override Direction Think(GameState gs)
        {
            return UseSmart ? SmartAgent.Think(gs) : DumbAgent.Think(gs);
        }

        public override void LevelCleared()
        {
            if (UseSmart)
                SmartAgent.LevelCleared();
            else
                DumbAgent.LevelCleared();
        }

        public override void EatPill()
        {
            if (UseSmart)
                SmartAgent.EatPill();
            else
                DumbAgent.EatPill();
        }

        public override void EatPowerPill()
        {
            if (UseSmart)
                SmartAgent.EatPowerPill();
            else
                DumbAgent.EatPowerPill();
        }

        public override void EatenByGhost()
        {
            if (UseSmart)
                SmartAgent.EatenByGhost();
            else
                DumbAgent.EatenByGhost();
        }
    }
}
