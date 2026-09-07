using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SalesNET.Tests
{
    public class GlobalExceptionHandlerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public GlobalExceptionHandlerTests(WebApplicationFactory<Program> factory)
        {
            // A diferencia de los demás tests, acá usamos una fábrica independiente que
            // apunta deliberadamente a una base de datos inalcanzable (puerto inexistente),
            // para forzar una SqlException real y verificar que el GlobalExceptionHandler
            // la traduce a un 503 con formato ProblemDetails, en vez de dejar que se
            // propague como un 500 sin control. Connect Timeout corto para que la prueba
            // falle rápido en vez de esperar el timeout por defecto (15s).
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Server=localhost,59999;Database=SalesBD;User Id=sa;Password=x;TrustServerCertificate=True;Connect Timeout=2;"
                    });
                });
            });

            _client = appFactory.CreateClient();
        }

        [Theory]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ConBaseDeDatosInalcanzable_DeberiaResponder503(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/productos");

            response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task ConBaseDeDatosInalcanzable_DeberiaResponderConFormatoProblemDetails()
        {
            var response = await _client.GetAsync("/api/adonet/productos");

            var problema = await response.Content.ReadFromJsonAsync<ProblemDetailsRespuesta>();

            problema.Should().NotBeNull();
            problema!.Status.Should().Be((int)HttpStatusCode.ServiceUnavailable);
            problema.Title.Should().Be("Error de base de datos");
            problema.Detail.Should().NotBeNullOrWhiteSpace();
        }

        // Réplica mínima de los campos de ProblemDetails que nos interesa verificar,
        // para no depender de detalles de deserialización del tipo real de ASP.NET Core.
        private class ProblemDetailsRespuesta
        {
            public int Status { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Detail { get; set; } = string.Empty;
        }
    }
}
