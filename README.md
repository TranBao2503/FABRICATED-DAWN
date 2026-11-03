# Fabricated Dawn — plugin và chuẩn bị dự án

Game do nhóm 1 lớp thiết kế game 65CNTT làm

Đã tạo hai phần đi cùng nhau:

- **Fabricated Dawn Assistant:** plugin Codex để đọc tài liệu và hỗ trợ phát triển Unity.
- **Fabricated Dawn Toolkit:** gói UPM có công cụ Unity Editor, dữ liệu khởi tạo và prototype M4.

Google Docs gốc chỉ được đọc, không bị sửa, thêm bình luận hay thay quyền chia sẻ.

## Bắt đầu trong Unity

1. Trong Unity Hub, thêm thư mục `UnityProject` của workspace này. Dùng Unity **6000.6.6f1** đã cài trên máy.
2. Mở **Tools > Fabricated Dawn > Toolkit**.
3. Chọn **Tạo / mở prototype M4**, nhấn Play để thử thao tác quan sát dấu mốc → mở khóa → mở cửa.

Các asset 8 màn, 8 phòng và 13 vật phẩm đã được tạo trong `Assets/FabricatedDawn/Data`. Phần hình ảnh, hotspot nội dung chính thức, âm thanh và hội thoại vẫn cần đội game xây dựng.

## Dùng plugin Codex

Mở link plugin được trả trong chat, thêm/chọn plugin để dùng hai skills. Đọc Google Docs cần Google Drive đã kết nối. Plugin có chỉ dẫn đọc nguồn, giữ nguyên tài liệu và phân biệt yêu cầu chính thức với đề xuất triển khai.

Các script Node đi kèm hỗ trợ kiểm tra dự án, cài gói vào dự án được chỉ định và gọi ba lệnh Unity batch có phạm vi cố định. Không có server MCP hay kết nối live với Unity Editor.

## Tài liệu và gói cài

- [Kế hoạch dự án](docs/PROJECT_PLAN.md)
- [Bản đọc thiết kế và các điểm cần chốt](fabricated-dawn-assistant/skills/unity-workflow/references/project-brief.md)
- [Hướng dẫn Unity Toolkit](UnityProject/Packages/com.4koq.fabricated-dawn/README.md)
- [Plugin Codex](fabricated-dawn-assistant/README.md)
- `dist/fabricated-dawn-assistant-0.1.0.zip`: gói plugin Codex.
- `dist/com.4koq.fabricated-dawn-0.1.0.tgz`: gói Unity Package Manager, chọn **Install package from tarball** nếu cài vào dự án khác.

## Kiểm chứng

Unity 6000.6.6f1 đã biên dịch, tạo dữ liệu/scene và chạy **16/16 kiểm tra hành vi**. Chạy lại scaffold không thay nội dung catalog đã có.

Xem `UnityProject/Assets/FabricatedDawn/Reports/verification-report.json` và `content-report.json`. Thiếu background/icon và True Ending chưa duyệt là cảnh báo dự kiến. Đây là bộ khởi đầu, chưa phải tám màn chơi hoàn chỉnh. Chưa kiểm tra tương tác UI trong Play mode hoặc build phát hành.
