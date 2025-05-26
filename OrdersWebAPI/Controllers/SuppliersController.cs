using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Services.Interfaces;

namespace OrdersWebAPI.Controllers
{
    // ===============================================
    // CONTROLADOR DE SUPPLIERS
    // ===============================================

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class SuppliersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMappingService _mappingService;

        public SuppliersController(ECommerceDbContext context, IMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/suppliers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> GetSuppliers(
            [FromQuery] string? country = null,
            [FromQuery] string? city = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            try
            {
                var query = _context.Suppliers.AsQueryable();

                // Filtros
                if (!string.IsNullOrEmpty(country))
                    query = query.Where(s => s.Country != null && s.Country.Contains(country));

                if (!string.IsNullOrEmpty(city))
                    query = query.Where(s => s.City != null && s.City.Contains(city));

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(s => s.CompanyName.Contains(search) ||
                                           (s.ContactName != null && s.ContactName.Contains(search)) ||
                                           (s.Phone != null && s.Phone.Contains(search)));

                var totalItems = await query.CountAsync();
                var suppliers = await query
                    .OrderBy(s => s.CompanyName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var supplierDtos = _mappingService.MapToDto(suppliers);

                // Headers de paginación
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling((double)totalItems / pageSize)).ToString());

                return Ok(supplierDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving suppliers.", details = ex.Message });
            }
        }

        // GET: api/suppliers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SupplierDto>> GetSupplier(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);

                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                return Ok(_mappingService.MapToDto(supplier));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the supplier.", details = ex.Message });
            }
        }

        // GET: api/suppliers/5/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSupplierProducts(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                var productDtos = _mappingService.MapToDto(supplier.Products);
                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving supplier products.", details = ex.Message });
            }
        }

        // GET: api/suppliers/5/products/active
        [HttpGet("{id}/products/active")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetSupplierActiveProducts(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Products.Where(p => !p.IsDiscontinued))
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                var productDtos = _mappingService.MapToDto(supplier.Products);
                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving active supplier products.", details = ex.Message });
            }
        }

        // POST: api/suppliers
        [HttpPost]
        public async Task<ActionResult<SupplierDto>> CreateSupplier(SupplierCreateUpdateDto supplierDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var supplier = _mappingService.MapToEntity(supplierDto);

                _context.Suppliers.Add(supplier);
                await _context.SaveChangesAsync();

                var createdSupplierDto = _mappingService.MapToDto(supplier);
                return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, createdSupplierDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the supplier.", details = ex.Message });
            }
        }

        // PUT: api/suppliers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, SupplierCreateUpdateDto supplierDto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                // Usar el servicio de mapeo en lugar del método UpdateFromDto
                _mappingService.MapToEntity(supplierDto, supplier);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await SupplierExists(id))
                    return NotFound(new { message = $"Supplier with ID {id} no longer exists." });
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the supplier.", details = ex.Message });
            }
        }

        // DELETE: api/suppliers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Products)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                if (supplier.Products.Any())
                    return BadRequest(new { message = "Cannot delete supplier with existing products." });

                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the supplier.", details = ex.Message });
            }
        }

        // GET: api/suppliers/countries
        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<string>>> GetCountries()
        {
            try
            {
                var countries = await _context.Suppliers
                    .Where(s => !string.IsNullOrEmpty(s.Country))
                    .Select(s => s.Country!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(countries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving countries.", details = ex.Message });
            }
        }

        // GET: api/suppliers/cities
        [HttpGet("cities")]
        public async Task<ActionResult<IEnumerable<string>>> GetCities([FromQuery] string? country = null)
        {
            try
            {
                var query = _context.Suppliers.AsQueryable();

                if (!string.IsNullOrEmpty(country))
                    query = query.Where(s => s.Country == country);

                var cities = await query
                    .Where(s => !string.IsNullOrEmpty(s.City))
                    .Select(s => s.City!)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving cities.", details = ex.Message });
            }
        }

        // GET: api/suppliers/search/{term}
        [HttpGet("search/{term}")]
        public async Task<ActionResult<IEnumerable<SupplierDto>>> SearchSuppliers(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required." });

            if (term.Length < 2)
                return BadRequest(new { message = "Search term must be at least 2 characters long." });

            try
            {
                var suppliers = await _context.Suppliers
                    .Where(s => s.CompanyName.Contains(term) ||
                               (s.ContactName != null && s.ContactName.Contains(term)) ||
                               (s.Phone != null && s.Phone.Contains(term)) ||
                               (s.City != null && s.City.Contains(term)) ||
                               (s.Country != null && s.Country.Contains(term)))
                    .OrderBy(s => s.CompanyName)
                    .Take(20)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(suppliers));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching suppliers.", details = ex.Message });
            }
        }

        // GET: api/suppliers/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetSupplierStatistics()
        {
            try
            {
                var totalSuppliers = await _context.Suppliers.CountAsync();

                var suppliersWithProducts = await _context.Suppliers
                    .Where(s => s.Products.Any())
                    .CountAsync();

                var suppliersWithActiveProducts = await _context.Suppliers
                    .Where(s => s.Products.Any(p => !p.IsDiscontinued))
                    .CountAsync();

                var suppliersByCountry = await _context.Suppliers
                    .Where(s => !string.IsNullOrEmpty(s.Country))
                    .GroupBy(s => s.Country)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                var topSuppliersByProductCount = await _context.Suppliers
                    .Select(s => new {
                        s.Id,
                        s.CompanyName,
                        s.Country,
                        TotalProducts = s.Products.Count(),
                        ActiveProducts = s.Products.Count(p => !p.IsDiscontinued)
                    })
                    .Where(x => x.TotalProducts > 0)
                    .OrderByDescending(x => x.TotalProducts)
                    .Take(5)
                    .ToListAsync();

                var supplierProductRevenue = await _context.Suppliers
                    .Select(s => new {
                        s.Id,
                        s.CompanyName,
                        TotalRevenue = s.Products
                            .SelectMany(p => p.OrderItems)
                            .Sum(oi => (decimal?)oi.UnitPrice * oi.Quantity) ?? 0
                    })
                    .Where(x => x.TotalRevenue > 0)
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(5)
                    .ToListAsync();

                var statistics = new
                {
                    Overview = new
                    {
                        TotalSuppliers = totalSuppliers,
                        SuppliersWithProducts = suppliersWithProducts,
                        SuppliersWithoutProducts = totalSuppliers - suppliersWithProducts,
                        SuppliersWithActiveProducts = suppliersWithActiveProducts
                    },
                    Geographic = new
                    {
                        TopCountries = suppliersByCountry
                    },
                    Performance = new
                    {
                        TopSuppliersByProductCount = topSuppliersByProductCount,
                        TopSuppliersByRevenue = supplierProductRevenue
                    }
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", details = ex.Message });
            }
        }

        // GET: api/suppliers/5/performance
        [HttpGet("{id}/performance")]
        public async Task<ActionResult<object>> GetSupplierPerformance(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplier = await _context.Suppliers
                    .Include(s => s.Products)
                        .ThenInclude(p => p.OrderItems)
                            .ThenInclude(oi => oi.Order)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (supplier == null)
                    return NotFound(new { message = $"Supplier with ID {id} not found." });

                var totalProducts = supplier.Products.Count();
                var activeProducts = supplier.Products.Count(p => !p.IsDiscontinued);
                var discontinuedProducts = supplier.Products.Count(p => p.IsDiscontinued);

                var orderItems = supplier.Products.SelectMany(p => p.OrderItems).ToList();
                var totalQuantitySold = orderItems.Sum(oi => oi.Quantity);
                var totalRevenue = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity);
                var totalOrders = orderItems.Select(oi => oi.OrderId).Distinct().Count();

                var averageProductPrice = totalProducts > 0
                    ? supplier.Products.Average(p => p.UnitPrice)
                    : 0;

                var salesByMonth = orderItems
                    .Where(oi => oi.Order.OrderDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(oi => new { oi.Order.OrderDate.Year, oi.Order.OrderDate.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Quantity = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();

                var topProducts = supplier.Products
                    .Select(p => new {
                        p.Id,
                        p.ProductName,
                        p.UnitPrice,
                        p.IsDiscontinued,
                        QuantitySold = p.OrderItems.Sum(oi => oi.Quantity),
                        Revenue = p.OrderItems.Sum(oi => oi.UnitPrice * oi.Quantity)
                    })
                    .OrderByDescending(x => x.QuantitySold)
                    .Take(5)
                    .ToList();

                var performance = new
                {
                    SupplierInfo = new
                    {
                        supplier.Id,
                        supplier.CompanyName,
                        supplier.ContactName,
                        supplier.Country,
                        supplier.City
                    },
                    ProductOverview = new
                    {
                        TotalProducts = totalProducts,
                        ActiveProducts = activeProducts,
                        DiscontinuedProducts = discontinuedProducts,
                        AverageProductPrice = averageProductPrice
                    },
                    SalesMetrics = new
                    {
                        TotalQuantitySold = totalQuantitySold,
                        TotalRevenue = totalRevenue,
                        TotalOrders = totalOrders,
                        AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0
                    },
                    SalesTrends = salesByMonth,
                    TopProducts = topProducts
                };

                return Ok(performance);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving supplier performance.", details = ex.Message });
            }
        }

        private async Task<bool> SupplierExists(int id)
        {
            return await _context.Suppliers.AnyAsync(e => e.Id == id);
        }
    }
}
