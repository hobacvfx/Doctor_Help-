# Classic NPC collection

Latest pencil textures, prefab proportions and Unity preview supersede the historical images here: see [PencilRevision](../PencilRevision/README.md).

Nine modular NPC prefabs in `Assets/2D_Assets/NPC`, with part prefabs/sprites in `Assets/2D_Assets/Special_Look/Classic`. The assembled Unity renders in `NPC_Collection_Preview.png` use this row order:

1. Frankenstein's Creature / Sherlock Holmes / Dr. Jekyll–Mr. Hyde
2. The Invisible Man / Grim Reaper / Medusa
3. Headless Horseman / The Mummy / Pinocchio

Headless Horseman is represented as a pumpkin-headed rider. Ordinary characters reuse the project's existing Head_Male/Head_Female sprites. Bandaged, skull, pumpkin and wooden-puppet characters use special heads. All retain the original Body, Bone, Neck and Heart transforms; Bone scale remains (1,1,1).

Eye, Hair, Mouth and Outfit are locked nested appearance prefabs. Noses are independent nested prefabs; Frankenstein has a separate stitched-scar overlay. Special necks are separate sprites on the original Neck renderer. Each new NPC has a stable prefab GUID, one profile entry, one spawn entry and a newspaper portrait. Disease assignments use the existing authoring rule (one disease, independent chance of a compatible second disease); dialogue slots are unassigned. Existing NPC records were preserved. NpcProfile and NpcCatalog now contain 41 entries.

Clothing uses broad muted colors, uneven black pencil outlines and sparse hatching, with long bodies/sleeves fitted to standard anatomy. Layering keeps the head and facial parts over front clothing while the front clothing covers the lower neck base. Dracula has an additional rear collar/cape layer behind Neck and Head. Shared materials, original head sprites and existing NPC_0 were not changed.

## Source art

Generated with the built-in image_gen tool using original project sprites as references. The `*-prompt.txt`, `clothing-pencil-prompt.txt` and `Medusa_Hair-prompt.txt` files record prompts. `*_Flat_Source.png` sheets contain the component sources; the ordinary head cells are unused. `Clothing_Pencil_Source.png` supplies the final garment art and scar overlay. `Medusa_Hair_Source.png` is the uncropped final snake-hair source. Cropping/import and prefab authoring were performed through Unity APIs.

## Verification

Unity 6000.3.9f1 batch Edit Mode on an isolated temporary project: compiled, loaded saved prefabs, checked connected nested parts, stable profile/catalog identities and compatible disease IDs, verified original anatomy transforms and Heart/lip references, inspected assembled render output and checked torso coverage against an NPC_0 baseline. Face sprites were recropped to exclude neighboring-cell fragments. This was not a full daily-flow Play Mode or Android-device test. No player save was accessed. The open Office scene was not saved or replaced.

Temporary authoring scripts and the isolated test project are removed after delivery; use the existing NPC authoring tools and nested prefab Inspectors for future changes.
