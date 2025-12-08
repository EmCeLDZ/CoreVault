using System.Net;
using System.Net.Http.Json;
using CoreKV;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CoreKV.Tests;

[Trait("Category", "Integration")]
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Tworzymy wirtualnego klienta HTTP (jak przeglądarka/Postman)
        _client = factory.CreateClient();
        
        // Dodajemy klucz API do nagłówków
        _client.DefaultRequestHeaders.Add("X-Api-Key", "test-key-for-ci");
    }

    [Fact]
    public async Task Get_ShouldReturnNotFound_WhenKeyDoesNotExist()
    {
        // Act (Działanie): Pytamy o klucz, którego nie ma
        var response = await _client.GetAsync("/api/keyvalue/nieistnieje");

        // Assert (Sprawdzenie): Powinniśmy dostać 404 Not Found
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_And_Get_ShouldReturnSavedValue()
    {
        // Arrange: Dane testowe
        var key = "moj-klucz-testowy";
        var value = "tajna-wartosc";
        
        // Act 1: Zapisz wartość (POST)
        // Zakładam, że Twoje API przyjmuje JSON. Jeśli przyjmuje string w body, zmień to.
        var postResponse = await _client.PostAsJsonAsync("/api/keyvalue", new { Key = key, Value = value });
        
        // Assert 1: Zapis powinien się udać (200 OK lub 201 Created)
        postResponse.EnsureSuccessStatusCode();

        // Act 2: Odczytaj wartość (GET)
        var getResponse = await _client.GetAsync($"/api/keyvalue/{key}");
        
        // Assert 2: Sprawdź czy dostaliśmy to, co zapisaliśmy
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await getResponse.Content.ReadAsStringAsync();
        content.Should().Contain(value);
    }
}
