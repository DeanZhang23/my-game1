using System;

namespace Echoes.Simulation
{
    public struct FateGrant
    {
        public int Gained;
        public int Added;
        public int Discarded;
        public int Pool;
    }

    // Team fate pool. Damage remainder is shared. Skill-energy remainder stays on each ally.
    // Points that do not fit under the cap are discarded. The remainder is still consumed.
    public sealed class FatePool
    {
        public const int Cap = 100;
        public const int DamageQuantum = 10;
        public const int SkillQuantum = 5;

        public int Pool { get; private set; }
        public int DamageRemainder { get; private set; }

        public FateGrant AddDamage(int reduced)
        {
            if (reduced <= 0)
                return Grant(0);
            int total = DamageRemainder + reduced;
            int points = total / DamageQuantum;
            DamageRemainder = total % DamageQuantum;
            return Grant(points);
        }

        public FateGrant AddFlat(int points)
        {
            return Grant(points);
        }

        public FateGrant AddSkillEnergy(ref decimal remainder, decimal gained)
        {
            if (gained <= 0m)
                return Grant(0);
            if (remainder < 0m)
                remainder = 0m;
            remainder += gained;
            int points = (int)decimal.Floor(remainder / SkillQuantum);
            if (points < 0)
                points = 0;
            remainder -= points * SkillQuantum;
            if (remainder < 0m)
                remainder = 0m;
            return Grant(points);
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0)
                throw new ArgumentOutOfRangeException("cost");
            if (Pool < cost)
                return false;
            Pool -= cost;
            return true;
        }

        internal void SetPool(int value)
        {
            if (value < 0)
                value = 0;
            if (value > Cap)
                value = Cap;
            Pool = value;
        }

        private FateGrant Grant(int points)
        {
            var grant = new FateGrant();
            if (points <= 0)
            {
                grant.Pool = Pool;
                return grant;
            }
            int room = Cap - Pool;
            if (room < 0)
                room = 0;
            int added = points < room ? points : room;
            Pool += added;
            grant.Gained = points;
            grant.Added = added;
            grant.Discarded = points - added;
            grant.Pool = Pool;
            return grant;
        }
    }
}
