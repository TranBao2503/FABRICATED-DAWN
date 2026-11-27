# Fabricated Dawn — demo Màn 1–4

## Mở bản hiện tại

- Unity Hub: thêm thư mục `FabricatedDawn` trong repo, dùng Unity **6000.6.6f1**.
- Mở `Assets/FabricatedDawn/Scenes/RushDemo.unity`, nhấn **Play → Bắt đầu**.
- Khi cần dựng lại dữ liệu: **Tools → Fabricated Dawn → Demo Màn 1–4 → Chuẩn bị và mở**. Lệnh này dựng lại riêng dữ liệu DemoFour; sửa nội dung lâu dài trong RushFourSetup.cs để lần dựng sau không mất.
- Kiểm tra logic: **Demo Màn 1–4 → Kiểm tra mạch chơi**. Báo cáo nằm trong `Assets/FabricatedDawn/Reports/rush-four-checks.json`.
- Tab mở túi, Esc tạm dừng. Chọn vật phẩm rồi nhấn mục tiêu. Sổ manh mối giữ toàn bộ lời kể và dấu vết đã thu thập.

## Phạm vi

Đọc theo GDD gốc mục 5.2–5.5. Google Docs gốc chỉ đọc. Bỏ Prologue dài; mở bằng bối cảnh Nam cùng ba bạn thuê nhà, học Đại học Thủy Lợi tại Hà Nội năm 2004.

- Màn 1: sinh hoạt, bát thứ năm, gương/dấu tay, đường tới **phòng thờ riêng trên tầng 2**, bóng đỏ đêm khuya.
- Màn 2: Vỗ Vai Ma, chạy tới phòng thờ, bác quản lý nối lại điện. Chọn sai lần đầu có vật hộ thân cứu; sai tiếp dẫn đến Bad End.
- Màn 3: tìm giấy thuê và ảnh; gọi người nhà/bà cho thuê, hỏi hàng xóm; đối chiếu ba kết luận có chứng cứ; nhận vật trấn, tìm chìa cửa phụ và gom hành lý.
- Màn 4: cầu thang lặp, tìm dấu mốc cố định qua hai lượt, mở khóa cửa phụ. **Một lần mở cửa đổi toàn bộ nhà**, đưa nhóm về lối vào của căn nhà cũ. Kiểm tra cửa/cửa sổ/phòng kín để hiểu đã bị giữ lại, rồi dừng ở mốc cuối Màn 4.

Demo đầy đủ vẫn dự kiến **8 màn**. Mốc cuối Màn 4 chưa phải ending thường. Không bật True Ending. Phòng thờ vẫn còn bảo hộ; không tự cho nó mất hiệu lực ở Màn 4. Chưa tiết lộ nguyên nhân thật về cái chết của bé.

## Tự chơi nhanh

1. Lấy chìa trên bàn phòng ngủ, chọn chìa → mở tủ → đọc lời nhắn. Xuống phòng khách, nói hết với cả ba bạn và xem bát nhỏ trên mâm.
2. Lên WC 2: rửa mặt → nhìn gương → quay lại → xem dấu tay. Ghé phòng thờ đọc đường đi. Về phòng ngủ, nghỉ đến khuya; ra hành lang xem bóng đỏ, đi WC, quay về xem bóng trước cửa, về ngủ. Qua vài ngày, nói chuyện nhóm rồi chơi Vỗ Vai Ma.
3. Nghe luật, đi lần lượt bốn góc, chạy lên tầng 2 → phòng thờ. Nói với bác quản lý, đợi sáng. Có thể thử đường sai lần đầu để xem vật hộ thân cứu; lần sau sẽ Bad End và tải checkpoint để thử lại.
4. Xem giấy thuê và ảnh, gọi hai cuộc điện thoại, hỏi hàng xóm. Ghé hàng tạp hóa hỏi người lớn tuổi, nhận vật trấn; nhang/diêm/pin là đồ mua thêm. Trở lại bàn đối chiếu, chọn ba điều có chứng cứ rồi chốt. Lên gác lấy chìa cửa phụ, nói chuyện gom đồ, xuống cầu thang.
5. Nhìn ô gạch dưới cửa thoáng, thử xuống thang, quan sát ô gạch lần hai. Theo dấu gạch về bếp, chọn chìa cửa phụ → mở chốt → mở cửa.
6. Đóng đoạn chuyển cảnh, kiểm tra cửa ra và cửa sổ. Lên chiếu nghỉ cũ, vào cửa phòng mới xuất hiện, đối chiếu thỏ vải với ảnh. Chốt nhận định, chọn ngồi lại tính đường để tới mốc cuối Màn 4.

## Hình, thoại và âm thanh tạm

Nền trong `Art/RushFour` là các bản vẽ lại bằng **OpenAI Imagegen**, hướng nét mực mảnh và màu gouache lì. Đây là art tạm chờ nhóm duyệt; không phải ảnh nhóm tự vẽ. Nền cũ và kho AssetVault được giữ lại. Vật phẩm tiếp tục dùng AssetVault; chưa vẽ lại toàn bộ icon.

Nhân vật dùng `StickFigureElement`, không nhúng chân dung mới. Khi có ảnh được duyệt, giữ hotspot ID/vị trí, đặt `artwork`, bỏ `stickFigure` là thay được. NPC, lời thoại và lựa chọn giải đố là bản triển khai dựa trên các tình tiết GDD, **chưa phải câu chữ đã được nhóm duyệt**. Cách xưng hô giản dị, phù hợp nhóm sinh viên và người lớn tuổi; tránh cổ trang hóa bối cảnh 2004.

Không thêm âm thanh cho bản này. Những tiếng động quan trọng được diễn tả bằng chữ để còn hiểu diễn biến.

## Dữ liệu và kiểm tra

- `Data/DemoFour/RushDemo.asset`: dữ liệu riêng, không ghi đè ChapterOne/StoryDemo.
- `RushFourSetup.cs`: dựng các màn, thoại, manh mối, vật phẩm và mục tiêu.
- `RushFourVerification.cs`: chạy đường chơi đủ 1–4, bảo vệ nhánh chưa đủ chứng cứ, dùng sai đồ, lặp đường không mất manh mối, cứu một lần và Bad End, checkpoint trước/sau đổi thế giới, giới hạn ending. Chạy kèm các kiểm tra ChapterOne và StoryDemo cũ.
- Checkpoint dùng content ID riêng `fabricated-dawn-rush-four-v1`, tách khỏi các bản thử cũ. Hội thoại chỉ cấp manh mối khi nghe hết; UI lưu lại sau cuộc nói chuyện.
- Không có sửa âm thầm Google Docs, không tự chốt các tình tiết Màn 5–8.

Xác minh ngày 27/11/2026 trên Unity 6000.6.6f1: bộ demo mới 29/29 kiểm tra, ChapterOne 29/29, StoryDemo 26/26 (có kiểm tra toolkit chung), giao diện Play 18/18 bước. Đã bấm thử mở đầu, nhặt/chọn/dùng chìa, đi cầu thang và chuyển thoại bằng chuột trong Unity. Báo cáo UI: [rush-ui-checks.json](verification/rush-ui-checks.json).
