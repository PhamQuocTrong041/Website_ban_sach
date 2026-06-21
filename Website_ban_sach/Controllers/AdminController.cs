using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    /// <summary>
    /// Controller quản lý thành viên dành riêng cho vai trò Admin
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin
        // Hiển thị danh sách tất cả người dùng và vai trò của họ
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesViewModelList = new List<UserRoleViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesViewModelList.Add(new UserRoleViewModel
                {
                    UserId = user.Id,
                    Username = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    Roles = roles
                });
            }

            return View(userRolesViewModelList);
        }

        // POST: /Admin/ToggleRole
        // Chuyển đổi vai trò của người dùng giữa Admin và Customer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleRole(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Ngăn chặn việc tự tước quyền Admin của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["Error"] = "Bạn không thể tự thay đổi vai trò của tài khoản đang đăng nhập!";
                return RedirectToAction(nameof(Index));
            }

            var roles = await _userManager.GetRolesAsync(user);
            bool isAdmin = roles.Contains("Admin");

            if (isAdmin)
            {
                // Xóa vai trò Admin và chuyển thành Customer
                await _userManager.RemoveFromRoleAsync(user, "Admin");
                if (!await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                }
                TempData["Success"] = $"Đã hạ quyền tài khoản '{user.UserName}' xuống khách hàng (Customer).";
            }
            else
            {
                // Thăng chức lên Admin
                if (await _userManager.IsInRoleAsync(user, "Customer"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Customer");
                }
                await _userManager.AddToRoleAsync(user, "Admin");
                TempData["Success"] = $"Đã thăng quyền tài khoản '{user.UserName}' lên quản trị viên (Admin).";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/DeleteUser
        // Xóa tài khoản thành viên khỏi hệ thống
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Ngăn tự xóa tài khoản chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null && currentUser.Id == user.Id)
            {
                TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Đã xóa thành công tài khoản '{user.UserName}'.";
            }
            else
            {
                TempData["Error"] = "Có lỗi xảy ra khi xóa tài khoản: " + string.Join(", ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
