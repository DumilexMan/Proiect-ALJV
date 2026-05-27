using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

namespace MOD_AI
{
    public static class CombatDecisionUtility
    {
        public static bool ShouldRush(Pawn threat)
        {
            if (threat == null) return false;

            var comp = threat.TryGetComp<CompPawnCombatAnalyzer>();
            // Dacă un inamic nu are complet acest Comp atașat (ex. un animal), inamicii noștri se apropie de obicei
            if (comp == null) return true; 

            // ===============================================
            // LOGICA NOUĂ: REACȚIA LA COMPETENȚA COLONISTULUI
            // ===============================================

            // Acuratețea din clasa ta de analiză (presupunând că 'Accuracy' returnează o valoare de la 0 la 1)
            float threatAccuracy = comp.Accuracy; 

            // Câte focuri valide a asimilat statistica modului tău
            int totalShots = comp.shotsFired;

            // 1. DACA COLONISTUL ESTE FOARTE PRECIS: Inamicii nu au curaj să facă rush
            // Dacă ținta a tras măcar câteva gloanțe și are o acuratețe mare (ex: hit rate mai mare de 40%)
            if (totalShots > 3 && threatAccuracy >= 0.4f)
            {
                return false; // NU mai face Rush. Forțează RangedAssault / Cover!
            }

            // 2. DACA COLONISTUL TRAGE BINE, DAR FACE EROARE / NU MAI TRAGE TIMP DE 8 SECUNDE:
            // "Trage de timp" - Atunci reluăm Rush-ul
            float timpInactiv = comp.SecondsSinceLastAttack; // Proprietatea creată anterior
            if (timpInactiv > 8f)
            {
                return true; // L-am prins la reîncărcare, luăm cu asalt!
            }

            // 3. DACA E UN COLONIST SLAB SAU INCLUSIV RĂNIT: Prind curaj
            // Dacă dă cu rateuri multe (ex. sub 25% acuratețe), se apropie imediat (Rush!)
            if (totalShots > 2 && threatAccuracy < 0.25f)
            {
                return true; 
            }

            // Comportament default (pentru primele 1-2 secunde cât abia văd ținta, nu pot decide clar)
            // Lăsăm pe false (să prefere să stea după un bloc la începutul luptei)
            return false;
        }

        public static bool ShouldFlank(Pawn threat)
        {
            if (threat == null) return false;

            var comp = threat.TryGetComp<CompPawnCombatAnalyzer>();
            if (comp == null) return false;

            // Dacă inamicul stă de 3-8 secunde fără să tragă (dar nici 
            // destul încât ShouldRush să dea push), este momentul perfect pentru manevre laterale.
            float timpInactiv = comp.SecondsSinceLastAttack;

            if (timpInactiv > 3f && timpInactiv <= 8f)
            {
                return true;
            }

            // Putem adăuga și șansă pur tactică: liderii cu skill mare de Shooting preferă Flancul
            return false;
        }
    }
}