# 命运残响 / Echoes of Fate

M0 Test Arena. Unity **6.3 LTS** (`6000.3.29f1`, changeset `96d2fe17529c`) with URP 17.3 and the 2D Renderer. The battle is pure 2D: an orthographic camera and a greybox UI. There is no 3D mesh pipeline.

The design is `docs/gdd/m0-test-arena.md`. This slice is ticket E1 only: dice, the combat state machine on a fixed lineup, formula tests, and one Golden Seed.

## Open the project

1. Install Unity `6000.3.29f1` with the 2D and URP modules.
2. Open this repository folder in the Unity Hub (the folder that contains `Assets/` and `ProjectSettings/`).
3. Wait for packages to import. On the first editor load, `Assets/Editor/EnsureUrp2DPipeline.cs` creates `Assets/Settings/EchoesURP2D.asset` (URP asset + 2D Renderer) and assigns it to Graphics and Quality settings. Save the project if Unity marks those settings dirty.
4. Open `Assets/Scenes/TestArena.unity`. It is the only scene in the build. Press Play. There is no login or intro.

Input is the legacy Input Manager (`activeInputHandler: 0`) so the greybox buttons work without the Input System package.

## Run the tests

Simulation (`Assets/Scripts/Simulation`) has no Unity references. Formula tests, dice/crack tests, and the Golden Seed run on .NET 8:

```bash
dotnet test tests/Echoes.Simulation.Tests/Echoes.Simulation.Tests.csproj
```

The same command runs in CI (`.github/workflows/simulation-tests.yml`). Golden Seed replays are simulation-only: they never start the presentation.

To regenerate the golden file after an intentional formula change, explain the change in the PR and run:

```bash
UPDATE_GOLDEN=1 dotnet test tests/Echoes.Simulation.Tests/Echoes.Simulation.Tests.csproj --filter GoldenSeed_LockFirstDie
```

## Play Mode checklist

Fixed lineup (no formation UI): melee on `ally_c2`, caster on `ally_a2`, foes on `enemy_c1` and `enemy_c2`.

1. Press Play. Type a seed or leave it blank, then press Begin. Three dice faces appear.
2. Click one die to lock it, then Reroll. That first reroll is free: crack stays 0 and the locked face does not change. Reroll again for crack 1, then 2, then 3. The fourth paid reroll shows corrosion. A fifth reroll stays at crack 3 with corrosion still on.
3. Lock all three dice and reroll: nothing changes, and the free reroll is not spent.
4. Attunement: enter seed `79` (three shields) or `223` (three swords), Begin, and March without rerolling. The log shows attunement. About 8 seconds into combat the tactical window opens. Confirm or Skip (or wait 8 seconds). The log records `tactical_window_opened` and then `confirmed` or `skipped`. There is no six-face tactic in this slice.
5. March on any roll and let the fight finish. Kills or the 150 second timeout enter Resolve and show victory or defeat, plus gold. Coin gold is paid only on a win.
6. The Golden Seed script (seed `424242`, lock the first die, one free reroll, skip the window if it opens) ends in victory at 12.5s with 8 gold. That replay is what `dotnet test` locks.

## Content

Definitions are ScriptableObjects under `Assets/Resources/M0/`. Player-facing copy is only in `M0Localization.asset` (zh-Hans default, en on the locale button). Runtime HP, dice, and buffs stay on the battle instance.
