using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. TRANG HIỂN THỊ DANH SÁCH YÊU THÍCH (Index)
        public async Task<IActionResult> Index()
        {
            // Lấy ID người dùng hiện tại (Yêu cầu phải Đăng nhập)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Nếu chưa đăng nhập, tạm thời cho hiển thị danh sách trống hoặc chuyển hướng đến trang Login
                return View(new List<WishlistItem>());
            }

            // Lấy danh sách yêu thích và Include luôn bảng Product để lấy thông tin sách (hình ảnh, tên, giá)
            var wishlist = await _context.WishlistItems
                .Include(w => w.Product)
                .Where(w => w.UserId == userId)
                .ToListAsync();

            return View(wishlist);
        }

        // 2. HÀM THÊM SÁCH VÀO DANH SÁCH YÊU THÍCH
        [HttpPost]
        public async Task<IActionResult> AddToWishlist(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                // Nếu chưa đăng nhập, có thể chuyển hướng đến trang Đăng nhập
                return RedirectToAction("Index");
            }

            // Kiểm tra xem sách đã có trong danh sách chưa
            var exists = await _context.WishlistItems.AnyAsync(w => w.UserId == userId && w.BookId == bookId);

            if (!exists)
            {
                var item = new WishlistItem
                {
                    UserId = userId,
                    BookId = bookId,
                    CreatedDate = DateTime.Now
                };
                _context.Add(item);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index"); // Chuyển hướng về trang danh sách yêu thích
        }

        // 3. HÀM XÓA KHỎI DANH SÁCH YÊU THÍCH (Bổ sung thêm cho đủ tính năng)
        [HttpPost]
        public async Task<IActionResult> RemoveFromWishlist(int id)
        {
            var item = await _context.WishlistItems.FindAsync(id);
            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddToCartFromWishlist(int bookId)
        {
            // 1. Tìm cuốn sách dựa vào bookId từ DB (sử dụng đúng bảng _context.Products)
            var product = await _context.Products.FindAsync(bookId);
            if (product == null)
            {
                return NotFound();
            }

            // 2. Lấy giỏ hàng từ Session ra bằng đúng Key "CartSessionKey" giống CartController
            var sessionData = HttpContext.Session.GetString("CartSessionKey");
            List<CartItem> cart = string.IsNullOrEmpty(sessionData)
                ? new List<CartItem>()
                : System.Text.Json.JsonSerializer.Deserialize<List<CartItem>>(sessionData) ?? new List<CartItem>();

            // 3. Kiểm tra xem mã sách (ProductId) đã tồn tại trong giỏ hàng phẳng chưa
            var cartItem = cart.FirstOrDefault(c => c.ProductId == bookId);
            if (cartItem != null)
            {
                cartItem.Quantity += 1; // Thêm nhanh từ Wishlist mặc định tăng 1
            }
            else
            {
                // Khớp hoàn toàn với cấu trúc thuộc tính phẳng của CartItem trong CartController
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Title = product.Title,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = 1
                });
            }

            // 4. Lưu lại danh sách vào Session đúng Key "CartSessionKey"
            string newSessionData = System.Text.Json.JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("CartSessionKey", newSessionData);

            // 5. Điều hướng thẳng qua trang Index của CartController (Trang giỏ hàng)
            return RedirectToAction("Index", "Cart");
        }
    }
}