using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesNET.Domain.Entities
{
    public class Cliente
    {
        public int ClienteID { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
        public DateTime? FechaModificacion { get; set; }

        public ICollection<Orden> Ordenes { get; set; } = new List<Orden>();
    }
}