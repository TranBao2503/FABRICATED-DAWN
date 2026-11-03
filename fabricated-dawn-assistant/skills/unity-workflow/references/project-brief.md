# Fabricated Dawn — bản đọc phục vụ phát triển

Nguồn: [BTL Game Nhóm 1](https://docs.google.com/document/d/1AlE5j8_13of6geX94sKJDvszqVpkJn0DIIvbFeWY168/edit?tab=t.0).
Đã đọc tab `t.0`, các phần 1–7 ngày 02/11/2026. Đây là bản tổng hợp cục bộ, không đồng bộ tự động và không sửa tài liệu nguồn.

## Yêu cầu từ tài liệu

- Kinh dị tâm linh Việt Nam, khám phá và giải đố qua các khung cảnh 2D cố định; người chơi thao tác chuột, góc nhìn thứ nhất. Không có chiến đấu đánh quái (2.1–2.3; 3.1).
- Bối cảnh Hà Nội năm 2004. Đồ dùng và âm thanh phù hợp: quạt điện, CRT, cassette, điện thoại bàn, lịch giấy, đèn tuýp (4.4).
- Bốn nam sinh viên, nhân vật chính Trần Văn Nam; ba bạn Bia, Băng, Bảo. Nam thuộc dòng họ bên người chồng đã chết (4.3–4.5).
- Ba vong khác vai trò: người vợ là boss; người chồng gây ma che mắt; bé gái không chủ động săn người, có quan hệ tin tưởng với người chơi (3.5).
- Hiện tại và không gian oán niệm dùng cùng cấu trúc nền, hai bộ hình và hotspot. Mở cửa ở M4 đổi toàn bộ không gian một lần, không phải du hành thời gian hay biến đổi dần (3.3; 5.5; 5.12).
- Bảo hộ phòng thờ là trạng thái riêng, còn hiệu lực sau cửa M4 rồi bị vượt qua tại M6. Lựa chọn trước đó ảnh hưởng cảnh báo/hỗ trợ, không loại bỏ biến cố bắt buộc này (3.6; 5.7).
- Vật lý, ký ức, liên kết và thông tin trong sổ phải phân biệt. Dao gây án M7 là vật ký ức, tác động hai vong vợ/chồng trong không gian oán niệm; không giết boss, không dùng hại vong nhi (3.7–3.9).
- Checkpoint phải phục hồi đồng thời các trạng thái, tài nguyên và lựa chọn. Chết thông thường là Game Over; Bad Ending là kết thúc có hậu quả cốt truyện (3.10; 3.14.5).
- Gợi ý âm thanh cần dấu hiệu thị giác hoặc lời thoại tương ứng. Giữ tỷ lệ khung cảnh, cỡ chữ dễ đọc, có giảm nhấp nháy/rung (3.12; 6).

## Tám màn và điểm kiểm tra

| Màn | Mục tiêu | Checkpoint quan trọng |
| --- | --- | --- |
| 1 — Những ngày ở trọ | Sinh hoạt, học bản đồ, gieo dấu hiệu bát nhỏ/bóng/gương/chó | Trước Vỗ Vai Ma |
| 2 — Người thứ năm | Chạm vai tại góc trống, trốn tới phòng thờ | Sau khi tới phòng thờ |
| 3 — Chuyện nhà chủ cũ | Hỏi người sống, biết án mạng, quyết định chuyển trọ | Trước khi rời nhà |
| 4 — Cánh cửa trở về | Giải vòng lặp, mở lối phụ, đổi không gian | Trước và ngay sau cửa |
| 5 — Những người chưa đi | Đồ chơi của bé, ngăn tủ người chồng | Sau hai câu đố, trước truy đuổi |
| 6 — Lời báo mộng | Ghép lời nhắc, phòng thờ bị xâm nhập | Trước và sau thoát hiểm |
| 7 — Sự thật bị chôn giấu | Đối chiếu ba cái chết, lấy dao, học tác dụng dao | Sau giải trình tự và lấy dao |
| 8 — Đêm cuối | Dùng dao và chứng cứ, đối diện boss, chọn ending | Trước đối đầu và lựa chọn cuối |

## Những điều nhóm cần chốt

1. **True Ending:** 3.11 nhắc bạn có thể đã chết/trạng thái bất thường; 4.6 bổ sung dòng họ pháp sư và yêu cầu chốt thời điểm biến cố, luật tồn tại. Chưa đủ để khẳng định ai, chết lúc nào hoặc twist chính thức. Giữ `trueRouteApproved = false`.
2. **Good Ending:** 4.5 nói chưa ấn định bản án khi chưa có đường chứng cứ; 4.6 và 5.9 lại ghi bà nội đi tù. Cần quyết định diễn biến và chứng cứ trước khi viết kết cục chính thức.
3. **Bảo hộ phòng thờ:** 3.11 mô tả lựa chọn ảnh hưởng thời điểm mất an toàn, còn 5.7 xác định biến cố bắt buộc ở M6. Đề xuất giữ mốc M6; lựa chọn tác động cảnh báo/hỗ trợ. Đây là đề xuất, chưa phải sửa nguồn.
4. **Âm thanh vong nam:** một số đoạn dùng hắt xì, một số dùng ho khan. Cần chốt tín hiệu nhận diện và cách thể hiện bằng hình ảnh.
5. **UI và art:** nhiều mục ở phần 6 chỉ có tiêu đề; phần 7 chưa có bộ hình triển khai. Cần wireframe menu, HUD, inventory, sổ, hội thoại, puzzle, hint, Game Over/ending.
6. **Số liệu và nền tảng:** thời lượng pin, độ trễ truy đuổi, thời gian bùa, ngưỡng tin tưởng, mã puzzle, sơ đồ chính thức, nền tảng phát hành và cấu hình tối thiểu chưa chốt trong bản đã đọc. Không coi PC/URP/Input System là yêu cầu bắt buộc từ nguồn.

## Đề xuất triển khai đã tạo

Gói UPM có Editor window, dữ liệu khởi tạo 8 màn/8 phòng/13 vật phẩm, validator, lớp trạng thái/checkpoint và prototype M4. Các giá trị ending trong mã chỉ là chính sách mẫu để thử việc khóa/mở lựa chọn; cần thay bằng chuỗi manh mối được nhóm duyệt.

Không có tám màn hoàn chỉnh, art chính thức, hội thoại đầy đủ, hệ thống âm thanh, build phát hành hay kết nối MCP trực tiếp tới Unity trong phiên bản 0.1.0.
