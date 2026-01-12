using Core.Entities;

namespace Core.Tests;

public class ProductTests
{
    [Fact]
    public void Product_DefaultId_IsZero()
    {
        var product = new Product
        {
            Name = "Test",
            Description = "Test",
            PictureUrl = "test.png",
            Type = "TestType",
            Brand = "TestBrand",
            Price = 9.99m,
            QuantityInStock = 0
        };

        Assert.Equal(0, product.Id);
    }
}
