using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using SalesNET.Domain.DTOs;

namespace SalesNET.Tests
{
    [Collection("BaseDatosSalesNET")]
    public class ClientesEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ClientesEndpointsTests(WebApplicationFactory<Program> factory)
        {
            var appFactory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

            _client = appFactory.CreateClient();
        }

        [Theory(DisplayName = "GetClientes: debería responder OK")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task GetClientes_Ok(string proveedor)
        {
            var response = await _client.GetAsync($"/api/{proveedor}/clientes");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "GetClientes con proveedor inválido: debería retornar BadRequest")]
        public async Task GetClientes_ProveedorInvalido()
        {
            var response = await _client.GetAsync("/api/inventado/clientes");

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "GetClientePorId inexistente: debería retornar NotFound")]
        public async Task GetClientePorId_Inexistente()
        {
            var response = await _client.GetAsync("/api/adonet/clientes/999999999");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory(DisplayName = "CrearCliente: debería crear y aparecer en el listado")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearCliente_Ok(string proveedor)
        {
            var cliente = new ClienteDto
            {
                Nombre = $"ClienteCrear_{Guid.NewGuid():N}",
                Email = $"{Guid.NewGuid():N}@pruebas.com",
                Telefono = "111222333"
            };

            var response = await _client.PostAsJsonAsync($"/api/{proveedor}/clientes", cliente);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();
            resultado.Data.Should().BeGreaterThan(0);

            var obtenido = await _client.GetFromJsonAsync<ClienteDto>($"/api/{proveedor}/clientes/{resultado.Data}");
            obtenido.Should().NotBeNull();
            obtenido!.Email.Should().Be(cliente.Email);
        }

        [Theory(DisplayName = "CrearCliente con email repetido: no debería crear uno nuevo")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task CrearCliente_EmailDuplicado(string proveedor)
        {
            var cliente = new ClienteDto
            {
                Nombre = $"ClienteDuplicado_{Guid.NewGuid():N}",
                Email = $"{Guid.NewGuid():N}@pruebas.com",
                Telefono = "111222333"
            };

            var primeraRespuesta = await _client.PostAsJsonAsync($"/api/{proveedor}/clientes", cliente);
            var primerResultado = await primeraRespuesta.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            primerResultado!.Exito.Should().BeTrue();

            var otroCliente = new ClienteDto
            {
                Nombre = $"OtroNombre_{Guid.NewGuid():N}",
                Email = cliente.Email,
                Telefono = "444555666"
            };
            var segundaRespuesta = await _client.PostAsJsonAsync($"/api/{proveedor}/clientes", otroCliente);
            var segundoResultado = await segundaRespuesta.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            segundoResultado.Should().NotBeNull();
            segundoResultado!.Exito.Should().BeFalse();
        }

        [Theory(DisplayName = "ActualizarCliente: debería modificar nombre y teléfono")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarCliente_Ok(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var nombreActualizado = $"ClienteActualizado_{Guid.NewGuid():N}";

            var response = await _client.PutAsJsonAsync($"/api/{proveedor}/clientes", new ClienteDto
            {
                ClienteID = clienteId,
                Nombre = nombreActualizado,
                Email = $"{Guid.NewGuid():N}@pruebas.com",
                Telefono = "000111222"
            });
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var obtenido = await _client.GetFromJsonAsync<ClienteDto>($"/api/{proveedor}/clientes/{clienteId}");
            obtenido!.Nombre.Should().Be(nombreActualizado);
        }

        [Theory(DisplayName = "ActualizarCliente inexistente: debería retornar BadRequest")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task ActualizarCliente_Inexistente(string proveedor)
        {
            var response = await _client.PutAsJsonAsync($"/api/{proveedor}/clientes", new ClienteDto
            {
                ClienteID = int.MaxValue,
                Nombre = "NoExiste",
                Email = $"{Guid.NewGuid():N}@pruebas.com",
                Telefono = "000000000"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "EliminarCliente sin órdenes: debería eliminarlo")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task EliminarCliente_SinOrdenes(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);

            var response = await _client.DeleteAsync($"/api/{proveedor}/clientes/{clienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var obtenidoResponse = await _client.GetAsync($"/api/{proveedor}/clientes/{clienteId}");
            obtenidoResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Theory(DisplayName = "EliminarCliente con órdenes pendientes: no debería eliminarlo")]
        [InlineData("adonet")]
        [InlineData("dapper")]
        [InlineData("efcore")]
        public async Task EliminarCliente_ConOrdenesPendientes(string proveedor)
        {
            var clienteId = await ClienteTestHelper.CrearClienteDePruebaAsync(_client, proveedor);
            var categoriaId = await CategoriaTestHelper.CrearCategoriaDePruebaAsync(_client, proveedor);

            var productoResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/productos", new ProductoDto
            {
                Nombre = $"ProductoParaOrden_{Guid.NewGuid():N}",
                Precio = 15.00m,
                CategoriaID = categoriaId
            });
            var productoResultado = await productoResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            var ordenResponse = await _client.PostAsJsonAsync($"/api/{proveedor}/ordenes", new OrdenDto
            {
                ClienteID = clienteId,
                Detalles = new List<OrdenDetalleDto>
                {
                    new() { ProductoID = productoResultado!.Data, Cantidad = 1 }
                }
            });
            var ordenResultado = await ordenResponse.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();
            ordenResultado!.Exito.Should().BeTrue();

            var response = await _client.DeleteAsync($"/api/{proveedor}/clientes/{clienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var obtenidoResponse = await _client.GetAsync($"/api/{proveedor}/clientes/{clienteId}");
            obtenidoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
