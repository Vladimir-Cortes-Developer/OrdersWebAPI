using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrdersWebAPI.Data;
using OrdersWebAPI.Models.DTO;
using OrdersWebAPI.Services.Interfaces;

namespace OrdersWebAPI.Controllers
{
    // ===============================================
    // CONTROLADOR DE ORDER ITEMS
    // ===============================================

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class OrderItemsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMappingService _mappingService;

        public OrderItemsController(ECommerceDbContext context, IMappingService mappingService)
        {
            _context = context;
            _mappingService = mappingService;
        }

        // GET: api/orderitems/order/5
        [HttpGet("order/{orderId}")]
        public async Task<ActionResult<IEnumerable<OrderItemDto>>> GetOrderItems(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID." });

            try
            {
                var orderItems = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync();

                if (!orderItems.Any())
                {
                    // Verificar si la orden existe
                    var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
                    if (!orderExists)
                        return NotFound(new { message = $"Order with ID {orderId} not found." });

                    // La orden existe pero no tiene items
                    return Ok(new List<OrderItemDto>());
                }

                var orderItemDtos = _mappingService.MapToDto(orderItems);
                return Ok(orderItemDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving order items.", details = ex.Message });
            }
        }

        // GET: api/orderitems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderItemDto>> GetOrderItem(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == id);

                if (orderItem == null)
                    return NotFound(new { message = $"Order item with ID {id} not found." });

                return Ok(_mappingService.MapToDto(orderItem));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the order item.", details = ex.Message });
            }
        }

        // PUT: api/orderitems/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderItem(int id, [FromBody] OrderItemUpdateDto updateDto)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (updateDto.Quantity <= 0)
                return BadRequest(new { message = "Quantity must be greater than 0." });

            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.Id == id);

                if (orderItem == null)
                    return NotFound(new { message = $"Order item with ID {id} not found." });

                // Verificar si la orden es reciente
                if (orderItem.Order.OrderDate < DateTime.UtcNow.AddHours(-24))
                    return BadRequest(new { message = "Cannot modify items in orders older than 24 hours." });

                var oldTotal = orderItem.UnitPrice * orderItem.Quantity;
                orderItem.Quantity = updateDto.Quantity;
                var newTotal = orderItem.UnitPrice * orderItem.Quantity;

                // Actualizar el total de la orden
                orderItem.Order.TotalAmount += (newTotal - oldTotal);

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await OrderItemExists(id))
                    return NotFound(new { message = $"Order item with ID {id} no longer exists." });
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the order item.", details = ex.Message });
            }
        }

        // DELETE: api/orderitems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderItem(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid order item ID." });

            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .FirstOrDefaultAsync(oi => oi.Id == id);

                if (orderItem == null)
                    return NotFound(new { message = $"Order item with ID {id} not found." });

                // Verificar si la orden es reciente
                if (orderItem.Order.OrderDate < DateTime.UtcNow.AddHours(-24))
                    return BadRequest(new { message = "Cannot modify items in orders older than 24 hours." });

                // Verificar que no sea el único item de la orden
                var itemCount = await _context.OrderItems.CountAsync(oi => oi.OrderId == orderItem.OrderId);
                if (itemCount <= 1)
                    return BadRequest(new { message = "Cannot delete the last item from an order. Delete the entire order instead." });

                // Actualizar el total de la orden
                var itemTotal = orderItem.UnitPrice * orderItem.Quantity;
                orderItem.Order.TotalAmount -= itemTotal;

                _context.OrderItems.Remove(orderItem);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the order item.", details = ex.Message });
            }
        }

        // GET: api/orderitems/product/5/sales
        [HttpGet("product/{productId}/sales")]
        public async Task<ActionResult<object>> GetProductSales(
            int productId,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            try
            {
                var query = _context.OrderItems
                    .Include(oi => oi.Order)
                    .Include(oi => oi.Product)
                    .Where(oi => oi.ProductId == productId);

                if (fromDate.HasValue)
                    query = query.Where(oi => oi.Order.OrderDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(oi => oi.Order.OrderDate <= toDate.Value);

                var orderItems = await query.ToListAsync();

                if (!orderItems.Any())
                {
                    var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
                    if (!productExists)
                        return NotFound(new { message = $"Product with ID {productId} not found." });

                    return Ok(new
                    {
                        ProductId = productId,
                        TotalQuantitySold = 0,
                        TotalRevenue = 0m,
                        OrderCount = 0,
                        AverageQuantityPerOrder = 0,
                        AveragePrice = 0m,
                        DateRange = new
                        {
                            From = fromDate,
                            To = toDate
                        }
                    });
                }

                var stats = new
                {
                    ProductId = productId,
                    ProductName = orderItems.First().Product?.ProductName ?? "Unknown",
                    TotalQuantitySold = orderItems.Sum(oi => oi.Quantity),
                    TotalRevenue = orderItems.Sum(oi => oi.UnitPrice * oi.Quantity),
                    OrderCount = orderItems.Select(oi => oi.OrderId).Distinct().Count(),
                    AverageQuantityPerOrder = orderItems.Average(oi => (double)oi.Quantity),
                    AveragePrice = orderItems.Average(oi => oi.UnitPrice),
                    DateRange = new
                    {
                        From = fromDate ?? orderItems.Min(oi => oi.Order.OrderDate),
                        To = toDate ?? orderItems.Max(oi => oi.Order.OrderDate)
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving product sales.", details = ex.Message });
            }
        }

        // GET: api/orderitems/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetOrderItemsStatistics()
        {
            try
            {
                var totalItems = await _context.OrderItems.CountAsync();
                var totalQuantity = await _context.OrderItems.SumAsync(oi => oi.Quantity);
                var totalRevenue = await _context.OrderItems.SumAsync(oi => oi.UnitPrice * oi.Quantity);

                var averagePrice = totalItems > 0
                    ? await _context.OrderItems.AverageAsync(oi => oi.UnitPrice)
                    : 0;

                var averageQuantity = totalItems > 0
                    ? await _context.OrderItems.AverageAsync(oi => (double)oi.Quantity)
                    : 0;

                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.ProductName })
                    .Select(g => new
                    {
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
                    TotalOrderItems = totalItems,
                    TotalQuantitySold = totalQuantity,
                    TotalRevenue = totalRevenue,
                    AveragePrice = averagePrice,
                    AverageQuantityPerItem = averageQuantity,
                    TopSellingProducts = topProducts
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving statistics.", details = ex.Message });
            }
        }

        private async Task<bool> OrderItemExists(int id)
        {
            return await _context.OrderItems.AnyAsync(e => e.Id == id);
        }
    }
}


