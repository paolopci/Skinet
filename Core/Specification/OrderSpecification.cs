using Core.Entities;

namespace Core.Specification;

public class OrderSpecification : BaseSpecification<Order>
{
    public OrderSpecification(OrderSpecParams specParams, string currentUserId, bool isAdmin)
        : base(order =>
            isAdmin
                ? string.IsNullOrWhiteSpace(specParams.FilterByUserId) || order.UserId == specParams.FilterByUserId
                : order.UserId == currentUserId)
    {
        ApplySorting(specParams.SortBy, specParams.Order);
    }

    public OrderSpecification(int orderId, string currentUserId, bool isAdmin)
        : base(order => order.Id == orderId && (isAdmin || order.UserId == currentUserId))
    {
        AddInclude(order => order.Details);
        AddInclude("Details.Product");
    }

    public static bool IsSupportedSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return true;
        }

        var normalizedSortBy = sortBy.Trim().ToLowerInvariant();
        return normalizedSortBy is "dataordine" or "orderid" or "tipopagamento" or "userid";
    }

    public static bool IsSupportedOrder(string? order)
    {
        if (string.IsNullOrWhiteSpace(order))
        {
            return true;
        }

        var normalizedOrder = order.Trim().ToLowerInvariant();
        return normalizedOrder is "asc" or "desc";
    }

    private void ApplySorting(string? sortBy, string? order)
    {
        var normalizedSortBy = sortBy?.Trim().ToLowerInvariant();
        var isDesc = string.Equals(order?.Trim(), "desc", StringComparison.OrdinalIgnoreCase);
        var isAsc = string.Equals(order?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);

        if (!isDesc && !isAsc)
        {
            isDesc = true;
        }

        switch (normalizedSortBy)
        {
            case "orderid":
                if (isDesc)
                {
                    AddOrderByDescending(order => order.Id);
                }
                else
                {
                    AddOrderBy(order => order.Id);
                }
                break;
            case "tipopagamento":
                if (isDesc)
                {
                    AddOrderByDescending(order => order.PaymentType);
                }
                else
                {
                    AddOrderBy(order => order.PaymentType);
                }
                break;
            case "userid":
                if (isDesc)
                {
                    AddOrderByDescending(order => order.UserId);
                }
                else
                {
                    AddOrderBy(order => order.UserId);
                }
                break;
            case "dataordine":
            default:
                if (isDesc)
                {
                    AddOrderByDescending(order => order.OrderDate);
                }
                else
                {
                    AddOrderBy(order => order.OrderDate);
                }
                break;
        }
    }
}
