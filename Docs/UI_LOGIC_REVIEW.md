# Soát logic UI — 32 màn mockup

Canvas: https://claude.ai/artifact/CKUsrWUzb9ZUodwQa57DSA · Mức độ: 🔴 sai / vi phạm chính sách · 🟠 thiếu đường đi · 🟡 nên sửa.
Mockup chỉ để kiểm luồng & bố cục. Hình ảnh, FX trong mockup là **placeholder**, art thật do ChatGPT vẽ theo `GPT_ASSET_BRIEF.md`.

## A. Vấn đề xuyên suốt cả 3 game
| # | Mức | Vấn đề | Sửa |
|---|---|---|---|
| X1 | 🔴 | **Mời đánh giá (S7) đang "lọc" người dùng**: hỏi "Bạn thích không?", chỉ người trả lời "Thích" mới được đưa lên Store. Google Play cấm review gating. | Bỏ màn hỏi trước. Gọi thẳng **In-App Review API** vào lúc vui (qua màn thứ 3 / kỷ lục mới), tối đa 1 lần/30 ngày. Google tự quyết có hiện hay không. |
| X2 | 🔴 | **Blast & Merge không có đường tới "Tùy chọn quyền riêng tư"** (UMP bắt buộc cho người dùng EEA/UK được đổi lại lựa chọn). Hai game không có Home, Tạm dừng chỉ có bật/tắt. | Tạm dừng thêm nút **"Cài đặt khác"** → màn Cài đặt đầy đủ (S3). |
| X3 | 🟠 | Tạm dừng và Cài đặt trộn lẫn | Chốt 2 tầng: **Tạm dừng** = Tiếp tục · Chơi lại · (Home) · 3 công tắc nhanh · "Cài đặt khác". **Cài đặt** = mọi thứ còn lại (ngôn ngữ, Play Games, mua hàng, quyền riêng tư, version). |
| X4 | 🟠 | Nút "Gỡ quảng cáo" vẫn hiện khi đã mua | Đã mua → ẩn khỏi Home/Tạm dừng, trong Cài đặt đổi thành dòng "Đã gỡ quảng cáo ✓". |
| X5 | 🟠 | Ngôn ngữ: mockup toàn tiếng Việt | Game mặc định theo ngôn ngữ máy, **EN làm dự phòng**. Mọi nút phải chịu được chữ dài hơn 30% (tiếng Đức/Nga sau này). |
| X6 | 🟡 | Icon "Nhạc nền" đang dùng icon ▶ | Cần icon nốt nhạc riêng (thêm vào brief). |
| X7 | 🟡 | Giá tiền | Luôn lấy giá bản địa từ Google Play Billing, không ghi cứng. Chưa tải được giá → nút "Mua" hiện spinner, không hiện giá cũ. |
| X8 | 🟡 | Màu khối/bi phân biệt chỉ bằng sắc độ | Người mù màu: mỗi màu khác nhau cả **độ sáng**, và theme có thể thêm ký hiệu nhỏ (tùy chọn trong Cài đặt). |

## B. Hệ thống
| Màn | Mức | Nhận xét |
|---|---|---|
| S1 Khởi động | 🟡 | Mỗi app một màn khởi động riêng (logo của game đó), không dùng chung logo Eye Merge. Nếu tải < 1s thì bỏ thanh %, chỉ hiện logo. |
| S2 Đồng ý (UMP) | ✅ | Thứ tự đúng: **UMP xong mới khởi tạo SDK quảng cáo**. Bổ sung: người ngoài EEA không thấy form. Mất mạng → không hiện, không chặn chơi. |
| S3 Cài đặt | 🟠 | Thiếu "Khôi phục giao dịch" ở trạng thái đã mua (vẫn cần để đổi máy). Hàng "Ngôn ngữ" chưa có hành vi (mở danh sách). |
| S4 Play Games | 🟠 | Trạng thái 5 "xung đột save" thiếu 2 nút **"Giữ bản trên máy" / "Dùng bản đám mây"** và mặc định chọn bản tiến xa hơn. Với Play Games v2 đăng nhập là tự động; nút "Đăng nhập" chỉ hiện khi lần tự động thất bại. |
| S5 Gỡ quảng cáo | ✅ | Đủ trạng thái. Thêm: mua thành công → toast + hiệu ứng nhận thưởng, không bắt người dùng đóng popup. |
| S6 QC có thưởng | ✅ | Đủ 5 trạng thái. Thêm: hạn mức (vd. tối đa 10 QC/giờ) để không bị Google đánh dấu lạm dụng. |
| S7 Mời đánh giá | 🔴 | Xem X1. |

## C. Arrow Out
| Màn | Mức | Nhận xét |
|---|---|---|
| A1 Home | 🟠 | Người mới thấy 4 chế độ ngay → rối. **Khóa Vô hạn tới màn 10, Daily tới màn 15** (hiện ổ khóa + "Qua màn 10 để mở"). Thiếu chấm đỏ báo "Daily hôm nay chưa chơi". |
| A2 Chọn màn | 🔴 | Trộn 2 cấp điều hướng trên cùng một màn (danh sách chương + lưới màn của chương 5), chương 6–10 bị khuất. Sửa: **màn Chương** (lưới 10 thẻ chương, cuộn dọc) → chạm → **màn Lưới màn của chương đó** (10 ô + thanh sao chương + Back). |
| A3 Hướng dẫn | 🟡 | Màn 1 nên ẩn nút Gợi ý/Chơi lại/Màn trước-sau cho đỡ rối; chỉ còn bàn cờ + bàn tay. Màn 2 mới hiện đủ HUD. |
| A4 Đang chơi | ✅ | Thêm dòng mục tiêu rõ ràng ở màn 1–3: "Gỡ hết mũi tên để qua màn". |
| A5 Chạm sai | 🟡 | Màn 1–5 **không trừ tim** (chỉ giải thích), từ màn 6 mới trừ. Mũi tên cản đường phải nháy sáng, không chỉ mũi tên bị chặn đỏ lên. |
| A6 Qua màn | ✅ | Quảng cáo xen ván chỉ chạy **sau khi bấm "Màn tiếp"**, trước khi màn mới hiện; không bao giờ đè lên bảng thắng. |
| A7 Hết tim | ✅ | Khi không có QC: nút "+1 tim" xám + chữ "Chưa có quảng cáo", không ẩn (để người chơi hiểu vì sao). |
| A8 Hết gợi ý | 🟡 | Quy tắc "mỗi ngày tặng 1 gợi ý" cần hiện ngay trên nút gợi ý (đồng hồ đếm ngược), không chỉ trong popup. |
| A9 Vô hạn | 🟡 | Nút góc phải là "Tạm dừng" còn màn Level là "Cài đặt": thống nhất dùng Tạm dừng ở mọi màn chơi. |
| A10 Daily | 🟠 | "Hạng trong bạn bè" cần quyền danh sách bạn của Play Games (người dùng phải đồng ý riêng) → dùng **bảng xếp hạng ngày toàn cầu**, bạn bè là tùy chọn. |
| A11 Mở chương | 🟡 | Cơ chế mới của chương (mũi tên khóa…) chưa có trong code — mockup đánh dấu "sắp có", không hứa trong store listing. |

## D. Eye Blast
| Màn | Mức | Nhận xét |
|---|---|---|
| B1 Đang chơi | 🟠 | Không có màn Home → mở app vào thẳng ván: cần **tự lưu ván dở** (bàn + 3 khối + điểm) và khôi phục. Mockup ghi nhưng code chưa có. |
| B2 Kéo khối | ✅ | Khối được nâng cao hơn ngón tay 1 ô là đúng. Thêm: bóng đổ dưới khối đang kéo (xem FEEL_SPEC). |
| B3 Combo | ✅ | Chữ khen theo số hàng: 2 = "Tuyệt!", 3 = "Xuất sắc!", ≥4 = "Không thể tin nổi!". |
| B4 Hết chỗ | ✅ | Thứ tự: bàn xám dần (0.6s) → bảng. |
| B5 Kỷ lục mới | 🟠 | Chưa rõ khi nào hiện. Quy tắc: thua mà phá kỷ lục → **màn Kỷ lục mới trước**, bấm "Tiếp" mới tới bảng Hồi sinh/Chơi lại. Kỷ lục phá **giữa ván** chỉ hiện toast nhỏ, không dừng game. |
| B6 Tạm dừng | 🔴 | Xem X2. |

## E. Eye Merge
| Màn | Mức | Nhận xét |
|---|---|---|
| M1 Đang chơi | 🟠 | Như B1: tự lưu ván dở (vị trí + cấp từng bi). Nút quà (skin) trên HUD dễ bấm nhầm khi đang ngắm → dời sang Tạm dừng / Game Over, HUD chỉ còn Tạm dừng. |
| M2 Hợp thể lớn | 🔄 | Viết lại theo chuỗi mới trong `FEEL_SPEC.md` (hút → flash → toon splat → bật lên). |
| M3 Nguy hiểm | ✅ | Thêm tiếng tim đập nhỏ + rung nhẹ; không đếm số trên màn nếu làm người chơi hoảng — thử cả 2 bản. |
| M4 Hũ đầy | ✅ | "Đổi skin" ở đây hợp lý (lúc người chơi đang nghỉ). |
| M5 Cửa hàng | 🟠 | Đã mua trọn bộ → ẩn banner trọn bộ, mọi thẻ hiện "Dùng". Thiếu nút "Khôi phục giao dịch" trong cửa hàng. Theme đang dùng luôn ở vị trí đầu. |
| M6 Chi tiết skin | 🟠 | Đổi skin giữa ván: **áp dụng từ ván sau** (đang chơi giữ skin cũ) — cần dòng thông báo. Mở bằng 5 QC: tiến độ QC lưu vĩnh viễn, không reset theo tuần (reset gây ức chế); giới hạn là **1 theme mở bằng QC tại một thời điểm**. |
| M7 Tạm dừng | 🔴 | Xem X2. |

## F. Việc tiếp theo
1. Sửa các mục 🔴 trên canvas (S7, A2, B6/M7) — sau khi có asset GPT đợt đầu để không phải vẽ lại 2 lần.
2. Chốt `GPT_ASSET_BRIEF.md` → bạn gen đợt 1 (style bible) → duyệt → gen các đợt còn lại.
3. Làm lại M2/B3 theo `FEEL_SPEC.md` bằng asset GPT.
