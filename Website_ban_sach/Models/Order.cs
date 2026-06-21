using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Lớp thực thể lưu trữ thông tin đơn hàng đặt mua sách
    /// </summary>
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [NotMapped]
        public string OrderCode => new Random(Id).Next(0, 10000).ToString("D4");

        public string UserId { get; set; } = string.Empty;

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Tên người nhận không được để trống")]
        [StringLength(100, ErrorMessage = "Tên người nhận không được dài quá 100 ký tự")]
        [Display(Name = "Tên người nhận")]
        public string ReceiverName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string ReceiverPhone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Địa chỉ nhận hàng không được để trống")]
        [StringLength(300, ErrorMessage = "Địa chỉ không được dài quá 300 ký tự")]
        [Display(Name = "Địa chỉ nhận hàng")]
        public string ReceiverAddress { get; set; } = string.Empty;

        [Display(Name = "Tổng tiền")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        [Display(Name = "Phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "COD"; // COD, BankTransfer

        [Display(Name = "Trạng thái thanh toán")]
        public string PaymentStatus { get; set; } = "Chưa thanh toán"; // Chưa thanh toán, Đã thanh toán

        [Display(Name = "Trạng thái đơn hàng")]
        public string Status { get; set; } = "Chờ xử lý"; // Chờ xử lý, Đang giao, Đã giao, Đã hủy

        // Mối quan hệ Một - Nhiều với OrderItem
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        
    }
}
