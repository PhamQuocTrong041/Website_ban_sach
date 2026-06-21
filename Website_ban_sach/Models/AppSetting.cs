namespace Website_ban_sach.Models
{
    /// <summary>
    /// Thực thể lưu trữ cấu hình hệ thống động trong cơ sở dữ liệu
    /// </summary>
    public class AppSetting
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Group { get; set; } = "General"; // General, Contact, Payment
    }
}
