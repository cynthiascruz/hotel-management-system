using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManager
{
    internal class Cliente
    {
        // Propiedades básicas que corresponden a las columnas de la tabla Clientes
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string ApellidoPaterno { get; set; }
        public string ApellidoMaterno { get; set; }
        public string Ciudad { get; set; }
        public string Estado { get; set; }
        public string Pais { get; set; }
        public string RFC { get; set; }
        public string Correo { get; set; }
        public string TelefonoCasa { get; set; }
        public string TelefonoCelular { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string EstadoCivil { get; set; }

        // Propiedades de auditoría
        public DateTime FechaRegistro { get; set; }
        public int UsuarioRegistro { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int? UsuarioModificacion { get; set; }

        // Propiedad calculada para mostrar el nombre completo
        public string NombreCompleto
        {
            get
            {
                return $"{Nombre} {ApellidoPaterno} {ApellidoMaterno}".Trim();
            }
        }

        // Constructor por defecto
        public Cliente()
        {
            // Inicializa la fecha de registro con la fecha actual
            FechaRegistro = DateTime.Now;
        }

        // Mostrar información del cliente de forma legible
        public override string ToString()
        {
            return NombreCompleto;
        }
    }
}