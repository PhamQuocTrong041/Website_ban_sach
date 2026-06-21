using System.ComponentModel.DataAnnotations;

namespace Website_ban_sach.Models
{
    /// <summary>
    /// Lớp biểu diễn Danh mục sách (ví dụ: Văn học, Kinh tế, Thiếu nhi...)
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Biểu tượng")]
        public string? Icon { get; set; } // Class CSS FontAwesome (ví dụ: "fa-book")

        // Mối quan hệ Một - Nhiều: Một danh mục có nhiều sản phẩm
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
