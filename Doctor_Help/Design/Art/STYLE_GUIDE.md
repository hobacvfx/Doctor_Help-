# Art style — Doctor Help

Đọc file này trước khi vẽ mới hoặc sửa hình cho project. Áp dụng cho NPC, thuốc, icon triệu chứng và UI minh họa. Đây là hướng dẫn dùng lại giữa các hội thoại; yêu cầu cụ thể mới của người dùng được ưu tiên.

## Mẫu chuẩn và thứ tự ưu tiên

- **Cách render:** bộ thuốc `Design/MedicineIllustrations/Medicine_Collection.png`; asset thuốc trong `Assets/2D_Assets/Medicines/`. Dùng nét chì đen, texture vẽ tay và cel shading như các hình này.
- **NPC mới:** `Design/Characters/VictorianRedraw/Collection.png` và các sprite rời trong `Assets/2D_Assets/Special_Look/VictorianRedraw/`. Dùng để tham khảo mức độ nét và chất liệu.
- **Tỷ lệ, góc nhìn, vị trí cơ thể:** prefab `Assets/2D_Assets/NPC/NPC_0.prefab` là chuẩn kỹ thuật bắt buộc; nó có ưu tiên cao hơn hình concept hoặc NPC AI khác.
- **UI popup kẹp tài liệu (duyệt 2026-11-21):** `Assets/Art/UI/ClinicClipboard/CleanClipboardAtlas.png` — giấy kem sạch, nguyên vẹn, khung gỗ gọn và kẹp đồng; nét rõ theo chất lượng popup thưởng Dart nhưng không có họa tiết hội chợ. Ảnh triệu chứng cũ giữ tại `Assets/Art/UI/DoctorBook/Symptoms/`. Khung, hình và chữ UI tách riêng; không dùng lại giấy rách/nát cho hai popup này.
- Bộ NPC AI trong `Design/Characters/PublicDomain/`, `PencilRevision/`, `Dracula/` là lịch sử, không phải chuẩn chất lượng. Không lấy chúng để sửa lại khi người dùng yêu cầu vẽ mới.

## Nét vẽ và màu

- 2D cartoon hơi spooky, gợi Tim Burton; London thời Victoria trong thế giới giả tưởng. Hình rõ, hơi lệch tự nhiên, không kinh dị tả thực.
- Viền chính **đen đậm**, độ dày biến thiên và hơi nham nhở. Tránh viền xám bạc, nhạt hoặc sạch như vector.
- Quanh viền chính có nhiều nét chì mảnh, dài, lệch khỏi đường bao, như phác đi phác lại. Mật độ cao hơn khoảng **5 lần** bản NPC AI cũ thưa nét; nét phác xuất hiện thường xuyên nhưng không thành đám nhiễu che silhouette.
- Vải, gỗ, giấy, kim loại và thủy tinh đều có nét chì mô tả vật liệu. Vùng tối dùng vài nhóm gạch chéo/gạch song song theo nếp và mặt phẳng; không phủ hatch đều toàn bộ hình.
- **Cel shading, không tô một màu duy nhất:** màu nền + 1–2 mảng bóng rõ + một ít sáng có chủ đích. Mảng lớn dễ đọc ở cỡ mobile. Không gradient mượt, ánh sáng bóng bẩy, chất liệu ảnh chụp hoặc khối 3D quá nổi.
- Palette trầm nhưng màu vẫn rõ: đỏ rượu, tím mận, xanh rêu, teal bụi, xanh slate, ivory, ochre, đồng cũ. Các vật/nhân vật cùng nhóm cần khác silhouette, chi tiết và màu chủ đạo để phân biệt.
- Nền PNG phải thật sự có alpha, kể cả khe tóc và cổ áo. Không có nền giả, checkerboard, glow, bóng đổ hoặc vệt ở vùng trống. Kiểm tra alpha thay vì chỉ nhìn RGB preview.

## NPC: ghép từ sprite rời

- Không vẽ nguyên nhân vật thành một ảnh để thay toàn bộ NPC. Tách Eye, Nose, Mouth, Hair/hat, Outfit và lớp phụ nếu cần. Giữ anatomy, xương, Heart, animator, slot triệu chứng và Speaking Lip từ NPC_0.
- **Đầu thường dùng đầu có sẵn trong project.** Chỉ làm đầu riêng khi bản chất nhân vật cần, ví dụ xác ướp. Không phóng to đầu hoặc bóp ngang xương để ép vừa hình.
- Góc nhìn hơi nghiêng **về bên trái màn hình**, không chính diện. Mắt bên trái nhỏ hơn nhẹ, tai bên phải còn thấy. Đặt vị trí mắt/mũi/miệng theo bản render NPC_0, không chỉ căn giữa mỗi PNG.
- Khi đo hình gốc, phải tính `SpriteRenderer.drawMode`, `size`, pivot, sprite rect và toàn bộ parent transforms. Sliced renderer có thể hiển thị khác kích thước PNG; bỏ qua điều này sẽ làm mặt mới quá nhỏ.
- Tóc/mũ chỉ chứa tóc/mũ, để hở vùng mặt. Không vẽ thêm tai hoặc đầu lên hairpiece gây tai kép. Mũ và lọn tóc không che mắt ngoài chủ đích thiết kế.
- Quần áo đủ dài che toàn bộ torso ở mép dưới, có tay áo đầy đủ và vai khớp NPC_0. Kiểm tra cả eo và khe giữa tay/thân. Với bệnh nhân Special Look, silhouette của chính áo ngoài phải che hết thân, vai và hai tay. Bỏ lớp Garment lining và ưu tiên đường cổ thấp, gọn.
- Cổ áo phải có khoảng trống để cổ luồn vào. Cổ dựng phía sau đầu/cổ phải tách sprite riêng, sorting thấp hơn Neck + Head; phần áo trước và lớp sau không bịt kín cổ.
- Nose tách khỏi subtree Mouth: dialogue có thể ẩn Mouth để chạy Speaking Lip, nhưng không được làm mất mũi.
- Lưu các part thành nested prefab, khóa các slot ngoại hình đã author. Không đổi GUID, danh tính, bệnh hoặc hội thoại của bản ghi NPC cũ để thay art.

## Thuốc, triệu chứng và UI

- Thuốc là lọ/chai/hộp/sách/ống tiêm thông thường phù hợp thời Victoria; tránh bình phép, sinh vật fantasy hoặc phụ kiện kỳ ảo không được yêu cầu. Đa dạng dáng lọ, nắp, nhãn, màu chất thuốc và cách đóng gói.
- Minh họa triệu chứng chỉ dành cho triệu chứng có biểu hiện hình ảnh trong gameplay. Buồn nôn, đau đầu, khó thở và phép đo đi vào mô tả chữ, không tự thêm icon. Minh họa có cùng ý nghĩa nhưng không chép y hệt sprite triệu chứng trong game.
- Sprite, nền, khung và chữ tách riêng. Danh sách bệnh dùng AllSickness và ID ổn định; ảnh triệu chứng qua SymptomReferenceArt. Không bake tên, description hoặc toàn bộ danh sách vào bitmap.

## Gắn vào game và kiểm chứng

1. Render NPC_0 và NPC mới cùng camera/scale, kiểm tra góc, vị trí mặt, độ phủ quần áo, tai kép và lỗi cắt atlas. Cắt đúng asset, không lấy nét từ ô kế bên.
2. Reload prefab đã lưu; kiểm tra nested refs, material, component, sorting và rig. Material đúng nhưng thiếu script vẫn có thể làm X-ray hỏng.
3. Mọi lớp che anatomy dùng shader xuyên phải có `ClothSeeThroughController` trên **chính GameObject chứa Renderer**: áo trước, áo lót, tóc/mũ, cổ áo sau và đầu đặc biệt nếu dùng material này. Đối chiếu `M_Cloth_1` và NPC_0. Giữ cấu hình riêng của Bone/M_Bone. Không tự đổi Eye/Nose/Mouth sang material áo nếu prefab gốc không dùng.
4. Test kính lúp bằng input thật/giả lập qua ExaminationToolSelection: lần bấm chọn không soi; giữ tiếp theo mới soi, kéo vùng xuyên theo kính, thả/đổi dụng cụ/rời Examination phải khôi phục. Kiểm tra cả nhiều lớp áo cùng xuyên, không chỉ một Renderer.
5. Với NPC mới, test spawn, idle và chuyển động nhiều frame khi đi vào/ra; kiểm tra ngoài ảnh tĩnh. Dùng môi trường/save thử riêng, không dùng save thật.
6. Tự nhận xét bản render, sửa lỗi thấy được rồi mới bàn giao. Báo rõ phần kiểm tra Edit Mode, Play Mode và thiết bị; không gọi compile hoặc reference khác null là đã test gameplay.

## Prompt khung

> Production modular 2D sprite for a spooky Victorian London mobile doctor game. Hand-drawn Tim Burton style cartoon. Strong near-black irregular pencil contours; many long fine offset sketch strokes around major outlines; selective material hatching in shadow areas. Clear two-to-three-tone cel shading, muted but distinct colors, no smooth 3D gradients or photorealism. NPC_0 reference controls anatomy, slight screen-left three-quarter view and facial placement; approved medicine reference controls rendering only. Transparent alpha background. Separate sprite parts with generous gutters, no neighboring-cell fragments, no text or background haze. Ordinary heads reuse the existing project head; hair contains no ears/face. Clothing fully covers the standard torso and has an open neckline.

Bổ sung mô tả nhân vật/vật thể, từng sprite cần xuất và vai trò từng ảnh tham chiếu. Nếu người dùng yêu cầu vẽ mới, dùng nguồn NPC_0 và mẫu render được duyệt; không dùng NPC AI cũ làm ảnh sửa.
