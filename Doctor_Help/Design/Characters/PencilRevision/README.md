# Pencil revision — 2026-11-05

All ten authored NPCs (Dracula plus nine classic characters) now use revised pencil sprites, saved into their existing part and NPC prefabs. Generated with the built-in image_gen tool; selected transparent source atlases are in Sources and prompts in Prompts. Medusa additionally received background extraction to remove a gray halo.

The comparison render includes the original NPC_0 at top-left and original NPC_1 at bottom-right, at the same camera scale. The other cells show Dracula, Frankenstein, Sherlock, Jekyll/Hyde, Invisible Man, Grim Reaper, Medusa, Headless Horseman, Mummy and Pinocchio.

- Pressure-variable graphite outlines, thin offset contour strokes and material hatching replace clean hard outlines and volumetric rendering.
- Ordinary characters still reference the existing project heads. Special head sprites use the original head canvas and pixels per unit; anatomy transforms, Bone, Body, Neck and Heart remain those of NPC_0.
- Eyes are fitted to the visible bounds of the original sliced Eye_03. Nose and mouth have independent bounds derived from the two portions of original Mouth_04, preserving the original left-facing positions. Pinocchio retains an extended nose.
- Clothing is fitted to the original outfit footprint with a small coverage allowance, rather than the previous oversized 3.3 × 4.4 envelope. Dracula's rear collar remains behind Neck and Head with an open neckline.
- Saved part transforms are also applied to their nested source prefabs. Old NPC instance overrides were replaced using the official appearance tool so reapplying the authored look keeps the same placement.
- The additional Frankenstein facial stitches overlay was removed because it crossed an eye; the stitched mouth and patched costume remain.
- Existing GUIDs, profile identity, diseases, dialogue and spawn catalog were preserved. Corresponding newspaper portraits were refreshed.

Verified saved prefab reload, unchanged anatomy transforms, no missing scripts, feature bounds and nonoverlap, stable locked nested parts after reapply, and assembled renders in Unity 6000.3.9f1 Edit Mode. Main Unity Editor was also observed showing the revised Grim Reaper. This does not constitute Play Mode or Android testing. The user's unsaved Office scene was not saved or replaced.

Temporary authoring and inspection scripts live only in the disposable batch project and are removed after installation. Latest preview: Comparison.png. Older images in PublicDomain and Dracula are historical previews superseded by this folder.
