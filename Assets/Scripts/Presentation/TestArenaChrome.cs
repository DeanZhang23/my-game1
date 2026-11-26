using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Echoes.Presentation
{
    public sealed class CombatPlate
    {
        public Image root;
        public Text name;
        public Text hpNum;
        public Text shieldNum;
        public Image hpFill;
        public Image shieldFill;
        public CanvasGroup group;
        public Image bone;
        public Image skullRust;
        public Image skullAsh;
        public Image blood;

        public void Paint(string unitLabel, int hp, int maxHp, int shield, bool ally, bool dead)
        {
            if (name != null)
            {
                name.text = HudMetrics.Ellipsize(unitLabel, 80f);
                name.color = Hex(dead ? HudMetrics.InkDim : HudMetrics.Ink);
            }
            if (hpNum != null)
            {
                hpNum.text = hp.ToString(CultureInfo.InvariantCulture) + "/" + maxHp.ToString(CultureInfo.InvariantCulture);
                hpNum.color = Hex(dead ? HudMetrics.NumberDim : HudMetrics.Ink);
            }
            if (shieldNum != null)
            {
                shieldNum.text = shield.ToString(CultureInfo.InvariantCulture);
                shieldNum.color = Hex(dead ? HudMetrics.NumberDim : HudMetrics.Ink);
            }
            if (hpFill != null)
                hpFill.fillAmount = dead || maxHp <= 0 ? 0f : Mathf.Clamp01(hp / (float)maxHp);
            if (shieldFill != null)
            {
                // Display ratio only. The number is the shield value. No new combat cap.
                float ratio = maxHp <= 0 || shield <= 0 ? 0f : Mathf.Clamp01(shield / (float)maxHp);
                shieldFill.fillAmount = ratio;
            }
            if (group != null)
                group.alpha = dead ? 0.5f : 1f;
            SetActive(bone, !dead && ally);
            SetActive(skullRust, !dead && !ally);
            SetActive(skullAsh, dead);
            SetActive(blood, dead || !ally);
        }

        private static void SetActive(Image image, bool on)
        {
            if (image != null)
                image.gameObject.SetActive(on);
        }

        private static Color Hex(string html)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(html, out color))
                color = Color.white;
            return color;
        }
    }

    // Builds the wired TestArena chrome. Decorative images do not raycast.
    // Do not call SetNativeSize: portraits and VFX stay in fixed boxes.
    public sealed class TestArenaChrome
    {
        private const float RailX = 670f;
        private const float LabelY = 296f;
        private const float BarY = 278f;
        private const float SlotY = 242f;

        private readonly List<Texture2D> _held = new List<Texture2D>();
        private readonly List<Shot> _shots = new List<Shot>();
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly Dictionary<string, Sprite> _diceFaces = new Dictionary<string, Sprite>();
        private TestArenaKit _kit;
        private Font _font;
        private AudioSource _audio;
        private Image _track;
        private Image _fill;
        private Image _tick;
        private Image _capL;
        private Image _capR;
        private Image _stain;
        private Image _edge;
        private Text _fateLabel;
        private Text _fateValue;
        private Text _canRoll;
        private Image _glow;
        private Image _badge;
        private Text _badgeText;
        private readonly Image[] _slotFaces = new Image[3];
        private Sprite _plateAlly;
        private Sprite _plateEnemy;
        private Sprite _plateDead;

        private sealed class Shot
        {
            public Image image;
            public Sprite[] frames;
            public float time;
        }

        private sealed class Pending
        {
            public Image portrait;
            public bool enemy;
            public bool death;
        }

        public void Install(GameObject host, Font font, TestArenaKit kit)
        {
            _font = font;
            _kit = kit;
            EnsureAudioListener();
            if (host != null)
            {
                _audio = host.GetComponent<AudioSource>();
                if (_audio == null)
                    _audio = host.AddComponent<AudioSource>();
                _audio.playOnAwake = false;
                _audio.spatialBlend = 0f;
                _audio.loop = false;
            }
            if (_kit != null)
            {
                _plateAlly = _kit.nameplateAlly;
                _plateEnemy = _kit.nameplateEnemy;
                _plateDead = _kit.nameplateDead;
            }
        }

        public void BuildBackdrop(Transform root)
        {
            // BGFar < BGMid < Ground, then the three dressing overlays. Raycast off.
            // env_riftarena_prop_dressing.png is the optional composite and is not drawn here.
            PlacePlate(root, "env_riftarena_bg_far.png");
            PlacePlate(root, "env_riftarena_bg_mid.png");
            PlacePlate(root, "env_riftarena_ground.png");
            PlaceSprite(root, "PropMidline", _kit != null ? _kit.propMidline : null);
            PlaceSprite(root, "PropFootholds", _kit != null ? _kit.propFootholds : null);
            PlaceSprite(root, "PropEdge", _kit != null ? _kit.propEdgeRubble : null);
        }

        public void BuildForeground(Transform root)
        {
            // FG draws above units and under the rail. It must not take clicks.
            PlacePlate(root, "env_riftarena_fg.png");
        }

        public void BuildRail(Transform root)
        {
            RectTransform row = Box(root, "FateLabel", new Vector2(RailX, LabelY), new Vector2(HudMetrics.FateBarW, 16f));
            _fateLabel = Text(row, 12, TextAnchor.MiddleLeft, new Vector2(-78f, 0f), new Vector2(110f, 16f));
            _canRoll = Text(row, 11, TextAnchor.MiddleCenter, new Vector2(28f, 0f), new Vector2(52f, 16f));
            _fateValue = Text(row, 11, TextAnchor.MiddleRight, new Vector2(86f, 0f), new Vector2(64f, 16f));

            RectTransform bar = Box(root, "FateBar", new Vector2(RailX, BarY), new Vector2(HudMetrics.FateBarW, HudMetrics.FateBarH));
            _track = bar.GetComponent<Image>();
            SkinSlice(_track, _kit != null ? _kit.barTrack : null);
            _fill = Filled(bar, "FateFill", new Vector2(0f, 0f), new Vector2(HudMetrics.FateBarW - 6f, HudMetrics.FateBarH - 6f), _kit != null ? _kit.barFillFate : null, HudMetrics.ShieldFill);
            _edge = Box(bar, "FateEdge", Vector2.zero, new Vector2(3f, 10f)).GetComponent<Image>();
            _edge.color = Hex(HudMetrics.ReadyInk);
            _edge.raycastTarget = false;
            _tick = Box(bar, "FateTick", Vector2.zero, new Vector2(3f, 16f)).GetComponent<Image>();
            _tick.raycastTarget = false;
            _capL = Box(bar, "FateCapL", new Vector2(-HudMetrics.FateBarW * 0.5f, 0f), new Vector2(16f, 16f)).GetComponent<Image>();
            _capL.rectTransform.pivot = new Vector2(0f, 0.5f);
            _capR = Box(bar, "FateCapR", new Vector2(HudMetrics.FateBarW * 0.5f, 0f), new Vector2(14f, 16f)).GetComponent<Image>();
            _capR.rectTransform.pivot = new Vector2(1f, 0.5f);
            _stain = Box(bar, "FateStain", new Vector2(-96f, 0f), new Vector2(12f, 12f)).GetComponent<Image>();
            if (_kit != null && _kit.ornamentBlood != null)
                _stain.sprite = _kit.ornamentBlood;
            _stain.raycastTarget = false;

            _glow = Box(root, "DiceGlow", new Vector2(628f, SlotY), new Vector2(180f, 68f)).GetComponent<Image>();
            if (_kit != null && _kit.pendingGlow != null)
                _glow.sprite = _kit.pendingGlow;
            _glow.raycastTarget = false;
            _glow.enabled = false;

            float[] slotX = { 574f, 628f, 682f };
            for (int i = 0; i < 3; i++)
            {
                RectTransform slot = Box(root, "DiceSlot" + i.ToString(), new Vector2(slotX[i], SlotY), new Vector2(HudMetrics.DiceSlot, HudMetrics.DiceSlot));
                Image slotImage = slot.GetComponent<Image>();
                SkinSlice(slotImage, _kit != null ? _kit.diceSlotEmpty : null);
                RectTransform face = Box(slot, "Face", Vector2.zero, new Vector2(HudMetrics.DiceFace, HudMetrics.DiceFace));
                Image faceImage = face.GetComponent<Image>();
                faceImage.preserveAspect = true;
                faceImage.raycastTarget = false;
                faceImage.enabled = false;
                _slotFaces[i] = faceImage;
            }

            RectTransform badge = Box(root, "ComboBadge", new Vector2(740f, SlotY), new Vector2(52f, 22f));
            _badge = badge.GetComponent<Image>();
            SkinSlice(_badge, _kit != null ? _kit.badgeCombo : null);
            _badgeText = Text(badge, 11, TextAnchor.MiddleCenter, new Vector2(8f, 0f), new Vector2(28f, 18f));
            _badge.gameObject.SetActive(false);
        }

        public void Skin(GameObject button, bool primary)
        {
            if (button == null || _kit == null)
                return;
            Image image = button.GetComponent<Image>();
            Button widget = button.GetComponent<Button>();
            if (image == null || widget == null)
                return;
            Sprite normal = primary ? _kit.btnPrimaryNormal : _kit.btnSecondaryNormal;
            Sprite hover = primary ? _kit.btnPrimaryHover : _kit.btnSecondaryHover;
            Sprite pressed = primary ? _kit.btnPrimaryPressed : _kit.btnSecondaryPressed;
            Sprite disabled = primary ? _kit.btnPrimaryDisabled : _kit.btnSecondaryDisabled;
            if (normal == null)
                return;
            image.sprite = normal;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            widget.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = widget.spriteState;
            state.highlightedSprite = hover;
            state.pressedSprite = pressed;
            state.disabledSprite = disabled;
            widget.spriteState = state;
            ColorBlock colors = widget.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.colorMultiplier = 1f;
            widget.colors = colors;
        }

        public static void PaintLabel(Text text, bool primary, bool enabled)
        {
            if (text == null)
                return;
            string hex = !enabled ? HudMetrics.InkDim : (primary ? HudMetrics.PrimaryInk : HudMetrics.Ink);
            text.color = Hex(hex);
            text.fontStyle = FontStyle.Bold;
        }

        public void PaintFate(int energy, bool rolled, string label, string canRoll)
        {
            FateTrackState state = FateTrack.Classify(energy, rolled);
            bool ready = state == FateTrackState.Ready;
            if (_fateLabel != null)
            {
                _fateLabel.text = label;
                _fateLabel.color = Hex(HudMetrics.Ink);
            }
            if (_fateValue != null)
            {
                _fateValue.text = energy.ToString() + "/" + FateTrack.Max.ToString();
                _fateValue.color = Hex(HudMetrics.Ink);
            }
            if (_canRoll != null)
            {
                bool show = FateTrack.ShowCanRoll(energy, rolled);
                _canRoll.gameObject.SetActive(show);
                _canRoll.text = show ? canRoll : "";
                _canRoll.color = Hex(HudMetrics.ReadyInk);
            }
            if (_track != null && _kit != null)
                _track.sprite = ready ? _kit.barTrackReady : _kit.barTrack;
            if (_fill != null)
                _fill.fillAmount = FateTrack.Max <= 0 ? 0f : Mathf.Clamp01(energy / (float)FateTrack.Max);
            if (_tick != null && _kit != null)
                _tick.sprite = ready ? _kit.barTickReady : _kit.barTick;
            if (_capL != null && _kit != null)
                _capL.sprite = ready ? _kit.endcapLeftReady : _kit.endcapLeft;
            if (_capR != null && _kit != null)
                _capR.sprite = ready ? _kit.endcapRightReady : _kit.endcapRight;
            if (_stain != null)
                _stain.gameObject.SetActive(state == FateTrackState.Spent);
            if (_edge != null && _fill != null)
            {
                _edge.gameObject.SetActive(ready && energy > 0);
                float width = HudMetrics.FateBarW - 6f;
                float filled = width * _fill.fillAmount;
                _edge.rectTransform.anchoredPosition = new Vector2(-width * 0.5f + filled - 1.5f, 0f);
            }
        }

        public void PaintDice(bool pending, string badge, string face0, string face1, string face2)
        {
            if (_glow != null)
                _glow.enabled = pending && _glow.sprite != null;
            bool showBadge = !string.IsNullOrEmpty(badge);
            if (_badge != null)
                _badge.gameObject.SetActive(showBadge);
            if (_badgeText != null)
                _badgeText.text = showBadge ? badge : "";
            PaintFace(0, face0);
            PaintFace(1, face1);
            PaintFace(2, face2);
        }

        public CombatPlate BuildPlate(RectTransform card, bool ally)
        {
            var plate = new CombatPlate();
            float feet = ally ? 0.125f : 0.15f;
            float top = HudMetrics.PlateTopY(UnitCardLayout.PortraitRestY, 32f, feet, HudMetrics.FeetGap);
            RectTransform root = Box(card, "Nameplate", new Vector2(0f, top), new Vector2(HudMetrics.NameplateW, HudMetrics.NameplateH));
            root.pivot = new Vector2(0.5f, 1f);
            Image image = root.GetComponent<Image>();
            image.raycastTarget = false;
            Sprite chrome = ally ? _plateAlly : _plateEnemy;
            if (chrome != null)
            {
                image.sprite = chrome;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
            }
            else
            {
                image.color = new Color(0.08f, 0.09f, 0.11f, 0.92f);
            }
            plate.root = image;
            plate.group = root.gameObject.AddComponent<CanvasGroup>();
            plate.group.blocksRaycasts = false;
            plate.group.interactable = false;

            float x, y, w, h;
            HudMetrics.ContentCenter(4f, 3f, 84f, 16f, out x, out y, out w, out h);
            plate.name = Text(root, 11, TextAnchor.MiddleLeft, new Vector2(x, y), new Vector2(w, h));
            HudMetrics.ContentCenter(4f, 18f, 58f, 26f, out x, out y, out w, out h);
            plate.hpFill = Filled(root, "HpFill", new Vector2(x, y), new Vector2(w, h), _kit != null ? _kit.barFillHp : null, HudMetrics.HpFill);
            HudMetrics.ContentCenter(60f, 18f, 84f, 26f, out x, out y, out w, out h);
            plate.hpNum = Text(root, 9, TextAnchor.MiddleRight, new Vector2(x, y), new Vector2(w, h));
            HudMetrics.ContentCenter(4f, 28f, 58f, 35f, out x, out y, out w, out h);
            plate.shieldFill = Filled(root, "ShieldFill", new Vector2(x, y), new Vector2(w, h), _kit != null ? _kit.barFillShield : null, HudMetrics.ShieldFill);
            HudMetrics.ContentCenter(60f, 28f, 84f, 35f, out x, out y, out w, out h);
            plate.shieldNum = Text(root, 9, TextAnchor.MiddleRight, new Vector2(x, y), new Vector2(w, h));

            plate.bone = Stamp(root, "Bone", new Vector2(-38f, -6f), new Vector2(12f, 12f), _kit != null ? _kit.ornamentBoneCorner : null);
            plate.skullRust = Stamp(root, "SkullRust", new Vector2(-37f, -7f), new Vector2(12f, 12f), _kit != null ? _kit.ornamentSkullRust : null);
            plate.skullAsh = Stamp(root, "SkullAsh", new Vector2(-37f, -7f), new Vector2(12f, 12f), _kit != null ? _kit.ornamentSkullAsh : null);
            plate.blood = Stamp(root, "Blood", new Vector2(36f, -7f), new Vector2(12f, 12f), _kit != null ? _kit.ornamentBlood : null);
            plate.skullRust.gameObject.SetActive(!ally);
            plate.skullAsh.gameObject.SetActive(false);
            plate.blood.gameObject.SetActive(!ally);
            plate.bone.gameObject.SetActive(ally);
            return plate;
        }

        public void RememberPlate(CombatPlate plate, bool ally, bool dead)
        {
            if (plate == null || plate.root == null)
                return;
            Sprite sprite = dead ? _plateDead : (ally ? _plateAlly : _plateEnemy);
            if (sprite != null)
                plate.root.sprite = sprite;
        }

        public void QueueHit(Image portrait, bool enemy)
        {
            if (portrait == null)
                return;
            var pending = new Pending();
            pending.portrait = portrait;
            pending.enemy = enemy;
            pending.death = false;
            _pending.Add(pending);
        }

        public void QueueDeath(Image portrait, bool enemy)
        {
            if (portrait == null)
                return;
            var pending = new Pending();
            pending.portrait = portrait;
            pending.enemy = enemy;
            pending.death = true;
            _pending.Add(pending);
        }

        public void Flush(float dt)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                Pending pending = _pending[i];
                Sprite[] frames = Frames(pending.enemy, pending.death);
                if (frames == null || pending.portrait == null)
                    continue;
                Spawn(pending.portrait, frames, pending.death, pending.enemy);
            }
            _pending.Clear();

            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                Shot shot = _shots[i];
                if (shot.image == null)
                {
                    _shots.RemoveAt(i);
                    continue;
                }
                shot.time += dt;
                int frame;
                bool playing = VfxClock.TryFrame(shot.time, out frame);
                Sprite sprite = FrameAt(shot.frames, frame);
                if (sprite != null)
                    shot.image.sprite = sprite;
                if (!playing)
                {
                    Object.Destroy(shot.image.gameObject);
                    _shots.RemoveAt(i);
                }
            }
        }

        public void Play(AudioClip clip, float volume)
        {
            if (clip == null || _audio == null)
                return;
            // Overlapping hits may overlap. PlayOneShot does not loop.
            // The listener is attached in Install, before the first one-shot.
            _audio.PlayOneShot(clip, volume);
        }

        // PlayOneShot is silent when the scene has no AudioListener. Main Camera
        // carries one. If it is missing, add a single listener there and do not
        // add a second when one already exists.
        private static void EnsureAudioListener()
        {
            if (UnityEngine.Object.FindAnyObjectByType<AudioListener>() != null)
                return;
            Camera camera = Camera.main;
            if (camera == null)
                camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            if (camera == null || camera.GetComponent<AudioListener>() != null)
                return;
            camera.AddComponent<AudioListener>();
        }

        private void Spawn(Image portrait, Sprite[] frames, bool death, bool enemy)
        {
            RectTransform parent = portrait.rectTransform.parent as RectTransform;
            if (parent == null)
                return;
            Vector2 pos = portrait.rectTransform.anchoredPosition;
            float size = portrait.rectTransform.sizeDelta.x;
            if (size <= 0f)
                size = 32f;
            var shot = new Shot();
            shot.frames = frames;
            shot.time = 0f;
            RectTransform rect = Box(parent, death ? "DeathPuff" : "HitFlash", pos, new Vector2(size, size));
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.sprite = FrameAt(frames, 0);
            image.color = Color.white;
            if (death)
            {
                float feetPivot = enemy ? 0.15f : 0.125f;
                float feetY = pos.y - size * 0.5f + size * feetPivot;
                rect.pivot = new Vector2(0.5f, 0.18f);
                rect.anchoredPosition = new Vector2(pos.x, feetY);
            }
            else
            {
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
            shot.image = image;
            _shots.Add(shot);
        }

        private Sprite[] Frames(bool enemy, bool death)
        {
            if (_kit == null)
                return null;
            Sprite[] frames = death
                ? (enemy ? _kit.deathPuffEnemy : _kit.deathPuffAlly)
                : (enemy ? _kit.hitFlashEnemy : _kit.hitFlashAlly);
            if (frames == null || frames.Length != VfxClock.FrameCount)
                return null;
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] == null)
                    return null;
            }
            return frames;
        }

        private void PaintFace(int index, string faceId)
        {
            Image image = _slotFaces[index];
            if (image == null)
                return;
            Sprite sprite = Face(faceId);
            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private Sprite Face(string faceId)
        {
            if (string.IsNullOrEmpty(faceId) || !faceId.StartsWith("face_"))
                return null;
            Sprite cached;
            if (_diceFaces.TryGetValue(faceId, out cached))
                return cached;
            string file = "ui_dice_" + faceId.Substring(5) + "_normal.png";
            Sprite sprite = LoadProjectSprite("_Project/Art/UI/Dice/" + file, new Vector2(0.5f, 0.5f));
            _diceFaces[faceId] = sprite;
            return sprite;
        }

        private void PlacePlate(Transform root, string file)
        {
            // Read the locked PNG. Do not write a .meta and do not change its pivot.
            Sprite sprite = LoadProjectSprite("_Project/Art/Environment/TestArena/" + file, new Vector2(0.5f, 0.5f));
            PlaceSprite(root, file, sprite);
        }

        private void PlaceSprite(Transform root, string name, Sprite sprite)
        {
            if (sprite == null || root == null)
                return;
            RectTransform rect = Box(root, name, Vector2.zero, new Vector2(TestArenaDressing.CanvasW, TestArenaDressing.CanvasH));
            Image image = rect.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            image.preserveAspect = false;
        }

        private Sprite LoadProjectSprite(string projectRelative, Vector2 pivot)
        {
            string path = Path.Combine(Application.dataPath, projectRelative);
            if (!File.Exists(path))
                return null;
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
                return null;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            _held.Add(tex);
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), pivot, 64f);
        }

        private Image Filled(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite, string fallbackHex)
        {
            RectTransform rect = Box(parent, name, pos, size);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.fillAmount = 0f;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.color = Hex(fallbackHex);
            }
            return image;
        }

        private static Image Stamp(RectTransform parent, string name, Vector2 pos, Vector2 size, Sprite sprite)
        {
            RectTransform rect = Box(parent, name, pos, size);
            Image image = rect.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.enabled = false;
            }
            return image;
        }

        private static void SkinSlice(Image image, Sprite sprite)
        {
            if (image == null)
                return;
            image.raycastTarget = false;
            if (sprite == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        private Text Text(RectTransform parent, int size, TextAnchor anchor, Vector2 pos, Vector2 box)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = box;
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Hex(HudMetrics.Ink);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Image image = go.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = false;
            return rt;
        }

        private static Sprite FrameAt(Sprite[] frames, int index)
        {
            if (frames == null || index < 0 || index >= frames.Length)
                return null;
            return frames[index];
        }

        private static Color Hex(string html)
        {
            Color color;
            if (!ColorUtility.TryParseHtmlString(html, out color))
                color = Color.white;
            return color;
        }
    }
}
