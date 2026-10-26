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
Then Preparation begins with the next day and `open` is shown again. A failed save keeps the current day and allows retry via Give.
Pill buttons are colored image placeholders with text labels, require a selection, and do not consume inventory or decide patient outcomes yet.
One random NPC is instantiated under Office's `NPC_Position` using references in `Assets/Resources/NpcCatalog.asset`.
The catalogue references the six existing prefabs in `Assets/2D_Assets/NPC`; original assets stay in place and are included in builds through these references.
Each scene contains its own `Gameplay Canvas` and `EventSystem`. The Canvas contains `Safe Area`, day/step and error labels, FPS text, and prefab instances of that scene's buttons/popups.
Home owns OpenButton and ResultsPopup; Office owns GivePillsButton and SelectPillPopup.
UI exists in the scene hierarchy before Play and is destroyed naturally when its scene unloads. No UI is instantiated or parented under `DontDestroyOnLoad`.
The persistent Game Services object holds only profile, flow and performance settings; the flow binds scene UI references after scene loading.

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

## Validation

Run **Doctor Help > Validate Initial Setup** in Unity to check settings, NPC references, defaults, JSON round trip, backup recovery, invalid stock, duplicate IDs, preservation of corrupt/newer saves, and daily checkpoint semantics.
Tests write isolated data under `Library/ProfileValidation`, never to the player's real save directory.
Run **Doctor Help > Validate Scene UI** to inspect both scenes' Canvas, nested prefab references, FPS and EventSystem in preview scenes without changing currently open scenes.
On a phone, verify touch buttons, NPC visibility, portrait lock and FPS placement. Close during Office: reopening must repeat that day from Home; close during/after results: reopening must start the next day from Home.
