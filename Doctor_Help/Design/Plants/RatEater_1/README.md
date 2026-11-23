# RatEater_1

Prefab: `Assets/Prefabs/Basement/RatEater_1.prefab`.
Editable Spine 4.3 Essential project: `RatEater_1.spine` with region images in `images/`.
Runtime export: `Assets/Art/Plants/RatEater_1/` (atlas, texture, JSON, material and SkeletonDataAsset).

- `Idle`: 2.4 seconds, loops with subtle stem/head sway and an open active mouth.
- `Eat`: 1.68 seconds, plays once. Anticipation, forward stem lean, enlarged mouth, snap shut, recoil and two small closed-mouth chewing movements.
- `Chew`: exactly 1 second with two pronounced chewing pulses, roughly 13% head squash/stretch at maximum compression, stronger head/stem movement and closed-jaw pressure. Repeated by code throughout the 30-second cooldown. The clip itself is not 30 seconds long.

The pot stays fixed. Stem, head and jaws use independent bones and region attachments, compatible with Spine Essential. No weighted meshes are required. PNG parts were extracted from the approved existing artwork; original sprites are preserved.

`RatEater.TryEat()` returns false unless the plant is ready. Eat completion starts Chew and the cooldown; reaching zero returns to Idle. Disabling resets the cycle and removes the completion listener. No rat detection, killing, placement, inventory or save behavior is implemented by this animation component.

To try it, drag the prefab into a scene, enter Play Mode and use the RatEater component context menu **Preview > Eat (Play Mode)**. It starts in Idle. **Preview > Reset Idle (Play Mode)** resets it for another comparison. `cooldownSeconds` is editable on the prefab, default 30.

The GIFs in `Preview/` are sampled from Unity's actual Spine renderer at 20 FPS. `RatEater_1_Demo.gif` shows Idle, Eat, three Chew loops and Idle for a compact visual preview; the component's actual cooldown is 30 seconds. The Eat GIF repeats for viewing, while runtime Eat is one-shot.

Validation: prefab saved/reloaded through Unity API; three repeated Eat→Chew→Idle cycles; duplicate Eat rejected; still cooling down at 29.9 seconds and ready after 30 seconds; completion listener detached on disable; Chew duration checked at 1 second. Rendered multiple frames to check the stem/head connection, mouth closure and animation changes. Validation used an isolated Editor preview scene with manually stepped runtime components. No gameplay scene or player save was loaded; device build and full Play Mode integration have not been tested.

Texture: no mipmaps, max size 2048, ASTC 6×6 overrides for Android and iPhone. Source full-size art remains under House_Prop/BasementDefense.

Color correction: this project's Linear color space requires the exported straight-alpha atlas to have sRGB (Color Texture) enabled, Alpha Is Transparency enabled, and Straight Alpha Texture enabled on its Spine material. The atlas previously had sRGB disabled, which washed out its colors. Corrected on this atlas only and verified by rendering all three animations again. Keep these settings when re-exporting; Spine's default texture importer may initially assume PMA. Reference: https://esotericsoftware.com/spine-unity-assets
