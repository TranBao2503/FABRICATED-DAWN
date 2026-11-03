# Fabricated Dawn Assistant

Plugin Codex gồm hai workflow:

1. **document-reader:** đọc tài liệu Google Docs qua Google Drive đã kết nối, tóm tắt yêu cầu, trả lời theo nguồn, phát hiện mâu thuẫn. Không có thao tác ghi tài liệu trong workflow này.
2. **unity-workflow:** chuẩn bị và kiểm tra dự án Unity, làm việc với C#, trạng thái truyện và checkpoint, cài gói Editor đi kèm vào dự án người dùng chỉ định.

Gói Unity nằm trong `skills/unity-workflow/templates/com.4koq.fabricated-dawn`. Mở README của gói để xem hướng dẫn Editor window và prototype M4.

## Sử dụng sau khi thêm plugin vào Codex

- “Đọc tài liệu Fabricated Dawn, tóm tắt và chỉ ra các điểm chưa chốt. Không sửa tài liệu.”
- “Kiểm tra dự án Unity tại [đường dẫn], chỉ báo lỗi, chưa sửa.”
- “Cài toolkit Fabricated Dawn vào dự án [đường dẫn] và tạo cấu trúc dữ liệu.”

Plugin là gói skills và script local, không có MCP server riêng. Google Docs cần Google Drive được kết nối trong phiên chat. Script Unity cần Node.js và Unity Editor cài sẵn. Việc lưu plugin vào tài khoản không đồng nghĩa đã cài/chọn trong mọi chat.

Nguồn thiết kế được tham chiếu bằng link và bản tóm tắt trong `references/project-brief.md`; không đóng gói bản sao đầy đủ tài liệu cá nhân hay thông tin cấp phép. Không gửi dữ liệu dự án tới dịch vụ bên ngoài qua các script local.

Tài liệu định dạng plugin: https://developers.openai.com/plugins/build/plugins .
