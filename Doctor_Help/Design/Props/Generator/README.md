# generator

Prefab: `Assets/Prefabs/Basement/generator.prefab`.
Editable Spine 4.3 Essential project: `generator.spine`; source region PNGs in `images/`.
Unity data, atlas, texture and material: `Assets/Art/Props/Generator/`.

- `Idle`: one-second static clip, all parts stationary; default prefab state.
- `Operating`: two-second looping clip. The flywheel makes two full clockwise revolutions per loop and the gauge needle fluctuates within the dial. The whole machine gently squashes/stretches by ±0.9% over two seconds, with no translation jitter or rotational shake. Matching end poses keep the loop continuous.

Only body, wheel and needle are included in the rig and atlas. Draw order is wheel behind body, then needle on top. Wiring/terminal boards remain independent objects outside this prefab, ready for their own animation later.

`GeneratorAnimationController.SetOperating(true/false)` controls the visual state. In Play Mode use its Operating checkbox or component context menu **Preview > Start Operating (Play Mode)** / **Preview > Stop (Play Mode)**. Repeating the current state does not restart playback. Electricity generation, rat attacks and wire animations are not connected in this change.

Rendering uses the approved existing art with sRGB enabled, straight-alpha export and material, no mipmaps, max size 2048, and Android/iOS ASTC 6×6. Original full-canvas source PNGs are preserved under House_Prop/BasementDefense. Wheel and needle have separate bones with pivots at their actual hubs.

Validation uses an isolated Unity Editor preview scene and manually stepped Spine runtime components. Prefab is saved and reloaded through Unity APIs; Idle/Operating switching, idempotent state requests, attachment names, sampled bone transforms and multiple rendered frames are checked. No player saves or gameplay scenes are loaded. Full Play Mode integration and a device build are not tested.
