using LibraryManagement.Data;
using LibraryManagement.Models.Enums;
using LibraryManagement.Services;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers.Api;

// GET /api/books            : danh sách có lọc, sắp xếp, phân trang
// GET /api/books/{id}       : chi tiết một đầu sách
[ApiController]
[Route("api/books")]
[Produces("application/json")]
public class BooksApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public BooksApiController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResult<BookDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedApiResult<BookDto>>> Get(
        [FromQuery] string? q,
        [FromQuery] int? categoryId,
        [FromQuery] bool availableOnly = false,
        [FromQuery] string? sort = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);   // chặn việc kéo cả kho sách trong một lần gọi

        var query = _db.Books.AsNoTracking().InStock().Search(q, categoryId, availableOnly, sort);

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);

        var cards = await query.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(BookQueries.ToCard).ToListAsync();

        return Ok(new PagedApiResult<BookDto>
        {
            Items = cards.Select(BookDto.From).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total,
            TotalPages = totalPages
        });
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BookDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookDetailDto>> GetById(int id)
    {
        var book = await _db.Books.AsNoTracking()
            .Include(b => b.Author).Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id && b.IsActive);
        if (book == null) return NotFound();

        var copies = await _db.BookCopies.AsNoTracking().Where(c => c.BookId == id)
            .Select(c => new { c.Status, c.ReadOnSiteOnly }).ToListAsync();

        return Ok(new BookDetailDto
        {
            Id = book.Id,
            Title = book.Title,
            Author = book.Author!.Name,
            Category = book.Category!.Name,
            CoverImagePath = book.CoverImagePath,
            RentalFee = book.RentalFee,
            Borrowable = copies.Count(c => c.Status == CopyStatus.Available && !c.ReadOnSiteOnly),
            ReadOnSite = copies.Count(c => c.Status == CopyStatus.Available && c.ReadOnSiteOnly),
            TotalCopies = copies.Count(c => c.Status != CopyStatus.Disposed),
            Url = $"/Catalog/Details/{book.Id}",
            Isbn = book.ISBN,
            Publisher = book.Publisher,
            PublishYear = book.PublishYear,
            PageCount = book.PageCount,
            Description = book.Description
        });
    }
}
