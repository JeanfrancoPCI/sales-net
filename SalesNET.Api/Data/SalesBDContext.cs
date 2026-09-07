using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SalesNET.Domain.Entities;

namespace SalesNET.Api.Data
{
    public class SalesBDContext : DbContext
    {
        public SalesBDContext(DbContextOptions<SalesBDContext> options) : base(options) { }

        public DbSet<Producto> Productos { get; set; } = null!;
        public DbSet<Categoria> Categorias { get; set; } = null!;
        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<Orden> Ordenes { get; set; } = null!;
        public DbSet<OrdenDetalle> OrdenDetalles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.ToTable("Categorias");
                entity.HasKey(c => c.CategoriaID);
                entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Descripcion).HasMaxLength(255);
                entity.Property(c => c.Activo).HasDefaultValue(true);
                entity.Property(c => c.FechaCreacion).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Producto>(entity =>
            {
                entity.ToTable("Productos");
                entity.HasKey(p => p.ProductoID);
                entity.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
                entity.Property(p => p.Precio).HasPrecision(10, 2);
                entity.Property(p => p.Activo).HasDefaultValue(true);
                entity.Property(p => p.FechaCreacion).HasDefaultValueSql("GETDATE()");

                entity.HasOne(p => p.Categoria)
                      .WithMany(c => c.Productos)
                      .HasForeignKey(p => p.CategoriaID);
            });

            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.ToTable("Clientes");
                entity.HasKey(c => c.ClienteID);
                entity.Property(c => c.Nombre).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Email).HasMaxLength(100).IsRequired();
                entity.Property(c => c.Telefono).HasMaxLength(20);
                entity.Property(c => c.Activo).HasDefaultValue(true);
                entity.Property(c => c.FechaCreacion).HasDefaultValueSql("GETDATE()");
            });

            modelBuilder.Entity<Orden>(entity =>
            {
                // OJO: la tabla real se llama "Ordenes" (plural), no "Orden".
                entity.ToTable("Ordenes");
                entity.HasKey(od => od.OrdenID);
                entity.Property(od => od.Total).HasPrecision(10, 2);
                entity.Property(od => od.Activo).HasDefaultValue(true);
                entity.Property(od => od.FechaCreacion).HasDefaultValueSql("GETDATE()");

                entity.HasOne(p => p.Cliente)
                      .WithMany(c => c.Ordenes)
                      .HasForeignKey(p => p.ClienteID);
            });

            modelBuilder.Entity<OrdenDetalle>(entity =>
            {
                entity.ToTable("OrdenDetalle");
                entity.HasKey(od => od.OrdenDetalleID);
                entity.Property(od => od.Precio).HasPrecision(10, 2);

                entity.HasOne(p => p.Orden)
                    .WithMany(p => p.Detalles)
                    .HasForeignKey(p => p.OrdenID);

                entity.HasOne(p => p.Producto)
                    .WithMany(p => p.OrdenDetalles)
                    .HasForeignKey(p => p.ProductoID);
            });
        }
    }
}
