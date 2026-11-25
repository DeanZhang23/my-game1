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
        public void GoldenSeed_LockFirstDie_RerollOnce_ReplaysExactly()
        {
            string actual = Canonical(Play());
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
            sim.SetLock(0, true);
            sim.Reroll();
            sim.Commit();
            sim.BeginAutoCombat();
            sim.RunToEnd(TacticalDecision.Skip);
            return sim;
        }

        private static string GoldenPath()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = Path.Combine(dir.FullName, "tests", "Echoes.Simulation.Tests", "golden", "m0-lock0-reroll.txt");
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
    }
}
