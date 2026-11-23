# Give-medicine desk tray

Replacement art for `Assets/2D_Assets/Cho thuoc.png`: worn pewter metal tray containing amber/green/cobalt medicine bottles, a paper medicine packet, tablets, rolled bandage and period glass/metal syringe. Hand-pencilled dark outlines and cel shading follow the approved medicine reference. Room_Desk.png controls the view from above/in front: horizontal front/back edges, sides converging backward.

The original PNG and metadata are backed up in `Source/`. `MedicineTray_Generated.png` is the full-resolution selected imagegen output; exact prompt is in `Generation.prompt.txt`. The replacement intentionally keeps the original asset path/GUID and button references. Its higher-resolution pixels-per-unit is adjusted to preserve the old 4.25-unit full-canvas width, centered pivot and existing scene transforms. Existing collider and DeskActionButton behavior are preserved.

Mobile texture overrides: ASTC6×6 Android/iPhone, no mipmaps, sRGB and alpha transparency. No gameplay code or scene layout is changed. `OfficeDesk_TrayPreview.png` is an isolated Editor preview render from saved Office scene content to check the new artwork in desk perspective; it does not save the scene or use player data. Full Play Mode interaction/device build is not tested in this art-only replacement.
