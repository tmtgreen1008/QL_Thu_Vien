/* =====================================================================
   HE THONG QUAN LY THU VIEN - SCRIPT TAO DATABASE (SQL Server)
   Chay trong SSMS: File > Open > File..., chon file nay, bam Execute (F5).

   - Tao database CSDL_QL_TV neu chua co.
   - Tao 7 bang Identity (dung cau truc cua ASP.NET Core Identity).
   - Tao 10 bang nghiep vu khop 100% voi cac class trong thu muc Models.
   - Neu database da co bang thi script tu dung lai, KHONG xoa du lieu.
   - Script chi tao cau truc. Role, tai khoan, sach mau do ung dung tu them
     khi chay lan dau (DbSeeder), vi mat khau phai duoc ma hoa boi ASP.NET.
   ===================================================================== */

USE master;
GO
IF DB_ID(N'CSDL_QL_TV') IS NULL
    CREATE DATABASE [CSDL_QL_TV];
GO
USE [CSDL_QL_TV];
GO

-- Bao ve du lieu: neu da co bang thi bo qua toan bo phan tao bang phia duoi
IF OBJECT_ID(N'dbo.Books', N'U') IS NOT NULL OR OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NOT NULL
BEGIN
    PRINT N'>>> Database da co bang, bo qua viec tao. Muon tao lai tu dau thi xoa database truoc.';
    SET NOEXEC ON;
END
GO

/* ===================== 1. IDENTITY (tai khoan, vai tro) ===================== */

CREATE TABLE dbo.AspNetRoles (
    Id                nvarchar(450) NOT NULL,
    Name              nvarchar(256) NULL,
    NormalizedName    nvarchar(256) NULL,
    ConcurrencyStamp  nvarchar(max) NULL,
    CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;

-- ApplicationUser = IdentityUser + FullName, IsActive, CreatedAt
CREATE TABLE dbo.AspNetUsers (
    Id                    nvarchar(450)  NOT NULL,
    FullName              nvarchar(100)  NOT NULL,
    IsActive              bit            NOT NULL,
    CreatedAt             datetime2      NOT NULL,
    UserName              nvarchar(256)  NULL,
    NormalizedUserName    nvarchar(256)  NULL,
    Email                 nvarchar(256)  NULL,
    NormalizedEmail       nvarchar(256)  NULL,
    EmailConfirmed        bit            NOT NULL,
    PasswordHash          nvarchar(max)  NULL,
    SecurityStamp         nvarchar(max)  NULL,
    ConcurrencyStamp      nvarchar(max)  NULL,
    PhoneNumber           nvarchar(max)  NULL,
    PhoneNumberConfirmed  bit            NOT NULL,
    TwoFactorEnabled      bit            NOT NULL,
    LockoutEnd            datetimeoffset NULL,
    LockoutEnabled        bit            NOT NULL,
    AccessFailedCount     int            NOT NULL,
    CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
);
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;

CREATE TABLE dbo.AspNetRoleClaims (
    Id          int IDENTITY(1,1) NOT NULL,
    RoleId      nvarchar(450) NOT NULL,
    ClaimType   nvarchar(max) NULL,
    ClaimValue  nvarchar(max) NULL,
    CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId)
        REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims (RoleId);

CREATE TABLE dbo.AspNetUserClaims (
    Id          int IDENTITY(1,1) NOT NULL,
    UserId      nvarchar(450) NOT NULL,
    ClaimType   nvarchar(max) NULL,
    ClaimValue  nvarchar(max) NULL,
    CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims (UserId);

CREATE TABLE dbo.AspNetUserLogins (
    LoginProvider        nvarchar(128) NOT NULL,
    ProviderKey          nvarchar(128) NOT NULL,
    ProviderDisplayName  nvarchar(max) NULL,
    UserId               nvarchar(450) NOT NULL,
    CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins (UserId);

CREATE TABLE dbo.AspNetUserRoles (
    UserId  nvarchar(450) NOT NULL,
    RoleId  nvarchar(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId)
        REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
);
CREATE INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles (RoleId);

CREATE TABLE dbo.AspNetUserTokens (
    UserId         nvarchar(450) NOT NULL,
    LoginProvider  nvarchar(128) NOT NULL,
    Name           nvarchar(128) NOT NULL,
    Value          nvarchar(max) NULL,
    CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE
);
GO

/* ===================== 2. SACH ===================== */

CREATE TABLE dbo.Authors (
    Id    int IDENTITY(1,1) NOT NULL,
    Name  nvarchar(150) NOT NULL,
    Bio   nvarchar(max) NULL,
    CONSTRAINT PK_Authors PRIMARY KEY (Id)
);

CREATE TABLE dbo.Categories (
    Id           int IDENTITY(1,1) NOT NULL,
    Name         nvarchar(100) NOT NULL,
    Description  nvarchar(500) NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (Id)
);
CREATE UNIQUE INDEX IX_Categories_Name ON dbo.Categories (Name);

-- Dau sach (thong tin chung)
CREATE TABLE dbo.Books (
    Id                int IDENTITY(1,1) NOT NULL,
    Title             nvarchar(250) NOT NULL,
    ISBN              nvarchar(20)  NOT NULL,
    Description       nvarchar(max) NULL,
    Publisher         nvarchar(150) NULL,
    PublishYear       int           NULL,
    PageCount         int           NULL,
    CoverImagePath    nvarchar(300) NULL,
    RentalFee         decimal(18,0) NOT NULL,
    ReplacementPrice  decimal(18,0) NOT NULL,
    AuthorId          int           NOT NULL,
    CategoryId        int           NOT NULL,
    IsActive          bit           NOT NULL,
    CONSTRAINT PK_Books PRIMARY KEY (Id),
    CONSTRAINT FK_Books_Authors_AuthorId FOREIGN KEY (AuthorId) REFERENCES dbo.Authors (Id),
    CONSTRAINT FK_Books_Categories_CategoryId FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id)
);
CREATE UNIQUE INDEX IX_Books_ISBN ON dbo.Books (ISBN);
CREATE INDEX IX_Books_Title ON dbo.Books (Title);
CREATE INDEX IX_Books_AuthorId ON dbo.Books (AuthorId);
CREATE INDEX IX_Books_CategoryId ON dbo.Books (CategoryId);

-- Ban sao vat ly. Status: 0 San sang, 1 Dang muon, 2 Mat, 3 Hong, 4 Thanh ly
-- Condition: 0 Moi, 1 Tot, 2 Cu
CREATE TABLE dbo.BookCopies (
    Id              int IDENTITY(1,1) NOT NULL,
    BookId          int           NOT NULL,
    Barcode         nvarchar(30)  NOT NULL,
    Status          int           NOT NULL,
    Condition       int           NOT NULL,
    ShelfLocation   nvarchar(50)  NULL,
    ReadOnSiteOnly  bit           NOT NULL,
    PurchasePrice   decimal(18,0) NOT NULL,
    AcquiredDate    datetime2     NOT NULL,
    RowVersion      rowversion    NOT NULL,
    CONSTRAINT PK_BookCopies PRIMARY KEY (Id),
    CONSTRAINT FK_BookCopies_Books_BookId FOREIGN KEY (BookId) REFERENCES dbo.Books (Id)
);
CREATE UNIQUE INDEX IX_BookCopies_Barcode ON dbo.BookCopies (Barcode);
CREATE INDEX IX_BookCopies_BookId ON dbo.BookCopies (BookId);
GO

/* ===================== 3. GOI VA THANH VIEN ===================== */

CREATE TABLE dbo.MembershipPlans (
    Id              int IDENTITY(1,1) NOT NULL,
    Name            nvarchar(100) NOT NULL,
    Price           decimal(18,0) NOT NULL,
    DurationMonths  int           NOT NULL,
    DepositAmount   decimal(18,0) NOT NULL,
    MaxBooks        int           NOT NULL,
    LoanDays        int           NOT NULL,
    MaxRenewals     int           NOT NULL,
    IsActive        bit           NOT NULL,
    CONSTRAINT PK_MembershipPlans PRIMARY KEY (Id)
);

-- Ho so khach. UserId = NULL khi khach moi den quay, chua co tai khoan web
CREATE TABLE dbo.Members (
    Id           int IDENTITY(1,1) NOT NULL,
    UserId       nvarchar(450) NULL,
    MemberCode   nvarchar(20)  NOT NULL,
    FullName     nvarchar(100) NOT NULL,
    Phone        nvarchar(15)  NOT NULL,
    Email        nvarchar(100) NULL,
    Address      nvarchar(250) NULL,
    DateOfBirth  datetime2     NULL,
    IsLocked     bit           NOT NULL,
    LockReason   nvarchar(250) NULL,
    CreatedAt    datetime2     NOT NULL,
    CONSTRAINT PK_Members PRIMARY KEY (Id),
    CONSTRAINT FK_Members_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id)
);
CREATE UNIQUE INDEX IX_Members_MemberCode ON dbo.Members (MemberCode);
CREATE INDEX IX_Members_Phone ON dbo.Members (Phone);
CREATE UNIQUE INDEX IX_Members_UserId ON dbo.Members (UserId) WHERE UserId IS NOT NULL;

-- The thanh vien. DepositStatus: 0 Dang giu, 1 Da hoan, 2 Mat coc
-- Status: 0 Con han, 1 Het han, 2 Da huy
CREATE TABLE dbo.Memberships (
    Id             int IDENTITY(1,1) NOT NULL,
    MemberId       int           NOT NULL,
    PlanId         int           NOT NULL,
    StartDate      datetime2     NOT NULL,
    EndDate        datetime2     NOT NULL,
    PricePaid      decimal(18,0) NOT NULL,
    DepositAmount  decimal(18,0) NOT NULL,
    DepositStatus  int           NOT NULL,
    Status         int           NOT NULL,
    CONSTRAINT PK_Memberships PRIMARY KEY (Id),
    CONSTRAINT FK_Memberships_Members_MemberId FOREIGN KEY (MemberId) REFERENCES dbo.Members (Id),
    CONSTRAINT FK_Memberships_MembershipPlans_PlanId FOREIGN KEY (PlanId) REFERENCES dbo.MembershipPlans (Id)
);
CREATE INDEX IX_Memberships_MemberId ON dbo.Memberships (MemberId);
CREATE INDEX IX_Memberships_PlanId ON dbo.Memberships (PlanId);
GO

/* ===================== 4. MUON TRA ===================== */

-- Phieu muon. AdminId = Admin lap phieu
CREATE TABLE dbo.Loans (
    Id            int IDENTITY(1,1) NOT NULL,
    MemberId      int           NOT NULL,
    MembershipId  int           NOT NULL,
    AdminId       nvarchar(450) NOT NULL,
    LoanDate      datetime2     NOT NULL,
    Note          nvarchar(250) NULL,
    CONSTRAINT PK_Loans PRIMARY KEY (Id),
    CONSTRAINT FK_Loans_Members_MemberId FOREIGN KEY (MemberId) REFERENCES dbo.Members (Id),
    CONSTRAINT FK_Loans_Memberships_MembershipId FOREIGN KEY (MembershipId) REFERENCES dbo.Memberships (Id),
    CONSTRAINT FK_Loans_AspNetUsers_AdminId FOREIGN KEY (AdminId) REFERENCES dbo.AspNetUsers (Id)
);
CREATE INDEX IX_Loans_MemberId ON dbo.Loans (MemberId);
CREATE INDEX IX_Loans_MembershipId ON dbo.Loans (MembershipId);
CREATE INDEX IX_Loans_AdminId ON dbo.Loans (AdminId);

-- Tung cuon trong phieu. ReturnedAt = NULL nghia la chua tra.
-- Status: 0 Dang muon, 1 Da tra, 2 Mat. ConditionOnReturn: 0 Moi, 1 Tot, 2 Cu
CREATE TABLE dbo.LoanItems (
    Id                 int IDENTITY(1,1) NOT NULL,
    LoanId             int           NOT NULL,
    BookCopyId         int           NOT NULL,
    DueDate            datetime2     NOT NULL,
    RenewCount         int           NOT NULL,
    RentalFeeCharged   decimal(18,0) NOT NULL,
    ReturnedAt         datetime2     NULL,
    ConditionOnReturn  int           NULL,
    Status             int           NOT NULL,
    CONSTRAINT PK_LoanItems PRIMARY KEY (Id),
    CONSTRAINT FK_LoanItems_Loans_LoanId FOREIGN KEY (LoanId) REFERENCES dbo.Loans (Id),
    CONSTRAINT FK_LoanItems_BookCopies_BookCopyId FOREIGN KEY (BookCopyId) REFERENCES dbo.BookCopies (Id)
);
CREATE INDEX IX_LoanItems_LoanId ON dbo.LoanItems (LoanId);
-- Mot ban sach chi duoc co toi da 1 dong muon chua tra tai mot thoi diem
CREATE UNIQUE INDEX UX_LoanItems_ActiveCopy ON dbo.LoanItems (BookCopyId) WHERE ReturnedAt IS NULL;
GO

/* ===================== 5. TIEN ===================== */

-- So giao dich. Khong xoa dong, chi huy (Status = 2). Hoan coc la so tien am.
-- Type: 0 Phi the, 1 Coc, 2 Hoan coc, 3 Phi thue, 4 Phat tre, 5 Den sach
-- Method: 0 Tien mat, 1 Chuyen khoan
-- Status: 0 Da thu, 1 Con no, 2 Da huy
CREATE TABLE dbo.Payments (
    Id             int IDENTITY(1,1) NOT NULL,
    ReceiptNo      nvarchar(20)  NOT NULL,
    Type           int           NOT NULL,
    Amount         decimal(18,0) NOT NULL,
    Method         int           NOT NULL,
    Status         int           NOT NULL,
    MemberId       int           NULL,
    MembershipId   int           NULL,
    LoanItemId     int           NULL,
    ReceivedById   nvarchar(450) NOT NULL,
    CreatedAt      datetime2     NOT NULL,
    PaidAt         datetime2     NULL,
    Note           nvarchar(250) NULL,
    VoidReason     nvarchar(250) NULL,
    CONSTRAINT PK_Payments PRIMARY KEY (Id),
    CONSTRAINT FK_Payments_Members_MemberId FOREIGN KEY (MemberId) REFERENCES dbo.Members (Id),
    CONSTRAINT FK_Payments_Memberships_MembershipId FOREIGN KEY (MembershipId) REFERENCES dbo.Memberships (Id),
    CONSTRAINT FK_Payments_LoanItems_LoanItemId FOREIGN KEY (LoanItemId) REFERENCES dbo.LoanItems (Id),
    CONSTRAINT FK_Payments_AspNetUsers_ReceivedById FOREIGN KEY (ReceivedById) REFERENCES dbo.AspNetUsers (Id)
);
CREATE UNIQUE INDEX IX_Payments_ReceiptNo ON dbo.Payments (ReceiptNo);
CREATE INDEX IX_Payments_MemberId ON dbo.Payments (MemberId);
CREATE INDEX IX_Payments_MembershipId ON dbo.Payments (MembershipId);
CREATE INDEX IX_Payments_LoanItemId ON dbo.Payments (LoanItemId);
CREATE INDEX IX_Payments_ReceivedById ON dbo.Payments (ReceivedById);
GO

SET NOEXEC OFF;
GO

/* ===================== KIEM TRA ===================== */
SELECT name AS TenBang FROM sys.tables ORDER BY name;   -- phai co 17 bang
GO
