# Tutorial lần đầu

- Trong Unity, chọn **Tools > Tutorial > Preview First Run** khi đang ở Edit Mode. Preview dùng save riêng trong `Library/TutorialPreview`. Sau cutscene mở đầu, bấm **Open** ở Home để bắt đầu tutorial trong Office.
- Chỉnh thứ tự, lời hướng dẫn, thời gian giữ và vùng tương tác trong `Assets/Resources/FirstRunTutorial.asset`. Ca đầu tiên chọn một NPC chỉ mắc bệnh theo `sicknessId`. Thuốc duy nhất chữa bệnh này phải khớp `medicineId` dùng ở bước cho thuốc.
- Bước kính lúp dùng `HoldSeconds` cho tổng thời gian soi liên tục và `WormHoldSeconds` cho thời gian giữ liên tục trên con giun. Hai khoảng có thể chồng nhau. Khi thả tay, cả hai bộ đếm bắt đầu lại. Khi kéo kính khỏi giun, bộ đếm trên giun bắt đầu lại.
- Bước Doctor Book cho phép cuộn danh sách và bấm X. Người chơi dùng các triệu chứng đã thấy để đối chiếu bệnh trước khi chọn thuốc.
- Chỉnh sprite bàn tay và đường cong hai nhịp chạm trong `Assets/Resources/UI/TutorialOverlay.prefab`. Lớp phủ mặc định trong suốt (`Color` alpha bằng 0) và vẫn chặn thao tác ngoài vùng đang hướng dẫn. Các tham chiếu Home và Office nằm trên component TutorialSceneView của Gameplay Canvas.
- Popup Give Medicines giữ lưới 10 icon thuốc 5×2, không có giá/tổng/Give/Done. Kéo Antiparasitic Medicine tới ô nhận ngang ngực NPC; nhận đủ ô tự chốt thanh toán và điều trị. Tutorial lưu cờ hoàn tất sau lần thả cuối thành công, kể cả khi draft đã được xóa bởi giao dịch. Reset Player Profile xóa cờ để hướng dẫn chạy lại sau lần bấm Open tiếp theo.

## Sprite bàn tay

`Assets/Art/UI/MedicinePopups/TutorialHand.png` được tạo bằng ImageGen tích hợp. Ảnh tham chiếu chất liệu là atlas MedicineChrome.

```text
Create one clean mobile game tutorial hand pointer sprite on transparent alpha for the Doctor Help Victorian clinic game. Only a single right hand, viewed from the back at a slight tilt, wrist enters from lower right, index finger extends straight vertically upward to tap exactly at its fingertip, other fingers folded into palm, thumb rests on the side. The entire glove and small cuff remain visible with generous transparent margins. Ivory antique glove and short warm brown cuff, strong readable near-black ink/pencil contour and subtle hatching, two clear tones, matches the warm cream and antique brass UI atlas used as the material reference. At tiny mobile size should immediately read as a pointing/tapping index finger. No text, no touch effects or rings, no floor, no shadow behind, no background, no border, centered. True transparent pixels around the complete silhouette.
```
