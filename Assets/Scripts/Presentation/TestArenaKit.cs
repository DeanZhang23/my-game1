using UnityEngine;

namespace Echoes.Presentation
{
    // Sprites and clips already imported on main. TestArenaApp wires them.
    // Locked environment plates are not fields here: they have no .meta, and this
    // kit does not invent a guid for them.
    [CreateAssetMenu(menuName = "Echoes/Test Arena Kit")]
    public sealed class TestArenaKit : ScriptableObject
    {
        public Sprite[] hitFlashAlly;
        public Sprite[] hitFlashEnemy;
        public Sprite[] deathPuffAlly;
        public Sprite[] deathPuffEnemy;

        public AudioClip sfxHit;
        public AudioClip sfxDown;
        public AudioClip sfxRoll;
        public AudioClip sfxConfirm;
        public AudioClip sfxMarch;

        public Sprite propMidline;
        public Sprite propFootholds;
        public Sprite propEdgeRubble;
        // Optional composite of the three overlays. Not drawn on top of them.
        public Sprite propDressing;
        // Bound so the loose stamps travel with the kit. The scene draws the
        // three overlays instead of scattering these a second time.
        public Sprite[] looseProps;

        public Sprite btnPrimaryNormal;
        public Sprite btnPrimaryHover;
        public Sprite btnPrimaryPressed;
        public Sprite btnPrimaryDisabled;
        public Sprite btnSecondaryNormal;
        public Sprite btnSecondaryHover;
        public Sprite btnSecondaryPressed;
        public Sprite btnSecondaryDisabled;

        public Sprite barTrack;
        public Sprite barTrackReady;
        public Sprite barFillFate;
        public Sprite barFillHp;
        public Sprite barFillShield;
        public Sprite barTick;
        public Sprite barTickReady;
        public Sprite endcapLeft;
        public Sprite endcapRight;
        public Sprite endcapLeftReady;
        public Sprite endcapRightReady;
        public Sprite ornamentBlood;

        public Sprite nameplateAlly;
        public Sprite nameplateEnemy;
        public Sprite nameplateDead;
        public Sprite ornamentBoneCorner;
        public Sprite ornamentSkullRust;
        public Sprite ornamentSkullAsh;

        public Sprite diceSlotEmpty;
        public Sprite badgeCombo;
        public Sprite pendingGlow;
    }
}
