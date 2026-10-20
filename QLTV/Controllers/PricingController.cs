using LibraryManagement.Data;
using LibraryManagement.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

public class PricingController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public PricingController(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        return View(new PricingVm
        {
            Plans = await _db.MembershipPlans.AsNoTracking()
                .Where(p => p.IsActive).OrderBy(p => p.Price).ToListAsync(),
            LateFeePerDay = _config.GetValue<decimal?>("Library:LateFeePerDay") ?? 2000m
        });
    }
}
