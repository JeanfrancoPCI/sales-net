using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    public class ProductosCrudEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ProductosCrudEndpointsTests(WebApplicationFactory<Program> factory)
        {
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            _client = appFactory.CreateClient();
        }

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
        public async Task CicloCompletoCrud_DeberiaCrearActualizarYEliminarProducto(string proveedor)
        {
            // Arrange: creamos nuestra propia categoría, sin depender de ningún dato preexistente
            var categoriaId = await CrearCategoriaDePruebaAsync();

            var nuevoProducto = new ProductoDto
            {
                Nombre = $"Producto de Prueba {proveedor} {Guid.NewGuid():N}",
                Precio = 123.45m,
                CategoriaID = categoriaId
            };

            // Act 1: Crear
            var crearResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/productos", nuevoProducto);
            crearResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var resultadoCrear = await crearResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            resultadoCrear.Should().NotBeNull();
            resultadoCrear!.Exito.Should().BeTrue();
            var nuevoId = resultadoCrear.Data;
            nuevoId.Should().BeGreaterThan(0);

            // Assert: el producto creado aparece en el listado
            var listaDespuesDeCrear = await _client.GetFromJsonAsync<List<ProductoDto>>($"/api/{proveedor}/productos");
            listaDespuesDeCrear.Should().Contain(p => p.ProductoID == nuevoId && p.Nombre == nuevoProducto.Nombre);

            // Act 2: Actualizar precio
            var productoActualizado = new ProductoDto
            {
                ProductoID = nuevoId,
                Nombre = nuevoProducto.Nombre,
                Precio = 199.99m,
                CategoriaID = categoriaId
            };

            var actualizarResponse = await _client.PutAsJsonAsync($"/api/{proveedor}/productos", productoActualizado);
            actualizarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var listaDespuesDeActualizar = await _client.GetFromJsonAsync<List<ProductoDto>>($"/api/{proveedor}/productos");
            listaDespuesDeActualizar.Should().Contain(p => p.ProductoID == nuevoId && p.Precio == 199.99m);

            // Act 3: Eliminar (soft delete)
            var eliminarResponse = await _client.DeleteAsync($"/api/{proveedor}/productos/{nuevoId}");
            eliminarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Assert: ya no aparece en el listado porque el listado filtra Activo = 1
            var listaDespuesDeEliminar = await _client.GetFromJsonAsync<List<ProductoDto>>($"/api/{proveedor}/productos");
            listaDespuesDeEliminar.Should().NotContain(p => p.ProductoID == nuevoId);
        }
    }
}
