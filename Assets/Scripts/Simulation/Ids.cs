using System;

namespace Echoes.Simulation
{
    public static class FaceIds
    {
        public const string Sword = "face_sword";
        public const string Shield = "face_shield";
        public const string Star = "face_star";
        public const string Feather = "face_feather";
        public const string Coin = "face_coin";
        public const string Skull = "face_skull";

        public static readonly string[] Order =
        {
            Sword, Shield, Star, Feather, Coin, Skull
        };

        public static int IndexOf(string faceId)
        {
            for (int i = 0; i < Order.Length; i++)
            {
                if (Order[i] == faceId)
                    return i;
            }
            return -1;
        }
    }

    public static class ComboIds
    {
        public const string None = "combo_none";
        public const string Twin = "combo_twin";
        public const string Attunement = "combo_attunement";
    }

    public static class StatusIds
    {
        public const string Keen = "status_keen";
        public const string Crack = "status_crack";
        public const string Corrosion = "mod_corrosion";
        public const string Wild = "token_wild";
    }

    public static class UnitIds
    {
        public const string Melee = "unit_placeholder_melee";
        public const string Caster = "unit_placeholder_caster";
        public const string Enemy = "unit_placeholder_enemy";
    }

    public static class SkillIds
    {
        public const string MeleeBasic = "skill_placeholder_melee_basic";
        public const string Cleave = "skill_placeholder_cleave";
        public const string CasterBasic = "skill_placeholder_caster_basic";
        public const string Bolt = "skill_placeholder_bolt";
        public const string EnemyBasic = "skill_placeholder_enemy_basic";
    }

    public static class BattleEvents
    {
        public const string BattleStart = "battle_start";
        public const string DiceRoll = "dice_roll";
        public const string DiceLock = "dice_lock";
        public const string DiceReroll = "dice_reroll";
        public const string CrackGained = "crack_gained";
        public const string DiceCommit = "dice_commit";
        public const string ComboResolved = "combo_resolved";
        public const string UnitSpawned = "unit_spawned";
        public const string Damage = "damage";
        public const string ShieldApplied = "shield_applied";
        public const string ShieldExpired = "shield_expired";
        public const string HealOrHpLoss = "heal_or_hp_loss";
        public const string SkillCast = "skill_cast";
        public const string TacticalWindowOpened = "tactical_window_opened";
        public const string TacticalWindowConfirmed = "tactical_window_confirmed";
        public const string TacticalWindowSkipped = "tactical_window_skipped";
        public const string UnitDeath = "unit_death";
        public const string BattleEnd = "battle_end";
    }

    public static class LocKeys
    {
        public const string Title = "ui.test_arena.title";
        public const string Begin = "ui.begin";
        public const string Seed = "ui.seed";
        public const string Reroll = "ui.reroll";
        public const string Commit = "ui.commit";
        public const string Confirm = "ui.confirm";
        public const string Skip = "ui.skip";
        public const string Locale = "ui.locale";
        public const string Crack = "ui.crack";
        public const string CorrosionOn = "ui.corrosion.on";
        public const string CorrosionOff = "ui.corrosion.off";
        public const string FreeReroll = "ui.free_reroll";
        public const string PhaseSetup = "ui.phase.setup";
        public const string PhaseCountdown = "ui.phase.countdown";
        public const string PhaseAuto = "ui.phase.autocombat";
        public const string PhaseTactical = "ui.phase.tactical";
        public const string PhaseResolve = "ui.phase.resolve";
        public const string Victory = "ui.result.victory";
        public const string Defeat = "ui.result.defeat";
        public const string Timeout = "ui.result.timeout";
        public const string TacticalTitle = "ui.tactical.title";
        public const string TacticalBody = "ui.tactical.body";
        public const string Gold = "ui.gold";
        public const string Hp = "ui.hp";
        public const string Shield = "ui.shield";
        public const string Locked = "ui.locked";
        public const string Unlocked = "ui.unlocked";
        public const string Log = "ui.log";
        public const string Combo = "ui.combo";
        public const string CritMark = "log.crit_mark";
        public const string Fallback = "ui.error.catalog_fallback";
        public const string FormationBench = "ui.formation.bench";
        public const string FormationFront = "ui.formation.front";
        public const string FormationMid = "ui.formation.mid";
        public const string FormationBack = "ui.formation.back";
        public const string FormationIllegal = "ui.formation.illegal";
        public const string FormationAlly = "ui.formation.ally";
        public const string FormationEnemy = "ui.formation.enemy";
    }

    public enum FormationRow
    {
        Back = 0,
        Mid = 1,
        Front = 2
    }

    public enum DamageSchool
    {
        Physical = 0,
        Magic = 1
    }

    public enum BattlePhase
    {
        Idle = 0,
        Setup = 1,
        Countdown = 2,
        AutoCombat = 3,
        TacticalWindow = 4,
        Resolve = 5
    }

    public enum TacticalDecision
    {
        Skip = 0,
        Confirm = 1
    }

    public static class Slots
    {
        // Back, mid, front. Column 1 then 2 then 3. Visual order is not this array:
        // allies read back-to-front toward the center; enemies read front-to-back away from it.
        public static readonly string[] AllyIds =
        {
            "ally_a1", "ally_a2", "ally_a3",
            "ally_b1", "ally_b2", "ally_b3",
            "ally_c1", "ally_c2", "ally_c3"
        };

        public static readonly string[] EnemyIds =
        {
            "enemy_a1", "enemy_a2", "enemy_a3",
            "enemy_b1", "enemy_b2", "enemy_b3",
            "enemy_c1", "enemy_c2", "enemy_c3"
        };

        public static FormationRow RowOf(string slotId)
        {
            bool ally;
            FormationRow row;
            int column;
            if (!TryParse(slotId, out ally, out row, out column))
                throw new ArgumentException("Bad slot id: " + slotId);
            return row;
        }

        public static bool IsAlly(string slotId)
        {
            return slotId != null && slotId.StartsWith("ally_", StringComparison.Ordinal);
        }

        public static bool IsAllySlot(string slotId)
        {
            return IndexOf(AllyIds, slotId) >= 0;
        }

        public static bool IsEnemySlot(string slotId)
        {
            return IndexOf(EnemyIds, slotId) >= 0;
        }

        public static bool TryParse(string slotId, out bool ally, out FormationRow row, out int column)
        {
            ally = false;
            row = FormationRow.Back;
            column = 0;
            int index = IndexOf(AllyIds, slotId);
            if (index >= 0)
                ally = true;
            else
            {
                index = IndexOf(EnemyIds, slotId);
                if (index < 0)
                    return false;
            }

            if (index < 3)
                row = FormationRow.Back;
            else if (index < 6)
                row = FormationRow.Mid;
            else
                row = FormationRow.Front;
            column = (index % 3) + 1;
            return true;
        }

        public static string RowName(FormationRow row)
        {
            if (row == FormationRow.Back)
                return "back";
            if (row == FormationRow.Mid)
                return "mid";
            return "front";
        }

        private static int IndexOf(string[] ids, string slotId)
        {
            if (slotId == null)
                return -1;
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] == slotId)
                    return i;
            }
            return -1;
        }
    }
}
