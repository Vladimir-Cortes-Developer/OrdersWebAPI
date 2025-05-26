using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Models;

namespace OrdersWebAPI.Services.Interfaces
{
    // ===============================================
    // INTERFAZ IMappingService
    // ===============================================
    public interface IMappingService
    {
        // Customer mappings
        CustomerDto MapToDto(Customer customer);
        Customer MapToEntity(CustomerCreateUpdateDto dto);
        void MapToEntity(CustomerCreateUpdateDto dto, Customer entity);
        List<CustomerDto> MapToDto(IEnumerable<Customer> customers);

        // Supplier mappings
        SupplierDto MapToDto(Supplier supplier);
        Supplier MapToEntity(SupplierCreateUpdateDto dto);
        void MapToEntity(SupplierCreateUpdateDto dto, Supplier entity);
        List<SupplierDto> MapToDto(IEnumerable<Supplier> suppliers);

        // Product mappings
        ProductDto MapToDto(Product product);
        Product MapToEntity(ProductCreateUpdateDto dto);
        void MapToEntity(ProductCreateUpdateDto dto, Product entity);
        List<ProductDto> MapToDto(IEnumerable<Product> products);

        // Order mappings
        OrderDto MapToDto(Order order);
        Task<Order> MapToEntityAsync(OrderCreateDto dto);
        List<OrderDto> MapToDto(IEnumerable<Order> orders);

        // OrderItem mappings
        OrderItemDto MapToDto(OrderItem orderItem);
        OrderItem MapToEntity(OrderItemCreateDto dto, decimal unitPrice);
        List<OrderItemDto> MapToDto(IEnumerable<OrderItem> orderItems);
    }

}
