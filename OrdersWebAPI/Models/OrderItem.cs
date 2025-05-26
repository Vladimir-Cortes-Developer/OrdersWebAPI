using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models
{
    // Modelo OrderItem (tabla de relación muchos a muchos entre Order y Product)
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [ForeignKey("Order")]
        public int OrderId { get; set; }

        [Required]
        [ForeignKey("Product")]
        public int ProductId { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Quantity { get; set; }

        // Propiedad calculada para el total del item
        [NotMapped]
        public decimal ItemTotal => UnitPrice * Quantity;

        // Navegación hacia Order
        public virtual Order Order { get; set; } = null!;

        // Navegación hacia Product
        public virtual Product Product { get; set; } = null!;
    }
}
