using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models.DTO
{
    // DTO para crear OrderItem
    public class OrderItemCreateDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Quantity { get; set; }
    }
}
