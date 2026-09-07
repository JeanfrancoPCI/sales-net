namespace SalesNET.Domain.Entities
{
    public class Producto
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int CategoriaID { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public Categoria? Categoria { get; set; }

        public ICollection<OrdenDetalle> OrdenDetalles { get; set; } = new List<OrdenDetalle>();
    }
}