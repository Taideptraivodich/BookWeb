namespace BookWeb.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        //Một danh mục có nhiều sách
        public List<Book> Books { get; set; } = new();
    }
}
