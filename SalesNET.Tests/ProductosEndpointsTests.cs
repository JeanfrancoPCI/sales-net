using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    public class ProductosEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProductosEndpointsTests(WebApplicationFactory<Program> factory)
        {
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            _client = appFactory.CreateClient();
        }

        // Ya no dependemos de que exista ninguna categoría previa: la creamos nosotros mismos
        // vía la API. Los tests quedan 100% autosuficientes, sin importar si se corrió o no
        // el script de semilla.
        private async Task<int> CrearCategoriaDePruebaAsync()
        {
            var categoria = new CategoriaDto
            {
                Nombre = $"CategoriaPrueba_{Guid.NewGuid():N}",
                Descripcion = "Categoría creada por los tests de integración"
            };

            var response = await _client.PostAsJsonAsync("/api/adonet/categorias", categoria);
            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();

            return resultado.Data;
        }

        [Theory]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_DeberiaResponderOk(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/productos");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_FiltradoPorCategoria_DeberiaRetornarSoloEsaCategoria(string proveedor)
        {
            // Arrange: creamos una categoría y un producto propios, sin depender de datos externos
            var categoriaId = await CrearCategoriaDePruebaAsync();
            var nombreUnico = $"ProdFiltroCategoria_{Guid.NewGuid():N}";

            var crearResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/productos", new ProductoDto
            {
                Nombre = nombreUnico,
                Precio = 10.00m,
                CategoriaID = categoriaId
            });
            var resultadoCrear = await crearResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            // Act
            var filtrados = await _client.GetFromJsonAsync<List<ProductoDto>>(
                $"/api/{proveedor}/productos?categoriaId={categoriaId}");

            // Assert: como la categoría es nueva, el único producto que puede tener es el nuestro
            filtrados.Should().NotBeNull();
            filtrados!.Should().HaveCount(1);
            filtrados.Should().ContainSingle(p => p.ProductoID == resultadoCrear!.Data && p.Nombre == nombreUnico);
        }

        [Theory]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_FiltradoPorNombre_DeberiaRetornarCoincidencias(string proveedor)
        {
            // Arrange: nombre único e identificable, generado en tiempo de ejecución
            var categoriaId = await CrearCategoriaDePruebaAsync();
            var claveUnica = $"ZZFILTRO{Guid.NewGuid():N}";
            var nombreUnico = $"Producto {claveUnica}";

            await _client.PostAsJsonAsync($"/api/{proveedor}/productos", new ProductoDto
            {
                Nombre = nombreUnico,
                Precio = 10.00m,
                CategoriaID = categoriaId
            });

            // Act
            var filtrados = await _client.GetFromJsonAsync<List<ProductoDto>>(
                $"/api/{proveedor}/productos?nombre={claveUnica}");

            // Assert
            filtrados.Should().NotBeNullOrEmpty();
            filtrados!.Should().OnlyContain(p => p.Nombre.Contains(claveUnica));
            filtrados.Should().ContainSingle(p => p.Nombre == nombreUnico);
        }

        [Fact]
        public async Task GetProductos_ConProveedorInvalido_DeberiaRetornarBadRequest()
        {
            var response = await _client.GetAsync("/api/inventado/productos");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
