using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    public class JobGiver_RetreatIfCriticallyInjured : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Verificăm dacă sub 30% viață. (Poți folosi HitPoints sau Pain)
            if (pawn.health.summaryHealth.SummaryHealthPercent > 0.3f && pawn.health.hediffSet.PainTotal < 0.6f)
            {
                return null; // Nu e rănit grav. Trecem la următorul nod în ThinkTree!
            }

            // Găsim marginea hărții pentru a fugi
            IntVec3 escapeCell;
            if (RCellFinder.TryFindBestExitSpot(pawn, out escapeCell))
            {
                Job retreatJob = JobMaker.MakeJob(JobDefOf.FleeAndCower, escapeCell);
                retreatJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return retreatJob;
            }

            return null;
        }
    }
}