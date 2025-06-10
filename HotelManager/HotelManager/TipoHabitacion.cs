using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelManager
{
    internal class TipoHabitacion
    {
        public int IdTipoHabitacion { get; set; }
        public string Nombre { get; set; }
        public string Caracteristicas { get; set; }
        public string TipoCama { get; set; }
        public string Nivel { get; set; }
        public string Ubicacion { get; set; }
        public decimal PrecioPorNoche { get; set; }
        public int CapacidadPersonas { get; set; }
        public int HabitacionesDisponibles { get; set; }
        public int CantidadSeleccionada { get; set; }
        public int PersonasAsignadas { get; set; }

        public bool ReservacionExistente { get; set; } = false;
        public string NumeroHabitacion { get; set; }
        public string CodigoReservacion { get; set; }
        public DateTime FechaCheckIn { get; set; }
        public DateTime FechaCheckOut { get; set; }
        public string EstadoReservacion { get; set; }
        public string NombreHotel { get; set; }

        // Constructor por defecto
        public TipoHabitacion()
        {
            CantidadSeleccionada = 0;
            PersonasAsignadas = 0;
        }

        // Método para calcular el subtotal
        public decimal CalcularSubtotal(int noches)
        {
            return PrecioPorNoche * noches;
        }
    }
}