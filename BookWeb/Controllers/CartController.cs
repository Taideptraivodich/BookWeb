using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookWeb.Data;
using BookWeb.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BookWeb.Controllers
{
    public class CartController : Controller
    {
        private const string CartKey = "Cart";
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var json = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json))
            {
                return new List<CartItem>();
            }

            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
            HttpContext.Session.SetInt32("CartCount", cart.Sum(c => c.Quantity));
        }

        public async Task<IActionResult> Index()
        {
            var vm = new CartViewModel { Lines = await BuildLinesAsync(GetCart()) };
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int bookId, int quantity = 1)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null)
            {
                return NotFound();
            }
            if (book.Stock <= 0)
            {
                return RedirectToAction("Details", "Home", new { id = bookId });
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.BookId == bookId);
            if (item == null)
            {
                item = new CartItem { BookId = bookId, Quantity = 0 };
                cart.Add(item);
            }

            item.Quantity = Math.Min(item.Quantity + Math.Max(quantity, 1), book.Stock);

            SaveCart(cart);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Update(int bookId, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(c => c.BookId == bookId);
            if(item != null)
            {
                var book = await _context.Books.FindAsync(bookId);
                var newQuantity = book == null ? 0 : Math.Min(quantity, book.Stock);
                if(newQuantity <= 0)
                {
                    cart.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                SaveCart(cart);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int bookId)
        {
            var cart = GetCart();
            cart.RemoveAll(c => c.BookId == bookId);
            return RedirectToAction(nameof(Index));
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var lines = await BuildLinesAsync(GetCart());
            if(!lines.Any())
            {
                return RedirectToAction(nameof(Index));
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = await _context.Users.FindAsync(userId);

            var vm = new CheckoutViewModel()
            {
                CustomerName = user?.FullName ?? string.Empty,
                Lines = lines
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Checkout(CheckoutViewModel model)
        {
            var lines = await BuildLinesAsync(GetCart());
            if(!lines.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            if(!ModelState.IsValid)
            {
                model.Lines = lines;
                return View(model);
            }
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            using var transaction = await _context.Database.BeginTransactionAsync();
            var order = new Order
            {
                UserId = userId,
                CustomerName = model.CustomerName,
                Phone = model.Phone.Trim(),
                Address = model.Address.Trim(),
                Note = model.Note?.Trim(),
                Status = OrderStatus.Pending,
                Total = lines.Sum(l => l.SubTotal),
            };
            foreach (var line in lines)
            {
                var affected = await _context.Books
                    .Where(b => b.Id == line.Book.Id && b.Stock >= line.Quantity)
                    .ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, b => b.Stock - line.Quantity));

                if(affected == 0)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", $"Sách \"{line.Book.Title}\" Không đủ hàng. Vui lòng kiểm tra lại giỏ.");
                    model.Lines = lines;
                    return View(model);
                }
                order.Details.Add(new OrderDetail
                {
                    BookId = line.Book.Id,
                    BookTitle = line.Book.Title,
                    Quantity = line.Quantity,
                    UnitPrice = line.Book.Price
                });
            }
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            SaveCart(new List<CartItem>());
            TempData["Success"] = "Đặt hàng thành công!";
            return RedirectToAction("Details", "Order", new{id = order.Id});
        }


        private async Task<List<CartLine>> BuildLinesAsync(List<CartItem> cart)
        {

            var ids = cart.Select(i => i.BookId).ToList();

            var books = await _context.Books.Where(b => ids.Contains(b.Id)).ToListAsync();

            var lines = new List<CartLine>();
            foreach (var item in cart)
            {
                var book = books.FirstOrDefault(b => b.Id == item.BookId);
                if (book != null)
                {
                    lines.Add(new CartLine { Book = book, Quantity = item.Quantity });
                }
            }
            return lines;
        }
    }
}
    
