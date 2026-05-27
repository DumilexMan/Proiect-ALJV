using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    public class JobGiver_MeleeRush : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            Thing target = pawn.mindState.enemyTarget;
            if (target == null) return null;

            // Folosim funcția gândita deja pentru Rush!
            // Etapele inferioare (Suppress, Ranged) rulează doar dacă aceasta returnează null
            if (CombatDecisionUtility.ShouldRush(target as Pawn))
            {
                Job rushJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
                rushJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return rushJob;
            }

            // Dacă ShouldRush zice FALS, dăm pass și lasă JobGiver_RangedAttack (din XML) să decidă
            return null;
        }
    }
}