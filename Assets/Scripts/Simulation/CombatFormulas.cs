using System;

namespace Echoes.Simulation
{
    public static class CombatFormulas
    {
        public const int ShieldDurationMs = 8000;
        public const decimal Corrosion = 1.15m;
        public const decimal KeenPerStack = 1.20m;
        public const decimal Twin = 1.35m;
        public const decimal Attunement = 1.80m;
        public const decimal MinIntervalSeconds = 0.35m;

        public static int PreCrit(decimal stat, decimal ratio, decimal defense, decimal damageMod)
        {
            if (defense < 0m)
                defense = 0m;
            decimal raw = stat * ratio * (100m / (100m + defense)) * damageMod;
            if (raw < 1m)
                raw = 1m;
            return (int)decimal.Floor(raw);
        }

        public static int ApplyCrit(int preCrit, bool crit, decimal critDamage)
        {
            if (!crit)
                return preCrit;
            return (int)decimal.Floor(preCrit * (1m + critDamage));
        }

        public static decimal AttackIntervalSeconds(decimal baseInterval, decimal attackSpeedBonus)
        {
            decimal denom = 1m + attackSpeedBonus;
            if (denom < 0.0000001m)
                denom = 0.0000001m;
            decimal interval = baseInterval / denom;
            if (interval < MinIntervalSeconds)
                interval = MinIntervalSeconds;
            return interval;
        }

        public static int SecondsToMs(decimal seconds)
        {
            return (int)decimal.Round(seconds * 1000m, 0, MidpointRounding.AwayFromZero);
        }

        public static int IntervalMs(decimal baseInterval, decimal attackSpeedBonus)
        {
            int ms = SecondsToMs(AttackIntervalSeconds(baseInterval, attackSpeedBonus));
            if (ms < 350)
                ms = 350;
            return ms;
        }

        public static int ShieldAmount(int maxHp, decimal lifeRatio)
        {
            if (lifeRatio <= 0m || maxHp <= 0)
                return 0;
            int amount = (int)decimal.Floor(maxHp * lifeRatio);
            return amount < 0 ? 0 : amount;
        }

        public static int SacrificeLoss(int currentHp, decimal fraction)
        {
            if (currentHp <= 1 || fraction <= 0m)
                return 0;
            int loss = (int)decimal.Floor(currentHp * fraction);
            int maxLoss = currentHp - 1;
            if (loss > maxLoss)
                loss = maxLoss;
            return loss < 0 ? 0 : loss;
        }

        public static int Gold(decimal amount)
        {
            if (amount <= 0m)
                return 0;
            return (int)decimal.Floor(amount);
        }

        public static void ApplyShieldThenHp(ref int shield, ref int hp, int damage, out int absorbed, out int hpLost)
        {
            if (damage < 0)
                damage = 0;
            if (shield < 0)
                shield = 0;
            absorbed = damage < shield ? damage : shield;
            shield -= absorbed;
            hpLost = damage - absorbed;
            hp -= hpLost;
            if (hp < 0)
                hp = 0;
        }
    }
}
