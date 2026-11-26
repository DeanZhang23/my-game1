using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class GoldenSeedTests
    {
        public const int Seed = 424242;

        [Fact]
        public void GoldenSeed_MidCombatDice_ReplaysExactly()
        {
            BattleSimulator sim = Play();
            Assert.Equal(1, Count(sim, BattleEvents.DiceRoll));
            Assert.Equal(1, Count(sim, BattleEvents.DiceCommit));
            Assert.Equal("player", Find(sim, BattleEvents.DiceCommit).Get("reason"));
            Assert.Equal(1, Count(sim, BattleEvents.ComboResolved));
            Assert.Equal(0, Count(sim, BattleEvents.DiceReroll));
            Assert.Equal(0, Count(sim, BattleEvents.DiceLock));
            Assert.Equal(0, Count(sim, BattleEvents.CrackGained));
            Assert.Equal(0, Count(sim, "tactical_window_opened"));
            string actual = Canonical(sim);
            string path = GoldenPath();
            if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, actual);
            }

            Assert.True(File.Exists(path), "Golden file missing at " + path);
            string expected = File.ReadAllText(path).Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SameSeedAndScript_ReplayByteForByte()
        {
            Assert.Equal(Canonical(Play()), Canonical(Play()));
        }

        private static BattleSimulator Play()
        {
            var sim = new BattleSimulator();
            sim.Start(M0Catalog.CreateEncounter(), Seed);
            sim.Commit();
            sim.BeginAutoCombat();
            // 50ms is the movement lattice. Roll on the first return where the pool can pay,
            // using the same Roll and Confirm the greybox buttons call. One cycle per battle.
            bool rolled = false;
            int guard = 0;
            while (sim.Phase != BattlePhase.Resolve)
            {
                if (++guard > 200000)
                    throw new InvalidOperationException("Battle did not finish.");
                if (!rolled && sim.CanRoll)
                {
                    sim.Roll();
                    sim.Confirm();
                    rolled = true;
                }
                if (sim.Phase != BattlePhase.AutoCombat)
                    break;
                sim.PumpMs(50);
            }
            return sim;
        }

        private static string GoldenPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "tests", "Echoes.Simulation.Tests", "golden", "m0-midcombat-dice.txt");
                if (File.Exists(candidate) || Directory.Exists(Path.Combine(dir.FullName, "tests")))
                {
                    if (Directory.Exists(Path.Combine(dir.FullName, "tests")))
                        return candidate;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Could not locate the golden file.");
        }

        public static string Canonical(BattleSimulator sim)
        {
            var builder = new StringBuilder();
            IList<BattleEvent> events = sim.Events;
            for (int i = 0; i < events.Count; i++)
            {
                BattleEvent evt = events[i];
                builder.Append("{\"seq\":").Append(evt.Seq.ToString());
                builder.Append(",\"type\":\"").Append(evt.Type).Append("\"");
                for (int f = 0; f < evt.Fields.Count; f++)
                {
                    builder.Append(",\"").Append(evt.Fields[f].Key).Append("\":\"")
                        .Append(Escape(evt.Fields[f].Value)).Append("\"");
                }
                builder.Append("}\n");
            }
            return builder.ToString();
        }

        private static string Escape(string value)
        {
            if (value == null)
                return "";
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static int Count(BattleSimulator sim, string type)
        {
            int count = 0;
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    count++;
            }
            return count;
        }

        private static BattleEvent Find(BattleSimulator sim, string type)
        {
            for (int i = 0; i < sim.Events.Count; i++)
            {
                if (sim.Events[i].Type == type)
                    return sim.Events[i];
            }
            return null;
        }
    }
}
