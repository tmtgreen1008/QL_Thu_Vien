using LibraryManagement.Data;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers.Api;

// GET /api/categories : danh sách thể loại kèm số đầu sách
[ApiController]
[Route("api/categories")]
[Produces("application/json")]
public class CategoriesApiController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public CategoriesApiController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> Get()
    {
        var list = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Titles = c.Books.Count(b => b.IsActive) })
            .ToListAsync();
        return Ok(list.Where(c => c.Titles > 0).ToList());
    }
}
