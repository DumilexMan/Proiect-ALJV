using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    public class JobGiver_TargetPowerGenerators : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.mindState.enemyTarget != null) return null;

            bool IsValuablePowerSource(Thing thing)
            {
                Building building = thing as Building;
                if (building == null || building.Faction != Faction.OfPlayer) return false;

                var powerComp = building.TryGetComp<CompPowerTrader>();
                var batteryComp = building.TryGetComp<CompPowerBattery>();

                // FIX 1: PowerOutput reprezintă curentul (Negativ = consumă, Pozitiv = Generează). 
                // În versiunile noi de Rimworld accesăm PowerOutput în loc de basePowerConsumption!
                return (powerComp != null && powerComp.PowerOutput > 0) || batteryComp != null;
            }

            Thing targetGenerator = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.Touch,
                // FIX 2: Înlocuim "ByWeight" cu "PassDoors" (dacă vrei să poată lovi uși în drum spre ea)
                // sau cu "NoPassClosedDoors" (să caute doar ce e lăsat pe afară).
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.NoPassClosedDoors, false),
                maxDistance: 60f,
                validator: t => IsValuablePowerSource(t) && pawn.CanReserve(t)
            );

            if (targetGenerator != null)
            {
                return JobMaker.MakeJob(JobDefOf.AttackMelee, targetGenerator);
            }

            return null;
        }
    }
}