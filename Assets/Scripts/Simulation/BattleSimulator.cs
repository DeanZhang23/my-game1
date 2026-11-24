using System;
using System.Collections.Generic;

namespace Echoes.Simulation
{
    public sealed class BattleSimulator
    {
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<Combatant> _units = new List<Combatant>();
        private EncounterDef _encounter;
        private DeterministicRng _rng;
        private string[] _faces = new string[3];
        private bool[] _locked = new bool[3];
        private int _seed;
        private int _crack;
        private int _free = 1;
        private int _nowMs;
        private int _goldOnWin;
        private int _windowRemainingMs;
        private bool _corrosion;
        private bool _attunement;
        private bool _windowUsed;
        private FaceResolution _resolution = new FaceResolution();

        public BattlePhase Phase { get; private set; }
        public int Crack { get { return _crack; } }
        public bool CorrosionActive { get { return _corrosion; } }
        public int FreeRerollsRemaining { get { return _free; } }
        public int NowMs { get { return _nowMs; } }
        public int Seed { get { return _seed; } }
        public IList<BattleEvent> Events { get { return _events; } }
        public string ComboId { get { return _resolution.ComboId; } }

        public string Face(int index)
        {
            return _faces[index];
        }

        public bool IsLocked(int index)
        {
            return _locked[index];
        }

        public void Start(EncounterDef encounter, int seed)
        {
            Start(encounter, seed, null);
        }

        public void Start(EncounterDef encounter, int seed, string[] presetFaces)
        {
            if (encounter == null)
                throw new ArgumentNullException("encounter");
            if (encounter.Allies == null || encounter.Allies.Count < 1)
                throw new InvalidOperationException("Encounter needs at least one ally.");
            if (encounter.Allies.Count > 5)
                throw new InvalidOperationException("Ally cap is 5.");

            _encounter = encounter;
            _seed = seed;
            _rng = new DeterministicRng(seed);
            _events.Clear();
            _units.Clear();
            Phase = BattlePhase.Setup;
            _crack = 0;
            _corrosion = false;
            _free = 1;
            _locked = new bool[3];
            _faces = new string[3];
            _windowUsed = false;
            _nowMs = 0;
            _goldOnWin = 0;
            _attunement = false;
            _resolution = new FaceResolution();

            var start = Fields();
            Add(start, "seed", EventFormat.Int(seed));
            Add(start, "encounter", encounter.Id);
            Log(BattleEvents.BattleStart, start);

            if (presetFaces != null)
            {
                if (presetFaces.Length != 3)
                    throw new ArgumentException("Preset faces must be 3.");
                for (int i = 0; i < 3; i++)
                {
                    if (FaceIds.IndexOf(presetFaces[i]) < 0)
                        throw new ArgumentException("Unknown face: " + presetFaces[i]);
                    _faces[i] = presetFaces[i];
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    _faces[i] = RollFace();
            }

            LogDice("initial");
        }

        public void SetLock(int index, bool locked)
        {
            RequireSetup();
            if (index < 0 || index > 2)
                throw new ArgumentOutOfRangeException("index");
            if (_locked[index] == locked)
                return;
            _locked[index] = locked;
            var fields = Fields();
            Add(fields, "index", EventFormat.Int(index));
            Add(fields, "locked", EventFormat.Bool(locked));
            Log(BattleEvents.DiceLock, fields);
        }

        public void Reroll()
        {
            RequireSetup();
            bool any = false;
            for (int i = 0; i < 3; i++)
            {
                if (!_locked[i])
                    any = true;
            }

            var reroll = Fields();
            Add(reroll, "ineffective", EventFormat.Bool(!any));
            if (!any)
            {
                Add(reroll, "freeUsed", EventFormat.Bool(false));
                Add(reroll, "crack", EventFormat.Int(_crack));
                Log(BattleEvents.DiceReroll, reroll);
                return;
            }

            bool freeUsed = _free > 0;
            if (freeUsed)
                _free--;
            Add(reroll, "freeUsed", EventFormat.Bool(freeUsed));
            Add(reroll, "crack", EventFormat.Int(_crack));
            Log(BattleEvents.DiceReroll, reroll);

            if (!freeUsed && _crack < 3)
            {
                _crack++;
                if (_crack >= 3)
                    _corrosion = true;
                var crack = Fields();
                Add(crack, "stacks", EventFormat.Int(_crack));
                Add(crack, "corrosion", EventFormat.Bool(_corrosion));
                Log(BattleEvents.CrackGained, crack);
            }

            for (int i = 0; i < 3; i++)
            {
                if (!_locked[i])
                    _faces[i] = RollFace();
            }

            LogDice("reroll");
        }

        public void Commit()
        {
            RequireSetup();
            _resolution = FaceEffects.Resolve(_faces[0], _faces[1], _faces[2]);
            _attunement = _resolution.ComboId == ComboIds.Attunement;
            _goldOnWin = _resolution.Gold;

            var commit = Fields();
            Add(commit, "die0", _faces[0]);
            Add(commit, "die1", _faces[1]);
            Add(commit, "die2", _faces[2]);
            Add(commit, "crack", EventFormat.Int(_crack));
            Add(commit, "corrosion", EventFormat.Bool(_corrosion));
            Log(BattleEvents.DiceCommit, commit);

            var combo = Fields();
            Add(combo, "combo", _resolution.ComboId);
            Add(combo, "face", _resolution.ComboFace);
            Add(combo, "multiplier", EventFormat.Dec(_resolution.Coefficient));
            Add(combo, "wildFace", _resolution.WildFace);
            Add(combo, "wildUsed", EventFormat.Int(_resolution.WildUsed));
            Add(combo, "wildIdle", EventFormat.Int(_resolution.WildTokens - _resolution.WildUsed));
            Log(BattleEvents.ComboResolved, combo);

            SpawnUnits();
            ApplySacrifice();
            Phase = BattlePhase.Countdown;
        }

        public void BeginAutoCombat()
        {
            if (Phase != BattlePhase.Countdown)
                throw new InvalidOperationException("Countdown is required before combat.");

            _nowMs = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                unit.NextAttackMs = unit.IntervalMs;
                unit.NextSkillReadyMs = 0;
                if (!unit.IsAlly || unit.Row != FormationRow.Front || _resolution.ShieldLifeRatio <= 0m)
                    continue;
                int amount = CombatFormulas.ShieldAmount(unit.MaxHp, _resolution.ShieldLifeRatio);
                if (amount <= 0)
                    continue;
                unit.Shield = amount;
                unit.ShieldTracked = true;
                unit.ShieldExpired = false;
                unit.ShieldExpireMs = CombatFormulas.ShieldDurationMs;
                var shield = Fields();
                Add(shield, "time", EventFormat.Ms(_nowMs));
                Add(shield, "slot", unit.SlotId);
                Add(shield, "amount", EventFormat.Int(amount));
                Add(shield, "expire", EventFormat.Ms(unit.ShieldExpireMs));
                Log(BattleEvents.ShieldApplied, shield);
            }

            Phase = BattlePhase.AutoCombat;
            if (CheckEnd())
                return;
            if (_nowMs >= _encounter.TimeoutMs)
                EndBattle("defeat", "timeout", 0);
        }

        public void ConfirmTacticalWindow()
        {
            if (Phase != BattlePhase.TacticalWindow)
                throw new InvalidOperationException("Not in a tactical window.");
            Log(BattleEvents.TacticalWindowConfirmed, Fields());
            Phase = BattlePhase.AutoCombat;
        }

        public void SkipTacticalWindow()
        {
            if (Phase != BattlePhase.TacticalWindow)
                throw new InvalidOperationException("Not in a tactical window.");
            Log(BattleEvents.TacticalWindowSkipped, Fields());
            Phase = BattlePhase.AutoCombat;
        }

        public void TickTacticalWindow(int deltaMs)
        {
            if (Phase != BattlePhase.TacticalWindow)
                return;
            if (deltaMs < 0)
                deltaMs = 0;
            _windowRemainingMs -= deltaMs;
            if (_windowRemainingMs <= 0)
                SkipTacticalWindow();
        }

        private decimal _pumpRemainder;

        public void PumpSeconds(float deltaSeconds)
        {
            if (deltaSeconds <= 0f || Phase == BattlePhase.Resolve || Phase == BattlePhase.Setup || Phase == BattlePhase.Idle)
                return;
            _pumpRemainder += (decimal)deltaSeconds * 1000m;
            int whole = (int)_pumpRemainder;
            if (whole <= 0)
                return;
            _pumpRemainder -= whole;
            PumpMs(whole);
        }

        public void PumpMs(int budgetMs)
        {
            if (budgetMs < 0)
                budgetMs = 0;
            if (Phase == BattlePhase.TacticalWindow)
            {
                TickTacticalWindow(budgetMs);
                return;
            }
            if (Phase != BattlePhase.AutoCombat || budgetMs == 0)
                return;

            int spent = 0;
            int guard = 0;
            while (Phase == BattlePhase.AutoCombat && spent < budgetMs)
            {
                if (++guard > 100000)
                    throw new InvalidOperationException("Combat pump did not advance.");

                int next = PeekNextMs();
                int limit = _nowMs + (budgetMs - spent);
                if (next > limit)
                {
                    AdvanceEnergy(limit - _nowMs);
                    _nowMs = limit;
                    return;
                }

                int delta = next - _nowMs;
                if (delta > 0)
                {
                    AdvanceEnergy(delta);
                    _nowMs = next;
                    spent += delta;
                }

                bool acted = ResolveDue();
                if (Phase != BattlePhase.AutoCombat)
                    return;
                if (!acted && delta == 0)
                    throw new InvalidOperationException("Combat clock stalled at " + _nowMs + ".");
            }
        }

        public void RunToEnd(TacticalDecision decision)
        {
            int guard = 0;
            while (Phase != BattlePhase.Resolve)
            {
                if (++guard > 100000)
                    throw new InvalidOperationException("Battle did not finish.");
                if (Phase == BattlePhase.TacticalWindow)
                {
                    if (decision == TacticalDecision.Confirm)
                        ConfirmTacticalWindow();
                    else
                        SkipTacticalWindow();
                    continue;
                }
                if (Phase != BattlePhase.AutoCombat)
                    throw new InvalidOperationException("Unexpected phase " + Phase + ".");
                PumpMs(1000);
            }
        }

        internal void SetCrackStacksForTest(int stacks)
        {
            RequireSetup();
            if (stacks < 0)
                stacks = 0;
            if (stacks > 3)
                stacks = 3;
            _crack = stacks;
            _corrosion = stacks >= 3;
        }

        private void SpawnUnits()
        {
            _units.Clear();
            AddSide(_encounter.Allies, true);
            AddSide(_encounter.Enemies, false);
            _units.Sort(delegate (Combatant a, Combatant b)
            {
                return string.CompareOrdinal(a.SlotId, b.SlotId);
            });
        }

        private void AddSide(List<Placement> placements, bool ally)
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < placements.Count; i++)
            {
                Placement placement = placements[i];
                if (placement == null || placement.Unit == null || string.IsNullOrEmpty(placement.SlotId))
                    throw new InvalidOperationException("Placement is incomplete.");
                if (Slots.IsAlly(placement.SlotId) != ally)
                    throw new InvalidOperationException("Slot team does not match the side: " + placement.SlotId);
                if (!seen.Add(placement.SlotId))
                    throw new InvalidOperationException("Duplicate slot: " + placement.SlotId);

                UnitDef def = placement.Unit;
                FormationRow row = Slots.RowOf(placement.SlotId);
                var unit = new Combatant();
                unit.SlotId = placement.SlotId;
                unit.UnitId = def.Id;
                unit.NameKey = def.NameKey;
                unit.IsAlly = ally;
                unit.Row = row;
                unit.MaxHp = def.MaxHp;
                unit.Hp = def.MaxHp;
                unit.SpellPower = def.SpellPower;
                unit.MagicResist = def.MagicResist;
                unit.CritRate = def.CritRate;
                unit.CritDamage = def.CritDamage;
                unit.EnergyPerSecond = def.EnergyPerSecond;
                unit.MaxEnergy = def.MaxEnergy;
                unit.BasicId = def.Basic.Id;
                unit.BasicSchool = def.Basic.School;
                unit.BasicRatio = def.Basic.Ratio;
                unit.Alive = true;
                unit.SkillDamageMultiplier = 1m;

                if (ally)
                {
                    unit.Attack = def.Attack * _resolution.AttackMultiplier;
                    unit.Armor = def.Armor + _resolution.ArmorBonus;
                    unit.IntervalMs = CombatFormulas.IntervalMs(def.BaseAttackInterval, _resolution.AttackSpeedBonus);
                    unit.SkillDamageMultiplier = _resolution.SkillDamageMultiplier;
                    unit.Energy = row == FormationRow.Back ? _resolution.BackEnergy : 0;
                    unit.Keen = row == FormationRow.Front ? _resolution.KeenPerFront : 0;
                }
                else
                {
                    unit.Attack = def.Attack;
                    unit.Armor = def.Armor;
                    unit.IntervalMs = CombatFormulas.IntervalMs(def.BaseAttackInterval, 0m);
                    unit.Energy = 0;
                    unit.Keen = 0;
                }

                if (unit.Energy > unit.MaxEnergy)
                    unit.Energy = unit.MaxEnergy;

                if (def.Skill != null)
                {
                    decimal cooldown = def.Skill.CooldownSeconds;
                    if (ally)
                        cooldown -= _resolution.CooldownReductionSeconds;
                    if (cooldown < 0m)
                        cooldown = 0m;
                    unit.HasSkill = true;
                    unit.SkillId = def.Skill.Id;
                    unit.SkillSchool = def.Skill.School;
                    unit.SkillRatio = def.Skill.Ratio;
                    unit.SkillCost = def.Skill.EnergyCost;
                    unit.CooldownMs = CombatFormulas.SecondsToMs(cooldown);
                }

                _units.Add(unit);
                var spawned = Fields();
                Add(spawned, "slot", unit.SlotId);
                Add(spawned, "unitId", unit.UnitId);
                Add(spawned, "row", Slots.RowName(unit.Row));
                Add(spawned, "hp", EventFormat.Int(unit.Hp));
                Add(spawned, "maxHp", EventFormat.Int(unit.MaxHp));
                Add(spawned, "energy", EventFormat.Int((int)unit.Energy));
                Add(spawned, "attack", EventFormat.Dec(unit.Attack));
                Add(spawned, "armor", EventFormat.Dec(unit.Armor));
                Add(spawned, "keen", EventFormat.Int(unit.Keen));
                Add(spawned, "intervalMs", EventFormat.Int(unit.IntervalMs));
                Log(BattleEvents.UnitSpawned, spawned);
            }
        }

        private void ApplySacrifice()
        {
            if (_resolution.SacrificeFraction <= 0m)
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.IsAlly || !unit.Alive)
                    continue;
                int loss = CombatFormulas.SacrificeLoss(unit.Hp, _resolution.SacrificeFraction);
                unit.Hp -= loss;
                if (unit.Hp < 1)
                    unit.Hp = 1;
                var fields = Fields();
                Add(fields, "slot", unit.SlotId);
                Add(fields, "unitId", unit.UnitId);
                Add(fields, "delta", EventFormat.Int(-loss));
                Add(fields, "hp", EventFormat.Int(unit.Hp));
                Add(fields, "reason", "sacrifice");
                Log(BattleEvents.HealOrHpLoss, fields);
            }
        }

        private int PeekNextMs()
        {
            int next = _encounter.TimeoutMs;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive)
                    continue;
                if (unit.NextAttackMs < next)
                    next = unit.NextAttackMs;
                int skillAt = unit.NextSkillMs(_nowMs);
                if (skillAt < next)
                    next = skillAt;
                if (unit.ShieldTracked && !unit.ShieldExpired && unit.ShieldExpireMs < next)
                    next = unit.ShieldExpireMs;
            }

            if (_attunement && !_windowUsed && _encounter.TacticalWindowDelayMs < next)
                next = _encounter.TacticalWindowDelayMs;
            return next;
        }

        // Same timestamp: shields expire, then skills, then basic attacks.
        // Actors at that timestamp run in slot-id order. A unit that dies earlier in the order does not act.
        private bool ResolveDue()
        {
            bool acted = false;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.ShieldTracked || unit.ShieldExpired)
                    continue;
                if (unit.ShieldExpireMs > _nowMs)
                    continue;
                int remaining = unit.Shield;
                unit.Shield = 0;
                unit.ShieldExpired = true;
                var fields = Fields();
                Add(fields, "time", EventFormat.Ms(_nowMs));
                Add(fields, "slot", unit.SlotId);
                Add(fields, "remaining", EventFormat.Int(remaining));
                Log(BattleEvents.ShieldExpired, fields);
                acted = true;
            }

            if (CheckEnd())
                return true;

            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive || unit.NextSkillMs(_nowMs) > _nowMs)
                    continue;
                CastSkill(unit);
                acted = true;
                if (CheckEnd())
                    return true;
            }

            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive || unit.NextAttackMs > _nowMs)
                    continue;
                BasicAttack(unit);
                acted = true;
                if (CheckEnd())
                    return true;
            }

            if (_nowMs >= _encounter.TimeoutMs)
            {
                EndBattle("defeat", "timeout", 0);
                return true;
            }

            if (_attunement && !_windowUsed && _nowMs >= _encounter.TacticalWindowDelayMs)
            {
                OpenWindow();
                return true;
            }

            return acted;
        }

        private void CastSkill(Combatant unit)
        {
            if (unit.Energy + 0.001m < unit.SkillCost)
            {
                unit.NextSkillReadyMs = _nowMs + 1;
                return;
            }

            Combatant target = SelectTarget(unit);
            unit.Energy -= unit.SkillCost;
            if (unit.Energy < 0m)
                unit.Energy = 0m;
            unit.NextSkillReadyMs = _nowMs + unit.CooldownMs;
            if (target == null)
                return;

            var fields = Fields();
            Add(fields, "time", EventFormat.Ms(_nowMs));
            Add(fields, "slot", unit.SlotId);
            Add(fields, "unitId", unit.UnitId);
            Add(fields, "skillId", unit.SkillId);
            Add(fields, "targetSlot", target.SlotId);
            Log(BattleEvents.SkillCast, fields);
            DealDamage(unit, target, true);
        }

        private void BasicAttack(Combatant unit)
        {
            unit.NextAttackMs = _nowMs + unit.IntervalMs;
            Combatant target = SelectTarget(unit);
            if (target == null)
                return;
            DealDamage(unit, target, false);
        }

        private void DealDamage(Combatant source, Combatant target, bool skill)
        {
            DamageSchool school = skill ? source.SkillSchool : source.BasicSchool;
            decimal stat = school == DamageSchool.Physical ? source.Attack : source.SpellPower;
            decimal ratio = skill ? source.SkillRatio : source.BasicRatio;
            decimal defense = school == DamageSchool.Physical ? target.Armor : target.MagicResist;
            decimal mod = 1m;
            if (skill)
                mod *= source.SkillDamageMultiplier;
            bool keen = source.Keen > 0;
            if (keen)
            {
                mod *= CombatFormulas.KeenPerStack;
                source.Keen -= 1;
            }
            if (_corrosion)
                mod *= CombatFormulas.Corrosion;

            bool crit = _rng.NextUnit() < source.CritRate;
            int pre = CombatFormulas.PreCrit(stat, ratio, defense, mod);
            int amount = CombatFormulas.ApplyCrit(pre, crit, source.CritDamage);
            int shield = target.Shield;
            int hp = target.Hp;
            int absorbed;
            int hpLost;
            CombatFormulas.ApplyShieldThenHp(ref shield, ref hp, amount, out absorbed, out hpLost);
            target.Shield = shield;
            target.Hp = hp;
            bool killed = target.Alive && target.Hp <= 0;
            if (target.Hp <= 0)
            {
                target.Hp = 0;
                target.Alive = false;
            }

            var fields = Fields();
            Add(fields, "time", EventFormat.Ms(_nowMs));
            Add(fields, "sourceSlot", source.SlotId);
            Add(fields, "sourceUnit", source.UnitId);
            Add(fields, "targetSlot", target.SlotId);
            Add(fields, "targetUnit", target.UnitId);
            Add(fields, "kind", skill ? "skill" : "basic");
            Add(fields, "skillId", skill ? source.SkillId : source.BasicId);
            Add(fields, "amount", EventFormat.Int(amount));
            Add(fields, "crit", EventFormat.Bool(crit));
            Add(fields, "shieldAbsorbed", EventFormat.Int(absorbed));
            Add(fields, "hpLost", EventFormat.Int(hpLost));
            Add(fields, "targetHp", EventFormat.Int(target.Hp));
            Add(fields, "keen", EventFormat.Bool(keen));
            Log(BattleEvents.Damage, fields);

            if (killed)
            {
                var death = Fields();
                Add(death, "time", EventFormat.Ms(_nowMs));
                Add(death, "slot", target.SlotId);
                Add(death, "unitId", target.UnitId);
                Log(BattleEvents.UnitDeath, death);
            }
        }

        private Combatant SelectTarget(Combatant attacker)
        {
            Combatant best = null;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant candidate = _units[i];
                if (!candidate.Alive || candidate.IsAlly == attacker.IsAlly)
                    continue;
                if (best == null
                    || candidate.Hp < best.Hp
                    || (candidate.Hp == best.Hp && string.CompareOrdinal(candidate.SlotId, best.SlotId) < 0))
                {
                    best = candidate;
                }
            }
            return best;
        }

        private void AdvanceEnergy(int deltaMs)
        {
            if (deltaMs <= 0)
                return;
            decimal dt = deltaMs / 1000m;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive)
                    continue;
                unit.Energy += unit.EnergyPerSecond * dt;
                if (unit.Energy > unit.MaxEnergy)
                    unit.Energy = unit.MaxEnergy;
                if (unit.Energy < 0m)
                    unit.Energy = 0m;
            }
        }

        private bool CheckEnd()
        {
            if (Phase == BattlePhase.Resolve)
                return true;
            bool ally = false;
            bool enemy = false;
            for (int i = 0; i < _units.Count; i++)
            {
                if (!_units[i].Alive)
                    continue;
                if (_units[i].IsAlly)
                    ally = true;
                else
                    enemy = true;
            }
            if (!ally)
            {
                EndBattle("defeat", "ally_wiped", 0);
                return true;
            }
            if (!enemy)
            {
                EndBattle("victory", "enemy_wiped", _goldOnWin);
                return true;
            }
            return false;
        }

        private void OpenWindow()
        {
            _windowUsed = true;
            Phase = BattlePhase.TacticalWindow;
            _windowRemainingMs = _encounter.TacticalWindowDurationMs;
            var fields = Fields();
            Add(fields, "time", EventFormat.Ms(_nowMs));
            Log(BattleEvents.TacticalWindowOpened, fields);
        }

        private void EndBattle(string outcome, string reason, int gold)
        {
            Phase = BattlePhase.Resolve;
            var fields = Fields();
            Add(fields, "outcome", outcome);
            Add(fields, "reason", reason);
            Add(fields, "duration", EventFormat.Ms(_nowMs));
            Add(fields, "gold", EventFormat.Int(gold));
            Log(BattleEvents.BattleEnd, fields);
        }

        private string RollFace()
        {
            return FaceIds.Order[_rng.NextInt(FaceIds.Order.Length)];
        }

        private void LogDice(string kind)
        {
            var fields = Fields();
            Add(fields, "kind", kind);
            Add(fields, "die0", _faces[0]);
            Add(fields, "die1", _faces[1]);
            Add(fields, "die2", _faces[2]);
            Log(BattleEvents.DiceRoll, fields);
        }

        private void RequireSetup()
        {
            if (Phase != BattlePhase.Setup)
                throw new InvalidOperationException("Dice can change only during Setup.");
        }

        private void Log(string type, List<KeyValuePair<string, string>> fields)
        {
            _events.Add(new BattleEvent(_events.Count, type, fields));
        }

        private static List<KeyValuePair<string, string>> Fields()
        {
            return new List<KeyValuePair<string, string>>();
        }

        private static void Add(List<KeyValuePair<string, string>> fields, string key, string value)
        {
            fields.Add(new KeyValuePair<string, string>(key, value));
        }

        private sealed class Combatant
        {
            public string SlotId;
            public string UnitId;
            public string NameKey;
            public bool IsAlly;
            public FormationRow Row;
            public int MaxHp;
            public int Hp;
            public int Shield;
            public int ShieldExpireMs;
            public bool ShieldTracked;
            public bool ShieldExpired;
            public decimal Attack;
            public decimal SpellPower;
            public decimal Armor;
            public decimal MagicResist;
            public decimal CritRate;
            public decimal CritDamage;
            public int IntervalMs;
            public int NextAttackMs;
            public decimal Energy;
            public decimal EnergyPerSecond;
            public int MaxEnergy;
            public bool HasSkill;
            public string SkillId;
            public string BasicId;
            public DamageSchool BasicSchool;
            public decimal BasicRatio;
            public DamageSchool SkillSchool;
            public decimal SkillRatio;
            public decimal SkillCost;
            public int CooldownMs;
            public int NextSkillReadyMs;
            public decimal SkillDamageMultiplier = 1m;
            public int Keen;
            public bool Alive;

            public int NextSkillMs(int now)
            {
                if (!HasSkill || !Alive)
                    return int.MaxValue;
                int energyReady;
                if (Energy >= SkillCost)
                {
                    energyReady = now;
                }
                else if (EnergyPerSecond <= 0m)
                {
                    return int.MaxValue;
                }
                else
                {
                    decimal seconds = (SkillCost - Energy) / EnergyPerSecond;
                    int add = (int)decimal.Ceiling(seconds * 1000m);
                    if (add < 0)
                        add = 0;
                    energyReady = now + add;
                }
                return energyReady > NextSkillReadyMs ? energyReady : NextSkillReadyMs;
            }
        }
    }
}
