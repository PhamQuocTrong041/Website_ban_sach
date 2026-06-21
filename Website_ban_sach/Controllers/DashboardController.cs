using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    /// <summary>
    /// Controller điều hướng trang quản trị tổng quan dành riêng cho Admin
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Dashboard
        public async Task<IActionResult> Index()
        {
            // 1. Thống kê số lượng tổng quan
            int userCount = await _userManager.Users.CountAsync();
            int productCount = await _context.Products.CountAsync();
            int orderCount = await _context.Orders.CountAsync();
            decimal totalRevenue = await _context.Orders
                .Where(o => o.Status != "Đã hủy")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            // 2. Thống kê trạng thái đơn hàng phục vụ vẽ biểu đồ Doughnut
            int pendingCount = await _context.Orders.CountAsync(o => o.Status == "Chờ xử lý");
            int shippingCount = await _context.Orders.CountAsync(o => o.Status == "Đang giao");
            int deliveredCount = await _context.Orders.CountAsync(o => o.Status == "Đã giao");
            int cancelledCount = await _context.Orders.CountAsync(o => o.Status == "Đã hủy");

            // 3. Lấy 5 đơn hàng mới đặt
            var latestOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .ToListAsync();

            // 4. Lấy 5 sách bán chạy nhất
            var topSellingGroup = await _context.OrderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.Price)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            var topSellingProducts = new List<ProductSalesViewModel>();
            foreach (var item in topSellingGroup)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    topSellingProducts.Add(new ProductSalesViewModel
                    {
                        Product = product,
                        QuantitySold = item.QuantitySold,
                        TotalRevenue = item.TotalRevenue
                    });
                }
            }

            // 5. Chuẩn bị dữ liệu vẽ biểu đồ Doanh thu 7 ngày gần đây nhất
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .OrderBy(d => d)
                .ToList();

            var dailyRevenueList = new List<decimal>();
            var dailyLabelsList = new List<string>();

            foreach (var day in last7Days)
            {
                var nextDay = day.AddDays(1);
                var revenue = await _context.Orders
                    .Where(o => o.OrderDate >= day && o.OrderDate < nextDay && o.Status != "Đã hủy")
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                dailyRevenueList.Add(revenue);
                dailyLabelsList.Add(day.ToString("dd/MM"));
            }

            ViewBag.DailyRevenueLabels = dailyLabelsList;
            ViewBag.DailyRevenueValues = dailyRevenueList;

            var stats = new DashboardStatsViewModel
            {
                UserCount = userCount,
                ProductCount = productCount,
                OrderCount = orderCount,
                TotalRevenue = totalRevenue,
                LatestOrders = latestOrders,
                TopSellingProducts = topSellingProducts,
                PendingCount = pendingCount,
                ShippingCount = shippingCount,
                DeliveredCount = deliveredCount,
                CancelledCount = cancelledCount
            };

            return View(stats);
        }

        // GET: /Dashboard/Orders
        // Hiển thị danh sách tất cả các đơn đặt hàng trong hệ thống
        public async Task<IActionResult> Orders(string? statusFilter)
        {
            var query = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(o => o.Status == statusFilter);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            ViewBag.StatusFilter = statusFilter;
            return View(orders);
        }

        // POST: /Dashboard/UpdateStatus
        // Thay đổi nhanh trạng thái đơn hàng của khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cập nhật trạng thái đơn hàng #{id} thành '{status}' thành công.";
            return RedirectToAction(nameof(Orders));
        }
    }
}
