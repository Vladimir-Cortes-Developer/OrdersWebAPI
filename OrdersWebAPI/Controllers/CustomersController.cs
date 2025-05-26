using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using AutoMapper;
using OrdersWebAPI.Services.Interfaces;

namespace OrdersWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CustomersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMappingService _mappingService;

        public CustomersController(ECommerceDbContext context, IMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers(
            [FromQuery] string? country = null,
            [FromQuery] string? city = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _context.Customers.AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(country))
                query = query.Where(c => c.Country != null && c.Country.Contains(country));

            if (!string.IsNullOrEmpty(city))
                query = query.Where(c => c.City != null && c.City.Contains(city));

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.FirstName.Contains(search) ||
                                       c.LastName.Contains(search) ||
                                       (c.Phone != null && c.Phone.Contains(search)));

            // Paginación
            var totalItems = await query.CountAsync();
            var customers = await query
                .OrderBy(c => c.LastName)
                .ThenBy(c => c.FirstName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var customerDtos = _mappingService.MapToDto(customers);

            // Headers de paginación
            Response.Headers.Add("X-Total-Count", totalItems.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());
            Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling((double)totalItems / pageSize)).ToString());

            return Ok(customerDtos);
        }

        // GET: api/customers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid customer ID." });

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            return Ok(_mappingService.MapToDto(customer));
        }

        // GET: api/customers/5/orders
        [HttpGet("{id}/orders")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetCustomerOrders(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid customer ID." });

            var customer = await _context.Customers
                .Include(c => c.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            var orderDtos = _mappingService.MapToDto(customer.Orders);
            return Ok(orderDtos);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(CustomerCreateUpdateDto customerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var customer = _mappingService.MapToEntity(customerDto);

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                var createdCustomerDto = _mappingService.MapToDto(customer);
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, createdCustomerDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the customer.", details = ex.Message });
            }
        }

        // PUT: api/customers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, CustomerCreateUpdateDto customerDto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid customer ID." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            try
            {
                // Usar el servicio de mapeo en lugar del método UpdateFromDto
                _mappingService.MapToEntity(customerDto, customer);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CustomerExists(id))
                    return NotFound(new { message = $"Customer with ID {id} no longer exists." });
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the customer.", details = ex.Message });
            }
        }

        // DELETE: api/customers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid customer ID." });

            var customer = await _context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
                return NotFound(new { message = $"Customer with ID {id} not found." });

            if (customer.Orders.Any())
                return BadRequest(new { message = "Cannot delete customer with existing orders." });

            try
            {
                _context.Customers.Remove(customer);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the customer.", details = ex.Message });
            }
        }

        // GET: api/customers/countries
        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<string>>> GetCountries()
        {
            try
            {
                var countries = await _context.Customers
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .Select(c => c.Country!)
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

        // GET: api/customers/cities
        [HttpGet("cities")]
        public async Task<ActionResult<IEnumerable<string>>> GetCities([FromQuery] string? country = null)
        {
            try
            {
                var query = _context.Customers.AsQueryable();

                if (!string.IsNullOrEmpty(country))
                    query = query.Where(c => c.Country == country);

                var cities = await query
                    .Where(c => !string.IsNullOrEmpty(c.City))
                    .Select(c => c.City!)
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

        // GET: api/customers/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetCustomerStatistics()
        {
            try
            {
                var totalCustomers = await _context.Customers.CountAsync();

                var customersWithOrders = await _context.Customers
                    .Where(c => c.Orders.Any())
                    .CountAsync();

                var customersByCountry = await _context.Customers
                    .Where(c => !string.IsNullOrEmpty(c.Country))
                    .GroupBy(c => c.Country)
                    .Select(g => new { Country = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync();

                var statistics = new
                {
                    TotalCustomers = totalCustomers,
                    CustomersWithOrders = customersWithOrders,
                    CustomersWithoutOrders = totalCustomers - customersWithOrders,
                    TopCountries = customersByCountry
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", details = ex.Message });
            }
        }

        private async Task<bool> CustomerExists(int id)
        {
            return await _context.Customers.AnyAsync(e => e.Id == id);
        }
    }

}
