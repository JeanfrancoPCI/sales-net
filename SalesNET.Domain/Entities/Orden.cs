using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesNET.Domain.Entities
{
    public class Orden
    {
        public int OrdenID { get; set; }
        public int ClienteID { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public bool Activo { get; set; } = false;
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }

        public Cliente? Cliente { get; set; }
        public ICollection<OrdenDetalle> Detalles { get; set; } = new List<OrdenDetalle>();
    }
}