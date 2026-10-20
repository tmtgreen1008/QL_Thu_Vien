# Quản lý thư viện

Bài tập lớn học phần **Lập trình Web** – Trường Đại học Giao thông Vận tải, Khoa Công nghệ thông tin.
Đề tài: **Website quản lý thư viện** bằng ASP.NET Core MVC.

- Sinh viên: Tô Minh Trường – Lớp CNTT2 – Khoá K63
- Giảng viên hướng dẫn: Lại Mạnh Dũng

## Giới thiệu
Hệ thống phục vụ một thư viện hoạt động theo kiểu cửa hàng: khách có thể đọc tại chỗ hoặc mua thẻ thành viên để mượn sách về nhà.
Có hai vai trò **Admin** (vận hành tại quầy) và **User** (khách hàng), cùng phần công khai cho khách vãng lai.

## Chức năng chính
- **Khu quản trị (`/Admin`)**: tác giả, thể loại, đầu sách và bản sao (mã vạch tự sinh, ảnh bìa); gói thẻ, khách hàng, bán thẻ, cọc; cho mượn, trả, gia hạn, phạt, đền, ghi nợ, thu nợ; sổ giao dịch (lọc, xuất CSV, hủy có lý do); 5 báo cáo.
- **Công khai**: tra cứu sách (tìm, lọc, sắp xếp, phân trang, AJAX), bảng giá, giỏ quan tâm (Session), sách xem gần đây (Cookie).
- **Khách hàng (`/My`)**: thẻ, sách đang mượn, lịch sử, thanh toán, gia hạn trực tuyến, hồ sơ.
- **REST API**: `/api/books`, `/api/books/{id}`, `/api/categories`, `/api/me/loans`, `POST /api/me/loans/{id}/renew`.
- Responsive, Paging, Filtering, AJAX, Session, Cookie, Annotation, bảo mật (Identity, CSRF, XSS, khóa tài khoản), quốc tế hóa tiếng Việt và tiếng Anh.

## Công nghệ
ASP.NET Core MVC (.NET 10), C#, Entity Framework Core, SQL Server, ASP.NET Core Identity, Bootstrap 5, jQuery.

## Cài đặt nhanh
Yêu cầu: .NET 10 SDK, SQL Server (Express hoặc Developer), SQL Server Management Studio.

1. Mở `Database/01_TaoDatabase.sql` trong SSMS và chạy để tạo database `CSDL_QL_TV` cùng 17 bảng.
2. Sửa chuỗi kết nối `DefaultConnection` trong `appsettings.json` cho đúng máy chủ SQL của bạn.
3. Chạy ứng dụng (`dotnet run --launch-profile https` hoặc nút chạy trong Visual Studio). Lần đầu ứng dụng tự nạp vai trò, tài khoản mẫu và 21 đầu sách.
4. Mở `Database/gan-anh-bia.sql` và chạy một lần để gắn ảnh bìa cho các sách mẫu.
5. Mở trình duyệt tại địa chỉ ứng dụng in ra (ví dụ `https://localhost:7105`).

Chi tiết hơn: xem `docs/Huong-dan-cai-dat.docx`.

## Tài khoản mẫu (chỉ dùng để thử nghiệm)
| Vai trò | Email | Mật khẩu |
|---|---|---|
| Admin | admin@library.local | Admin@123 |
| Khách hàng (TV000001) | khach@library.local | Khach@123 |

## Cấu trúc thư mục
```
Areas/Admin/       Khu quản trị (controller, dịch vụ, view)
Areas/Identity/    Trang đăng nhập, đăng ký tùy biến
Controllers/       Công khai, khách hàng và REST API
Data/              DbContext, nạp dữ liệu mẫu
Models/            Thực thể và kiểu liệt kê
Services/          Tìm kiếm dùng chung, đa ngôn ngữ, session/cookie
Resources/         Tài nguyên ngôn ngữ (vi, en)
wwwroot/           CSS, JavaScript, ảnh bìa
Database/          Script tạo database và gắn ảnh bìa
docs/              Báo cáo, hướng dẫn cài đặt, test case, sơ đồ thiết kế
```

## Tài liệu
- `docs/Bao-cao-BTL-Quan-ly-thu-vien.pdf`: báo cáo bài tập lớn
- `docs/Test-case-thu-vien.xlsx`: 115 test case và kết quả
- `docs/so-do-thiet-ke/`: use-case, sitemap, wireframe, ERD (kèm file draw.io)
