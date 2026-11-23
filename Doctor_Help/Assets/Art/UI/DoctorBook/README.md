Current treatment UI (2026-11-23): the historical Given Medicines/Done descriptions below are superseded by `Design/UI/ChestMedicineSlots/README.md`. Office now uses chest receiving cards and submits automatically on the final distinct medicine drop; the current tutorial has no Done step.

# Doctor's Casebook

Popup mở khi bấm Doctor_Book trong Examination. Office lưu sẵn một nested prefab tắt mặc định dưới Gameplay Canvas / Safe Area. DoctorBookController dùng ExaminationToolSelection hiện có. Rời Examination thì đóng sổ. Không thay đổi gameplay flow hoặc dữ liệu lưu người chơi.

## Cập nhật nội dung

1. Thêm/chỉnh bệnh trong `Assets/Data/Medical/AllSickness.asset`, chọn triệu chứng từ SymptomProfile như trước. Popup đọc toàn bộ danh sách mỗi lần mở, không giới hạn 12 bệnh.
2. Mỗi bệnh có ô **Mô tả trong sổ bác sĩ**. Viết ngắn nhưng đủ triệu chứng, gồm cả lời khai như nausea/headache/shortness of breath. Để trống sẽ tự liệt kê tên tất cả triệu chứng. Nếu sửa triệu chứng của bệnh có mô tả riêng, cập nhật mô tả cho khớp.
3. Thêm triệu chứng mới trong `Assets/Data/Medical/SymptomProfile.asset`. Giữ ID hiện có khi đổi tên hoặc sắp xếp.
4. Với triệu chứng có hình ảnh thật trong gameplay, vẽ một minh họa ngữ nghĩa riêng, lưu PNG nền trong suốt tại `Symptoms/`, import Sprite (2D and UI), bật Alpha Is Transparency và tắt mipmap.
5. Mở `Assets/Data/Medical/SymptomReferenceArt.asset`, bấm **Thêm slot cho triệu chứng mới (giữ ảnh cũ)**, rồi kéo Sprite vào đúng triệu chứng. Liên kết dùng ID; đổi tên/sắp xếp không làm tráo ảnh. Để Sprite trống với triệu chứng chỉ có chữ/đo đạc hoặc chưa có hình gameplay. Nếu xóa triệu chứng nguồn, Inspector báo slot thiếu để sửa/xóa chủ động.

Nhiều bệnh dùng chung một triệu chứng sẽ tái sử dụng cùng Sprite. Một bệnh có nhiều triệu chứng được gán ảnh sẽ có nhiều ảnh xếp trong vùng minh họa. Không ghép chữ, tên bệnh hoặc toàn bộ hàng bệnh vào bitmap. Brain Abnormality và Brain Abnormality (Tung Tung Tung) là hai ID riêng; bản hiện tại chỉ gán ảnh cho loại thứ nhất.

## Chỉnh giao diện

- `Assets/Resources/UI/DoctorBookPopup.prefab`: khung, giấy, tiêu đề, đóng, nền tối, ScrollRect, thanh cuộn, footer. Các chữ tĩnh/font/màu/vị trí chỉnh ở prefab.
- `Assets/Resources/UI/DiseaseReferenceRow.prefab`: số thứ tự, tên bệnh, mô tả và vùng ảnh. Chữ auto-size/wrap; nên giữ mô tả ngắn để dễ đọc trên mobile.
- Mỗi `DiseaseReferenceRow.prefab` giữ một Image con `SymptomIllustrations/IllustrationTemplate` làm mẫu tại chỗ, giữ tỷ lệ gốc. Không gán một component Image từ prefab bên ngoài vào slot này. `SymptomReferenceImage.prefab` là mẫu nguồn khi authoring; runtime dùng mẫu con đã lưu trong hàng.
- `ClipboardFrame.png`, `ClipboardPaper.png` và 5 PNG trong `Symptoms/` là các asset độc lập. Art tạo bằng built-in imagegen; prompt lưu tại `Design/DoctorBookPopup/ProductionPrompts/`.

Popup tự chia chiều cao viewport để hiển thị đủ 6 hàng; số bệnh tăng thì vùng nội dung cuộn dài thêm. Cuộn bằng kéo hoặc bánh xe; đóng bằng nút × hoặc vùng ngoài bảng. Giữ vị trí cuộn trong lần khám đang chạy. ID thiếu/trùng hoặc nguồn profile sai được báo rõ thay vì tự gán ảnh khác.

## Kiểm chứng

Đã kiểm tra Play Mode trong bản sao project cô lập, tắt bootstrap/profile service và dùng dữ liệu thử trong bộ nhớ: click sách qua hệ thống chọn dụng cụ thật, kéo/bánh xe cuộn, nút đóng/vùng ngoài, mở lại 5 lần, rời Examination, thêm bệnh và triệu chứng mới, gán ảnh rồi đổi thứ tự dữ liệu. Render 1080×1920 và 720×1600 đều đủ 6 hàng, không cắt chữ với dữ liệu hiện tại. Ảnh và kết quả tại `Design/DoctorBookPopup/Verification/`. Chưa kiểm tra trên thiết bị Android.

Office được tích hợp bằng Unity Editor API, có recovery scene trước khi sửa tại `Assets/_Recovery/Office_BeforeDoctorBook_20261105_105852.unity`. Các trường ID, bệnh/thuốc và profile người chơi không được thay đổi bởi bước tích hợp này.

Sửa lỗi hiển thị 2026-11-05: Unity ở project chính từng đọc tham chiếu Image từ prefab bên ngoài thành null, làm Bind lỗi và ẩn danh sách. Đã thay bằng Image mẫu con, lưu bằng PrefabUtility và force-reimport rồi kiểm tra render trực tiếp từ asset của project chính: đủ 12 bệnh, ba lần đóng/mở và cả vị trí cuộn đầu/cuối. Kiểm tra sửa lỗi này chạy trong preview scene riêng của Editor, không chạy bootstrap hay dùng save người chơi. Kết quả `Design/DoctorBookPopup/Verification/RepairVerification.txt` và `DoctorBook_Fixed_*.png`. Sách luôn đọc toàn bộ AllSickness, không có unlock hoặc bộ lọc theo tiến độ.

## Clean clipboard redesign — 2026-11-21

The approved redesign uses `Assets/Art/UI/ClinicClipboard/CleanClipboardAtlas.png`: clean walnut/ivory panel, thin paper card, brass clip, burgundy close button, and telephone. The five sprites are sliced in the importer; panel/card use 9-slice borders. Android and iOS overrides use ASTC 4x4, max 2048, no mipmaps. The source was generated with built-in imagegen and references to the approved mockups; prompts are in `Design/DoctorBookPopup/ProductionPrompts/CleanClipboard-20261121.txt`.

DoctorBookPopup now has three aligned columns: symptom illustrations, the widest description column (disease heading plus description), and required medicine icons. Existing symptom bindings and authored medical data are preserved. `DiseaseReferenceRow` reads every required medicine by stable ID from `AllSickness.PillProfile`; police cases display the telephone. RemoveGhosts cases currently have an empty treatment image cell. Multiple medicine icons can share the treatment cell. Missing/duplicate medicine links or missing art produce a visible catalogue error instead of silently showing a different treatment.

Office's SelectPillPopup is a compact clipboard with a fixed five-column square grid (the current ten medicines form two rows). It has no medicine captions, price, total, Give or Decline control. Clicking X or outside cancels back to Examination and leaves the NPC waiting. `MedicineRow` forwards drag events only for treatment; the shared inventory list keeps its existing click behavior.

During a drag, the panel/backdrop become transparent while retaining the EventSystem gesture; a scene-owned icon follows the pointer. Touch raises the visual slightly above the finger; the actual release point determines the hit. Only the current arrived NPC's visible body/head/outfit regions accept the drop; dialogue, background, another patient, and other blocking UI do not. A valid drop submits exactly one medicine through the existing TreatmentDecisionState transaction, then plays VFX_Puff and PatientDeparture on success. A miss, focus/pause, or popup closure cancels without a transaction. Errors keep the popup available for retry. Report via telephone remains available as before. First-run instructions teach dragging instead of selection followed by Give.

Prefab dimensions are authored for the existing Office Canvas reference resolution 540x960. Static type, colors, margins and sprite references stay in prefabs. `MedicineGridSizing` only computes responsive square cell sizes; no runtime service creates a popup canvas. A one-bottle drop now commits the consultation; the existing multi-disease correctness evaluator is unchanged.

Validation on 2026-11-21: Unity compilation passed. Play Mode used an isolated TutorialPreview profile and mouse/touch EventSystem gestures. The final run passed 36 checks across cancel/reopen, pointer ownership, focus loss, rejected transaction/retry, debt, one-record drop, visible puff, measured NPC departure motion, subsequent patients, police reporting, and two complete day/result/checkpoint cycles. Rendered UI was visually inspected at 1080x1920 and 720x1600. Real player-profile SHA-256 remained unchanged. No physical Android device or AAB build was tested. Temporary setup/verification scripts, screenshots and test saves were removed; pre-migration prefab/scene backups remain in Logs/ClipboardRedesignBackup.

## Touch scrolling and medicine thanks — 2026-11-21

Doctor Book has no scrollbar. Its viewport contains a transparent raycast Image so dragging over text, symptom icons or blank row areas reaches the ScrollRect. The content remains clamped and scrollable with touch/mouse drag.

SelectPillPopup.dragIconScale defaults to 2.5 and changes only the dragged preview; slot icons retain their normal size. The release point remains the actual finger/mouse location.

Successful medication transactions mark the current consultation HasReceivedMedicine. PatientSpawnPoint waits for NpcDialogue's one-shot `medicineThanks` (default "Thank you!") to finish before triggering Walk_Out. This uses the existing opening, typing/lip animation, one-second readable hold and closing sequence with no Next button, including patients without authored dialogue. The line does not overwrite NpcDialogueAsset. Failed/cancelled drops, police reports and rock-hit departures do not trigger medicine thanks. The flag resets on each consultation, and departure retries do not replay the line once it has started.

Validation: 24 focused isolated Play Mode checks passed. Actual GraphicRaycaster targets routed mouse/touch drags to ScrollRect in both directions; scale was measured at exactly 2.5x. Screenshots confirmed the enlarged drag icon and readable thank-you bubble; animation checks confirmed Idle during thanks and root movement after it. The next consultation resets the flag; reporting still departs without thanks. Real save hash was unchanged. No physical Android device test was run. Temporary fixtures and captures were removed after validation.

## Multi-medicine draft and Done — 2026-11-21

This revision supersedes the earlier one-drop commit behavior. Valid drops add distinct stable medicine IDs to PatientExaminationProfile.StagedMedicineIds. The draft belongs to the consultation, survives popup close/reopen and gesture cancellation, and resets in Initialize. Unity can serialize it across Editor code reload on that patient; it is never part of the player's profile JSON or an unfinished-day checkpoint. Dropping an already staged type is ignored.

The compact clipboard contains an inline `Thuốc đã cho` strip directly above the 2x5 grid. GivenMedicineTray sizes mini cards to 25% of normal cards. Both the card and its top-right X remove that medicine and call Headbar.PlayUiPuff at the card. ContentSizeFitter/VerticalLayoutGroup shrink the whole board to its contents. TreatmentDoneButton is a nested prefab below the board and reuses the existing clean green/brass button artwork. Fonts, sprites, padding, labels and control positions remain authored in prefabs.

Only Done raises GiveRequested. TreatmentDecisionState copies the entire draft before calling the existing atomic service transaction. Successful payment/journal recording clears the draft and enables the existing thank-you/departure sequence. Closing and all draft edits are free; a failed Done keeps the draft for retry. Done requires a nonempty list, repeated medicine types are not added, and the existing debt rules still apply. Police reporting commits independently without charging pending medicines. No medical data or save schema was changed.

FirstRunTutorial teaches one drag, then Done; removal/X and the repeated drag are omitted. The drag instruction is exactly `find the medicine and give it to patient.` TutorialOverlay loops a finger gesture from the medicine card to the current patient's visible head, using unscaled time and an Inspector-authored motion curve. The hand hides during real dragging; dragging opens the overlay's pointer area until release. Done retains tap guidance. The prior examination and Doctor Book steps retain their authored content and stable action values.

Validation of the simplified tutorial: 18 focused Play Mode checks passed using an isolated profile and the real tutorial controller. These covered the exact text, gesture source/midpoint/current-patient endpoint/repetition, UI raycasts, hiding the guide during an actual drag, staging medicine, advancing directly to Done and committing once. Rendered gesture and Done captures were inspected at 1080x1920. The hand renders above the caption throughout its path. The real player save remained unchanged; physical Android touch was not tested.

Validation for the multi-medicine revision: 107 focused Play Mode checks passed, including real UI raycasts, multiple staged medicines, duplicate prevention, close/outside/reopen, card/X removal and puff, error/retry, exact payment on Done, multi-disease correctness, thank-you/departure motion, pending-medicine police reporting, all four changed tutorial steps with the real FirstRunTutorial controller/overlay, and two consecutive day/result/checkpoint cycles. A separate 19-check portrait pass verified 720x1600, all ten mini-cards fitting, removal hit testing, compact height and retained selection on reopen. UI captures at 1080x1920 and 720x1600 were visually inspected. The Done button dims without becoming translucent when disabled or while dragging. Real player save hashes remained unchanged. Physical Android/touch and AAB export were not tested. Temporary authoring/verification scripts, captures and isolated saves were removed afterward; authoring backups remain under Logs/MedicineDraftBackup.
