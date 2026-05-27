using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MOD_AI
{
    // Ne atașăm direct de baza evenimentului de raid
    [HarmonyPatch(typeof(IncidentWorker_Raid), "TryExecuteWorker")]
    public static class Patch_IncidentWorker_Raid
    {
        public static void Postfix(IncidentParms parms, ref bool __result)
        {
            // Dacă raidul nu a reușit să se genereze sau nu e inamic, ignorăm
            if (!__result || parms.target == null || parms.faction == null || !parms.faction.HostileTo(Faction.OfPlayer))
                return;

            Map map = (Map)parms.target;
            if (map == null) return;

            RaidSquadManager manager = map.GetComponent<RaidSquadManager>();
            if (manager == null) return;

            // Preia toți inamicii de pe hartă din facțiunea respectivă 
            // care încă NU au primit o echipă
            List<Pawn> raiders = map.mapPawns.SpawnedPawnsInFaction(parms.faction)
                .Where(p => manager.GetSquadFor(p) == null && !p.Dead && !p.Downed)
                .ToList();

            if (raiders.Count == 0) return;

            int squadSize = 10;
            for (int i = 0; i < raiders.Count; i += squadSize)
            {
                List<Pawn> squadMembers = raiders.Skip(i).Take(squadSize).ToList();
                manager.RegisterNewSquad(squadMembers);
            }

            Log.Message($"[MOD_AI] Raid inamic procesat. Am preluat trupele si organizat {manager.ActiveSquads.Count} noi echipe tactice.");
        }
    }
}