using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    // Un ThinkNode simplu - decide doar DACA intram in ramura de lupta
    public class ThinkNode_TacticalEvaluation : ThinkNode_Priority
    {
        public override float GetPriority(Pawn pawn)
        {
            // Dacă am văzut un inamic recent sau suntem în combat, acest nod prinde prioritate uriașă
            if (pawn.mindState.enemyTarget != null || pawn.mindState.lastEngageTargetTick > Find.TickManager.TicksGame - 600)
            {
                return 90f;
            }
            return 0f;
        }

        /* 
           Fiindcă moștenește ThinkNode_Priority, el va rula subNodes (MeleeRush, Suppress etc.) 
           în ordinea din XML, de sus în jos, cerându-le un Job până când una returnează ceva valid.
        */
    }
}