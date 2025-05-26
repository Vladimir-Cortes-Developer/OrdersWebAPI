using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models.DTO
{
    // DTO para crear/actualizar Supplier
    public class SupplierCreateUpdateDto
    {
        [Required]
        [StringLength(100)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ContactName { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }

        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? Fax { get; set; }
    }
}
