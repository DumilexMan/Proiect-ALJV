using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace MOD_AI
{
    [HarmonyPatch(typeof(Pawn), "Tick")]
    public static class Patch_Pawn_Tickz
    {
        public static void Postfix(Pawn __instance)
        {
            if (__instance.IsColonist)
            {
                //Log.Message($"Pawn {__instance.Name} tick");
            }
        }
    }
}