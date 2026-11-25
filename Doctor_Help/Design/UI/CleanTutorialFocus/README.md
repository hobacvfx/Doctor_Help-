# Clean tutorial camera focus

When the existing stain tutorial detects a visible stain, it locks manual walking and asks the scene's PlayerHomeCameraFollow to focus on the **sprite bounds center**, rather than the decal pivot. The camera eases to that center over **one unscaled second**, preserving depth and zoom. The tutorial overlay remains hidden until the move completes.

Camera follow stays overridden while the player reads the explanation and receives the Clean prompt. Starting cleaning ends the focus and releases the tutorial movement lock; normal player follow resumes smoothly. Missing stains, invalid gameplay state, disabled tutorials or canceled camera focus also clean up both locks. SnapToTarget cancels a previous focus when scene navigation needs the normal spawn camera pose.

Player_Home stores an explicit reference to its existing PlayerHomeCameraFollow on PlayerHomeCleanTutorial; cameraFocusSeconds is serialized as 1. No new camera or UI is spawned by the feature. Existing tutorial eligibility, Clean unlock and profile saving rules are unchanged.

`PrepareValidation.py` stages copies of production scripts and the actual TutorialOverlay prefab in an isolated Unity project. `Validation.cs.txt` checks detection, hidden overlay at 0/.25/.5/.75 seconds, exact centering and explanation at 1 second, holding during review, Clean handoff, normal follow recovery, disappearance cancellation and disabling mid-pan. Writes use TutorialPreviewSession only. Results are in `Validation.txt`. This is manually stepped Editor integration, not a full gameplay Play Mode or Android device test.
