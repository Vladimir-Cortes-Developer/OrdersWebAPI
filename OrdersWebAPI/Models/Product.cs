using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models
{
    // Modelo Product
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [ForeignKey("Supplier")]
        public int SupplierId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [StringLength(100)]
        public string? Package { get; set; }

        [Required]
        public bool IsDiscontinued { get; set; } = false;

        // Navegación hacia Supplier
        public virtual Supplier Supplier { get; set; } = null!;

        // Relación uno a muchos con OrderItems
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
