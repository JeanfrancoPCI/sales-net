using System.Net.Http.Json;
using FluentAssertions;
using SalesNET.Domain.DTOs;
using Xunit;

namespace SalesNET.Tests
{
    // Los tests de integración golpean la misma instancia real de SQL Server (sin
    // aislamiento por test). Se agrupan en "BaseDatosSalesNET" para que xUnit los
    // ejecute en serie entre sí y evitar contención de locks/timeouts, sin
    // desactivar el paralelismo del resto del assembly.
    [CollectionDefinition("BaseDatosSalesNET")]
    public class BaseDatosSalesNETCollection
    {
    }

    internal static class CategoriaTestHelper
    {
        public static async Task<int> CrearCategoriaDePruebaAsync(HttpClient client, string proveedor = "adonet")
        {
            var categoria = new CategoriaDto
            {
                Nombre = $"CategoriaPrueba_{Guid.NewGuid():N}",
                Descripcion = "Categoría creada por los tests de integración"
            };

            var response = await client.PostAsJsonAsync($"/api/{proveedor}/categorias", categoria);
            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();

            return resultado.Data;
        }
    }

    internal static class ClienteTestHelper
    {
        public static async Task<int> CrearClienteDePruebaAsync(HttpClient client, string proveedor = "adonet")
        {
            var cliente = new ClienteDto
            {
                Nombre = $"ClientePrueba_{Guid.NewGuid():N}",
                Email = $"{Guid.NewGuid():N}@pruebas.com",
                Telefono = "999999999"
            };

            var response = await client.PostAsJsonAsync($"/api/{proveedor}/clientes", cliente);
            var resultado = await response.Content.ReadFromJsonAsync<ResultadoOperacion<int>>();

            resultado.Should().NotBeNull();
            resultado!.Exito.Should().BeTrue();

            return resultado.Data;
        }
    }
}
