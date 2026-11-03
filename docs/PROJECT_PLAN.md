# Chuẩn bị dự án Fabricated Dawn

Nguồn thiết kế: [BTL Game Nhóm 1](https://docs.google.com/document/d/1AlE5j8_13of6geX94sKJDvszqVpkJn0DIIvbFeWY168/edit?tab=t.0), đọc ngày 02/11/2026. Không thay đổi tài liệu nguồn.

Bản phân tích và các điểm chưa chốt: [project-brief.md](../fabricated-dawn-assistant/skills/unity-workflow/references/project-brief.md).

## Mục tiêu giai đoạn tiếp theo

| Giai đoạn đề xuất | Kết quả cần có | Điều kiện nghiệm thu |
| --- | --- | --- |
| Chốt thiết kế | Luật True Ending, chứng cứ Good Ending, sơ đồ nhà, quy cách art/UI | Nhóm duyệt các điểm đang mở; mỗi phòng có lối ra và ID hotspot |
| Vertical slice M1–M2 | Khám phá, inventory, sổ, puzzle sinh hoạt, người thứ năm, phòng thờ | Chơi được toàn tuyến bằng chuột; chết/retry không mất vật phẩm |
| Kiểm chứng M4–M6 | Vòng lặp, cánh cửa, hai bộ hình, báo mộng, mất bảo hộ | Đổi không gian đúng một lần ở M4; M6 thay bảo hộ độc lập hình nền |
| Nội dung và hoàn thiện | M3/M5/M7/M8, manh mối, ending, audio và menu | Không softlock; ending đủ điều kiện; hint và cài đặt không làm đổi tiến độ |

Đây là kế hoạch đề xuất, chưa gán lịch hoặc phân công mới cho thành viên.

## Cấu trúc kỹ thuật

- `UnityProject/`: dự án Unity 6.6 (6000.6.6f1) với package nhúng, có thể mở bằng Unity Hub.
- `Packages/com.4koq.fabricated-dawn/Runtime`: định nghĩa nội dung, trạng thái truyện, checkpoint, prototype cửa M4.
- `Editor`: tạo cấu trúc, validate asset, xuất báo cáo, kiểm tra hành vi.
- `Assets/FabricatedDawn`: dữ liệu màn/phòng/vật phẩm và scene được công cụ tạo; giữ lại khi chạy lại scaffold.
- `fabricated-dawn-assistant`: plugin Codex gồm hai skills và các script Node; không có MCP server riêng.

## Tiêu chí kiểm tra thiết kế

- Người chơi giải thích được vì sao muốn chuyển trọ, vì sao cửa M4 dẫn vào nhà cũ, dao tác động ai và phòng thờ mất an toàn khi nào.
- Không dùng một manh mối ký ức đơn lẻ làm chứng cứ vật lý ngoài hiện thực.
- Sai item không tiêu hao; đồ bắt buộc không mất; sự kiện một lần không lặp; puzzle không reset khi chuyển cảnh.
- Checkpoint lưu đồng bộ mọi bộ sưu tập và trạng thái quan hệ, số lần bảo hộ.
- Giảm âm lượng hoặc hiệu ứng hình ảnh không làm puzzle mất lời giải.
- Chỉ mở ending đã đủ điều kiện trước đó; Game Over được xử lý riêng.

Phần điều khiển, inventory hiển thị, hội thoại, âm thanh, art và các puzzle chính thức cần được triển khai trong yêu cầu tiếp theo. Prototype hiện tại chỉ dùng để kiểm chứng cánh cửa và checkpoint.
