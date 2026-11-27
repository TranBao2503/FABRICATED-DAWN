# Fabricated Dawn

Game kinh dị giải đố point-and-click Việt Nam của nhóm 1, lớp thiết kế game 65CNTT.

## Chơi bản hiện tại — Màn 1–4

1. Unity Hub → **Add project from disk** → chọn thư mục **`FabricatedDawn`** trong repo. Dùng Unity **6000.6.6f1**.
2. Mở `Assets/FabricatedDawn/Scenes/RushDemo.unity`, nhấn **Play → Bắt đầu**.
3. Khi cần dựng lại: **Tools → Fabricated Dawn → Demo Màn 1–4 → Chuẩn bị và mở**.

Mở đầu giới thiệu Nam và nhóm sinh viên Thủy Lợi thuê nhà ở Hà Nội năm 2004. Chơi liên tục tới hết Màn 4: sinh hoạt và điềm lạ → Vỗ Vai Ma → hỏi chuyện/đối chiếu chứng cứ → cầu thang lặp và cánh cửa đổi cả căn nhà. Phòng thờ là phòng riêng trên tầng 2. Nhân vật là người que tạm; chưa thêm âm thanh. Nền và thoại triển khai đang chờ nhóm duyệt.

**Bản demo đầy đủ vẫn dự kiến 8 màn** với Bad End và ending thường. Cuối Màn 4 hiện là mốc tạm dừng phát triển, chưa phải ending thường.

Xem [cách chơi, phạm vi, nguồn art và kiểm tra](docs/DEMO_1_4.md). Các scene `ChapterOne`, `StoryDemo` cũ được giữ để tham khảo; scene hiện tại là **RushDemo**.

## Công cụ dự án

- **Fabricated Dawn Assistant**: plugin Codex đọc tài liệu và hỗ trợ phát triển Unity; [hướng dẫn](fabricated-dawn-assistant/README.md).
- **Fabricated Dawn Toolkit**: UPM package trong `FabricatedDawn/Packages/com.4koq.fabricated-dawn`, có dữ liệu, inventory, tương tác, checkpoint và các công cụ Editor.
- `UnityProject` là scaffold toolkit ban đầu; dùng **FabricatedDawn** để mở bản game hiện tại.
- Google Docs gốc chỉ được đọc; không sửa, bình luận hay thay quyền chia sẻ.

## Tài liệu

- [Hướng dẫn demo 1–4](docs/DEMO_1_4.md)
- [Kế hoạch dự án ban đầu](docs/PROJECT_PLAN.md)
- [Bản đọc thiết kế](fabricated-dawn-assistant/skills/unity-workflow/references/project-brief.md)
- [Hướng dẫn Unity Toolkit](FabricatedDawn/Packages/com.4koq.fabricated-dawn/README.md)

## Kiểm tra

**Tools → Fabricated Dawn → Demo Màn 1–4 → Kiểm tra mạch chơi** kiểm tra đường chơi đầy đủ, nhánh lỗi, checkpoint và giới hạn ending; chạy kèm bộ kiểm tra ChapterOne và StoryDemo cũ. Báo cáo JSON trong `FabricatedDawn/Assets/FabricatedDawn/Reports`.

**Kiểm tra giao diện trong Play** chạy một tour UI trong Unity, kiểm tra vùng bấm và khung chữ; ảnh và báo cáo ở thư mục tạm `FabricatedDawn-RushUI`. Đây là kiểm tra bản chơi trong Editor, chưa phải xác nhận chất lượng bản build phát hành.
