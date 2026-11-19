# Medicine UI touch revision

Later review at 16:15 approved Shop and requested compact Inventory/treatment. Their latest implementation is documented in ../CompactRevision_2026-11-08; this document records the preceding large-button revision. Shop keeps this layout.

Implements the first part of the 15:25 email from hobac.vfx@gmail.com, 2026-11-08. Head bar remains an image-only proposal in ../HeadbarApproval_2026-11-08.

Updated the existing MedicineRow prefab to use 242-high cards, aligned 60-high Buy X1/Buy X10 and Select buttons, 24pt button captions with 14 horizontal / 7 vertical padding. Name, description and Owned share a left edge; Price aligns under the icon frame; purchase buttons share a baseline and equal widths. The treatment Give button and Selected count have a larger aligned footer, and the common X target/visual is 64×64.

Every medicine uses the same nested `Assets/Resources/UI/MedicineIconFrame.prefab` at exactly 104×104 logical UI units. The existing medicine Image is a child with equal inset and Preserve Aspect, so the original sprites are not distorted or reimported. The square border reuses the existing MedicineCard sprite. Medicine data, IDs and runtime transaction logic are unchanged. Static layout/text styling remains editable in prefabs, not runtime code.

Saved/reloaded with Unity Editor API; original revised prefabs backed up to `Assets/_Recovery/MedicineUI_Touch_20261108/`. Validated all current catalogue rows in Play Mode: fixed frame dimensions and shared source prefab, full medicine name/description, button height, caption width/height within its padded rect, X size, X1/X10 transactions and live updates. Inventory and treatment modes display correct controls, checks/count and Give enabled state. Native mouse input toggled Select (count 2 → 1 → 2) and confirmed Give, closing the popup and starting the existing departure step.

Screenshots are actual 1080×1920 Game View captures using the isolated UsabilityValidation save directory and example stock. Play was stopped before the end-of-day checkpoint; the isolation flag was reset. No real player save was changed. Temporary authoring/test files are removed after checks; recovery backups and these handoff images are retained. Android device/touch was not tested. The earlier one-off runtime prefab-connection assertion was corrected to inspect the saved nested source; runtime clones do not carry the same Editor prefab linkage.
