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

        //Bảng danh sách tất cả đơn hàng của người dùng cùng trạng thái và ô search cùng ô lọc
        public async Task<IActionResult> Index(string? status, string? search, int page = 1)
        {            
            //Nạp phương thức lấy Orders kèm User vào biến query,
            //  dùng AsQueryable để cho query có khả năng gán lại phương thức truy vấn cho biến query
            var query = _context.Orders.Include(o => o.User).AsQueryable();
            //Nếu status nhận được có tồn tại thì truy vấn lấy order có status là status nhận dc
            if(!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }
            //Nếu có tồn tại giá trị search người dùng gửi (không bao gồm chỉ mỗi dấu cách)
            if(!string.IsNullOrWhiteSpace(search))
            {
            //gán dữ liệu search người dùng truyền vào sau khi ngắt mấy cái dấu cách ở đầu và cuối vào keyword
                var keyword = search.Trim();
                //Trong giá trị người dùng search hàm này sẽ thử biên dịch nó thành kiểu int rồi gán vào 1 biến orderId
                if(int.TryParse(keyword, out var orderId))
                {
                    //Nếu biên dịch dc thì truy vấn lấy order theo id hoặc lấy theo tên khách hàng, hoặc lấy theo số điện thoại
                    query = query.Where(o => o.Id == orderId
                    || o.CustomerName.Contains(keyword)
                    || o.Phone.Contains(keyword));
                }
                else
                {
                    //Nếu giá trị search không biên dịch dc thành kiểu int
                    //như người dùng chỉ nhập 1 chuỗi nhiều số hoặc chỉ mỗi chữ thì lấy theo tên hoặc sđt khách
                    query = query.Where(o => o.CustomerName.Contains(keyword) 
                        || o.Phone.Contains(keyword));
                }
            }
            //sắp xếp lại query sau khi có order theo điều kiện lọc ở trên thì sắp xếp lại theo ngày mới nhất
            //Rồi truyền vào biến orders
            query= query.OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id);

            var result = await PagedResult<Order>.CreateAsync(query, page, 10);

            //Gửi dữ liệu status và search về lại view (mục đích là giữ lại giá trị cũ mà admin đang nhập dở
            //rồi thoát giữa chừng vào lại thì mọi status và search sẽ dc giữ nguyên giá trị đã chọn trước đó
            ViewBag.Status = status;
            ViewBag.Search = search;
            //Đổ biến orders vào View index của AdminOrders
            return View(result);
        }

        
        public async Task<IActionResult> Details(int id)
        {
            //Lấy Orders nhưng kèm cả User và Details, và chỉ lấy Order chỉ định
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);

            //nếu không lấy dc gì thì trả màn hình không tìm thấy
            if(order == null)
            {
                return NotFound();
            }
            //Đổ dữ liệu order qua view
            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string newStatus)
        {
            //lấy order gồm Details theo đúng id chỉ định
            var order = await _context.Orders
                .Include(o => o.Details)
                .FirstOrDefaultAsync(o => o.Id == id);
            //Không tìm thấy trả not found
            if (order == null)
            {
                return NotFound();
            }
            //nếu next status của status hiện tại không giống với newStatus dc gửi từ view qua thì trả error và điều hướng về trang details
            if (!OrderStatus.NextStatuses(order.Status).Contains(newStatus))
            {
                TempData["Error"] = "Không thể chuyển đơn sang trạng thái này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            //cho status hiện tại vào currentStatus
            var currentStatus = order.Status;
            //mở transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            //theo dõi sự thay đổi, cho tìm kiếm các đơn có id = id từ view gửi qua
            //cùng với status =  với status hiện tại
            //Lý do: vì Status hiện tại có thể đã bị thay đổi do admin 2 đã thực hiện trước việc cập nhật trạng thái
            //cho nên cần lấy lại 1 lần nữa status hiện tại ở database và xét nó với status ở thời điểm admin 1 đang thấy
            //Thực thi ngay lập tức việc gán newStatus vào status hiện tại
            var changed = await _context.Orders
                .Where(o => o.Id == id && o.Status == currentStatus)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, newStatus));
            //vì chỉ có 1 order lấy ra dc nên nếu sau khi thực thi sẽ có 1 giá trị dc thay đổi hoặc 0 có giá trị nào
            //Nếu trả về 0 thì rollback lại, gửi Error qua TempData(dạng dữ liệu hiện 1 lần)
            //Điều hướng về details gửi kèm id tức điều hướng về details hiện tại đang đứng
            if(changed == 0)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Đơn hàng vừa được người khác thay đổi, Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(Details), new { id });
            }
            //Nếu newStatus dc gửi qua là cancelled
            if(newStatus == OrderStatus.Cancelled)
            {
                //Lặp Details trong order rồi rồi thực thi việc cộng lại tồn kho từng đầu mục sách vào database
                foreach(var d in order.Details)
                {
                    await _context.Books
                        .Where(b => b.Id == d.BookId)
                        .ExecuteUpdateAsync(s => s.SetProperty(b => b.Stock, b => b.Stock + d.Quantity));
                }
            }
            //Kết thúc transaction
            await transaction.CommitAsync();

            //Gửi 1 dữ liệu success sang view
            TempData["Success"] = $"Đã chuyển đơn #{id} sang \"{newStatus}\".";
            //Điều hướng về lại details đang đứng
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
