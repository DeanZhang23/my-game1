# 命运残响 / Echoes of Fate

M0 Test Arena. Unity **6.3 LTS** (`6000.3.29f1`, changeset `96d2fe17529c`) with URP 17.3 and the 2D Renderer. The battle is pure 2D: an orthographic camera and a greybox UI. There is no 3D mesh pipeline.

The design is `docs/gdd/m0-test-arena.md`. This slice is E1 plus E1.1: dice, the combat state machine, formula tests, one Golden Seed, and Setup placement on the ally 3×3.

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

Unity `6000.3.29f1`, scene `Assets/Scenes/TestArena.unity`. The cloud VM has no Unity Editor, so this list was not run in Play Mode.

Default lineup until an ally is moved during Setup: melee on `ally_c2` (`unit_placeholder_melee`), caster on `ally_a2` (`unit_placeholder_caster`), foes on `enemy_c1` and `enemy_c2` (`unit_placeholder_enemy`). Enemy cells are the encounter preset. There is no enemy placement editor.

Ally cells, the reserve strip, and the row labels sit below the dice and the reroll / march buttons (nothing interactive above y=90) and left of the log (nothing interactive at x≥350). Those controls stay clickable. Decorative text does not take clicks. A dragged token does not raycast, so releasing it over a die, Reroll, or March cancels the move instead of stealing the click.

1. Press Play. The grids show the default lineup. Tokens are not draggable yet. Type a seed or leave it blank, then press Begin. Three dice faces appear. During Setup, drag an ally onto another ally cell to swap, or onto an empty ally cell to move. Or click a token, then click a cell, another token, or Reserve (待命). Each cell shows its slot id.
2. Put both allies on Reserve. March (出征) stays on screen and does not start the countdown. The status line says the formation is empty. Put at least one ally back on any ally cell. March works again.
3. March on the default lineup, or after a move. Countdown starts and the tokens can no longer be dragged or clicked into a new cell. Spawn lines in the log include the slot id (`近战 ally_c2`). Later damage lines include both slots.
4. Position check, using sword / shield / star faces (seeds are in the E1 list below, or reroll until those faces show). Melee on a front cell (`ally_c*`) gains keen, and a shield face gives that front unit a shield. Caster on a back cell (`ally_a*`) gains starting energy from a star. The same units on a mid cell (`ally_b*`) do not gain keen, that shield, or that energy. Team attack, armor, and skill-damage bonuses still apply on the mid row.
5. From a fresh Begin, click one die to lock it. The free-reroll count is still 1. Click Reroll (重掷). The count goes from 1 to 0, the log gains a reroll line, the locked face stays, and at least the unlocked faces are rerolled. Then click March (出征). Countdown starts and the fight proceeds.
6. From a fresh Begin, reroll again after the free one: crack goes to 1, then 2, then 3 with corrosion. A fifth reroll stays at crack 3 with corrosion still on.
7. Lock all three dice and reroll: nothing changes, and the free reroll is not spent.
8. Attunement: enter seed `79` (three shields) or `223` (three swords), Begin, and March without rerolling. The log shows attunement. About 8 seconds into combat the tactical window opens. Confirm or Skip (or wait 8 seconds). The log records `tactical_window_opened` and then `confirmed` or `skipped`. There is no six-face tactic in this slice.
9. March on any roll and let the fight finish. Kills or the 150 second timeout enter Resolve and show victory or defeat, plus gold. Coin gold is paid only on a win.
10. The Golden Seed script (seed `424242`, lock the first die, one free reroll, no formation edit, skip the window if it opens) ends in victory at 12.5s with 8 gold. That replay is what `dotnet test` locks. Formation edits are not part of the script, so the golden file is unchanged.

## Content

Definitions are ScriptableObjects under `Assets/Resources/M0/`. Player-facing copy is only in `M0Localization.asset` (zh-Hans default, en on the locale button). Runtime HP, dice, and buffs stay on the battle instance.
