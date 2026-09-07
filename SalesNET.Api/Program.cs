using Microsoft.EntityFrameworkCore;
using SalesNET.Api.Data;
using SalesNET.Api.Middleware;
using SalesNET.Api.Repositories.ADO;
using SalesNET.Api.Repositories.Dapper;
using SalesNET.Api.Repositories.EFCore;
using SalesNET.Domain.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<SalesBDContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddKeyedScoped<IProductoRepository, ProductoRepositoryAdoNet>("adonet");
builder.Services.AddKeyedScoped<ICategoriaRepository, CategoriaRepositoryAdoNet>("adonet");
builder.Services.AddKeyedScoped<ICategoriaRepository, CategoriaRepositoryDapper>("dapper");
builder.Services.AddKeyedScoped<ICategoriaRepository, CategoriaRepositoryEfCore>("efcore");
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryAdoNet>("adonet");
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryDapper>("dapper");
builder.Services.AddKeyedScoped<IClienteRepository, ClienteRepositoryEfCore>("efcore");
builder.Services.AddKeyedScoped<IOrdenRepository, OrdenRepositoryAdoNet>("adonet");
builder.Services.AddKeyedScoped<IOrdenRepository, OrdenRepositoryDapper>("dapper");
builder.Services.AddKeyedScoped<IOrdenRepository, OrdenRepositoryEfCore>("efcore");
builder.Services.AddKeyedScoped<IProductoRepository, ProductoRepositoryDapper>("dapper");
builder.Services.AddKeyedScoped<IProductoRepository, ProductoRepositoryEfCore>("efcore");

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
