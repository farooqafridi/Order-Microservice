using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace UserService.IntegrationTests;

public sealed class AuthControllerTests: IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    //[Fact]
    //public async Task Register_ValidRequest_ReturnsCreated()
    //{
    //    //await _factory.InitializeDatabaseAsync();

    //    using var client = _factory.CreateClient();

    //    var request = new
    //    {
    //        Email = $"customer-{Guid.NewGuid():N}@example.com",
    //        Password = "farooqafridi@gmail.cm!",
    //        DisplayName = "Integration Test Customer"
    //    };

    //    var response = await client.PostAsJsonAsync(
    //        "/api/auth/register",
    //        request);

    //    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    //    var body = await response.Content.ReadFromJsonAsync<JsonElement>();

    //    Assert.Equal(
    //        request.Email,
    //        body.GetProperty("email").GetString());

    //    Assert.Equal(
    //        request.DisplayName,
    //        body.GetProperty("displayName").GetString());
    //}


    [Fact]
    public async Task Login_ValidCredentials_ReturnsAccessToken()
    {
        // Arrange
        //await _factory.InitializeDatabaseAsync();

        using var client = _factory.CreateClient();

        //var email = $"login-{Guid.NewGuid():N}@example.com";
        //var password = "TestPassword123!";

        //// Create a real user through the registration endpoint.
        //var registerResponse = await client.PostAsJsonAsync(
        //    "/api/auth/register",
        //    new
        //    {
        //        Email = email,
        //        Password = password,
        //        DisplayName = "Login Test Customer"
        //    });

        //Assert.Equal(
        //    HttpStatusCode.Created,
        //    registerResponse.StatusCode);

        // Act: log in using the registered credentials.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                Email = "farooqafridi@gmail.cm",
                Password = "Agnip@t1983"
            });

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var accessToken = body.GetProperty("accessToken").GetString();

        Assert.False(string.IsNullOrWhiteSpace(accessToken));
        Assert.Equal("Bearer", body.GetProperty("tokenType").GetString());
       // Assert.Equal(email, body.GetProperty("email").GetString());
        //Assert.Equal(
        //    "Login Test Customer",
        //    body.GetProperty("displayName").GetString());

        //Assert.True(body.GetProperty("expiresAtUtc").GetDateTime() > DateTime.UtcNow);

        var roles = body.GetProperty("roles")
            .EnumerateArray()
            .Select(role => role.GetString())
            .ToList();

        Assert.Contains("Customer", roles);
    }

}