# Định hướng game đã thống nhất

## Thể loại, hình ảnh và bối cảnh

- Trước khi tạo/chỉnh art (NPC, thuốc, icon triệu chứng hoặc UI minh họa), đọc `Design/Art/STYLE_GUIDE.md` và các ảnh mẫu được dẫn trong đó. File này lưu chuẩn render và các ràng buộc ghép sprite đã thống nhất để dùng giữa các hội thoại.

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
- Có player profile được load mỗi lần mở game và lưu theo luật lưu bên dưới.
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
- Mặc định hiện tại: tiền 200, ngày 1, số người chết/cứu 0, tồn kho rỗng. Thuốc chưa có trong kho được tính là số lượng 0. Đây là giá trị khởi tạo, không phải quyết định cân bằng kinh tế cuối cùng.
- Tiền có thể âm để thể hiện nợ; số lượng thuốc và số bệnh nhân không được âm.
- Give thuốc vẫn được chấp nhận khi không đủ vàng: tiền thuốc bị trừ thẳng và vàng có thể xuống âm. Mua nội thất/vật phẩm trong shop vẫn cần đủ vàng.
- Mỗi lần bắt đầu phiên chơi, kể cả Editor Play Mode, phải vào Home ở bước Preparation. Không khôi phục bước giữa ngày.
- Thay đổi trong ngày chỉ nằm trong bộ nhớ. Không lưu khi sửa giá trị profile, vào nền, mất focus, thoát app hoặc dừng Play Mode.
- Ngoài ngày làm việc (Player_Home, Street, phòng khám lân cận trước khi bấm Open), mọi thay đổi profile được lưu trong vòng 1 giây sau khi xảy ra (vàng, mua/trang bị nội thất, dọn dẹp, quest...). Thời gian chơi dùng cho vàng/phút không tự kích hoạt lưu; nó đi kèm các lần lưu khác và được lưu khi app pause/mất focus/thoát.
- Tối ưu lưu cho mobile: thay đổi chỉ đánh dấu cần lưu, gộp thành một lần lưu sau 1 giây (lỗi ghi thì thử lại sau 5 giây). Autosave tạo JSON gọn (không prettyPrint) trên main thread rồi ghi file ở worker thread, chỉ giữ bản mới nhất khi đang ghi; không chép file .bak. App pause/mất focus/thoát thì ghi đồng bộ ngay một lần (Pause và Focus cùng gọi không lưu trùng). Checkpoint cuối ngày ghi đồng bộ, chờ worker xong trước và mới xoay vòng file .bak.
- Bấm Open lưu profile một lần rồi mở ngày làm việc; từ đó đến khi chốt báo cuối ngày, thay đổi chỉ giữ trong RAM. Thoát giữa ngày làm việc thì mở lại quay về đúng trạng thái lúc bấm Open của ngày đó.
- Cuối ngày chốt toàn bộ profile và lưu số ngày kế tiếp. Ví dụ: hết ngày 1 lưu checkpoint cho đầu ngày 2.
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
- Bước kết quả chờ video Home chạy hết qua VideoPlayer.loopPointReached; không còn timer 3 giây hoặc text placeholder. Kết quả được đối chiếu trước checkpoint, cache để retry scene/video không lưu hoặc tăng ngày lại.
- Phép tính tiền thưởng/phạt trong đêm phải hoàn tất trước checkpoint và chỉ được áp dụng một lần cho ngày kết thúc.
- Spawn NPC qua component `PatientSpawnPoint` trên `NPC_Position`, không phụ thuộc vào tên object để tìm điểm spawn.

## Thời gian hoạt động và vòng bệnh nhân

- Bấm Open bắt đầu đồng hồ từ 08:00. 08:00–21:00 tương đương 240 giây (4 phút) thực. Đồng hồ hiển thị HH:mm ở góc trên phải, trong Safe Area của Office; UI có sẵn trong scene và lưu thành prefab để chỉnh.
- Đồng hồ tiếp tục chạy khi chọn thuốc và khi NPC bước vào/ra, không phụ thuộc Time.timeScale. App vào nền/mất focus thì tạm dừng; trở lại không cộng bù thời gian ở nền. Không lưu tiến trình đồng hồ giữa ngày.
- Đồng hồ dừng ở 21:00, không vượt quá. Không tự kết thúc phiên khám khi hết giờ. NPC đã bắt đầu bước vào bằng A_Walk_in thì phải được khám xong kể cả đã đến 21:00.
- Give chốt thao tác cho bệnh nhân hiện tại, đóng popup và dừng tương tác khám; NPC chạy A_Walk_Out một lần. Chờ animation hoàn tất và NPC cũ được hủy rồi mới nhận khách mới bằng A_Walk_in. Chỉ mở thao tác khám khi khách bước vào xong và đến NPC_Idle.
- Sau khi khách bước ra xong: nếu chưa đến 21:00 thì nhận NPC tiếp theo; nếu đã đến giờ thì chuyển sang kết quả ngày. Không nhận thêm khách khi đã đóng cửa. Mỗi lần spawn là phiên khám mới, kể cả trùng prefab; thân nhiệt giữ nguyên trong từng phiên.
- Chỉ lưu checkpoint/tăng ngày sau vị khách cuối cùng; các bệnh nhân ở giữa ngày không lưu hoặc tăng ngày. Báo cảnh sát đã triển khai qua điện thoại trong Desk, dùng chung TreatmentDecision và vòng PatientDeparture với Give. Give trừ 1 đơn vị mỗi thuốc và ghi lịch sử; cuối ngày đã đối chiếu đúng/sai để chọn video. Chưa tính sống/chết hoặc tiền thưởng/phạt.

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
- Lớp dữ liệu và truy vấn điều kiện thuốc không tự xử lý giao dịch; trừ kho nằm trong PlayerProfileSession/Service. Chưa tích hợp chẩn đoán hay kết quả bệnh nhân. Thuốc thừa được phép: chỉ cần đủ toàn bộ thuốc yêu cầu của các bệnh. TreatmentEvaluator đối chiếu nhiều bệnh cuối ngày; HasRequiredPills vẫn chỉ là truy vấn một bệnh.
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
- Triệu chứng nội tạng: OrganAbnormalityAppearance đọc Internal Organ Abnormality bằng ID triệu chứng. Mỗi phiên khám chọn ngẫu nhiên một trong bốn prefab với xác suất bằng nhau. Than vào slot Than, Gan và Gan_Than cùng dùng slot Gan, Da_Day vào slot Da_day dưới NoiTang. Lưu cặp prefab/Transform trực tiếp trong prefab NPC. Đặt localPosition = 0 và giữ rotation, scale, material, layer, sorting. Giữ lựa chọn trong suốt phiên. Chỉ dọn instance đã sinh khi khởi tạo lại. NPC tạo từ NPC_0 kế thừa cấu hình này.
- Tim/Nao: SymptomPrefabAppearance dùng cặp ID triệu chứng–prefab–slot lưu sẵn. Heart Abnormality sinh Tim vào Tim, Brain Abnormality sinh Nao vào Nao, Brain Abnormality (Tung Tung Tung) của Brain Rot sinh Nao_BrainRot vào Nao. Các slot khác nhau hiển thị đồng thời. Nếu cả hai triệu chứng não có mặt, hai hình ở slot Nao luân phiên mỗi giây. Slot Tim khác Heart dùng nghe nhịp tim. Giữ cấu hình prefab và localPosition = 0, tiếp tục cùng phiên khi đổi dụng cụ và chỉ dọn các instance đã sinh khi khởi tạo lại.
- Nghe tim: chọn `TaiNghe` thì bật `HeartCheck`, bỏ chọn thì tắt. Giữ nguyên vị trí HeartCheck đã đặt trong scene; không di chuyển nó theo con trỏ. Khi giữ ngoài UI mới bắt đầu phản hồi; thả tay dừng và dọn particle/âm thanh.
- Với bệnh nhân có nhịp tim, mỗi nhịp restart `VFX_HeartBeat`; nếu còn trong phạm vi nghe thì đồng thời restart `VFX_Sound` và phát `SFX_HeartBeat.mp3`. Đổi nhịp bằng khoảng cách giữa các lần trigger, không đổi pitch/tốc độ phát clip.
- Khoảng cách tính trong mặt phẳng world XY từ chuột/ngón tay đến Heart của bệnh nhân hiện tại. Hệ số tín hiệu giảm tuyến tính theo `Clamp01(1 - distance / 4)`: sát tim 100%, cách 2 đơn vị 50%, từ 4 đơn vị trở lên bằng 0. Khoảng cách tối đa và âm lượng tối đa chỉnh được trong Inspector.
- Hệ số trên áp dụng cho Start Size của `VFX_Sound` và âm lượng, cập nhật cả particle đã phát khi di chuyển con trỏ. Ngoài phạm vi không có sóng/tiếng; `VFX_HeartBeat` vẫn phản hồi với kích thước đã thiết kế, không giảm theo khoảng cách.
- Giữ cấu hình hình ảnh, material, lifetime và kích thước tối đa trong particle system do người dùng thiết kế. Tắt Play On Awake để không tự phát khi mở scene/chọn dụng cụ. Hiệu ứng phản hồi phải hiển thị phía trên sprite bệnh nhân.

## Hội thoại NPC và animation nói

- Mỗi bản ghi NpcProfile có slot NpcDialogueAsset (ScriptableObject .asset), nội dung TextArea nhiều dòng chỉnh trực tiếp trong Inspector của asset hoặc ngay trong NpcProfile. Có nút Tạo hội thoại mới và Chọn asset hội thoại; cũng tạo được qua Assets/Create/Doctor Help/Dialogue/NPC Dialogue. Không gán asset hoặc nội dung trống/toàn khoảng trắng thì NPC không nói và không mở ChatBox. Nhập NPC mới/tạo hàng loạt mặc định để trống slot; refresh không ghi đè asset của NPC cũ. NPC_0 đang gán Assets/Data/NPC/NPC_0_Dialogue.asset. Nội dung là dữ liệu do người dùng chỉnh; không tự xóa, ghi đè hoặc coi nó luôn rỗng.
- Trong phòng khám, chỉ bắt đầu hội thoại sau khi Walk_in kết thúc và animator đã về NPC_Idle. ChatBox nằm cạnh All ở root của prefab. NpcDialogue chạy clip A_NPC_Chatbox trên riêng ChatBox, giữ frame cuối rồi mới chạy chữ. Animator của All chỉ điều khiển chuyển động nhân vật.
- Mỗi dòng trong ô nội dung là một lượt nói, kể cả dòng trống bên trong nội dung; newline cuối nội dung không tạo lượt trống thừa. Hỗ trợ LF/CRLF, nội dung là plain text. Text tự wrap/auto-size và mask trong vùng ChatBox, không coi wrap tự động là lượt mới. Text và chữ Next có Sorting Order -11; font, giới hạn cỡ chữ và khung chỉnh trong prefab. NpcDialogue có Fit In Viewport (mặc định bật), Maximum Viewport Width (mặc định 0,38) và Screen Padding (16 px), căn pose của ChatBox trong LateUpdate để không bị cắt ở Safe Area; có thể chỉnh/tắt để giữ bố cục thủ công.
- Khi khám bệnh, NPC mắc Brain Rot thỉnh thoảng chen một cụm âm tiết vô nghĩa tự viết vào giữa lượt nói có đủ từ. Tạo lời chen một lần mỗi phiên khám, giữ nguyên khi bấm Next quay vòng và giữ nội dung gốc của NpcDialogueAsset. NPC thiếu hội thoại vẫn theo quy tắc hội thoại trống.
- Tốc độ chữ mặc định 30 ký tự/giây, chỉnh trong Inspector. Next khi đang gõ thì hiện hết lượt hiện tại; lần bấm tiếp mới sang lượt sau. Hết lượt cuối thì Next quay lại đầu, không chạy lại animation mở hộp. Next dùng collider sẵn có, hỗ trợ chuột/touch và không đồng thời kích hoạt dụng cụ khám; popup UI phía trên chặn bấm xuyên.
- Khi đang chạy chữ, ẩn các part miệng trong Mouth và bật Speaking Lip với các ảnh trong 2D_Assets/Lip theo thứ tự số, lặp 10 FPS. Hết lượt/chờ Next thì tắt Lip và khôi phục trạng thái miệng ban đầu. Speaking Lip là sibling của Mouth để tool random ngoại hình không xóa nó.
- Vẫn được khám, chọn dụng cụ và mở popup thuốc khi NPC nói. Chốt Give hoặc Report phải tắt Text/Next và khôi phục miệng ngay, chạy ngược A_NPC_Chatbox đồng thời với Walk_out rồi tắt ChatBox. Bỏ/hủy NPC phải dọn trạng thái thoại và đăng ký nút Next. Không có âm thanh hội thoại trong phạm vi hiện tại.
- Trong Player_Home, hội thoại dùng một câu cho mỗi lượt, mở hộp, chạy chữ, giữ 5 giây sau khi chữ hiện đủ rồi xóa chữ, chạy ngược clip và tắt hộp. Hộp thoại giữ vị trí cạnh người nói trong scene. Chế độ này không hiện Next. Nội dung theo hoàn cảnh của NPC thường sẽ được triển khai riêng. Lễ tân chọn ngẫu nhiên một trong ba câu chào khi NPC đi bộ vào vùng quầy cùng tầng. Khi Player đi bộ vào vùng quầy cùng tầng trong Player_Home, lễ tân chào `Morning Doctor!`; lễ tân ở các phòng khám lân cận không chào Player. Ghost không kích hoạt lời chào. Khi một Ghost đi xuyên qua vị trí của NPC cùng tầng, NPC đó nói `Why it so cold?`. Khi thang máy chở một Ghost mà Player không đi cùng, các NPC ở cả hai tầng đứng cách trục thang máy tối đa 4 đơn vị (chỉnh trong Inspector) nói `The elevator move it self!`, mỗi chuyến một lần. NPC đang nói thì giữ lượt hiện tại. Áp dụng cho Player_Home và các phòng khám lân cận vì dùng chung logic. Nếu lễ tân đang nói thì giữ lượt hiện tại.

## Street và phòng khám lân cận

- Các cửa được gán trong Inspector qua NeighborhoodView và dùng DeskActionButton trên prefab. Cửa Player_Home dẫn ra Street. Street có cửa về Player_Home và tới Lantern_Surgery, Ashbury_Clinic, Bramwell_Clinic, Ivy_Clinic, Crown_Clinic. Các phòng khám lân cận dùng cửa Street để quay ra. Nút về Home/Preparation đặt riêng trong Player_Home.
- Khi vào scene đi bộ, đặt người chơi tại điểm Entrance Spawn và đưa camera tới vị trí đó ngay. Street có điểm đến riêng ở ngoài từng cửa, chọn theo scene vừa rời. Các điểm xuất hiện chỉnh được trong Hierarchy và gán qua NeighborhoodView.
- Di chuyển qua khu phố thuộc bước Preparation, không mở đồng hồ khám hay tạo checkpoint. Giữ điều khiển PlayerHomeMovement trong Street và các phòng khám. Street có một tầng đi bộ, mặc định 10 NPC thường.
- Mỗi prefab NPC thường chỉ được chọn một lần trong mỗi lần tải scene. Khi hết danh sách, giữ số NPC tối đa có thể tạo từ danh sách còn lại. Tải lại scene thì chọn danh sách mới. Ghost dùng số lượng riêng của scene hoặc profile.
- Các phòng khám lân cận dùng chung logic NPC, thang máy, lễ tân và Ghost với Player_Home. Số Ghost cố định trong Inspector theo độ sang, mặc định Lantern 10, Ashbury 8, Bramwell 5, Ivy 2 và Crown 0. Player_Home đọc số Ghost từ profile nhưng hiện tối đa 15 Ghost; profile vẫn đếm đủ số Ghost thật.
- Số khách ở phòng khám lân cận nội suy tuyến tính từ 2 đến 20 theo Clinic Luxury (0–1), làm tròn đến số nguyên gần nhất và làm tròn lên khi đúng nửa. Mặc định Lantern 2, Ashbury 7, Bramwell 11, Ivy 16 và Crown 20. Số khách ở Player_Home dựa vào Rating.
- Rating của phòng khám người chơi = (5 × 3,5 + tổng rating các ngày đã chấm) / (5 + số ngày đã chấm), tức coi như có 5 ngày 3,5 sao trước Day 1. Profile mới bắt đầu ở 3,5 sao. Rating của một ngày là trung bình sao của các bệnh nhân không phải ca cảnh sát.
- Sau mỗi bệnh nhân rời Office, có (20 − 10X)% + (50 / Rating)% khả năng phải chờ thêm 0–2 giây cộng 8 / Rating giây trừ Y giây trước khi bệnh nhân kế tiếp vào; tỉ lệ được giới hạn trong 0–100%, thời gian chờ không âm. X, Y theo cấp biển quảng cáo: Lv1 X=0 Y=0, Lv2 X=3 Y=1, Lv3 X=4 Y=2, Lv4 X=5 Y=3, Lv5 X=7 Y=4. Rating dưới 1 (chỉ có khi sửa debug) được tính là 1.
- Số bệnh nhân tối đa vào Office mỗi ngày = 1 + 3,5 × (Rating − 1), làm tròn đến số nguyên gần nhất (1 sao 1 khách, 3,5 sao 10 khách, 5 sao 15 khách), cộng thêm theo biển quảng cáo: Lv1 +0, Lv2 +1, Lv3 +2, Lv4 +3, Lv5 +4. Khi bệnh nhân cuối trong giới hạn rời đi, Office hiện `Maximum client reached, increase rating to get more client.` cùng dòng `Day end in N` đếm ngược 15 giây, rồi kết thúc ngày và chạy cutscene như khi hết giờ. Báo cuối ngày của ngày đó hiện thêm dòng `Patient limit reached! (low rating)`.
- Khi mở cửa phòng khám, đếm số Ghost hiện ở Player_Home (tối đa 15). Mỗi Ghost trừ 0,25 sao vào đánh giá của mỗi bệnh nhân trong ngày (cộng dồn với phạt phòng bẩn, đánh giá tối thiểu vẫn là 1 sao). Nếu có bệnh nhân bị trừ sao vì lý do này, báo cuối ngày hiện dòng `The clinic is cold! (ghost)`.
- Vết bẩn trên tường Player_Home: hai vết cùng tầng phải cách nhau (tâm tới tâm) ít nhất 1 + `minimumStainGap` lần bề rộng vết (mặc định 1,5 lần, tức còn nửa vết tường trống ở giữa), áp dụng cho mọi vết đang hiện kể cả của khách khác. Không còn chỗ thì vết đó chưa hiện; khách bẩn chưa để lại vết vẫn tính vào độ bẩn như cũ.
- Treasure hunt (soi X-ray ở Office): mỗi phiên khám roll một lần, 70% có kho báu (`chance` trên TreasureHunt).
- Các dòng cảnh báo của báo cuối ngày dùng chung font, cỡ chữ, màu và kiểu chữ với dòng `The clinic is too dirty.`; xếp từ trên xuống theo thứ tự phòng bẩn, ghost, giới hạn bệnh nhân, chỉ hiện các dòng có xảy ra, danh sách bệnh nhân bắt đầu dưới dòng cuối cùng.
- Hiệu ứng nội thất theo description trong shop; mỗi cấp chỉ có các hiệu ứng ghi trong description của cấp đó (không cộng dồn các cấp dưới), hiệu ứng của các món khác nhau cộng với nhau. Bảng số liệu nằm trong `ClinicUpgrades.cs`.
  - Quầy tiếp tân: Lv1 +0,25 sao mỗi đánh giá và +5% vàng kinh doanh; Lv2 +0,5 sao và +10% thời gian kiên nhẫn; Lv3 +0,75 sao và +15 vàng/phút; Lv4 +1 sao và +20% kiên nhẫn; Lv5 +1,5 sao.
  - Lễ tân: Lv1 +20% vàng kinh doanh và +10% kiên nhẫn; Lv2 +40% vàng và +0,5 sao; Lv3 +60% vàng và +20 vàng/phút; Lv4 +80% vàng và +1,5 sao; Lv5 +120% vàng, +60 vàng/phút và +20% kiên nhẫn.
  - Thang máy: LvN +10N% thời gian kiên nhẫn của bệnh nhân trong Office.
  - Biển quảng cáo: hiệu ứng chờ bệnh nhân và cộng giới hạn bệnh nhân như mục trên.
  - Vàng kinh doanh là vàng nhận từ mỗi bệnh nhân chữa đúng (25 × hệ số, làm tròn). Vàng/phút tính theo phút thật người chơi đang chơi (mọi scene, kể cả Player_Home và Street; app bị pause hoặc chạy nền không tính), cộng dồn từ báo cuối ngày trước tới báo cuối ngày hiện tại và cộng vào Total Earn. Thời gian chơi ngoài ngày làm việc được lưu theo luật lưu; thoát giữa ngày làm việc thì quay về thời gian chơi lúc bấm Open. Kiên nhẫn gốc 45 giây nhân với tổng hệ số kiên nhẫn.

### HUD nhiệm vụ và bảng xếp hạng phòng khám

- Bố cục ở các scene đi bộ dùng Headbar chung cho vàng, ngày và Settings. HUD nhiệm vụ và bảng xếp hạng nằm bên dưới Headbar, trong Safe Area và chừa vùng bấm cửa, di chuyển khi bảng đang đóng.
- HUD nhiệm vụ dùng nền bán trong suốt và tự tăng chiều cao theo số dòng. Chỉ hiện HUD ở các scene điều khiển Player đi bộ, gồm Street, Player_Home và năm phòng khám lân cận. Main Objective luôn hiện. Cả tiêu đề lẫn nội dung Side Objective ẩn khi danh sách quest đang hoạt động trống.
- Main Objective lần lượt là `Clear the first day.`, `Make 400 Gold.`, `Upgrade Receptionist to Lv 3.` và `Be top 1 Clinic in leader board.`. Điều kiện lần lượt là vào ngày 2, có ít nhất 400 vàng, trang bị lễ tân cấp 3 trở lên và đứng hạng 1 theo bảng xếp hạng. Mục cuối vẫn hiện cùng dấu ✓ sau khi hoàn thành. Tiến độ chỉ tiến tới và được lưu cùng checkpoint cuối ngày.
- HUD bảng xếp hạng dùng cùng phạm vi scene đi bộ. Bảng mở từ nút cúp, dùng bố cục gọn để giữ phần lớn màn hình cho cảnh đi bộ. Mỗi hàng hiển thị hạng, logo, tên phòng khám và số vàng. Xếp theo số vàng giảm dần. Dòng phòng khám của người chơi lấy vàng từ PlayerProfile hiện tại và được phân biệt bằng giao diện.
- Theo Clinic Luxury từ thấp đến cao, số vàng đầu ngày 1 và mức tăng sau mỗi ngày lần lượt là Lantern Surgery 323 và +70, Ashbury Clinic 2.548 và +80, Bramwell Clinic 6.753 và +100, Ivy Clinic 12.050 và +130, Crown Clinic 36.042 và +200.
- Vàng của phòng khám lân cận được tính từ ngày hiện tại theo công thức `vàng đầu ngày 1 + (ngày hiện tại - 1) × mức tăng mỗi ngày`. Mức tăng đi theo checkpoint cuối ngày. Đổi scene, mở HUD và retry chuyển scene dùng lại cùng ngày. Reset profile đưa số vàng các phòng khám về mức ngày 1.

## Công cụ tạo và đồng bộ NPC

- Giữ tool authoring chính thức **Tools > NPC > Tạo NPC hàng loạt** cùng các custom Inspector, importer và HomePlayModeStart. Đây là công cụ phát triển đang dùng, không phải test/setup tạm để xóa khi dọn project.
- Tool lấy NPC_0 làm mẫu mặc định, seed bắt đầu 6, tên NPC_<seed>, tăng seed tuần tự theo số lượng; sau đợt thành công thì cửa sổ tự tăng seed bắt đầu. Giữ cấu trúc, nested prefab, Heart, animator và các component/tham chiếu triệu chứng/hội thoại từ mẫu. Seed quyết định ngoại hình, không đảm bảo các NPC đều có ngoại hình khác nhau.
- Mặc định bỏ Lock Override của các slot để áp ngoại hình theo seed mới; có tùy chọn giữ slot đã khóa. Kiểm tra toàn bộ dải tên trước khi tạo, không ghi đè prefab/.meta có sẵn. Chỉ coi GUID của asset còn tồn tại là trùng, không dùng GUID cache của asset đã xóa để chặn tạo lại tên.
- NPC mới được thêm vào NpcProfile bằng GUID mới và tên theo prefab. Chọn Diff 1–4 trong tool để sinh đúng số bệnh tương thích từ AllSickness. Tùy chọn theo xác suất cũ dùng secondSicknessChance của NpcProfile (mặc định 30%). Giữ bệnh/tên/ID/hội thoại của bản ghi cũ và để trống hội thoại của NPC mới. Nếu chọn NpcCatalog thì cập nhật để các NPC mới có thể spawn. Hủy/lỗi trong đợt phải dọn asset vừa tạo và khôi phục profile/catalog của đợt đó.
- Nút **Refresh / cập nhật NPC** có trong NpcProfile Inspector, NpcCatalog Inspector và cửa sổ tạo hàng loạt: quét thư mục/subfolder, thêm bản ghi mới, giữ dữ liệu bản ghi cũ và dựng lại catalog từ toàn bộ prefab còn tồn tại trong profile, bỏ null/trùng. Import thường thêm NPC mới không bệnh; random bệnh chỉ thuộc thao tác tạo hàng loạt.
- Refresh thường giữ bản ghi mất prefab để người dùng kiểm tra. Chỉ nút **Xóa bản ghi mất prefab + cập nhật spawn** mới dọn các bản ghi đó; không xóa asset prefab. Prefab xóa rồi tạo lại cùng tên có GUID và bản ghi mới, không tự chuyển danh tính/bệnh/hội thoại theo tên. Dùng Undo cho các chỉnh sửa profile/catalog qua Inspector.

## Quy trình Spine → Unity tiết kiệm usage

- Với chỉnh animation hoặc export lại, dùng project `.spine`, ảnh nguồn, prefab và Spine Runtime đã có. Kiểm tra phiên bản và giấy phép Spine bằng CLI trước khi chọn cách rig hoặc export. Spine Essential hỗ trợ attachment dạng ảnh và key đổi ảnh. Rig mesh có trọng số cần Professional để chỉnh và export.
- Ưu tiên Spine CLI cho import, lưu project và export JSON kèm atlas. Giữ tên file cùng GUID của asset Unity khi cập nhật. Dùng bản Spine Editor và Spine Runtime cùng nhánh phiên bản. Chỉ xử lý lại atlas hoặc ảnh nguồn khi thay đổi cần đến chúng.
- Với một lượt export thông thường, gom thay đổi rồi kiểm tra một lượt có mục tiêu trong Unity. Xác minh prefab nạp đúng dữ liệu, material và atlas, phát đúng clip và lặp đúng nếu clip cần loop. Kiểm tra thêm khi có lỗi hoặc thay đổi ảnh hưởng trực tiếp đến hình ảnh và chuyển động.
- Chỉ dùng điều khiển màn hình khi cần thao tác trực quan mà CLI không đáp ứng, hoặc khi người dùng muốn xem trực tiếp. Sau khi kiểm tra, kết thúc phiên chụp để tránh để lại viền vàng quanh màn hình.
- BunnyPoleDance hiện dùng `Design/Characters/BunnyPoleDance/Essential/BunnyPoleDance.spine` cho bản Essential. Rig mesh gốc ở `Design/Characters/BunnyPoleDance/BunnyPoleDance_Weighted.spine`. Prefab Unity ở `Assets/2D_Assets/NPC_Type2/BunnyPoleDance/BunnyPoleDance.prefab`, tự phát lặp `hip_bounce`.

## Profile NPC, bệnh và triệu chứng

- Dùng `Assets/Data/NPC/NpcProfile.asset` làm nguồn tên và bệnh của NPC; `NpcAppearanceProfile` chỉ quản lý ngoại hình. Không tạo nguồn bệnh riêng cho mechanic nghe tim.
- Spawner hiện chọn prefab từ NpcCatalog, rồi liên kết đúng bản ghi NpcProfile bằng tham chiếu prefab/ID ổn định. `PatientExaminationProfile` trên mỗi prefab giữ tham chiếu Heart; không tìm Heart toàn scene hoặc suy ra danh tính từ tên NPC runtime.
- Đọc bệnh từ bản ghi NPC, đọc triệu chứng từ AllSickness bằng ID ổn định. Đổi tên/sắp xếp dữ liệu không được làm sai liên kết. Thay đổi bệnh/khoảng nhịp trong Play có hiệu lực từ phiên khám kế tiếp.
- NPC không có phân loại chủng tộc riêng. Profile có tên, danh sách sicknessIds gồm tối đa 4 bệnh khác nhau và asset hội thoại NpcDialogueAsset tùy chọn để chỉnh. ID và tham chiếu prefab giữ liên kết kỹ thuật. Zombie Infection và Vampire Infection là bệnh trong AllSickness, giữ nguyên tên và ID hiện có.
- Thân nhiệt: bình thường 37°C; triệu chứng High Temperature random 38,6–42°C; Low Temperature random 33–34,9°C. Mỗi phiên khám của một NPC chỉ lấy một nhiệt độ và giữ nguyên đến khi phiên khám kết thúc (cho thuốc hoặc báo công an, chuyển sang NPC tiếp theo). Đo lại, thả/giữ, đổi dụng cụ hoặc bật/tắt nhiệt kế không được random lại. Phiên khám mới được lấy giá trị mới trong đúng khoảng triệu chứng, kể cả khi dùng lại cùng prefab NPC. Nhiệt độ thuộc trạng thái runtime của phiên bệnh nhân, không ghi ngược vào NpcProfile hay save người chơi. Nhiệt độ chỉ phụ thuộc triệu chứng của bệnh đang gán.
- Nhịp tim theo triệu chứng bệnh và NPC Diff (số bệnh, 1–4; NPC khỏe dùng Diff 1). Mỗi phiên khám random một BPM nguyên trong khoảng (gồm hai đầu) và giữ nguyên tới hết phiên, nghe lại hoặc đổi dụng cụ không random lại. Bình thường 65–80 ở mọi Diff. Chậm: Diff 1 15–30, Diff 2 20–40, Diff 3 và 4 20–50. Nhanh: Diff 1 180–200, Diff 2 160–200, Diff 3 140–200, Diff 4 120–200. No Heartbeat là 0 BPM và ưu tiên hơn triệu chứng nhanh/chậm. Các khoảng chỉnh được trong Inspector của HeartbeatExamination. Vampire Infection hiện có No Heartbeat nên không có nhịp tim; không có nhánh ưu tiên theo danh tính NPC.
- Khi không có nhịp tim, thao tác giữ để nghe chỉ hiện hình tim đứng yên: không restart VFX, không phát sự kiện nhịp lặp, không VFX_Sound, không tiếng. Hình tim vẫn hiện khi ở ngoài phạm vi nghe. Thả tay hoặc bỏ chọn dừng và dọn hiệu ứng; lần giữ mới hiển thị lại hình tim tĩnh.
- Các mechanic đọc cùng danh sách bệnh từ NpcProfile qua NpcMedicalConditions, gộp triệu chứng bằng ID và không tự đổi bệnh khi khám. Dữ liệu cũ đã được chuyển: Zombie sang Zombie Infection, Vampire sang Vampire Infection; không chạy lại việc chuyển này khi mở game.
- Liên kết bệnh/triệu chứng thiếu hoặc đồng thời có triệu chứng nhanh và chậm phải báo lỗi rõ; không âm thầm coi là nhịp bình thường.

## Nhiều bệnh trên một NPC

- Bệnh được gán cố định trong NpcProfile. Refresh/import thường giữ bệnh của bản ghi đã có và để trống bệnh của bản ghi mới.
- NpcMedicalConditions là nơi chung gộp và xử lý triệu chứng. Không cho Vampire Infection đi cùng Zombie Infection. Nếu một cặp có Rapid Heartbeat và Slow Heartbeat mà không được ưu tiên No Heartbeat giải quyết thì cặp đó không hợp lệ; tool chọn trong danh sách bệnh tương thích, Inspector lọc tổ hợp không hợp lệ. Không tự chuyển sang nhịp bình thường.
- Triệu chứng Vampire/Zombie ưu tiên hơn bệnh còn lại trong cùng nhóm xung đột: hiện áp dụng nhóm nhịp tim (Rapid/Slow/No Heartbeat) và nhóm thân nhiệt (High/Low Temperature). Vampire + bệnh sốt vẫn nhiệt độ thấp, không nhịp tim; Zombie giữ nhiệt độ cao. Nhóm triệu chứng mới có xung đột phải bổ sung quy tắc rõ trong resolver chung.
- Hiệu ứng hình ảnh của các bệnh cùng tồn tại; triệu chứng trùng chỉ áp dụng một lần. Hai bệnh cùng gây giun vẫn chỉ có một prefab giun tại một Pos; gãy xương vẫn chỉ một cấu hình vết nứt. Tim và Nao độc lập, có thể cùng hiện.
- Cuối ngày đối chiếu quyết định qua TreatmentEvaluator. Có Vampire hoặc Zombie thì báo cảnh sát đúng loại là đủ, kể cả khi còn ma. Các ca còn lại cần đủ thuốc theo hợp các công thức và dọn hết ma nếu có Haunted. Ca chỉ có Haunted có thể xác nhận Give với danh sách thuốc trống sau khi đã dọn hết ma.
- Hội thoại tiếng Anh viết sẵn theo bộ bệnh: 3–6 câu ngắn, mỗi dòng một lượt. Chào rồi nói cảm giác trước khi phỏng đoán nguyên nhân. Chỉ nêu cụ thể đau đầu/buồn nôn/chóng mặt/khó thở/đau nhức cơ thể khi có triệu chứng tương ứng; nếu không thì dùng cảm giác mơ hồ như mệt/không khỏe. Khoảng 60% NPC có phỏng đoán, 40% không biết vì sao; đổi cách kể, tránh dập khuôn. Đổi bệnh thủ công không tự sinh lại nội dung asset, người dùng cần chỉnh hội thoại cho khớp. NPC mắc Zombie Infection (có triệu chứng Nausea) phải nói rõ buồn nôn trong hội thoại (nauseous, queasy, muốn nôn...), không chỉ dùng từ mơ hồ như `feel sick`; đổi cách diễn đạt giữa các NPC.
- Dữ liệu hiện có đã chuyển từ sicknessId đơn sang sicknessIds, giữ bệnh đầu, danh tính, prefab và asset hội thoại; đã lấy xác suất thêm bệnh thứ hai một lần. Backup và danh sách chuyển đổi ở Logs/NpcMultiSicknessMigration. Không chạy lại migration khi khởi động.
## Diff_System

- Chỉnh Success Streak và tỷ lệ gọi bệnh nhân Office theo từng mức Diff Progress trong Assets/Resources/Diff_System.asset. Success Streak mặc định 3 ngày, giá trị lẻ cần đủ số ngày nguyên làm tròn lên.
- Diff Progress bắt đầu 1 và tối đa 4. Một chuỗi ngày liên tiếp được phép có tổng tối đa 1 ca sai. Khi vượt giới hạn, giữ đoạn ngày liên tiếp còn thỏa điều kiện. Ngày không có ca đã xử lý ngắt chuỗi. Đạt đủ ngày thì tăng một mức và bắt đầu chuỗi mới. Lưu mức và chuỗi cùng checkpoint cuối ngày.
- Nhóm Diff của NPC bằng số bệnh đã gán, tính cả Haunted, Vampire và Zombie. NPC chưa có bệnh chưa vào nhóm Office. Độ khó đuổi ma là thiết lập riêng. Office bốc nhóm theo tỷ lệ của mức hiện tại rồi bốc NPC chưa gặp trong nhóm đó. Nhóm Diff 1 lấy phần còn lại của 100%. Khi nhóm hết ứng viên, chia tỷ lệ theo trọng số của các nhóm còn ứng viên.

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



## Cửa hàng, inventory và lịch sử Give

- Shop trong Home chỉ mua ở Preparation. Mỗi thuốc có Buy X1 và Buy X10; không đủ tiền thì báo lỗi, không thay đổi tiền/kho, không mua chịu. Giá riêng trong PillProfile, hiện mặc định 1.
- PillProfile hiện có tên, giá, description riêng và Sprite cho từng thuốc; giữ ID/liên kết cũ. PlayerStartingProfile tại Assets/Resources cấu hình tiền khởi đầu 200 cho save mới. Inspector PlayerProfileService trên Game Services cho chỉnh tiền runtime để test; ngoài ngày làm việc, giá trị chỉnh cũng được lưu như mọi thay đổi khác.
- Inventory và chọn thuốc dùng chung MedicineListView/MedicineRow, hiện các thuốc sở hữu cùng số lượng realtime; shop hiện mọi mục PillProfile. Sprite chưa gán dùng placeholder. Description là Text tách riêng tên/số lượng.
- Cho phép chọn nhiều loại thuốc, check từng loại; Give trừ đúng 1 mỗi loại, không phụ thuộc số bệnh/công thức. Chọn/bỏ chọn/đóng popup không trừ. Bấm ngoài panel hoặc X đóng popup, TreatmentDecision quay về Examination qua nhánh return tập trung trong DayFlowPlan.
- Mỗi Give ghi lịch sử chỉ trong RAM qua TreatmentJournal, gồm consultation GUID, NPC ID/tên, bệnh ID/tên tại thời điểm Give và thuốc ID/tên/số lượng. Không ghi file lịch sử. Ghi bản ghi thất bại thì không trừ thuốc. Chặn trùng một consultation.
- Lịch sử Give chỉ giữ trong RAM để đối chiếu trước checkpoint cuối ngày; GetTreatmentSnapshot trả về bản sao độc lập. Lưu checkpoint thành công mới dọn lịch sử, lưu lỗi giữ lại để retry. Thoát phiên thì mất lịch sử giữa ngày. Tiền/kho thay đổi trong ngày làm việc chỉ lưu cùng checkpoint cuối ngày. Báo cảnh sát ghi quyết định vào cùng lịch sử RAM; đã đối chiếu đúng/sai để chọn cutscene; chưa triển khai sống/chết hoặc thưởng/phạt.



## Báo cảnh sát qua điện thoại

- Bấm điện thoại có ExaminationToolKind.Telephone trên Desk trong Examination, sau khi NPC vào xong, sẽ mở PoliceReportPopup thuộc Canvas/Safe Area của Office. Dùng đường input chuột/touch chung ExaminationToolSelection; không tìm điện thoại theo tên runtime.
- Popup hỏi "Report this case to police?", có hai lựa chọn loại trừ nhau "Vampire infected" và "Zombie infected". Mở popup mặc định chưa chọn; Report chỉ bật khi đã chọn một loại. Có thể đổi lựa chọn trước xác nhận. Bấm X hoặc ngoài panel để hủy và về Examination; hủy không ghi quyết định.
- Xác nhận Report ghi một quyết định ReportToPolice cho consultation hiện tại rồi NPC Walk_Out như Give, tiếp tục nhận bệnh nhân hoặc kết thúc ngày theo đồng hồ. Không trừ thuốc/tiền khi báo; quyết định được đối chiếu cùng lịch sử cuối ngày. Đồng hồ vẫn chạy khi popup mở.
- TreatmentJournal ghi cả decisionKind, reportReason và reportedSicknessId ổn định, đồng thời giữ snapshot bệnh thật của NPC; không tự đổi bệnh NPC theo lựa chọn báo cáo. Give có decisionKind GiveMedicine và danh sách thuốc; Report có danh sách thuốc rỗng. Chỉ giữ trong RAM đến checkpoint thành công.
- Một consultation chỉ chốt một quyết định. Retry cùng quyết định là idempotent; không cho đổi loại báo cáo hoặc chuyển Give/Report sau xác nhận. Bố cục/chữ/check và button chỉnh tại Assets/Resources/UI/PoliceReportPopup.prefab, Report là nested prefab của GiveButton.

- TreatmentJournal phải nhận diện lịch sử rỗng bằng số bản ghi, không chỉ current == null: Unity inline serialization có thể tạo Day mặc định ngày 0. Bản ghi Give/Report đầu tiên gán ngày hiện tại; lịch sử không rỗng khác ngày vẫn phải báo lỗi, không tự xóa.

## Cutscene kết quả cuối ngày

- TreatmentEvaluator đối chiếu lịch sử RAM bằng ID: bệnh thông thường cần hợp của tất cả thuốc yêu cầu, mỗi loại tối thiểu 1. Thuốc thừa không tính sai. Vampire/Zombie phải báo đúng loại; cho thuốc hoặc báo sai loại là sai. Một ca sai/thiếu làm cả ngày Fail; tất cả đúng thì Success.
- Dữ liệu công thức rỗng/trùng ID/liên kết thiếu, lịch sử sai ngày hoặc consultation trùng phải báo lỗi dữ liệu, không tự ghi checkpoint với kết quả giả. Không thay đổi bệnh/thuốc tác giả đã chỉnh trong Inspector.
- Đối chiếu trước checkpoint và cache DayTreatmentResult trong DayCompletionService. Lưu lỗi giữ lịch sử/ngày; lưu thành công mới dọn lịch sử. Retry tải Home hoặc phát video dùng kết quả đã cache, không lưu/tăng ngày lần nữa.
- Home dùng ResultsPopup.prefab với ResultsVideoPresenter, VideoPlayer → ResultsVideo.renderTexture → RawImage. Clip tham chiếu trực tiếp tới Assets/2D_Assets/Clip/Success.mp4 và Fail.mp4; không tách thành ảnh, không dùng frameReady. Giữ tỷ lệ trong Canvas, không méo/cắt hình.
- Sau khách cuối Walk_Out hoàn tất: Office fade đen 1s, giữ đen xuyên chuyển Home. Hiện lần lượt `The day has ended.` và `Later that night, your client takes your pill...`, mỗi dòng fade in 1s, giữ 3s, fade out 1s. Video fade in 1s, chờ loopPointReached rồi giữ frame cuối fade out 1s. Hiện `new day has started.` trong 3s (fade in 1s, giữ 1s, fade out 1s), sau đó reveal Home Preparation 1s. Chỉ hoàn tất trình bày sau cả chuỗi; retry không lưu/tăng ngày lại.
- SceneTransitionFade.prefab thuộc Canvas mỗi scene, phủ toàn màn hình (sorting 100); ResultsPopup có Canvas sorting 200 và chữ trong Safe Area. Chỉnh chữ/font trong ResultsPopup, thời lượng trên ResultsVideoPresenter/SceneTransitionFade. Home được lưu với cover đen để không lóe cảnh khi load; Preparation reveal khi đã sẵn sàng.
- Chuẩn bị/phát lỗi đi theo UI Thử lại của flow; hủy/dừng phải tháo callback. Fade/chữ và video tạm dừng khi app mất focus/vào nền. Android import H.264; vẫn cần kiểm thử trên thiết bị thật.

