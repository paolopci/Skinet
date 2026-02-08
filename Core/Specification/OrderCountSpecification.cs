using Core.Entities;

namespace Core.Specification;

public class OrderCountSpecification : BaseSpecification<Order>
{
    public OrderCountSpecification(OrderSpecParams specParams, string currentUserId, bool isAdmin)
        : base(order =>
            isAdmin
                ? string.IsNullOrWhiteSpace(specParams.FilterByUserId) || order.UserId == specParams.FilterByUserId
                : order.UserId == currentUserId)
    {
    }
}
