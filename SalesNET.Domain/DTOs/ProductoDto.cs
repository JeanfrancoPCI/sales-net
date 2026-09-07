namespace SalesNET.Domain.DTOs
{
    public class ProductoDto
    {
        public int ProductoID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int CategoriaID { get; set; }
        public string CategoriaNombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}