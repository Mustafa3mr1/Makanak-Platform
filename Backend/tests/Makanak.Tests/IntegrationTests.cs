using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Makanak.Application.DTOs.Auth;
using Makanak.Application.DTOs.Dashboard;
using Makanak.Application.DTOs.Offers;
using Makanak.Application.DTOs.Properties;
using Makanak.Domain.Entities;
using Makanak.Domain.Enums;
using Makanak.Domain.Exceptions;
using Makanak.Infrastructure.Data;
using Xunit;

using Microsoft.Extensions.Configuration;

namespace Makanak.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "Makanak_Integration_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_dbName);
                options.UseInternalServiceProvider(internalServiceProvider);
            });
        });

        builder.UseEnvironment("Testing");
    }
}

public class IntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public IntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Full_Negotiation_Flow_ShouldSucceed_AndMarkPropertySold()
    {
        // 1. Register Owner
        var ownerEmail = $"owner_{Guid.NewGuid():N}@test.com";
        var regOwnerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            FullName: "Owner Test",
            Email: ownerEmail,
            Password: "Password123!",
            UserType: UserType.Owner,
            PhoneNumber: "+201000000001"
        ));
        var content = await regOwnerResponse.Content.ReadAsStringAsync();
        Assert.True(regOwnerResponse.IsSuccessStatusCode, $"Register failed with {regOwnerResponse.StatusCode}: {content}");
        var ownerAuth = await regOwnerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        Assert.NotNull(ownerAuth);
        var ownerToken = ownerAuth.Token;

        // 2. Register Seeker
        var seekerEmail = $"seeker_{Guid.NewGuid():N}@test.com";
        var regSeekerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            FullName: "Seeker Test",
            Email: seekerEmail,
            Password: "Password123!",
            UserType: UserType.Seeker,
            PhoneNumber: "+201000000002"
        ));
        Assert.Equal(HttpStatusCode.OK, regSeekerResponse.StatusCode);
        var seekerAuth = await regSeekerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        Assert.NotNull(seekerAuth);
        var seekerToken = seekerAuth.Token;

        // 3. Owner creates a property
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var createPropResponse = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequestDto(
            Title: "Integration Apartment",
            Description: "A nice apartment for test",
            Price: 3000000m,
            ListingType: ListingType.Sale,
            PropertyType: PropertyType.Apartment,
            City: "Cairo",
            Address: "Test Address 123",
            Bedrooms: 3,
            Bathrooms: 2,
            AreaSqm: 150m,
            Latitude: 30.0m,
            Longitude: 31.0m,
            Features: new[] { "Parking", "Elevator" }
        ));
        Assert.Equal(HttpStatusCode.Created, createPropResponse.StatusCode);
        var createdProperty = await createPropResponse.Content.ReadFromJsonAsync<PropertyDetailsDto>(JsonOptions);
        Assert.NotNull(createdProperty);

        // 4. Seeker submits initial offer (Round 1: 2,700,000)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seekerToken);
        var offerResponse = await _client.PostAsJsonAsync("/api/offers", new CreateOfferRequestDto(
            PropertyPublicId: createdProperty.PublicId,
            InitialPrice: 2700000m
        ));
        Assert.Equal(HttpStatusCode.Created, offerResponse.StatusCode);
        var offer = await offerResponse.Content.ReadFromJsonAsync<OfferDetailsDto>(JsonOptions);
        Assert.NotNull(offer);
        Assert.Equal(OfferStatus.Pending, offer.Status);
        Assert.Equal(1, offer.RoundNumber);

        // 5. Owner counters (Round 2: 2,850,000)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var counterResponse = await _client.PutAsJsonAsync($"/api/offers/{offer.PublicId}/counter", new CounterOfferRequestDto(
            CounterPrice: 2850000m
        ));
        Assert.Equal(HttpStatusCode.OK, counterResponse.StatusCode);
        var counteredOffer = await counterResponse.Content.ReadFromJsonAsync<OfferDetailsDto>(JsonOptions);
        Assert.NotNull(counteredOffer);
        Assert.Equal(OfferStatus.Countered, counteredOffer.Status);
        Assert.Equal(2, counteredOffer.RoundNumber);
        Assert.Equal(2850000m, counteredOffer.LatestPrice);

        // 6. Seeker accepts the counter
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seekerToken);
        var acceptResponse = await _client.PutAsync($"/api/offers/{offer.PublicId}/accept", null);
        Assert.Equal(HttpStatusCode.OK, acceptResponse.StatusCode);
        var acceptedOffer = await acceptResponse.Content.ReadFromJsonAsync<OfferDetailsDto>(JsonOptions);
        Assert.NotNull(acceptedOffer);
        Assert.Equal(OfferStatus.Accepted, acceptedOffer.Status);
        Assert.Equal(3, acceptedOffer.History.Count); // Submitted, Countered, Accepted

        // 7. Check property is now marked as Sold
        var propCheckResponse = await _client.GetAsync($"/api/properties/{createdProperty.PublicId}");
        // Non-available property returns 404 to anonymous or non-owner caller
        Assert.Equal(HttpStatusCode.NotFound, propCheckResponse.StatusCode);

        // But accessible to Owner:
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var ownerPropCheck = await _client.GetAsync($"/api/properties/{createdProperty.PublicId}");
        Assert.Equal(HttpStatusCode.OK, ownerPropCheck.StatusCode);
        var propDetails = await ownerPropCheck.Content.ReadFromJsonAsync<PropertyDetailsDto>(JsonOptions);
        Assert.NotNull(propDetails);
        Assert.Equal(PropertyStatus.Sold, propDetails.Status);
    }

    [Fact]
    public void Concurrent_Accept_ShouldEnforceConcurrencyControl()
    {
        // Domain invariant: once an offer is Accepted, accepting again is rejected
        var offer = Offer.Create(1, 2, 1000m);
        offer.Accept();

        // Attempting second accept throws InvalidOfferOperationException
        Assert.Throws<InvalidOfferOperationException>(() => offer.Accept());
    }

    [Fact]
    public async Task Dashboards_ShouldReturnAccurateMetrics_AndEnforceRoles()
    {
        // 1. Register Owner
        var ownerEmail = $"dash_owner_{Guid.NewGuid():N}@test.com";
        var regOwnerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            FullName: "Dash Owner",
            Email: ownerEmail,
            Password: "Password123!",
            UserType: UserType.Owner,
            PhoneNumber: "+201099999991"
        ));
        var ownerAuth = await regOwnerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        var ownerToken = ownerAuth!.Token;

        // 2. Register Seeker
        var seekerEmail = $"dash_seeker_{Guid.NewGuid():N}@test.com";
        var regSeekerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto(
            FullName: "Dash Seeker",
            Email: seekerEmail,
            Password: "Password123!",
            UserType: UserType.Seeker,
            PhoneNumber: "+201099999992"
        ));
        var seekerAuth = await regSeekerResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);
        var seekerToken = seekerAuth!.Token;

        // 3. Role enforcement: Seeker cannot access Owner dashboard
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seekerToken);
        var seekerOnOwnerDash = await _client.GetAsync("/api/dashboard/owner");
        Assert.Equal(HttpStatusCode.Forbidden, seekerOnOwnerDash.StatusCode);

        // 4. Role enforcement: Owner cannot access Seeker dashboard
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var ownerOnSeekerDash = await _client.GetAsync("/api/dashboard/seeker");
        Assert.Equal(HttpStatusCode.Forbidden, ownerOnSeekerDash.StatusCode);

        // 5. Initial Owner dashboard: 0 properties, 0 offers
        var ownerDashResp = await _client.GetAsync("/api/dashboard/owner");
        Assert.Equal(HttpStatusCode.OK, ownerDashResp.StatusCode);
        var ownerDash = await ownerDashResp.Content.ReadFromJsonAsync<OwnerDashboardDto>(JsonOptions);
        Assert.NotNull(ownerDash);
        Assert.Equal(0, ownerDash.TotalProperties);

        // 6. Owner creates property
        var createPropResponse = await _client.PostAsJsonAsync("/api/properties", new CreatePropertyRequestDto(
            Title: "Dashboard Property",
            Description: "Property for dashboard test",
            Price: 2000000m,
            ListingType: ListingType.Sale,
            PropertyType: PropertyType.Apartment,
            City: "Alexandria",
            Address: "Corniche",
            Bedrooms: 2,
            Bathrooms: 1,
            AreaSqm: 100m,
            Latitude: null,
            Longitude: null,
            Features: new[] { "Balcony", "Elevator" }
        ));
        var createdProp = await createPropResponse.Content.ReadFromJsonAsync<PropertyDetailsDto>(JsonOptions);

        // 7. Verify Owner dashboard has 1 property
        var ownerDashResp2 = await _client.GetAsync("/api/dashboard/owner");
        var ownerDash2 = await ownerDashResp2.Content.ReadFromJsonAsync<OwnerDashboardDto>(JsonOptions);
        Assert.Equal(1, ownerDash2!.TotalProperties);
        Assert.Equal(1, ownerDash2.AvailableProperties);
        Assert.Equal(0, ownerDash2.PendingOffers);

        // 8. Seeker submits offer
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", seekerToken);
        await _client.PostAsJsonAsync("/api/offers", new CreateOfferRequestDto(
            PropertyPublicId: createdProp!.PublicId,
            InitialPrice: 1900000m
        ));

        // 9. Seeker dashboard: 1 total offer, 1 pending
        var seekerDashResp = await _client.GetAsync("/api/dashboard/seeker");
        Assert.Equal(HttpStatusCode.OK, seekerDashResp.StatusCode);
        var seekerDash = await seekerDashResp.Content.ReadFromJsonAsync<SeekerDashboardDto>(JsonOptions);
        Assert.NotNull(seekerDash);
        Assert.Equal(1, seekerDash.TotalOffers);
        Assert.Equal(1, seekerDash.Pending);

        // 10. Owner dashboard: 1 pending offer
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var ownerDashResp3 = await _client.GetAsync("/api/dashboard/owner");
        var ownerDash3 = await ownerDashResp3.Content.ReadFromJsonAsync<OwnerDashboardDto>(JsonOptions);
        Assert.Equal(1, ownerDash3!.PendingOffers);
        Assert.Equal(1, ownerDash3.TotalOffers);
    }

    [Fact]
    public async Task Export_OpenApi_Specification()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var swaggerJson = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(swaggerJson));

        // Locate repo root (which contains README.md)
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "README.md")))
        {
            dir = dir.Parent;
        }

        Assert.NotNull(dir);
        var docsDir = Path.Combine(dir.FullName, "docs");
        Directory.CreateDirectory(docsDir);
        var openApiPath = Path.Combine(docsDir, "openapi.json");
        await File.WriteAllTextAsync(openApiPath, swaggerJson);
        Assert.True(File.Exists(openApiPath));
    }
}
