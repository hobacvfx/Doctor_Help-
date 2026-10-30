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

- Flow hiện tại: `Preparation -> Examination -> TreatmentDecision -> PatientDeparture`, sau đó quay về Examination nếu còn thời gian hoạt động, hoặc sang `DayResults -> Preparation` khi đã đóng cửa. Nhánh lặp khai báo tập trung trong DayFlowPlan.
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

## Thời gian hoạt động và vòng bệnh nhân

- Bấm Open bắt đầu đồng hồ từ 08:00. 08:00–21:00 tương đương 720 giây (12 phút) thực. Đồng hồ hiển thị HH:mm ở góc trên phải, trong Safe Area của Office; UI có sẵn trong scene và lưu thành prefab để chỉnh.
- Đồng hồ tiếp tục chạy khi chọn thuốc và khi NPC bước vào/ra, không phụ thuộc Time.timeScale. App vào nền/mất focus thì tạm dừng; trở lại không cộng bù thời gian ở nền. Không lưu tiến trình đồng hồ giữa ngày.
- Đồng hồ dừng ở 21:00, không vượt quá. Không tự kết thúc phiên khám khi hết giờ. NPC đã bắt đầu bước vào bằng A_Walk_in thì phải được khám xong kể cả đã đến 21:00.
- Give chốt thao tác cho bệnh nhân hiện tại, đóng popup và dừng tương tác khám; NPC chạy A_Walk_Out một lần. Chờ animation hoàn tất và NPC cũ được hủy rồi mới nhận khách mới bằng A_Walk_in. Chỉ mở thao tác khám khi khách bước vào xong và đến NPC_Idle.
- Sau khi khách bước ra xong: nếu chưa đến 21:00 thì nhận NPC tiếp theo; nếu đã đến giờ thì chuyển sang kết quả ngày. Không nhận thêm khách khi đã đóng cửa. Mỗi lần spawn là phiên khám mới, kể cả trùng prefab; thân nhiệt giữ nguyên trong từng phiên.
- Chỉ lưu checkpoint/tăng ngày sau vị khách cuối cùng; các bệnh nhân ở giữa ngày không lưu hoặc tăng ngày. Báo công an chưa triển khai trong vòng này. Give hiện vẫn là thao tác placeholder, chưa tự thêm trừ thuốc, tính sống/chết hoặc tiền thưởng/phạt.

## UI, scene và prefab

- Canvas, EventSystem, FPS, button và popup phải thuộc scene, có sẵn trong Hierarchy trước Play. Không spawn UI dưới `DontDestroyOnLoad`.
- Home chứa UI chuẩn bị và kết quả; Office chứa UI khám và chọn thuốc. Đối tượng persistent chỉ giữ các service và trạng thái cần xuyên scene.
- Button và popup phải lưu thành prefab để người dùng chỉnh giao diện. Các prefab UI hiện ở `Assets/Resources/UI`; ưu tiên giữ liên kết nested prefab khi tái sử dụng button.
- Chỉnh bố cục, màu, sprite, font và chữ tĩnh trong prefab/scene; code chỉ kết nối hành động và cập nhật nội dung/trạng thái động cần thiết.
- Nội dung placeholder được phép theo yêu cầu thiết kế, nhưng phần triển khai vẫn phải có cấu trúc có thể bảo trì.

## Dữ liệu bệnh, triệu chứng và thuốc

- Ba ScriptableObject tại `Assets/Data/Medical`: `AllSickness.asset` quản lý bệnh và liên kết; `SymptomProfile.asset` quản lý triệu chứng; `PillProfile.asset` quản lý thuốc/phương pháp chữa trong sơ đồ thiết kế.
- Cho phép thêm, xóa và đặt tên trong Inspector. Mỗi bệnh có các slot triệu chứng và thuốc chọn từ hai profile nguồn; không nhập lại tên thành dữ liệu riêng trong từng bệnh.
- Dùng ID ổn định cho liên kết. Đổi tên hoặc sắp xếp không được làm đổi ID; không dùng tên hiển thị hay vị trí trong danh sách làm khóa. Xóa mục nguồn phải cảnh báo nếu đang được dùng, giữ liên kết thiếu hiển thị rõ để sửa, không tự gán sang mục khác.
- Bệnh dùng thuốc yêu cầu đủ TẤT CẢ loại thuốc đã khai báo, không phải chọn một thuốc thay thế. Slot biểu thị loại thuốc, chưa biểu thị số lượng/liều. Công thức rỗng, trùng ID, thiếu thuốc hoặc tham chiếu không hợp lệ không được tính là đủ thuốc.
- `SicknessTreatment` phân biệt `RequiredPills` và `ReportToPolice`. Zombie Infection và Vampire Infection phải báo cảnh sát, không có công thức thuốc; báo cảnh sát không phải một mục trong PillProfile.
- Bộ dữ liệu nhập từ sơ đồ hiện có 12 bệnh, 15 triệu chứng dùng chung và 9 loại thuốc/phương pháp chữa, đặt tên tiếng Anh. Chi tiết ánh xạ song ngữ ở `Assets/Data/Medical/README.md`; đây là dữ liệu thiết kế có thể chỉnh sửa, không phải số lượng cố định cho hệ thống.
- Giữ đúng ánh xạ trong sơ đồ game, kể cả Internal Organ Disease dùng Deworming Medicine, Flu dùng Antibiotics và Brain Rot dùng Science Book. Không tự sửa theo kiến thức y khoa thực tế. `Brain Abnormality (Tung Tung Tung)` hiện là triệu chứng riêng với `Brain Abnormality`.
- Không tự tái tạo dữ liệu mẫu khi khởi động hoặc ghi đè các chỉnh sửa Inspector của người dùng.
- Hiện đây là lớp dữ liệu và truy vấn điều kiện thuốc; chưa tích hợp chẩn đoán, trừ tồn kho hay kết quả bệnh nhân. `HasRequiredPills` chưa loại trường hợp cho thêm thuốc ngoài công thức; tác dụng của thuốc thừa chưa được chốt.
- Khi tích hợp gameplay, popup phải hỗ trợ cả bộ thuốc yêu cầu và quyết định báo cảnh sát. Việc trừ thuốc, tính kết quả và thưởng/phạt phải tuân thủ luật lưu cuối ngày; không coi truy vấn dữ liệu là giao dịch tồn kho.

## Chọn dụng cụ và mechanic khám bệnh

- Chỉ sử dụng dụng cụ trong bước Examination. Mỗi lần chỉ chọn một dụng cụ: bấm lại để bỏ chọn, bấm dụng cụ khác để chuyển lựa chọn. Khi rời bước khám bệnh phải bỏ chọn và dừng hiệu ứng/âm thanh.
- Dụng cụ đang chọn trên Desk có viền glow trắng mềm, nhấp nháy chậm và nhẹ; bật object con `Selected_Icon`. Bỏ chọn thì tắt cả hai. Độ dày, độ sáng và nhịp glow chỉnh được trong Inspector.
- Dùng `ExaminationToolSelection` quản lý chung lựa chọn và thao tác chuột/touch. Cú bấm chọn dụng cụ không đồng thời sử dụng mechanic; cần một lần giữ tiếp theo. Thao tác bắt đầu trên UI không được kích hoạt khám bệnh khi kéo sang bệnh nhân.
- X-ray: chọn `Desk/KinhLup`, sau đó giữ/kéo để soi; thả tay dừng soi nhưng giữ lựa chọn. Kính trên bàn và kính đi theo con trỏ là hai object khác nhau, dùng tham chiếu rõ ràng, không tìm bằng tên KinhLup.
- Nhiệt kế: chọn `Desk/NhietKe` như các dụng cụ khác (glow và Selected_Icon). Sau đó giữ chuột/touch ở bất kỳ đâu ngoài button để hiện `Mechanics/NhietKe`; thủy ngân tăng từ 33°C đến nhiệt độ đo được trong 2 giây. Thả tay, bỏ chọn, đổi dụng cụ hoặc rời bước khám thì ẩn và hủy lần đo; lần giữ mới bắt đầu lại từ 33°C.
- Thang nhiệt kế dùng tham chiếu trực tiếp Min = 33°C và Max = 43°C. Giữ khung, vị trí, bề rộng và hình bầu thủy ngân; chỉ thay đổi độ cao phần thủy ngân hiển thị, không kéo dãn cả sprite. Phân biệt object dụng cụ trên Desk với object hiển thị trong Mechanics.
- Triệu chứng gãy xương: BoneFractureAppearance đọc Bone Fracture qua ID triệu chứng của bệnh đang gán. Có triệu chứng thì đặt _BoneCrack = 1 và random độc lập X/Y của _Crack_Offset trong 0–1 một lần cho mỗi phiên khám; không có thì đặt _BoneCrack = 0 và offset về 0. Dùng tham chiếu Renderer Bone được lưu trong prefab và MaterialPropertyBlock riêng từng NPC, giữ các thuộc tính X-ray khác; không sửa shared material, không random lại khi soi/bật tắt. NPC mới tạo từ NPC_0 kế thừa component và liên kết này.
- Triệu chứng giun: WormInfestationAppearance đọc Worm Infestation qua ID triệu chứng của bệnh. Mỗi NPC chỉ sinh đúng một prefab được chọn ngẫu nhiên trong Giun_1/Giun_2/Giun_3 tại đúng một điểm Pos_* ngẫu nhiên dưới Giun, một lần mỗi phiên khám (áp dụng cả bệnh Giun và Zombie Infection khi có triệu chứng). Giữ nguyên layer, sorting, material, rotation và scale prefab; đặt localPosition = 0 tại điểm, không đổi vị trí điểm. Không random lại khi soi/bật tắt; phiên mới thay các instance đã sinh, NPC không có triệu chứng thì dọn giun. Chỉ dọn instance do component tạo, không xóa object người dùng đặt sẵn. NPC tạo từ NPC_0 kế thừa tham chiếu prefab và điểm.
- Triệu chứng nội tạng: OrganAbnormalityAppearance đọc Internal Organ Abnormality bằng ID triệu chứng của bệnh. Mỗi phiên khám chỉ sinh một bộ phận ngẫu nhiên: Than vào slot Than, Gan vào Gan, Da_Day vào Da_day dưới NoiTang. Lưu cặp prefab/Transform trực tiếp trong prefab NPC, không tìm theo tên lúc runtime. Đặt localPosition = 0, giữ rotation/scale/material/layer/sorting prefab. Giữ lựa chọn trong suốt phiên; không có triệu chứng thì dọn instance đã sinh, không xóa object có sẵn. NPC tạo từ NPC_0 kế thừa cấu hình này.
- Tim/Nao: SymptomPrefabAppearance trên NPC dùng cặp ID triệu chứng–prefab–slot lưu sẵn. Heart Abnormality sinh Tim vào Tim; Brain Abnormality sinh Nao vào Nao. Đây là các liên kết độc lập, không random giữa Tim/Nao; nếu có cả hai triệu chứng thì hiện cả hai. Không coi Brain Abnormality (Tung Tung Tung) là Brain Abnormality; Vampire Infection vẫn hiện Tim vì có Heart Abnormality. Slot Tim khác Heart dùng nghe nhịp tim. Giữ nguyên cấu hình prefab, localPosition = 0, không sinh lại khi đổi dụng cụ; phiên mới dọn các instance cũ, không xóa object có sẵn.
- Nghe tim: chọn `TaiNghe` thì bật `HeartCheck`, bỏ chọn thì tắt. Giữ nguyên vị trí HeartCheck đã đặt trong scene; không di chuyển nó theo con trỏ. Khi giữ ngoài UI mới bắt đầu phản hồi; thả tay dừng và dọn particle/âm thanh.
- Với bệnh nhân có nhịp tim, mỗi nhịp restart `VFX_HeartBeat`; nếu còn trong phạm vi nghe thì đồng thời restart `VFX_Sound` và phát `SFX_HeartBeat.mp3`. Đổi nhịp bằng khoảng cách giữa các lần trigger, không đổi pitch/tốc độ phát clip.
- Khoảng cách tính trong mặt phẳng world XY từ chuột/ngón tay đến Heart của bệnh nhân hiện tại. Hệ số tín hiệu giảm tuyến tính theo `Clamp01(1 - distance / 4)`: sát tim 100%, cách 2 đơn vị 50%, từ 4 đơn vị trở lên bằng 0. Khoảng cách tối đa và âm lượng tối đa chỉnh được trong Inspector.
- Hệ số trên áp dụng cho Start Size của `VFX_Sound` và âm lượng, cập nhật cả particle đã phát khi di chuyển con trỏ. Ngoài phạm vi không có sóng/tiếng; `VFX_HeartBeat` vẫn phản hồi với kích thước đã thiết kế, không giảm theo khoảng cách.
- Giữ cấu hình hình ảnh, material, lifetime và kích thước tối đa trong particle system do người dùng thiết kế. Tắt Play On Awake để không tự phát khi mở scene/chọn dụng cụ. Hiệu ứng phản hồi phải hiển thị phía trên sprite bệnh nhân.

## Hội thoại NPC và animation nói

- Mỗi bản ghi NpcProfile có slot NpcDialogueAsset (ScriptableObject .asset), nội dung TextArea nhiều dòng chỉnh trực tiếp trong Inspector của asset hoặc ngay trong NpcProfile. Có nút Tạo hội thoại mới và Chọn asset hội thoại; cũng tạo được qua Assets/Create/Doctor Help/Dialogue/NPC Dialogue. Không gán asset hoặc nội dung trống/toàn khoảng trắng thì NPC không nói và không mở ChatBox. Nhập NPC mới/tạo hàng loạt mặc định để trống slot; refresh không ghi đè asset của NPC cũ. NPC_0 đang gán Assets/Data/NPC/NPC_0_Dialogue.asset. Nội dung là dữ liệu do người dùng chỉnh; không tự xóa, ghi đè hoặc coi nó luôn rỗng.
- Chỉ bắt đầu hội thoại sau khi Walk_in kết thúc và animator đã về NPC_Idle. Chatbox_Open chạy trên layer Dialogue riêng, không thay animation idle/đi bộ; clip không loop và giữ tư thế cuối. Layer này bắt buộc dùng AvatarMask DialogueOnly chỉ chứa subtree ChatBox, loại trừ root/toàn thân; trọng số mặc định 0, chỉ bật 1 lúc mở/nói và trở về 0 khi dừng. Không để layer override rỗng trọng số 1 vì có thể ghi đè chuyển động Walk_in/Walk_out. Chạy chữ sau khi clip mở hoàn tất.
- Mỗi dòng trong ô nội dung là một lượt nói, kể cả dòng trống bên trong nội dung; newline cuối nội dung không tạo lượt trống thừa. Hỗ trợ LF/CRLF, nội dung là plain text. Text tự wrap/auto-size và mask trong vùng ChatBox, không coi wrap tự động là lượt mới. Text và chữ Next có Sorting Order -11; font, giới hạn cỡ chữ và khung chỉnh trong prefab. NpcDialogue có Fit In Viewport (mặc định bật), Maximum Viewport Width (mặc định 0,38) và Screen Padding (16 px), căn pose của ChatBox trong LateUpdate để không bị cắt ở Safe Area; có thể chỉnh/tắt để giữ bố cục thủ công.
- Tốc độ chữ mặc định 30 ký tự/giây, chỉnh trong Inspector. Next khi đang gõ thì hiện hết lượt hiện tại; lần bấm tiếp mới sang lượt sau. Hết lượt cuối thì Next quay lại đầu, không chạy lại animation mở hộp. Next dùng collider sẵn có, hỗ trợ chuột/touch và không đồng thời kích hoạt dụng cụ khám; popup UI phía trên chặn bấm xuyên.
- Khi đang chạy chữ, ẩn các part miệng trong Mouth và bật Speaking Lip với các ảnh trong 2D_Assets/Lip theo thứ tự số, lặp 10 FPS. Hết lượt/chờ Next thì tắt Lip và khôi phục trạng thái miệng ban đầu. Speaking Lip là sibling của Mouth để tool random ngoại hình không xóa nó.
- Vẫn được khám, chọn dụng cụ và mở popup thuốc khi NPC nói. Give xác nhận kết thúc phiên phải dừng chữ/môi, tắt ChatBox/Text/Next và khôi phục miệng trước Walk_out. Bỏ/hủy NPC phải dọn coroutine và đăng ký nút Next. Không có âm thanh hội thoại trong phạm vi hiện tại.

## Công cụ tạo và đồng bộ NPC

- Giữ tool authoring chính thức **Tools > NPC > Tạo NPC hàng loạt** cùng các custom Inspector, importer và HomePlayModeStart. Đây là công cụ phát triển đang dùng, không phải test/setup tạm để xóa khi dọn project.
- Tool lấy NPC_0 làm mẫu mặc định, seed bắt đầu 6, tên NPC_<seed>, tăng seed tuần tự theo số lượng; sau đợt thành công thì cửa sổ tự tăng seed bắt đầu. Giữ cấu trúc, nested prefab, Heart, animator và các component/tham chiếu triệu chứng/hội thoại từ mẫu. Seed quyết định ngoại hình, không đảm bảo các NPC đều có ngoại hình khác nhau.
- Mặc định bỏ Lock Override của các slot để áp ngoại hình theo seed mới; có tùy chọn giữ slot đã khóa. Kiểm tra toàn bộ dải tên trước khi tạo, không ghi đè prefab/.meta có sẵn. Chỉ coi GUID của asset còn tồn tại là trùng, không dùng GUID cache của asset đã xóa để chặn tạo lại tên.
- NPC mới được thêm vào NpcProfile bằng GUID mới, tên theo prefab và random đúng một bệnh từ AllSickness. Không đổi bệnh/tên/ID/hội thoại của bản ghi cũ; slot hội thoại của NPC mới để trống. Nếu chọn NpcCatalog thì cập nhật để các NPC mới có thể spawn. Hủy/lỗi trong đợt phải dọn asset vừa tạo và khôi phục profile/catalog của đợt đó.
- Nút **Refresh / cập nhật NPC** có trong NpcProfile Inspector, NpcCatalog Inspector và cửa sổ tạo hàng loạt: quét thư mục/subfolder, thêm bản ghi mới, giữ dữ liệu bản ghi cũ và dựng lại catalog từ toàn bộ prefab còn tồn tại trong profile, bỏ null/trùng. Import thường thêm NPC mới không bệnh; random bệnh chỉ thuộc thao tác tạo hàng loạt.
- Refresh thường giữ bản ghi mất prefab để người dùng kiểm tra. Chỉ nút **Xóa bản ghi mất prefab + cập nhật spawn** mới dọn các bản ghi đó; không xóa asset prefab. Prefab xóa rồi tạo lại cùng tên có GUID và bản ghi mới, không tự chuyển danh tính/bệnh/hội thoại theo tên. Dùng Undo cho các chỉnh sửa profile/catalog qua Inspector.

## Profile NPC, bệnh và triệu chứng

- Dùng `Assets/Data/NPC/NpcProfile.asset` làm nguồn tên và bệnh của NPC; `NpcAppearanceProfile` chỉ quản lý ngoại hình. Không tạo nguồn bệnh riêng cho mechanic nghe tim.
- Spawner hiện chọn prefab từ NpcCatalog, rồi liên kết đúng bản ghi NpcProfile bằng tham chiếu prefab/ID ổn định. `PatientExaminationProfile` trên mỗi prefab giữ tham chiếu Heart; không tìm Heart toàn scene hoặc suy ra danh tính từ tên NPC runtime.
- Đọc bệnh từ bản ghi NPC, đọc triệu chứng từ AllSickness bằng ID ổn định. Đổi tên/sắp xếp dữ liệu không được làm sai liên kết. Thay đổi bệnh/nhịp trong Play hiện được đọc ở lần giữ để nghe tiếp theo.
- NPC không có phân loại chủng tộc riêng. Profile có tên, một bệnh (hoặc không bệnh) và asset hội thoại NpcDialogueAsset tùy chọn để chỉnh; ID và tham chiếu prefab vẫn được giữ làm liên kết kỹ thuật. Zombie Infection và Vampire Infection là bệnh trong AllSickness, giữ nguyên tên và ID hiện có.
- Thân nhiệt: bình thường 37°C; triệu chứng High Temperature random 38,6–42°C; Low Temperature random 33–34,9°C. Mỗi phiên khám của một NPC chỉ lấy một nhiệt độ và giữ nguyên đến khi phiên khám kết thúc (cho thuốc hoặc báo công an, chuyển sang NPC tiếp theo). Đo lại, thả/giữ, đổi dụng cụ hoặc bật/tắt nhiệt kế không được random lại. Phiên khám mới được lấy giá trị mới trong đúng khoảng triệu chứng, kể cả khi dùng lại cùng prefab NPC. Nhiệt độ thuộc trạng thái runtime của phiên bệnh nhân, không ghi ngược vào NpcProfile hay save người chơi. Nhiệt độ chỉ phụ thuộc triệu chứng của bệnh đang gán.
- Nhịp tim chỉ phụ thuộc triệu chứng bệnh: mặc định 60 BPM, Rapid Heartbeat 180 BPM (gấp ba nhịp gốc), Slow Heartbeat 30 BPM (nửa nhịp gốc); No Heartbeat là 0 BPM và ưu tiên hơn triệu chứng nhanh/chậm. BPM gốc chỉnh được trong Inspector. Vampire Infection hiện có No Heartbeat nên không có nhịp tim; không có nhánh ưu tiên theo danh tính NPC.
- Khi không có nhịp tim, thao tác giữ để nghe chỉ hiện hình tim đứng yên: không restart VFX, không phát sự kiện nhịp lặp, không VFX_Sound, không tiếng. Hình tim vẫn hiện khi ở ngoài phạm vi nghe. Thả tay hoặc bỏ chọn dừng và dọn hiệu ứng; lần giữ mới hiển thị lại hình tim tĩnh.
- Các mechanic đọc cùng một bệnh từ NpcProfile và không tự đổi bệnh khi khám. Dữ liệu cũ đã được chuyển: Zombie sang Zombie Infection, Vampire sang Vampire Infection; không chạy lại việc chuyển này khi mở game.
- Liên kết bệnh/triệu chứng thiếu hoặc đồng thời có triệu chứng nhanh và chậm phải báo lỗi rõ; không âm thầm coi là nhịp bình thường.

## Tình trạng quyền sử dụng nhân vật tham chiếu

- Đã trao đổi về Tung Tung Tung Sahur, nhưng chưa xác minh quyền sử dụng hoặc có giấy phép cho project; người dùng chưa chốt sử dụng hay loại bỏ nhân vật này.
- Tên triệu chứng `Brain Abnormality (Tung Tung Tung)` trong dữ liệu không chứng minh quyền sử dụng nhân vật, hình ảnh hoặc âm thanh liên quan. Không suy diễn meme hoặc nội dung có AI hỗ trợ là được tự do dùng thương mại.
- Nếu tiếp tục đánh giá quyền sử dụng, kiểm tra nguồn và tình trạng pháp lý tại thời điểm đó; phân biệt tuyên bố của bên nhận sở hữu, đăng ký và kết luận của tòa án. Không ghi nhận project đã có giấy phép nếu chưa có bằng chứng.

## Tiêu chuẩn triển khai và kiểm chứng

- Không code tạm bợ. Ưu tiên trách nhiệm rõ ràng, tên có ý nghĩa, vòng đời đúng và khả năng mở rộng; tránh framework hoặc lớp trừu tượng không cần thiết.
- Không thêm hoặc giữ Editor tool/menu chỉ để thực hiện một thao tác setup một lần. Nếu cần script migration nội bộ, xóa script cùng .meta sau khi asset đã được lưu và xác minh. Dọn harness, launcher, fixture, ảnh/log/save test và getter/sự kiện chẩn đoán chỉ phục vụ test khi đã hết dùng; không xóa tool authoring đang dùng. Chỉ giữ bộ hồi quy khi có giá trị và còn được sử dụng. Giữ backup chuyển đổi dữ liệu, _Recovery và cache Unity có chủ đích, không coi chúng là code test thừa.
- Với scene/prefab hoặc nested reference phức tạp, ưu tiên Unity Editor/Unity API để tạo và lưu. Không tự ghép YAML dựa trên fileID hoặc font ID phỏng đoán.
- Khi sửa scene đang mở, bảo toàn chỉnh sửa của người dùng và kiểm tra trạng thái đã lưu thực tế trong Unity; không chỉ kiểm tra file trên đĩa rồi khẳng định Hierarchy đã đúng.
- Khi kiểm tra animation đi vào/ra, phải đo thay đổi vị trí root và kiểm tra ảnh render qua nhiều frame, không chỉ kiểm tra tên state hoặc normalizedTime. Lặp nhiều lượt Give/khách mới, cả có và không có hội thoại, để phát hiện layer hội thoại ghi đè chuyển động.
- Compile thành công hoặc tham chiếu khác null chưa đủ chứng minh UI hoạt động. Kiểm tra Button gắn với GameObject/Graphic thật, font hợp lệ, chữ không bị cắt/chồng, vị trí và trạng thái nút.
- Với thay đổi UI/flow, chạy thử hành vi liên quan trong Play Mode và kiểm tra ảnh render thực tế. Luồng chọn thuốc cần xác nhận popup mở ngay lần đầu, chọn được thuốc, Give hiển thị và hoạt động, rồi sang kết quả/ngày mới đúng luật.
- Kiểm tra mở lại popup và lặp vòng ngày để phát hiện listener trùng, lưu trùng và tăng ngày trùng. Chú ý dữ liệu runtime khi Unity reload code trong Play Mode.
- Báo đúng phạm vi đã kiểm chứng; phân biệt kiểm tra code, Edit Mode, Play Mode và bản build trên thiết bị. Không khẳng định đã kiểm thử phần chưa chạy.
- Chọn phạm vi kiểm thử theo thay đổi: sửa quy tắc dữ liệu thì ưu tiên kiểm tra Edit Mode tập trung; chỉ chạy lại bộ Play Mode rộng khi thay đổi hành vi/UI hoặc có rủi ro hồi quy liên quan. Không lặp kiểm tra đã đạt nếu không có thay đổi, lỗi hoặc nghi vấn mới; ưu tiên đọc log tóm tắt để tiết kiệm token.
- Chi tiết cấu trúc và hướng dẫn mở rộng: xem `INITIAL_SETUP.md`.

