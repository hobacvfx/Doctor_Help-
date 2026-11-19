# Victorian NPC redraw

Six newly generated modular looks, installed in the game project. Built-in imagegen was used; prompts are the adjacent `*-prompt.txt` files and original transparent source atlases are `*_Atlas.png`. The old GPT character artwork was not used as a generation/edit source. References were NPC_0 alone for shape/direction and the approved medicine collection for rendering.

## Installed assets

- `Assets/2D_Assets/NPC/NPC_Sherlock.prefab` — Sherlock Holmes.
- `Assets/2D_Assets/NPC/NPC_Frankenstein.prefab` — Frankenstein's Creature.
- `Assets/2D_Assets/NPC/NPC_Rapper.prefab` — tattooed Victorian street performer.
- `Assets/2D_Assets/NPC/NPC_Mummy.prefab` — The Mummy.
- `Assets/2D_Assets/NPC/NPC_Noblewoman.prefab` — Victorian noblewoman.
- `Assets/2D_Assets/NPC/NPC_Courtesan.prefab` — adult Victorian courtesan (gái bán hoa).

Individual PNGs and locked nested Eye/Hair/Mouth/Outfit part prefabs live in `Assets/2D_Assets/Special_Look/VictorianRedraw/<character>/`. Nose is a separate sibling of Mouth so dialogue can hide the mouth without hiding the nose. The updated outer garments cover the torso and skeleton on their own. All ordinary NPCs reuse the existing NPC_0 head. Mummy has a newly generated wrapped shell over the same head anchor.

The saved Head/Neck/Body/Bone/Heart/All transforms are identical to NPC_0. Outfit coverage was fitted to NPC_0 rather than deforming the rig. The new facial images account for the original sliced SpriteRenderer sizing, rather than measuring only the raw texture size. Hairpieces were regenerated without ears to avoid double ears. Source cropping isolates the intended connected hairpiece to exclude neighboring atlas marks.

NpcProfile and NpcCatalog contain all six identities. New records received one disease and an independent chance of a compatible second disease using the existing authoring helper. Each now has an examination dialogue asset based on its assigned diseases. Six newspaper portraits were added. Original profile/catalog/portrait data was backed up in `Assets/_Recovery/NpcVictorianRedraw_20261107_133814` before installation.

The November 10 outfit pass uses seven new outer garments, including a dedicated Dracula outfit in `Assets/2D_Assets/Special_Look/Dracula`. It removes the five Garment lining objects from the patient prefabs. Generation and sleeve correction prompts are recorded in [SingleLayerOutfit-prompt.txt](SingleLayerOutfit-prompt.txt). The collection and verification renders below record the initial installation.

The outfit pass was verified in Unity with the saved prefabs, isolated player data and Office input. All seven patients entered, displayed dialogue and departed. The lens revealed the skeleton through each new outfit and the opaque outfit returned after release.

## Review and verification

`Collection.png` is the assembled Unity render of the six looks at the same camera scale. NPC_0 was rendered separately as the size reference. Review caught and fixed undersized facial parts, duplicate ears, exposed shoulder/waist anatomy and a neighboring hair fragment in the atlas crop. Rendering uses dark pencil contours, fine offset strokes, material hatching and cel-shaded color blocks.

Edit Mode verification reloaded the saved prefabs, checked unchanged rig anchors, locked nested parts, face center ordering, Heart and animator references, and missing scripts, then rendered each assembled character. `Verification/Verify.txt` records the checks.

Isolated Play Mode used the actual PatientSpawnPoint and saved prefabs/profile to spawn all six, measure arrival and departure across frames, wait for idle, render and destroy the departing patient. All six moved 4.43 world units on arrival and 7.92 on departure. The profile/save bootstrap was disabled only in the disposable test project; PlayerProfileService remained null. See `Verification/Play.txt`. The last hair-only crop cleanup was additionally checked by saved-prefab Edit Mode renders. This original check covered animation, not the full day loop, dialogue or Android devices. X-ray was subsequently tested separately as described below.

## X-ray repair

The new outfit/hair sprites already referenced M_Cloth_1, but their part prefabs lacked ClothSeeThroughController. The extra garment lining and mummy wrapped head lacked it too. Added the missing components on the renderer GameObjects via Unity prefab APIs: 6 outfits, 5 hair parts, 5 lining layers and 1 wrapped head. Part prefabs and all 6 assembled NPC prefabs are saved, preserving identities, pose and sorting. Shared materials and runtime scripts were not changed.

Isolated Play Mode selected the actual Office X-ray tool and used the saved MagnifierLens geometry, then held, dragged and released through ExaminationToolSelection for each NPC. Checks confirmed all active scan layers received the same lens position/radius, rendered pixels changed to expose the skeleton, and releasing restored the normal image with zero residual pixels in the scan region. Lens artwork was hidden only in captures to isolate the shader result. The test used a separate fixture with no player save service and was not an Android or touchscreen device test. Reports and before/during/after images: `XrayVerification/`.

The reusable rendering and integration specification for future work is `Design/Art/STYLE_GUIDE.md`, linked from AGENTS.md and the scoped art rule.

Temporary setup/test scripts and the isolated project are removed after installation; use the existing NPC appearance and profile authoring tools for future edits. Keep the appearance slots locked when preserving these authored characters.
