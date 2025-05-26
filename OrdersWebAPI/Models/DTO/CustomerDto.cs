namespace OrdersWebAPI.Models.DTO
{
    // DTO básico para Customer
    public class CustomerDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
        public string FullName => $"{FirstName} {LastName}";
    }
}
