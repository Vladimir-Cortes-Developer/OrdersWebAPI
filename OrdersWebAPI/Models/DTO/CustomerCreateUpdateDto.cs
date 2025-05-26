using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models.DTO
{
    // DTO para crear/actualizar Customer
    public class CustomerCreateUpdateDto
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }
    }
}
