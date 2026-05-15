using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace MOD_AI
{
    public class CompPawnCombatAnalyzer : ThingComp
    {
        public int shotsFired;
        public int shotsHit;
        public int ticksIdle;

        public float Accuracy
        {
            get
            {
                if (shotsFired == 0) return 0f;
                return (float)shotsHit / shotsFired;
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent is Pawn pawn)
            {
                if (pawn.stances?.curStance == null)
                {
                    ticksIdle++;
                }
            }
        }
    }
}