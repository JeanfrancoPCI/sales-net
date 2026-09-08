using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    [Collection("BaseDatosSalesNET")]
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

        [Theory(DisplayName = "GetProductos: debería responder OK")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_Ok(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/productos");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory(DisplayName = "GetProductos filtrado por categoría: debería retornar solo esa categoría")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_FiltradoPorCategoria(string proveedor)
        {
            // Arrange: creamos una categoría y un producto propios, sin depender de datos externos
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client);
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

        [Theory(DisplayName = "GetProductos filtrado por nombre: debería retornar coincidencias")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetProductos_FiltradoPorNombre(string proveedor)
        {
            // Arrange: nombre único e identificable, generado en tiempo de ejecución
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client);
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

        [Fact(DisplayName = "GetProductos con proveedor inválido: debería retornar BadRequest")]
        public async Task GetProductos_ProveedorInvalido()
        {
            var response = await _client.GetAsync("/api/inventado/productos");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "Ciclo completo CRUD: debería crear, actualizar y eliminar producto")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CicloCompletoCrud(string proveedor)
        {
            // Arrange: creamos nuestra propia categoría, sin depender de ningún dato preexistente
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client);

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
