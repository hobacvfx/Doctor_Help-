# Bộ PNG ở kích thước gốc

Gói này chỉ chứa 60 sprite gốc trong Native/, nền gốc và các ảnh xem nhanh. Đây là bản tải nhẹ; không có các thư mục Level_1–Level_6 hoặc Decor_Level_3 xuất ở 4608 × 4608 px.

Tên file có hậu tố _L1 đến _L6 cho cấp sang trọng. Nhóm đồ trang trí phụ chỉ có _L3. Các PNG đều có alpha thật; dùng chính các PNG để kiểm tra thay vì chỉ xem RGB ẩn của vùng trong suốt.

Ảnh gốc có kích thước/canvas khác nhau. Poster gốc chưa được căn cùng canvas với khung: cần bố trí theo lòng khung nếu dùng bản gốc trực tiếp. Gói Full_4608 có sẵn các canvas đồng bộ để ghép poster/khung và thay cấp.

AssetManifest.json mô tả cả đường dẫn của bản xuất lớn; các đường dẫn Level_* và Decor_Level_3 trong manifest chỉ có ở gói đầy đủ. Các trường nativeFile/nativeCanvas/nativeArtBounds vẫn áp dụng cho gói này.

PlacementGuide.json là hướng dẫn bố trí gợi ý, không phải scene Unity. GenerationPlan.json và ElevatorLandingGatePrompts.json lưu prompt dùng với image_gen tích hợp. Một số chi tiết bị che trong ảnh phẳng đã được vẽ bù khi tách. Chưa có rig, animation hoặc import Unity.

