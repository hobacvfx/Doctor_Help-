# Clockwork Clinic — bản nhạc nền thử đầu tiên

Nhạc không lời được viết riêng cho phòng khám Doctor Help: gothic Victorian, hơi tinh nghịch, phù hợp nghe nền liên tục. Đây là bản phối bằng nhạc cụ sample/MIDI, không phải thu dàn nhạc thật.

## Âm nhạc

- 96 BPM cố định, nhịp 3/4, D minor, 32 ô nhịp, đúng 60 giây.
- Harpsichord chạy nốt móc đơn đều; pizzicato và contrabass giữ nhịp waltz nhẹ. Clarinet mang giai điệu, bassoon đáp lại thấp, celesta điểm màu nhỏ.
- Bốn cụm 8 ô nhịp có biến tấu; phối khí và độ lớn tương đối giữ ổn định. Không intro/outro, đổi tempo, cao trào đột ngột, vocal, lời, choir, hoặc trống mô phỏng nhịp tim.
- Đoạn cuối nằm trên A7 và quay về D minor ở đầu vòng. Melody/hòa âm được viết mới; không chép hoặc lấy sample nhạc phim.

## Tham khảo đã nghiên cứu

1. [Danny Elfman: Corpse Bride](https://www.dannyelfman.com/elfman-burton/corpse-bride): lời tác giả về chất kể chuyện và ảnh hưởng sân khấu Gilbert & Sullivan trong các bài hát. Điều rút ra cho game là nét dí dỏm, có tính nhân vật, trong một thế giới gothic.
2. [Walt Disney Records: Frankenweenie soundtrack](https://www.prnewswire.com/news-releases/walt-disney-records-presents-frankenweenie-original-motion-picture-soundtrack-available-now-171412301.html): phần nhạc được mô tả chuyển giữa sự hồn nhiên/vui nghịch và yếu tố quái vật tối tăm. Bản game giữ sự kết hợp cảm xúc này trong một mức năng lượng ổn định, thay vì mô phỏng chuyển cảnh kịch tính của phim.

Nguồn trên là tài liệu tham khảo bằng văn bản. Nhịp 3/4, tempo và lựa chọn nhạc cụ ở bản này là quyết định phối khí riêng cho Doctor Help, không phải tuyên bố đây là phối khí chính xác của hai phim.

## File và sử dụng

- `ClockworkClinic_Loop.wav`: master stereo 48 kHz / PCM 24-bit, peak −3 dBFS.
- `../../../Assets/Audio/Music/ClockworkClinic_Loop.ogg`: bản Vorbis cho Unity, khoảng 0,95 MB; importer Streaming, stereo, không normalize. Bật Loop trên AudioSource; nhạc nền dùng spatialBlend = 0.
- `ClockworkClinic.mid`: 6 track nhạc cụ chỉnh sửa được, một tempo event, có marker LOOP_START/LOOP_END.
- `compose.py`: nguồn giai điệu, hòa âm, xuất MIDI và render; chạy từ project bằng Python với numpy/mido/soundfile. Công cụ và soundfont đặt ở Library/MusicTools, không vào build.
- `Validation.json`: độ dài, frame count, peak/RMS, biên độ nối và RMS từng ô nhịp. Bản render được tạo bằng ba vòng liên tục, giữ vòng giữa để đuôi nhạc/reverb có sẵn ở đầu vòng, không fade-in/out. Cả WAV và OGG có đúng 2.880.000 sample frame. Kiểm tra kỹ thuật không thay thế duyệt nghe trên thiết bị.

Chưa thay bài `Resources/Audio/ClinicMusic.wav` hoặc gán bản mới vào scene; đây là bản để duyệt nghe.

## Công cụ và nguồn âm

FluidSynth 2.6.3; GeneralUser GS 2.0.3 của S. Christian Collins. Giấy phép nguồn âm cho phép sử dụng bản thu được render trong sản phẩm thương mại; xem `GeneralUser-LICENSE.txt` đính kèm. Chỉ bản nhạc render được thêm vào Assets, không đóng gói soundfont hay DLL.

- https://github.com/FluidSynth/fluidsynth
- https://www.schristiancollins.com/generaluser.php
- https://github.com/mrbumpy409/GeneralUser-GS
