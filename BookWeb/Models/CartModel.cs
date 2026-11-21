namespace BookWeb.Models
{
    public class CartItem
    {
        public int BookId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartLine
    {
        public Book Book { get; set; } = new ();
        public int Quantity { get; set; }
        public decimal SubTotal => Book.Price * Quantity;
    }

    public class CartViewModel
    {
        public List<CartLine> Lines { get; set; } = new();
        public decimal Total => Lines.Sum(l => l.SubTotal);
    }
}
