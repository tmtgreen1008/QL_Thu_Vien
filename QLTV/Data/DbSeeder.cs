using LibraryManagement.Models;
using LibraryManagement.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public static class DbSeeder
{
    public const string AdminEmail = "admin@library.local";
    public const string AdminPassword = "Admin@123";
    public const string DemoUserEmail = "khach@library.local";
    public const string DemoUserPassword = "Khach@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // Database và bảng đã được tạo bằng script 01_TaoDatabase.sql
        if (!await db.Database.CanConnectAsync())
            throw new InvalidOperationException(
                "Không kết nối được database. Kiểm tra chuỗi kết nối trong appsettings.json và đã chạy script SQL chưa.");

        foreach (var role in new[] { AppRoles.Admin, AppRoles.User })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var admin = await EnsureUserAsync(userManager, AdminEmail, AdminPassword, "Quản trị viên", AppRoles.Admin);
        var demoUser = await EnsureUserAsync(userManager, DemoUserEmail, DemoUserPassword, "Nguyễn Văn A", AppRoles.User);

        if (!await db.MembershipPlans.AnyAsync())
            await SeedPlansAsync(db);

        if (!await db.Books.AnyAsync())
            await SeedBooksAsync(db);

        if (!await db.Members.AnyAsync())
            await SeedDemoMemberAsync(db, admin, demoUser);
    }

    private static async Task<ApplicationUser> EnsureUserAsync(
        UserManager<ApplicationUser> userManager, string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user != null) return user;

        user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Không tạo được tài khoản mẫu {email}: {errors}");
        }

        await userManager.AddToRoleAsync(user, role);
        return user;
    }

    private static async Task SeedPlansAsync(ApplicationDbContext db)
    {
        db.MembershipPlans.AddRange(
            new MembershipPlan { Name = "Thường - 1 tháng", Price = 50000, DurationMonths = 1, DepositAmount = 100000, MaxBooks = 3, LoanDays = 14, MaxRenewals = 1 },
            new MembershipPlan { Name = "Thường - 6 tháng", Price = 250000, DurationMonths = 6, DepositAmount = 100000, MaxBooks = 3, LoanDays = 14, MaxRenewals = 1 },
            new MembershipPlan { Name = "VIP - 1 năm", Price = 400000, DurationMonths = 12, DepositAmount = 200000, MaxBooks = 8, LoanDays = 30, MaxRenewals = 2 });
        await db.SaveChangesAsync();
    }

    private static async Task SeedBooksAsync(ApplicationDbContext db)
    {
        var categories = new[]
        {
            ("VN", "Văn học Việt Nam"),
            ("NN", "Văn học nước ngoài"),
            ("TN", "Thiếu nhi"),
            ("KN", "Kỹ năng sống"),
            ("KH", "Lịch sử - Khoa học"),
            ("IT", "Công nghệ thông tin")
        }.ToDictionary(c => c.Item1, c => new Category { Name = c.Item2 });

        var authors = new[]
        {
            "Nguyễn Nhật Ánh", "Tô Hoài", "Nam Cao", "Nguyễn Ngọc Tư", "Paulo Coelho",
            "Dale Carnegie", "Yuval Noah Harari", "J.K. Rowling", "Robert C. Martin",
            "Haruki Murakami", "Antoine de Saint-Exupéry"
        }.ToDictionary(n => n, n => new Author { Name = n });

        // (Tên sách, Tác giả, Mã thể loại, Năm, Phí thuê, Giá đền, Số bản sao)
        var data = new (string Title, string Author, string Cat, int Year, int Rental, int Price, int Copies)[]
        {
            ("Cho tôi xin một vé đi tuổi thơ", "Nguyễn Nhật Ánh", "VN", 2008, 5000, 80000, 3),
            ("Mắt biếc", "Nguyễn Nhật Ánh", "VN", 1990, 5000, 90000, 3),
            ("Tôi thấy hoa vàng trên cỏ xanh", "Nguyễn Nhật Ánh", "VN", 2010, 5000, 95000, 3),
            ("Dế Mèn phiêu lưu ký", "Tô Hoài", "TN", 1941, 0, 60000, 2),
            ("Chí Phèo", "Nam Cao", "VN", 1941, 0, 50000, 2),
            ("Lão Hạc", "Nam Cao", "VN", 1943, 0, 50000, 2),
            ("Cánh đồng bất tận", "Nguyễn Ngọc Tư", "VN", 2005, 0, 75000, 2),
            ("Nhà giả kim", "Paulo Coelho", "NN", 1988, 5000, 79000, 3),
            ("Đắc nhân tâm", "Dale Carnegie", "KN", 1936, 5000, 86000, 4),
            ("Quẳng gánh lo đi và vui sống", "Dale Carnegie", "KN", 1948, 0, 80000, 2),
            ("Sapiens: Lược sử loài người", "Yuval Noah Harari", "KH", 2011, 8000, 189000, 3),
            ("Homo Deus", "Yuval Noah Harari", "KH", 2015, 8000, 199000, 2),
            ("21 bài học cho thế kỷ 21", "Yuval Noah Harari", "KH", 2018, 8000, 185000, 2),
            ("Harry Potter và Hòn đá phù thủy", "J.K. Rowling", "TN", 1997, 5000, 120000, 3),
            ("Harry Potter và Phòng chứa bí mật", "J.K. Rowling", "TN", 1998, 5000, 125000, 3),
            ("Clean Code", "Robert C. Martin", "IT", 2008, 10000, 350000, 2),
            ("Clean Architecture", "Robert C. Martin", "IT", 2017, 10000, 380000, 2),
            ("The Clean Coder", "Robert C. Martin", "IT", 2011, 10000, 320000, 1),
            ("Rừng Na Uy", "Haruki Murakami", "NN", 1987, 5000, 110000, 2),
            ("Kafka bên bờ biển", "Haruki Murakami", "NN", 2002, 5000, 135000, 2),
            ("Hoàng tử bé", "Antoine de Saint-Exupéry", "TN", 1943, 0, 55000, 3)
        };

        int bookNo = 0;
        foreach (var d in data)
        {
            bookNo++;
            var book = new Book
            {
                Title = d.Title,
                ISBN = $"97860400{bookNo:D5}",   // ISBN mẫu, không phải ISBN thật
                Author = authors[d.Author],
                Category = categories[d.Cat],
                PublishYear = d.Year,
                RentalFee = d.Rental,
                ReplacementPrice = d.Price,
                Description = $"Dữ liệu mẫu: {d.Title}."
            };

            for (int copy = 1; copy <= d.Copies; copy++)
            {
                book.Copies.Add(new BookCopy
                {
                    Barcode = $"BC{bookNo:D4}{copy:D2}",
                    ShelfLocation = $"Kệ {d.Cat}-{(bookNo % 4) + 1}",
                    PurchasePrice = d.Price * 8 / 10,
                    AcquiredDate = DateTime.Today.AddMonths(-3),
                    // Một bản của Clean Code là sách tham khảo, chỉ đọc tại chỗ
                    ReadOnSiteOnly = d.Title == "Clean Code" && copy == d.Copies
                });
            }

            db.Books.Add(book);
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedDemoMemberAsync(ApplicationDbContext db, ApplicationUser admin, ApplicationUser demoUser)
    {
        var plan = await db.MembershipPlans.OrderBy(p => p.Id).FirstAsync();
        var now = DateTime.Now;

        var member = new Member
        {
            UserId = demoUser.Id,
            MemberCode = "TV000001",
            FullName = demoUser.FullName,
            Phone = "0901234567",
            Email = demoUser.Email,
            Address = "Hà Nội"
        };

        var membership = new Membership
        {
            Member = member,
            Plan = plan,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(plan.DurationMonths),
            PricePaid = plan.Price,
            DepositAmount = plan.DepositAmount
        };

        db.Memberships.Add(membership);
        db.Payments.AddRange(
            new Payment
            {
                ReceiptNo = "PT000001", Type = PaymentType.MembershipFee, Amount = plan.Price,
                Member = member, Membership = membership, ReceivedById = admin.Id,
                CreatedAt = now, PaidAt = now
            },
            new Payment
            {
                ReceiptNo = "PT000002", Type = PaymentType.Deposit, Amount = plan.DepositAmount,
                Member = member, Membership = membership, ReceivedById = admin.Id,
                CreatedAt = now, PaidAt = now
            });

        await db.SaveChangesAsync();
    }
}
