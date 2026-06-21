using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Website_ban_sach.Models
{
    public class WishlistItem
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        public int BookId { get; set; }

        [ForeignKey("BookId")]
        public virtual Product? Product { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}