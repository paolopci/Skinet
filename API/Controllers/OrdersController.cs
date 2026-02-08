using API.DTOs;
using Core.Entities;
using Core.Interfaces;
using Core.Specification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace API.Controllers;

[Authorize]
public class OrdersController(
    IGenericRepository<Order> orderRepository,
    UserManager<AppUser> userManager) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OrderListItemDto>>> GetOrders([FromQuery] OrderQueryParamsDto query)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var isAdmin = User.IsInRole("Admin");
            var specParams = new OrderSpecParams
            {
                SortBy = query.SortBy,
                Order = query.Order,
                FilterByUserId = isAdmin ? query.FilterByUserId : currentUserId
            };

            if (!OrderSpecification.IsSupportedSortBy(specParams.SortBy) ||
                !OrderSpecification.IsSupportedOrder(specParams.Order))
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            var spec = new OrderSpecification(specParams, currentUserId, isAdmin);
            var countSpec = new OrderCountSpecification(specParams, currentUserId, isAdmin);

            var orders = await orderRepository.ListAsync(spec);
            var totalCount = await orderRepository.CountAsync(countSpec);
            Response.Headers.Append("X-Total-Count", totalCount.ToString());

            return Ok(orders.Select(MapToListItem).ToList());
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred processing your request" });
        }
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDetailsResponseDto>> GetOrderById(int orderId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var existingOrder = await orderRepository.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !string.Equals(existingOrder.UserId, currentUserId, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var spec = new OrderSpecification(orderId, currentUserId, true);
            var order = await orderRepository.GetEntityWithSpec(spec);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            return Ok(MapToDetails(order));
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred processing your request" });
        }
    }

    [HttpPost]
    public async Task<ActionResult<OrderDetailsResponseDto>> CreateOrder([FromBody] CreateOrderRequestDto? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var isAdmin = User.IsInRole("Admin");
            var targetUserId = isAdmin && !string.IsNullOrWhiteSpace(request.UserId)
                ? request.UserId.Trim()
                : currentUserId;

            if (!await userManager.Users.AnyAsync(user => user.Id == targetUserId))
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            if (!TryValidateOrderRequest(
                    request.TipoPagamento,
                    request.NumeroCarta,
                    request.StatoOrdine,
                    request.TotaleOrdine,
                    request.Dettagli,
                    out var maskedCardNumber))
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            var order = new Order
            {
                UserId = targetUserId,
                OrderDate = request.DataOrdine ?? DateTime.UtcNow,
                PaymentType = request.TipoPagamento.Trim().ToLowerInvariant(),
                CardNumberMasked = maskedCardNumber,
                OrderTotal = request.TotaleOrdine,
                OrderStatus = string.IsNullOrWhiteSpace(request.StatoOrdine) ? "completato" : request.StatoOrdine.Trim().ToLowerInvariant(),
                Details = request.Dettagli.Select(MapToEntity).ToList()
            };

            orderRepository.Add(order);
            var saved = await orderRepository.SaveAllAsync();
            if (!saved)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred processing your request" });
            }

            var response = MapToDetails(order);
            return CreatedAtAction(nameof(GetOrderById), new { orderId = order.Id }, response);
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred processing your request" });
        }
    }

    [HttpPut("{orderId:int}")]
    public async Task<ActionResult<OrderDetailsResponseDto>> UpdateOrder(int orderId, [FromBody] UpdateOrderRequestDto? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var existingOrder = await orderRepository.GetByIdAsync(orderId);
            if (existingOrder == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !string.Equals(existingOrder.UserId, currentUserId, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var targetUserId = isAdmin && !string.IsNullOrWhiteSpace(request.UserId)
                ? request.UserId.Trim()
                : existingOrder.UserId;

            if (!await userManager.Users.AnyAsync(user => user.Id == targetUserId))
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            if (!TryValidateOrderRequest(
                    request.TipoPagamento,
                    request.NumeroCarta,
                    request.StatoOrdine,
                    request.TotaleOrdine,
                    request.Dettagli,
                    out var maskedCardNumber))
            {
                return BadRequest(new { error = "Invalid order data" });
            }

            var spec = new OrderSpecification(orderId, currentUserId, true);
            var order = await orderRepository.GetEntityWithSpec(spec);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            order.UserId = targetUserId;
            order.OrderDate = request.DataOrdine ?? order.OrderDate;
            order.PaymentType = request.TipoPagamento.Trim().ToLowerInvariant();
            order.CardNumberMasked = maskedCardNumber;
            order.OrderTotal = request.TotaleOrdine;
            order.OrderStatus = string.IsNullOrWhiteSpace(request.StatoOrdine) ? order.OrderStatus : request.StatoOrdine.Trim().ToLowerInvariant();

            order.Details.Clear();
            foreach (var detail in request.Dettagli)
            {
                order.Details.Add(MapToEntity(detail));
            }

            var saved = await orderRepository.SaveAllAsync();
            if (!saved)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred processing your request" });
            }

            return Ok(MapToDetails(order));
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred processing your request" });
        }
    }

    [HttpDelete("{orderId:int}")]
    public async Task<IActionResult> DeleteOrder(int orderId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            var order = await orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return NotFound(new { error = "Order not found" });
            }

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && !string.Equals(order.UserId, currentUserId, StringComparison.Ordinal))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error = "Insufficient permissions" });
            }

            orderRepository.Remove(order);
            var saved = await orderRepository.SaveAllAsync();
            if (!saved)
            {
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { error = "An error occurred processing your request" });
            }

            return NoContent();
        }
        catch
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new { error = "An error occurred processing your request" });
        }
    }

    private static bool TryValidateOrderRequest(
        string paymentType,
        string? cardNumber,
        string orderStatus,
        decimal orderTotal,
        List<OrderDetailDto>? details,
        out string? maskedCardNumber)
    {
        maskedCardNumber = null;

        if (string.IsNullOrWhiteSpace(paymentType) ||
            string.IsNullOrWhiteSpace(orderStatus) ||
            orderTotal < 0m ||
            details is null ||
            details.Count == 0)
        {
            return false;
        }

        if (!details.All(detail => detail.Quantita > 0 && detail.PrezzoUnitario >= 0m))
        {
            return false;
        }

        return TryMaskCardNumber(paymentType, cardNumber, out maskedCardNumber);
    }

    private static bool TryMaskCardNumber(string paymentType, string? cardNumber, out string? maskedCardNumber)
    {
        maskedCardNumber = null;
        var normalizedPaymentType = paymentType.Trim().ToLowerInvariant();
        if (normalizedPaymentType != "carta")
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return false;
        }

        var trimmedCard = cardNumber.Trim();
        var fourDigitsMatch = Regex.Match(trimmedCard, @"^\d{4}$");
        if (fourDigitsMatch.Success)
        {
            maskedCardNumber = $"**** {trimmedCard}";
            return true;
        }

        var maskedMatch = Regex.Match(trimmedCard, @"^\*{4}\s?(\d{4})$");
        if (!maskedMatch.Success)
        {
            return false;
        }

        maskedCardNumber = $"**** {maskedMatch.Groups[1].Value}";
        return true;
    }

    private static OrderDetail MapToEntity(OrderDetailDto dto)
    {
        return new OrderDetail
        {
            ProductId = dto.ProdottoId,
            Quantity = dto.Quantita,
            UnitPrice = dto.PrezzoUnitario
        };
    }

    private static OrderListItemDto MapToListItem(Order order)
    {
        return new OrderListItemDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            DataOrdine = order.OrderDate,
            TipoPagamento = order.PaymentType,
            NumeroCarta = order.CardNumberMasked,
            TotaleOrdine = order.OrderTotal,
            StatoOrdine = order.OrderStatus
        };
    }

    private static OrderDetailsResponseDto MapToDetails(Order order)
    {
        return new OrderDetailsResponseDto
        {
            OrderId = order.Id,
            UserId = order.UserId,
            DataOrdine = order.OrderDate,
            TipoPagamento = order.PaymentType,
            NumeroCarta = order.CardNumberMasked,
            TotaleOrdine = order.OrderTotal,
            StatoOrdine = order.OrderStatus,
            Dettagli = order.Details.Select(detail => new OrderDetailDto
            {
                DettaglioId = detail.Id,
                ProdottoId = detail.ProductId,
                NomeProdotto = string.IsNullOrWhiteSpace(detail.Product?.Name)
                    ? $"Prodotto #{detail.ProductId}"
                    : detail.Product.Name,
                ImmagineUrl = detail.Product?.PictureUrl,
                Quantita = detail.Quantity,
                PrezzoUnitario = detail.UnitPrice
            }).ToList()
        };
    }
}
