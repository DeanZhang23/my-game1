using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Echoes.Simulation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echoes.Presentation
{
    public sealed class TestArenaApp : MonoBehaviour
    {
        public EncounterDefinitionAsset encounter;
        public LocalizationTableAsset localization;
        public FaceCatalogAsset faces;
        // Idle stills only. Melee shows ashguard, caster shows riftmage, enemies show the placeholder.
        public UnitSpriteCatalog sprites;
        public int fixedSeed;

        private BattleSimulator _sim;
        private EncounterDef _def;
        private string _locale = "zh-Hans";
        private int _applied;
        private float _countdown;
        private bool _usedFallback;
        private Font _font;
        private InputField _seedInput;
        private Text _title;
        private Text _seedLabel;
        private Text _beginLabel;
        private Text _rerollLabel;
        private Text _commitLabel;
        private Text _confirmLabel;
        private Text _skipLabel;
        private Text _localeLabel;
        private Text _status;
        private Text _result;
        private Text _tacticalTitle;
        private Text _tacticalBody;
        private Text _log;
        private RectTransform _logContent;
        private ScrollRect _logScroll;
        private GameObject _beginButton;
        private GameObject _rerollButton;
        private GameObject _commitButton;
        private GameObject _tacticalPanel;
        private readonly Text[] _dice = new Text[3];
        private readonly Image[] _diceImage = new Image[3];
        private readonly Dictionary<string, UnitWidget> _units = new Dictionary<string, UnitWidget>();
        private readonly StringBuilder _logBuilder = new StringBuilder();
        private Transform _board;
        private int _free = 1;
        private int _crack;
        private bool _corrosion;
        private string _comboId = ComboIds.None;
        private int _logChars;
        private readonly string[] _facesShown = new string[3];
        private readonly bool[] _locksShown = new bool[3];
        private readonly Dictionary<string, CellView> _cells = new Dictionary<string, CellView>();
        private readonly List<TokenView> _tokens = new List<TokenView>();
        private readonly Dictionary<GameObject, string> _allyDrops = new Dictionary<GameObject, string>();
        private readonly Text[] _rowLabels = new Text[6];
        private readonly string[] _rowKeys =
        {
            LocKeys.FormationBack, LocKeys.FormationMid, LocKeys.FormationFront,
            LocKeys.FormationFront, LocKeys.FormationMid, LocKeys.FormationBack
        };
        private RectTransform _bench;
        private RectTransform _dragLayer;
        private Text _benchLabel;
        private Text _allyCaption;
        private Text _enemyCaption;
        private string _selectedUnitId;
        private string _dragUnitId;
        private bool _fromDrag;

        // Reference-pixel rects. Interactive formation stays below the dice (y 214-286)
        // and the reroll / march buttons (y 150-190), and left of the log (x >= 350).
        private const float CellW = 132f;
        private const float CellH = 48f;
        private static readonly Vector2 BoardPos = new Vector2(0f, -95f);
        private static readonly Color AllyCell = new Color(0.18f, 0.24f, 0.21f, 1f);
        private static readonly Color AllyCellEmpty = new Color(0.12f, 0.14f, 0.13f, 1f);
        private static readonly Color EnemyCell = new Color(0.28f, 0.16f, 0.16f, 1f);
        private static readonly Color TokenColor = new Color(0.28f, 0.38f, 0.34f, 1f);
        private static readonly Color TokenSelected = new Color(0.62f, 0.52f, 0.22f, 1f);
        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);
        private const float PortraitSize = 56f;

        private void Awake()
        {
            _font = LoadFont();
            EnsureEventSystem();
            LoadContent();
            BuildUi();
            Paint();
        }

        private void Update()
        {
            if (_sim == null)
                return;
            if (_sim.Phase == BattlePhase.Countdown)
            {
                _countdown -= Time.deltaTime;
                if (_countdown <= 0f)
                    _sim.BeginAutoCombat();
            }
            else if (_sim.Phase == BattlePhase.AutoCombat || _sim.Phase == BattlePhase.TacticalWindow)
            {
                _sim.PumpSeconds(Time.deltaTime);
            }

            ApplyNew();
            Paint();
        }

        private void LateUpdate()
        {
            if (_dragUnitId == null)
                _fromDrag = false;
        }

        private static bool FaceCatalogMatches(FaceCatalogAsset catalog)
        {
            if (catalog == null || catalog.faces == null || catalog.faces.Length != FaceIds.Order.Length)
                return false;
            for (int i = 0; i < FaceIds.Order.Length; i++)
            {
                if (catalog.faces[i] == null || catalog.faces[i].id != FaceIds.Order[i])
                    return false;
            }
            return true;
        }

        private void LoadContent()
        {
            if (encounter == null)
                encounter = Resources.Load<EncounterDefinitionAsset>("M0/M0TestEncounter");
            if (localization == null)
                localization = Resources.Load<LocalizationTableAsset>("M0/M0Localization");
            if (faces == null)
                faces = Resources.Load<FaceCatalogAsset>("M0/M0FaceCatalog");
            if (sprites == null)
                sprites = Resources.Load<UnitSpriteCatalog>("M0/M0UnitSprites");
            try
            {
                _def = encounter != null ? ContentMapper.ToEncounter(encounter) : null;
            }
            catch
            {
                _def = null;
            }
            if (_def == null || !FaceCatalogMatches(faces))
            {
                if (_def == null)
                    _def = M0Catalog.CreateEncounter();
                _usedFallback = true;
            }
        }

        private void BeginBattle()
        {
            int seed = fixedSeed;
            int typed;
            if (_seedInput != null && int.TryParse(_seedInput.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out typed))
                seed = typed;
            if (seed == 0)
                seed = System.Environment.TickCount;
            _seedInput.text = seed.ToString(CultureInfo.InvariantCulture);
            _selectedUnitId = null;
            _dragUnitId = null;
            _fromDrag = false;
            _sim = new BattleSimulator();
            _sim.Start(_def, seed);
            _applied = 0;
            _free = 1;
            _crack = 0;
            _corrosion = false;
            _comboId = ComboIds.None;
            _logChars = 0;
            for (int i = 0; i < 3; i++)
            {
                _facesShown[i] = null;
                _locksShown[i] = false;
            }
            _logBuilder.Length = 0;
            if (_usedFallback)
                _logBuilder.AppendLine(L(LocKeys.Fallback));
            ClearUnits();
            ApplyNew();
            Paint();
        }

        private void ApplyNew()
        {
            if (_sim == null)
                return;
            while (_applied < _sim.Events.Count)
                Apply(_sim.Events[_applied++]);
        }

        private void Apply(BattleEvent evt)
        {
            if (evt.Type == BattleEvents.DiceRoll)
            {
                _facesShown[0] = evt.Get("die0");
                _facesShown[1] = evt.Get("die1");
                _facesShown[2] = evt.Get("die2");
            }
            else if (evt.Type == BattleEvents.DiceLock)
            {
                int index = Parse(evt.Get("index"));
                if (index >= 0 && index < 3)
                    _locksShown[index] = evt.Get("locked") == "true";
            }
            else if (evt.Type == BattleEvents.DiceReroll)
            {
                if (evt.Get("freeUsed") == "true")
                    _free = 0;
            }
            else if (evt.Type == BattleEvents.CrackGained)
            {
                int.TryParse(evt.Get("stacks"), NumberStyles.Integer, CultureInfo.InvariantCulture, out _crack);
                _corrosion = evt.Get("corrosion") == "true";
            }
            else if (evt.Type == BattleEvents.ComboResolved)
            {
                _comboId = evt.Get("combo");
            }
            else if (evt.Type == BattleEvents.UnitSpawned)
            {
                UnitWidget widget = Widget(evt.Get("slot"), evt.Get("unitId"));
                widget.maxHp = Parse(evt.Get("maxHp"));
                widget.hp = Parse(evt.Get("hp"));
                widget.shield = 0;
            }
            else if (evt.Type == BattleEvents.ShieldApplied)
            {
                UnitWidget widget = FindWidget(evt.Get("slot"));
                if (widget != null)
                    widget.shield = Parse(evt.Get("amount"));
            }
            else if (evt.Type == BattleEvents.ShieldExpired)
            {
                UnitWidget widget = FindWidget(evt.Get("slot"));
                if (widget != null)
                    widget.shield = 0;
            }
            else if (evt.Type == BattleEvents.Damage || evt.Type == BattleEvents.HealOrHpLoss)
            {
                string slot = evt.Type == BattleEvents.Damage ? evt.Get("targetSlot") : evt.Get("slot");
                UnitWidget widget = FindWidget(slot);
                if (widget != null)
                {
                    widget.hp = Parse(evt.Get(evt.Type == BattleEvents.Damage ? "targetHp" : "hp"));
                    if (evt.Type == BattleEvents.Damage)
                        widget.shield = Mathf.Max(0, widget.shield - Parse(evt.Get("shieldAbsorbed")));
                }
            }

            _logBuilder.AppendLine(Format(evt));
        }

        private string Format(BattleEvent evt)
        {
            if (evt.Type == BattleEvents.BattleStart)
                return F("log.battle_start", evt.Get("seed"));
            if (evt.Type == BattleEvents.DiceRoll)
                return F("log.dice_roll", FaceName(evt.Get("die0")), FaceName(evt.Get("die1")), FaceName(evt.Get("die2")));
            if (evt.Type == BattleEvents.DiceLock)
            {
                string state = evt.Get("locked") == "true" ? L(LocKeys.Locked) : L(LocKeys.Unlocked);
                string shown = (Parse(evt.Get("index")) + 1).ToString(CultureInfo.InvariantCulture);
                return F("log.dice_lock", shown, state);
            }
            if (evt.Type == BattleEvents.DiceReroll)
                return L("log.dice_reroll");
            if (evt.Type == BattleEvents.CrackGained)
                return F("log.crack_gained", evt.Get("stacks"));
            if (evt.Type == BattleEvents.DiceCommit)
                return F("log.dice_commit", FaceName(evt.Get("die0")), FaceName(evt.Get("die1")), FaceName(evt.Get("die2")));
            if (evt.Type == BattleEvents.ComboResolved)
                return F("log.combo_resolved", ComboName(evt.Get("combo")));
            if (evt.Type == BattleEvents.UnitSpawned)
                return F("log.unit_spawned", UnitName(evt.Get("unitId")), evt.Get("slot"));
            if (evt.Type == BattleEvents.Damage)
            {
                string crit = evt.Get("crit") == "true" ? " " + L(LocKeys.CritMark) : "";
                return F(
                    "log.damage",
                    UnitName(evt.Get("sourceUnit")),
                    evt.Get("sourceSlot"),
                    UnitName(evt.Get("targetUnit")),
                    evt.Get("targetSlot"),
                    evt.Get("amount"),
                    crit);
            }
            if (evt.Type == BattleEvents.ShieldApplied)
                return F("log.shield_applied", evt.Get("slot"), evt.Get("amount"));
            if (evt.Type == BattleEvents.ShieldExpired)
                return F("log.shield_expired", evt.Get("slot"));
            if (evt.Type == BattleEvents.HealOrHpLoss)
                return F("log.hp_loss", evt.Get("slot"), evt.Get("delta"));
            if (evt.Type == BattleEvents.SkillCast)
                return F("log.skill_cast", SkillName(evt.Get("skillId")), evt.Get("slot"));
            if (evt.Type == BattleEvents.TacticalWindowOpened)
                return L("log.tactical_opened");
            if (evt.Type == BattleEvents.TacticalWindowConfirmed)
                return L("log.tactical_confirmed");
            if (evt.Type == BattleEvents.TacticalWindowSkipped)
                return L("log.tactical_skipped");
            if (evt.Type == BattleEvents.UnitDeath)
                return F("log.unit_death", UnitName(evt.Get("unitId")), evt.Get("slot"));
            if (evt.Type == BattleEvents.BattleEnd)
                return F("log.battle_end", ResultLabel(evt), evt.Get("duration"), evt.Get("gold"));
            return evt.Type;
        }

        private void Paint()
        {
            _title.text = L(LocKeys.Title);
            _seedLabel.text = L(LocKeys.Seed);
            _beginLabel.text = L(LocKeys.Begin);
            _rerollLabel.text = L(LocKeys.Reroll);
            _commitLabel.text = L(LocKeys.Commit);
            _confirmLabel.text = L(LocKeys.Confirm);
            _skipLabel.text = L(LocKeys.Skip);
            _localeLabel.text = L(LocKeys.Locale);
            _tacticalTitle.text = L(LocKeys.TacticalTitle);
            _tacticalBody.text = L(LocKeys.TacticalBody);

            bool setup = _sim != null && _sim.Phase == BattlePhase.Setup;
            bool countdown = _sim != null && _sim.Phase == BattlePhase.Countdown;
            bool window = _sim != null && _sim.Phase == BattlePhase.TacticalWindow;
            _beginButton.SetActive(_sim == null);
            _rerollButton.SetActive(setup);
            _commitButton.SetActive(setup);
            _tacticalPanel.SetActive(window);
            _seedInput.interactable = _sim == null;

            for (int i = 0; i < 3; i++)
            {
                if (_sim == null)
                {
                    _dice[i].text = "";
                    _diceImage[i].color = new Color(0.22f, 0.24f, 0.28f, 1f);
                    continue;
                }
                string face = FaceName(_facesShown[i]);
                string mark = _locksShown[i] ? L(LocKeys.Locked) : L(LocKeys.Unlocked);
                _dice[i].text = face + "\n" + mark;
                _diceImage[i].color = _locksShown[i]
                    ? new Color(0.45f, 0.36f, 0.16f, 1f)
                    : new Color(0.24f, 0.28f, 0.34f, 1f);
            }

            string phase = PhaseLabel();
            if (countdown)
                phase = phase + " " + Mathf.CeilToInt(Mathf.Max(0f, _countdown)).ToString(CultureInfo.InvariantCulture);
            string corrosion = _corrosion ? L(LocKeys.CorrosionOn) : L(LocKeys.CorrosionOff);
            _status.text = phase
                + "   "
                + L(LocKeys.Crack) + " " + _crack.ToString(CultureInfo.InvariantCulture)
                + "   "
                + corrosion
                + "   "
                + L(LocKeys.FreeReroll) + " " + _free.ToString(CultureInfo.InvariantCulture)
                + "   "
                + L(LocKeys.Combo) + " " + ComboName(_comboId);
            if (setup && !_sim.CanCommit)
                _status.text = _status.text + "   " + L(LocKeys.FormationIllegal);
            if (_commitButton != null)
                _commitButton.GetComponent<Button>().interactable = setup && _sim.CanCommit;

            if (_sim != null && _sim.Phase == BattlePhase.Resolve)
            {
                BattleEvent end = null;
                for (int i = _sim.Events.Count - 1; i >= 0; i--)
                {
                    if (_sim.Events[i].Type == BattleEvents.BattleEnd)
                    {
                        end = _sim.Events[i];
                        break;
                    }
                }
                _result.text = end == null ? "" : ResultLabel(end) + "  " + end.Get("duration") + "  " + L(LocKeys.Gold) + " " + end.Get("gold");
            }
            else
            {
                _result.text = "";
            }

            foreach (KeyValuePair<string, UnitWidget> pair in _units)
                pair.Value.Paint(UnitName(pair.Value.unitId), L(LocKeys.Hp), L(LocKeys.Shield));

            _log.text = _logBuilder.ToString();
            float height = Mathf.Max(200f, _log.preferredHeight + 12f);
            _logContent.sizeDelta = new Vector2(0f, height);
            if (_logScroll != null && _logBuilder.Length != _logChars)
            {
                _logChars = _logBuilder.Length;
                Canvas.ForceUpdateCanvases();
                _logScroll.verticalNormalizedPosition = 0f;
            }
            LayoutFormation();
        }

        private string PhaseLabel()
        {
            if (_sim == null)
                return "";
            if (_sim.Phase == BattlePhase.Setup)
                return L(LocKeys.PhaseSetup);
            if (_sim.Phase == BattlePhase.Countdown)
                return L(LocKeys.PhaseCountdown);
            if (_sim.Phase == BattlePhase.AutoCombat)
                return L(LocKeys.PhaseAuto);
            if (_sim.Phase == BattlePhase.TacticalWindow)
                return L(LocKeys.PhaseTactical);
            if (_sim.Phase == BattlePhase.Resolve)
                return L(LocKeys.PhaseResolve);
            return "";
        }

        private string ResultLabel(BattleEvent end)
        {
            if (end.Get("reason") == "timeout")
                return L(LocKeys.Timeout);
            if (end.Get("outcome") == "victory")
                return L(LocKeys.Victory);
            return L(LocKeys.Defeat);
        }

        private void BuildUi()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            Transform root = canvasGo.transform;

            _title = MakeText(Box(root, "Title", new Vector2(0f, 400f), new Vector2(700f, 40f), new Color(0f, 0f, 0f, 0f)), 28);
            _localeLabel = MakeText(ButtonBox(root, "Locale", new Vector2(680f, 400f), new Vector2(140f, 36f), ToggleLocale), 18);

            _seedLabel = MakeText(Box(root, "SeedLabel", new Vector2(-220f, 340f), new Vector2(120f, 36f), new Color(0f, 0f, 0f, 0f)), 18);
            _seedInput = MakeInput(root, new Vector2(-40f, 340f));
            if (fixedSeed != 0)
                _seedInput.text = fixedSeed.ToString(CultureInfo.InvariantCulture);
            _beginLabel = MakeText(ButtonBox(root, "Begin", new Vector2(180f, 340f), new Vector2(160f, 40f), BeginBattle), 20);
            _beginButton = _beginLabel.transform.parent.gameObject;

            BuildFormation(root);

            for (int i = 0; i < 3; i++)
            {
                int index = i;
                RectTransform die = ButtonBox(root, "Die" + i.ToString(CultureInfo.InvariantCulture), new Vector2(-180f + i * 180f, 250f), new Vector2(160f, 72f), delegate { ToggleDie(index); });
                _diceImage[i] = die.GetComponent<Image>();
                _dice[i] = MakeText(die, 18);
            }

            _rerollLabel = MakeText(ButtonBox(root, "Reroll", new Vector2(-90f, 170f), new Vector2(160f, 40f), OnReroll), 20);
            _rerollButton = _rerollLabel.transform.parent.gameObject;
            _commitLabel = MakeText(ButtonBox(root, "Commit", new Vector2(90f, 170f), new Vector2(160f, 40f), OnCommit), 20);
            _commitButton = _commitLabel.transform.parent.gameObject;
            _rerollButton.SetActive(false);
            _commitButton.SetActive(false);
            Button commit = _commitButton.GetComponent<Button>();
            ColorBlock colors = commit.colors;
            colors.disabledColor = new Color(0.25f, 0.25f, 0.28f, 0.75f);
            commit.colors = colors;

            _status = MakeText(Box(root, "Status", new Vector2(0f, 115f), new Vector2(1100f, 32f), new Color(0f, 0f, 0f, 0f)), 16);
            _result = MakeText(Box(root, "Result", new Vector2(0f, -360f), new Vector2(700f, 36f), new Color(0f, 0f, 0f, 0f)), 26);

            BuildLog(root);
            BuildTactical(root);
            BuildDragLayer(root);
            EnsureTokens();
        }

        private void BuildLog(Transform root)
        {
            RectTransform panel = Box(root, "LogPanel", new Vector2(560f, -40f), new Vector2(420f, 640f), new Color(0.08f, 0.09f, 0.11f, 0.92f));
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(panel, false);
            RectTransform scrollRt = Stretch(scrollGo.GetComponent<RectTransform>(), 8f);
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGo.transform, false);
            RectTransform viewRt = Stretch(viewport.GetComponent<RectTransform>(), 0f);
            viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.02f);
            viewport.GetComponent<Mask>().showMaskGraphic = false;
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            _logContent = content.GetComponent<RectTransform>();
            _logContent.anchorMin = new Vector2(0f, 1f);
            _logContent.anchorMax = new Vector2(1f, 1f);
            _logContent.pivot = new Vector2(0.5f, 1f);
            _logContent.sizeDelta = new Vector2(0f, 200f);
            _log = MakeText(_logContent, 15);
            _log.alignment = TextAnchor.UpperLeft;
            _log.horizontalOverflow = HorizontalWrapMode.Wrap;
            _log.verticalOverflow = VerticalWrapMode.Overflow;
            RectTransform logRt = _log.rectTransform;
            logRt.anchorMin = new Vector2(0f, 1f);
            logRt.anchorMax = new Vector2(1f, 1f);
            logRt.pivot = new Vector2(0.5f, 1f);
            logRt.sizeDelta = new Vector2(-8f, 0f);
            _logScroll = scrollGo.GetComponent<ScrollRect>();
            _logScroll.viewport = viewRt;
            _logScroll.content = _logContent;
            _logScroll.horizontal = false;
            _logScroll.vertical = true;
            _logScroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void BuildTactical(Transform root)
        {
            RectTransform panel = Box(root, "Tactical", new Vector2(0f, 0f), new Vector2(420f, 180f), new Color(0.12f, 0.10f, 0.08f, 0.96f));
            _tacticalPanel = panel.gameObject;
            _tacticalTitle = MakeText(Box(panel, "TacticalTitle", new Vector2(0f, 50f), new Vector2(380f, 36f), new Color(0f, 0f, 0f, 0f)), 22);
            _tacticalBody = MakeText(Box(panel, "TacticalBody", new Vector2(0f, 10f), new Vector2(380f, 36f), new Color(0f, 0f, 0f, 0f)), 16);
            _confirmLabel = MakeText(ButtonBox(panel, "Confirm", new Vector2(-90f, -50f), new Vector2(140f, 36f), OnConfirm), 18);
            _skipLabel = MakeText(ButtonBox(panel, "Skip", new Vector2(90f, -50f), new Vector2(140f, 36f), OnSkip), 18);
            _tacticalPanel.SetActive(false);
        }

        private void OnReroll()
        {
            if (_sim == null || _sim.Phase != BattlePhase.Setup)
                return;
            _sim.Reroll();
            ApplyNew();
            Paint();
        }

        private void OnCommit()
        {
            if (_sim == null || !_sim.CanCommit)
                return;
            _selectedUnitId = null;
            _dragUnitId = null;
            _sim.Commit();
            _countdown = _def.CountdownMs / 1000f;
            ApplyNew();
            Paint();
        }

        private void OnConfirm()
        {
            if (_sim == null || _sim.Phase != BattlePhase.TacticalWindow)
                return;
            _sim.ConfirmTacticalWindow();
            ApplyNew();
            Paint();
        }

        private void OnSkip()
        {
            if (_sim == null || _sim.Phase != BattlePhase.TacticalWindow)
                return;
            _sim.SkipTacticalWindow();
            ApplyNew();
            Paint();
        }

        private void ToggleDie(int index)
        {
            if (_sim == null || _sim.Phase != BattlePhase.Setup)
                return;
            _sim.SetLock(index, !_sim.IsLocked(index));
            ApplyNew();
            Paint();
        }

        private void ToggleLocale()
        {
            _locale = _locale == "en" ? "zh-Hans" : "en";
            if (_sim != null)
            {
                _logBuilder.Length = 0;
                if (_usedFallback)
                    _logBuilder.AppendLine(L(LocKeys.Fallback));
                for (int i = 0; i < _applied; i++)
                    _logBuilder.AppendLine(Format(_sim.Events[i]));
            }
            Paint();
        }

        private UnitWidget Widget(string slot, string unitId)
        {
            UnitWidget found = FindWidget(slot);
            if (found != null)
                return found;
            Color color = slot.StartsWith("ally_")
                ? new Color(0.28f, 0.38f, 0.34f, 1f)
                : new Color(0.42f, 0.26f, 0.26f, 1f);
            RectTransform panel = Box(_board, slot, SlotLocal(slot), new Vector2(CellW, CellH), color);
            var widget = new UnitWidget();
            widget.root = panel.gameObject;
            widget.unitId = unitId;
            widget.portrait = AddPortrait(panel, SpriteFor(unitId), new Vector2(-36f, 12f), 52f);
            float textX = widget.portrait != null ? 24f : 0f;
            float textW = widget.portrait != null ? 80f : 124f;
            widget.nameText = MakeText(Box(panel, "Name", new Vector2(textX, 12f), new Vector2(textW, 14f), Clear), 13);
            widget.hpText = MakeText(Box(panel, "Hp", new Vector2(textX, -4f), new Vector2(textW, 12f), Clear), 11);
            float barX = widget.portrait != null ? 24f : 0f;
            float barW = widget.portrait != null ? 78f : 116f;
            RectTransform bar = Box(panel, "Bar", new Vector2(barX, -18f), new Vector2(barW, 6f), new Color(0.08f, 0.08f, 0.08f, 1f));
            widget.fill = Box(bar, "Fill", Vector2.zero, new Vector2(116f, 6f), new Color(0.45f, 0.75f, 0.42f, 1f)).GetComponent<Image>();
            RectTransform fillRt = widget.fill.rectTransform;
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            _units[slot] = widget;
            return widget;
        }

        private UnitWidget FindWidget(string slot)
        {
            UnitWidget widget;
            if (slot != null && _units.TryGetValue(slot, out widget))
                return widget;
            return null;
        }

        private void ClearUnits()
        {
            foreach (KeyValuePair<string, UnitWidget> pair in _units)
            {
                if (pair.Value.root != null)
                    Destroy(pair.Value.root);
            }
            _units.Clear();
        }

        private static Vector2 SlotLocal(string slot)
        {
            return SlotAbsolute(slot) - BoardPos;
        }

        // Allies read back-to-front toward the center. Enemies read front-to-back away from it.
        private static Vector2 SlotAbsolute(string slot)
        {
            bool ally;
            FormationRow row;
            int column;
            if (!Slots.TryParse(slot, out ally, out row, out column))
                return Vector2.zero;
            int rowIndex = row == FormationRow.Back ? 0 : row == FormationRow.Mid ? 1 : 2;
            int colIndex = column - 1;
            float x = (colIndex - 1) * 148f + (ally ? rowIndex * 10f : -rowIndex * 10f);
            float y = ally ? 48f - rowIndex * 54f : -128f - (2 - rowIndex) * 54f;
            return new Vector2(x, y);
        }

        private void BuildFormation(Transform root)
        {
            _board = Box(root, "Board", BoardPos, new Vector2(520f, 350f), new Color(0.10f, 0.11f, 0.14f, 0.55f), false);
            float allyFront = SlotAbsolute("ally_c2").y - CellH * 0.5f;
            float enemyFront = SlotAbsolute("enemy_c2").y + CellH * 0.5f;
            float midY = (allyFront + enemyFront) * 0.5f;
            Box(_board, "Midline", new Vector2(0f, midY - BoardPos.y), new Vector2(440f, 2f), new Color(0.55f, 0.55f, 0.6f, 0.8f), false);

            for (int i = 0; i < Slots.AllyIds.Length; i++)
                BuildCell(Slots.AllyIds[i], true);
            for (int i = 0; i < Slots.EnemyIds.Length; i++)
                BuildCell(Slots.EnemyIds[i], false);

            string[] samples = { "ally_a2", "ally_b2", "ally_c2", "enemy_c2", "enemy_b2", "enemy_a2" };
            for (int i = 0; i < samples.Length; i++)
            {
                Vector2 at = SlotAbsolute(samples[i]);
                _rowLabels[i] = PlaceLabel(root, "Row" + i.ToString(CultureInfo.InvariantCulture), new Vector2(296f, at.y), new Vector2(68f, 22f), 14);
            }

            _allyCaption = PlaceLabel(root, "AllyCaption", new Vector2(-330f, SlotAbsolute("ally_a2").y), new Vector2(72f, 22f), 14);
            _enemyCaption = PlaceLabel(root, "EnemyCaption", new Vector2(-330f, SlotAbsolute("enemy_a2").y), new Vector2(72f, 22f), 14);

            _bench = Box(root, "Bench", new Vector2(-470f, -6f), new Vector2(124f, 184f), new Color(0.14f, 0.15f, 0.18f, 0.95f), true);
            _benchLabel = PlaceLabel(_bench, "BenchLabel", new Vector2(0f, 78f), new Vector2(112f, 18f), 13);
            Listen(_bench.gameObject, EventTriggerType.PointerClick, delegate { NotifyBenchClick(); });
            _bench.gameObject.SetActive(false);
        }

        private void BuildCell(string slotId, bool ally)
        {
            RectTransform rect = Box(_board, slotId, SlotLocal(slotId), new Vector2(CellW, CellH), ally ? AllyCellEmpty : EnemyCell, ally);
            RectTransform slotBar = Box(rect, "SlotId", new Vector2(0f, -18f), new Vector2(120f, 12f), new Color(0.04f, 0.05f, 0.06f, 0.82f), false);
            Text slotText = MakeText(slotBar, 11);
            slotText.text = slotId;
            slotText.color = new Color(0.90f, 0.86f, 0.72f, 1f);
            var cell = new CellView();
            cell.slotId = slotId;
            cell.ally = ally;
            cell.rect = rect;
            cell.image = rect.GetComponent<Image>();
            cell.slotBar = slotBar;
            if (!ally)
            {
                cell.unitId = EnemyOn(slotId);
                cell.portrait = AddPortrait(rect, SpriteFor(cell.unitId), new Vector2(-26f, -2f), PortraitSize);
                Vector2 namePos = cell.portrait != null ? new Vector2(28f, -6f) : new Vector2(0f, 4f);
                Vector2 nameSize = cell.portrait != null ? new Vector2(68f, 28f) : new Vector2(124f, 22f);
                cell.occupant = PlaceLabel(rect, "Occupant", namePos, nameSize, 13);
            }
            _cells[slotId] = cell;
            if (!ally)
                return;
            _allyDrops[rect.gameObject] = slotId;
            string captured = slotId;
            Listen(rect.gameObject, EventTriggerType.PointerClick, delegate { NotifyCellClick(captured); });
        }

        private string EnemyOn(string slotId)
        {
            if (_def == null || _def.Enemies == null)
                return null;
            for (int i = 0; i < _def.Enemies.Count; i++)
            {
                Placement placement = _def.Enemies[i];
                if (placement != null && placement.SlotId == slotId && placement.Unit != null)
                    return placement.Unit.Id;
            }
            return null;
        }

        private void BuildDragLayer(Transform root)
        {
            var go = new GameObject("DragLayer", typeof(RectTransform));
            go.transform.SetParent(root, false);
            _dragLayer = Stretch(go.GetComponent<RectTransform>(), 0f);
        }

        private void EnsureTokens()
        {
            if (_def == null || _def.Allies == null)
                return;
            for (int i = 0; i < _def.Allies.Count; i++)
            {
                Placement placement = _def.Allies[i];
                if (placement == null || placement.Unit == null || string.IsNullOrEmpty(placement.Unit.Id))
                    continue;
                if (FindToken(placement.Unit.Id) != null)
                    continue;
                string unitId = placement.Unit.Id;
                Sprite sprite = SpriteFor(unitId);
                RectTransform rect = Box(_board, "Token" + unitId, Vector2.zero, new Vector2(112f, sprite != null ? 62f : 28f), TokenColor, true);
                var view = new TokenView();
                view.unitId = unitId;
                view.rect = rect;
                view.image = rect.GetComponent<Image>();
                view.portrait = AddPortrait(rect, sprite, new Vector2(-26f, 6f), PortraitSize);
                if (view.portrait != null)
                {
                    view.image.color = Clear;
                    RectTransform namePlate = Box(rect, "NamePlate", new Vector2(28f, -4f), new Vector2(50f, 28f), new Color(0.05f, 0.06f, 0.08f, 0.78f), false);
                    view.label = MakeText(namePlate, 12);
                    Outline outline = view.label.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                    outline.effectDistance = new Vector2(1f, -1f);
                }
                else
                {
                    view.label = MakeText(rect, 14);
                }
                _tokens.Add(view);
                Listen(rect.gameObject, EventTriggerType.PointerClick, delegate (BaseEventData data) { NotifyTokenClick(unitId); });
                Listen(rect.gameObject, EventTriggerType.BeginDrag, delegate (BaseEventData data) { BeginTokenDrag(unitId, (PointerEventData)data); });
                Listen(rect.gameObject, EventTriggerType.Drag, delegate (BaseEventData data) { DragToken(unitId, (PointerEventData)data); });
                Listen(rect.gameObject, EventTriggerType.EndDrag, delegate (BaseEventData data) { EndTokenDrag(unitId, (PointerEventData)data); });
            }
        }

        private void LayoutFormation()
        {
            if (_board == null)
                return;
            bool setup = FormationEditable();
            bool showRoster = _sim == null || setup;
            if (_bench != null)
                _bench.gameObject.SetActive(setup);
            if (_benchLabel != null)
                _benchLabel.text = L(LocKeys.FormationBench);
            if (_allyCaption != null)
                _allyCaption.text = L(LocKeys.FormationAlly);
            if (_enemyCaption != null)
                _enemyCaption.text = L(LocKeys.FormationEnemy);
            for (int i = 0; i < _rowLabels.Length; i++)
            {
                if (_rowLabels[i] != null)
                    _rowLabels[i].text = L(_rowKeys[i]);
            }

            var occupied = new HashSet<string>();
            int stacked = 0;
            for (int i = 0; i < _tokens.Count; i++)
            {
                TokenView token = _tokens[i];
                string slot = DisplaySlot(token.unitId);
                if (token.unitId == _dragUnitId)
                {
                    if (!string.IsNullOrEmpty(slot))
                        occupied.Add(slot);
                    continue;
                }

                RectTransform parent;
                Vector2 pos;
                bool hasPortrait = token.portrait != null;
                if (!string.IsNullOrEmpty(slot) && _cells.ContainsKey(slot))
                {
                    parent = _cells[slot].rect;
                    pos = hasPortrait ? new Vector2(0f, 1f) : new Vector2(0f, 8f);
                    occupied.Add(slot);
                }
                else
                {
                    parent = _bench;
                    pos = hasPortrait ? new Vector2(0f, 18f - stacked * 66f) : new Vector2(0f, 44f - stacked * 30f);
                    stacked++;
                }
                if (parent != null && token.rect.parent != parent)
                    token.rect.SetParent(parent, false);
                token.rect.anchoredPosition = pos;
                token.rect.sizeDelta = hasPortrait ? new Vector2(112f, 62f) : new Vector2(124f, 28f);
                bool selected = token.unitId == _selectedUnitId;
                if (hasPortrait)
                {
                    token.image.color = selected
                        ? new Color(0.62f, 0.48f, 0.16f, 0.55f)
                        : new Color(0.05f, 0.06f, 0.07f, 0.35f);
                    token.portrait.color = selected ? new Color(1f, 0.84f, 0.48f, 1f) : Color.white;
                }
                else
                {
                    token.image.color = selected ? TokenSelected : TokenColor;
                }
                token.image.raycastTarget = setup;
                token.label.text = UnitName(token.unitId);
                token.rect.gameObject.SetActive(showRoster);
            }

            foreach (KeyValuePair<string, CellView> pair in _cells)
            {
                CellView cell = pair.Value;
                if (!cell.ally)
                {
                    cell.image.raycastTarget = false;
                    if (cell.occupant != null)
                        cell.occupant.text = string.IsNullOrEmpty(cell.unitId) ? "" : UnitName(cell.unitId);
                }
                else
                {
                    cell.image.raycastTarget = setup;
                    bool selectedHere = !string.IsNullOrEmpty(_selectedUnitId) && DisplaySlot(_selectedUnitId) == cell.slotId;
                    if (occupied.Contains(cell.slotId))
                        cell.image.color = selectedHere ? new Color(0.55f, 0.42f, 0.16f, 1f) : AllyCell;
                    else
                        cell.image.color = AllyCellEmpty;
                }
                if (cell.portrait != null)
                    cell.portrait.gameObject.SetActive(FindWidget(cell.slotId) == null);
                if (cell.occupant != null)
                    cell.occupant.transform.parent.SetAsLastSibling();
                if (cell.slotBar != null)
                    cell.slotBar.SetAsLastSibling();
            }
        }

        private Sprite SpriteFor(string unitId)
        {
            if (sprites == null)
                return null;
            return sprites.SpriteFor(unitId);
        }

        private Image AddPortrait(RectTransform parent, Sprite sprite, Vector2 pos, float size)
        {
            if (sprite == null || parent == null)
                return null;
            RectTransform rt = Box(parent, "Portrait", pos, new Vector2(size, size), Color.white, false);
            Image image = rt.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private string DisplaySlot(string unitId)
        {
            if (_sim != null)
                return _sim.AllySlotOf(unitId);
            if (_def == null || _def.Allies == null)
                return null;
            for (int i = 0; i < _def.Allies.Count; i++)
            {
                Placement placement = _def.Allies[i];
                if (placement != null && placement.Unit != null && placement.Unit.Id == unitId)
                    return placement.SlotId;
            }
            return null;
        }

        public bool FormationEditable()
        {
            return _sim != null && _sim.Phase == BattlePhase.Setup;
        }

        private void NotifyTokenClick(string unitId)
        {
            if (_fromDrag || !FormationEditable())
                return;
            if (_selectedUnitId == unitId)
            {
                _selectedUnitId = null;
                LayoutFormation();
                return;
            }
            if (string.IsNullOrEmpty(_selectedUnitId))
            {
                _selectedUnitId = unitId;
                LayoutFormation();
                return;
            }
            Exchange(_selectedUnitId, unitId);
            _selectedUnitId = null;
            Paint();
        }

        private void NotifyCellClick(string slotId)
        {
            if (_fromDrag || !FormationEditable() || string.IsNullOrEmpty(_selectedUnitId))
                return;
            _sim.TrySetAllySlot(_selectedUnitId, slotId);
            _selectedUnitId = null;
            Paint();
        }

        private void NotifyBenchClick()
        {
            if (_fromDrag || !FormationEditable() || string.IsNullOrEmpty(_selectedUnitId))
                return;
            _sim.TrySetAllySlot(_selectedUnitId, null);
            _selectedUnitId = null;
            Paint();
        }

        private void BeginTokenDrag(string unitId, PointerEventData eventData)
        {
            if (!FormationEditable())
                return;
            TokenView token = FindToken(unitId);
            if (token == null || _dragLayer == null)
                return;
            _fromDrag = true;
            _dragUnitId = unitId;
            token.image.raycastTarget = false;
            token.rect.SetParent(_dragLayer, true);
        }

        private void DragToken(string unitId, PointerEventData eventData)
        {
            if (_dragUnitId != unitId)
                return;
            TokenView token = FindToken(unitId);
            if (token == null)
                return;
            token.rect.position = eventData.position;
        }

        private void EndTokenDrag(string unitId, PointerEventData eventData)
        {
            if (_dragUnitId != unitId)
                return;
            _fromDrag = true;
            _dragUnitId = null;
            ApplyDrop(unitId, eventData);
            _selectedUnitId = null;
            Paint();
        }

        private void ApplyDrop(string unitId, PointerEventData eventData)
        {
            if (_sim == null || _sim.Phase != BattlePhase.Setup || EventSystem.current == null)
                return;
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                GameObject go = hits[i].gameObject;
                string other;
                if (TryToken(go, out other))
                {
                    if (other != unitId)
                        Exchange(unitId, other);
                    return;
                }
                string slot;
                if (_allyDrops.TryGetValue(go, out slot))
                {
                    _sim.TrySetAllySlot(unitId, slot);
                    return;
                }
                if (go == _bench.gameObject)
                {
                    _sim.TrySetAllySlot(unitId, null);
                    return;
                }
                return;
            }
        }

        private void Exchange(string unitId, string otherId)
        {
            if (_sim == null)
                return;
            string dest = _sim.AllySlotOf(otherId);
            if (string.IsNullOrEmpty(dest))
            {
                string from = _sim.AllySlotOf(unitId);
                _sim.TrySetAllySlot(unitId, null);
                if (!string.IsNullOrEmpty(from))
                    _sim.TrySetAllySlot(otherId, from);
                return;
            }
            _sim.TrySetAllySlot(unitId, dest);
        }

        private bool TryToken(GameObject go, out string unitId)
        {
            unitId = null;
            if (MatchToken(go, out unitId))
                return true;
            if (go != null && go.transform.parent != null && MatchToken(go.transform.parent.gameObject, out unitId))
                return true;
            unitId = null;
            return false;
        }

        private bool MatchToken(GameObject go, out string unitId)
        {
            unitId = null;
            if (go == null)
                return false;
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].rect != null && _tokens[i].rect.gameObject == go)
                {
                    unitId = _tokens[i].unitId;
                    return true;
                }
            }
            return false;
        }

        private TokenView FindToken(string unitId)
        {
            for (int i = 0; i < _tokens.Count; i++)
            {
                if (_tokens[i].unitId == unitId)
                    return _tokens[i];
            }
            return null;
        }

        private static void Listen(GameObject go, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = go.AddComponent<EventTrigger>();
            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry();
            entry.eventID = type;
            entry.callback = new EventTrigger.TriggerEvent();
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        private Text PlaceLabel(Transform parent, string name, Vector2 pos, Vector2 size, int fontSize)
        {
            return MakeText(Box(parent, name, pos, size, Clear, false), fontSize);
        }

        private string L(string key)
        {
            if (localization == null)
                return key;
            return localization.Resolve(key, _locale);
        }

        private string F(string key, params string[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, L(key), args);
        }

        private string FaceName(string faceId)
        {
            if (string.IsNullOrEmpty(faceId) || !faceId.StartsWith("face_"))
                return faceId;
            return L("face." + faceId.Substring(5) + ".name");
        }

        private string UnitName(string unitId)
        {
            if (string.IsNullOrEmpty(unitId) || !unitId.StartsWith("unit_"))
                return unitId;
            return L("unit." + unitId.Substring(5) + ".name");
        }

        private string SkillName(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || !skillId.StartsWith("skill_"))
                return skillId;
            return L("skill." + skillId.Substring(6) + ".name");
        }

        private string ComboName(string comboId)
        {
            if (string.IsNullOrEmpty(comboId) || !comboId.StartsWith("combo_"))
                return comboId ?? "";
            return L("combo." + comboId.Substring(6) + ".name");
        }

        private static int Parse(string value)
        {
            int parsed;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                return parsed;
            return 0;
        }

        private Text MakeText(RectTransform parent, int size)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = Stretch(go.GetComponent<RectTransform>(), 4f);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.93f, 0.94f, 0.95f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private RectTransform ButtonBox(Transform parent, string name, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction click)
        {
            RectTransform rt = Box(parent, name, pos, size, new Color(0.30f, 0.36f, 0.32f, 1f), true);
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(click);
            return rt;
        }

        private RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color, bool raycast = false)
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
            image.color = color;
            image.raycastTarget = raycast;
            return rt;
        }

        private InputField MakeInput(Transform parent, Vector2 pos)
        {
            RectTransform rt = Box(parent, "Seed", pos, new Vector2(180f, 36f), new Color(0.16f, 0.17f, 0.2f, 1f), true);
            Text text = MakeText(rt, 18);
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = true;
            var input = rt.gameObject.AddComponent<InputField>();
            input.textComponent = text;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static RectTransform Stretch(RectTransform rt, float pad)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad);
            rt.offsetMax = new Vector2(-pad, -pad);
            return rt;
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        private static Font LoadFont()
        {
            Font font = null;
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                font = null;
            }
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }

        private sealed class UnitWidget
        {
            public GameObject root;
            public string unitId;
            public Text nameText;
            public Text hpText;
            public Image fill;
            public Image portrait;
            public int hp;
            public int maxHp;
            public int shield;

            public void Paint(string unitLabel, string hpLabel, string shieldLabel)
            {
                nameText.text = unitLabel;
                hpText.text = hpLabel + " " + hp.ToString(CultureInfo.InvariantCulture)
                    + "  " + shieldLabel + " " + shield.ToString(CultureInfo.InvariantCulture);
                float ratio = maxHp <= 0 ? 0f : Mathf.Clamp01(hp / (float)maxHp);
                RectTransform rt = fill.rectTransform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(ratio, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        private sealed class CellView
        {
            public string slotId;
            public bool ally;
            public string unitId;
            public RectTransform rect;
            public Image image;
            public Text occupant;
            public RectTransform slotBar;
            public Image portrait;
        }

        private sealed class TokenView
        {
            public string unitId;
            public RectTransform rect;
            public Image image;
            public Text label;
            public Image portrait;
        }
    }
}
