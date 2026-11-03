# Fabricated Dawn — dự án chính mới

Được tạo mới bằng Unity **6000.6.6f1** ngày 03/11/2026. Dự án mẫu `../UnityProject` được giữ nguyên.

Mở thư mục này bằng Unity Hub. Scene bắt đầu hiện tại: `Assets/FabricatedDawn/Scenes/ToolkitCaseFile.unity`.

Đã tích hợp gói UI **Horror GUI Kit – Case File UI Toolkit** có sẵn trên máy: menu chính, HUD, inventory, journal và pause được nối với dữ liệu toolkit. Xem [hướng dẫn tích hợp](Assets/FabricatedDawn/Integrations/HorrorCaseFile/README.md).

Scene dùng UI cơ bản trước đó vẫn được giữ tại `Assets/FabricatedDawn/Scenes/ToolkitSandbox.unity`.

## Thử toolkit

Nhấn **Play**. Cảnh hiện tại dùng hình khối và nội dung thử, chưa phải Màn 1 chính thức.

1. Nhấn bạn cùng phòng để thử hội thoại.
2. Thử cửa phòng: bị khóa theo tiến độ.
3. Nhặt cục chặn giấy, chọn trong túi và nhấn tủ: có phản hồi dùng sai, đồ không mất.
4. Nhặt chìa khóa, chọn chìa trong túi, nhấn tủ: tủ mở, chìa được dùng một lần.
5. Đọc tờ giấy: manh mối vào sổ, lối ra hành lang được mở.
6. Ra hành lang, đọc bảng tin, quay lại: tủ vẫn mở, đồ đã nhặt không xuất hiện lại.
7. Thử lưu/tải checkpoint, đóng rồi chạy lại Play mode và tải để kiểm tra lưu trên đĩa.

Mọi thao tác chính có nút chuột. **Tab** ẩn/hiện túi đồ; **Esc** mở/đóng tạm dừng. Khi tạm dừng, thời gian game dừng. Hội thoại và các cửa sổ nổi chặn tương tác với khung cảnh phía sau.

## Các hệ thống đã có

- Hotspot theo tọa độ chuẩn hóa, có điều kiện hiện/ẩn và phản hồi khóa.
- Nhặt, chọn, dùng vật phẩm; chống nhặt lặp và tiêu hao khi dùng sai.
- Puzzle, sự kiện và quyền ra vào khu vực được ghi bằng ID.
- Hội thoại nhiều dòng, sổ manh mối có thể xem lại.
- Giao diện UI Toolkit, giữ tỷ lệ khung cảnh 16:9 trong vùng hiển thị.
- Checkpoint toàn bộ trạng thái trên đĩa; ghi qua file tạm, giữ bản trước ở `.bak`, kiểm tra phiên bản/nội dung/checksum trước khi tải.
- Validator nội dung và bộ kiểm tra hành vi.
- Các cơ chế nền M4/M6 và dao ký ức được giữ từ toolkit trước.

Checkpoint tự lưu sau các mốc của cảnh thử; có nút lưu thủ công, tải và khôi phục bản dự phòng trong menu tạm dừng. Bắt đầu lại cảnh thử không xóa checkpoint cũ ngay lập tức.

Save runtime nằm ở `Application.persistentDataPath/toolkit-checkpoint.json`. Trong Editor Windows, với tên sản phẩm hiện tại, Unity thường đặt thư mục dữ liệu dưới `AppData/LocalLow/4KoQ/Fabricated Dawn`. Dữ liệu kiểm thử tự động dùng thư mục `Temp/ToolkitChecks` của dự án.

## Điểm chỉnh sửa khi có asset

Chọn **Tools > Fabricated Dawn > Chọn dữ liệu tương tác**, hoặc mở:

`Assets/FabricatedDawn/Data/Sandbox/ToolkitAdventure.asset`

- `Rooms`: tên phòng, background hiện tại/quá khứ, hotspot.
- `Hotspots`: bounds 0..1, loại thao tác, item/manh mối/hội thoại, điều kiện và kết quả.
- `Items`: tên, icon, số lần dùng, nguồn gốc.
- `Clues`, `Dialogues`, `Objective Updates`: nội dung hiển thị.

Đổi hình và bố cục thông qua asset; giữ ID và GUID `.meta`. Không cần sửa code để thay icon, background hoặc các câu hội thoại mẫu. Không đổi `Content ID` tùy tiện: checkpoint chỉ nhận đúng bản nội dung.

Code lõi ở `Packages/com.4koq.fabricated-dawn`, phiên bản cục bộ **0.2.0**. Đây là bản phát triển mới trong dự án này; plugin Codex riêng tư 0.1.0 trước đó chưa được cập nhật trên tài khoản.

## Kiểm tra

Trong Unity chọn **Tools > Fabricated Dawn > Toolkit > Kiểm tra toàn bộ toolkit**, khi scene toolkit đang mở và đã dừng Play.

Kết quả: `Assets/FabricatedDawn/Reports/toolkit-verification.json`.

Kiểm tra tự động gồm vòng tương tác từ đầu tới cuối, sai vật phẩm, điều kiện cửa, dữ liệu giữ qua chuyển cảnh, hội thoại chặn input, lưu/tải vào session mới, bản dự phòng và từ chối dữ liệu hỏng/sai phiên bản.

Chưa có art/audio chính thức, ghép vật phẩm, puzzle nhập mã/kéo thả, hệ thống truy đuổi hoàn chỉnh, cài đặt âm thanh hoặc đầy đủ tám màn. Đây là nền để triển khai các phần đó theo thiết kế được duyệt.
