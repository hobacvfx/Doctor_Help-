# Initial setup

- Unity: 6000.3.9f1; Android application ID: `com.doctorhelp.game`.
- Portrait only. Existing scenes, rendering assets and gameplay are preserved.
- Services start automatically before the first scene and persist across scenes.
- FPS overlay: top left inside the safe area, FPS and average frame time updated every 0.5 seconds, including release builds.
- Performance test setting: VSync off, target 200 FPS, render every frame. Mobile refresh rate and device performance still limit actual FPS. Revisit this test setting before release.

## Player profile

Automatically loads at startup. Changes during a day exist only in memory.
Only `CompleteDay()` saves a checkpoint, including all profile values and the next day's number.
Pausing, losing focus, stopping Play mode and quitting do not save an unfinished day.
Restarting always enters Home in Preparation at the checkpoint's day (day 1 for a new profile).
Stored at `Application.persistentDataPath/player-profile.json`, with `.bak` recovery and `.tmp` staging files.
Defaults: money 0, day 1, patients killed/saved 0, empty medicine inventory. Unknown medicine IDs have quantity 0.
No medicine catalogue or starting economy has been invented. IDs are case-sensitive stable identifiers, independent of display names.
Money may be negative to support debt; quantities and patient counts cannot be negative.
If both saves are corrupt or the primary uses a newer schema, loading fails visibly in the Console and existing files are preserved.

Use `DoctorHelp.PlayerProfileService.Instance` after bootstrap (for scene components, in `Start`):

```csharp
var profile = DoctorHelp.PlayerProfileService.Instance;
if (!profile.IsLoaded) return;
profile.AddMoney(100); // Negative amounts allowed for expenses/debt.
profile.AddMedicine("medicine_id", 3); // Replace with a real catalogue ID.
bool consumed = profile.TryConsumeMedicine("medicine_id");
int owned = profile.GetMedicineQuantity("medicine_id");
profile.RecordPatientSaved();
profile.RecordPatientKilled();
var snapshot = profile.GetSnapshot(); // Independent copy for UI/read access.
// At the end of the day only (called by DailyGameLoop):
bool saved = profile.CompleteDay(); // Advances the day only if saving succeeds.
```

`Changed` notifies UI after in-memory mutations and successful day completion.
There are no automatic gameplay rewards, patient outcomes or bankruptcy rules.

## Placeholder daily loop

Editor Play mode starts Home regardless of the currently edited scene. Home is also first in the build list.
Home Preparation: `open` -> Office Examination -> `Give Pills` -> `Select Pill` popup.
Select `pill_a`, `pill_b` or `pill_c`, then press `Give` to finish the day.
The profile is saved for the next morning, Home loads, and displays `chạy cut scene ở đây và xem kết quả ở đây` for 3 real-time seconds.
Then Preparation begins with the next day and `open` is shown again. A failed transition displays an error and a `Thử lại` action using the scene's primary button; its authored label is restored after recovery.
Pill buttons are colored image placeholders with text labels, require a selection, and do not consume inventory or decide patient outcomes yet.
One random NPC is instantiated by the `PatientSpawnPoint` component on Office's `NPC_Position`, using references in `Assets/Resources/NpcCatalog.asset`. Renaming the object does not break spawning. Re-entering examination in the same scene reuses the existing patient.
The catalogue references the six existing prefabs in `Assets/2D_Assets/NPC`; original assets stay in place and are included in builds through these references.
Each scene contains its own `Gameplay Canvas` and `EventSystem`. The Canvas contains `Safe Area`, day/step and error labels, FPS text, and prefab instances of that scene's buttons/popups.
Home owns OpenButton and ResultsPopup; Office owns GivePillsButton and SelectPillPopup.
UI exists in the scene hierarchy before Play and is destroyed naturally when its scene unloads. No UI is instantiated or parented under `DontDestroyOnLoad`.
The persistent Game Services object holds only profile, flow and performance settings; the flow binds scene UI references after scene loading.

## Flow architecture and extending steps

`DailyGameLoop` is the composition root. It declares the ordered `DayFlowPlan`, registers the states and services, and forwards advance/retry requests.
`DayFlowMachine` is pure C# and is the only owner of state transitions. States implement `IDayFlowState.EnterAsync` and `Exit`; Exit always removes subscriptions and hides the previous view before entry starts.
The current order is `Preparation -> Examination -> TreatmentDecision -> DayResults -> Preparation`. Enum values are stable identifiers, not sequence indexes.
`UnityDaySceneService` loads only when the destination differs, checks the build list and resolves exactly one configured UI from the destination scene. It does not subscribe to unrelated scene-load events.
`PatientService` resolves the scene-owned spawn-point component. UI exposes gameplay intent events; it never loads scenes or writes saves.
`DayCompletionService` makes a successful checkpoint idempotent for the closing day, including retries after a subsequent scene-load failure. The save still occurs before results presentation. Future nightly penalties/rewards belong before this checkpoint and must also be applied only once per closing day.

To add a step (for example Diagnosis):
1. Add a named `DayStep` identifier with an unused explicit value.
2. Implement its state with the required services; subscribe to UI intents on Enter and unsubscribe on Exit.
3. Register the state and insert its identifier into the plan in `DailyGameLoop.Start`.
4. Add its scene-owned UI if needed, and test order, retry and cleanup.

To remove an optional step, remove it from the plan and remove its registration if no longer needed. Other states do not encode their successor. Preparation and DayResults remain mandatory boundaries to protect startup and checkpoint rules.
Adding a branch, such as reporting a patient to police, can initially stay inside TreatmentDecision. If it becomes a distinct stage, explicitly extend the flow model with validated branches rather than bypassing the state machine.

Results finish through `ResultsCompleted`. The current scene view uses a configurable 3-second placeholder timer. For real cutscenes/results UI, disable `Use Placeholder Results Timer` on the scene Canvas and call `CompleteResultsPresentation()` from the presentation completion/confirm action.
Failures leave the machine unlocked in a fault state. Retry re-enters the failed step and never re-exits an already cleaned previous state. Destroying the loop cancels its lifetime and detaches subscriptions; Unity scene loads already in progress finish before cancellation is observed.
Check scene configuration errors in the Console if no valid scene view exists to display a retry action.

### Editing UI prefabs

Open prefabs in `Assets/Resources/UI` in Unity Prefab Mode:
- `OpenButton.prefab` and `GivePillsButton.prefab`: button style, label and bottom-of-screen position.
- `GiveButton.prefab`: shared Give button style and label.
- `PillOptionButton.prefab`: shared pill button style, image and label layout.
- `SelectPillPopup.prefab`: backdrop, panel, title, selected-pill label, save error and pill arrangement. Give and pill buttons are nested prefab instances; their common style comes from the button prefabs. Each pill's label and horizontal position are local overrides.
- `ResultsPopup.prefab`: result backdrop and placeholder message.

The loop preserves prefab layout, colors, sprites and static labels. Code attaches click handlers, enables Give after selection, updates selected-pill text and shows save errors. The `SelectPillPopup` component stores button/text references and medicine IDs; keep these references assigned when replacing child objects. Keep the resource prefab names stable.
Canvas, safe-area container, status/error labels and FPS can also be edited directly in Home and Office. `SceneGameplayUI` holds Inspector references and updates safe-area anchors during play. Status and error text content is updated by code.
Keep the `SceneGameplayUI` references assigned when replacing scene objects. Popups start inactive; enable temporarily in Edit mode to preview their layout. Edit shared styling in Prefab Mode or apply scene instance overrides to the prefab.

## Medical data profiles

Three editable ScriptableObject assets live under `Assets/Data/Medical`:
- `SymptomProfile.asset`: add/remove symptoms and edit their names.
- `PillProfile.asset`: add/remove pill types and edit their names.
- `AllSickness.asset`: references the two source profiles and lists diseases, each with a name, symptom slots and required-pill slots.

Enter the source symptoms/pills first. Then add a disease in AllSickness and select existing source entries from its dropdown slots. `+` adds an entry/slot and `−` removes it. Asset changes use Unity serialization/Undo.
IDs are generated automatically and are read-only in the Inspector. Renaming or reordering entries does not change their IDs or disease references. Duplicating a serialized entry receives a new ID on validation.
Deleting a source item used by a disease asks for confirmation and leaves a visible missing-reference slot in AllSickness; it never silently redirects that reference to a different entry. Select a replacement or remove the obsolete slot.

All pills listed for a disease are required together. Each ID is a distinct required pill type, not a quantity/dosage. Empty, duplicate, unresolved or incomplete recipes do not satisfy `AllSickness.HasRequiredPills`. Providing additional pills does not make this data query reject an otherwise complete required set; side effects/incorrect extra medication are not defined yet.
The assets contain the user-approved diagram dataset: 12 diseases, 15 shared symptoms and 9 remedies. See `Assets/Data/Medical/README.md` for the imported mappings.
Each disease has a treatment mode: RequiredPills or ReportToPolice. Zombie Infection and Vampire Infection require reporting to police and have no pill recipe. `HasRequiredPills` always returns false for police cases.
The data is not yet wired into patient diagnosis, inventory consumption or the single-pill gameplay popup. That integration must support selecting the entire required set and handling police cases separately.
The default data assets are outside Resources; future gameplay consumers must hold explicit serialized references or load them through an intentional asset-loading system.

## Validation

Run **Doctor Help > Validate Initial Setup** in Unity to check settings, NPC references, defaults, JSON round trip, backup recovery, invalid stock, duplicate IDs, preservation of corrupt/newer saves, daily checkpoint semantics, and state-machine order/lifecycle/retry/cancellation.
UI regression checks also load all six prefabs through Unity, verify real Button/GameObject/Graphic references, usable fonts and text bounds, toggle interactability, select every pill, confirm Give, and reset/reopen the popup to detect duplicate callbacks.
Tests write isolated data under `Library/ProfileValidation`, never to the player's real save directory.
Run **Doctor Help > Validate Scene UI** to inspect both scenes' Canvas, nested prefab references, FPS and EventSystem in preview scenes without changing currently open scenes.
On a phone, verify touch buttons, NPC visibility, portrait lock and FPS placement. Close during Office: reopening must repeat that day from Home; close during/after results: reopening must start the next day from Home.
