using Verse;
using Verse.AI;
using RimWorld;

namespace MOD_AI
{
    public class JobGiver_SquadTactics : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            // Luăm managerul hărții curente
            var squadManager = pawn.Map.GetComponent<RaidSquadManager>();
            if (squadManager == null) return null; // Fallback to vanilla

            // Căutăm echipa pionului
            CombatSquad mySquad = squadManager.GetSquadFor(pawn);

            // Dacă nu e într-o echipă, lăsăm Vanilla/AI normal să acționeze
            if (mySquad == null) return null;

            // ==========================================
            // LOGICA NOUA: SUPPORT LEADER LAYER
            // ==========================================
            // Verificăm dacă pionul chiar poate să ajute și NU este chiar el liderul
            if (pawn != mySquad.Leader)
            {
                Job supportJob = TrySupportLeaderJob(pawn, mySquad);
                if (supportJob != null)
                {
                    return supportJob; 
                }
            }

            // Dacă Liderul nu e în pericol, continuăm cu Tacticile Echipei:
            switch (mySquad.CurrentTactic)
            {
                case CombatTactic.GroupUp:
                    return TryGroupUpJob(pawn, mySquad);

                case CombatTactic.MeleePush:
                    return TryMeleePushJob(pawn, mySquad.MainTarget);

                case CombatTactic.RangedAssault:
                    return TryRangedAssaultJob(pawn, mySquad.MainTarget);

                case CombatTactic.Flanking:
                    return TryFlankJob(pawn, mySquad.MainTarget);
            }

            return null; // Trecem la următorul nod în arbore
        }

        // ==========================================
        // FUNCȚIA DE SUPORT A LIDERULUI
        // ==========================================
        private Job TrySupportLeaderJob(Pawn pawn, CombatSquad squad)
        {
            Pawn leader = squad.Leader;

            // Dacă liderul e pe jos sau mort, nu avem pe cine suporta momentan (managerul va alege alt lider curând)
            if (leader == null || leader.Dead || leader.Downed) return null;

            // SCENARIUL 1: LIDERUL ESTE ATACAT ÎN MELEE
            // Dacă un inamic se luptă corp la corp cu liderul nostru
            if (leader.mindState.meleeThreat != null)
            {
                float distToLeader = pawn.Position.DistanceTo(leader.Position);
                
                // Dacă suntem într-o rază de 15 pătrate, intervenim imediat!
                if (distToLeader <= 15f)
                {
                    Pawn threat = leader.mindState.meleeThreat;

                    // Decidem CUM ajutăm în funcție de arma pe care o avem:
                    bool isRanged = pawn.equipment?.Primary?.def.IsRangedWeapon ?? false;

                    if (!isRanged)
                    {
                        // Avem armă melee: facem urgență alergare (Sprint) să-l tăiem pe atacator
                        Job rescueMelee = JobMaker.MakeJob(JobDefOf.AttackMelee, threat);
                        rescueMelee.locomotionUrgency = LocomotionUrgency.Sprint;
                        return rescueMelee;
                    }
                    else if (pawn.CanSee(threat))
                    {
                        // Avem pușcă și îl vedem pe atacator: tragem automat pe el ca să degajăm liderul
                        Job suppressFire = JobMaker.MakeJob(JobDefOf.Wait_Combat, threat);
                        suppressFire.expiryInterval = 120; // Tragem 2 secunde și reverificăm
                        return suppressFire;
                    }
                }
            }

            // SCENARIUL 2: PIONUL S-A DEPĂRTAT PREA MULT DE LIDER (Straying)
            // Dacă echipa e angajată în RangedAssault sau Melee, dar un pion s-a dus prea departe
            // el va abandona acel inamic și se va întoarce spre grupul principal pentru a forma un zid defensiv.
            float distance = pawn.Position.DistanceTo(leader.Position);
            if (distance > 25f && squad.CurrentTactic != CombatTactic.GroupUp)
            {
                Job returnToFormation = JobMaker.MakeJob(JobDefOf.Goto, leader.Position);
                returnToFormation.locomotionUrgency = LocomotionUrgency.Jog;
                return returnToFormation;
            }

            return null; // Nicio condiție critică de support nu a fost atinsă, trece la atacurile normale.
        }

        private Job TryGroupUpJob(Pawn pawn, CombatSquad squad)
        {
            // Dacă nu ești lider, și ești departe de el, mișcă-te spre el
            if (pawn != squad.Leader)
            {
                float distToLeader = pawn.Position.DistanceTo(squad.Leader.Position);
                if (distToLeader > 8f) // Dacă suntem la mai mult de 8 pătrate
                {
                    Job gotoJob = JobMaker.MakeJob(JobDefOf.Goto, squad.Leader.Position); // Mergem spre lider
                    gotoJob.locomotionUrgency = LocomotionUrgency.Jog;
                    return gotoJob;
                }
            }
            return null; // Suntem destul de aproape, așadar facem ce zice tactica următoare sau vanilla
        }

        private Job TryMeleePushJob(Pawn pawn, Thing target)
        {
            if (target == null || target.Destroyed) return null;

            // Tot squad-ul face push extrem spre ținta liderului
            Job rushJob = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
            rushJob.locomotionUrgency = LocomotionUrgency.Sprint;
            return rushJob;
        }

        private Job TryRangedAssaultJob(Pawn pawn, Thing target)
        {
            if (target == null || target.Destroyed) return null;

            bool isRanged = pawn.equipment?.Primary?.def.IsRangedWeapon ?? false;

            if (isRanged)
            {
                VerbProperties primaryVerb = pawn.equipment.Primary.def.Verbs[0];
                float attackRange = primaryVerb.range;
                float distToTarget = pawn.Position.DistanceTo(target.Position);

                // Dacă AI-ul vede ținta și este într-o distanță excelentă de tragere (nu chiar pe margine ca să nu trebuiască să fugă iar)
                if (distToTarget < attackRange * 0.9f && pawn.CanSee(target))
                {
                    // Îl lăsăm să tragă dându-i comanda Vanilla de Wait_Combat. 
                    // Aceasta este comanda default de "Draft" în care se află un colonist cu arma: 
                    // nu se mișcă, trage automat, respectă cooldown-ul.
                    Job combatJob = JobMaker.MakeJob(JobDefOf.Wait_Combat, target);
                    combatJob.expiryInterval = 200; // Mai stă cam 3 secunde, apoi managerul refaculează
                    return combatJob;
                }
                else
                {
                    // === CERE POZIȚIE PENTRU TRAGERE INTELIGENTĂ (COVER MAXIM) ===
                    CastPositionRequest request = new CastPositionRequest
                    {
                        caster = pawn,
                        target = target,
                        verb = pawn.equipment.PrimaryEq.PrimaryVerb,
                        maxRangeFromTarget = attackRange,
                        wantCoverFromTarget = true,

                        // ===== SETĂRILE NOI / INTELIGENTE =====
                        maxRangeFromCaster = 15f, // Nu-l lăsăm să alerge juma' de hartă după cover, să-l caute aproape
                        preferredCastPosition = pawn.Position, // Dacă e OK aici, să nu se mai plimbe

                        // Aceasta e foarte importantă. Dacă pui true, AI-ul va prefera 
                        // poziții care maximizează Cover-ul (ex: Ziduri vs Copaci) 
                        // și nu va ieși din ele!
                        maxRegions = 5
                    };

                    IntVec3 dest;

                    // Chemăm CastellPositionFinder
                    if (CastPositionFinder.TryFindCastPosition(request, out dest))
                    {
                        // Dacă destinația aleasă este de fapt fix unde e el, nu-i dăm GoTo,
                        // Dăm Wait_Combat ca să tragă!
                        if (dest == pawn.Position)
                        {
                            Job combatJob = JobMaker.MakeJob(JobDefOf.Wait_Combat, target);
                            combatJob.expiryInterval = 200;
                            return combatJob;
                        }

                        // Altfel, aleargă cu disperare spre ZID/Bolovan!
                        Job gotoCastJob = JobMaker.MakeJob(JobDefOf.Goto, dest);
                        gotoCastJob.locomotionUrgency = LocomotionUrgency.Sprint;
                        return gotoCastJob;
                    }
                    else
                    {
                        // Fallback (Nu există cover)
                        // Dacă e chiar imposibil să se ascundă, va rămâne doar pe foc!
                        Job standAndShoot = JobMaker.MakeJob(JobDefOf.Wait_Combat, target);
                        standAndShoot.expiryInterval = 200;
                        return standAndShoot;
                    }
                }
            }

            // Dacă inamicul nu are armă la distanță (are bâtă/sabie), forțează rush de urgență!
            return TryMeleePushJob(pawn, target);
        }
        private Job TryFlankJob(Pawn pawn, Thing target)
        {
            if (target == null || target.Destroyed) return null;

            bool isRanged = pawn.equipment?.Primary?.def.IsRangedWeapon ?? false;

            // Dacă un membru al echipei are doar sabie, va fi nevoit oricum să facă Push direct!
            if (!isRanged) return TryMeleePushJob(pawn, target);

            float attackRange = pawn.equipment.Primary.def.Verbs[0].range;

            // ===== LOGICA DE FLANCARE =====
            // Căutăm o celulă în apropierea țintei (dar destul de departe să nu fie corp-la-corp)
            bool FlankValidator(IntVec3 cell)
            {
                // Trebuie să putem merge acolo
                if (!cell.Walkable(pawn.Map) || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)) return false;

                // Trebuie să o putem vedea pe țintă de acolo (fără ziduri între ei)
                if (!GenSight.LineOfSight(cell, target.Position, pawn.Map)) return false;

                // Nu vrem să depășească range-ul armei
                if (cell.DistanceTo(target.Position) > attackRange * 0.9f) return false;

                // MAGIA: Trebuie să fie DEPARTE de poziția actuală a inamicului (cel puțin 15 blocuri)
                // Asta forțează AI-ul să o ia "pe ocolite", creând un cerc larg (flancare reală)
                if (cell.DistanceTo(pawn.Position) < 15f) return false;

                return true;
            }

            IntVec3 flankPos;
            // Căutăm o celulă random care respectă regula noastră strictă pe o rază de bătaie a armei
            if (CellFinder.TryFindRandomReachableNearbyCell(target.Position, pawn.Map, attackRange * 0.8f, TraverseParms.For(pawn), FlankValidator, null, out flankPos))
            {
                // Îi dăm alergare spre punctul de flanc
                Job flankJob = JobMaker.MakeJob(JobDefOf.Goto, flankPos);
                flankJob.locomotionUrgency = LocomotionUrgency.Sprint;
                return flankJob;
            }

            // Dacă este închis într-un coridor lung și nu are pe unde să ocolească fizic
            // Abandonează flancul și face Ranged Assault normal (cover și tras).
            return TryRangedAssaultJob(pawn, target);
        }
    }
}