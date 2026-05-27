using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace MOD_AI
{
    // O echipă (Squad)
    public class CombatSquad
    {
        public Pawn Leader;
        public List<Pawn> Members = new List<Pawn>();
        public Thing MainTarget;
        public CombatTactic CurrentTactic;
        public CombatTactic PreviousTactic;

        // Memoria Echipei (Învățarea tactică în timp real)
        public int CasualtiesTaken = 0;
        public int TicksSinceLastTacticWorked = 0;
        public bool IsFrustrated = false;
        public bool FearsRush = false;
    }

    public enum CombatTactic
    {
        None,
        GroupUp,
        RangedAssault,
        MeleePush,
        Flanking
    }

    // Managerul atașat hărții
    public class RaidSquadManager : MapComponent
    {
        public List<CombatSquad> ActiveSquads = new List<CombatSquad>();

        public RaidSquadManager(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            // Rulăm calculul la ~2 secunde (120 ticks) 
            if (Find.TickManager.TicksGame % 120 == 0)
            {
                UpdateSquadTactics();
            }
        }

        public void RegisterNewSquad(List<Pawn> raiders)
        {
            if (raiders == null || !raiders.Any()) return;

            CombatSquad squad = new CombatSquad();

            squad.Leader = raiders.OrderByDescending(p => p.skills?.GetSkill(SkillDefOf.Shooting).Level ?? 0).First();

            foreach (var raider in raiders)
            {
                squad.Members.Add(raider);
            }

            squad.CurrentTactic = CombatTactic.GroupUp;
            ActiveSquads.Add(squad);

            Log.Message($"[MOD_AI] O noua echipa a fost formata din {squad.Members.Count} membri. Lider desemnat: {squad.Leader.Name.ToStringShort} ({squad.Leader.KindLabel})");
        }

        private void UpdateSquadTactics()
        {
            ActiveSquads.RemoveAll(s => s.Leader == null || s.Leader.Dead || !s.Members.Any(m => !m.Dead));

            foreach (var squad in ActiveSquads)
            {
                if (squad.Leader == null || squad.Leader.Dead)
                {
                    squad.Leader = squad.Members.FirstOrDefault(m => m != null && !m.Dead);
                    if (squad.Leader == null) continue;
                }

                EvaluateTacticFor(squad);
            }
        }

        private void EvaluateTacticFor(CombatSquad squad)
        {
            Pawn leader = squad.Leader;
            Thing threat = leader.mindState.enemyTarget;

            squad.PreviousTactic = squad.CurrentTactic;

            // ===================================
            // 1. ÎNVĂȚAREA ÎN TIMP REAL (MACHINE LEARNING)
            // ===================================

            // Numărăm câți membri echipa a pierdut (Morți sau downed)
            int deadOrDowned = squad.Members.Count(m => m == null || m.Dead || m.Downed);

            if (deadOrDowned > squad.CasualtiesTaken)
            {
                squad.CasualtiesTaken = deadOrDowned;

                // Dacă inamicii mureau în timp ce încercau să facă Push...
                if (squad.CurrentTactic == CombatTactic.MeleePush)
                {
                    squad.FearsRush = true; // S-au ars, refuză să mai atace frontal!
                    Log.Message($"[MOD_AI Learning] Squad-ul Liderului {leader.LabelShort} a luat frica de Rush! (Au murit {(float)squad.CasualtiesTaken / squad.Members.Count * 100}% din ei)");
                }
            }

            if (threat == null || !leader.CanSee(threat))
            {
                squad.TicksSinceLastTacticWorked += 120;
                if (squad.TicksSinceLastTacticWorked > 4500) // ~75 secunde de blocaj total
                {
                    squad.IsFrustrated = true;
                    Log.Message($"[MOD_AI Learning] Squad-ul se frustreaza. Schimba pe distrugere/oportunism/flancare.");
                }
            }
            else
            {
                squad.TicksSinceLastTacticWorked = 0;
            }

            // ===================================
            // 2. APLICAREA DECIZIEI TACTICE 
            // ===================================

            if (threat == null)
            {
                squad.CurrentTactic = squad.IsFrustrated ? CombatTactic.Flanking : CombatTactic.GroupUp;
            }
            else
            {
                squad.MainTarget = threat;

                bool wantRush = CombatDecisionUtility.ShouldRush(threat as Pawn);

                // Dacă regula obișnuită vrea Rush, dar ne e FRICĂ (au murit pioni pe Rush înainte)
                // Ignorăm comanda de rush și folosim trasul de la depărtare! Îi silim să stea cuminți.
                if (wantRush && squad.FearsRush)
                {
                    squad.CurrentTactic = CombatTactic.RangedAssault;
                }
                else if (wantRush)
                {
                    squad.CurrentTactic = CombatTactic.MeleePush;
                }
                else if (CombatDecisionUtility.ShouldFlank(threat as Pawn) || squad.IsFrustrated)
                {
                    squad.CurrentTactic = CombatTactic.Flanking;
                }
                else
                {
                    squad.CurrentTactic = CombatTactic.RangedAssault;
                }
            }

            // Evită spamul din consolă la schimbarea tacticilor:
            if (squad.CurrentTactic != squad.PreviousTactic)
            {
                string logMessage = $"[MOD_AI Squad] Liderul '{squad.Leader.LabelShort}' a ordonat echipei " +
                                    $"schimbarea tacticii din {squad.PreviousTactic} in = {squad.CurrentTactic} =!";
                if (squad.MainTarget != null) logMessage += $" Tinta: {squad.MainTarget.LabelShort}";
                Log.Message(logMessage);
            }
        }

        public CombatSquad GetSquadFor(Pawn pawn)
        {
            return ActiveSquads.FirstOrDefault(s => s.Members.Contains(pawn));
        }
    }
}
