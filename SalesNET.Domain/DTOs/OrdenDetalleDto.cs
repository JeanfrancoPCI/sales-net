namespace SalesNET.Domain.DTOs
{
    public class OrdenDetalleDto
    {
        public int OrdenDetalleID { get; set; }
        public int ProductoID { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal Subtotal => Cantidad * Precio;
    }
}