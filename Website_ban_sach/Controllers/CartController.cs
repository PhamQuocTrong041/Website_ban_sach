using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    /// <summary>
    /// Controller xử lý các chức năng liên quan đến Giỏ hàng (sử dụng Session dạng JSON)
    /// </summary>
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string CART_KEY = "CartSessionKey";

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Cart
        public IActionResult Index()
        {
            List<CartItem> cart = GetCartItems();
            return View(cart);
        }

        // POST: /Cart/AddToCart/5
        [HttpPost]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            List<CartItem> cart = GetCartItems();
            
            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Title = product.Title,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }

            SaveCartItems(cart);

            // Quay trở lại trang trước hoặc trang giỏ hàng
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(id);
            }

            List<CartItem> cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
            }

            SaveCartItems(cart);
            return RedirectToAction(nameof(Index));
        }

        // POST: /Cart/RemoveFromCart/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveFromCart(int id)
        {
            List<CartItem> cart = GetCartItems();
            var cartItem = cart.FirstOrDefault(c => c.ProductId == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
            }

            SaveCartItems(cart);
            return RedirectToAction(nameof(Index));
        }

        // GET: /Cart/GetCartCount
        [HttpGet]
        public IActionResult GetCartCount()
        {
            var cart = GetCartItems();
            var count = cart.Sum(c => c.Quantity);
            return Json(new { count });
        }

        // GET: /Cart/Checkout
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CartItems = cart;

            var user = await _userManager.GetUserAsync(User);
            var order = new Order
            {
                ReceiverName = user?.UserName ?? string.Empty,
                ReceiverPhone = user?.PhoneNumber ?? string.Empty,
                TotalAmount = cart.Sum(i => i.TotalPrice)
            };

            return View(order);
        }

        // POST: /Cart/Checkout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order model)
        {
            var cart = GetCartItems();
            if (!cart.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Challenge();
                }

                var order = new Order
                {
                    UserId = user.Id,
                    OrderDate = System.DateTime.Now,
                    ReceiverName = model.ReceiverName,
                    ReceiverPhone = model.ReceiverPhone,
                    ReceiverAddress = model.ReceiverAddress,
                    TotalAmount = cart.Sum(i => i.TotalPrice),
                    PaymentMethod = model.PaymentMethod,
                    PaymentStatus = "Chưa thanh toán",
                    Status = "Chờ xử lý"
                };

                foreach (var item in cart)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        // Giảm số lượng tồn kho của sách
                        product.StockQuantity = System.Math.Max(0, product.StockQuantity - item.Quantity);
                        
                        order.OrderItems.Add(new OrderItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                }

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng trong session
                HttpContext.Session.Remove(CART_KEY);

                TempData["SuccessMessage"] = "Đơn hàng của bạn đã được đặt thành công!";
                return RedirectToAction(nameof(CheckoutSuccess), new { id = order.Id });
            }

            // Nếu model có lỗi, hiển thị lại trang kèm lỗi
            ViewBag.CartItems = cart;
            return View(model);
        }

        // GET: /Cart/CheckoutSuccess/5
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CheckoutSuccess(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null || (order.UserId != user.Id && !User.IsInRole("Admin")))
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: /Cart/ConfirmPayment/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null || (order.UserId != user.Id && !User.IsInRole("Admin")))
            {
                return NotFound();
            }

            if (order.PaymentStatus != "Đã thanh toán")
            {
                order.PaymentStatus = "Đã thanh toán";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xác nhận thanh toán đơn hàng thành công!";
            }

            return RedirectToAction(nameof(CheckoutSuccess), new { id = order.Id });
        }

        // Helper: Lấy danh sách sản phẩm trong giỏ hàng từ Session
        private List<CartItem> GetCartItems()
        {
            string? sessionData = HttpContext.Session.GetString(CART_KEY);
            if (string.IsNullOrEmpty(sessionData))
            {
                return new List<CartItem>();
            }
            return JsonSerializer.Deserialize<List<CartItem>>(sessionData) ?? new List<CartItem>();
        }

        // Helper: Lưu danh sách sản phẩm trong giỏ hàng vào Session
        private void SaveCartItems(List<CartItem> cart)
        {
            string sessionData = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CART_KEY, sessionData);
        }
    }
}
