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
Defaults: money 500, day 1, patients killed/saved 0, empty medicine inventory. Unknown medicine IDs have quantity 0.
PlayerStartingProfile in Assets/Resources configures starting money (500) for new saves and references the existing PillProfile. Existing checkpoints retain their money. During Play, select Game Services > PlayerProfileService and edit Player Money to test the working day. IDs are case-sensitive stable identifiers, independent of display names.
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

## Daily loop

Editor Play mode starts Home regardless of the currently edited scene. Home is also first in the build list.
Home Preparation: `open` -> Office Examination -> `Give Pills` -> `Select Pill` popup.
Select one or more owned medicines, then press `Give` to consume one of each and finish that consultation. Click outside the panel or Close to cancel without consuming; the flow returns to Examination through the return branch declared in DayFlowPlan. The patient plays A_Walk_Out completely before removal. If the clock is still before 21:00, the next patient enters with A_Walk_in; otherwise the day ends. A patient already admitted keeps their consultation even if closing occurs during their arrival.
TreatmentEvaluator compares the day history before its checkpoint; Home then plays Success.mp4 if every case is correct, or Fail.mp4 if any is wrong/incomplete. The selected result survives scene/video retries after the checkpoint.
Then Preparation begins with the next day and `open` is shown again. A failed transition displays an error and a `Thử lại` action using the scene's primary button; its authored label is restored after recovery.
MedicineRow and MedicineListView provide the shared catalogue, stock display and selection controls for shop, inventory and treatment. Give consumes the whole selection atomically and records its audit before departure; patient outcomes remain unimplemented.
One random NPC at a time is instantiated by PatientSpawnPoint on NPC_Position, using Assets/Resources/NpcCatalog.asset. Re-entering a failed examination reuses the existing consultation. After a completed departure, the next admission spawns a fresh patient consultation.
The catalogue references the NPC prefabs currently registered in `Assets/2D_Assets/NPC`; original assets stay in place and are included in builds through these references.
Each scene contains its own `Gameplay Canvas` and `EventSystem`. The Canvas contains `Safe Area`, day/step and error labels, FPS text, and prefab instances of that scene's buttons/popups.
Home owns OpenButton and ResultsPopup; Office owns GivePillsButton, SelectPillPopup and the ClinicClock prefab instance at the top right of Safe Area.
UI exists in the scene hierarchy before Play and is destroyed naturally when its scene unloads. No UI is instantiated or parented under `DontDestroyOnLoad`.
The persistent Game Services object holds only profile, flow and performance settings; the flow binds scene UI references after scene loading.

## Worm flipbooks

`Giun_1`, `Giun_2` and `Giun_3` prefabs in `Assets/2D_Assets/TrieuTrung/Giun` automatically play their default looping Animator state when active. Each clip changes only the root SpriteRenderer sprite. Prefab transforms, material, layers and sorting are preserved. Nine sprites are sliced in row order from each transparent 3x3 atlas under `Flipbook`, with a shared centered pivot and the original sprite's world size. Giun_1 uses `Giun_1_Crawl_v3_Atlas`; Giun_2 and Giun_3 use their `Wriggle_Atlas` images. Timing is approximately 130 ms per frame. Edit the `.anim` and `.controller` assets directly in Unity; no runtime setup component is required.

Giun_2 wriggles in place. Giun_3 alternates its two legs, flexes its tail and gently sways around a registered abdomen center. Source PNGs are preserved. GIF previews and image-generation prompts are stored beside the atlases. Editor verification sampled all nine rendered frames, advanced each Animator through three cycles and checked preserved prefab settings; this is not an Android device test.

## Clinic opening hours and patient motion

ClinicClock maps 240 real seconds (4 minutes) to 08:00–21:00, displays HH:mm and clamps at 21:00. DailyGameLoop starts it on Open, updates it with unscaled foreground time, and skips the resume frame so time spent in the background is not added. It runs during the medicine popup and all patient animations. There is no unfinished-day clock persistence. Opening the following day starts at 08:00 again.

The Office Give Pills desk button stays visible during arrival, departure and popups. It accepts input only during Examination (or as the flow retry action). Touch X-ray scanning offsets both magnifier and cutout together above the finger by 240 reference pixels at a 1920px screen height; edit Touch Magnifier Offset on the scene's ExaminationToolSelection. Mouse scanning keeps its original alignment.

PatientDepartureState waits for the animator's actual normalized completion, then removes the old patient before the plan checks whether another may enter. ExaminationState waits for NPC_Idle before enabling tools/treatment. Office stays loaded between patients. PatientSpawnPoint initializes each newly spawned consultation, preserving the existing temperature cache for its duration. Failed entry retry reuses the current instance; completed departure retry is idempotent. The shared All.controller uses its existing A_Walk_in -> NPC_Idle and a Leave trigger for NPC_Idle -> A_Walk_Out. Arrival and departure clips are nonlooping. Missing animator/controller states produce a flow error.

## Flow architecture and extending steps

`DailyGameLoop` is the composition root. It declares the ordered `DayFlowPlan`, registers the states and services, and forwards advance/retry requests.
`DayFlowMachine` is pure C# and is the only owner of state transitions. States implement `IDayFlowState.EnterAsync` and `Exit`; Exit always removes subscriptions and hides the previous view before entry starts.
The current order is `Preparation -> Examination -> TreatmentDecision -> PatientDeparture`, then the plan repeats Examination while the clinic is open, otherwise continues `DayResults -> Preparation`. Enum values are stable identifiers, not sequence indexes.
`UnityDaySceneService` loads only when the destination differs, checks the build list and resolves exactly one configured UI from the destination scene. It does not subscribe to unrelated scene-load events.
`PatientService` resolves the scene-owned spawn-point component. UI exposes gameplay intent events; it never loads scenes or writes saves.
`DayCompletionService` makes a successful checkpoint idempotent for the closing day, including retries after a subsequent scene-load failure. The save still occurs before results presentation. Future nightly penalties/rewards belong before this checkpoint and must also be applied only once per closing day.

To add a step (for example Diagnosis):
1. Add a named `DayStep` identifier with an unused explicit value.
2. Implement its state with the required services; subscribe to UI intents on Enter and unsubscribe on Exit.
3. Register the state and insert its identifier into the plan in `DailyGameLoop.Start`.
4. Add its scene-owned UI if needed, and test order, retry and cleanup.

To remove an optional step, remove it from the plan and remove its registration if no longer needed. Other states do not encode their successor. Preparation and DayResults remain mandatory boundaries to protect startup and checkpoint rules.
Medication and police reporting are separate decisions inside TreatmentDecision. If it becomes a distinct stage, explicitly extend the flow model with validated branches rather than bypassing the state machine.

DayResultsState awaits SceneGameplayUI.PresentResultsAsync. ResultsVideoPresenter completes through VideoPlayer.loopPointReached, then DailyGameLoop asks the state machine to advance to Preparation. Preparation never uses a fixed results timer. Errors/cancellation propagate into the existing transition/retry flow.
Failures leave the machine unlocked in a fault state. Retry re-enters the failed step and never re-exits an already cleaned previous state. Destroying the loop cancels its lifetime and detaches subscriptions; Unity scene loads already in progress finish before cancellation is observed.
Check scene configuration errors in the Console if no valid scene view exists to display a retry action.

### Editing UI prefabs

Open prefabs in `Assets/Resources/UI` in Unity Prefab Mode:
- `OpenButton.prefab` and `GivePillsButton.prefab`: button style, label and bottom-of-screen position.
- `GiveButton.prefab`: shared Give button style and label.
- `MedicineRow.prefab`: shared medicine icon, name/quantity, description, price and purchase/selection controls.
- `SelectPillPopup.prefab`: backdrop, panel, scroll viewport, selected count, error label and nested Give button; medicine rows are instantiated from MedicineRow within the scene popup.
- `ResultsPopup.prefab`: black backdrop, VideoPlayer, RawImage and result video presenter; both point to ResultsVideo.renderTexture.

The loop preserves prefab layout, colors, sprites and static labels. Code attaches click handlers, enables Give after selection, updates selected-pill text and shows save errors. The `SelectPillPopup` component stores button/text references and its shared MedicineListView; keep these references assigned when replacing child objects. Keep the resource prefab names stable.
Canvas, safe-area container, status/error labels and FPS can also be edited directly in Home and Office. `SceneGameplayUI` holds Inspector references and updates safe-area anchors during play. Status and error text content is updated by code.
Keep the `SceneGameplayUI` references assigned when replacing scene objects. Popups start inactive; enable temporarily in Edit mode to preview their layout. Edit shared styling in Prefab Mode or apply scene instance overrides to the prefab.

## Medical data profiles

Three editable ScriptableObject assets live under `Assets/Data/Medical`:
- `SymptomProfile.asset`: add/remove symptoms and edit their names.
- `PillProfile.asset`: add/remove pill types and edit name, unit price, separate description and sprite. Prices currently default to 1. Removing a pill preserves its stable ID stock in old saves and shows missing disease references; it never transfers stock to another item.
- `AllSickness.asset`: references the two source profiles and lists diseases, each with a name, symptom slots and required-pill slots.

Enter the source symptoms/pills first. Then add a disease in AllSickness and select existing source entries from its dropdown slots. `+` adds an entry/slot and `−` removes it. Asset changes use Unity serialization/Undo.
IDs are generated automatically and are read-only in the Inspector. Renaming or reordering entries does not change their IDs or disease references. Duplicating a serialized entry receives a new ID on validation.
Deleting a source item used by a disease asks for confirmation and leaves a visible missing-reference slot in AllSickness; it never silently redirects that reference to a different entry. Select a replacement or remove the obsolete slot.

All pills listed for a disease are required together. Each ID is a distinct required pill type, not a quantity/dosage. Empty, duplicate, unresolved or incomplete recipes do not satisfy `AllSickness.HasRequiredPills`. Providing additional pills does not make this data query reject an otherwise complete required set; side effects/incorrect extra medication are not defined yet.
The assets contain the user-approved diagram dataset: 12 diseases, 15 shared symptoms and 9 remedies. See `Assets/Data/Medical/README.md` for the imported mappings.
Each disease has a treatment mode: RequiredPills or ReportToPolice. Zombie Infection and Vampire Infection require reporting to police and have no pill recipe. `HasRequiredPills` always returns false for police cases.
The shared inventory/treatment popup selects multiple pill IDs and consumes one unit of each only after Give. It does not evaluate cure correctness, patient outcomes or rewards. Police reporting is a separate decision in the same TreatmentDecision step. AllSickness.HasRequiredPills remains a single-disease data query, not the treatment transaction.
The default data assets are outside Resources; future gameplay consumers must hold explicit serialized references or load them through an intentional asset-loading system.

## NPC profiles

Select `Assets/Data/NPC/NpcProfile.asset` in the Project window. It holds all authored NPC records and references `Assets/Data/Medical/AllSickness.asset`.

- **Refresh / cập nhật NPC** scans the assigned NPC Folder and all subfolders for prefab assets. The default folder is `Assets/2D_Assets/NPC`. New entries use the prefab name and no sickness. Loading again adds only new prefab GUIDs and preserves authored names and sickness IDs. The folder should contain complete NPC prefabs, not individual appearance parts.
- Search by NPC name or prefab name; the Inspector displays 30 matching slots per page. Click a slot to view its front-facing 2D prefab preview and edit its NPC name, up to two compatible sicknesses (or none), and optional NpcDialogueAsset. **Mở prefab NPC** opens the source in Unity Prefab Mode.
- NPC IDs use prefab asset GUIDs; sickness references use existing stable disease IDs. Renaming/moving prefabs or renaming diseases preserves the links. Keep `.meta` files when moving assets outside Unity. Names, sickness assignments and optional dialogue assets are the editable NPC gameplay data; stable IDs and prefab references remain technical links. Zombie Infection and Vampire Infection are ordinary sickness records with their existing names and IDs.
- Deleted/missing prefabs remain listed with a warning on normal Refresh. The explicit **Xóa bản ghi mất prefab + cập nhật spawn** button removes only missing-prefab records; it never deletes prefab assets. Missing sickness IDs also remain visible until explicitly replaced. Changing the scan folder does not remove previous entries. Inspector changes and loading support Undo.
- No NPCs or medical data are generated at runtime by this profile. The examination spawner still selects prefabs from `Assets/Resources/NpcCatalog.asset`, then resolves the exact prefab reference in its assigned NpcProfile and binds that record to the spawned patient. Stethoscope rhythm and temperature read this authored sickness and its symptoms. Diagnosis, treatment and spawn selection rules remain separate gameplay work.

## Bulk NPC authoring tool

Open **Tools > NPC > Tạo NPC hàng loạt**. Defaults: NPC_0 template, current NpcProfile and NpcCatalog, first seed 6, count 10. Set the count before pressing Tạo NPC. Outputs are NPC_<seed>.prefab with ascending Seed and Confirmed Seed, saved into the profile's NPC Folder. The next successful batch automatically advances the window's first seed by its count.

The template hierarchy, Heart reference and animation controller are retained; appearance is applied through NpcAppearanceProfile with nested part prefab links. By default, template Lock Override flags are cleared so each slot uses the new seed. Enable Giữ Lock Override to retain intentionally locked parts. Seeds determine appearance, not uniqueness: different seeds can produce matching looks when the available part combinations overlap.

Each generated prefab is registered immediately in NpcProfile using its new asset GUID, its NPC_<seed> name and one random existing AllSickness ID, with a configurable 30% chance of a compatible second ID. Only new records receive random diseases; existing NPC names, diseases and IDs are preserved. Dialogue content uses NpcDialogueAsset; Editor checks verified multiline Vietnamese serialization, profile references, refresh preservation and empty dialogue references on newly generated NPCs. Legacy assigned .txt references were checked and converted before the field changed; original text source files are preserved.

The selected NpcCatalog also receives the new prefabs so they can spawn; leave that field empty for authoring without spawn registration. The NPC Profile must reference a nonempty valid disease catalogue.

The tool prechecks the entire requested name range and refuses existing prefab paths; it never overwrites NPCs. Canceling or a failure during generation removes assets created by that batch and restores profile/catalog data. No real NPCs are generated just by opening the tool. The editor window/service are permanent authoring tools, not temporary test menus.

NPC refresh controls are available in NpcProfile Inspector, NpcCatalog Inspector and the bulk-generation window. Refresh discovers current prefabs by GUID, preserves authored data for existing records, and rebuilds the selected spawn catalogue from all live profile prefab references (removing null/duplicate entries). Select the intended profile/catalog before refreshing; the catalogue is synchronized to the full profile, not a manually selected subset. Use the explicit missing-record cleanup after intentionally deleting outdated NPC prefabs. Prefabs recreated with the same filename get new GUIDs and new records; no identity or sickness is transferred by name. The generator checks OnlyExistingAssets so Unity's recently-deleted GUID cache cannot block reusing a deleted filename. New bulk-generated NPCs still receive random diseases; a plain folder import defaults newly discovered entries to no sickness.

## Verification

Temporary Editor validation menus and their test harnesses have been removed after implementation checks. The production custom Inspectors, NPC importer and Home Play Mode startup remain. Future verification must use isolated player data; never run save checks against the real player profile.

On a phone, verify touch buttons, NPC visibility, portrait lock, FPS/clock placement and background/resume. Close during Office: reopening must repeat that day from Home; close after the final checkpoint: reopening starts the next day from Home.

## Examination tools and X-ray

Office's `Desk` owns `ExaminationToolSelection`, with explicit references to the camera, the separate pointer magnifier and its selectable tools. Each desk tool has `ExaminationTool`, a sprite-shaped `PolygonCollider2D`, its existing `Selected_Icon`, and `ToolSelectionGlow`. Select nothing on entry. Click a tool to select it, click it again to deselect, or click another to switch. Selection is available only during Examination and clears on leaving that step.

Xray and Stethoscope currently start mechanics. After selecting `Desk/KinhLup`, start a new mouse hold/touch outside UI and drag to scan. The selecting gesture itself never scans. Releasing stops the scan but keeps the selected tool. UI gestures and other tools cannot activate X-ray. Cloth/hair see-through controllers read the scene selection service and use its explicit pointer magnifier; they no longer search for an ambiguous object named KinhLup.

On each tool's `Tool Selection Glow` component, adjust Thickness (source texture pixels), Brightness, Pulse Amount and Pulse Period (seconds). Defaults are a soft white border with a subtle 2.5-second pulse. The existing icon's position/style remains authored in the scene. The glow quad is scene-owned; only its mesh is generated at runtime.

To add a future tool, configure its `ExaminationTool` kind, icon, collider and glow references, then add it to the Desk selection component's Tools array. New mechanics should read the selected kind and respect gesture/UI ownership rather than polling raw pointer state independently.

## Stethoscope and heartbeat

`Desk/HeartbeatExamination` owns the scene-authored HeartCheck view, its `VFX_HeartBeat` feedback, `VFX_Sound` signal, and the AudioSource playing `SFX_HeartBeat.mp3`. Selecting TaiNghe activates HeartCheck at its authored position; deselection, switching tools and leaving Examination hide it. Selecting alone does not emit particles or audio. A subsequent hold outside UI starts listening immediately; release stops and clears both particle systems and audio. Pointer ownership/UI blocking are shared with X-ray through ExaminationToolSelection.

The PatientSpawnPoint has an explicit reference to NpcProfile. Each NPC prefab has PatientExaminationProfile with an explicit Heart transform; the spawned instance receives the selected prefab's stable NPC record. Runtime code does not discover a Heart by name or guess a profile from the spawned object's name. Adding an NPC requires its profile record and assigned examination Heart anchor, as well as the spawn catalogue entry.

At each new listening gesture, HeartbeatRhythm resolves the patient's authored sickness through AllSickness and stable symptom IDs. Normal is 60 BPM, Rapid Heartbeat triples it (180 BPM at the default baseline), Slow Heartbeat halves it. No Heartbeat means zero biological signal: the feedback heart is materialized once at No Heartbeat Preview Time (default 0.1 seconds) and its particle simulation is paused, leaving a static heart while holding. It never repeats or emits pulse events, including outside range. Release clears the static heart; a new hold displays it again. Missing disease/symptom links and conflicting fast/slow symptoms report errors rather than silently becoming normal. No Heartbeat takes precedence if explicitly combined with other heartbeat symptoms. There are no identity/classification overrides: Vampire Infection currently has No Heartbeat, so it resolves to zero through the same symptom rules as every other illness.

For patients with a heartbeat, each pulse restarts the feedback VFX. For an audible biological signal it also restarts VFX_Sound and the prepared clip together. The 0.34-second clip keeps its authored pitch; rapid pulses change the trigger interval, not audio playback speed. The invisible container particle system is not played. Play On Awake is disabled so selecting/opening Office cannot accidentally emit a pulse.

Distance is measured in world XY from pointer to the current patient's Heart, projected through the scene camera onto the heart's depth plane. Signal gain is `Clamp01(1 - distance / Max Distance)`, with Max Distance default 4. Gain multiplies the authored VFX_Sound Start Size and Max Volume, including live particles already emitted. At/beyond the limit or with No Heartbeat, sound particles are cleared and audio stops. VFX_HeartBeat keeps its authored size and is never attenuated by distance. Particle renderers use Order in Layer 200 to keep the feedback visible above patient sprites. None of these settings move HeartCheck.

The HeartbeatExamination Inspector exposes Max Distance, Max Volume, Normal BPM and symptom dropdowns. Visual shape, material, lifetime and maximum Start Size remain authored in the original particle systems. Editing a patient's sickness or rhythm during Play takes effect on the next listening gesture; thermometer values remain fixed for the current consultation.

GivePillsButton uses its authored Collider2D through Collider2DButtonHitArea (uGUI raycast filter). Adjust the collider's Size/Offset in the prefab or scene to change the clickable area; leave the image layout independent. Hits outside the collider are rejected by GraphicRaycaster, including examination UI-blocking queries. The collider should stay inside the UI RectTransform. Collider transform synchronization accounts for CanvasScaler/layout changes. 

## Bone fracture appearance

NPC_0 includes BoneFractureAppearance on its root, with an explicit reference to the Bone renderer and the stable Bone Fracture symptom ID. New prefabs from the batch generator retain this configuration. PatientExaminationProfile.Initialize applies it from the consultation's assigned sickness; diseases share the same symptom mapping (for example Trauma and Brittle Bone Disease).

For fracture, _BoneCrack is 1 and _Crack_Offset receives two independent random floats in 0–1 once per consultation. Otherwise the boolean and offset are reset to zero. Values persist through disabling/re-enabling and X-ray holds; a newly initialized consultation resamples. Per-renderer MaterialPropertyBlock overrides preserve the shared material asset and other properties used by X-ray. The material asset's Inspector does not show these runtime per-renderer overrides. Missing shader properties, renderer, sickness or symptom mapping report a configuration error. NPC prefabs without this optional appearance component keep their existing behavior.

BoneFractureAppearance's Inspector exposes the renderer and a symptom dropdown. The bone's art, material and transform stay authored in the prefab. Edit Mode checks exercised disease mapping, range, offset persistence, healthy reset, per-patient isolation and shared asset preservation; GPU preview renders confirmed the fracture/healthy difference. This has not yet been tested on Android.

## Worm infestation appearance

WormInfestationAppearance on NPC_0 references the three authored Giun_1/Giun_2/Giun_3 prefabs and six direct Pos_* anchors below Giun. On PatientExaminationProfile.Initialize, exactly one randomly selected anchor receives one randomly selected prefab if the assigned disease has the stable Worm Infestation symptom ID. Both Worm Infestation and Zombie Infection currently share that symptom. Healthy patients receive none. No disease-name matching or runtime scene-name search is used.

Instances are children of the anchors with localPosition zero. The prefab's layers, sorting, materials, rotation and scale remain intact. Each consultation samples once; tool toggles and enabling/disabling do not respawn or reroll worms. Reinitialization removes only instances tracked by this component before spawning the new consultation's worms. During Play, old instances are deactivated before deferred destruction to prevent an overlapping frame. Authored marker transforms and decorations are preserved.

The component Inspector exposes positions, prefab options and a symptom dropdown. Prefabs generated from NPC_0 inherit these references. Earlier Edit Mode checks covered prefab/anchor configuration, both matching diseases, healthy cleanup, repeated initialization, stable instances on re-enable and preservation of authored prefab/profile data. The later single-worm adjustment selects one anchor and instantiates once; it has been compile-checked. The temporary setup/check script was removed after verification.

## Internal organ abnormality appearance

OrganAbnormalityAppearance on NPC_0 references three explicit prefab/anchor pairs under NoiTang: Than -> Than, Gan -> Gan, Da_Day -> Da_day. PatientExaminationProfile.Initialize queries the assigned disease for the stable Internal Organ Abnormality symptom ID. When present, exactly one random pair is selected and its prefab is spawned at the paired anchor with localPosition zero. Other transforms and renderer/material settings remain authored on the prefab. The normal Internal Organ Disease currently supplies that symptom.

The selected organ stays fixed through tool switches and disable/enable. A new consultation clears only the tracked generated instance before selecting again; a consultation without the symptom clears it and spawns none. Deferred destruction is preceded by deactivation. The component Inspector allows editing the prefab/anchor pairs and symptom mapping. Bulk-generated NPCs inherit the configuration from NPC_0. Edit Mode verification exercised six successive consultations, single-instance counts, correct pairings, renderer preservation, stable re-enable and healthy cleanup. Temporary setup/check scripts were removed after verification.

## Heart and brain symptom prefabs

SymptomPrefabAppearance on NPC_0 binds Heart Abnormality to Tim.prefab at the Tim anchor and Brain Abnormality to Nao.prefab at the Nao anchor. PatientExaminationProfile.Initialize applies these mappings independently from the current disease's stable symptom IDs. There is no random selection between Tim and Nao; if both mapped symptoms exist, both spawn. Brain Abnormality (Tung Tung Tung) remains a different symptom, so Brain Rot does not use Nao through this mapping. Vampire Infection does show Tim because it has Heart Abnormality. The Tim display anchor is separate from the Heart listening anchor.

Spawned objects use localPosition zero and retain authored prefab layers, materials, sorting, rotation and scale. Tool/enable toggles preserve instances; initializing another consultation removes only tracked generated objects before applying new symptoms. The Inspector exposes each prefab, position and symptom dropdown. NPCs generated from NPC_0 inherit the configuration.

Play Mode verification entered Office through Open, confirmed the spawned NPC's saved mappings, exercised healthy/Heart Disease/Brain Disease/Brain Rot/Vampire Infection and a combined-symptom fixture, repeated consultation initialization and checked cleanup, prefab settings and slot positions. Disease fixtures were in-memory clones; no Give/day completion was invoked, and the player's state/save remained unchanged. Temporary setup/check code was removed afterward.

## NPC dialogue

In NpcProfile, select a record and assign **Asset hội thoại** (NpcDialogueAsset, a ScriptableObject .asset). Its multiline content field is editable inline in the NPC Inspector, or select the asset to edit its TextArea in its own Inspector. **Tạo hội thoại mới** creates and assigns an empty asset; **Chọn asset hội thoại** selects it. Assets can also be created with Assets/Create/Doctor Help/Dialogue/NPC Dialogue. NPC_0 references NPC_0_Dialogue.asset; its content is authored user data and must be preserved. Empty/unassigned/whitespace-only content means no dialogue. New imported/generated records start with no dialogue; refresh preserves an existing record's reference. The current consultation snapshots content; edits are read when a new consultation initializes.

PatientSpawnPoint starts NpcDialogue only after arrival reaches NPC_Idle. The shared All.controller has an independent Dialogue layer with Closed and ChatBox_Open states; the existing Chatbox_Open clip plays once and keeps its final pose. DialogueOnly.mask limits this layer to the ChatBox subtree, explicitly excluding the animator root and body. Layer weight defaults to zero, becomes one while dialogue opens/is shown and returns to zero on StopDialogue. Both masking and the zero-weight closed state are required: an unmasked empty override layer at weight one can freeze root translation even while Base Layer state/normalizedTime advance. Text/Next stay hidden until the opening completes. Confirmation of Give stops dialogue before Leave/A_Walk_Out. Opening the medicine popup does not stop it.

Every line in the asset content field is a turn (including internal empty lines); LF/CRLF are supported and a terminating newline adds no extra empty turn. TMP plain text progressively reveals at 30 characters/second, adjustable on NpcDialogue. Clicking Next during typing completes that turn; a later click advances; clicking after the final completed turn restarts from line zero without reopening ChatBox. The Dialogue Text child uses wrap, auto-size and masking in a defined RectTransform above Next. Font, font size range, color and bounds are authored in the prefab; both text renderers use Sorting Order -11. Fit In Viewport defaults on with Maximum Viewport Width 0.38 and Screen Padding 16 pixels; LateUpdate fits the animated bubble pose inside the camera/screen Safe Area. Adjust or disable these settings for a fully manual layout.

NpcLipAnimation hides Mouth's existing part objects while typing and cycles the 11 ordered sprite frames in 2D_Assets/Lip at 10 FPS. It restores each part's previous active state when waiting or stopping. Speaking Lip is a sibling of Mouth so NpcAppearanceProfile can replace the Mouth parts without breaking dialogue references. The slot's transform and sprite renderer are editable in the prefab.

NpcDialogueNext uses the authored Next BoxCollider2D with mouse/touch press-release handling, ignores clicks through popup UI and registers an examination gesture blocker. No global Physics2DRaycaster is added to the camera, so other world colliders remain tool targets. All new text/lip objects and component references are authored in NPC_0; bulk-generated NPCs inherit them.

Dialogue was verified in actual Home -> Office Play Mode with in-memory dialogue fixtures: Walk_in/open sequencing, Vietnamese typewriter text, empty lines, mouse/touch Next completion/advance/restart, 10 FPS lip changes, original-mouth restoration, text sorting/bounds, popup coexistence and Give/Walk_out cleanup. Portrait render captures were inspected. Player state/save and authored NPC profile were unchanged. Edit the NPC_0 dialogue asset content in the Inspector to control that patient's speech; do not overwrite authored text during setup or cleanup. Android/device verification remains separate.

A later motion regression exposed a gap in the earlier dialogue checks: state transitions succeeded while the unmasked Dialogue layer overrode root movement. The fix was verified in Play Mode by sampling actual root positions during arrival/departure, testing movement with the masked layer forced to weight one, and running three Give -> departure -> next-arrival -> dialogue cycles. Opening transforms, lip/Next behavior, no-dialogue behavior and source/save preservation were also checked; render captures confirmed physical movement. Temporary probes and verification artifacts were removed after the checks.

## Thermometer

Select Desk/NhietKe using the same ExaminationToolSelection, glow and existing Selected_Icon as other desk tools. The selector's stable enum appends Thermometer = 4. A new hold outside UI/button hit areas shows Mechanics/NhietKe; it takes 2 seconds to rise linearly from 33 C to the sampled result. Release, tool switch, deselection or leaving Examination hides the view. There is no distance-to-body requirement. UI-started drags do not trigger measurements.

ThermometerExamination on Desk reads the current patient's consultation temperature, sampled once from its NpcProfile sickness through TemperatureRules and the stable High Temperature / Low Temperature symptom IDs. Normal is 37 C, high is 38.6-42 C, low is 33-34.9 C, rounded to one decimal. Repeated measurements during the same consultation reuse the exact same temperature. PatientExaminationProfile caches it on first measurement; Initialize begins a new consultation and resets that cache. A new patient consultation may sample a different valid value, even for the same prefab. Releasing, switching tools, hiding the thermometer and Editor script reload do not reset it. Only the assigned sickness symptoms determine temperature. Missing references and contradictory high/low symptoms report errors instead of silently yielding normal temperature.

ThermometerDisplay explicitly references ThuyNgan and the authored Min (33 C) / Max (43 C) transforms. ThermometerMercury material clips the mercury sprite above the interpolated local-space height using a MaterialPropertyBlock; neither sprite scaling nor the bulb/width is changed. Mercury renders above the opaque thermometer frame (Order in Layer 302 versus 301). Sprites, frame layout and calibration marks remain editable in the scene. Editing the Min/Max positions recalibrates the scale without code changes.

Adjust Measurement Seconds and symptom dropdowns on Desk > Thermometer Examination. Both tool and display are authored scene objects; no UI is spawned at runtime.

NPC data conversion removed the old classification field. Legacy Zombie/Vampire records were assigned Zombie Infection/Vampire Infection through Unity APIs before the field was removed. Names, stable IDs, prefab references and existing disease IDs were retained. One-time backup snapshots and the conversion manifest are in Library/NpcSicknessMigration; verification is in Logs/NpcSicknessMigration. The conversion scripts were removed after verification; no startup migration or automatic disease assignment remains.















## Multiple NPC diseases

NpcDefinition now stores sicknessIds (0–2 distinct stable IDs). The profile has secondSicknessChance (default 0.3). NpcSicknessAssignment is used by the batch generator: choose one disease, independently roll for a second, then sample only compatible candidates. This is authoring data, never a consultation-time disease roll. Ordinary refresh/import preserves authored records and gives new imports an empty list. The Inspector adds/removes disease slots and filters incompatible choices.

NpcMedicalConditions resolves a deduplicated union for all examination mechanics. Vampire and Zombie cannot coexist. Their symptoms take precedence in the heartbeat and temperature categories when present in that category. No Heartbeat suppresses rapid/slow; remaining rapid+slow combinations are rejected. Vampire+CoroNONO therefore has zero BPM and low temperature while retaining shortness of breath. Zombie retains high temperature. Other visual symptoms coexist, and shared symptoms spawn only one effect. Extend the central category rules when adding new mutually exclusive symptom types.

The one-time conversion retained each NPC's first disease and asset references, rolled a compatible second disease, and updated dialogue complaints for the effective symptoms. Backups and a conversion manifest remain in Logs/NpcMultiSicknessMigration; no startup conversion script remains. Dialogue is authored English, 3–6 short lines, greeting and feelings before any suspected cause, approximately 60% guessing / 40% unsure. Manual disease edits require checking the dialogue text.

The treatment design is recorded in AGENTS.md, but Give and outcome evaluation remain unchanged. AllSickness.HasRequiredPills still checks one disease only and must not be mistaken for a complete multi-disease treatment evaluator.

Edit Mode checks covered all 12 single diseases and 66 distinct pairs (64 valid, 2 rejected), order independence, Vampire priority, Zombie temperature, BPM, consultation temperature caching, duplicate-worm suppression, simultaneous heart/brain appearances, fractures and cleanup. These checks do not claim a new Android/device test.


## Shop, inventory and treatment audit

Home's existing Shop and Inventory buttons are connected in the saved scene. Office also has an Inventory button for viewing stock separately from treatment selection. Shop is available only during Preparation. Buy X1/X10 charges the per-item price and adds stock in one memory-only transaction; insufficient funds show a message and leave both money and stock unchanged. There is no purchase on credit. Inventory/treatment lists show owned medicines; Shop lists all PillProfile entries. Empty inventories show a message. Lists scroll and use scene-owned popups with prefab rows. Clicking the backdrop or X closes the popup; clicking the panel does not close it. All UI lives under each scene's Safe Area.

Edit MoneyDisplay, MedicineRow, ShopPopup, InventoryPopup, InventoryButton and SelectPillPopup in Assets/Resources/UI. Missing medicine artwork uses a temporary generic sprite; assign actual art in PillProfile. Descriptions are editable placeholder game text. The money display subscribes to the same PlayerProfileService.Changed event as the medicine lists. Prices, description, sprite and display names are catalogue data; saved inventory keeps only stable medicine IDs and quantities.

TreatmentDecisionState snapshots selected IDs and delegates an atomic transaction to PlayerProfileService/PlayerProfileSession. Deselected or cancelled pills are not consumed. Confirming Give consumes one per selected type, independent of the disease recipe. Repeated Give is blocked per consultation, including duplicate journal detection. A history-recording failure leaves stock unchanged and keeps the popup available for retry. No patient result or money reward is calculated yet.

TreatmentJournal holds the current day's records only in RAM: consultation GUID, stable NPC ID/name, exact sickness ID/name snapshot, medicine ID/name/quantity snapshot and UTC timestamp. Give does not write any treatment file. PlayerProfileService.GetTreatmentSnapshot() returns an independent copy for future result calculations before CompleteDay. Successful day checkpoint clears history; failed saving retains it for retry. Restarting discards unfinished history together with the working day. The Inspector displays the number of treatments in RAM. Player checkpoints still use persistentDataPath and are saved only at day completion.

Shop/inventory verification used an isolated profile directory, exercised real button handlers and UI raycasts, checked rendered screens and repeated daily checkpoints. Temporary setup and verification runners/data were removed afterward. Physical Android/touch testing remains separate.



## Police reporting

Click the existing Telephone tool on Desk during Examination to open the scene-owned PoliceReportPopup. ExaminationToolSelection raises ToolSelected through the same mouse/touch gesture handling and UI blocking used by the other tools. SceneGameplayUI requests TreatmentDecision with ReportToPolice; UI never jumps directly to departure. Phone input is disabled during arrival/departure, while a popup is open, and outside Examination.

The popup asks "Report this case to police?" and offers mutually exclusive Vampire infected / Zombie infected choices. Neither is preselected; Report requires one reason. Selecting the other replaces the check. Close/backdrop cancel returns through the DayFlowPlan return branch and records nothing. Reopening clears the previous choice. The clinic clock continues.

Confirming Report calls PlayerProfileService.TryReportToPolice, records one RAM decision, then follows the existing PatientDeparture and repeat/closing rules. It does not consume medicines, charge money, classify correctness or modify NPC disease assignments. TreatmentJournal.Entry stores decisionKind, reportReason, reportedSicknessId and a snapshot of the actual NPC sicknesses for later evaluation. Reasons map to the existing stable Vampire/Zombie IDs independently of display names. Medication entries retain their selected medicines. Exact duplicate confirmations are idempotent; conflicting decisions for the same consultation are rejected. Successful day checkpoints clear RAM history, failed saves retain it.

Edit Assets/Resources/UI/PoliceReportPopup.prefab for the panel, static question, option labels/check sprites and Report button. The Report button remains a nested GiveButton prefab. The popup instance and references are saved in Office before Play; no persistent service creates UI.

Police reporting verification passed 36 isolated Play Mode checks: real telephone hit testing with simulated mouse/touch, mandatory exclusive choice, cancel/reopen, duplicate/conflicting decisions, medicine/report coexistence, NPC root motion/rendering with and without dialogue, and two consecutive day checkpoints. Real player save contents and project input settings were preserved. Temporary runners, captured frames and test saves were removed; no physical Android build was tested.


TreatmentJournal treats an empty serialized Day as uninitialized, including Unity's inline null-to-default conversion (day 0). GetTreatmentSnapshot returns null for an empty journal; the first Give/Report assigns the active profile day. Nonempty history from another day remains an error and is never silently discarded. This applies after Inspector serialization, code reload and clearing history at a successful checkpoint.

## Result cutscenes and day evaluation

TreatmentEvaluator reads the closing day RAM snapshot and AllSickness from the Office PatientSpawnPoint profile before saving. Ordinary diseases require the union of every recipe; one unit covers each distinct required type even if shared by two diseases. Extra medicines are allowed. Vampire/Zombie require the matching police reason and stable reported disease ID, taking priority over the second disease's medicine recipe. Any incorrect/incomplete consultation selects Fail; an entirely correct day selects Success. Invalid catalogue/history data produces a retryable flow error before the checkpoint, rather than inventing a patient outcome. No money rewards, fines or saved/killed counters are calculated by this change.

DayCompletionService caches DayTreatmentResult only after a successful profile checkpoint. The RAM journal is then cleared. Home/decoder errors can retry the same cutscene using that cached result without recomputing from cleared history or advancing/saving twice.

Home's ResultsPopup.prefab contains ResultsVideoPresenter, a VideoPlayer and RawImage/AspectRatioFitter. The VideoPlayer renders to Assets/Resources/UI/ResultsVideo.renderTexture, also assigned to the RawImage; no frameReady callback or extracted image sequence is used. Clips are direct references to the supplied Success.mp4/Fail.mp4. Both are 1080x1920, about 10 seconds, with audio. They are fitted inside the Canvas without stretching/cropping. VideoPlayer uses unscaled time, does not loop/autoplay, and finishes through loopPointReached. Playback pauses on focus/background loss. Preparation has a foreground-only timeout; decoder errors use the flow's retry UI. Android clip import uses H.264 transcoding; actual-device validation is still required.

After the last patient's departure, Office fades black for 1 second before checkpoint/Home loading. Home remains black across the scene load. The captions `The day has ended.` and `Later that night, your client takes your pill...` each fade in for 1 second, hold for 3 seconds and fade out for 1 second. The video fades in over its first second, plays to completion, then fades its final decoded frame to black for 1 second. `new day has started.` follows the same 5-second caption timing. Preparation is shown behind the cover before revealing Home over 1 second. The whole sequence is awaited by DayResultsState, so Open cannot start the next day early. All fades and caption timers pause in the background.

Edit caption text/font/layout and ResultsVideoPresenter timing fields in Assets/Resources/UI/ResultsPopup.prefab. Edit the scene fade duration in Assets/Resources/UI/SceneTransitionFade.prefab. Both scenes contain a full-screen cover Canvas at sorting order 100; result content uses a Canvas at 200 inside Safe Area. Home's authored cover starts opaque and Preparation reveals it after initialization; Office starts clear. Flow errors clear the cover so Retry remains accessible.

Transition verification covered consecutive Success/Fail days in Play Mode with isolated checkpoints, visible caption/video screenshots, caption durations, video fades/final frame retention, and return to Preparation without double saves. A separate run measured the animated NPC root moving 7.17 world units and checked departure renders before the Office fade. Timers start counting after their first awaited frame so scene-loading time is not counted toward the next fade. Temporary validation scripts and data were removed afterward; scene/prefab backups remain in Logs/DayTransitionBackup.

Result cutscene verification passed 30 focused checks (Edit Mode rules and Play Mode integration): missing/wrong/extra medicines, multiple diseases and police priorities; real Give → departure → checkpoint → native Success/Fail playback through RenderTexture over two days; video error retry, background pause, waiting for actual clip completion, and unchanged real player save. Temporary runners, screenshots and isolated saves were removed after validation.


## End-of-day newspaper popup

Home now shows NewspaperResultsPopup after the final new-day caption, while the black cover remains visible. Continue returns through the existing DayResults/Preparation flow. The saved popup/row prefabs, editable Article Content, stable-ID prefab portrait capture workflow and verification details are documented in Design/Newspaper/README.md. Patient summaries are cached with DayTreatmentResult before journal cleanup, without introducing additional saves or day increments.

## Desk actions and aspect ratio handling

The base Home/Office actions are now DeskActionButton sprite/collider prefabs under Desk, saved in Assets/Prefabs/Desk. Popup actions remain screen UI. Camera framing, world input blocking, the magnifier's projected lens calibration, clamped Safe Area, editable settings and verification scope are documented in Design/DeskLayout/README.md.

DeskActionButton also exposes press/release dimming settings. Each scene contains ClickFeedback.prefab to display Assets/VFX/VFX_Click.prefab once per new mouse press or touch, including over popups and black covers. Configuration is documented in Design/DeskLayout/README.md.
