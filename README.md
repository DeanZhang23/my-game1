# Nia Island

Unity 2D fishing game. This repo currently implements **Ticket E1** (pier movement and the cast / bite / hook state machine) and **E1.5** (MVP art wired into that scene).

Slice 1 design lives in [`docs/gdd/slice-1-mvp.md`](docs/gdd/slice-1-mvp.md). Art notes live in [`docs/art/USAGE.md`](docs/art/USAGE.md) and [`docs/art/STYLE_GUIDE.md`](docs/art/STYLE_GUIDE.md).

Fish tables, catch result UI, login, and the intro are later tickets. The pier scene is the default Play scene.

## Unity version

- **Assumed editor:** Unity **2022.3.62f1 LTS** (built-in render pipeline, uGUI, Input Manager).
- Unity 6 (6000.x) can open the project. Allow the upgrade if the editor asks.
- Active Input Handling must be **Input Manager (Old)** or **Both**. An editor script switches **Input System Package (New)** over to Input Manager (Old) on load. If Unity then asks to restart, restart and press Play again.

## How to test in Play Mode

1. Add this folder as a Unity project and open it with Unity 2022.3 LTS (or Unity 6 and accept the upgrade).
2. If a dialog says the input handling change needs a restart, restart the editor.
3. Open `Assets/Scenes/Pier.unity` (menu: **Nia Island → Open Pier Scene**). It is the only scene in Build Settings.
4. Set the Game view to a **16:9** aspect (the night backdrop is 1280×720 and fills that frame).
5. Press Play. You should see the night pier backdrop, Nia as the black cat sprite (golden eyes), and the parchment **CAST** button. There is no login or intro, and the old colored-rectangle pier is hidden.
6. Hold **A** or **Left Arrow**, then **D** or **Right Arrow**. Nia walks along the pier (walk frames, facing the way she moves) and stops at the ends (about x = -7.4 and 7.4). Standing still plays idle frames.
7. Click **CAST**. The on-screen state goes **Cast → Waiting**. A line and bobber travel out over the water.
8. When the state reaches **BiteWindow**, the green hook cue flashes and the button sprite swaps to **HOOK**. Do not click. After about 1 second the state becomes **Miss**, the Console logs `Miss`, the cue hides, and the state returns to **Ready** with the **CAST** button.
9. Click **CAST** again. During **BiteWindow**, click **HOOK**. The Console logs `Resolved`, the state shows **Resolved**, then it returns to **Ready**.
10. Stop Play. Select the **Fishing** object. In the Inspector, set **Bite Window Duration** (default `1`, Slice 1 range about `0.8`–`1.2`). Press Play and repeat step 8. The bite cue stays up for that many seconds. Changing it during Play Mode applies on the next tick.

Clicks outside Ready and BiteWindow do nothing. Cast, Waiting, Miss, and Resolved ignore the button until the machine is Ready again.

## Project layout

- `Assets/Scenes/Pier.unity` — pier, Nia, bobber, line, and the fishing component.
- `Assets/Scripts/Fishing/FishingStateMachine.cs` — Ready, Cast, Waiting, BiteWindow, Miss, Resolved. No fish data.
- `Assets/Scripts/Fishing/FishingController.cs` — Inspector timings, button, bobber, and line.
- `Assets/Scripts/Fishing/FishingHud.cs` — state label, bite cue, Cast/Hook button.
- `Assets/Scripts/Pier/NiaMovement.cs` — left/right movement clamped to the pier.
- `Assets/Scripts/Pier/NiaVisual.cs` — idle and walk frames from the sliced cat sheet.
- `Assets/Art/Backgrounds/bg_pier_night.png` — night pier backdrop (pixels per unit 72, Point filter).
- `Assets/Art/Characters/char_nia_idle_00.png` … `char_nia_walk_02.png` — transparent cuts from `sheet_nia_idle_walk.png`. Feet pivot, Point filter, no compression.
- `Assets/Art/UI/ui_btn_cast.png`, `ui_btn_hook.png`, `ui_cue_bite.png` — cuts from `ui_fishing_controls.png`.

The concept sheets are 1280×720 illustrations with a baked checkerboard, so Play Mode uses these tight transparent cuts instead of a literal 64×64 grid. Fish silhouettes stay unwired (E2). The line and bobber are still simple shapes; the art pack has no line sprite.

## Logic tests

The state machine and pier clamp do not reference `UnityEngine`, so they can run without the editor:

```bash
dotnet test tests/FishingLogic.Tests
```
