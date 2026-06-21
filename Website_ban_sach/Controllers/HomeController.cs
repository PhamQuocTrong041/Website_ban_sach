using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    /// <summary>
    /// Controller chính cho trang chủ, xử lý hiển thị danh sách sách, tìm kiếm và lọc theo danh mục
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: / (Trang chủ)
        public async Task<IActionResult> Index(string? searchString, int? categoryId)
        {
            // Lấy toàn bộ danh sách danh mục để hiển thị lên Sidebar và Menu
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.CurrentSearch = searchString;

            // Xây dựng câu truy vấn sản phẩm
            var productsQuery = _context.Products.Include(p => p.Category).AsQueryable();

            // Lọc theo từ khóa tìm kiếm (Tiêu đề hoặc Tác giả)
            if (!string.IsNullOrEmpty(searchString))
            {
                string searchLower = searchString.ToLower();
                productsQuery = productsQuery.Where(p => p.Title.ToLower().Contains(searchLower) || p.Author.ToLower().Contains(searchLower));
            }

            // Lọc theo Danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            }

            var products = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();
            return View(products);
        }

        // GET: /Home/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Xử lý lưu vết sản phẩm đã xem vào Session
            const string RECENTLY_VIEWED_KEY = "RecentlyViewedProducts";
            string? sessionData = HttpContext.Session.GetString(RECENTLY_VIEWED_KEY);
            List<int> viewedProductIds = new List<int>();

            if (!string.IsNullOrEmpty(sessionData))
            {
                try
                {
                    viewedProductIds = JsonSerializer.Deserialize<List<int>>(sessionData) ?? new List<int>();
                }
                catch
                {
                    viewedProductIds = new List<int>();
                }
            }

            viewedProductIds.Remove(product.Id);
            viewedProductIds.Insert(0, product.Id);

            if (viewedProductIds.Count > 6)
            {
                viewedProductIds.RemoveAt(viewedProductIds.Count - 1);
            }

            HttpContext.Session.SetString(RECENTLY_VIEWED_KEY, JsonSerializer.Serialize(viewedProductIds));

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Wishlist()
        {
            return View(); 
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
