using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Services.Interfaces;

namespace OrdersWebAPI.Controllers
{
    // ===============================================
    // CONTROLADOR DE PRODUCTS
    // ===============================================

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMappingService _mappingService;

        public ProductsController(ECommerceDbContext context, IMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
            [FromQuery] int? supplierId = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null,
            [FromQuery] bool? isDiscontinued = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            try
            {
                var query = _context.Products
                    .Include(p => p.Supplier)
                    .AsQueryable();

                // Filtros con validaciones
                if (supplierId.HasValue)
                {
                    if (supplierId.Value <= 0)
                        return BadRequest(new { message = "Invalid supplier ID." });
                    query = query.Where(p => p.SupplierId == supplierId.Value);
                }

                if (minPrice.HasValue)
                {
                    if (minPrice.Value < 0)
                        return BadRequest(new { message = "Minimum price cannot be negative." });
                    query = query.Where(p => p.UnitPrice >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    if (maxPrice.Value < 0)
                        return BadRequest(new { message = "Maximum price cannot be negative." });
                    query = query.Where(p => p.UnitPrice <= maxPrice.Value);
                }

                // Validar rango de precios
                if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
                    return BadRequest(new { message = "Minimum price cannot be greater than maximum price." });

                if (isDiscontinued.HasValue)
                    query = query.Where(p => p.IsDiscontinued == isDiscontinued.Value);

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(p => p.ProductName.Contains(search) ||
                                           (p.Package != null && p.Package.Contains(search)) ||
                                           (p.Supplier != null && p.Supplier.CompanyName.Contains(search)));

                var totalItems = await query.CountAsync();
                var products = await query
                    .OrderBy(p => p.ProductName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var productDtos = _mappingService.MapToDto(products);

                // Headers de paginación
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling((double)totalItems / pageSize)).ToString());

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products.", details = ex.Message });
            }
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            try
            {
                var product = await _context.Products
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                return Ok(_mappingService.MapToDto(product));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the product.", details = ex.Message });
            }
        }

        // GET: api/products/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetActiveProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .Where(p => !p.IsDiscontinued)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving active products.", details = ex.Message });
            }
        }

        // GET: api/products/discontinued
        [HttpGet("discontinued")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetDiscontinuedProducts()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .Where(p => p.IsDiscontinued)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving discontinued products.", details = ex.Message });
            }
        }

        // GET: api/products/supplier/5
        [HttpGet("supplier/{supplierId}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsBySupplier(int supplierId)
        {
            if (supplierId <= 0)
                return BadRequest(new { message = "Invalid supplier ID." });

            try
            {
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == supplierId);
                if (!supplierExists)
                    return NotFound(new { message = $"Supplier with ID {supplierId} not found." });

                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .Where(p => p.SupplierId == supplierId)
                    .OrderBy(p => p.ProductName)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving products by supplier.", details = ex.Message });
            }
        }

        // GET: api/products/search/{term}
        [HttpGet("search/{term}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required." });

            if (term.Length < 2)
                return BadRequest(new { message = "Search term must be at least 2 characters long." });

            try
            {
                var products = await _context.Products
                    .Include(p => p.Supplier)
                    .Where(p => p.ProductName.Contains(term) ||
                               (p.Package != null && p.Package.Contains(term)) ||
                               (p.Supplier != null && p.Supplier.CompanyName.Contains(term)))
                    .OrderBy(p => p.ProductName)
                    .Take(20)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(products));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while searching products.", details = ex.Message });
            }
        }

        // POST: api/products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct(ProductCreateUpdateDto productDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // Verificar que el supplier existe
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == productDto.SupplierId);
                if (!supplierExists)
                    return BadRequest(new { message = $"Supplier with ID {productDto.SupplierId} not found." });

                var product = _mappingService.MapToEntity(productDto);

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Recargar con supplier para el DTO
                await _context.Entry(product)
                    .Reference(p => p.Supplier)
                    .LoadAsync();

                var createdProductDto = _mappingService.MapToDto(product);
                return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, createdProductDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the product.", details = ex.Message });
            }
        }

        // PUT: api/products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductCreateUpdateDto productDto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                // Verificar que el supplier existe
                var supplierExists = await _context.Suppliers.AnyAsync(s => s.Id == productDto.SupplierId);
                if (!supplierExists)
                    return BadRequest(new { message = $"Supplier with ID {productDto.SupplierId} not found." });

                // Usar el servicio de mapeo en lugar del método UpdateFromDto
                _mappingService.MapToEntity(productDto, product);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await ProductExists(id))
                    return NotFound(new { message = $"Product with ID {id} no longer exists." });
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product.", details = ex.Message });
            }
        }

        // PATCH: api/products/5/price
        [HttpPatch("{id}/price")]
        public async Task<IActionResult> UpdateProductPrice(int id, [FromBody] decimal newPrice)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            if (newPrice < 0)
                return BadRequest(new { message = "Price cannot be negative." });

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                product.UnitPrice = newPrice;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the product price.", details = ex.Message });
            }
        }

        // PATCH: api/products/5/discontinue
        [HttpPatch("{id}/discontinue")]
        public async Task<IActionResult> DiscontinueProduct(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                if (product.IsDiscontinued)
                    return BadRequest(new { message = "Product is already discontinued." });

                product.IsDiscontinued = true;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while discontinuing the product.", details = ex.Message });
            }
        }

        // PATCH: api/products/5/reactivate
        [HttpPatch("{id}/reactivate")]
        public async Task<IActionResult> ReactivateProduct(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                if (!product.IsDiscontinued)
                    return BadRequest(new { message = "Product is already active." });

                product.IsDiscontinued = false;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while reactivating the product.", details = ex.Message });
            }
        }

        // DELETE: api/products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            try
            {
                var product = await _context.Products
                    .Include(p => p.OrderItems)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null)
                    return NotFound(new { message = $"Product with ID {id} not found." });

                if (product.OrderItems.Any())
                    return BadRequest(new { message = "Cannot delete product with existing order items. Consider discontinuing instead." });

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the product.", details = ex.Message });
            }
        }

        // GET: api/products/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetProductStatistics()
        {
            try
            {
                var totalProducts = await _context.Products.CountAsync();
                var activeProducts = await _context.Products.CountAsync(p => !p.IsDiscontinued);
                var discontinuedProducts = await _context.Products.CountAsync(p => p.IsDiscontinued);

                var averagePrice = totalProducts > 0
                    ? await _context.Products.AverageAsync(p => p.UnitPrice)
                    : 0;

                var mostExpensiveProduct = await _context.Products
                    .Include(p => p.Supplier)
                    .OrderByDescending(p => p.UnitPrice)
                    .FirstOrDefaultAsync();

                var cheapestProduct = await _context.Products
                    .Include(p => p.Supplier)
                    .OrderBy(p => p.UnitPrice)
                    .FirstOrDefaultAsync();

                var productsBySupplier = await _context.Products
                    .Include(p => p.Supplier)
                    .GroupBy(p => new { p.SupplierId, p.Supplier!.CompanyName })
                    .Select(g => new {
                        SupplierId = g.Key.SupplierId,
                        SupplierName = g.Key.CompanyName,
                        ProductCount = g.Count(),
                        ActiveCount = g.Count(p => !p.IsDiscontinued),
                        AveragePrice = g.Average(p => p.UnitPrice)
                    })
                    .OrderByDescending(x => x.ProductCount)
                    .ToListAsync();

                var topSellingProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.ProductName })
                    .Select(g => new {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.UnitPrice * oi.Quantity)
                    })
                    .OrderByDescending(x => x.TotalQuantitySold)
                    .Take(5)
                    .ToListAsync();

                var statistics = new
                {
                    Overview = new
                    {
                        TotalProducts = totalProducts,
                        ActiveProducts = activeProducts,
                        DiscontinuedProducts = discontinuedProducts,
                        AveragePrice = averagePrice
                    },
                    PriceRange = new
                    {
                        MostExpensive = mostExpensiveProduct != null ? new
                        {
                            mostExpensiveProduct.Id,
                            mostExpensiveProduct.ProductName,
                            mostExpensiveProduct.UnitPrice,
                            SupplierName = mostExpensiveProduct.Supplier?.CompanyName
                        } : null,
                        Cheapest = cheapestProduct != null ? new
                        {
                            cheapestProduct.Id,
                            cheapestProduct.ProductName,
                            cheapestProduct.UnitPrice,
                            SupplierName = cheapestProduct.Supplier?.CompanyName
                        } : null
                    },
                    BySupplier = productsBySupplier,
                    TopSelling = topSellingProducts
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", details = ex.Message });
            }
        }

        private async Task<bool> ProductExists(int id)
        {
            return await _context.Products.AnyAsync(e => e.Id == id);
        }
    }
}
