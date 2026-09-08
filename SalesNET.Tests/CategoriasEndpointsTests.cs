using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    [Collection("BaseDatosSalesNET")]
    public class CategoriasEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public CategoriasEndpointsTests(WebApplicationFactory<Program> factory)
        {
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            _client = appFactory.CreateClient();
        }

        [Theory(DisplayName = "GetCategorias: debería responder OK")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetCategorias_Ok(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/categorias");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "GetCategorias con proveedor inválido: debería retornar BadRequest")]
        public async Task GetCategorias_ProveedorInvalido()
        {
            var response = await _client.GetAsync("/api/inventado/categorias");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "CrearCategoria: debería crear y aparecer en el listado")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearCategoria_Ok(string proveedor)
        {
            var nombreUnico = $"CategoriaCrear_{Guid.NewGuid():N}";

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/categorias", new CategoriaDto
            {
                Nombre = nombreUnico,
                Descripcion = "Creada por CategoriasEndpointsTests"
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();
            resultado.Data.Should().BeGreaterThan(0);

            var listado = await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/{proveedor}/categorias");
            listado.Should().Contain(c => c.CategoriaID == resultado.Data && c.Nombre == nombreUnico);
        }

        [Theory(DisplayName = "CrearCategoria con nombre repetido: no debería crear una segunda")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearCategoria_NombreDuplicado(string proveedor)
        {
            var categoria = new CategoriaDto
            {
                Nombre = $"CategoriaDuplicada_{Guid.NewGuid():N}",
                Descripcion = "Original"
            };

            var primeraRespuesta = await _client.PostAsJsonAsync($"/api/{proveedor}/categorias", categoria);
            var primerResultado = await primeraRespuesta.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            primerResultado!.Exito.Should().BeTrue();

            var segundaRespuesta = await _client.PostAsJsonAsync($"/api/{proveedor}/categorias", categoria);
            var segundoResultado = await segundaRespuesta.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            segundoResultado.Should().NotBeNull();
            segundoResultado!.Exito.Should().BeFalse();
            segundoResultado.Data.Should().Be(primerResultado.Data);
        }

        [Theory(DisplayName = "ActualizarCategoria: debería modificar nombre y descripción")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarCategoria_Ok(string proveedor)
        {
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client, proveedor);
            var nombreActualizado = $"CategoriaActualizada_{Guid.NewGuid():N}";

            var response = await _client.PutAsJsonAsync($"/api/{proveedor}/categorias", new CategoriaDto
            {
                CategoriaID = categoriaId,
                Nombre = nombreActualizado,
                Descripcion = "Descripción actualizada"
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var listado = await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/{proveedor}/categorias");
            listado.Should().Contain(c => c.CategoriaID == categoriaId && c.Nombre == nombreActualizado);
        }

        [Theory(DisplayName = "ActualizarCategoria inexistente: debería retornar BadRequest")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarCategoria_Inexistente(string proveedor)
        {
            var response = await _client.PutAsJsonAsync($"/api/{proveedor}/categorias", new CategoriaDto
            {
                CategoriaID = int.MaxValue,
                Nombre = $"NoExiste_{Guid.NewGuid():N}",
                Descripcion = "N/A"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "EliminarCategoria sin productos asociados: debería eliminarla del listado")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task EliminarCategoria_SinProductos(string proveedor)
        {
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client, proveedor);

            var response = await _client.DeleteAsync($"/api/{proveedor}/categorias/{categoriaId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var listado = await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/{proveedor}/categorias");
            listado.Should().NotContain(c => c.CategoriaID == categoriaId);
        }

        [Theory(DisplayName = "EliminarCategoria con productos asociados: no debería eliminarla")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task EliminarCategoria_ConProductos(string proveedor)
        {
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client, proveedor);

            await _client.PostAsJsonAsync($"/api/{proveedor}/productos", new ProductoDto
            {
                Nombre = $"ProductoAsociado_{Guid.NewGuid():N}",
                Precio = 10.00m,
                CategoriaID = categoriaId
            });

            var response = await _client.DeleteAsync($"/api/{proveedor}/categorias/{categoriaId}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var listado = await _client.GetFromJsonAsync<List<CategoriaDto>>($"/api/{proveedor}/categorias");
            listado.Should().Contain(c => c.CategoriaID == categoriaId);
        }
    }
}
