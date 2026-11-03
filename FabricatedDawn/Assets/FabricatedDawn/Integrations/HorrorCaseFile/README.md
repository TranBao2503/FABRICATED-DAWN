# Horror Case File UI cho Fabricated Dawn

Gói nguồn trên máy: **Horror GUI Kit – Case File UI Toolkit**, Walhemic Interactive. Đã nhập từ bộ nhớ Asset Store vào `Assets/HorrorCaseFile`, giữ GUID và các file đi kèm. Gói có 33 layout UXML, 34 stylesheet USS, font, icon, hình minh họa và script UI/demo.

## Chạy

Mở `Assets/FabricatedDawn/Scenes/ToolkitCaseFile.unity`, nhấn **Play**.

- Menu chính: bắt đầu cảnh thử, tiếp tục checkpoint, hướng dẫn.
- HUD: click đồ vật, dùng vật phẩm đang chọn; nút túi đồ, sổ và tạm dừng.
- Túi đồ: dữ liệu inventory thực; chọn một vật phẩm, nhấn **Chọn dùng** để trở lại cảnh.
- Sổ: chỉ hiển thị manh mối đã đọc, không dùng tài liệu mẫu của asset.
- Pause: tiếp tục, lưu/tải checkpoint, tải bản dự phòng, mở sổ, về menu chính.
- **Tab**: túi đồ. **Esc**: tạm dừng/quay lại/đóng cửa sổ khi thích hợp. Hội thoại đang diễn ra cần nhấn tiếp tục/kết thúc.

Scene này là scene khởi chạy đầu tiên trong Build Settings. `ToolkitSandbox.unity` cũ được giữ để đối chiếu.

## Cách tích hợp

`CaseFileAdventureUI` clone các template thật của kit: `S01_MainMenu`, `S07_Hud`, `S11_Inventory`, `S13_Journal`, `S10_PauseMenu`. Nó nối các nút và trường hiển thị với `AdventureSession` và `CheckpointStore` đang có.

Giao diện được đặt trong canvas logic 1920×1080 và co đều theo cửa sổ, giữ tỷ lệ của kit. Các nhãn tiếng Việt dùng font có sẵn của Unity để hiển thị đầy đủ dấu; tiêu đề tiếng Anh vẫn giữ font display của kit.

Các phần không dùng trong gameplay hiện tại như súng, đạn, máu, stamina và vật phẩm mẫu bị ẩn hoặc thay bằng dữ liệu game. Hình phòng hiện vẫn là khối tạm. Ảnh menu đi kèm kit có nhãn hình minh họa, chưa phải bối cảnh chính thức.

Mã và style riêng ở thư mục tích hợp này. Không sửa file nguồn `Assets/HorrorCaseFile`, không dùng demo director của kit làm game controller, không thay luật cốt truyện hoặc tài liệu Google Docs.

Menu Unity:

- **Tools > Fabricated Dawn > Dùng Horror Case File UI**: cấu hình/mở scene tích hợp.
- **Tools > Fabricated Dawn > Kiểm tra tích hợp Case File UI**: chạy kiểm tra core và tham chiếu template, khi scene tích hợp đang mở.

Báo cáo: `Assets/FabricatedDawn/Reports/casefile-ui-verification.json`.

Gói UI không cung cấp các màn chơi Fabricated Dawn, câu đố chính thức hoặc hệ thống truy đuổi. Các màn settings, map, examine, loading và kết thúc của kit chưa được nối trong lượt này.
