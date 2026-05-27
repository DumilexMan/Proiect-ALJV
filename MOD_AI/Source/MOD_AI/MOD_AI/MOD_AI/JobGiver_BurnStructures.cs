using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    public class JobGiver_BurnStructures : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.mindState.enemyTarget != null) return null;

            bool IsFlammableAndPlayerOwned(Thing t)
            {
                if (t.Faction != Faction.OfPlayer && !(t is Plant p && p.sown)) return false;

                float flammability = t.GetStatValue(StatDefOf.Flammability);
                return flammability > 0.5f;
            }

            Thing targetToBurn = GenClosest.ClosestThingReachable(
                pawn.Position,
                pawn.Map,
                // FIX 3: Eliminăm ThingRequestGroup.Thing (Rimworld nu lasă o căutare atât de generică pentru performanță)
                // Folosim "HaulableEver" pentru iteme lăsate pe jos (suporturi, mese) sau "BuildingArtificial"
                ThingRequest.ForGroup(ThingRequestGroup.BuildingArtificial),
                PathEndMode.Touch,
                // FIX 4: La fel, schimbăm TraverseMode
                TraverseParms.For(pawn, Danger.Deadly, TraverseMode.NoPassClosedDoors, false),
                maxDistance: 40f,
                validator: t => IsFlammableAndPlayerOwned(t) && pawn.CanReserve(t)
            );

            if (targetToBurn != null)
            {
                return JobMaker.MakeJob(JobDefOf.Ignite, targetToBurn);
            }

            return null;
        }
    }
}