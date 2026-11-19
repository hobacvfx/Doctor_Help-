# UI demo v1 — awaiting email approval

Source: self-sent To chat gpt email from hobac.vfx@gmail.com, 2026-11-08 11:17 Asia/Saigon. Request: complete Inventory, Give Medicine and Shop UI designs matching art style; email demos for approval before any Unity updates.

These PNGs are visual approval mockups, not production sprite sheets or implemented screens. Generated with built-in image_gen using Design/Art/STYLE_GUIDE.md, Medicine_Collection.png, VictorianRedraw/Collection.png and ClipboardFrame.png references. Text/numbers in the PNGs are demo copy; final text, icons, frames and states must be separated if approved. All stock counts, balance and shop prices are illustrative, not balance changes. Existing canonical medicine IDs/names remain authoritative; short bottle labels are presentation only.

## Screens

- 01 Inventory: 3-column medicine grid, quantity badges, unavailable stock treatment, selected item detail, Close. Read-only stock inspection, no medicine consumption.
- 02 Give Medicine: current patient, multiselect medicine grid, unavailable item disabled, selected tray, Cancel/Give. One unit per selected medicine on confirmation; no automatic diagnosis or change to the existing treatment evaluator.
- 03 Shop: scrolling catalog, owned count, unit price, quantity steppers, total and remaining balance, Cancel/Buy. Keep purchasing within Home Preparation and current profile transaction/save rules. This UI proposal does not change debt/purchase rules.

## States to author after approval

- Inventory: normal, selected, zero stock, empty list and scroll overflow.
- Give: normal, selected, zero stock, no selection (Give disabled), submitting, validation error and retry without duplicate consumption. Close/Cancel returns to the same consultation.
- Shop: quantity zero, positive quantity, submitting, existing purchase-rule rejection, failure and retry without duplicate purchase. Honor existing affordability/debt rules rather than inventing them in art.
- Shared: pressed/disabled buttons, readable stock badges, Safe Area, dynamic text wrapping, scrolling under fixed action footer, preserve reusable button/nested-prefab links.

## Review notes

Large near-black pencil contours, material hatching, aged ivory paper, dark wood, old brass, burgundy titles and muted teal selection/primary actions. The three demos share a clipboard vocabulary. Shop header placement differs slightly and should be standardized during final authoring if approved. Tiny AI-rendered bottle text is preview-only and will be replaced by existing medicine sprites and separate UI typography. No code, scene, prefab, live UI, medicine data or save file is modified by this demo package.

Approval gate: wait for explicit approval email identifying this v1 (or requesting revisions). Do not integrate into Unity before approval. Existing gameplay-video request remains pending separately.
