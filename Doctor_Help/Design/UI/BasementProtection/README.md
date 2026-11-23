# Basement protection UI assets

Approved concept: `Protection_Popups_Concept_v1.png`. The concept's scrollbar is superseded: keep vertical drag scrolling, with no visible scrollbar, rail, thumb or arrow buttons.

Ready-to-import sprites: `Assets/Art/UI/BasementProtection/` (15 separate RGBA PNGs).

## Reusable UI pieces

- `Popup_Square`: protection popup background; use Image Type **Sliced** and a square RectTransform.
- `Popup_Shop`: portrait shop background; **Sliced**.
- `Plant_Card`: horizontal list item background; **Sliced**.
- `Title_Plate`: burgundy header, no baked title; **Sliced**.
- `Button_Action` / `Button_Disabled`: matching canvas, pivot and borders, no baked label or cost; **Sliced**. Reuse for Buy and Upgrade. Place UI text and water cost as separate children.
- `Button_Close`: burgundy close button with X.
- `Button_Swap`: round teal button with circular arrows, no text.
- `Button_Add`: round teal plus button.
- `Icon_Water`: clean-water currency icon.
- `Plant_Nameplate`: blank cream name/level plate; **Sliced**.
- `Clipboard_Clip`: separate brass clip; place centered over the panel's top edge. Do not slice.

Use **Simple**, preserve aspect for circular buttons, icons, clip and portraits. Sliced sprites include transparent outer gutters; do not trim them or overwrite their authored borders. Borders/pixel dimensions are recorded in `AssetManifest.json` and saved in each texture's Unity metadata. All pivots are centered; 100 pixels per unit. Draw names, levels, stats and prices with actual UI text. Keep an independently sized central vertical divider in the protection layout rather than baking it into the popup background.

Action/disabled buttons and the plant nameplate use left/right slice borders only (top/bottom zero) so their bevels remain continuous at compact UI heights. Keep a consistent button height and stretch width for labels. Panel/card/title sprites use four-sided borders.

## Plant portrait illustrations

- `Portrait_RatEater`: UI-only likeness of the existing RatEater_1 plant.
- `Portrait_ThornSpitter_Concept` and `Portrait_SpottedBell_Concept`: separated illustrative shop portraits from the approved design. These are not implemented gameplay plants, rigs or finalized profiles.

Portraits use matching 560×720 transparent canvases and aligned pot baselines. Preserve aspect and contain them in the left image area. Runtime Spine prefabs remain independent.

## Layout handoff to Claude

Protection popup: square panel; left half plant/name/level and icon-only Swap, or large Add when empty; right half Attack Damage, Attack Cooldown, Targets, then Upgrade and water cost. Empty stats show `—`, Upgrade disabled. An occupied shelf's world button is `Protection`; an empty shelf's is `Add protection`.

Shop popup: vertical ScrollRect + RectMask2D, **no scrollbar**. Each card contains plant portrait on the left, name/stats on the right, a distinct Buy button and water cost. Buying/swap/profile logic is not included in this asset task. Swapped-out plants go into inventory. Both purchase and upgrade use clean water.

Profile rules already agreed: base Attack Damage, Attack Cooldown, integer Targets; fixed per-level increments, initial upgrade cost, per-level cost increment and max level. Cooldown improves by reducing it, floored at 20% of its base value. Swallowing plants use infinite damage; future projectile plants use finite damage. Exact prices, increments, max levels and future plant balance remain configurable; mockup numbers are illustrative.

## Import and provenance

Single Sprite, Full Rect, sRGB, alpha transparency, clamp/bilinear, no mipmaps, max size 2048. UI Android/iPhone overrides: ASTC 4×4. Portrait Android/iPhone overrides: ASTC 6×6. Default/PC compression is not overridden by this preparation.

Generated using built-in image_gen from the approved concept; exact prompts are in `AssetGeneration.prompts.txt`. Full transparent sheets are under `Source/`. `SliceAssets.py` is the repeatable source-sheet slicing recipe, using Pillow; it preserves source colors, removes detached alpha specks and near-invisible alpha ≤3, and gives sprites safe transparent gutters.

`Protection_Assets_UnityPreview.png` is an Editor-rendered contact sheet of the imported sprites, with panel/card/button slices displayed at different target sizes for visual verification of alpha and borders. It is an asset preview, not a runtime prefab or implemented popup. No gameplay scenes, plant profiles, buying, swapping or upgrade code are changed by this asset preparation.
