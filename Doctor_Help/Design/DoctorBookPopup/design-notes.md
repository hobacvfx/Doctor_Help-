# Doctor_Book popup — image demo only

Opening action: click/tap the existing Doctor_Book object. This task supplies concept images only; no Unity scene, prefab, script or data asset is changed.

Visual direction: an ordinary Victorian doctor's clipboard with a modest brass clip, dark brown backing and aged ivory paper. Dark ink contours and many long, thin exploratory pencil marks follow the approved medicine illustration direction. Materials use discrete cel-shaded base, shadow and highlight shapes. Text stays clean, high contrast and clear of the sketch fringe.

Portrait mobile layout: one vertical list, six complete rows visible simultaneously, rather than two narrow columns. Each row puts eligible symptom drawings in the left ~23% and a bold condition name plus short description in the right ~70%. Thin horizontal separators conserve space. A narrow right-hand scroll rail and small bottom scroll cue communicate continuous scrolling. The title, close button and compact footer remain outside the scrolling viewport. No treatments or medicine controls are added to this reference popup.

## Revised illustration rule

Only physical anatomical symptoms with implemented visual appearances in the game belong in the illustration column: fractured bones, parasites, abnormal internal organs, abnormal heart and ordinary abnormal brain. Draw new conceptual medical illustrations with the same meaning, without copying the actual game sprites. An abdomen containing a parasite is an approved example. The organ group can show liver, kidney and stomach without reproducing the game sprites' character faces, props or poses.

Dialogue symptoms (headache, nausea, shortness of breath, incoherent speech) are text only. This demo also puts measured temperature and heartbeat details in the description instead of adding thermometer or ECG pictograms. A condition with no eligible anatomical visual leaves the illustration area empty; no invented substitute icon or placeholder label is added. Keep all symptoms in the descriptions even when their images are removed.

The current NPC symptom prefab bindings include ordinary Brain Abnormality but not the separate Brain Abnormality (Tung Tung Tung) used by Brain Rot. Consequently Brain Disease has a brain illustration, while Brain Rot stays text-only in this revision. This does not change either symptom's data or behavior.

Two images show the same popup at the top and bottom of its 12-condition list. These are scroll positions, not separate pages or pagination. All symptom content below follows the current AllSickness.asset and SymptomProfile.asset, including the updated name Physical Injury. No symptoms are inferred from real-world medicine or added just because they are common in a disease.

## Top of list

1. Physical Injury — Broken bones visible on X-ray. Illustration: fractured bone.
2. Delusional Disorder — Speech is confused and incoherent. Illustration area empty.
3. Parasite Infestation — Parasites are visible inside the body. Illustration: abdomen X-ray with parasites.
4. Internal Organ Disease — Abnormal internal organs on X-ray. Illustration: abnormal liver, kidney and stomach.
5. Heart Disease — An abnormal heart is visible on X-ray. Illustration: abnormal heart. No extra pulse symptom is assigned to this condition.
6. Flu — A high temperature is detected. Illustration area empty.

## Lower scroll position

7. Brain Disease — An abnormal brain is visible on X-ray. Illustration: abnormal brain.
8. CoroNONO Virus Infection — Shortness of breath, high temperature and rapid heartbeat. Illustration area empty.
9. Brain Rot — Headache, slow heartbeat and a distinctive brain abnormality. Illustration area empty. This maps to Brain Abnormality (Tung Tung Tung) in the game data, without inventing a visual symptom or meme character likeness.
10. Brittle Bone Disease — Bone fractures with a high temperature. Illustration: fractured bone only.
11. Zombie Infection — Nausea, high temperature and parasites inside the body. Illustration: abdomen containing parasites only.
12. Vampire Infection — Abnormal heart, no heartbeat and low temperature. Illustration: abnormal anatomical heart only.

Headache, shortness of breath and nausea are written explicitly, because these are text/dialogue symptoms. Other symptom names are also stated briefly so the imagery does not carry the entire meaning. Brain Abnormality (Tung Tung Tung) remains distinct from the ordinary Brain Abnormality symptom.

Images are generated with the built-in image_gen tool. The latest revision is `DoctorBookPopup_Top_v2.png` for conditions 01–06 and `DoctorBookPopup_Bottom_v2.png` for conditions 07–12, with prompts `prompt-top-v2.txt` and `prompt-bottom-v2.txt`. Earlier non-v2 images and prompts are historical and do not follow the revised illustration rule.

Both v2 images were visually checked: six complete rows each, unchanged descriptions, empty illustration areas for text-only conditions, no symptom portraits/thermometers/ECG traces, and semantic anatomical illustrations only. The organ cluster contains liver, kidney and stomach; the parasite illustration remains a conceptual abdomen view. These v2 images are historical design mockups.

The popup is now implemented in Office with separate paper/frame/symptom sprites, reusable prefabs, stable-ID art bindings, and an editable symptom-art catalogue. Maintenance instructions are in `Assets/Art/UI/DoctorBook/README.md`; actual isolated Unity Play Mode renders and the verification report are in `Verification/`. The production UI reads AllSickness rather than baking the 12 demo rows into an image.
