# Doctor Book examination clues

Implemented in Office, 2026-11-22. Evidence belongs to a consultation ID and lives in RAM only. It never changes the player save or reveals the patient's undiscovered symptom list in the UI.

## Behaviour

- Dialogue automatically registers a configured symptom phrase once the entire phrase has appeared, including when Next reveals the rest of the line. The phrase must describe an actual symptom of the current patient. The flight uses only the short symptom display name from SymptomProfile (for example Rapid Heartbeat), in white, even if the matched dialogue alias is a long clause. The original text stays in its bubble. Returning to a line does not replay evidence.
- X-ray requires 0.5 continuous seconds over the actual active abnormal visual. Leaving the lens, releasing, changing tools, pausing, losing focus or opening the book resets observation time. Fracture detection samples visible crack positions, mirroring the bone shader's UV * 2 + offset. Alternate brain images must each be observed while active.
- An abnormal thermometer result registers at the end of its existing 2-second measurement. Listening requires 3 continuous seconds with positive signal, even very quiet. Leaving audible range or releasing resets listening time. Normal temperature and heartbeat produce no evidence. A silent/no-heartbeat result does not meet the requested positive-audio rule; an explicit dialogue clue can still register No Heartbeat.
- Duplicate words/icons/visuals fly to the physical book. Source objects remain unchanged. Visual snapshots use a temporary duplicate renderer and preserve its material/property block. Temporary render textures are released with the flight.
- Unread clues make the book gently pulse and show a red exclamation mark. Opening it acknowledges all current updates. Later evidence shows the notice again. Reading does not erase discoveries.
- Every casebook occurrence of a discovered symptom is bold red and gently pulses. Matching uses stable symptom IDs, never the order of a disease's symptoms. A new consultation clears evidence, highlights and stale flights.

## Authoring

- `Assets/Data/Medical/ExaminationClueCatalog.asset`: assign stable symptom ID, measurement category, literal dialogue phrases and legacy casebook wording. Add synonyms here when writing new NPC dialogue. This is deterministic phrase matching, not semantic language interpretation. Common English and Vietnamese phrases are included. Brain Rot's inserted em-dash interjection is ignored for matching without changing original text indices. Simple explicit negations are ignored.
- New casebook descriptions can use `<symptom id="STABLE_SYMPTOM_ID">wording</symptom>`. Existing `<symptom>wording</symptom>` tags use the catalog's reference aliases constrained to that disease's symptom IDs. Ambiguous aliases are not guessed.
- `DoctorBookClues` on Office's tool-selection object exposes observation times, flight prefab, icon references and book pulse settings. `Clue Feedback` is authored in Office at sorting 99, below the gameplay popup canvas. It has no raycaster. `New Clue` is an editable child of the physical book.
- `Assets/Resources/UI/ClueFlight.prefab` contains image, snapshot and word variants. `ClueTextPulse` on the row's description changes only discovered red glyphs.
- `Assets/Data/Medical/BoneClueMask.asset` contains small editor-baked alpha masks for the existing skeleton/crack texture. It does not require readable mobile textures. If changing that skeleton or crack texture/UV mapping, rebake the masks to match.

## Validation

Unity compilation and focused Play Mode checks passed with an isolated TutorialPreview profile. Checks covered journal deduplication/stale consultation rejection, acknowledgement/new updates, 0.5/3-second thresholds and interruptions, partial/full dialogue reveal and unchanged source text, negation/word boundaries/Brain Rot interjection, all existing casebook tag mappings, positive near-zero heartbeat signal, thermometer before/after completion, fracture and parasite observation, nonempty duplicate GPU capture without modifying the source, flight cleanup, normal results, same-object consultation reset and clearing all book highlights. Scene references, row/popup behaviour and 1080x1920 captures were inspected. Physical Android input and an AAB build were not tested.

Temporary setup/test scripts and isolated save data were removed. QA captures are in `Design/UI/DoctorBookClues`.

Caption refinement (2026-11-23): dialogue flights use the stable symptom's short display name in white. Focused Unity checks passed for Rapid Heartbeat, Headache and Nausea labels, preserved source spans, runtime white caption visibility and flight cleanup. The original dialogue was not changed. An isolated preview save was used; the temporary authoring/check script was removed. Capture: `White_Short_Caption.png`.
