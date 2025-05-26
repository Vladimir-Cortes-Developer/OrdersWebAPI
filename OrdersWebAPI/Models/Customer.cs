using System.ComponentModel.DataAnnotations;

namespace OrdersWebAPI.Models
{
    // Modelo Customer
    public class Customer
    {
        [Key]
        public int Id { get; set; }

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

        // Relación uno a muchos con Orders
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }

}
