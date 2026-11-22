using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BookWeb.Data;

namespace BookWeb.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly AppDbContext _context;
        public OrdersController(AppDbContext context)
        {
            _context = context;
        }
        private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Where(o => o.UserId  == CurrentUserId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);
            if(order == null)
            {
                return NotFound();
            }

            if(order.UserId !=  CurrentUserId || !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            return View(order);
        }
    }
}
