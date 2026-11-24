using BookWeb.Data;
using BookWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace BookWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        public HomeController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(string? search, int? categoryId, int page = 1)
        {            
            var query = _context.Books.Include(b => b.Category).AsQueryable();
            if(!string.IsNullOrEmpty(search))
            {
                query = query.Where(b => b.Title.Contains(search) || b.Author.Contains(search));
            }
            if(categoryId.HasValue)
            {
                query = query.Where(b => b.CategoryId == categoryId);
            }

            query = query.OrderByDescending(b => b.Id);

            var result = await PagedResult<Book>.CreateAsync(query, page, 8);

            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            ViewBag.Related = await _context.Books
                .Where(b => b.CategoryId == book.CategoryId && b.Id != book.Id)
                .OrderByDescending(b => b.Id)
                .Take(4)
                .ToListAsync();

            return View(book);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
