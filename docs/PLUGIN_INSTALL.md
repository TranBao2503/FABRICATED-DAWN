# Cài và sử dụng

## Codex

Plugin riêng tư đã được lưu vào tài khoản:

[Fabricated Dawn Assistant](https://chatgpt.com/plugins/plugins_6ae89af08fcc8191835a0a977cd9b105)

Mở link và thêm/chọn plugin trong Codex. Nếu chat hiện tại chưa nạp skills, chọn plugin trong một chat mới. Gói gồm `document-reader` và `unity-workflow`; không có MCP server hoặc tool live Unity riêng.

Google Docs dùng Google Drive đã kết nối với host. Workflow đọc không sửa tài liệu. Các script local cần Node.js; thao tác Unity cần Unity Editor.

## Unity

Dự án đã chuẩn bị nằm trong `UnityProject` tại workspace. Thêm thư mục này vào Unity Hub, mở với **6000.6.6f1**, sau đó chọn **Tools > Fabricated Dawn > Toolkit**.

Muốn cài vào một dự án khác: mở Package Manager, chọn **Install package from tarball**, chọn `dist/com.4koq.fabricated-dawn-0.1.0.tgz`. Không cài chồng vào dự án đã có package cùng tên.

Gói ZIP plugin Codex lưu tại `dist/fabricated-dawn-assistant-0.1.0.zip`. Cả hai gói chỉ chứa mã công cụ, hướng dẫn và bản tổng hợp thiết kế; không chứa bản sao đầy đủ của Google Docs.

## Bằng chứng đã chạy

- Unity 6000.6.6f1 trả mã 0, biên dịch mã và tạo scene/dữ liệu.
- 16/16 kiểm tra hành vi qua; scaffold chạy lại giữ nguyên catalog.
- Báo cáo nội dung: 8 màn, 8 phòng, 13 vật phẩm, không có lỗi. Cảnh báo art/icon tạm và True Ending chưa duyệt là dự kiến.
- Kiểm tra gói và helper: hai skill, JSON hợp lệ, đường dẫn tham chiếu tồn tại, mã Unity trong plugin trùng nguồn đã thử, installer giữ manifest và từ chối ghi đè, batch từ chối lệnh ngoài danh sách/Unity đang mở.
- Chưa kiểm chứng thao tác UI trong Play mode hoặc build phát hành.

Thông tin phiên bản để cập nhật sau này: plugin `plugins_6ae89af08fcc8191835a0a977cd9b105`, release `pluginrel_6ae89af316008191ae348a503ab650c9`, phiên bản `0.1.0`, riêng tư theo tài khoản.
