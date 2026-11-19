# Count Dracula

Latest pencil textures, fitted prefab and Unity preview: [PencilRevision](../PencilRevision/README.md). Earlier preview images in this folder are historical.

Original literary Dracula adapted to Doctor Help's existing hand-drawn modular NPC style. No actor likeness or movie costume reference was used.

- NPC: `Assets/2D_Assets/NPC/NPC_Dracula.prefab`
- Parts: `Assets/2D_Assets/Special_Look/Dracula/`
- Head references the existing `Assets/2D_Assets/Head_Male.png`; the animated transform and examination references are preserved.
- Eye / Hair / Mouth / Outfit use locked nested prefabs. Nose is a nested prefab under Head. The high rear collar/cape is a separate `Dracula_RearCollar.prefab` in its own locked RearCollar slot.
- Identity: prefab GUID, display name Count Dracula, one existing Vampire Infection sickness ID. The dialogue asset is intentionally unassigned.
- Source sheet: `Dracula_Parts_Source.png`, generated with the built-in image_gen tool. Unity extracts the six components without repainting them. `generation-prompt.txt` records the prompt.
- `Dracula_Preview.png` is captured from the assembled saved Unity prefab, not generated concept art.

Edit the sprite files or nested part prefabs to adjust appearance. Keep the four Lock Override checkboxes enabled to retain this authored character when applying appearance again. The character's parts live outside the generic random-part folders, so they do not change the random appearance pool for existing NPCs.

The outfit retains ClothSeeThroughController and the original X-ray material. Body, Bone, Neck and Heart transforms match NPC_0; Bone scale is (1, 1, 1). The rear collar renders behind both Neck and Head. Clothing is fitted to the standard anatomy. Heart and symptom anchors remain inherited from NPC_0. The newspaper portrait and its catalog entry are included.

Clothing was revised to flat muted colors with irregular black pencil outlines and sparse hatching. The final clothing source/prompt is shared with the PublicDomain character set. Original source sheets remain as art history; the generated head from the original sheet is not used by this prefab.

## Verification

Verified in Unity 6000.3.9f1 Edit Mode: saved asset reload, connected nested prefabs, no missing scripts, unique profile/spawn identity, preservation of the 31 previous profile records, three fresh consultations, cached temperature within each consultation, Vampire Infection resolving to zero BPM and low temperature, exactly one Tim and no Nao, and no ChatBox with an empty dialogue slot. Walk-in/idle/walk-out clips were sampled with root-position measurements and rendered frames. X-ray was rendered with per-renderer property blocks. This is not a full Play Mode daily-flow or Android test. The open Office scene's existing unsaved changes were preserved.
