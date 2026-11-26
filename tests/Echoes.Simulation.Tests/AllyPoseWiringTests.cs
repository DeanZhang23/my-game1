using System;
using System.Collections.Generic;
using System.IO;
using Echoes.Presentation;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class AllyPoseWiringTests
    {
        private static readonly string[][] Allies =
        {
            new[] { "ashguard", "unit_placeholder_melee" },
            new[] { "oathblade", "unit_oathblade" },
            new[] { "lampsister", "unit_lampsister" },
            new[] { "riftmage", "unit_placeholder_caster" },
            new[] { "misthunter", "unit_misthunter" },
            new[] { "boneapostle", "unit_boneapostle" },
            new[] { "chainjudge", "unit_chainjudge" },
            new[] { "bloodacolyte", "unit_bloodacolyte" }
        };

        [Fact]
        public void Clock_UsesOneRateThatFitsTheExistingIntervals()
        {
            Assert.Equal(8f, AllyPoseClock.FramesPerSecond);
            Assert.Equal(4, AllyPoseClock.FrameCount);
            Assert.Equal(0.08f, AllyPoseClock.MoveHoldSeconds);
            float attack = AllyPoseClock.AttackSeconds;
            Assert.Equal(0.5f, attack);
            Assert.True(attack < 1.2f);
            Assert.True(attack < 1.5f);
            // 125 ms per frame. Faster than this starts to read as a flicker on a 4-frame stride.
            Assert.True(1f / AllyPoseClock.FramesPerSecond >= 0.1f);

            string clock = Read(RepoRoot(), "Assets/Scripts/Presentation/AllyPoseClock.cs");
            Assert.Contains("FramesPerSecond = 8f", clock);
            Assert.Contains("1.2 s", clock);
            Assert.Contains("1.5 s", clock);
        }

        [Fact]
        public void Playback_IdlesUntilAMoveOrASwing_AndAttackDoesNotLoop()
        {
            AllyPoseSample idle = AllyPoseClock.Advance(AllyPose.Idle, 0f, false, false, 0.5f);
            Assert.Equal(AllyPose.Idle, idle.Pose);

            AllyPoseSample walk = AllyPoseClock.Advance(AllyPose.Idle, 0f, true, false, 0.2f);
            Assert.Equal(AllyPose.Walk, walk.Pose);
            Assert.Equal(0, walk.Frame);

            walk = AllyPoseClock.Advance(walk.Pose, walk.Time, true, false, 0.125f);
            Assert.Equal(1, walk.Frame);
            walk = AllyPoseClock.Advance(walk.Pose, walk.Time, true, false, 0.125f);
            Assert.Equal(2, walk.Frame);
            walk = AllyPoseClock.Advance(walk.Pose, walk.Time, true, false, 0.125f);
            Assert.Equal(3, walk.Frame);
            walk = AllyPoseClock.Advance(walk.Pose, walk.Time, true, false, 0.125f);
            Assert.Equal(AllyPose.Walk, walk.Pose);
            Assert.Equal(0, walk.Frame);

            AllyPoseSample stopped = AllyPoseClock.Advance(AllyPose.Walk, 0.2f, false, false, 0.016f);
            Assert.Equal(AllyPose.Idle, stopped.Pose);

            AllyPoseSample swing = AllyPoseClock.Advance(AllyPose.Walk, 0.3f, true, true, 0.2f);
            Assert.Equal(AllyPose.Attack, swing.Pose);
            Assert.Equal(0, swing.Frame);
            Assert.Equal(0f, swing.Time);

            AllyPoseSample frame = AllyPoseClock.Advance(AllyPose.Attack, 0f, false, false, 0.125f);
            Assert.Equal(AllyPose.Attack, frame.Pose);
            Assert.Equal(1, frame.Frame);
            frame = AllyPoseClock.Advance(frame.Pose, frame.Time, true, false, 0.125f);
            Assert.Equal(2, frame.Frame);
            frame = AllyPoseClock.Advance(frame.Pose, frame.Time, false, false, 0.125f);
            Assert.Equal(3, frame.Frame);

            AllyPoseSample done = AllyPoseClock.Advance(AllyPose.Attack, 0.375f, false, false, 0.125f);
            Assert.Equal(AllyPose.Idle, done.Pose);
            AllyPoseSample still = AllyPoseClock.Advance(done.Pose, done.Time, false, false, 0.5f);
            Assert.Equal(AllyPose.Idle, still.Pose);

            AllyPoseSample intoWalk = AllyPoseClock.Advance(AllyPose.Attack, 0.375f, true, false, 0.125f);
            Assert.Equal(AllyPose.Walk, intoWalk.Pose);
            Assert.Equal(0, intoWalk.Frame);

            AllyPoseSample restart = AllyPoseClock.Advance(AllyPose.Attack, 0.3f, false, true, 0.016f);
            Assert.Equal(AllyPose.Attack, restart.Pose);
            Assert.Equal(0, restart.Frame);
            Assert.Equal(0f, restart.Time);
        }

        [Fact]
        public void MoveHold_BridgesTheFiftyMillisecondLattice()
        {
            Assert.True(AllyPoseClock.IsMoving(0f, true));
            Assert.True(AllyPoseClock.IsMoving(0.05f, true));
            Assert.True(AllyPoseClock.IsMoving(0.08f, true));
            Assert.False(AllyPoseClock.IsMoving(0.081f, true));
            Assert.False(AllyPoseClock.IsMoving(0f, false));
            Assert.False(AllyPoseClock.IsMoving(1f, true));
        }

        [Fact]
        public void Catalog_BindsWalkAndAttackForTheEightAllies_AndTheEnemy()
        {
            string root = RepoRoot();
            string catalog = Read(root, "Assets/Resources/M0/M0UnitSprites.asset");
            var seen = new HashSet<string>();
            for (int i = 0; i < Allies.Length; i++)
            {
                string artId = Allies[i][0];
                string unitId = Allies[i][1];
                Assert.True(seen.Add(unitId));
                string block = Entry(catalog, unitId);
                string idle = GuidOf(root, "chr_ally_" + artId + "_idle_01.png");
                int idleAt = block.IndexOf(idle, StringComparison.Ordinal);
                Assert.True(idleAt >= 0, unitId);
                int cursor = idleAt;
                for (int frame = 1; frame <= 4; frame++)
                {
                    string guid = GuidOf(root, "chr_ally_" + artId + "_walk_0" + frame + ".png");
                    int at = block.IndexOf(guid, cursor, StringComparison.Ordinal);
                    Assert.True(at > cursor, artId + " walk " + frame);
                    cursor = at;
                    AssertImport(root, "chr_ally_" + artId + "_walk_0" + frame + ".png");
                }
                for (int frame = 1; frame <= 4; frame++)
                {
                    string guid = GuidOf(root, "chr_ally_" + artId + "_attack_0" + frame + ".png");
                    int at = block.IndexOf(guid, cursor, StringComparison.Ordinal);
                    Assert.True(at > cursor, artId + " attack " + frame);
                    cursor = at;
                    AssertImport(root, "chr_ally_" + artId + "_attack_0" + frame + ".png");
                }
            }

            string enemy = Entry(catalog, "unit_placeholder_enemy");
            const string enemyIdle = "e1d00009e1d00009e1d00009e1d00009";
            int enemyAt = enemy.IndexOf(enemyIdle, StringComparison.Ordinal);
            Assert.True(enemyAt >= 0, "enemy idle");
            string[] enemyWalk =
            {
                "aa4458cbe138a5b540570a6a354e0a14",
                "981ada06e77bd65d2f5215c26f4ab808",
                "36d09fb683adb07bd8144acbfa2afe9c",
                "008fd34b0e5b58a933f03184f4df51f5"
            };
            string[] enemyAttack =
            {
                "0486a0e6f0755ac5651fbd09816779e1",
                "987afadc6a4125735da60837f470d254",
                "0b6822bbf96517456e6bc33465af83b0",
                "5d156f93554e2a835c67e30e4528febe"
            };
            for (int frame = 0; frame < enemyWalk.Length; frame++)
            {
                string file = "chr_enemy_placeholder_walk_0" + (frame + 1) + ".png";
                Assert.Equal(enemyWalk[frame], GuidOf(root, file));
                int at = enemy.IndexOf(enemyWalk[frame], enemyAt, StringComparison.Ordinal);
                Assert.True(at > enemyAt, "enemy walk " + (frame + 1));
                enemyAt = at;
                AssertEnemyImport(root, file);
            }
            for (int frame = 0; frame < enemyAttack.Length; frame++)
            {
                string file = "chr_enemy_placeholder_attack_0" + (frame + 1) + ".png";
                Assert.Equal(enemyAttack[frame], GuidOf(root, file));
                int at = enemy.IndexOf(enemyAttack[frame], enemyAt, StringComparison.Ordinal);
                Assert.True(at > enemyAt, "enemy attack " + (frame + 1));
                enemyAt = at;
                AssertEnemyImport(root, file);
            }
        }

        [Fact]
        public void DocsAndApp_DescribeTheWiredPlayback()
        {
            string root = RepoRoot();
            string style = Read(root, "docs/art/STYLE_GUIDE.md");
            string usage = Read(root, "docs/art/USAGE.md");
            string app = Read(root, "Assets/Scripts/Presentation/TestArenaApp.cs");
            Assert.DoesNotContain("not yet wired into TestArena", style);
            Assert.DoesNotContain("not yet wired into TestArena", usage);
            Assert.DoesNotContain("not yet wired", style);
            Assert.DoesNotContain("not yet wired", usage);
            Assert.DoesNotContain("Not yet wired", style);
            Assert.DoesNotContain("Not yet wired", usage);
            Assert.DoesNotContain("not wired into TestArena", style);
            Assert.DoesNotContain("not wired into TestArena", usage);
            Assert.DoesNotContain("not wired", style);
            Assert.DoesNotContain("not wired", usage);
            Assert.DoesNotContain("still plays `chr_enemy_placeholder_idle`", style);
            Assert.DoesNotContain("still plays `chr_enemy_placeholder_idle`", usage);
            Assert.Contains("`unit_placeholder_enemy` is wired into TestArena", style);
            Assert.Contains("`unit_placeholder_enemy` is wired into TestArena", usage);
            Assert.Contains("8 frames per second", style);
            Assert.Contains("8 frames per second", usage);
            Assert.Contains("basic-attack swing", style);
            Assert.Contains("basic-attack swing", usage);
            Assert.Contains("Movement loops the walk", usage);
            Assert.Contains("Faces **left**", style);
            Assert.Contains("#8B2E32", style);
            Assert.Contains("#8B2E32", usage);
            Assert.Contains("732×732", style);
            Assert.Contains("732×732", usage);
            Assert.Contains("evt.Get(\"kind\") == \"basic\"", app);
            Assert.Contains("AllyPoseClock.Advance", app);
            Assert.Contains("LocKeys.Begin", app);
            Assert.Contains("LocKeys.Roll", app);
            Assert.Contains("LocKeys.Confirm", app);
            Assert.Contains("\"Board\", BoardPos", app);
        }

        private static void AssertImport(string root, string file)
        {
            string meta = Read(root, "Assets/_Project/Art/Characters/" + file + ".meta");
            Assert.Contains("textureType: 8", meta);
            Assert.Contains("spritePixelsToUnits: 64", meta);
            Assert.Contains("filterMode: 1", meta);
            Assert.Contains("spritePivot: {x: 0.5, y: 0.125}", meta);
        }

        // Enemy feet sit at (0.50, 0.15). Do not reuse the ally pivot 0.125.
        private static void AssertEnemyImport(string root, string file)
        {
            string meta = Read(root, "Assets/_Project/Art/Characters/" + file + ".meta");
            Assert.Contains("textureType: 8", meta);
            Assert.Contains("spritePixelsToUnits: 64", meta);
            Assert.Contains("filterMode: 1", meta);
            Assert.Contains("spritePivot: {x: 0.5, y: 0.15}", meta);
            Assert.DoesNotContain("spritePivot: {x: 0.5, y: 0.125}", meta);
        }

        private static string GuidOf(string root, string file)
        {
            string meta = Read(root, "Assets/_Project/Art/Characters/" + file + ".meta");
            int at = meta.IndexOf("guid: ", StringComparison.Ordinal);
            Assert.True(at >= 0, file);
            int end = meta.IndexOf('\n', at);
            return meta.Substring(at + 6, end - (at + 6)).Trim();
        }

        private static string Entry(string catalog, string unitId)
        {
            string marker = "unitId: " + unitId;
            int at = catalog.IndexOf(marker, StringComparison.Ordinal);
            Assert.True(at >= 0, unitId);
            int next = catalog.IndexOf("\n  - unitId:", at + marker.Length, StringComparison.Ordinal);
            if (next < 0)
                next = catalog.Length;
            return catalog.Substring(at, next - at);
        }

        private static string Read(string root, string relative)
        {
            return File.ReadAllText(Path.Combine(root, relative));
        }

        private static string RepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "docs", "gdd", "m0-test-arena.md")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repo root not found.");
        }
    }
}
