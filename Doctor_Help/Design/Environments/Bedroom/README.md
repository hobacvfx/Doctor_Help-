# Player Home bedroom

Created and revised with built-in imagegen on 2026-11-23. Source/style references are recorded in `Generation.prompt.txt`; the height revision and extraction prompts are in `Revision_v2.prompt.txt`.

## Current PNG assets

- `Assets/2D_Assets/House_Prop/Bedroom/Bedroom_Background.png`: bed-free room, with the shortened wardrobe, lower wall pegs and shorter lowered coat. Original asset GUID is preserved.
- `Assets/2D_Assets/House_Prop/Bedroom/Bedroom_Bed.png`: the complete bed as a separate transparent PNG, including its wooden frame, feet, high pillows, mattress/sheet and half-cover burgundy blanket.
- `Bedroom_Composite_v2.png` in this Design folder: approved-direction composite preview before extraction. `Source/Bedroom_Composite_v1.png` preserves the previous full-room version.

Both runtime PNGs retain the 1536×1024 room canvas and original lower-left bed registration. They use Sprite Single, PPU100, centre pivot, no mipmaps and ASTC6×6 Android/iPhone overrides. To assemble, use the same position/scale for both sprites and place the bed at a higher sorting order. A same-scale import keeps the bathroom footprint. Furniture has not been rigged or placed into a scene.

The wardrobe is about 25% shorter while its feet remain on the floor. The peg rail is lower, and its coat is now a short jacket with a visible gap above the floor. The original wallpaper, clock, room proportions and brick perimeter remain the reference.

Alpha was inspected from the actual PNG channels, rather than the generated RGB preview: sample positions around the bed and between its open parts are alpha zero. The brown atmospheric RGB visible in some previews is transparent at those positions. Low-alpha edge specks from image generation may remain; no opaque rectangle or floor strip is included in the bed PNG.

References: AGENTS.md and STYLE_GUIDE.md; Bathroom_Background for scale/camera/frame; NenPhongKhamTrong (the Home sprite in Player_Home) for damask wallpaper and walnut wainscot; Medicine_Collection for pencil rendering. No gameplay or scene changes were made.
