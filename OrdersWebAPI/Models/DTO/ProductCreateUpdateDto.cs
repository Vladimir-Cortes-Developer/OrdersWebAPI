using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models.DTO
{
    // DTO para crear/actualizar Product
    public class ProductCreateUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public int SupplierId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal UnitPrice { get; set; }

        [StringLength(100)]
        public string? Package { get; set; }

        public bool IsDiscontinued { get; set; } = false;
    }
}
