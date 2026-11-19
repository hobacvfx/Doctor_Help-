# Medicine illustrations

Final art: `Assets/2D_Assets/Medicines`. Ten illustrations correspond to every existing entry in PillProfile; `InjectionSyringe.png` is an additional standalone equipment illustration. Vaccine includes its own vial and syringe as a compact medicine icon.

Generated using the built-in image_gen tool. The final prompt set is in `Prompts`. Rendering follows the user's Tim Burton style direction: dark ink contours, many longer thin exploratory pencil marks and material hatching, with discrete cel-shaded base, shadow and light planes. Object designs use ordinary Victorian pharmacy glassware and packaging, without fantasy-shaped vessels or magical details.

The completed original medicine images were retained after the user's instruction not to redo completed items. Additional alternative packaging studies were not substituted for these images. Transparent canvas padding was added mechanically without repainting or resampling the generated drawing pixels, to keep loose pencil strokes away from the UI bounds.

The medicine set is Bone Growth Medicine, Sedative, Deworming Medicine, Heart Medicine, Antibiotics, Brain Medicine, Vaccine, Science Book, Calcium and Organs_Detox_Pill. Glass color, vessel proportions, stopper/cap type, contents and paper-label treatment distinguish the medicines. Science Book remains an ordinary leather-bound book because it is already a remedy in the game.

Each existing PillProfile sprite slot was assigned through Unity's SerializedObject API using the stable medicine ID. Names, IDs, prices, descriptions and entry order were preserved. No new medicine ID, medicine type, treatment rule or injection mechanic was added for the standalone syringe.

PNG originals retain full resolution and alpha. Unity imports use centered Full Rect sprites, 512px maximum texture size, bilinear filtering, no mipmaps and uncompressed texture data for clean ink in small UI images. Existing MedicineRow images already preserve aspect ratio.

Verification: transparent background, non-clipped padded canvas edges, dark ink presence, successful Sprite imports, ten assigned stable-ID references, preserved non-sprite PillProfile data, and binding each medicine through the real MedicineRow.Refresh Image component with Preserve Aspect. The contact sheet was rendered from imported Unity sprites. Unity 6000.3.9f1 Edit Mode only; no Play Mode or Android test and no player save changes. The disposable import script/project was removed after installation.
