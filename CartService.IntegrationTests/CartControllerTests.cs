using CartService.Data;
using CompanyApi.IntegrationTests;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CartService.IntegrationTests;

public sealed class CartControllerTests: IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CartControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetCart_NoExistingCart_ReturnsEmptyCart()
    {
        // Arrange
        await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        // The controller reads customer ID from the authenticated
        // user's ClaimTypes.NameIdentifier claim.
        // We'll configure test authentication before sending this request.

        // Act
        var response = await client.GetAsync("/api/cart");

        // Assert
        Assert.Equal( HttpStatusCode.OK, response.StatusCode);
    }
}