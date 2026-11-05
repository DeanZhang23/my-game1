# Nia Island

Unity 2D fishing game. This repo currently implements **Ticket E1 only**: a pier scene, Nia walking left and right inside pier bounds, and the cast / bite / hook state machine.

Slice 1 design lives in [`docs/gdd/slice-1-mvp.md`](docs/gdd/slice-1-mvp.md) (design-owned). Later GDD slices belong in `docs/gdd/` as well.

E1 does not include fish tables, catch result UI, login, or the intro. Those are later tickets. The pier scene is the default Play scene, so no login or intro is required.

## Unity version

- **Assumed editor:** Unity **2022.3.62f1 LTS** (built-in render pipeline, uGUI, Input Manager).
- Unity 6 (6000.x) can open the project. Allow the upgrade if the editor asks.
- Active Input Handling must be **Input Manager (Old)** or **Both**. An editor script switches **Input System Package (New)** over to Input Manager (Old) on load. If Unity then asks to restart, restart and press Play again.

## How to test in Play Mode

1. Add this folder as a Unity project and open it with Unity 2022.3 LTS (or Unity 6 and accept the upgrade).
2. If a dialog says the input handling change needs a restart, restart the editor.
3. Open `Assets/Scenes/Pier.unity` (menu: **Nia Island → Open Pier Scene**). It is the only scene in Build Settings.
4. Press Play. You should see a placeholder pier, Nia (black cat, golden eyes), and a **Cast** button. There is no login or intro.
5. Hold **A** or **Left Arrow**, then **D** or **Right Arrow**. Nia moves along the pier and stops at the ends (about x = -5.2 and 5.2). She faces the direction she walks.
6. Click **Cast**. The on-screen state goes **Cast → Waiting**. A line and bobber travel out over the water.
7. When the state reaches **BiteWindow**, a **BITE!** cue and screen flash appear and the button label becomes **Hook**. Do not click. After about 1 second the state becomes **Miss**, the Console logs `Miss`, then the state returns to **Ready** and the button reads **Cast** again.
8. Click **Cast** again. During **BiteWindow**, click **Hook**. The Console logs `Resolved`, the state shows **Resolved**, then it returns to **Ready**.
9. Stop Play. Select the **Fishing** object. In the Inspector, set **Bite Window Duration** (default `1`, Slice 1 range about `0.8`–`1.2`). Press Play and repeat step 7. The bite cue stays up for that many seconds. You can also change it while Play Mode is running; the open window uses the new value on the next tick.

Clicks outside Ready and BiteWindow do nothing. Cast, Waiting, Miss, and Resolved ignore the button until the machine is Ready again.

## Project layout

- `Assets/Scenes/Pier.unity` — pier, Nia, bobber, line, and the fishing component.
- `Assets/Scripts/Fishing/FishingStateMachine.cs` — Ready, Cast, Waiting, BiteWindow, Miss, Resolved. No fish data.
- `Assets/Scripts/Fishing/FishingController.cs` — Inspector timings, button, bobber, and line.
- `Assets/Scripts/Fishing/FishingHud.cs` — state label, BITE cue, Cast/Hook button.
- `Assets/Scripts/Pier/NiaMovement.cs` — left/right movement clamped to the pier.
- `Assets/Art/white.png` — 4×4 white sprite tinted by SpriteRenderers.

Placeholder art is colored rectangles. That is intentional for E1.

## Logic tests

The state machine and pier clamp do not reference `UnityEngine`, so they can run without the editor:

```bash
dotnet test tests/FishingLogic.Tests
```
