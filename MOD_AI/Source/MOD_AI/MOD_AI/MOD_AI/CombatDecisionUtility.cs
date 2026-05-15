using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MOD_AI
{
    public static class CombatDecisionUtility
    {
        public static bool ShouldRush(Pawn target)
        {
            var comp = target.TryGetComp<CompPawnCombatAnalyzer>();
            if (comp == null) return false;

            float acc = comp.Accuracy;

            // regula simplă pentru a determina daca se ataca un inamic:
            return acc < 0.3f;
        }
    }
}