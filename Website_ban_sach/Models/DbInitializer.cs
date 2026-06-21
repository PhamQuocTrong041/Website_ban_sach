using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Lớp gieo dữ liệu khởi tạo (Database Seeding) cho Danh mục, Sách và Tài khoản Admin
    /// </summary>
    public static class DbInitializer
    {
        public static async Task SeedAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Tự động Migrate Database nếu chưa cập nhật hết
            await context.Database.MigrateAsync();

            // 1. Seed Vai trò (Roles)
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            // 2. Seed Tài khoản Admin mặc định: admin | admin123
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = "admin",
                    Email = "admin@bookstore.com",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // 3. Seed Danh mục (Categories)
            if (!await context.Categories.AnyAsync())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "Văn học", Icon = "fa-book" },
                    new Category { Name = "Kinh tế", Icon = "fa-chart-pie" },
                    new Category { Name = "Kỹ năng sống", Icon = "fa-seedling" },
                    new Category { Name = "Tâm lý", Icon = "fa-heart" },
                    new Category { Name = "Thiếu nhi", Icon = "fa-child" }
                };

                await context.Categories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed Sách mẫu (Products)
            if (!await context.Products.AnyAsync())
            {
                // Lấy danh mục vừa tạo để liên kết khóa ngoại
                var vanHoc = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Văn học");
                var kinhTe = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Kinh tế");
                var tamLy = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Tâm lý");
                var kyNang = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Kỹ năng sống");

                var products = new List<Product>
                {
                    new Product
                    {
                        Title = "Đắc Nhân Tâm",
                        Author = "Dale Carnegie",
                        Description = "Đắc nhân tâm được mệnh danh là cuốn sách bán chạy nhất mọi thời đại, giúp người đọc thấu hiểu bản thân và giao tiếp nghệ thuật.",
                        Price = 71200,
                        OldPrice = 89000,
                        ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?q=80&w=300",
                        CategoryId = tamLy?.Id ?? 1,
                        StockQuantity = 100,
                        CreatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Title = "Nhà Giả Kim",
                        Author = "Paulo Coelho",
                        Description = "Tiểu thuyết Nhà giả kim là một trong những cuốn sách bán chạy nhất lịch sử, kể về hành trình tìm kiếm vận mệnh của Santiago.",
                        Price = 63200,
                        OldPrice = 79000,
                        ImageUrl = "https://images.unsplash.com/photo-1589829085413-56de8ae18c73?q=80&w=300",
                        CategoryId = vanHoc?.Id ?? 1,
                        StockQuantity = 50,
                        CreatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Title = "Nghĩ Giàu Làm Giàu",
                        Author = "Napoleon Hill",
                        Description = "Cuốn sách kinh điển về làm giàu và thành công cá nhân của tác giả Napoleon Hill.",
                        Price = 88000,
                        OldPrice = 110000,
                        ImageUrl = "https://images.unsplash.com/photo-1592496431122-2349e0fbc666?q=80&w=300",
                        CategoryId = kinhTe?.Id ?? 1,
                        StockQuantity = 30,
                        CreatedDate = DateTime.Now
                    },
                    new Product
                    {
                        Title = "Hạt Giống Tâm Hồn",
                        Author = "Nhiều tác giả",
                        Description = "Bộ sách về những câu chuyện ngắn đầy triết lý nhân sinh giúp khơi gợi niềm tin yêu cuộc sống.",
                        Price = 45000,
                        OldPrice = 50000,
                        ImageUrl = "https://images.unsplash.com/photo-1512820790803-83ca734da794?q=80&w=300",
                        CategoryId = kyNang?.Id ?? 1,
                        StockQuantity = 80,
                        CreatedDate = DateTime.Now
                    }
                };

                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // 5. Seed Cài đặt mặc định (AppSettings)
            if (!await context.AppSettings.AnyAsync())
            {
                var settings = new List<AppSetting>
                {
                    new AppSetting { Key = "WebsiteName", Value = "BookStore Nhóm 2", DisplayName = "Tên Website", Group = "General" },
                    new AppSetting { Key = "Hotline", Value = "0123 456 789", DisplayName = "Số điện thoại Hotline", Group = "Contact" },
                    new AppSetting { Key = "Email", Value = "Hutech@nhom2.com", DisplayName = "Email liên hệ", Group = "Contact" },
                    new AppSetting { Key = "Address", Value = "Hutech Thu Duc Campus", DisplayName = "Địa chỉ", Group = "Contact" },
                    new AppSetting { Key = "MoMoPartnerCode", Value = "MOMO2AI520260621_TEST", DisplayName = "MoMo Partner Code", Group = "Payment" },
                    new AppSetting { Key = "MoMoAccessKey", Value = "SzVLICup7M4fILHt", DisplayName = "MoMo Access Key", Group = "Payment" },
                    new AppSetting { Key = "MoMoSecretKey", Value = "MBzpFanPjal183nFABbBqfxcZrx4PPMG", DisplayName = "MoMo Secret Key", Group = "Payment" }
                };

                await context.AppSettings.AddRangeAsync(settings);
                await context.SaveChangesAsync();
            }

            // 6. Seed Thông báo mẫu ban đầu (Notifications)
            if (!await context.Notifications.AnyAsync())
            {
                var notifications = new List<Notification>
                {
                    new Notification { Title = "Hệ thống sẵn sàng", Content = "Hệ thống quản trị Website bán sách Nhóm 2 đã khởi tạo thành công.", CreatedAt = DateTime.Now.AddDays(-2), IsRead = true, Type = "Info" },
                    new Notification { Title = "Sách sắp hết hàng", Content = "Sách 'Nghĩ Giàu Làm Giàu' chỉ còn 3 cuốn trong kho. Vui lòng nhập thêm hàng.", CreatedAt = DateTime.Now.AddDays(-1), IsRead = false, Type = "Stock" },
                    new Notification { Title = "Đơn hàng mẫu", Content = "Đơn hàng #3412 đặt bởi Nguyễn Minh Hiển đã thanh toán thành công qua MoMo.", CreatedAt = DateTime.Now, IsRead = false, Type = "Order" }
                };

                await context.Notifications.AddRangeAsync(notifications);
                await context.SaveChangesAsync();
            }
        }
    }
}
