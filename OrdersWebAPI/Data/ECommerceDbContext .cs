using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Models;

namespace OrdersWebAPI.Data
{
    public class ECommerceDbContext : DbContext
    {
        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)
        {
        }

        // DbSets para cada entidad
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Customer
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customer");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.City)
                    .HasMaxLength(100);

                entity.Property(e => e.Country)
                    .HasMaxLength(50);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                // Relación con Orders
                entity.HasMany(c => c.Orders)
                    .WithOne(o => o.Customer)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Supplier
            modelBuilder.Entity<Supplier>(entity =>
            {
                entity.ToTable("Supplier");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.CompanyName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.ContactName)
                    .HasMaxLength(100);

                entity.Property(e => e.City)
                    .HasMaxLength(100);

                entity.Property(e => e.Country)
                    .HasMaxLength(50);

                entity.Property(e => e.Phone)
                    .HasMaxLength(20);

                entity.Property(e => e.Fax)
                    .HasMaxLength(20);

                // Relación con Products
                entity.HasMany(s => s.Products)
                    .WithOne(p => p.Supplier)
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Product
            modelBuilder.Entity<Product>(entity =>
            {
                entity.ToTable("Product");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Package)
                    .HasMaxLength(100);

                entity.Property(e => e.IsDiscontinued)
                    .IsRequired()
                    .HasDefaultValue(false);

                // Relación con Supplier
                entity.HasOne(p => p.Supplier)
                    .WithMany(s => s.Products)
                    .HasForeignKey(p => p.SupplierId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con OrderItems
                entity.HasMany(p => p.OrderItems)
                    .WithOne(oi => oi.Product)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de Order
            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Order");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OrderDate)
                    .IsRequired()
                    .HasColumnType("datetime");

                entity.Property(e => e.TotalAmount)
                    .IsRequired()
                    .HasColumnType("decimal(12,2)");

                entity.Property(e => e.OrderNumber)
                    .IsRequired()
                    .HasMaxLength(20);

                // Índice único para OrderNumber
                entity.HasIndex(e => e.OrderNumber)
                    .IsUnique();

                // Relación con Customer
                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con OrderItems
                entity.HasMany(o => o.OrderItems)
                    .WithOne(oi => oi.Order)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de OrderItem
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItem");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.UnitPrice)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(e => e.Quantity)
                    .IsRequired();

                // Relación con Order
                entity.HasOne(oi => oi.Order)
                    .WithMany(o => o.OrderItems)
                    .HasForeignKey(oi => oi.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Relación con Product
                entity.HasOne(oi => oi.Product)
                    .WithMany(p => p.OrderItems)
                    .HasForeignKey(oi => oi.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice compuesto para mejorar rendimiento
                entity.HasIndex(e => new { e.OrderId, e.ProductId });
            });

            // Datos de semilla (opcional)
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Aquí puedes agregar datos de semilla si lo deseas
            // Ejemplo básico:
            /*
            modelBuilder.Entity<Customer>().HasData(
                new Customer { Id = 1, FirstName = "John", LastName = "Doe", City = "New York", Country = "USA" }
            );
            */
        }
    }
}
