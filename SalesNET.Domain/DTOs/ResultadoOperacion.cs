namespace SalesNET.Domain.DTOs
{
    public class ResultadoOperacion
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    public class ResultadoOperacion<T> : ResultadoOperacion
    {
        public T? Data { get; set; }
    }
}