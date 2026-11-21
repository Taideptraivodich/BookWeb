using BookWeb.Models;
using Microsoft.EntityFrameworkCore;
using static BookWeb.Models.Order;

namespace BookWeb.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {  }        
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }        
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<AppUser>().HasIndex(u => u.UserName).IsUnique();

            modelBuilder.Entity<Order>()
                .HasOne(o => o.User).WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderDetail>()
                .HasOne(d => d.Book).WithMany()
                .HasForeignKey(d => d.BookId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
