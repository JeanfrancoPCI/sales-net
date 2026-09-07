# SalesNET

Proyecto de taller en **.NET 10** que implementa una API REST de ventas (categorías, productos, clientes y órdenes) sobre **SQL Server**, exponiendo **tres tecnologías de acceso a datos intercambiables en tiempo de ejecución** —ADO.NET, Dapper y Entity Framework Core— bajo el mismo contrato por entidad.

## Objetivo

El taller pide demostrar el uso de ADO.NET, Dapper y EF Core contra la misma base de datos. En lugar de tres proyectos separados, esta solución los expone como **tres implementaciones intercambiables de un mismo repositorio**, seleccionables en tiempo de ejecución mediante un segmento de ruta (`{proveedor}`), usando el patrón **Strategy** junto con **Keyed Services** de ASP.NET Core (`AddKeyedScoped` / `GetRequiredKeyedService`). Esta es la "Variante 2" del enunciado del taller, elegida deliberadamente sobre la Variante 1 porque permite comparar las tres tecnologías con el mismo request, cambiando solo la URL.

## Estructura del proyecto

```
SalesNET/
├── SalesNET.Domain/              # Núcleo del dominio (sin dependencias de infraestructura)
│   ├── DTOs/                     # CategoriaDto, ProductoDto, ClienteDto, OrdenDto, OrdenDetalleDto, ResultadoOperacion
│   ├── Entities/                 # Entidades mapeadas por EF Core (Categoria, Producto, Cliente, Orden, OrdenDetalle)
│   └── Interfaces/                # Contratos: ICategoriaRepository, IProductoRepository, IClienteRepository, IOrdenRepository
│
├── SalesNET.Api/                 # API Web (ASP.NET Core)
│   ├── Controllers/               # CategoriasController, ProductosController, ClientesController
│   ├── Repositories/
│   │   ├── ADO/                   # Implementación con ADO.NET puro (SqlConnection, SqlCommand, SqlParameter)
│   │   ├── Dapper/                # Implementación con Dapper (DynamicParameters, QueryAsync, ExecuteAsync)
│   │   └── EFCore/                # Implementación con Entity Framework Core (LINQ sobre SalesBDContext)
│   ├── Data/
│   │   └── SalesBDContext.cs      # DbContext y configuración Fluent API
│   ├── Program.cs                 # Registro de servicios y de los tres proveedores por entidad
│   └── appsettings*.json          # Configuración (la cadena de conexión va en appsettings.Development.json, no versionado)
│
├── SalesNET.Tests/                # Pruebas de integración con xUnit
│   └── *.cs                       # WebApplicationFactory<Program>, pruebas autocontenidas (crean su propia data de prueba)
│
├── Scripts/                       # Scripts SQL, a ejecutar en orden contra SQL Server
│   ├── 1. CREATE_TABLES.sql
│   ├── 2. SCRIPTS_CATEGORIAS.sql
│   ├── 3. SCRIPTS_CLIENTE.sql
│   ├── 4. SCRIPTS_ORDEN.sql
│   ├── 5. SCRIPTS_PRODUCTOS.sql
│   └── 6. SCRIPTS_DATA.sql        # Datos semilla (10 categorías y 40 productos de computación/sistemas)
│
└── SalesNET.slnx
```

## Arquitectura: Strategy Pattern + Keyed Services

Cada entidad tiene una interfaz en `SalesNET.Domain.Interfaces` (por ejemplo `IClienteRepository`) con **tres implementaciones** en `SalesNET.Api.Repositories`, una por tecnología. Las tres se registran con una *key* distinta en `Program.cs`:

```csharp
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryAdoNet>("adonet");
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryDapper>("dapper");
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryEfCore>("efcore");
```

Cada controller recibe un `IServiceProvider` y resuelve la implementación correcta según el segmento `{proveedor}` de la ruta:

```csharp
var repo = _serviceProvider.GetRequiredKeyedService<IClienteRepository>(proveedor);
```

Esto permite invocar exactamente la misma operación de negocio con las tres tecnologías, cambiando solo la URL: `/api/adonet/clientes`, `/api/dapper/clientes`, `/api/efcore/clientes`.

### Convenciones seguidas por los stored procedures

- Nomenclatura: `SP_ACCION_ENTIDAD` (`SP_CREAR_CLIENTE`, `SP_ACTUALIZAR_CATEGORIA`, etc.), con parámetros en PascalCase alineados a los nombres de columna.
- Todos los SPs de escritura devuelven `@COD_MENSAJE INT OUTPUT` y `@MENSAJE NVARCHAR(255) OUTPUT` para indicar éxito/fracaso y un mensaje descriptivo.
- Baja lógica (soft delete) mediante la columna `Activo` (bit), en vez de `DELETE` físico, para preservar integridad referencial con órdenes/productos históricos.
- Las validaciones de duplicados y existencia siempre filtran por `Activo = 1`.

## Tecnologías

- .NET 10 / ASP.NET Core Web API
- SQL Server
- ADO.NET (`Microsoft.Data.SqlClient`)
- Dapper
- Entity Framework Core (proveedor SQL Server)
- xUnit + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) + FluentAssertions, para pruebas de integración

## Configuración y puesta en marcha

1. **Clonar el repositorio** y restaurar dependencias:
   ```bash
   dotnet restore
   ```

2. **Levantar SQL Server** (por ejemplo con Docker):
   ```bash
   docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=TU_PASSWORD_AQUI" \
     -p 1433:1433 --name sqlserver-salesnet -d mcr.microsoft.com/mssql/server:2022-latest
   ```

3. **Ejecutar los scripts SQL** de la carpeta `Scripts/` **en orden numérico** (1 al 6) contra una base `SalesBD`, usando `sqlcmd`, Azure Data Studio o SSMS.

4. **Configurar la cadena de conexión**: copiar `SalesNET.Api/appsettings.Development.json.example` a `SalesNET.Api/appsettings.Development.json` y completar usuario/contraseña reales. Este último archivo **no se versiona** (ver `.gitignore`) porque contiene credenciales.

5. **Ejecutar la API**:
   ```bash
   dotnet run --project SalesNET.Api
   ```

## Uso de la API

Todas las rutas siguen el patrón `api/{proveedor}/{recurso}`, donde `{proveedor}` es `adonet`, `dapper` o `efcore`.

### Categorías (`api/{proveedor}/categorias`)
- `GET /` — listar categorías activas
- `POST /` — crear categoría
- `PUT /` — actualizar categoría
- `DELETE /{categoriaId}` — baja lógica

### Productos (`api/{proveedor}/productos`)
- `GET /?categoriaId=&nombre=` — listar (con filtros opcionales)
- `POST /` — crear producto
- `PUT /` — actualizar producto
- `DELETE /{productoId}` — baja lógica

### Clientes (`api/{proveedor}/clientes`)
- `GET /` — listar clientes
- `GET /{clienteId}` — obtener por ID
- `GET /{clienteId}/ordenes` — órdenes del cliente
- `POST /` — crear cliente
- `PUT /` — actualizar cliente
- `DELETE /{clienteId}` — baja lógica

### Ejemplo (curl)

```bash
curl http://localhost:5xxx/api/efcore/productos?categoriaId=1
curl -X POST http://localhost:5xxx/api/dapper/clientes \
  -H "Content-Type: application/json" \
  -d '{"nombre":"Juan Perez","email":"juan@mail.com","telefono":"999999999"}'
```

> Órdenes (`IOrdenRepository`) ya tiene sus tres implementaciones (ADO.NET, Dapper, EF Core) registradas en `Program.cs`, pero todavía no cuenta con un controller HTTP — ver sección de pendientes.

## Pruebas

```bash
dotnet test
```

Las pruebas de `SalesNET.Tests` usan `WebApplicationFactory<Program>` para levantar la API en memoria y son autocontenidas: cada prueba crea sus propios datos (por ejemplo, una categoría propia) en lugar de depender de la data semilla, para poder ejecutarse en cualquier orden y contra cualquier base de datos limpia.

## Pendientes / próximos pasos

- Crear `OrdenesController` para exponer `IOrdenRepository` vía HTTP.
- Evaluar una base común para los controllers que elimine la duplicación de la validación del `{proveedor}` (clase base genérica, filtro de acción, o resolución con excepción + manejo global de excepciones).
