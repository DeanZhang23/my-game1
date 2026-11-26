# 命运残响 / Echoes of Fate

M0 Test Arena. Unity **6.3 LTS** (`6000.3.29f1`, changeset `96d2fe17529c`) with URP 17.3 and the 2D Renderer. The battle is pure 2D: an orthographic camera and a greybox UI. Formation tokens draw official ally idle sprites. There is no 3D mesh pipeline.

The design is `docs/gdd/m0-test-arena.md`, overridden where they conflict by `docs/gdd/m0-test-arena-placement.md`. State machine: Setup → Countdown → AutoCombat → Resolve. There is no tactical window and no pre-battle dice. After March both sides seek, move, and auto-attack. During AutoCombat the ally team shares one fate pool (cap 100). At 50 the player may roll three dice once, then confirm. Combat does not pause. Effects land by role. Crack, corrosion, and two-hero link faces stay disabled. March itself grants no attack, armor, shield, keen, energy, cooldown, or gold.

## Open the project

1. Install Unity `6000.3.29f1` with the 2D and URP modules.
2. Open this repository folder in the Unity Hub (the folder that contains `Assets/` and `ProjectSettings/`).
3. Wait for packages to import. On the first editor load, `Assets/Editor/EnsureUrp2DPipeline.cs` creates `Assets/Settings/EchoesURP2D.asset` (URP asset + 2D Renderer) and assigns it to Graphics and Quality settings. Save the project if Unity marks those settings dirty.
4. Open `Assets/Scenes/TestArena.unity`. It is the only scene in the build. Press Play. There is no login or intro.

Input is the legacy Input Manager (`activeInputHandler: 0`) so the greybox buttons work without the Input System package.

## Run the tests

Simulation (`Assets/Scripts/Simulation`) has no Unity references. Formula tests and the Golden Seed run on .NET 8:

```bash
dotnet test tests/Echoes.Simulation.Tests/Echoes.Simulation.Tests.csproj --configuration Release
```

The same command runs in CI (`.github/workflows/simulation-tests.yml`). Golden Seed replays are simulation-only: they never start the presentation.

The golden script is seed `424242`, default lineup, March with no pre-battle dice, then AutoCombat. It polls every 50ms of pumped budget and, on the first return where fate can pay, calls the same `Roll` and `Confirm` the greybox buttons use. It does not roll inside `PumpMs`, so same-timestamp hits that already resolved are included. The file is `tests/Echoes.Simulation.Tests/golden/m0-midcombat-dice.txt` (sha256 `a4707d49ea7acb0a3e524b6c7aa50ebc10aa1cf403621fecd89080ef5887bcf3`). That fight rolls feather / shield / feather at 10.000s (twin feather; the shield fizzles because the melee ally is already dead), then resolves as a defeat at 10.800s with 0 gold. `m0-autocombat.txt` and `m0-lock0-reroll.txt` are gone.

To regenerate the golden file after an intentional simulation change, explain the change in the PR and run:

```bash
UPDATE_GOLDEN=1 dotnet test tests/Echoes.Simulation.Tests/Echoes.Simulation.Tests.csproj --filter GoldenSeed_MidCombatDice
```

`FormulaTests` still cover the §8 formulas, including twin / attunement coefficients. This slice does not rewrite them. Face numbers still live in `FaceEffects`. Confirm applies them by role.

## Play Mode checklist

Unity `6000.3.29f1`, scene `Assets/Scenes/TestArena.unity`. The cloud VM has no Unity Editor, so this list was not run in Play Mode.

Default lineup until an ally is moved during Setup: melee on `ally_c2` (`unit_placeholder_melee`), caster on `ally_a2` (`unit_placeholder_caster`), foes on `enemy_c1` and `enemy_c2` (`unit_placeholder_enemy`). Enemy cells are the encounter preset. There is no enemy placement editor. The melee token shows `chr_ally_ashguard_idle_01` and the label 灰烬卫士 (Ashguard). The caster token shows `chr_ally_riftmage_idle_01` and the label 裂隙术士 (Riftmage). Both foes show `chr_enemy_placeholder_idle` and the shared label 锈蚀步卒 (Rustsoldier). Those strings live in `M0Localization.asset` under the existing `unit.placeholder_*.name` keys. Unit ids and the combat panel (HP, attack, spell power, armor, MR, crit, attack interval, skill energy) stay the same. Role, move speed, and attack range are on the unit assets: melee and the enemy are `melee` / 3.0 / 0.9, the caster is `ranged` / 2.5 / 3.5.

The canvas is a left / center / right layout. The ally 3×3 is on the left: back `*_a*` on the outer left, mid `*_b*` in the middle, front `*_c*` on the inner edge facing right toward the enemies. The enemy 3×3 is in the center: front `*_c*` on its left edge facing the allies, then mid, then back. Column 1 is the top file and column 3 is the bottom file. Reserve (待命) is a strip just left of the ally grid and appears only during Setup. Seed, March (出征), 掷骰, and 确认 are on the right. There is no lock and no Reroll (重掷). The combat log is under that column and does not cover March or the dice buttons. The board, midline, portraits, and captions do not receive clicks.

Screen positions are a uniform map of the simulation plane: 64 px per unit, sim `(0, 0)` at `(-368, 40)`. Adjacent slot centers are 1.5 apart (96 px) and the ally front center to the enemy front center is 4.0 (256 px). Cell widgets are 90×78 so they do not overlap that pitch. After March, the HP panels follow simulation positions instead of staying on the grid.

1. Press Play. The left grid shows the ashguard idle on `ally_c2` (front column, middle file, facing the center) labeled 灰烬卫士, and the riftmage idle on `ally_a2` (back column, middle file) labeled 裂隙术士. The center grid shows the enemy placeholder idle on `enemy_c1` and `enemy_c2` (front column, facing left), both labeled 锈蚀步卒. The locale button still switches zh-Hans and en; English labels are Ashguard, Riftmage, and Rustsoldier. Type a seed or leave it blank, then press Begin. No dice have been rolled. 掷骰 and 确认 are on the right and stay disabled through Setup: fate is 0, and there is no pre-battle roll. During Setup, drag an ally onto another ally cell to swap, or onto an empty ally cell to move. Or click a token, then click a cell, another token, or Reserve (待命). Each cell shows its slot id. The same idles and names stay on the unit panels after March.
2. Put both allies on Reserve. March (出征) stays on screen, stays disabled, and does not start the countdown. The status line shows 「空阵不可出征」. Put at least one ally back on any ally cell. March works again. The board does not steal the click.
3. March on the default lineup, or after a move. Countdown runs for about 3 seconds and the tokens can no longer be dragged or clicked into a new cell. There is no tactical window and combat does not pause. Spawn lines in the log include the display name and the slot id (`灰烬卫士 ally_c2`, `裂隙术士 ally_a2`, `锈蚀步卒 enemy_c1`). Later damage lines include both slots. The simulation event still records the unit id, not the display name.
4. When AutoCombat starts, both sides walk off the grid toward their targets. Melee starts out of attack range and closes. A unit in range stops and auto-attacks; if the target dies or leaves range, it retargets and walks again. The panels follow those positions. No unit snaps back onto a cell after March. Fate energy starts at 0 and rises only in this phase. 掷骰 stays disabled below 50.
5. When fate reaches 50, 掷骰 becomes clickable. Click it. Fate drops by 50 immediately and three faces appear on the right. Both sides keep walking and attacking. There is no target picker. 确认 is enabled only now. Click it before 6 seconds: the faces apply on their own (melee faces to living allied melee, the star to living allied ranged, feather and skull to living allies, coin to the battle). Wait instead, and 确认 fires on its own at 6 seconds. 掷骰 stays disabled after that one roll even if fate climbs again. The board does not steal the click from 掷骰, 确认, or 出征.
6. Let the fight finish. A wipe or the 150 second timeout enters Resolve. If a roll is still unconfirmed, it commits before the result. Coin gold is added only on victory. The golden script (seed `424242`, no formation edit) ends in defeat at 10.800s with 0 gold.

## Content

Definitions are ScriptableObjects under `Assets/Resources/M0/`. Player-facing copy is only in `M0Localization.asset` (zh-Hans default, en on the locale button). Runtime HP stays on the battle instance. Setup does not roll. The six M0 faces keep their numbers; confirm chooses recipients by role.
