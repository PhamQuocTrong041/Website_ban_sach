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
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Orders));
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

        // GET: /Dashboard/Notifications
        public async Task<IActionResult> Notifications()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            return View(notifications);
        }

        // POST: /Dashboard/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Notifications));
        }

        // POST: /Dashboard/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var unread = await _context.Notifications.Where(n => !n.IsRead).ToListAsync();
            foreach (var n in unread)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Notifications));
        }

        // POST: /Dashboard/DeleteNotification/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Notifications));
        }

        // GET: /Dashboard/Settings
        public async Task<IActionResult> Settings()
        {
            var settings = await _context.AppSettings.ToListAsync();
            return View(settings);
        }

        // POST: /Dashboard/SaveSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings)
        {
            if (settings != null)
            {
                foreach (var item in settings)
                {
                    var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == item.Key);
                    if (setting != null)
                    {
                        setting.Value = item.Value ?? string.Empty;
                    }
                }
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật cấu hình hệ thống thành công!";
            }
            return RedirectToAction(nameof(Settings));
        }
    }
}
