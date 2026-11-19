# Doctor Help — bộ prop phòng khám Victorian, mức 1–6

Đã dùng STYLE_GUIDE.md của project và công cụ image_gen tích hợp. Nguồn bố cục là concept mức 3, bản v2 nhìn ngang trực diện.

## Nội dung

- Background/Clinic_Empty_L3_4608x3072.png: nền hai tầng không có prop; còn kiến trúc, giấy dán tường, vách gỗ, sàn và hốc giếng thang.
- Level_1 đến Level_6: mỗi thư mục có 8 PNG nền trong suốt. Mức 3 là bản tách/vẽ bù từ concept; năm phiên bản còn lại là mức 1, 2, 4, 5, 6 trong dải sang trọng đã thống nhất.
- ReceptionCounter: quầy/bàn tầng 1.
- DoctorDesk: bàn làm việc tầng 2.
- Bookcase: tủ sách tầng 2, bao gồm sách trong kệ.
- Elevator: cabin thang máy đóng cửa, tách khỏi giếng thang và ray.
- ElevatorLandingGate: cổng tầng thang máy, có đủ sáu mức đồng bộ.
- EntranceDoor: cửa ra vào, gồm khung vòm và ô kính.
- AdvertisementFrame: khung biển, lòng khung alpha trong suốt.
- AdvertisementPoster: nội dung quảng cáo riêng, thương hiệu hư cấu DOCTOR HELP, logo DH và slogan CARE YOU CAN TRUST; không có tranh giải phẫu.
- Decor_Level_3: 12 prop phụ của mức 3: hai ghế, năm tranh treo tường, đèn gas, bình hoa, chuông bàn, chồng sách và bộ ray/cáp thang máy. Đèn và chuông được tái sử dụng tại nhiều vị trí.
- Native: giữ 60 PNG prop ở kích thước gốc công cụ trả về.
- Background cũng giữ PNG nền gốc.
- All_Tiers_Preview.png, Support_Alpha_Preview.png và Level_3_Reassembled_Preview.png: ảnh xem nhanh và ghép thử; không phải sprite có alpha.

Tổng: 60 sprite có alpha và một nền trống, chưa tính bản gốc và ảnh preview.

## Kích thước và ghép lớp

Sprite xuất trên canvas RGBA 4608 × 4608 px, giữ tỷ lệ và căn đáy giữa; có khoảng đệm trong suốt. Background xuất 4608 × 3072 px. Các bản lớn được nội suy bicubic từ PNG gốc, không phải ảnh được tạo native ở độ phân giải 4K.

AssetManifest.json ghi đường dẫn, kích thước, vùng pixel có hình, pivot đề xuất và kích thước gốc của từng ảnh. Điểm neo dùng quy ước Unity từ góc dưới trái: x = 0.5; y = 128/4608. Trong từng nhóm prop, các cấp dùng cùng vùng bố trí tham chiếu; vẫn cần kiểm tra kích thước hiển thị khi import.

Poster và khung cùng cấp có cùng canvas và điểm neo. Đặt poster phía sau khung, cùng transform; không tự crop riêng một trong hai ảnh vì sẽ làm lệch vị trí. Nội dung poster đã có bleed nhẹ dưới mép khung.

PlacementGuide.json ghi các hộp bố trí gợi ý ở canvas 1536 × 1024, nhân 3 cho nền lớn. Level_3_Reassembled_Preview.png được ghép từ chính các PNG bàn giao để kiểm tra lớp và alpha. Đây là bố cục gợi ý, không phải scene Unity.

Cabin và cổng tầng là sprite tĩnh đóng cửa. Ray/cáp là bộ dùng chung. Bộ này chưa có rig hoặc các frame mở cửa/chuyển động.

## Phạm vi kiểm chứng

Đã kiểm tra kích thước PNG, alpha thật ở nền ngoài, lòng khung và các khe của prop; ghép trên nền trung tính và ghép lại phòng mức 3 để xem viền, tỷ lệ và thứ tự lớp. Chữ quảng cáo được kiểm tra qua ảnh render.

Ảnh nguồn là bitmap phẳng, nên một số chi tiết bị che được AI vẽ bù khi tách. Bộ tách không khôi phục các layer gốc hoặc đảm bảo khớp từng pixel với concept. Chưa import, chỉnh prefab hay kiểm tra Play Mode/thiết bị; không thay đổi scene hoặc gameplay.

GenerationPlan.json và ElevatorLandingGatePrompts.json lưu prompt của toàn bộ bộ ảnh, dùng image_gen tích hợp.

