# Midnight at the Clinic — loop từ bản người dùng cung cấp

## Bản v2 — ưu tiên duyệt nghe bản này

- `MidnightAtTheClinic_Loop_v2.wav` và `Assets/Audio/Music/MidnightAtTheClinic_Loop_v2.ogg` là vòng **32 giây**, chọn motif tương đồng tại 7,375 và 39,375 giây trong nguồn. Mọi âm thanh vẫn nằm trong 40 giây đầu (kể cả vùng nối 7,315–39,435).
- Căn theo chu kỳ âm nhạc tương đồng 32 giây; tín hiệu hai đầu có tương quan khoảng 0,81. Nối ngắn 120 ms có bù năng lượng theo tương quan, thay vì chồng 2 giây. Không fade xuống im lặng, không đổi tốc độ hoặc cao độ.
- Cân độ lớn từ từ bằng RMS cửa sổ 2 giây và đường gain làm mềm 1 giây. Gain tương đối khoảng −3,75 tới +3,32 dB; RMS một giây đầu/cuối lệch khoảng 0,39 dB. Kiểm tra đủ frame và không clipping; chưa duyệt nghe trên thiết bị.
- `MidnightAtTheClinic_Loop_v2_2x_preview.wav`: hai vòng liên tiếp, điểm nối tại **00:32**, dùng để nghe kiểm tra.
- Nguồn xử lý: `make_loop_v2.py`; thông số: `Validation_v2.json`. Bản v1 bên dưới được giữ lại để so sánh.

## Đã áp dụng vào game

- Theo yêu cầu duyệt bản v2, master WAV v2 đã thay nội dung `Assets/Resources/Audio/ClinicMusic.wav`, giữ GUID cũ. `GameAudio` tiếp tục load `Audio/ClinicMusic`, AudioSource loop = true, spatialBlend = 0, dùng chung toàn game. Không đổi code hoặc mức MusicVolume người chơi đã chọn.
- Importer Streaming, Vorbis quality 0,7, stereo, không normalize, load in background. Bản nhạc cũ và cấu hình importer được lưu ở `Previous/` để khôi phục khi cần.
- Đối chiếu SHA-256 master v2 và WAV được dùng trong game khớp nhau. Chưa chạy Play Mode hoặc build Android cho lần thay nhạc này.

## Bản v1

- Nguồn: `C:/Users/nguye/Downloads/Midnight_at_the_Clinic.mp3`. Chỉ đọc vùng 00:00–00:40; giữ nguyên file gốc.
- Hai giây cuối chồng với hai giây đầu bằng equal-power crossfade có đường cong mềm. Chu kỳ thực tế **38 giây**; không thay tempo hoặc cao độ, không thêm âm thanh.
- Bản loop bắt đầu tại giây 2 của nguồn, chạy tới giây 38, rồi phần chồng cuối/đầu đưa trở lại giây 2. Không có fade về im lặng ở ranh giới vòng.
- Master: `MidnightAtTheClinic_Loop.wav`, stereo 44.1 kHz / PCM 24-bit.
- Unity: `Assets/Audio/Music/MidnightAtTheClinic_Loop.ogg`. Bật Loop trên AudioSource, spatialBlend = 0. Chưa thay nhạc hiện tại hoặc chỉnh scene.
- `make_loop.py` tái tạo kết quả; `Validation.json` ghi thông số kiểm tra kỹ thuật và hash nguồn. Việc kiểm tra frame count, clipping, điểm nối không thay thế duyệt nghe nhạc.
