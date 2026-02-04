using System;
using Core.Entities;

namespace Core.Interfaces;

public interface IPaymentService
{
    Task<ShoppingCart?> CreateUpdatePaymentIntent(string cartId);
}
