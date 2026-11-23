# Chest medicine receiving slots

Implemented in Office on 2026-11-23 following the second imagegen mockup (smaller receiving squares). `Approval_Demo_v1.png` is the approved visual direction; `Play_Empty_3.png` and `Play_Partial_3.png` are actual Unity captures. Generation prompts remain in `Generation.prompt.txt`.

The lower clipboard retains the existing ten icons in a 5 × 2 grid. It has one close X and no Done, Give or Confirm button. The old given-medicine strip is removed. Generic empty receiving squares follow the patient's chest; each matches a medicine-grid cell's rendered size. Their existing clipboard art has wood/brass corners. Static appearance remains editable in the UI prefabs.

`TreatmentSlotRequirements` counts distinct required medicine IDs across the patient's diseases. Shared prescriptions count once. A pure police case has one slot; coexisting ordinary conditions contribute their distinct prescriptions, with a minimum of one. Healthy cases without a recipe also use one slot for an attempted prescription. The already existing cleared Haunted-only no-medicine route completes with zero slots. No required-item silhouettes or correct-answer hints appear in the slots.

Dropping on the NPC or receiving-card region stages a medicine. Duplicate types do not occupy another slot. Partial drafts retain their pictures and X buttons; only X removes a staged type. Closing/reopening retains the draft for that consultation. Completing all slots immediately submits through the existing TreatmentDecisionState → PlayerProfileService transaction, regardless of correctness. The existing thanks/Walk_Out flow follows success. Day-end evaluation determines correct/incorrect; no day checkpoint is added. Failed transactions keep their draft and X controls; remove/re-drop to retry without an automatic retry loop.

Zombie/Vampire prescription attempts complete as incorrect, including when Haunted coexists. Ordinary haunted patients still require ghost removal before a medication transaction. Clearing a Haunted-only patient and reopening treatment discards any unpaid failed draft and completes without medicine. Reporting through the telephone remains available and does not charge for staged medication.

Tutorial ends with its medicine drag. The hand points at the chest cards. The legacy Done step is removed from the plan; stable enum values remain reserved. Completion observes the committed selection after the transaction clears the temporary draft, so it cannot wait for a removed Done button.

## Configuration

- `SelectPillPopup.prefab`: compact bottom clipboard, ten-item grid, close X, scene-owned receiving tray.
- `GivenMedicineSlot.prefab`: clipboard card, medicine image, corner X. Whole-card removal is disabled.
- `GivenMedicineTray`: patient/camera references assigned in Office; world `chestOffset` and pixel `screenPadding` are Inspector-editable. Card size follows the grid and row position follows the current patient's Heart/TreasureBody.
- Runtime changes: SelectPillPopup, GivenMedicineTray, GivenMedicineSlot, TreatmentSlotRequirements, TutorialSceneView, SceneGameplayUI and the special-case guard in PlayerProfileService. Medical recipes/prices/IDs are preserved.

Pre-change scene/UI/tutorial backups are in `Logs/ChestSlots_20261123_111941`; later authoring refinement backups use the same prefix. 83 focused flow checks and 12 final pointer-hit checks passed in Unity Play Mode. Results are in Validation.txt and Validation_PointerHits.txt. No real player save is used by the preview tests. This is a local Unity scene/prefab deployment, not a device-store release.

## Verified deployment

The saved Office scene and UI prefabs were loaded in Play Mode using an isolated TutorialPreview profile. Checks covered 1/2/3/4 receiving cards, empty/occupied appearance, exact rendered cell sizing, all ten source icons, no Done object, accepted patient/card-region drops, off-patient rejection, duplicate prevention, X removal, retained drafts across close/reopen, partial treatment without payment, failed transaction recovery, a single exact charge and decision on completion, incorrect medicine evaluation, both Zombie/Vampire with Haunted coexisting, a failed Haunted-only attempt followed by ghost clearance and free completion, shared-prescription deduplication on cloned medical data, real tutorial completion after draft clearing, and the working-day save boundary. The real NPC animation root was tracked during departure and its instance was removed after the animation.

Final UI checks used real GraphicRaycaster targets and pointer-click event dispatch: clicking an empty card keeps the popup open; clicking its occupied corner X removes the staged type without closing. The 1080x1920 empty/partial captures were inspected; the clip and title do not overlap. Existing mobile texture overrides are unchanged because all artwork is reused.

Temporary setup/check scripts, their metadata and isolated preview save data were removed. Real player saves were not used. Physical Android touch, narrower-device rendering and an AAB build were not tested in this change. The Unity project is ready to run with the deployed scene/prefabs.
