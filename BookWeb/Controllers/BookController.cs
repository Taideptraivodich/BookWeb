using BookWeb.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using BookWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookWeb.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BookController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public BookController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var books = _context.Books.Include(b => b.Category);
            var result = await PagedResult<Book>.CreateAsync(books, page, 10);
            return View(result);
        }

        public IActionResult Create()
        {
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(Book book, IFormFile? imageFile)
        {
            if (imageFile != null && !IsValidImage(imageFile))
            {
                ModelState.AddModelError("ImageUrl", "Chỉ nhận ảnh .jpg .png .webp và tối đa 2MB");
            }
            if (ModelState.IsValid)
            {
                book.ImageUrl = await SaveImageAsync(imageFile);
                _context.Books.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            return View(book);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            return View(book);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile? imageFile)
        {
            if (id != book.Id)
            {
                return NotFound();
            }
            if (imageFile != null && !IsValidImage(imageFile))
            {
                ModelState.AddModelError("ImageUrl", "Chỉ nhận ảnh .jpg, .png, .webp và tối đa 2MB");
            }

            if (ModelState.IsValid)
            {
                var newImage = await SaveImageAsync(imageFile);
                if (newImage != null)
                {
                    book.ImageUrl = newImage;
                }    
                _context.Update(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", book.CategoryId);
            return View(book);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var book = await _context.Books
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book != null)
            {
                _context.Books.Remove(book);
                await _context.SaveChangesAsync();
            }

            return RedirectToActionPermanent(nameof(Index));
        }
        private bool IsValidImage(IFormFile file)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowed.Contains(ext) && file.Length <= 2 * 1024 * 1024;
        }
        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var folder = Path.Combine(_env.WebRootPath, "images", "books");
            Directory.CreateDirectory(folder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = Guid.NewGuid().ToString() + ext;
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/images/books/" +  fileName;
        }
    }

}
