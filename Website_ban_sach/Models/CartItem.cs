using System.ComponentModel.DataAnnotations;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Lớp lưu trữ thông tin sản phẩm trong giỏ hàng (sử dụng Session/Cookie)
    /// </summary>
    public class CartItem
    {
        [Key]
        public int ProductId { get; set; }

        [Display(Name = "Tiêu đề sách")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Hình ảnh")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Đơn giá")]
        public decimal Price { get; set; }

        [Display(Name = "Số lượng")]
        public int Quantity { get; set; }

        [Display(Name = "Thành tiền")]
        public decimal TotalPrice => Price * Quantity;
    }
}
