# Compact Inventory and treatment UI

Implements the user's self-sent reply at 16:15, 2026-11-08. Shop was approved and its prefab remains byte-for-byte unchanged (SHA256 7A55AFB5583EDA8BA7BAB297CEE9A84DF72D2F4242A72CB244030DF730C790A8).

InventoryMedicineRow and TreatmentMedicineRow are prefab variants of MedicineRow, with heights 166 and 200 instead of 242. Both retain the shared fixed 104×104 medicine frame and full name/description. Inventory removes the unused button space/footer strip. Treatment places Owned/check/Select together, retaining 60-high Select buttons and a 64-high Give button. Content has a fixed compact header/footer spacing; the narrow treatment error strip remains available for actual errors.

PopupPanelFit now accepts optional content, minimum height and content-padding height. With no content reference it retains the existing fixed maximum behavior, including Shop and other popups. Inventory and treatment instances in Home/Office reference their own list content: short lists shrink the panel; longer lists remain bounded by Safe Area and scroll. The reference and style values are Inspector-editable. The original clipboard texture is unchanged; its Sprite border is now (100,100,100,220) for the two compact sliced frames. Shop and DoctorBook retain Simple image mode, so their frames do not use these slice borders. The original border was zero.

Unity API authored the variants, updated the existing popups and saved both scenes. Recovery copies are in Assets/_Recovery/MedicineUI_Compact_20261108. Original medicine data, IDs, transactions and day-saving rules were not changed.

Play Mode checks used the isolated UsabilityValidation profile, with no end-of-day checkpoint: empty Inventory height 420; two-item Inventory height 549; ten-item Inventory bounded at 928 and scrollable; two-item treatment height 749. All active medicine names/descriptions/Owned labels fit, square dimensions remain fixed, Select/Give targets retain their sizes, and selection/checks/count/Give state update correctly. The previous full-height panel was 928 at the same screen size. Actual Game View captures are 1080×1920; stock and money are examples. Android device/touch was not tested.

Temporary authoring/test runner and logs are removed after verification; recovery copies and final handoff screenshots are retained. Unity is stopped and the isolation flag reset. Headbar v2 is a separate image-only proposal in ../HeadbarApproval_2026-11-08_v2, awaiting approval before code.
