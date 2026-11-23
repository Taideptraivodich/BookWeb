using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookWeb.Data;
using BookWeb.Models;

namespace BookWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly AppDbContext _context;
        public AdminOrdersController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? status, string? search)
        {
            var query = _context.Orders.Include(o => o.User).AsQueryable();
            if(!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            if(!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                if(int.TryParse(keyword, out var orderId))
                {
                    query = query.Where(o => o.Id == orderId
                    || o.CustomerName.Contains(keyword)
                    || o.Phone.Contains(keyword));
                }
                else
                {
                    query = query.Where(o => o.CustomerName.Contains(keyword) 
                        || o.Phone.Contains(keyword));
                }
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();

            ViewBag.Status = status;
            ViewBag.Search = search;
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if(order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!OrderStatus.NextStatuses(order.Status).Contains(newStatus))
            {
                TempData["Error"] = "Không thể chuyển đơn sang trạng thái này.";
                return RedirectToAction(nameof(Details), new { id });
            }
            var currentStatus = order.Status;

            using var transaction = await _context.Database.BeginTransactionAsync();

            var changed = await _context.Orders
                .Where(o => o.Id == id && o.Status == currentStatus)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, newStatus));

            if(changed == 0)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Đơn hàng vừa được người khác thay đổi, Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if(newStatus == OrderStatus.Cancelled)
            {
                foreach(var d in order.Details)
                {
                    await _context.Books
                        .Where(b => b.Id == d.Book.Id)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, b => b.Stock + d.Quantity));
                }
            }
            await transaction.CommitAsync();

            TempData["Success"] = $"Đã chuyển đơn #{id} sang \"{newStatus}\".";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
