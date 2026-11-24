using System;

namespace Echoes.Simulation
{
    /// <summary>
    /// Mulberry32. One battle seed feeds every roll and crit.
    /// NextUnit is the top 24 bits over 2^24, in [0, 1).
    /// </summary>
    public sealed class DeterministicRng
    {
        private uint _state;

        public DeterministicRng(int seed)
        {
            _state = unchecked((uint)seed);
        }

        public uint NextUInt()
        {
            _state += 0x6D2B79F5u;
            uint z = _state;
            z = (z ^ (z >> 15)) * (z | 1u);
            z ^= z + ((z ^ (z >> 7)) * (z | 61u));
            return z ^ (z >> 14);
        }

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
                throw new ArgumentOutOfRangeException("exclusiveMax");

            uint range = (uint)exclusiveMax;
            uint limit = uint.MaxValue - (uint.MaxValue % range);
            uint value;
            do
            {
                value = NextUInt();
            }
            while (value >= limit);

            return (int)(value % range);
        }

        public decimal NextUnit()
        {
            uint bits = NextUInt() >> 8;
            return bits / 16777216m;
        }
    }
}
