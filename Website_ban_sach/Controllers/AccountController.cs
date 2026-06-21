using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
    /// Controller xử lý các nghiệp vụ Xác thực người dùng (Đăng ký, Đăng nhập, Đăng xuất)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.Username, Email = model.Email };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Tạo vai trò "Customer" mặc định nếu chưa tồn tại
                    if (!await _roleManager.RoleExistsAsync("Customer"))
                    {
                        await _roleManager.CreateAsync(new IdentityRole("Customer"));
                    }

                    // Gán vai trò mặc định cho người dùng mới đăng ký là Customer
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // Tự động đăng nhập người dùng sau khi đăng ký thành công
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }

                // Thêm lỗi vào ModelState để hiển thị ra View
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Username, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            }
            return View(model);
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Tạo nhanh tài khoản Admin mẫu để phát triển
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> InitializeAdmin()
        {
            // Kiểm tra và tạo vai trò "Admin"
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Tạo tài khoản admin mặc định: admin / admin123
            var adminUser = await _userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new IdentityUser { UserName = "admin", Email = "admin@bookstore.com", EmailConfirmed = true };
                var result = await _userManager.CreateAsync(adminUser, "admin123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    return Content("Khởi tạo tài khoản Admin thành công! Tài khoản: admin | Mật khẩu: admin123");
                }
                return Content("Khởi tạo thất bại: " + string.Join(", ", result.Errors));
            }

            return Content("Tài khoản Admin đã tồn tại từ trước.");
        }

        // GET: /Account/Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var profileViewModel = new ProfileViewModel
            {
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber
            };

            // Lấy danh sách đơn hàng
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.Orders = orders;

            // Lấy danh sách sản phẩm đã xem
            const string RECENTLY_VIEWED_KEY = "RecentlyViewedProducts";
            string? sessionData = HttpContext.Session.GetString(RECENTLY_VIEWED_KEY);
            List<Product> viewedProducts = new List<Product>();

            if (!string.IsNullOrEmpty(sessionData))
            {
                try
                {
                    var productIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(sessionData);
                    if (productIds != null && productIds.Any())
                    {
                        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
                        viewedProducts = productIds
                            .Select(id => products.FirstOrDefault(p => p.Id == id))
                            .Where(p => p != null)
                            .Cast<Product>()
                            .ToList();
                    }
                }
                catch {}
            }
            ViewBag.ViewedProducts = viewedProducts;

            return View(profileViewModel);
        }

        // POST: /Account/Profile
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Load lại dữ liệu đơn hàng và sản phẩm đã xem đề phòng hiển thị lại View do lỗi validation
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
            ViewBag.Orders = orders;

            const string RECENTLY_VIEWED_KEY = "RecentlyViewedProducts";
            string? sessionData = HttpContext.Session.GetString(RECENTLY_VIEWED_KEY);
            List<Product> viewedProducts = new List<Product>();

            if (!string.IsNullOrEmpty(sessionData))
            {
                try
                {
                    var productIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(sessionData);
                    if (productIds != null && productIds.Any())
                    {
                        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync();
                        viewedProducts = productIds
                            .Select(id => products.FirstOrDefault(p => p.Id == id))
                            .Where(p => p != null)
                            .Cast<Product>()
                            .ToList();
                    }
                }
                catch {}
            }
            ViewBag.ViewedProducts = viewedProducts;

            if (ModelState.IsValid)
            {
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;

                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var error in updateResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Nếu nhập mật khẩu mới thì tiến hành đổi mật khẩu
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    if (string.IsNullOrEmpty(model.CurrentPassword))
                    {
                        ModelState.AddModelError(string.Empty, "Vui lòng nhập mật khẩu hiện tại để thay đổi mật khẩu mới.");
                        return View(model);
                    }

                    var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                    if (!changePasswordResult.Succeeded)
                    {
                        foreach (var error in changePasswordResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công!";
                return RedirectToAction(nameof(Profile));
            }

            return View(model);
        }
    }
}
