using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Lớp kết nối Cơ sở dữ liệu, hỗ trợ quản lý tài khoản thông qua Identity
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Bảng danh mục sản phẩm
        public DbSet<Category> Categories { get; set; }

        // Bảng thông tin sách/sản phẩm
        public DbSet<Product> Products { get; set; }

        // Bảng đơn hàng
        public DbSet<Order> Orders { get; set; }

        // Bảng chi tiết đơn hàng
        public DbSet<OrderItem> OrderItems { get; set; }
    }
}
