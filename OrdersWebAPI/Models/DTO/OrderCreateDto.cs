using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models.DTO
{
    // DTO para crear Order
    public class OrderCreateDto
    {
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public List<OrderItemCreateDto> OrderItems { get; set; } = new List<OrderItemCreateDto>();
    }
}
