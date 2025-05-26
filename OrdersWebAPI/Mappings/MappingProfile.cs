using AutoMapper;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Models;

namespace OrdersWebAPI.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Mapeos para Customer
            CreateMap<Customer, CustomerDto>()
                .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));

            CreateMap<CustomerCreateUpdateDto, Customer>();
            CreateMap<Customer, CustomerCreateUpdateDto>();

            // Mapeos para Supplier
            CreateMap<Supplier, SupplierDto>();
            CreateMap<SupplierCreateUpdateDto, Supplier>();
            CreateMap<Supplier, SupplierCreateUpdateDto>();

            // Mapeos para Product
            CreateMap<Product, ProductDto>()
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.CompanyName));

            CreateMap<ProductCreateUpdateDto, Product>();
            CreateMap<Product, ProductCreateUpdateDto>();

            // Mapeos para Order
            CreateMap<Order, OrderDto>()
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => $"{src.Customer.FirstName} {src.Customer.LastName}"))
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));

            CreateMap<OrderCreateDto, Order>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => GenerateOrderNumber()))
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));

            // Mapeos para OrderItem
            CreateMap<OrderItem, OrderItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.ProductName))
                .ForMember(dest => dest.ItemTotal, opt => opt.MapFrom(src => src.UnitPrice * src.Quantity));

            CreateMap<OrderItemCreateDto, OrderItem>()
                .ForMember(dest => dest.UnitPrice, opt => opt.Ignore()); // Se establecerá desde el Product
        }

        private static string GenerateOrderNumber()
        {
            return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}
