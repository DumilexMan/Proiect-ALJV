using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace MOD_AI
{
    public class CompPawnCombatAnalyzer : ThingComp
    {
        // -------------------------
        // PROPRIETĂȚILE TALE EXISTENTE 
        // (presupunând pe baza felului cum funcționează restul codului)
        // -------------------------
        public int shotsFired = 0;
        public int shotsHit = 0;

        public float Accuracy 
        {
            get
            {
                if (shotsFired == 0) return 0f;
                return (float)shotsHit / shotsFired;
            }
        }

        // -------------------------
        // LOGICA NOUĂ PENTRU TIMP INACTIV
        // -------------------------
        // O valoare mică default pentru a nu confunda sistemul la start
        public int lastAttackTick = -99999; 

        // Această metodă este apelată automat de RimWorld în fiecare tick al colonistului
        public override void CompTick()
        {
            base.CompTick();
            
            Pawn pawn = parent as Pawn;
            if (pawn != null && pawn.stances != null)
            {
                // Verificăm dacă pionul tocmai țintește(Warmup), e gata să tragă sau recuperează
                if (pawn.stances.curStance is Stance_Warmup || pawn.stances.curStance is Stance_Cooldown)
                {
                    // Salvăm tick-ul momentului. Cât timp trage, diferența e mereu 0.
                    lastAttackTick = Find.TickManager.TicksGame;
                }
            }
        }

        // Returnează valoarea cerută de CombatDecisionUtility
        public float SecondsSinceLastAttack 
        {
            get 
            {
                // Pentru raționament la prima trezire, considerăm că au stătut o eternitate (100 secunde)
                if (lastAttackTick < 0) return 100f; 

                int tickDifference = Find.TickManager.TicksGame - lastAttackTick;
                
                // RimWorld rulează în medie 60 ticks per secundă reală (la viteză normală 1x)
                return tickDifference / 60f; 
            }
        }

        // Trebuie să salvăm tick-ul când salvăm jocul, altfel la Load se va reseta la -99999
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref shotsFired, "shotsFired", 0);
            Scribe_Values.Look(ref shotsHit, "shotsHit", 0);
            Scribe_Values.Look(ref lastAttackTick, "lastAttackTick", -99999);
        }
    }
}