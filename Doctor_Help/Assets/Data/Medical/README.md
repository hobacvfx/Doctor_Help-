# Medical design data

Current source: the user's colour-coded diagram updated on 2026-11-22, archived at `Design/Medical/Diagram_2026-11-22.png`. White nodes are diseases, blue nodes are symptoms, green nodes are remedies/decisions. English display names retain the game's existing stable IDs. These are fictional game rules, not real-world medical guidance.

## Disease mappings

- **Physical Injury** (Chấn thương): Bone Fracture → Bone Growth Medicine.
- **Delusional Disorder** (Bệnh hoang tưởng): Brain Abnormality (Delusion), Incoherent Speech, Rapid Heartbeat → Sedative.
- **Parasite Infestation** (Bệnh giun): Parasite Infestation, Rapid Heartbeat → Antiparasitic Medicine.
- **Internal Organ Disease** (Bệnh nội tạng): Internal Organ Abnormality, Low Temperature → Liver Detox Medicine.
- **Heart Disease** (Bệnh tim): Heart Abnormality, Slow Heartbeat → Heart Medicine.
- **Flu** (Bệnh cảm cúm): High Temperature, Rapid Heartbeat → Antibiotics.
- **Brain Disease** (Bệnh não): Brain Abnormality, Headache, High Temperature → Brain Medicine.
- **CoroNONO Virus Infection**: Rapid Heartbeat, Shortness of Breath, High Temperature → Vaccine.
- **Brain Rot** (Bệnh thối não): Brain Abnormality (Tung Tung Tung), Slow Heartbeat, Headache → Science Book.
- **Brittle Bone Disease** (Bệnh xương thủy tinh): Bone Fracture, High Temperature → Calcium.
- **Zombie Infection**: Parasite Infestation, High Temperature, Nausea, Brain Abnormality → Report to Police.
- **Vampire Infection**: Heart Abnormality, No Heartbeat, Low Temperature → Report to Police.

There are 12 diseases in this diagram. The existing **Haunted** disease and its Remove Ghosts mechanic remain an additional independent disease in the game. Current catalog totals: 13 diseases, 17 distinct symptoms (including Haunted and the separate Delusion brain variant), 10 remedies. Report to Police and Remove Ghosts are treatment modes, not pills.

The three brain symptoms are separate stable IDs and visuals: Brain Abnormality (`Nao`), Brain Abnormality (Delusion) (`Nao_Delusion`), and Brain Abnormality (Tung Tung Tung) (`Nao_BrainRot`). Zombie uses ordinary Brain Abnormality; Delusional Disorder uses the Delusion variant. Vampire now includes the existing Heart Abnormality visual. Shared anchors still alternate active images once per second when multiple brain variants coexist.

The existing `Organs_Detox_Pill` remedy ID, sprite and price are preserved; its display name is now **Liver Detox Medicine**, corresponding to Giải độc gan. Existing medicine inventory and treatment references continue to use the same ID. The old rule assigning Antiparasitic Medicine to Internal Organ Disease is superseded by this diagram.

## Multiple diseases and dialogue

Symptoms resolve through `NpcMedicalConditions`; duplicate symptoms apply once. Vampire/Zombie priorities for heartbeat and temperature remain in force. Invalid combinations with conflicting fast/slow heartbeat or high/low temperature are rejected; Vampire and Zombie cannot coexist.

This one-time authoring update repaired 23 now-incompatible NPC disease combinations using the fewest replacements, preserving each primary disease, disease count, identity, prefab, dialogue asset reference and existing Vampire/Zombie/Haunted status. It updated 118 populated English dialogue assets to the final resolved symptoms, including priority overrides. It did not add any runtime disease/dialogue regeneration. Empty/unassigned dialogue slots would remain empty.

Dialogue uses 3–6 short turns: greeting, subjective symptoms, then a guess or uncertainty. Since 2026-11-23 dialogue never reveals measured symptoms (temperature or heart rate, including No Heartbeat); those come only from the thermometer/stethoscope, and their catalog entries have no dialogue phrases. It does not reveal unseen X-ray diagnoses. Nausea is explicit for Zombie patients. New wording is registered in `ExaminationClueCatalog` so the automatic clue flight still triggers once the complete phrase appears. Doctor Book descriptions use explicit stable-ID symptom tags, so shared symptoms highlight correctly in every disease regardless of list order.

## Validation and recovery

All diagram mappings, stable IDs/references, correct/wrong/empty remedy queries, all 118 NPC combinations and their dialogue clue coverage, anatomical prefab bindings, all four compatible difficulty pools, conflict rejection and Vampire/Zombie priorities were checked in Unity. Actual Office measurement rules, casebook rendering and consultation visual initialization also passed isolated Play Mode checks; see `Design/Medical/UPDATE_2026-11-22.md` for the final result.

Before-update assets and the detailed NPC repair report are under `Logs/MedicalDiagram_20261122_234954`. No real player save is modified by this update or its verification. Assets stay Inspector-editable; do not reset IDs, auto-regenerate at startup, or re-run this migration on existing data.
