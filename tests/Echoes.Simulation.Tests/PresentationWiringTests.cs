using System;
using System.IO;
using Echoes.Presentation;
using Echoes.Simulation;
using Xunit;

namespace Echoes.Simulation.Tests
{
    public class PresentationWiringTests
    {
        [Fact]
        public void Death_TakesOverAnAttack_ThenHoldsTheLastFrame()
        {
            Assert.Equal(8f, AllyPoseClock.FramesPerSecond);
            Assert.Equal(4, AllyPoseClock.FrameCount);
            Assert.Equal(0.5f, AllyPoseClock.AttackSeconds);

            AllyPoseSample started = AllyPoseClock.Advance(AllyPose.Attack, 0.25f, true, false, true, 0.2f);
            Assert.Equal(AllyPose.Death, started.Pose);
            Assert.Equal(0, started.Frame);
            Assert.Equal(0f, started.Time);

            AllyPoseSample mid = AllyPoseClock.Advance(AllyPose.Death, 0f, false, true, false, 0.125f);
            Assert.Equal(AllyPose.Death, mid.Pose);
            Assert.Equal(1, mid.Frame);

            AllyPoseSample done = AllyPoseClock.Advance(AllyPose.Death, 0.375f, true, true, true, 0.125f);
            Assert.Equal(AllyPose.Death, done.Pose);
            Assert.Equal(3, done.Frame);

            AllyPoseSample held = AllyPoseClock.Advance(done.Pose, done.Time, false, true, true, 1f);
            Assert.Equal(AllyPose.Death, held.Pose);
            Assert.Equal(3, held.Frame);

            int frame;
            Assert.False(AllyPoseClock.TryDeathFrame(0.5f, out frame));
            Assert.Equal(3, frame);
            Assert.True(AllyPoseClock.TryDeathFrame(0.375f, out frame));
            Assert.Equal(3, frame);
        }

        [Fact]
        public void VfxClock_IsAPointFourSecondOneShot()
        {
            Assert.Equal(10f, VfxClock.FramesPerSecond);
            Assert.Equal(4, VfxClock.FrameCount);
            Assert.Equal(0.4f, VfxClock.Seconds);
            int frame;
            Assert.True(VfxClock.TryFrame(0f, out frame));
            Assert.Equal(0, frame);
            Assert.True(VfxClock.TryFrame(0.1f, out frame));
            Assert.Equal(1, frame);
            Assert.True(VfxClock.TryFrame(0.3f, out frame));
            Assert.Equal(3, frame);
            Assert.False(VfxClock.TryFrame(0.4f, out frame));
            Assert.Equal(3, frame);
            Assert.False(VfxClock.TryFrame(2f, out frame));
            Assert.Equal(3, frame);
        }

        [Fact]
        public void FateTrack_MatchesTheExistingRollGate()
        {
            Assert.Equal(BattleSimulator.DiceRollCost, FateTrack.Tick);
            Assert.Equal(FatePool.Cap, FateTrack.Max);
            Assert.Equal(FateTrackState.Empty, FateTrack.Classify(0, false));
            Assert.Equal(FateTrackState.Filling, FateTrack.Classify(1, false));
            Assert.Equal(FateTrackState.Filling, FateTrack.Classify(49, false));
            Assert.Equal(FateTrackState.Ready, FateTrack.Classify(50, false));
            Assert.Equal(FateTrackState.Ready, FateTrack.Classify(100, false));
            Assert.Equal(FateTrackState.Spent, FateTrack.Classify(50, true));
            Assert.Equal(FateTrackState.Spent, FateTrack.Classify(0, true));
            Assert.True(FateTrack.ShowCanRoll(50, false));
            Assert.False(FateTrack.ShowCanRoll(50, true));
            Assert.False(FateTrack.ShowCanRoll(49, false));
        }

        [Fact]
        public void HudMetrics_UseTheLockedSizesAndRustFill()
        {
            Assert.Equal(88f, HudMetrics.NameplateW);
            Assert.Equal(38f, HudMetrics.NameplateH);
            Assert.Equal(2f, HudMetrics.FeetGap);
            Assert.Equal(240f, HudMetrics.FateBarW);
            Assert.Equal(16f, HudMetrics.FateBarH);
            Assert.Equal(100f, HudMetrics.SecondaryW);
            Assert.Equal(32f, HudMetrics.SecondaryH);
            Assert.Equal(208f, HudMetrics.PrimaryW);
            Assert.Equal(36f, HudMetrics.PrimaryH);
            Assert.Equal(48f, HudMetrics.DiceSlot);
            Assert.Equal("#8B2E32", HudMetrics.HpFill);
            Assert.Equal("#C9A227", HudMetrics.ShieldFill);
            Assert.Equal("ui.testarena.fate_energy", HudMetrics.FateEnergy);
            Assert.Equal("combo.twin", HudMetrics.ComboTwin);
            Assert.Equal("combo.attunement", HudMetrics.ComboAttunement);
            float top = HudMetrics.PlateTopY(12f, 32f, 0.15f, 2f);
            Assert.Equal(-1.2f, top, 3);
            Assert.Equal("血契侍从", HudMetrics.Ellipsize("血契侍从", 80f));
            Assert.EndsWith("…", HudMetrics.Ellipsize("血契侍从血契侍从血契侍从", 80f));
        }

        [Fact]
        public void SfxVolumes_AreTheDocumentedMix()
        {
            Assert.Equal(0.70f, TestArenaSfx.Hit);
            Assert.Equal(0.85f, TestArenaSfx.Down);
            Assert.Equal(0.80f, TestArenaSfx.Roll);
            Assert.Equal(0.75f, TestArenaSfx.Confirm);
            Assert.Equal(0.90f, TestArenaSfx.March);
        }

        [Fact]
        public void Catalog_BindsDeathForTheThreeDefaultUnitsOnly()
        {
            string root = RepoRoot();
            string catalog = Read(root, "Assets/Resources/M0/M0UnitSprites.asset");
            string melee = Entry(catalog, "unit_placeholder_melee");
            string caster = Entry(catalog, "unit_placeholder_caster");
            string enemy = Entry(catalog, "unit_placeholder_enemy");
            AssertDeath(root, melee, "chr_ally_ashguard_death_0", "spritePivot: {x: 0.5, y: 0.125}");
            AssertDeath(root, caster, "chr_ally_riftmage_death_0", "spritePivot: {x: 0.5, y: 0.125}");
            AssertDeath(root, enemy, "chr_enemy_placeholder_death_0", "spritePivot: {x: 0.5, y: 0.15}");

            string idleMeta = Read(root, "Assets/_Project/Art/Characters/chr_enemy_placeholder_idle.png.meta");
            Assert.Contains("guid: e1d00009e1d00009e1d00009e1d00009", idleMeta);
            Assert.Contains("spritePivot: {x: 0.5, y: 0.15}", idleMeta);
            Assert.Contains("spritePixelsToUnits: 64", idleMeta);

            string[] others =
            {
                "unit_oathblade", "unit_lampsister", "unit_misthunter",
                "unit_boneapostle", "unit_chainjudge", "unit_bloodacolyte"
            };
            for (int i = 0; i < others.Length; i++)
            {
                string block = Entry(catalog, others[i]);
                Assert.DoesNotContain("death:", block);
            }
        }

        [Fact]
        public void Docs_DescribeTheTestArenaWiring()
        {
            string root = RepoRoot();
            string style = Read(root, "docs/art/STYLE_GUIDE.md");
            string usage = Read(root, "docs/art/USAGE.md");
            string sfx = Read(root, "docs/audio/m0-testarena-sfx.md");
            string hud = Read(root, "docs/ui/m0-testarena-hud.md");
            Assert.DoesNotContain("not yet wired", style);
            Assert.DoesNotContain("not wired", style);
            Assert.DoesNotContain("not yet wired", usage);
            Assert.DoesNotContain("not wired", usage);
            Assert.DoesNotContain("leave these sprites unplayed", style);
            Assert.DoesNotContain("leave these sprites unplayed", usage);
            Assert.DoesNotContain("later engineering ticket", style);
            Assert.DoesNotContain("later engineering ticket", usage);
            Assert.DoesNotContain("does not wire TestArena", style);
            Assert.DoesNotContain("does not wire TestArena", usage);
            Assert.Contains("holds frame 04", style);
            Assert.Contains("holds frame 04", usage);
            Assert.Contains("10 frames per second", style);
            Assert.Contains("10 frames per second", usage);
            Assert.Contains("`unit_placeholder_enemy` is wired into TestArena", style);
            Assert.Contains("8 frames per second", usage);
            Assert.Contains("basic-attack swing", usage);
            Assert.Contains("Movement loops the walk", usage);
            Assert.Contains("Faces **left**", style);
            Assert.Contains("#8B2E32", style);
            Assert.Contains("732×732", usage);
            Assert.DoesNotContain("no engine wiring", sfx);
            Assert.Contains("PlayOneShot", sfx);
            Assert.Contains("~0.70", sfx);
            Assert.Contains("~0.85", sfx);
            Assert.Contains("~0.80", sfx);
            Assert.Contains("~0.75", sfx);
            Assert.Contains("~0.90", sfx);
            Assert.DoesNotContain("Do not wire these slices", hud);
            Assert.Contains("88×38", hud);
            Assert.Contains("ui.testarena.fate_energy", hud);

            string loc = Read(root, "Assets/Resources/M0/M0Localization.asset");
            Assert.Contains("ui.testarena.fate_energy", loc);
            Assert.Contains("ui.testarena.btn_march", loc);
            Assert.Contains("ui.testarena.btn_roll", loc);
            Assert.Contains("ui.testarena.btn_confirm", loc);
            Assert.Contains("combo.twin", loc);
            Assert.Contains("combo.attunement", loc);
            Assert.Contains("ui.testarena.can_roll", loc);
            Assert.Contains("ui.unit.hp", loc);
            Assert.Contains("ui.unit.shield", loc);
            Assert.Contains("出征", loc);
            Assert.Contains("掷骰", loc);
            Assert.Contains("确认", loc);
        }

        [Fact]
        public void App_WiresPropsVfxSfxHudAndDeathWithoutMovingCards()
        {
            string root = RepoRoot();
            string app = Read(root, "Assets/Scripts/Presentation/TestArenaApp.cs");
            string chrome = Read(root, "Assets/Scripts/Presentation/TestArenaChrome.cs");
            string kit = Read(root, "Assets/Resources/M0/M0TestArenaKit.asset");

            Assert.Contains("UnitCardLayout.Place", app);
            Assert.Contains("place.CardX", app);
            Assert.Contains("place.SpriteX", app);
            Assert.Contains("new Vector2(UnitCardLayout.PortraitRestX, UnitCardLayout.PortraitRestY), 32f", app);
            Assert.Contains("preserveAspect = true", app);
            Assert.DoesNotContain("SetNativeSize()", app);
            Assert.DoesNotContain("SetNativeSize()", chrome);
            Assert.DoesNotContain("0.45f, 0.75f, 0.42f", app);
            Assert.Contains("HudMetrics.HpFill", chrome);
            Assert.Contains("#8B2E32", Read(root, "Assets/Scripts/Presentation/TestArenaWiring.cs"));

            int commit = app.IndexOf("!_sim.CanCommit", StringComparison.Ordinal);
            int march = app.IndexOf("sfxMarch", StringComparison.Ordinal);
            Assert.True(commit >= 0 && march > commit);
            int rollGate = app.IndexOf("!_sim.CanRoll", StringComparison.Ordinal);
            int roll = app.IndexOf("sfxRoll", StringComparison.Ordinal);
            Assert.True(rollGate >= 0 && roll > rollGate);
            Assert.Contains("reason == \"player\" || reason == \"timeout\"", app);
            Assert.Contains("TestArenaSfx.Hit", app);
            Assert.Contains("TestArenaSfx.Down", app);
            Assert.Contains("QueueHit", app);
            Assert.Contains("QueueDeath", app);
            Assert.Contains("widget.hasDeath", app);
            Assert.Contains("AllyPose.Death", app);
            Assert.Contains("LocKeys.Roll", app);
            Assert.Contains("LocKeys.Confirm", app);
            Assert.Contains("LocKeys.Commit", app);
            Assert.Contains("PlayOneShot", chrome);
            Assert.Contains("EnsureAudioListener()", chrome);
            int listener = chrome.IndexOf("EnsureAudioListener();", StringComparison.Ordinal);
            int source = chrome.IndexOf("AddComponent<AudioSource>()", StringComparison.Ordinal);
            Assert.True(listener >= 0 && source > listener);
            Assert.Contains("FindAnyObjectByType<AudioListener>() != null", chrome);
            const string listenerOnCameraObject = "camera.gameObject.AddComponent<AudioListener>()";
            Assert.Contains(listenerOnCameraObject, chrome);
            Assert.DoesNotContain("AddComponent<AudioListener>()", chrome.Replace(listenerOnCameraObject, ""));

            string scene = Read(root, "Assets/Scenes/TestArena.unity");
            int camera = scene.IndexOf("m_Name: Main Camera", StringComparison.Ordinal);
            Assert.True(camera > 0);
            int listenerBlock = scene.IndexOf("AudioListener:", StringComparison.Ordinal);
            Assert.True(listenerBlock > camera);
            Assert.Contains("m_GameObject: {fileID: 100001}", scene.Substring(listenerBlock, Math.Min(200, scene.Length - listenerBlock)));
            Assert.Equal(1, Count(scene, "!u!81"));
            Assert.Contains("raycastTarget = false", chrome);
            Assert.Contains("env_riftarena_prop_dressing.png", app);
            Assert.Contains("env_riftarena_ground.png", chrome);
            Assert.Contains("ui_dice_", chrome);
            Assert.Contains("VfxClock.TryFrame", chrome);
            Assert.Contains("pivot = new Vector2(0.5f, 0.18f)", chrome);
            Assert.Contains("pivot = new Vector2(0.5f, 0.5f)", chrome);

            Assert.Contains("bfb34d8d4bf94256a86aacfeee0f30b3", kit);
            Assert.Contains("5ec69033fd4648ab8c9d8734ba100167", kit);
            Assert.Contains("8ba510bf66604657ae12ae89bb803a66", kit);
            Assert.Contains("06ca607e87e44f1db457939e3a9661e8", kit);
            Assert.Contains("378fa55c77a74f4685c15ccc10f606bd", kit);
            Assert.Contains("a596e530fcd6be4c3142341a67841c77", kit);
            Assert.Contains("b695ebaea65101ab737ed6390a09be27", kit);
            Assert.Contains("be4e171b8ba9410f89438ee3edab1dd3", kit);
            Assert.Contains("a2327c963313495dbe4986a616c70703", kit);

            for (int i = 0; i < TestArenaDressing.LockedPlates.Length; i++)
            {
                string file = TestArenaDressing.LockedPlates[i];
                string path = Path.Combine(root, "Assets/_Project/Art/Environment/TestArena", file);
                Assert.True(File.Exists(path), file);
                Assert.False(File.Exists(path + ".meta"), file + " meta");
            }
            for (int i = 0; i < TestArenaDressing.GroundOverlays.Length; i++)
            {
                string file = TestArenaDressing.GroundOverlays[i];
                Assert.True(File.Exists(Path.Combine(root, "Assets/_Project/Art/Environment/TestArena", file)));
            }
            Assert.True(File.Exists(Path.Combine(root, "Assets/_Project/Art/Environment/TestArena", TestArenaDressing.OptionalComposite)));
            for (int i = 0; i < TestArenaDressing.LooseStamps.Length; i++)
            {
                string file = TestArenaDressing.LooseStamps[i];
                string meta = Read(root, "Assets/_Project/Art/Environment/TestArena/" + file + ".meta");
                int at = meta.IndexOf("guid: ", StringComparison.Ordinal);
                int end = meta.IndexOf('\n', at);
                string guid = meta.Substring(at + 6, end - (at + 6)).Trim();
                Assert.Contains(guid, kit);
            }
        }

        private static void AssertDeath(string root, string block, string prefix, string pivot)
        {
            int cursor = 0;
            for (int frame = 1; frame <= 4; frame++)
            {
                string file = prefix + frame + ".png";
                string meta = Read(root, "Assets/_Project/Art/Characters/" + file + ".meta");
                Assert.Contains("textureType: 8", meta);
                Assert.Contains("spritePixelsToUnits: 64", meta);
                Assert.Contains(pivot, meta);
                int at = meta.IndexOf("guid: ", StringComparison.Ordinal);
                int end = meta.IndexOf('\n', at);
                string guid = meta.Substring(at + 6, end - (at + 6)).Trim();
                int found = block.IndexOf(guid, cursor, StringComparison.Ordinal);
                Assert.True(found > cursor, file);
                cursor = found;
            }
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

        private static int Count(string text, string needle)
        {
            int count = 0;
            int at = 0;
            while (true)
            {
                at = text.IndexOf(needle, at, StringComparison.Ordinal);
                if (at < 0)
                    return count;
                count++;
                at += needle.Length;
            }
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
