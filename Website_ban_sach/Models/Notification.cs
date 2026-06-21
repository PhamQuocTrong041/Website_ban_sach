using System;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Thực thể lưu trữ các thông báo trong hệ thống quản trị
    /// </summary>
    public class Notification
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string Type { get; set; } = "Info"; // Info, Order, Stock, User
    }
}
