using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SalesNET.Domain.Entities
{
    public class OrdenDetalle
    {
        public int OrdenDetalleID { get; set; }
        public int OrdenID { get; set; }
        public int ProductoID { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; } 

        public Orden? Orden { get; set; }
        public Producto? Producto { get; set; }
    }
}