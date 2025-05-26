using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Models;
using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Services.Interfaces;

namespace OrdersWebAPI.Controllers
{
    // ===============================================
    // CONTROLADOR DE ORDERS
    // ===============================================

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrdersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMappingService _mappingService;

        public OrdersController(ECommerceDbContext context, IMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders(
            [FromQuery] int? customerId = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] decimal? minAmount = null,
            [FromQuery] decimal? maxAmount = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validar parámetros de paginación
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            try
            {
                var query = _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .AsQueryable();

                // Filtros
                if (customerId.HasValue)
                {
                    if (customerId.Value <= 0)
                        return BadRequest(new { message = "Invalid customer ID." });
                    query = query.Where(o => o.CustomerId == customerId.Value);
                }

                if (fromDate.HasValue)
                    query = query.Where(o => o.OrderDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(o => o.OrderDate <= toDate.Value);

                if (minAmount.HasValue)
                {
                    if (minAmount.Value < 0)
                        return BadRequest(new { message = "Minimum amount cannot be negative." });
                    query = query.Where(o => o.TotalAmount >= minAmount.Value);
                }

                if (maxAmount.HasValue)
                {
                    if (maxAmount.Value < 0)
                        return BadRequest(new { message = "Maximum amount cannot be negative." });
                    query = query.Where(o => o.TotalAmount <= maxAmount.Value);
                }

                // Validar rango de fechas
                if (fromDate.HasValue && toDate.HasValue && fromDate.Value > toDate.Value)
                    return BadRequest(new { message = "From date cannot be greater than to date." });

                // Validar rango de montos
                if (minAmount.HasValue && maxAmount.HasValue && minAmount.Value > maxAmount.Value)
                    return BadRequest(new { message = "Minimum amount cannot be greater than maximum amount." });

                var totalItems = await query.CountAsync();
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var orderDtos = _mappingService.MapToDto(orders);

                // Headers de paginación
                Response.Headers.Add("X-Total-Count", totalItems.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());
                Response.Headers.Add("X-Total-Pages", ((int)Math.Ceiling((double)totalItems / pageSize)).ToString());

                return Ok(orderDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving orders.", details = ex.Message });
            }
        }

        // GET: api/orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = $"Order with ID {id} not found." });

                return Ok(_mappingService.MapToDto(order));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the order.", details = ex.Message });
            }
        }

        // GET: api/orders/number/{orderNumber}
        [HttpGet("number/{orderNumber}")]
        public async Task<ActionResult<OrderDto>> GetOrderByNumber(string orderNumber)
        {
            if (string.IsNullOrWhiteSpace(orderNumber))
                return BadRequest(new { message = "Order number is required." });

            try
            {
                var order = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

                if (order == null)
                    return NotFound(new { message = $"Order with number {orderNumber} not found." });

                return Ok(_mappingService.MapToDto(order));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the order.", details = ex.Message });
            }
        }

        // GET: api/orders/customer/5
        [HttpGet("customer/{customerId}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByCustomer(int customerId)
        {
            if (customerId <= 0)
                return BadRequest(new { message = "Invalid customer ID." });

            try
            {
                var customerExists = await _context.Customers.AnyAsync(c => c.Id == customerId);
                if (!customerExists)
                    return NotFound(new { message = $"Customer with ID {customerId} not found." });

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.CustomerId == customerId)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(orders));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving customer orders.", details = ex.Message });
            }
        }

        // GET: api/orders/recent
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetRecentOrders([FromQuery] int days = 7)
        {
            if (days < 1 || days > 365)
                return BadRequest(new { message = "Days must be between 1 and 365." });

            try
            {
                var fromDate = DateTime.UtcNow.AddDays(-days);

                var orders = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.OrderDate >= fromDate)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(50)
                    .ToListAsync();

                return Ok(_mappingService.MapToDto(orders));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving recent orders.", details = ex.Message });
            }
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(OrderCreateDto orderDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (orderDto.OrderItems == null || !orderDto.OrderItems.Any())
                return BadRequest(new { message = "Order must contain at least one item." });

            try
            {
                // Usar el servicio de mapeo que ya tiene toda la lógica de validación
                var order = await _mappingService.MapToEntityAsync(orderDto);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Recargar la orden con todas las relaciones para el DTO
                var createdOrder = await _context.Orders
                    .Include(o => o.Customer)
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                var createdOrderDto = _mappingService.MapToDto(createdOrder!);
                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, createdOrderDto);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the order.", details = ex.Message });
            }
        }

        // DELETE: api/orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                    return NotFound(new { message = $"Order with ID {id} not found." });

                // Verificar si la orden es reciente (ej: menos de 24 horas)
                if (order.OrderDate < DateTime.UtcNow.AddHours(-24))
                    return BadRequest(new { message = "Cannot delete orders older than 24 hours." });

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the order.", details = ex.Message });
            }
        }

        // GET: api/orders/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetOrderStatistics()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var thirtyDaysAgo = today.AddDays(-30);
                var sevenDaysAgo = today.AddDays(-7);

                var totalOrders = await _context.Orders.CountAsync();
                var totalRevenue = totalOrders > 0 ? await _context.Orders.SumAsync(o => o.TotalAmount) : 0;
                var averageOrderValue = totalOrders > 0 ? await _context.Orders.AverageAsync(o => o.TotalAmount) : 0;

                var ordersToday = await _context.Orders.CountAsync(o => o.OrderDate.Date == today);
                var ordersLast7Days = await _context.Orders.CountAsync(o => o.OrderDate >= sevenDaysAgo);
                var ordersLast30Days = await _context.Orders.CountAsync(o => o.OrderDate >= thirtyDaysAgo);

                var revenueToday = await _context.Orders
                    .Where(o => o.OrderDate.Date == today)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                var revenueLast7Days = await _context.Orders
                    .Where(o => o.OrderDate >= sevenDaysAgo)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                var revenueLast30Days = await _context.Orders
                    .Where(o => o.OrderDate >= thirtyDaysAgo)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                var topCustomer = await _context.Orders
                    .Include(o => o.Customer)
                    .GroupBy(o => new { o.CustomerId, o.Customer!.FirstName, o.Customer.LastName })
                    .Select(g => new {
                        CustomerId = g.Key.CustomerId,
                        CustomerName = $"{g.Key.FirstName} {g.Key.LastName}",
                        TotalOrders = g.Count(),
                        TotalSpent = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalSpent)
                    .FirstOrDefaultAsync();

                var ordersByMonth = await _context.Orders
                    .Where(o => o.OrderDate >= DateTime.UtcNow.AddMonths(-12))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                var statistics = new
                {
                    Overview = new
                    {
                        TotalOrders = totalOrders,
                        TotalRevenue = totalRevenue,
                        AverageOrderValue = averageOrderValue
                    },
                    Today = new
                    {
                        Orders = ordersToday,
                        Revenue = revenueToday
                    },
                    Last7Days = new
                    {
                        Orders = ordersLast7Days,
                        Revenue = revenueLast7Days
                    },
                    Last30Days = new
                    {
                        Orders = ordersLast30Days,
                        Revenue = revenueLast30Days
                    },
                    TopCustomer = topCustomer,
                    MonthlyTrends = ordersByMonth
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", details = ex.Message });
            }
        }

        // GET: api/orders/revenue-by-period
        [HttpGet("revenue-by-period")]
        public async Task<ActionResult<object>> GetRevenueByPeriod(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string period = "day") // day, week, month
        {
            try
            {
                var startDate = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var endDate = toDate ?? DateTime.UtcNow;

                if (startDate > endDate)
                    return BadRequest(new { message = "Start date cannot be greater than end date." });

                var query = _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate);

                object result = period.ToLower() switch
                {
                    "week" => await query
                        .GroupBy(o => new {
                            Year = o.OrderDate.Year,
                            Week = (o.OrderDate.DayOfYear - 1) / 7 + 1
                        })
                        .Select(g => new {
                            Period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                            OrderCount = g.Count(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync(),

                    "month" => await query
                        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                        .Select(g => new {
                            Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                            OrderCount = g.Count(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync(),

                    _ => await query
                        .GroupBy(o => o.OrderDate.Date)
                        .Select(g => new {
                            Period = g.Key.ToString("yyyy-MM-dd"),
                            OrderCount = g.Count(),
                            Revenue = g.Sum(o => o.TotalAmount)
                        })
                        .OrderBy(x => x.Period)
                        .ToListAsync()
                };

                return Ok(new
                {
                    Period = period,
                    DateRange = new { From = startDate, To = endDate },
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving revenue data.", details = ex.Message });
            }
        }

        private async Task<bool> OrderExists(int id)
        {
            return await _context.Orders.AnyAsync(e => e.Id == id);
        }
    }
}
