using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace MOD_AI
{
    [HarmonyPatch(typeof(JobGiver_AIFightEnemy), "TryGiveJob")]
    public static class Patch_FightEnemy
    {
        private static float utl = -10.0f;
        public static void Postfix(Pawn pawn, ref Job __result)
        {
            if (pawn.Faction == Faction.OfPlayer) return;

            Pawn target = pawn.mindState.enemyTarget as Pawn;
            if (target == null) return;

            if (CombatDecisionUtility.ShouldRush(target))
            {
                __result = JobMaker.MakeJob(JobDefOf.AttackMelee, target);


                bool spaming = false;

                if (!spaming)
                {
                    Log.Message($"[AI] {pawn.Name} rushes {target.Name}!");
                    spaming = true;
                }
                //once every 10 seconds spaming is true
                float ct = Time.realtimeSinceStartup;
                if (ct - utl > 10f)
                {
                    spaming = false;
                }
            }
        }
    }
}