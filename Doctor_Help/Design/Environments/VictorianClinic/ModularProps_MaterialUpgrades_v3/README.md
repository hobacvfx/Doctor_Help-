# Doctor Help — Base + 5 lần nâng cấp bằng vật liệu

Bộ v3 theo hướng đã chốt: giảm hoa văn nhỏ, đổi vật liệu, màu chủ đạo và tiện nghi để nhận ra cấp khi nhìn nhỏ. Base là phòng khám mức sang trọng 3 trong concept trước; Upgrade 01–05 là năm lần nâng cấp cao hơn Base, không phải mức sang trọng 1–6 của bộ cũ.

## Dải nâng cấp

- Base: đồ gỗ của concept mức 3.
- Upgrade 01: gỗ kết hợp sắt/kim loại xỉn; thêm đèn dầu ở prop phù hợp.
- Upgrade 02: toàn thân kim loại thép/nickel, sạch và bóng hơn.
- Upgrade 03: kim loại mạ vàng, mảng đỏ rượu; cây cảnh ở bàn/quầy/tủ.
- Upgrade 04: kim loại sơn đen và đồng đỏ, tiện nghi cơ khí kiểu Victoria.
- Upgrade 05: men ngà và kính xanh, đèn điện và tiện nghi cuối thời Victoria.

Giữ góc nhìn ngang trực diện và cách render chì đen/cel shading của project. Chỉ dùng ít điểm nhấn trang trí lớn.

## File bàn giao

Có 60 sprite PNG alpha thật:
- Base và Upgrade_01 đến Upgrade_05, mỗi thư mục 8 ảnh: ReceptionCounter (bàn/quầy tầng 1), DoctorDesk (bàn tầng 2), Bookcase, Elevator, ElevatorLandingGate, EntranceDoor, AdvertisementFrame và AdvertisementPoster.
- Decor_Base gồm 12 prop phụ của phòng gốc: hai ghế, năm tranh giải phẫu treo tường, đèn tường, bình hoa, chuông, chồng sách, ray/cáp thang máy.
- Native giữ đủ 60 PNG gốc công cụ trả về, tên _S00 đến _S05 cho Base/năm lần nâng cấp và _Decor cho đồ phụ.
- Background gồm nền trống gốc và Clinic_Empty_L3_4608x3072.png.
- Material_Upgrade_Comparison.png: so sánh Base + năm lần nâng cấp của sáu nhóm prop chính.
- Elevator_Gates_Preview.png: so sánh riêng cổng tầng.
- Base_Reassembled_Preview.png và Upgrade_05_Reassembled_Preview.png: ghép thử từ chính các sprite bàn giao.

Các tranh giải phẫu thuộc decor treo tường. Biển quảng cáo dùng thương hiệu y tế hư cấu DOCTOR HELP, logo DH, slogan CARE YOU CAN TRUST, chữ LONDON; không dùng tranh khám bệnh.

## Canvas và ghép

Sprite lớn xuất trên canvas RGBA 4608 × 4608 px, giữ tỷ lệ, căn giữa ngang và cách đáy 128 px. Background là 4608 × 3072 px. Đây là bản nội suy bicubic từ PNG gốc, không phải tạo native ở 4K.

Poster và khung cùng cấp có cùng canvas và điểm neo. Đặt poster phía sau khung, cùng transform; không crop riêng một trong hai nếu muốn dùng căn chỉnh đã xuất.

Đèn/cây đi cùng các cụm prop nâng cấp khi được thiết kế kèm. Decor_Base giữ các đồ phụ gốc tách riêng. Cabin, cổng tầng và ray/cáp là ba sprite riêng; cabin và cổng đang ở hình tĩnh đóng cửa, chưa có rig hoặc animation.

AssetManifest.json ghi vùng pixel có hình, kích thước gốc và điểm neo đề xuất. PlacementGuide.json gợi ý bố trí cho Base, ở tọa độ 1536 × 1024; nhân ba để dùng với nền lớn. Hình dáng và độ cao phụ kiện thay đổi giữa các cấp, nên cần căn kích thước hiển thị khi import vào scene.

## Tạo ảnh và kiểm chứng

Dùng image_gen tích hợp; prompt lưu trong GenerationPlan.json, PosterInsertCorrection.json và BookcaseCleanup.json. Ảnh tham chiếu gốc trong prompt tương ứng với các file Native/*_S00.png; ba mẫu vật liệu bàn bác sĩ tương ứng Native/DoctorDesk_S01.png đến S03.png.

Đã kiểm tra số lượng/kích thước, alpha ngoài vật thể, lòng khung và khe cổng tầng; xem ảnh ghép trên nền trung tính và ghép vào phòng. Một số phần bị che ở Base đã được vẽ bù từ ảnh phẳng. Chưa import, author prefab hoặc test Play Mode/thiết bị; bộ này là bộ ảnh, không phải scene hoặc tính năng Unity.

