using System;

namespace Echoes.Simulation
{
    // M0 field on the simulation plane. Screen pixels are a presentation concern.
    // Adjacent slot centers are CellPitch apart. Ally front-column center (x = 0)
    // to enemy front-column center is FrontGap. Column 2 sits on y = 0; column 1 is +y.
    // The clamp rectangle is the axis-aligned bounds of the 18 slot centers.
    // The patch does not define a larger arena margin.
    public static class FieldLayout
    {
        public const decimal CellPitch = 1.5m;
        public const decimal FrontGap = 4.0m;
        public const int MoveStepMs = 50;
        public const int RetargetIntervalMs = 250;

        public static readonly decimal MinX = -2m * CellPitch;
        public static readonly decimal MaxX = FrontGap + 2m * CellPitch;
        public static readonly decimal MinY = -CellPitch;
        public static readonly decimal MaxY = CellPitch;

        public static void PositionOf(string slotId, out decimal x, out decimal y)
        {
            bool ally;
            FormationRow row;
            int column;
            if (!Slots.TryParse(slotId, out ally, out row, out column))
                throw new ArgumentException("Bad slot id: " + slotId);

            int stepsFromFront = row == FormationRow.Front ? 0 : row == FormationRow.Mid ? 1 : 2;
            decimal depth = stepsFromFront * CellPitch;
            x = ally ? -depth : FrontGap + depth;
            y = (2 - column) * CellPitch;
        }

        public static void Clamp(ref decimal x, ref decimal y)
        {
            if (x < MinX)
                x = MinX;
            else if (x > MaxX)
                x = MaxX;
            if (y < MinY)
                y = MinY;
            else if (y > MaxY)
                y = MaxY;
        }

        public static decimal RoundPos(decimal value)
        {
            return decimal.Round(value, 8, MidpointRounding.AwayFromZero);
        }

        // Deterministic decimal square root. No double, so the golden log does not depend on libm.
        public static decimal Sqrt(decimal value)
        {
            if (value <= 0m)
                return 0m;
            decimal seed = 1m;
            decimal scale = value;
            while (scale >= 100m)
            {
                scale /= 100m;
                seed *= 10m;
            }
            decimal x = seed;
            for (int i = 0; i < 16; i++)
                x = (x + value / x) / 2m;
            return RoundPos(x);
        }
    }
}
