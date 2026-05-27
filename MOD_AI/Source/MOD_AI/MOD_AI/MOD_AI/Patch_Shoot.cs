using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using RimWorld;
using Verse;

namespace MOD_AI
{
    [HarmonyPatch(typeof(Verb_Shoot), "TryCastShot")]
    public static class Patch_Shoot
    {
        public static void Postfix(Verb_Shoot __instance, bool __result)
        {
            Pawn caster = __instance.CasterPawn;
            Pawn target = __instance.CurrentTarget.Thing as Pawn;

            if (caster == null || target == null) return;

            var comp = target.TryGetComp<CompPawnCombatAnalyzer>();
            if (comp == null) return;

            comp.shotsFired++;

            if (__result)
                comp.shotsHit++;

            // Afișăm logul o dată la fiecare 10 secvențe de trageri, nu la fiecare glonț.
            // Schimbăm '!=' în '==' pentru a se afișa doar când numărul este multiplu de 10.
            bool shouldLog = comp.shotsFired % 10 == 0;

            if (shouldLog)
            {
                Log.Message($"[MOD_AI] Tinta: {target.LabelShort}. S-au inregistrat {comp.shotsFired} focuri. Acuratete: {comp.Accuracy}");
            }

            // Am eliminat linia Log.Message care declanșa spam de fiecare dată!
        }
    }
}