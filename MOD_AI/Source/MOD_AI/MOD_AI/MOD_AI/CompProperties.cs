using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;

namespace MOD_AI
{
    public class CompProperties_CombatAnalyzer : CompProperties
    {
        public CompProperties_CombatAnalyzer()
        {
            this.compClass = typeof(CompPawnCombatAnalyzer);
        }
    }
}