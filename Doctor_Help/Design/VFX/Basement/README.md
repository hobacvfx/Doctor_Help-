# Basement VFX flipbooks

Two packed PNG sprite sheets, each **1024×768**, four columns × three rows, **12 frames of exactly 256×256**. No loose PNG frame files are exported.

- `Assets/VFX/Texture/Basement/FrostPetal_Impact_Sheet_4x3.png`: one-shot ice-petal collision flash, expanding blue crystal fragments, frost flecks, dissipation.
- `Assets/VFX/Texture/Basement/Rat_Electrocuted_Ash_Sheet_4x3.png`: top-down, head-up crawling-rat orientation matching Rat_Run_01; electricity → two X-ray skeleton flashes → charcoal/ash → scattered dust → disappearance. No gore.

Read left-to-right, then top-to-bottom (01–04, 05–08, 09–12). Frame12 is fully transparent in both sheets. Use the fixed full-cell framing and centered pivot; do not trim each frame independently. Transparent safety gutters are built into each cell. The rat sheet contains the rat itself: when integrating the death VFX, replace/hide the original rat sprite to avoid drawing two rats. This asset request does not hook into health/damage or gameplay.

Suggested playback: 12 FPS, one-shot (1 second including the transparent frame); tune speed in the eventual VFX player. The review GIFs in this folder loop for viewing and hold the transparent ending briefly; the runtime effects should not loop.

Unity import: Sprite Multiple with 12 embedded sub-sprites (01–12), each 256×256, center pivot, Full Rect mesh, 100 PPU, no generated physics shapes, sRGB/Alpha Is Transparency, no mipmaps, clamp/bilinear, max1024. Android/iPhone overrides ASTC4×4 for thin electric/ice detail. These are sub-assets inside the two PNGs, not separate image files. No animation controller or gameplay script is added.

Art generated using built-in image_gen; exact prompts in Generation.prompts.txt. Original selected sheets are under Source/. PrepareSheets.py mechanically normalizes their grid to the user's required size and generates the review GIFs entirely in memory. It never exports individual PNG frames. Manifest.json records the dimensions and playback order.
