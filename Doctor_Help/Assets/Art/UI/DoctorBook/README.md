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
