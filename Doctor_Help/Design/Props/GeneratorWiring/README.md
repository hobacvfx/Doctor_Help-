# generator_wiring

Independent prefab: `Assets/Prefabs/Basement/generator_wiring.prefab`.
Editable Spine Essential 4.3 project: `generator_wiring.spine`; region images in `images/`, full rat art in `SourceArt/`.
Unity export: `Assets/Art/Props/GeneratorWiring/`.

Exactly two animations:

- `BreakWire`: 2.6 seconds, one-shot. Rat climbs from behind the right wire arch, rotating with the cable slope, leans in and bites. At 1.28 seconds the intact wire changes to broken ends and the Spine event `wire_break` fires. The rat gnaws the right broken end, on the same cable half supporting its paws.
- `Gnaw`: 1 second, loop. Continues the same rat and wire pose, with jaw chewing, small head/body movements and slight movement of the end being gnawed. Both broken ends remain broken throughout the loop.

The final pose of BreakWire matches the first pose of Gnaw. The controller transitions with zero mix duration so the wire state does not flash back. Rat slots begin behind the arch, then head/jaw draw above it as the rat reaches the bite point.

Rat rig scale is 0.55 (about 31% larger than the previous 0.42). The mouth follows a curved approach along the right cable; body rotation changes from -72 degrees near the base to -25 degrees at the bite point. During Gnaw, the rat follows the right wire end's small movement to maintain mouth contact.

`GeneratorWiringAnimationController.TryBreakWire()` starts the sequence only from the intact state. It automatically enters looping Gnaw after BreakWire completes. `ResetIntact()` clears playback and restores intact wiring with hidden rat. `IsBroken` becomes true at the bite event. `playOnEnable` is false by default, so placing the prefab shows intact wiring.

To preview in Play Mode, use component context menu **Preview > Break Wire (Play Mode)**. This resets and replays the full sequence. **Preview > Reset Intact (Play Mode)** restores the original wire.

The prefab contains the board, wire parts and this animation's rat, with no generator body, flywheel or gauge. It is not parented into the generator prefab. No electricity deductions, generator shutdown, rat pathing, save changes or plant interactions are connected here.

Texture: straight-alpha export and material, sRGB enabled for this Linear project, no mipmaps, max 2048, Android/iOS ASTC 6×6. Existing intact/broken wire art is reused. New rat art was generated using built-in imagegen; exact prompts are in `RatArt.prompts.txt`.

Validation: Unity prefab save/reload; repeated start/reset/loop cycles; duplicate starts blocked; bite event remains false at 1.2 seconds and true after 1.28 seconds; Gnaw loops while retaining broken attachments; end/start head, jaw and wire bone poses matched. Multiple actual Unity-rendered frames are inspected. Validation uses an isolated Editor preview scene and manually stepped runtime components, with no gameplay scene or player saves loaded. Full Play Mode integration and device builds are not tested.
