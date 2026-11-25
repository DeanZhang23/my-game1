using System;
using System.Collections.Generic;

namespace Echoes.Simulation
{
    public sealed class BattleSimulator
    {
        private readonly List<BattleEvent> _events = new List<BattleEvent>();
        private readonly List<Combatant> _units = new List<Combatant>();
        private readonly List<RosterUnit> _roster = new List<RosterUnit>();
        private EncounterDef _encounter;
        private DeterministicRng _rng;
        private int _seed;
        private int _nowMs;
        private int _nextRetargetMs;
        private int _bankMs;

        public BattlePhase Phase { get; private set; }
        public int NowMs { get { return _nowMs; } }
        public int Seed { get { return _seed; } }
        public IList<BattleEvent> Events { get { return _events; } }
        public int AllyRosterCount { get { return _roster.Count; } }
        public int CombatantCount { get { return _units.Count; } }

        public int PlacedAllyCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _roster.Count; i++)
                {
                    if (!string.IsNullOrEmpty(_roster[i].SlotId))
                        count++;
                }
                return count;
            }
        }

        public bool CanCommit
        {
            get { return Phase == BattlePhase.Setup && PlacedAllyCount > 0; }
        }

        public void Start(EncounterDef encounter, int seed)
        {
            if (encounter == null)
                throw new ArgumentNullException("encounter");
            List<RosterUnit> roster = CopyRoster(encounter);

            _encounter = encounter;
            _roster.Clear();
            _roster.AddRange(roster);
            _seed = seed;
            _rng = new DeterministicRng(seed);
            _events.Clear();
            _units.Clear();
            Phase = BattlePhase.Setup;
            _nowMs = 0;
            _nextRetargetMs = 0;
            _bankMs = 0;

            var start = Fields();
            Add(start, "seed", EventFormat.Int(seed));
            Add(start, "encounter", encounter.Id);
            Log(BattleEvents.BattleStart, start);
        }

        public string AllyUnitIdAt(int index)
        {
            return _roster[index].Def.Id;
        }

        public string AllySlotAt(int index)
        {
            return _roster[index].SlotId;
        }

        // First roster entry with this unit id. Null when that entry is benched or the id is absent.
        // M0 allies have distinct ids. Duplicate ids stay as separate roster rows for spawn.
        public string AllySlotOf(string unitId)
        {
            int index = FindRosterIndex(unitId);
            if (index < 0)
                return null;
            return _roster[index].SlotId;
        }

        // Setup only. An empty slot id benches the unit. An occupied ally slot swaps.
        // Does not mutate the encounter, and does not write a log line. Slot ids are logged at spawn.
        public bool TrySetAllySlot(string unitId, string slotId)
        {
            if (Phase != BattlePhase.Setup)
                return false;
            int index = FindRosterIndex(unitId);
            if (index < 0)
                return false;
            if (string.IsNullOrEmpty(slotId))
            {
                _roster[index].SlotId = null;
                return true;
            }
            if (!Slots.IsAllySlot(slotId))
                return false;

            for (int i = 0; i < _roster.Count; i++)
            {
                if (i == index || _roster[i].SlotId != slotId)
                    continue;
                _roster[i].SlotId = _roster[index].SlotId;
                break;
            }
            _roster[index].SlotId = slotId;
            return true;
        }

        public bool TryGetCombatant(int index, out CombatantView view)
        {
            view = new CombatantView();
            if (index < 0 || index >= _units.Count)
                return false;
            Combatant unit = _units[index];
            view.SlotId = unit.SlotId;
            view.UnitId = unit.UnitId;
            view.X = unit.X;
            view.Y = unit.Y;
            view.Alive = unit.Alive;
            view.Hp = unit.Hp;
            view.MaxHp = unit.MaxHp;
            view.Shield = unit.Shield;
            return true;
        }

        public void Commit()
        {
            RequireSetup();
            if (PlacedAllyCount < 1)
                throw new InvalidOperationException("Formation needs at least one ally.");
            SpawnUnits();
            Phase = BattlePhase.Countdown;
        }

        public void BeginAutoCombat()
        {
            if (Phase != BattlePhase.Countdown)
                throw new InvalidOperationException("Countdown is required before combat.");

            _nowMs = 0;
            _bankMs = 0;
            _nextRetargetMs = 0;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                unit.NextAttackMs = unit.IntervalMs;
                unit.NextSkillReadyMs = 0;
                unit.Target = null;
                unit.Engaged = false;
            }

            // Lock the opening target before any movement. The 0.25s clock starts after this pass.
            RetargetAll();
            Phase = BattlePhase.AutoCombat;
            if (CheckEnd())
                return;
            if (_nowMs >= _encounter.TimeoutMs)
                EndBattle("defeat", "timeout", 0);
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

        // Sim time advances in whole milliseconds. While anyone is out of range, movement
        // integrates on a fixed 50ms lattice so the path does not depend on how the caller
        // slices the budget. In-range stretches jump to the next attack, skill, retarget, or timeout.
        // A due swing stays armed while the target is out of range and fires on the lattice step
        // that enters range. It does not spend the interval during the walk.
        public void PumpMs(int budgetMs)
        {
            if (budgetMs < 0)
                budgetMs = 0;
            if (Phase != BattlePhase.AutoCombat)
                return;
            _bankMs += budgetMs;
            int guard = 0;
            while (Phase == BattlePhase.AutoCombat)
            {
                if (++guard > 200000)
                    throw new InvalidOperationException("Combat pump did not advance.");

                if (_nowMs >= _nextRetargetMs)
                    RetargetAll();
                if (Phase != BattlePhase.AutoCombat)
                    return;

                if (DueNow())
                {
                    bool acted = ResolveDue();
                    if (Phase != BattlePhase.AutoCombat)
                        return;
                    if (!acted)
                        throw new InvalidOperationException("Combat clock stalled at " + _nowMs + ".");
                    continue;
                }

                bool moving = AnyoneNeedsToMove();
                int horizon = NextHorizon(moving);
                if (horizon <= _nowMs)
                    throw new InvalidOperationException("Combat clock stalled at " + _nowMs + ".");
                int need = horizon - _nowMs;
                if (_bankMs < need)
                    return;

                bool moveStep = moving && need == LatticeGap(_nowMs) && _encounter.TimeoutMs >= horizon;
                _bankMs -= need;
                AdvanceEnergy(need);
                _nowMs = horizon;
                if (moveStep)
                {
                    MoveAll(need);
                    RefreshEngagement();
                }
            }
        }

        public void RunToEnd()
        {
            int guard = 0;
            while (Phase != BattlePhase.Resolve)
            {
                if (++guard > 100000)
                    throw new InvalidOperationException("Battle did not finish.");
                if (Phase != BattlePhase.AutoCombat)
                    throw new InvalidOperationException("Unexpected phase " + Phase + ".");
                PumpMs(1000);
            }
        }

        private int NextHorizon(bool moving)
        {
            if (moving)
            {
                int lattice = _nowMs + LatticeGap(_nowMs);
                if (_encounter.TimeoutMs < lattice)
                    return _encounter.TimeoutMs;
                return lattice;
            }

            int next = PeekNextActionMs();
            if (_nextRetargetMs < next)
                next = _nextRetargetMs;
            return next;
        }

        private static int LatticeGap(int now)
        {
            int mod = now % FieldLayout.MoveStepMs;
            if (mod == 0)
                return FieldLayout.MoveStepMs;
            return FieldLayout.MoveStepMs - mod;
        }

        private bool DueNow()
        {
            if (_nowMs >= _encounter.TimeoutMs)
                return true;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (unit.ShieldTracked && !unit.ShieldExpired && unit.ShieldExpireMs <= _nowMs)
                    return true;
                if (!unit.Alive || !IsInRange(unit))
                    continue;
                if (unit.NextAttackMs <= _nowMs)
                    return true;
                if (unit.NextSkillMs(_nowMs) <= _nowMs)
                    return true;
            }
            return false;
        }

        private void SpawnUnits()
        {
            _units.Clear();
            var allies = new List<Placement>();
            for (int i = 0; i < _roster.Count; i++)
            {
                RosterUnit unit = _roster[i];
                if (string.IsNullOrEmpty(unit.SlotId))
                    continue;
                allies.Add(new Placement { SlotId = unit.SlotId, Unit = unit.Def });
            }
            AddSide(allies, true);
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
                decimal x;
                decimal y;
                FieldLayout.PositionOf(placement.SlotId, out x, out y);
                var unit = new Combatant();
                unit.SlotId = placement.SlotId;
                unit.UnitId = def.Id;
                unit.NameKey = def.NameKey;
                unit.IsAlly = ally;
                unit.Row = row;
                unit.X = x;
                unit.Y = y;
                unit.Role = UnitRoles.Require(def.Role);
                unit.MoveSpeed = def.MoveSpeed;
                unit.AttackRange = def.AttackRange;
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
                unit.Shield = 0;
                unit.ShieldExpireMs = 0;
                unit.ShieldTracked = false;
                unit.ShieldExpired = true;
                unit.Attack = def.Attack;
                unit.Armor = def.Armor;
                unit.IntervalMs = CombatFormulas.IntervalMs(def.BaseAttackInterval, 0m);
                unit.Energy = 0m;
                unit.Keen = 0;

                if (def.Skill != null)
                {
                    decimal cooldown = def.Skill.CooldownSeconds;
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
                Add(spawned, "x", EventFormat.Dec(unit.X));
                Add(spawned, "y", EventFormat.Dec(unit.Y));
                Add(spawned, "role", unit.Role);
                Add(spawned, "moveSpeed", EventFormat.Dec(unit.MoveSpeed));
                Add(spawned, "attackRange", EventFormat.Dec(unit.AttackRange));
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

        private int PeekNextActionMs()
        {
            int next = _encounter.TimeoutMs;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (unit.ShieldTracked && !unit.ShieldExpired && unit.ShieldExpireMs < next)
                    next = unit.ShieldExpireMs;
                if (!unit.Alive || !IsInRange(unit))
                    continue;
                if (unit.NextAttackMs < next)
                    next = unit.NextAttackMs;
                int skillAt = unit.NextSkillMs(_nowMs);
                if (skillAt < next)
                    next = skillAt;
            }
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
                if (!unit.Alive || !IsInRange(unit) || unit.NextSkillMs(_nowMs) > _nowMs)
                    continue;
                CastSkill(unit);
                acted = true;
                if (CheckEnd())
                    return true;
            }

            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive || !IsInRange(unit) || unit.NextAttackMs > _nowMs)
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

            return acted;
        }

        private void CastSkill(Combatant unit)
        {
            if (!IsInRange(unit))
                return;
            if (unit.Energy + 0.001m < unit.SkillCost)
            {
                unit.NextSkillReadyMs = _nowMs + 1;
                return;
            }

            Combatant target = unit.Target;
            unit.Energy -= unit.SkillCost;
            if (unit.Energy < 0m)
                unit.Energy = 0m;
            unit.NextSkillReadyMs = _nowMs + unit.CooldownMs;
            if (target == null || !target.Alive)
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
            if (!IsInRange(unit))
                return;
            unit.NextAttackMs = _nowMs + unit.IntervalMs;
            Combatant target = unit.Target;
            if (target == null || !target.Alive)
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
                RetargetLockedOn(target);
            }
        }

        // Distance, then current HP, then slot id. Squared distance keeps the same order and the same ties.
        private Combatant SelectTarget(Combatant attacker)
        {
            Combatant best = null;
            decimal bestDist = 0m;
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant candidate = _units[i];
                if (!candidate.Alive || candidate.IsAlly == attacker.IsAlly)
                    continue;
                decimal dist = DistanceSquared(attacker, candidate);
                if (best == null
                    || dist < bestDist
                    || (dist == bestDist && candidate.Hp < best.Hp)
                    || (dist == bestDist && candidate.Hp == best.Hp && string.CompareOrdinal(candidate.SlotId, best.SlotId) < 0))
                {
                    best = candidate;
                    bestDist = dist;
                }
            }
            return best;
        }

        private void RetargetAll()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Alive)
                    LockTarget(_units[i]);
            }
            _nextRetargetMs = ((_nowMs / FieldLayout.RetargetIntervalMs) + 1) * FieldLayout.RetargetIntervalMs;
        }

        private void RetargetLockedOn(Combatant dead)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (unit.Alive && unit.Target == dead)
                    LockTarget(unit);
            }
        }

        private void LockTarget(Combatant unit)
        {
            if (!unit.Alive)
                return;
            Combatant next = SelectTarget(unit);
            if (unit.Target == next)
            {
                UpdateEngaged(unit);
                return;
            }
            unit.Target = next;
            unit.Engaged = false;
            if (next != null)
                LogAim(BattleEvents.UnitTarget, unit, next);
            UpdateEngaged(unit);
        }

        private void RefreshEngagement()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i].Alive)
                    UpdateEngaged(_units[i]);
            }
        }

        private void UpdateEngaged(Combatant unit)
        {
            bool inRange = IsInRange(unit);
            if (inRange && !unit.Engaged)
            {
                unit.Engaged = true;
                LogAim(BattleEvents.UnitEngage, unit, unit.Target);
            }
            else if (!inRange)
            {
                unit.Engaged = false;
            }
        }

        private bool AnyoneNeedsToMove()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive || unit.Target == null || !unit.Target.Alive)
                    continue;
                if (!IsInRange(unit))
                    return true;
            }
            return false;
        }

        private bool IsInRange(Combatant unit)
        {
            if (unit.Target == null || !unit.Target.Alive)
                return false;
            decimal range = unit.AttackRange;
            if (range < 0m)
                range = 0m;
            return DistanceSquared(unit, unit.Target) <= range * range + 0.0000001m;
        }

        private static decimal DistanceSquared(Combatant a, Combatant b)
        {
            decimal dx = a.X - b.X;
            decimal dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        // Simultaneous step. Each unit aims at its target's position at the start of the step
        // and will not step closer than attackRange to that snapshot.
        private void MoveAll(int deltaMs)
        {
            if (deltaMs <= 0)
                return;
            decimal dt = deltaMs / 1000m;
            int count = _units.Count;
            var sx = new decimal[count];
            var sy = new decimal[count];
            for (int i = 0; i < count; i++)
            {
                sx[i] = _units[i].X;
                sy[i] = _units[i].Y;
            }

            for (int i = 0; i < count; i++)
            {
                Combatant unit = _units[i];
                if (!unit.Alive || unit.Target == null || !unit.Target.Alive)
                    continue;
                int targetIndex = IndexOf(unit.Target);
                if (targetIndex < 0)
                    continue;
                decimal dx = sx[targetIndex] - sx[i];
                decimal dy = sy[targetIndex] - sy[i];
                decimal dist2 = dx * dx + dy * dy;
                decimal range = unit.AttackRange;
                if (range < 0m)
                    range = 0m;
                if (dist2 <= range * range)
                    continue;
                decimal dist = FieldLayout.Sqrt(dist2);
                if (dist <= 0m)
                    continue;
                decimal gap = dist - range;
                if (gap < 0m)
                    gap = 0m;
                decimal travel = unit.MoveSpeed * dt;
                if (travel > gap)
                    travel = gap;
                if (travel <= 0m)
                    continue;
                decimal nx = sx[i] + dx / dist * travel;
                decimal ny = sy[i] + dy / dist * travel;
                FieldLayout.Clamp(ref nx, ref ny);
                unit.X = FieldLayout.RoundPos(nx);
                unit.Y = FieldLayout.RoundPos(ny);
            }
        }

        private int IndexOf(Combatant unit)
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i] == unit)
                    return i;
            }
            return -1;
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
                EndBattle("victory", "enemy_wiped", 0);
                return true;
            }
            return false;
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

        private void RequireSetup()
        {
            if (Phase != BattlePhase.Setup)
                throw new InvalidOperationException("March is only available during Setup.");
        }

        private static List<RosterUnit> CopyRoster(EncounterDef encounter)
        {
            if (encounter.Allies == null || encounter.Allies.Count < 1)
                throw new InvalidOperationException("Encounter needs at least one ally.");
            if (encounter.Allies.Count > 5)
                throw new InvalidOperationException("Ally cap is 5.");

            var roster = new List<RosterUnit>();
            var seen = new HashSet<string>();
            for (int i = 0; i < encounter.Allies.Count; i++)
            {
                Placement placement = encounter.Allies[i];
                if (placement == null || placement.Unit == null || string.IsNullOrEmpty(placement.Unit.Id) || string.IsNullOrEmpty(placement.SlotId))
                    throw new InvalidOperationException("Placement is incomplete.");
                if (!Slots.IsAllySlot(placement.SlotId))
                    throw new InvalidOperationException("Slot team does not match the side: " + placement.SlotId);
                if (!seen.Add(placement.SlotId))
                    throw new InvalidOperationException("Duplicate slot: " + placement.SlotId);
                roster.Add(new RosterUnit { Def = placement.Unit, SlotId = placement.SlotId });
            }
            return roster;
        }

        private int FindRosterIndex(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return -1;
            for (int i = 0; i < _roster.Count; i++)
            {
                if (_roster[i].Def.Id == unitId)
                    return i;
            }
            return -1;
        }

        private void LogAim(string type, Combatant unit, Combatant target)
        {
            var fields = Fields();
            Add(fields, "time", EventFormat.Ms(_nowMs));
            Add(fields, "slot", unit.SlotId);
            Add(fields, "unitId", unit.UnitId);
            Add(fields, "targetSlot", target.SlotId);
            Add(fields, "targetUnit", target.UnitId);
            Log(type, fields);
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

        private sealed class RosterUnit
        {
            public UnitDef Def;
            public string SlotId;
        }

        private sealed class Combatant
        {
            public string SlotId;
            public string UnitId;
            public string NameKey;
            public bool IsAlly;
            public FormationRow Row;
            public decimal X;
            public decimal Y;
            public string Role;
            public decimal MoveSpeed;
            public decimal AttackRange;
            public Combatant Target;
            public bool Engaged;
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
