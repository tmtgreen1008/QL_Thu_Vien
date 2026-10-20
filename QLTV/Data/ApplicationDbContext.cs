using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookCopy> BookCopies => Set<BookCopy>();
    public DbSet<MembershipPlan> MembershipPlans => Set<MembershipPlan>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<LoanItem> LoanItems => Set<LoanItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Mọi cột decimal là tiền VND: decimal(18,0), không có số lẻ
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 0);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>().HasIndex(c => c.Name).IsUnique();

        builder.Entity<Book>(e =>
        {
            e.HasIndex(b => b.ISBN).IsUnique();
            e.HasIndex(b => b.Title);
        });

        builder.Entity<BookCopy>().HasIndex(c => c.Barcode).IsUnique();

        builder.Entity<Member>(e =>
        {
            e.HasIndex(m => m.MemberCode).IsUnique();
            e.HasIndex(m => m.Phone);
            // Mỗi tài khoản chỉ gắn với một hồ sơ; cho phép nhiều dòng UserId = null
            e.HasIndex(m => m.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            e.HasOne(m => m.User).WithOne(u => u.Member).HasForeignKey<Member>(m => m.UserId);
        });

        builder.Entity<Loan>()
            .HasOne(l => l.Admin).WithMany().HasForeignKey(l => l.AdminId);

        // Một bản sách chỉ có tối đa một dòng mượn chưa trả tại một thời điểm
        builder.Entity<LoanItem>()
            .HasIndex(li => li.BookCopyId)
            .IsUnique()
            .HasFilter("[ReturnedAt] IS NULL")
            .HasDatabaseName("UX_LoanItems_ActiveCopy");

        builder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.ReceiptNo).IsUnique();
            e.HasOne(p => p.ReceivedBy).WithMany().HasForeignKey(p => p.ReceivedById);
        });

        // Không xóa cứng dữ liệu: mọi khóa ngoại của các bảng nghiệp vụ dùng Restrict
        var foreignKeys = builder.Model.GetEntityTypes()
            .Where(t => t.ClrType.Namespace == typeof(Book).Namespace)
            .SelectMany(t => t.GetForeignKeys());
        foreach (var fk in foreignKeys)
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }
}
