using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    [Collection("BaseDatosSalesNET")]
    public class OrdenesEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public OrdenesEndpointsTests(WebApplicationFactory<Program> factory)
        {
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            _client = appFactory.CreateClient();
        }

        private async Task<int> CrearProductoDePruebaAsync(string proveedor, decimal precio = 20.00m)
        {
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client, proveedor);

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/productos", new ProductoDto
            {
                Nombre = $"ProductoParaOrden_{Guid.NewGuid():N}",
                Precio = precio,
                CategoriaID = categoriaId
            });
            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();

            return resultado.Data;
        }

        [Theory(DisplayName = "GetOrdenes: debería responder OK")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetOrdenes_Ok(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/ordenes");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "GetOrdenes con proveedor inválido: debería retornar BadRequest")]
        public async Task GetOrdenes_ProveedorInvalido()
        {
            var response = await _client.GetAsync("/api/inventado/ordenes");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "GetOrdenPorId inexistente: debería retornar NotFound")]
        public async Task GetOrdenPorId_Inexistente()
        {
            var response = await _client.GetAsync("/api/adonet/ordenes/999999999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory(DisplayName = "CrearOrden: debería crear con el total correcto y aparecer en el listado del cliente")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearOrden_Ok(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var productoId = await CrearProductoDePruebaAsync(proveedor, precio: 25.00m);

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoId, Cantidad = 3 }
                }
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();
            resultado.Data.Should().BeGreaterThan(0);

            var orden = await _client.GetFromJsonAsync<OrdenDto>($"/api/{proveedor}/ordenes/{resultado.Data}");
            orden.Should().NotBeNull();
            orden!.Total.Should().Be(75.00m);

            var ordenesDelCliente = await _client.GetFromJsonAsync<List<OrdenDto>>($"/api/{proveedor}/clientes/{clienteId}/ordenes");
            ordenesDelCliente.Should().Contain(o => o.OrdenID == resultado.Data);
        }

        [Theory(DisplayName = "CrearOrden con cliente inexistente: debería retornar BadRequest")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearOrden_ClienteInexistente(string proveedor)
        {
            var productoId = await CrearProductoDePruebaAsync(proveedor);

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = int.MaxValue,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoId, Cantidad = 1 }
                }
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "CrearOrden sin detalles: debería retornar BadRequest")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearOrden_SinDetalles(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>()
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "ActualizarProductoOrden: debería recalcular el total al cambiar la cantidad")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarProductoOrden_ModificarCantidad(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var productoId = await CrearProductoDePruebaAsync(proveedor, precio: 10.00m);

            var crearResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoId, Cantidad = 1 }
                }
            });
            var crearResultado = await crearResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            var ordenId = crearResultado!.Data;

            var response = await _client.PutAsync(
                $"/api/{proveedor}/ordenes/{ordenId}/productos/{productoId}?cantidad=5", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var orden = await _client.GetFromJsonAsync<OrdenDto>($"/api/{proveedor}/ordenes/{ordenId}");
            orden!.Total.Should().Be(50.00m);
        }

        [Theory(DisplayName = "ActualizarProductoOrden a 0 siendo el único producto: no debería permitirlo")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarProductoOrden_NoPuedeQuitarUltimoProducto(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var productoId = await CrearProductoDePruebaAsync(proveedor);

            var crearResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoId, Cantidad = 1 }
                }
            });
            var crearResultado = await crearResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            var ordenId = crearResultado!.Data;

            var response = await _client.PutAsync(
                $"/api/{proveedor}/ordenes/{ordenId}/productos/{productoId}?cantidad=0", null);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "EliminarOrden: debería marcarla inactiva y dejar de encontrarse por ID")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task EliminarOrden_Ok(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var productoId = await CrearProductoDePruebaAsync(proveedor);

            var crearResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoId, Cantidad = 1 }
                }
            });
            var crearResultado = await crearResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            var ordenId = crearResultado!.Data;

            var response = await _client.DeleteAsync($"/api/{proveedor}/ordenes/{ordenId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var obtenidaResponse = await _client.GetAsync($"/api/{proveedor}/ordenes/{ordenId}");
            obtenidaResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
