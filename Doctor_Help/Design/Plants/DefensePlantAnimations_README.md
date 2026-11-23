# Basement defense plant animation pack

Three standalone animated prefabs, prepared from the approved v4 design. They are now sold in the basement Plant Shop and driven by the basement defense simulation (Range, rat HP, damage, slow); see AGENTS.md, section "Tầng hầm: ba cây bắn xa đã triển khai".

- `Assets/Prefabs/Basement/ThornCactus_1.prefab`
- `Assets/Prefabs/Basement/VoltMushroom_1.prefab`
- `Assets/Prefabs/Basement/FrostSunflower_1.prefab`

For each plant, `Design/Plants/<name>/` contains an editable Spine **4.3 Essential** project, authoring JSON, separate PNG regions in `images/`, original generated part sheet in `SourceArt/`, and actual Unity-rendered GIF previews in `Preview/`. Unity atlas/JSON/material/data assets are in `Assets/Art/Plants/<name>/`.

## Clips

**ThornCactus_1**
- Idle: 2.4-second loop, subtle body/arm sway.
- Attack: 0.8 seconds, inward compression then forceful expansion. No flying needles in the rig. `fire` event at 0.32 seconds, payload 3 (volley count). Returns to Idle.

**VoltMushroom_1**
- Idle: 2.4-second loop, stem/cap sway and independently pulsing cyan underside halo.
- Attack: 0.9 seconds, cap squashes to charge and expands, with `fire` at 0.32 seconds (payload 1). Returns to Idle.
- No beam or LineRenderer demo is included in the prefab/controller. Gameplay code should draw the future electric stream to the actual target. The cap gills remain luminous, and the separate pulsing underside halo is retained.

**FrostSunflower_1**
- Idle: 2.4-second loop, small stem/leaf/head sway.
- Attack: 3.2-second volley, head turns three revolutions in the image plane. Exactly 16 adjacent petals disappear sequentially, one every 0.2 seconds. No flying petal sprites/bones/slots are included. `fire` payload is the petal/shot index 0–15. No fixed aim direction is authored.
- Reload: 1.4 seconds, petals grow from their roots with a slight settling overshoot, then return to Idle. The empty head remains throughout the start of reload. 16-petal magazine and 1.4-second reload are prototype authoring choices, not finalized balance settings.
- No four-point sparkle stars are baked into petals or leaves. Internal white diamond-facet reflections are retained. Three separate **ParticleSystem** children animate transient crystal glints using the project's existing Star_Shape_02 texture. They can be edited/disabled independently.

All pots/soil remain fixed. Rigs use region attachments and bones only; no weighted meshes or Spine Pro features.

## Runtime handoff

`Assets/Scripts/Gameplay/Basement/DefensePlantAnimation.cs` controls standalone presentation:
- Starts with looping Idle.
- `TryAttack()` starts a complete authored attack/volley only when not busy; repeated requests during Attack/Reload are rejected.
- The sunflower automatically goes Attack → Reload → Idle; the others go Attack → Idle.
- `PlayIdle()` cancels the action and restores the petals.
- `Fired` exposes the Spine fire event integer payload described above; it performs no damage transaction.
- Component context menus in Play Mode: **Preview / Attack Volley**, **Preview / Reset Idle**.
- `TryGetMuzzlePosition(out Vector3)` provides a world-space presentation anchor for future projectile/beam code. The sunflower anchor follows its stem/head centre without rotating around the rim. Direction, target tracking, projectile movement and hits belong to gameplay; this component never chooses an endpoint or emits a beam/projectile.

Independent upward-facing projectile Sprites are prepared at `Assets/2D_Assets/House_Prop/BasementDefense/Projectiles/`: `ThornCactus_Needle.png` and `FrostSunflower_Petal.png`, with bottom-center pivots. `BasementDefenseView` instantiates these and aims them at the actual rats.

**In the basement** the component runs in `simulated` mode: it never starts Idle/Attack on its own and ignores `TryAttack`/completion. The view calls `ShowStrike(attacking, elapsed, speed)` (cactus, mushroom) or `ShowMagazine(ammo, magazine, reloading, reloadElapsed, reloadSeconds, sinceShot, interval)` (sunflower) to pose the clip at the simulated time, so visible petals always equal the saved ammo. Damage, targeting, projectiles and the LineRenderer beam belong to `BasementDefenseSimulation`/`BasementDefenseView`. Gameplay stats: cactus Damage 2 / Targets 3 / AC 3s / Normal; mushroom Damage 10 / Targets 1 / AC 6s / Normal; sunflower Damage 0.5 / Targets 1 / AC 0.2s / Far, slow 30% for 3s without stacking. Attack clip duration and gameplay cooldown are separate concerns.

## Rendering/import

Straight-alpha Spine atlases, sRGB on for this Linear project, Alpha Is Transparency on, material `_STRAIGHT_ALPHA_INPUT` enabled, no mipmaps, max 2048, ASTC 6×6 Android/iPhone overrides. Original part art is generated with built-in image_gen; prompts are in `DefensePlantParts.prompts.txt`. `BuildDefensePlantRigs.py` is the repeatable source-sheet slicing/rig JSON authoring tool (Pillow; user requested part separation). Spine CLI imports this JSON and exports the editable project and packed Unity assets.

## Validation scope

Unity compiled and all three prefabs were saved/reloaded. Isolated Editor preview instances completed three attack cycles each; duplicate attacks were blocked; the cactus/mushroom emitted 3 fire events each across three cycles; sunflower emitted 48, depleted all 16 petals sequentially, restored all during Reload and returned to Idle. Confirmed no shot bones/slots or LineRenderer remain; all three presentation anchors are valid. All clips rendered at 20 FPS; actual frames were inspected for joints, fixed pots, glow and petal states. Tests used isolated preview scenes and no gameplay scenes/saves. This is manually stepped Editor validation, not full Play Mode/gameplay integration or device-build testing.

Gameplay integration (2026-11-22) was additionally tested in Play Mode with an isolated preview save: targeting, cadence, magazine/reload, slow, save/restart and the rendered needle/petal/beam visuals. See AGENTS.md for the full list and known limits.
