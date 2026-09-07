namespace SalesNET.Domain.DTOs
{
    public class CategoriaDto
    {
        public int CategoriaID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }
}
