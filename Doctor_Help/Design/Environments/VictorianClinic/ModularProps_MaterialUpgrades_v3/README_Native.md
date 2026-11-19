# PNG gốc — Base + 5 nâng cấp vật liệu, v3

Gói nhẹ chứa 60 PNG gốc trong Native/, nền gốc, ảnh so sánh và metadata/prompt. Không có các thư mục Base, Upgrade_01–05 hoặc Decor_Base đã xuất 4608 × 4608 px.

_S00 là bản gốc mức sang trọng 3. _S01–S05 là năm lần nâng cấp: gỗ+sắt → thép → mạ vàng → đen+đồng đỏ → men ngà+kính xanh. _Decor là đồ phụ gốc.

Ảnh gốc có canvas và kích thước khác nhau. Poster/khung gốc chưa căn đồng canvas; gói Full_4608 có sẵn bản căn cùng transform. AssetManifest.json mô tả cả bản lớn và bản gốc; các trường nativeFile/nativeCanvas/nativeArtBounds áp dụng cho gói này, còn các đường dẫn Base/Upgrade_*/Decor_Base chỉ có ở gói đầy đủ.

Các PNG có alpha thật; RGB ẩn ngoài vùng alpha có thể còn màu. Ảnh so sánh đã ghép alpha lên nền trung tính để kiểm chứng. Các bản có đèn/cây là cụm prop hoàn chỉnh. Không có rig hoặc animation.

Dùng image_gen tích hợp, prompt trong các file JSON kèm theo. Nền là phòng mức 3 được dọn prop; một số chi tiết Base bị che được vẽ bù từ concept phẳng. Chưa import hoặc kiểm tra Unity.

