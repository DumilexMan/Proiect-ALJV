using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using Verse;

namespace MOD_AI
{
    [StaticConstructorOnStartup]
    public static class Init
    {
        static Init()
        {
            var harmony = new Harmony("mod_ai.core");
            harmony.PatchAll();

            Log.Message("[MOD_AI] Harmony initialized!");
        }
    }
}
