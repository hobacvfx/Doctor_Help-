# UI v2 — approved for Unity integration

Approval received in the original self-sent email at 12:16, 2026-11-08, from hobac.vfx@gmail.com: proceed with Unity integration, no further concepts, standardize the close buttons. The approval condition below is historical and has been satisfied. Implementation and verification notes are in ../Implementation_2026-11-08/README.md.

Revises v1 after user's self-sent email at 11:41, 2026-11-08: preserve every existing field and button, including name, description, owned quantity and purchase quantity. This supersedes the v1 layout proposal. Three PNG approval demos only; no Unity changes.

## Existing UI audited

Read MedicineRow.cs, MedicineListView.cs, StoreInventoryUI.cs, SelectPillPopup.cs, PillProfile.cs and current InventoryPopup/ShopPopup/SelectPillPopup/MedicineRow/GiveButton prefabs. Read current PillProfile.asset data, which currently contains 10 remedies including Organs_Detox_Pill. Do not regenerate or change it.

- Every row: actual canonical medicine display name, description, sprite, current quantity. Existing title formats name Xquantity; demo separates name and Owned label for readability while preserving the same value.
- Inventory: scrolling owned-medicine list, X close and outside dismiss, empty-list message. No selecting, consuming or purchasing.
- Treatment: scrolling owned-medicine list, per-row Select button toggles selection with check indicator, Selected count, error/message area, Give button, X close and outside dismiss. Give disabled with zero selected items. Unowned medicines are hidden by current MedicineListView; do not introduce v1 disabled-zero rows.
- Shop: all medicine rows including zero stock, unit Price, per-row Buy X1 and Buy X10, money readout, message/error area, X close and outside dismiss. Purchase quantities are the existing 1/10 buttons. Do not replace them with steppers, basket, checkout or a global Buy.

## Data in demo

Three visible rows demonstrate a scroll viewport, not a reduction to three medicines. Use full dynamic source list at integration. Exact names/descriptions copied from current PillProfile: Bone Growth Medicine — Used to treat Trauma.; Sedative — Used to treat Delusional Disorder.; Deworming Medicine — Used for Worm Infestation and Internal Organ Disease. Unit price 1 matches current asset. Balance 500 and owned counts 4/2/3 are illustrative UI examples, not edits to player data.

## Visual QA and handoff

Reviewed all three outputs: all three full names, descriptions and owned counts visible; shop includes both purchase quantities for all rows; treatment includes Select for all rows plus checkmarks, Selected: 2, Give and status strip; all have X close and visible scrollbar. No invented cart/checkout/cancel buttons. Existing medicine sprites and real UI text must replace raster demo contents during authoring. Keep UI responsive and scrollable with fixed footer and Safe Area; support empty content, errors, long descriptions and dynamic quantities. Common frame and title placement should share a prefab when approved, while retaining current flow, purchases and end-of-day save rules.

Approval remains REQUIRED BY USER: wait for explicit email approval of v2 or further revisions before modifying Unity code, assets, scenes or prefabs. Generations used built-in image_gen with approved style references; exact prompts are in PROMPTS.md.
