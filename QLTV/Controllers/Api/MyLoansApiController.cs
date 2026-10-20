using System.Security.Claims;
using LibraryManagement.Areas.Admin.Services;
using LibraryManagement.Data;
using LibraryManagement.Models;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers.Api;

// GET  /api/me/loans                    : sách đang mượn của chính mình
// POST /api/me/loans/{itemId}/renew     : gia hạn một cuốn
// Cần đăng nhập với role User. Dữ liệu luôn lấy theo tài khoản đang đăng nhập, không nhận mã khách từ client.
[ApiController]
[Route("api/me/loans")]
[Authorize(Roles = AppRoles.User)]
[Produces("application/json")]
public class MyLoansApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    private readonly Loc _t;

    public MyLoansApiController(ApplicationDbContext db, Loc t)
    {
        _db = db;
        _t = t;
    }

    private async Task<Member?> CurrentMemberAsync()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return null;
        return await _db.Members.AsNoTracking().FirstOrDefaultAsync(m => m.UserId == userId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<LoanItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<LoanItemDto>>> Get()
    {
        var member = await CurrentMemberAsync();
        if (member == null) return Problem(title: "Chưa có hồ sơ thành viên", statusCode: StatusCodes.Status403Forbidden);

        var today = DateTime.Today;
        var elig = await LoanRules.CheckAsync(_db, member.Id, today);
        var max = elig.Card?.Plan?.MaxRenewals ?? 0;

        var raw = await _db.LoanItems.AsNoTracking()
            .Where(i => i.Loan!.MemberId == member.Id && i.ReturnedAt == null)
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.Id,
                Title = i.BookCopy!.Book!.Title,
                Author = i.BookCopy!.Book!.Author!.Name,
                Barcode = i.BookCopy!.Barcode,
                LoanDate = i.Loan!.LoanDate,
                i.DueDate,
                i.RenewCount
            })
            .ToListAsync();

        return Ok(raw.Select(r => new LoanItemDto
        {
            ItemId = r.Id,
            Title = r.Title,
            Author = r.Author,
            Barcode = r.Barcode,
            LoanDate = DateOnly.FromDateTime(r.LoanDate),
            DueDate = DateOnly.FromDateTime(r.DueDate),
            DaysLeft = (r.DueDate.Date - today).Days,
            RenewCount = r.RenewCount,
            CanRenew = elig.Card != null && r.DueDate.Date >= today && r.RenewCount < max
        }).ToList());
    }

    [HttpPost("{itemId:int}/renew")]
    [ValidateAntiForgeryToken]   // AJAX gửi token trong header RequestVerificationToken (chống CSRF)
    [ProducesResponseType(typeof(RenewResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RenewResultDto>> Renew(int itemId)
    {
        var member = await CurrentMemberAsync();
        if (member == null) return Problem(title: "Chưa có hồ sơ thành viên", statusCode: StatusCodes.Status403Forbidden);

        // Chỉ gia hạn được sách của chính mình, sách của người khác trả 404 (không tiết lộ là có tồn tại)
        var item = await _db.LoanItems
            .Include(i => i.Loan)
            .Include(i => i.BookCopy).ThenInclude(c => c!.Book)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Loan!.MemberId == member.Id);
        if (item == null) return NotFound();

        var today = DateTime.Today;
        var decision = await LoanRules.CheckRenewAsync(_db, item, today);
        if (decision.Error != null)
            return Problem(title: _t["loans.err.title"], detail: RenewMessages.Localize(_t, decision), statusCode: StatusCodes.Status409Conflict);

        item.DueDate = item.DueDate.Date.AddDays(decision.AddDays);
        item.RenewCount++;
        await _db.SaveChangesAsync();

        var elig = await LoanRules.CheckAsync(_db, member.Id, today);
        return Ok(new RenewResultDto
        {
            ItemId = item.Id,
            DueDate = DateOnly.FromDateTime(item.DueDate),
            RenewCount = item.RenewCount,
            MaxRenewals = elig.Card?.Plan?.MaxRenewals ?? item.RenewCount,
            Message = _t["loans.renewed.ok", item.BookCopy?.Book?.Title ?? "", decision.AddDays, item.DueDate.ToString("d")]
        });
    }
}
