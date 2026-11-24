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
                return F("log.damage", UnitName(evt.Get("sourceUnit")), UnitName(evt.Get("targetUnit")), evt.Get("amount"), crit);
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

            _status = MakeText(Box(root, "Status", new Vector2(0f, 115f), new Vector2(1100f, 32f), new Color(0f, 0f, 0f, 0f)), 16);
            _result = MakeText(Box(root, "Result", new Vector2(0f, -360f), new Vector2(700f, 36f), new Color(0f, 0f, 0f, 0f)), 26);

            Box(root, "Midline", new Vector2(-80f, -20f), new Vector2(520f, 2f), new Color(0.55f, 0.55f, 0.6f, 0.8f));
            _board = Box(root, "Board", new Vector2(-80f, -20f), new Vector2(640f, 460f), new Color(0.10f, 0.11f, 0.14f, 0.35f));

            BuildLog(root);
            BuildTactical(root);
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
            if (_sim == null || _sim.Phase != BattlePhase.Setup)
                return;
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
            RectTransform panel = Box(_board, slot, SlotPosition(slot), new Vector2(150f, 78f), color);
            var widget = new UnitWidget();
            widget.root = panel.gameObject;
            widget.nameText = MakeText(Box(panel, "Name", new Vector2(0f, 24f), new Vector2(140f, 22f), new Color(0f, 0f, 0f, 0f)), 14);
            widget.unitId = unitId;
            widget.hpText = MakeText(Box(panel, "Hp", new Vector2(0f, 4f), new Vector2(140f, 18f), new Color(0f, 0f, 0f, 0f)), 12);
            RectTransform bar = Box(panel, "Bar", new Vector2(0f, -22f), new Vector2(130f, 10f), new Color(0.08f, 0.08f, 0.08f, 1f));
            widget.fill = Box(bar, "Fill", Vector2.zero, new Vector2(130f, 10f), new Color(0.45f, 0.75f, 0.42f, 1f)).GetComponent<Image>();
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

        private static Vector2 SlotPosition(string slot)
        {
            int split = slot.IndexOf('_');
            char row = slot[split + 1];
            char col = slot[split + 2];
            int colIndex = col - '1';
            int rowIndex = row == 'a' ? 0 : row == 'b' ? 1 : 2;
            bool ally = slot.StartsWith("ally_");
            float x = (colIndex - 1) * 170f + (ally ? rowIndex * 16f : -rowIndex * 16f);
            float y = ally ? 150f - rowIndex * 78f : -70f - rowIndex * 78f;
            return new Vector2(x, y);
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
            RectTransform rt = Box(parent, name, pos, size, new Color(0.30f, 0.36f, 0.32f, 1f));
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = rt.GetComponent<Image>();
            button.onClick.AddListener(click);
            return rt;
        }

        private RectTransform Box(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return rt;
        }

        private InputField MakeInput(Transform parent, Vector2 pos)
        {
            RectTransform rt = Box(parent, "Seed", pos, new Vector2(180f, 36f), new Color(0.16f, 0.17f, 0.2f, 1f));
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
    }
}
