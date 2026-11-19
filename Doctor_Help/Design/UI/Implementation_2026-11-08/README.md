# Medicine UI v2 — Unity implementation

Approved by hobac.vfx@gmail.com at 12:16 and repeated in a separate instruction at 14:15 on 2026-11-08. Implements Inventory, Give Medicines and Medicine Shop using the approved clipboard, burgundy title, ivory cards and teal/brass buttons. The three screens share the same nested `MedicinePopupCloseVisual.prefab`; their original Button objects and gameplay callbacks are retained.

## Editable sources

- `Assets/Resources/UI/InventoryPopup.prefab`, `ShopPopup.prefab`, `SelectPillPopup.prefab`: layout, titles, scroll viewport/scrollbar, footer, message text and shared close visual. Panel Button absorbs blank interior clicks; the existing root outside Button still dismisses the popup.
- `Assets/Resources/UI/MedicineRow.prefab`: real medicine sprite, separate name/description/Owned fields, existing per-row Select and Buy X1/Buy X10 controls, selection check. Data continues to come from PillProfile with stable IDs and the full catalogue.
- `Assets/Resources/UI/MedicinePopupCloseVisual.prefab`: one close appearance used by all three screens, including both Inventory instances in Home/Office. Edit this asset to change the common graphic/X.
- `Assets/Art/UI/MedicinePopups/MedicineChrome.png`: transparent atlas with four imported sprites (TitlePlaque, ActionButton, MedicineCard, CloseButton), authored with built-in image_gen using approved v2 and ClipboardFrame references. Exact prompt in `../Approval_2026-11-08_v2/ProductionPrompt.md`. Text is separate Unity UI; original medicine sprites and existing clipboard frame/paper are reused.
- Home and Office retain scene-owned inactive popups under Safe Area. Their existing PopupPanelFit components now use maximum 520×1100 and padding 16 for these three popup types. Police popup settings and unrelated scene content are retained.

Runtime changes are limited to optional `MedicineRow.ownedText` and `MedicineListView.balanceText`. Existing purchase, selection, Give, journal, state machine and end-of-day saving behavior remains in its existing services. Old rows without an Owned field retain their previous name/quantity display.

## Verification

Unity 6000.3.9f1 compiled the implementation. Prefabs authored/reloaded with PrefabUtility and scenes saved with EditorSceneManager; confirmed saved fit values and live scene instances. Recovery copies of the four original prefabs and both scenes are in `Assets/_Recovery/MedicineUI_20261108/`. Scene differences from those copies are limited to the relevant fit limits and obsolete title position overrides.

Edit Mode renders reviewed at 1080×1920 and 720×1600. Play Mode verification used the existing `DoctorHelp.UsabilityValidation` isolation switch and `Library/UsabilityValidation/Save/player-profile.json`, initially empty, with all changes confined to the working profile. Stopped before any end-of-day checkpoint; no player save was used or written.

- Shop renders the complete current ten-entry catalogue, including zero-stock items; X1/X10 add exactly 1/10 and deduct their costs; Owned and Money refresh; insufficient funds reports an error without changing stock.
- Inventory shows owned rows and hides Select/purchase controls. Empty inventory message, repeated opening, close and outside dismiss verified.
- Office treatment popup is configured; Give is disabled with no selection, enabled with two selections; checks and Selected count update. Give deducts one of each chosen remedy, records treatment and initiates the existing departure flow.
- Native input verified inventory opening, common X, blank interior retaining the popup, outside dismiss and dragging the shop scrollbar to the final Organs_Detox_Pill row. Mouse wheel through CUA lost Game View focus, so wheel behavior was not established by that automation attempt.

`Play_Inventory.png`, `Play_Shop.png`, `Play_Treatment.png` are actual 1080×1920 Game View captures using isolated example stock/balance. Shop screenshot intentionally shows the insufficient-funds status. Temporary preview images, migration/test runner and its metadata, command/result files and the empty test-save directory were removed after verification. The isolation SessionState flag was reset; Unity is out of Play Mode. Recovery assets and these final handoff screenshots are retained intentionally. Android device/touch and a new Android build were not tested for this UI change.
