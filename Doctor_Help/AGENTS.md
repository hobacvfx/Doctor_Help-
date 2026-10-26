# Định hướng game đã thống nhất

## Thể loại, hình ảnh và bối cảnh

- Game mobile giả lập bác sĩ, đồ họa 2D, màn hình dọc.
- Phong cách vẽ hơi spooky, mang phong cách Tim Burton.
- Bối cảnh London, Anh, thời Victoria trong một thế giới giả tưởng.

## Gameplay và vòng lặp mỗi ngày

- Người chơi vào vai một bác sĩ. Các bước trong ngày thực hiện theo thứ tự:
  1. Chuẩn bị mở cửa: mua thuốc, sửa chữa dụng cụ.
  2. Mở cửa khám bệnh: bắt đầu khám, với các thao tác như soi X-ray, nghe nhịp tim, đọc lời khai, đo nhiệt độ.
  3. Cho thuốc hoặc báo cảnh sát: một số bệnh nhân nhiễm bệnh vampire hoặc zombie phải được báo cảnh sát thay vì cho thuốc. Nếu không báo cảnh sát, người chơi bị trừ tiền trong đêm đó.
  4. Xem kết quả cuối ngày: sau khi hoàn thành các bước, ngày kết thúc, chạy cutscene kết quả và hiển thị kết quả màn chơi của ngày đó.
- Các bước được chia vào hai scene:
  - Scene Home: chuẩn bị mở cửa (mua thuốc, sửa chữa dụng cụ), cutscene và kết quả cuối ngày. Việc mua thuốc được thực hiện qua popup store mở trong scene Home.
  - Scene Office: khám bệnh, cho thuốc và báo cảnh sát.
- Sau kết quả cuối ngày, quay lại bước chuẩn bị mở cửa cho ngày tiếp theo.
- Game tiếp tục cho đến khi người chơi nợ quá nhiều tiền và phá sản; khi đó phải chơi lại từ đầu.

## Phát hành và dữ liệu người chơi

- Phát hành trên Google Play Store, export thành file `.aab`.
- Có player profile được load mỗi lần mở game và chỉ lưu khi hoàn thành ngày, theo luật lưu bên dưới.
- Có quảng cáo, liên kết Firebase, AdMob để chạy UA sau khi phát hành.

## Phạm vi hội thoại cập nhật thiết kế

- Hội thoại nơi người dùng cung cấp các nội dung trên chỉ dành cho cập nhật lối chơi, game design, rules và định hướng phát triển project.
- Trong hội thoại cập nhật thiết kế đó, không tự viết code hay triển khai các tính năng chỉ được trao đổi về thiết kế. Quy định này không cấm triển khai khi người dùng yêu cầu rõ ràng trong các hội thoại phát triển sau đó.

## Cấu hình nền tảng đã chốt

- Unity hiện tại: `6000.3.9f1`; Android package name: `com.doctorhelp.game`.
- Khóa Portrait, không tự xoay màn hình.
- Khi test hiệu năng: tắt VSync, đặt mục tiêu 200 FPS và hiển thị FPS cùng thời gian mỗi frame trên Canvas, trong vùng Safe Area. FPS thực tế phụ thuộc thiết bị và tần số quét; cần đánh giá lại cấu hình test trước khi phát hành.

## Player profile và luật lưu game

- Dữ liệu cơ bản: tiền, tồn kho thuốc theo `medicineId`, ngày hiện tại, tổng số bệnh nhân đã chết và đã cứu.
- Mặc định hiện tại: tiền 0, ngày 1, số người chết/cứu 0, tồn kho rỗng. Thuốc chưa có trong kho được tính là số lượng 0. Đây là giá trị khởi tạo, không phải quyết định cân bằng kinh tế cuối cùng.
- Tiền có thể âm để thể hiện nợ; số lượng thuốc và số bệnh nhân không được âm.
- Mỗi lần bắt đầu phiên chơi, kể cả Editor Play Mode, phải vào Home ở bước Preparation. Không khôi phục bước giữa ngày.
- Thay đổi trong ngày chỉ nằm trong bộ nhớ. Không lưu khi sửa giá trị profile, vào nền, mất focus, thoát app hoặc dừng Play Mode.
- Cuối ngày mới chốt toàn bộ profile và lưu số ngày kế tiếp. Ví dụ: hết ngày 1 lưu checkpoint cho đầu ngày 2.
- Thoát khi ngày chưa hoàn thành thì mất tiến trình giữa ngày; mở lại bắt đầu từ đầu ngày của checkpoint gần nhất.
- Lưu thất bại không được tăng ngày. Retry sau khi lưu thành công nhưng chuyển scene thất bại không được lưu hoặc tăng ngày lần nữa.
- Giữ cơ chế backup/recovery; không âm thầm ghi đè dữ liệu bị lỗi hoặc dữ liệu có schema mới hơn bằng profile trắng.
- Kiểm thử phải dùng dữ liệu riêng, không thay đổi save thật của người chơi.

## Kiến trúc và mở rộng gameplay flow

- Flow hiện tại: `Preparation -> Examination -> TreatmentDecision -> DayResults -> Preparation`.
- Dùng state machine quản lý chuyển bước tập trung. Mỗi state có `EnterAsync/Exit`, chịu trách nhiệm đăng ký và dọn sự kiện của chính nó.
- Đặt tên state theo ý nghĩa gameplay. Chọn thuốc là thao tác UI bên trong TreatmentDecision; bước này cũng là nơi mở rộng quyết định báo cảnh sát.
- Thứ tự bước khai báo trong `DayFlowPlan` tại `DailyGameLoop`; không suy ra thứ tự từ giá trị enum và không gọi thẳng bước kế tiếp từ UI.
- Khi thêm bước: thêm identifier, triển khai state, đăng ký và chèn vào plan, bổ sung UI nếu cần và kiểm tra vòng đời/chuyển bước/retry.
- Khi bỏ bước tùy chọn: bỏ khỏi plan và đăng ký tương ứng. Preparation và DayResults là hai ranh giới bắt buộc để giữ luật khởi động và lưu ngày.
- Tách trách nhiệm quản lý bước, chuyển scene, spawn NPC, UI và lưu profile. Không dồn tất cả vào một MonoBehaviour.
- Chặn chuyển bước trùng; lỗi phải được báo rõ và cho retry khi có UI hợp lệ. Hủy tác vụ và tháo listener khi đối tượng bị hủy.
- Bước kết quả kết thúc bằng sự kiện hoàn tất trình bày. Timer 3 giây hiện chỉ là placeholder của phần trình bày; khi gắn cutscene/UI kết quả thật, thay bằng tín hiệu hoàn tất tương ứng.
- Phép tính tiền thưởng/phạt trong đêm phải hoàn tất trước checkpoint và chỉ được áp dụng một lần cho ngày kết thúc.
- Spawn NPC qua component `PatientSpawnPoint` trên `NPC_Position`, không phụ thuộc vào tên object để tìm điểm spawn.

## UI, scene và prefab

- Canvas, EventSystem, FPS, button và popup phải thuộc scene, có sẵn trong Hierarchy trước Play. Không spawn UI dưới `DontDestroyOnLoad`.
- Home chứa UI chuẩn bị và kết quả; Office chứa UI khám và chọn thuốc. Đối tượng persistent chỉ giữ các service và trạng thái cần xuyên scene.
- Button và popup phải lưu thành prefab để người dùng chỉnh giao diện. Các prefab UI hiện ở `Assets/Resources/UI`; ưu tiên giữ liên kết nested prefab khi tái sử dụng button.
- Chỉnh bố cục, màu, sprite, font và chữ tĩnh trong prefab/scene; code chỉ kết nối hành động và cập nhật nội dung/trạng thái động cần thiết.
- Nội dung placeholder được phép theo yêu cầu thiết kế, nhưng phần triển khai vẫn phải có cấu trúc có thể bảo trì.

## Tiêu chuẩn triển khai và kiểm chứng

- Không code tạm bợ. Ưu tiên trách nhiệm rõ ràng, tên có ý nghĩa, vòng đời đúng và khả năng mở rộng; tránh framework hoặc lớp trừu tượng không cần thiết.
- Không thêm hoặc giữ Editor tool/menu chỉ để thực hiện một thao tác setup một lần. Nếu cần script migration nội bộ, xóa sau khi asset đã được lưu và xác minh; có thể giữ kiểm tra hồi quy có giá trị.
- Với scene/prefab hoặc nested reference phức tạp, ưu tiên Unity Editor/Unity API để tạo và lưu. Không tự ghép YAML dựa trên fileID hoặc font ID phỏng đoán.
- Khi sửa scene đang mở, bảo toàn chỉnh sửa của người dùng và kiểm tra trạng thái đã lưu thực tế trong Unity; không chỉ kiểm tra file trên đĩa rồi khẳng định Hierarchy đã đúng.
- Compile thành công hoặc tham chiếu khác null chưa đủ chứng minh UI hoạt động. Kiểm tra Button gắn với GameObject/Graphic thật, font hợp lệ, chữ không bị cắt/chồng, vị trí và trạng thái nút.
- Với thay đổi UI/flow, chạy thử hành vi liên quan trong Play Mode và kiểm tra ảnh render thực tế. Luồng chọn thuốc cần xác nhận popup mở ngay lần đầu, chọn được thuốc, Give hiển thị và hoạt động, rồi sang kết quả/ngày mới đúng luật.
- Kiểm tra mở lại popup và lặp vòng ngày để phát hiện listener trùng, lưu trùng và tăng ngày trùng. Chú ý dữ liệu runtime khi Unity reload code trong Play Mode.
- Báo đúng phạm vi đã kiểm chứng; phân biệt kiểm tra code, Edit Mode, Play Mode và bản build trên thiết bị. Không khẳng định đã kiểm thử phần chưa chạy.
- Chi tiết cấu trúc và hướng dẫn mở rộng: xem `INITIAL_SETUP.md`.
