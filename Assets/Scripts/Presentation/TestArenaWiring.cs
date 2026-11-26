namespace Echoes.Presentation
{
    // Presentation mix for the five TestArena clips. Not simulation numbers.
    // Overlapping hits may overlap audio, so the hit volume stays conservative.
    public static class TestArenaSfx
    {
        public const float Hit = 0.70f;
        public const float Down = 0.85f;
        public const float Roll = 0.80f;
        public const float Confirm = 0.75f;
        public const float March = 0.90f;
    }

    public enum FateTrackState
    {
        Empty = 0,
        Filling = 1,
        Ready = 2,
        Spent = 3
    }

    // Fate bar look. Can-roll is energy >= 50 and this battle has not rolled yet.
    // The tick matches BattleSimulator.DiceRollCost. This does not change that rule.
    public static class FateTrack
    {
        public const int Tick = 50;
        public const int Max = 100;

        public static FateTrackState Classify(int energy, bool rolled)
        {
            if (rolled)
                return FateTrackState.Spent;
            if (energy <= 0)
                return FateTrackState.Empty;
            if (energy < Tick)
                return FateTrackState.Filling;
            return FateTrackState.Ready;
        }

        public static bool ShowCanRoll(int energy, bool rolled)
        {
            return energy >= Tick && !rolled;
        }
    }

    // Sizes from docs/ui/m0-testarena-hud.md. Player-visible copy is runtime text.
    public static class HudMetrics
    {
        public const float NameplateW = 88f;
        public const float NameplateH = 38f;
        public const float FeetGap = 2f;
        public const float FateBarW = 240f;
        public const float FateBarH = 16f;
        public const float SecondaryW = 100f;
        public const float SecondaryH = 32f;
        public const float PrimaryW = 208f;
        public const float PrimaryH = 36f;
        public const float DiceSlot = 48f;
        public const float DiceGap = 6f;
        public const float DiceFace = 42f;
        public const float ButtonGap = 8f;
        public const string HpFill = "#8B2E32";
        public const string ShieldFill = "#C9A227";
        public const string Ink = "#E8DCC4";
        public const string InkDim = "#6B6560";
        public const string NumberDim = "#9A938A";
        public const string PrimaryInk = "#0B1220";
        public const string ReadyInk = "#E8C547";

        public const string FateEnergy = "ui.testarena.fate_energy";
        public const string CanRoll = "ui.testarena.can_roll";
        public const string ComboTwin = "combo.twin";
        public const string ComboAttunement = "combo.attunement";
        public const string UnitHp = "ui.unit.hp";
        public const string UnitShield = "ui.unit.shield";

        // Plate pivot is top-center. Returns the plate's anchored Y (its top)
        // just under the resting portrait's feet.
        public static float PlateTopY(float portraitCenterY, float portraitSize, float feetPivot, float gap)
        {
            float feet = portraitCenterY - portraitSize * 0.5f + portraitSize * feetPivot;
            return feet - gap;
        }

        // Content rects are top-left origin on the 88×38 plate (manifest.json).
        // Returns a center position in plate space whose pivot is top-center, +y up.
        public static void ContentCenter(float x0, float y0, float x1, float y1, out float x, out float y, out float w, out float h)
        {
            w = x1 - x0;
            h = y1 - y0;
            x = (x0 + x1) * 0.5f - NameplateW * 0.5f;
            y = -((y0 + y1) * 0.5f);
        }

        // 11px CJK, ~6.2px Latin, matching the nameplate's 80px name field.
        public static string Ellipsize(string value, float maxPx)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            float width = 0f;
            for (int i = 0; i < value.Length; i++)
            {
                float step = value[i] > 127 ? 11f : 6.2f;
                if (width + step > maxPx)
                {
                    if (i <= 0)
                        return "…";
                    return value.Substring(0, i) + "…";
                }
                width += step;
            }
            return value;
        }
    }

    // Locked plates stay guid-less (no .meta). Dressing sits on the ground plate.
    // The three 1280×720 overlays are the authored layout. Loose stamps are the
    // pieces already composited into those overlays; drawing them again would
    // invent positions and double the marks. The optional dressing composite is
    // the same three overlays flattened and is not stacked on top of them.
    public static class TestArenaDressing
    {
        public static readonly string[] LockedPlates =
        {
            "env_riftarena_bg_far.png",
            "env_riftarena_bg_mid.png",
            "env_riftarena_ground.png",
            "env_riftarena_fg.png"
        };

        public static readonly string[] GroundOverlays =
        {
            "env_riftarena_prop_midline.png",
            "env_riftarena_prop_footholds.png",
            "env_riftarena_prop_edge_rubble.png"
        };

        public const string OptionalComposite = "env_riftarena_prop_dressing.png";

        public static readonly string[] LooseStamps =
        {
            "env_riftarena_prop_ash_01.png",
            "env_riftarena_prop_ash_02.png",
            "env_riftarena_prop_ashpuff_01.png",
            "env_riftarena_prop_ashpuff_02.png",
            "env_riftarena_prop_ashpuff_03.png",
            "env_riftarena_prop_ashpuff_04.png",
            "env_riftarena_prop_chip_01.png",
            "env_riftarena_prop_chip_02.png",
            "env_riftarena_prop_crack_01.png",
            "env_riftarena_prop_crack_02.png",
            "env_riftarena_prop_crack_03.png",
            "env_riftarena_prop_crack_04.png",
            "env_riftarena_prop_crack_05.png",
            "env_riftarena_prop_foothold_east.png",
            "env_riftarena_prop_foothold_west.png",
            "env_riftarena_prop_midline_stones.png",
            "env_riftarena_prop_rubble_01.png",
            "env_riftarena_prop_rubble_02.png",
            "env_riftarena_prop_rubble_03.png",
            "env_riftarena_prop_rust_01.png",
            "env_riftarena_prop_rust_02.png",
            "env_riftarena_prop_rust_03.png",
            "env_riftarena_prop_rust_04.png",
            "env_riftarena_prop_rust_05.png"
        };

        public const float CanvasW = 1280f;
        public const float CanvasH = 720f;
    }
}
