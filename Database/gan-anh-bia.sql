/* Gắn ảnh bìa minh họa cho 21 sách mẫu.
   1) Chép thư mục covers vào wwwroot\covers của dự án.
   2) Chọn đúng database rồi chạy script này trong SSMS. */
USE [CSDL_QL_TV];   -- đổi nếu database của bạn tên khác
GO
UPDATE Books SET CoverImagePath = N'/covers/seed-01.jpg' WHERE ISBN = N'9786040000001';   -- Cho tôi xin một vé đi tuổi thơ
UPDATE Books SET CoverImagePath = N'/covers/seed-02.jpg' WHERE ISBN = N'9786040000002';   -- Mắt biếc
UPDATE Books SET CoverImagePath = N'/covers/seed-03.jpg' WHERE ISBN = N'9786040000003';   -- Tôi thấy hoa vàng trên cỏ xanh
UPDATE Books SET CoverImagePath = N'/covers/seed-04.jpg' WHERE ISBN = N'9786040000004';   -- Dế Mèn phiêu lưu ký
UPDATE Books SET CoverImagePath = N'/covers/seed-05.jpg' WHERE ISBN = N'9786040000005';   -- Chí Phèo
UPDATE Books SET CoverImagePath = N'/covers/seed-06.jpg' WHERE ISBN = N'9786040000006';   -- Lão Hạc
UPDATE Books SET CoverImagePath = N'/covers/seed-07.jpg' WHERE ISBN = N'9786040000007';   -- Cánh đồng bất tận
UPDATE Books SET CoverImagePath = N'/covers/seed-08.jpg' WHERE ISBN = N'9786040000008';   -- Nhà giả kim
UPDATE Books SET CoverImagePath = N'/covers/seed-09.jpg' WHERE ISBN = N'9786040000009';   -- Đắc nhân tâm
UPDATE Books SET CoverImagePath = N'/covers/seed-10.jpg' WHERE ISBN = N'9786040000010';   -- Quẳng gánh lo đi và vui sống
UPDATE Books SET CoverImagePath = N'/covers/seed-11.jpg' WHERE ISBN = N'9786040000011';   -- Sapiens: Lược sử loài người
UPDATE Books SET CoverImagePath = N'/covers/seed-12.jpg' WHERE ISBN = N'9786040000012';   -- Homo Deus
UPDATE Books SET CoverImagePath = N'/covers/seed-13.jpg' WHERE ISBN = N'9786040000013';   -- 21 bài học cho thế kỷ 21
UPDATE Books SET CoverImagePath = N'/covers/seed-14.jpg' WHERE ISBN = N'9786040000014';   -- Harry Potter và Hòn đá phù thủy
UPDATE Books SET CoverImagePath = N'/covers/seed-15.jpg' WHERE ISBN = N'9786040000015';   -- Harry Potter và Phòng chứa bí mật
UPDATE Books SET CoverImagePath = N'/covers/seed-16.jpg' WHERE ISBN = N'9786040000016';   -- Clean Code
UPDATE Books SET CoverImagePath = N'/covers/seed-17.jpg' WHERE ISBN = N'9786040000017';   -- Clean Architecture
UPDATE Books SET CoverImagePath = N'/covers/seed-18.jpg' WHERE ISBN = N'9786040000018';   -- The Clean Coder
UPDATE Books SET CoverImagePath = N'/covers/seed-19.jpg' WHERE ISBN = N'9786040000019';   -- Rừng Na Uy
UPDATE Books SET CoverImagePath = N'/covers/seed-20.jpg' WHERE ISBN = N'9786040000020';   -- Kafka bên bờ biển
UPDATE Books SET CoverImagePath = N'/covers/seed-21.jpg' WHERE ISBN = N'9786040000021';   -- Hoàng tử bé
GO

-- Kiểm tra (phải thấy 21 dòng có đường dẫn ảnh):
SELECT Title, CoverImagePath FROM Books ORDER BY Id;
