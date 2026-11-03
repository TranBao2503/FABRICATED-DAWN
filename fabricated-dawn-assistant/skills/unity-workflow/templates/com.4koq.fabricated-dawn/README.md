# Fabricated Dawn Toolkit 0.1.0

Bộ công cụ chuẩn bị game 2D point-and-click cho nhóm 4KoQ. Yêu cầu Unity 6; đã kiểm thử batch với 6000.6.6f1. Không cần đăng nhập hoặc khóa API cho gói Unity này.

## Mở dự án đã chuẩn bị

Thêm thư mục `UnityProject` ở workspace vào Unity Hub, mở với 6000.6.6f1. Vào **Tools > Fabricated Dawn > Toolkit**.

- Tạo dữ liệu: 8 chapter, 8 phòng, 13 vật phẩm; chạy lại giữ nguyên asset đã có.
- Kiểm tra dữ liệu: ID, số màn, tham chiếu, hotspot, vật phẩm dao, checkpoint.
- Tạo / mở prototype M4: mở scene `Assets/FabricatedDawn/Scenes/M4_DeveloperPrototype.unity`, sau đó nhấn Play.
- Xuất JSON: báo cáo nội dung cho Codex trong `Assets/FabricatedDawn/Reports`.
- Kiểm tra trạng thái: chạy 16 kiểm tra hành vi và xem kết quả Console.

Prototype M4 có các nút quan sát dấu mốc, tìm chìa và mở cửa. Toàn bộ lớp mô tả đổi một lần sang không gian quá khứ; bảo hộ phòng thờ vẫn Active. Có nút lưu/retry checkpoint trong bộ nhớ. Đây là màn kiểm chứng cơ chế, chưa có art, audio hoặc gameplay hoàn chỉnh.

## Cài vào dự án khác

Trong Unity Package Manager, chọn **Install package from disk**, rồi chọn `package.json` của gói đã giải nén. Hoặc sao chép nguyên thư mục `com.4koq.fabricated-dawn` vào `Packages` của một dự án đóng. Tránh ghi đè gói đã có. Có thể dùng script `install-toolkit.mjs` đi kèm plugin Codex.

Các assembly Runtime/Editor tách riêng. Gói dùng các module Unity `imgui` và `jsonserialize`; không yêu cầu URP, Input System, NUnit hoặc gói trả phí.

## Kiểm thử qua dòng lệnh

Đóng dự án trong Unity trước khi chạy; dùng executable và đường dẫn đúng của máy:

```powershell
& 'C:\path\Editor\Unity.exe' -batchmode -nographics -quit -projectPath 'C:\path\Project' -executeMethod FabricatedDawn.Editor.Batch.Verify -logFile 'C:\path\verify.log'
```

`Scaffold`, `Validate`, `Verify` là ba entrypoint dưới `FabricatedDawn.Editor.Batch`.

Kết quả trong `Assets/FabricatedDawn/Reports`. Cảnh báo thiếu background/icon là dự kiến ở bản chuẩn bị; lỗi nội dung làm lệnh batch trả mã khác 0. Không sửa asset có sẵn chỉ để xóa cảnh báo hình tạm.

## Giới hạn

- Dữ liệu phòng/hotspot là schema để nhóm điền, chưa tự hiển thị một bản đồ hoàn chỉnh. Prototype M4 dùng giao diện IMGUI riêng.
- `StorySession` là lõi mẫu. Checkpoint được serialize thành JSON; gói chưa có hệ thống file save nhiều slot, migration hoặc autosave trên đĩa.
- Điều kiện ending và ID manh mối là mẫu tạm. True Ending mặc định khóa chờ duyệt cốt truyện.
- Chưa xác minh Play mode bằng thao tác UI hoặc build phát hành. Kết quả batch chỉ xác minh biên dịch, tạo asset/scene và các kiểm tra trạng thái.
- Không có kết nối live MCP điều khiển Unity; gói Editor không đọc hoặc ghi Google Docs.

Nguồn thiết kế: https://docs.google.com/document/d/1AlE5j8_13of6geX94sKJDvszqVpkJn0DIIvbFeWY168/edit?tab=t.0 . Tài liệu nguồn không bị chỉnh sửa.

Tài liệu cấu trúc gói Unity: https://docs.unity.com/en-us/engine/6000.6/manual/packages-list/cus-pkg-lp/cus-pkg-development/cus-asmdef .
