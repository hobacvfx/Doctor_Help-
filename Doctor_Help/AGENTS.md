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
- Có player profile được lưu tự động và load mỗi lần mở game.
- Có quảng cáo, liên kết Firebase, AdMob để chạy UA sau khi phát hành.

## Phạm vi hội thoại cập nhật thiết kế

- Hội thoại nơi người dùng cung cấp các nội dung trên chỉ dành cho cập nhật lối chơi, game design, rules và định hướng phát triển project.
- Không viết code hoặc triển khai bất kỳ tính năng nào được nêu trong hội thoại đó; các tính năng được trao đổi ở đó chỉ nhằm cập nhật thiết kế và định hướng phát triển.
