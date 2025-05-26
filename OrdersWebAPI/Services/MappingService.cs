using AutoMapper;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Models;
using OrdersWebAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace OrdersWebAPI.Services
{
    // ===============================================
    // IMPLEMENTACIÓN MappingService
    // ===============================================
    public class MappingService : IMappingService
    {
        private readonly ECommerceDbContext _context;

        public MappingService(ECommerceDbContext context)
        {
            _context = context;
        }

        // ===============================================
        // CUSTOMER MAPPINGS
        // ===============================================

        public CustomerDto MapToDto(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            return new CustomerDto
            {
                Id = customer.Id,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                City = customer.City,
                Country = customer.Country,
                Phone = customer.Phone
            };
        }

        public Customer MapToEntity(CustomerCreateUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Customer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                City = dto.City,
                Country = dto.Country,
                Phone = dto.Phone
            };
        }

        public void MapToEntity(CustomerCreateUpdateDto dto, Customer entity)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;
            entity.City = dto.City;
            entity.Country = dto.Country;
            entity.Phone = dto.Phone;
        }

        public List<CustomerDto> MapToDto(IEnumerable<Customer> customers)
        {
            if (customers == null)
                return new List<CustomerDto>();

            return customers.Select(MapToDto).ToList();
        }

        // ===============================================
        // SUPPLIER MAPPINGS
        // ===============================================

        public SupplierDto MapToDto(Supplier supplier)
        {
            if (supplier == null)
                throw new ArgumentNullException(nameof(supplier));

            return new SupplierDto
            {
                Id = supplier.Id,
                CompanyName = supplier.CompanyName,
                ContactName = supplier.ContactName,
                City = supplier.City,
                Country = supplier.Country,
                Phone = supplier.Phone,
                Fax = supplier.Fax
            };
        }

        public Supplier MapToEntity(SupplierCreateUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Supplier
            {
                CompanyName = dto.CompanyName,
                ContactName = dto.ContactName,
                City = dto.City,
                Country = dto.Country,
                Phone = dto.Phone,
                Fax = dto.Fax
            };
        }

        public void MapToEntity(SupplierCreateUpdateDto dto, Supplier entity)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.CompanyName = dto.CompanyName;
            entity.ContactName = dto.ContactName;
            entity.City = dto.City;
            entity.Country = dto.Country;
            entity.Phone = dto.Phone;
            entity.Fax = dto.Fax;
        }

        public List<SupplierDto> MapToDto(IEnumerable<Supplier> suppliers)
        {
            if (suppliers == null)
                return new List<SupplierDto>();

            return suppliers.Select(MapToDto).ToList();
        }

        // ===============================================
        // PRODUCT MAPPINGS
        // ===============================================

        public ProductDto MapToDto(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            return new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                SupplierId = product.SupplierId,
                SupplierName = product.Supplier?.CompanyName ?? string.Empty,
                UnitPrice = product.UnitPrice,
                Package = product.Package,
                IsDiscontinued = product.IsDiscontinued
            };
        }

        public Product MapToEntity(ProductCreateUpdateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new Product
            {
                ProductName = dto.ProductName,
                SupplierId = dto.SupplierId,
                UnitPrice = dto.UnitPrice,
                Package = dto.Package,
                IsDiscontinued = dto.IsDiscontinued
            };
        }

        public void MapToEntity(ProductCreateUpdateDto dto, Product entity)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            entity.ProductName = dto.ProductName;
            entity.SupplierId = dto.SupplierId;
            entity.UnitPrice = dto.UnitPrice;
            entity.Package = dto.Package;
            entity.IsDiscontinued = dto.IsDiscontinued;
        }

        public List<ProductDto> MapToDto(IEnumerable<Product> products)
        {
            if (products == null)
                return new List<ProductDto>();

            return products.Select(MapToDto).ToList();
        }

        // ===============================================
        // ORDER MAPPINGS
        // ===============================================

        public OrderDto MapToDto(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return new OrderDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer != null
                    ? $"{order.Customer.FirstName} {order.Customer.LastName}"
                    : string.Empty,
                TotalAmount = order.TotalAmount,
                OrderNumber = order.OrderNumber,
                OrderItems = order.OrderItems?.Select(MapToDto).ToList() ?? new List<OrderItemDto>()
            };
        }

        public async Task<Order> MapToEntityAsync(OrderCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // Verificar que el customer existe
            var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
                throw new ArgumentException($"Customer with ID {dto.CustomerId} not found.");

            var order = new Order
            {
                CustomerId = dto.CustomerId,
                OrderDate = DateTime.UtcNow,
                OrderNumber = GenerateOrderNumber(),
                OrderItems = new List<OrderItem>()
            };

            decimal totalAmount = 0;

            foreach (var itemDto in dto.OrderItems)
            {
                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new ArgumentException($"Product with ID {itemDto.ProductId} not found.");

                if (product.IsDiscontinued)
                    throw new ArgumentException($"Product '{product.ProductName}' is discontinued and cannot be ordered.");

                var orderItem = new OrderItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = product.UnitPrice
                };

                order.OrderItems.Add(orderItem);
                totalAmount += orderItem.UnitPrice * orderItem.Quantity;
            }

            order.TotalAmount = totalAmount;
            return order;
        }

        public List<OrderDto> MapToDto(IEnumerable<Order> orders)
        {
            if (orders == null)
                return new List<OrderDto>();

            return orders.Select(MapToDto).ToList();
        }

        // ===============================================
        // ORDER ITEM MAPPINGS
        // ===============================================

        public OrderItemDto MapToDto(OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            return new OrderItemDto
            {
                Id = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = orderItem.Product?.ProductName ?? string.Empty,
                UnitPrice = orderItem.UnitPrice,
                Quantity = orderItem.Quantity,
                ItemTotal = orderItem.UnitPrice * orderItem.Quantity
            };
        }

        public OrderItem MapToEntity(OrderItemCreateDto dto, decimal unitPrice)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new OrderItem
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                UnitPrice = unitPrice
            };
        }

        public List<OrderItemDto> MapToDto(IEnumerable<OrderItem> orderItems)
        {
            if (orderItems == null)
                return new List<OrderItemDto>();

            return orderItems.Select(MapToDto).ToList();
        }

        // ===============================================
        // MÉTODOS AUXILIARES PRIVADOS
        // ===============================================

        private static string GenerateOrderNumber()
        {
            // Genera un número de orden único basado en timestamp + número aleatorio
            return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{Random.Shared.Next(1000, 9999)}";
        }
    }
}


