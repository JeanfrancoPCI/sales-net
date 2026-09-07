namespace SalesNET.Domain.DTOs
{
    public class OrdenDto
    {
        public int OrdenID { get; set; }
        public int ClienteID { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string ClienteEmail { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public int TotalProductos { get; set; } = 0;
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public ICollection<OrdenDetalleDto> Detalles { get; set; } = new List<OrdenDetalleDto>();
    }
}