using System.Collections.Generic;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// ViewModel dùng để hiển thị danh sách người dùng và các vai trò tương ứng của họ trong trang quản trị
    /// </summary>
    public class UserRoleViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IList<string> Roles { get; set; } = new List<string>();
    }
}
