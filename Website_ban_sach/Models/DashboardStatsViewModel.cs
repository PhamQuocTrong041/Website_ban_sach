using System.Collections.Generic;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// ViewModel đóng gói dữ liệu thống kê tổng quan hiển thị trên trang Dashboard Admin
    /// </summary>
    public class DashboardStatsViewModel
    {
        public int UserCount { get; set; }
        public int OrderCount { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalRevenue { get; set; }

        // Bảng dữ liệu liên quan
        public List<Order> LatestOrders { get; set; } = new List<Order>();
        public List<ProductSalesViewModel> TopSellingProducts { get; set; } = new List<ProductSalesViewModel>();

        // Thống kê số lượng đơn hàng theo trạng thái để vẽ biểu đồ
        public int PendingCount { get; set; }
        public int ShippingCount { get; set; }
        public int DeliveredCount { get; set; }
        public int CancelledCount { get; set; }
    }

    /// <summary>
    /// Lớp biểu diễn số lượng bán chạy của từng sản phẩm
    /// </summary>
    public class ProductSalesViewModel
    {
        public Product? Product { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
