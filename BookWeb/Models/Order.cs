using System.ComponentModel.DataAnnotations.Schema;

namespace BookWeb.Models
{
        public static class OrderStatus
        {
            public const string Pending = "Chờ xác nhận";
            public const string Confirmed = "Đã xác nhận";
            public const string Shipping = "Đang giao ";
            public const string Completed = "Hoàn thành";
            public const string Cancelled = "Đã huỷ";
        }

        public class Order
        {
            public int Id { get; set; }
            public int UserId { get; set; }
            public AppUser? User { get; set; }
            public DateTime OrderDate { get; set; } = DateTime.Now;
            public string CustomerName { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string? Note { get; set; }
            public string Status { get; set; } = OrderStatus.Pending;
            [Column(TypeName ="decimal(18,2)")]
            public decimal Total { get; set; }
            public List<OrderDetail>? Details { get; set; }
        }

        public class OrderDetail
        {
            public int Id { get; set; }
            public int OrderId { get; set; }
            public Order? Order { get; set; }

            public int BookId { get; set; }
            public Book? Book { get; set; }

            public string BookTitle { get; set; } = string.Empty;
            public int Quantity { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal UnitPrice { get; set; }
            public decimal SubTotal => UnitPrice * Quantity;
        }
    }

