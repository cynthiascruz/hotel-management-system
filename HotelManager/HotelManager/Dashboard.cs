using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManager.Data;
using HotelManager.Forms;

namespace HotelManager
{
    public partial class Dashboard : Form
    {
        public Dashboard()
        {
            InitializeComponent();
            CargarEstadisticas();
        }

        private void CargarEstadisticas()
        {
            try
            {
                // Obtener cantidad de hoteles activos
                string queryHoteles = "SELECT COUNT(*) FROM Hoteles";
                DataTable dtHoteles = Database.ExecuteQuery(queryHoteles);
                if (dtHoteles.Rows.Count > 0)
                {
                    lblHoteles.Text = dtHoteles.Rows[0][0].ToString();
                }

                // Obtener cantidad de reservaciones activas
                string queryReservaciones = @"
                    SELECT COUNT(*) 
                    FROM Reservaciones 
                    WHERE EstadoReservacion IN ('Confirmada', 'CheckIn')";
                DataTable dtReservaciones = Database.ExecuteQuery(queryReservaciones);
                if (dtReservaciones.Rows.Count > 0)
                {
                    lblReservacionesActivas.Text = dtReservaciones.Rows[0][0].ToString();
                }

                // Obtener porcentaje de ocupación total
                string queryOcupacion = @"
                    WITH HabitacionesDisponibles AS (
                        SELECT SUM(CantidadHabitaciones) AS Total
                        FROM TiposHabitacion
                    ),
                    HabitacionesOcupadas AS (
                        SELECT COUNT(DISTINCT dr.IdHabitacion) AS Ocupadas
                        FROM Reservaciones r
                        INNER JOIN DetalleReservaciones dr ON r.IdReservacion = dr.IdReservacion
                        WHERE r.EstadoReservacion = 'CheckIn'
                    )
                    SELECT 
                        CASE 
                            WHEN hd.Total > 0 
                            THEN CAST(ho.Ocupadas AS FLOAT) / hd.Total * 100
                            ELSE 0 
                        END AS PorcentajeOcupacion
                    FROM 
                        HabitacionesDisponibles hd,
                        HabitacionesOcupadas ho";
                DataTable dtOcupacion = Database.ExecuteQuery(queryOcupacion);
                if (dtOcupacion.Rows.Count > 0)
                {
                    double porcentaje = Convert.ToDouble(dtOcupacion.Rows[0][0]);
                    lblOcupacionTotal.Text = porcentaje.ToString("0.00") + "%";
                }

                // Generar datos para el gráfico de ocupación mensual (simplificado)
                string queryMensual = @"
                    WITH Meses AS (
                        SELECT 1 AS Mes, 'Enero' AS NombreMes UNION
                        SELECT 2, 'Febrero' UNION SELECT 3, 'Marzo' UNION
                        SELECT 4, 'Abril' UNION SELECT 5, 'Mayo' UNION
                        SELECT 6, 'Junio' UNION SELECT 7, 'Julio' UNION
                        SELECT 8, 'Agosto' UNION SELECT 9, 'Septiembre' UNION
                        SELECT 10, 'Octubre' UNION SELECT 11, 'Noviembre' UNION
                        SELECT 12, 'Diciembre'
                    ),
                    ReservacionesPorMes AS (
                        SELECT 
                            MONTH(r.FechaCheckIn) AS Mes,
                            COUNT(*) AS Cantidad
                        FROM 
                            Reservaciones r
                        WHERE 
                            YEAR(r.FechaCheckIn) = YEAR(GETDATE())
                        GROUP BY 
                            MONTH(r.FechaCheckIn)
                    )
                    SELECT 
                        m.NombreMes,
                        ISNULL(rpm.Cantidad, 0) AS Cantidad
                    FROM 
                        Meses m
                    LEFT JOIN 
                        ReservacionesPorMes rpm ON m.Mes = rpm.Mes
                    ORDER BY 
                        m.Mes";
                DataTable dtMensual = Database.ExecuteQuery(queryMensual);

                // Para simplificar, solo mostramos el total de reservaciones del mes actual
                int mesActual = DateTime.Now.Month;
                string nombreMesActual = string.Empty;
                int cantidadReservaciones = 0;

                foreach (DataRow row in dtMensual.Rows)
                {
                    if (row["NombreMes"].ToString() == ObtenerNombreMes(mesActual))
                    {
                        nombreMesActual = row["NombreMes"].ToString();
                        cantidadReservaciones = Convert.ToInt32(row["Cantidad"]);
                        break;
                    }
                }

                lblOcupacionMensual.Text = $"Reservaciones en {nombreMesActual}: {cantidadReservaciones}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar estadísticas: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ObtenerNombreMes(int mes)
        {
            string[] meses = { "Enero", "Febrero", "Marzo", "Abril", "Mayo", "Junio",
                              "Julio", "Agosto", "Septiembre", "Octubre", "Noviembre", "Diciembre" };

            if (mes >= 1 && mes <= 12)
                return meses[mes - 1];

            return string.Empty;
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
           
        }

        private void Dashboard_Load(object sender, EventArgs e)
        {

        }
        private void btnGestionUsuarios_Click(object sender, EventArgs e)
        {
            GestionUsuarios gestionUsuarios = new GestionUsuarios();
            gestionUsuarios.Show();
            this.Close();
        }

        private void btnGestionHoteles_Click(object sender, EventArgs e)
        {
            GestionHoteles gestionHoteles = new GestionHoteles();
            gestionHoteles.Show();
            this.Close();
        }

        private void btnConfigHabitaciones_Click(object sender, EventArgs e)
        {
            ConfigHabitaciones configHabitaciones = new ConfigHabitaciones();
            configHabitaciones.Show();
            this.Close();
        }

        private void btnReservaciones_Click(object sender, EventArgs e)
        {
            Reservaciones reservaciones = new Reservaciones();
            reservaciones.Show();
            this.Close();
        }

        private void btnCheckIn_Click(object sender, EventArgs e)
        {
            CheckIn checkIn = new CheckIn();
            checkIn.Show();
            this.Close();
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            CheckOut checkOut = new CheckOut();
            checkOut.Show();
            this.Close();
        }

        private void btnCancelaciones_Click(object sender, EventArgs e)
        {
            Cancelaciones cancelaciones = new Cancelaciones();
            cancelaciones.Show();
            this.Close();
        }

        private void btnHistorialClientes_Click(object sender, EventArgs e)
        {
            HistorialClientes historialClientes = new HistorialClientes();
            historialClientes.Show();
            this.Close();
        }

        private void btnReportesVentas_Click(object sender, EventArgs e)
        {
            ReporteVentas reporteVentas = new ReporteVentas();
            reporteVentas.Show();
            this.Close();
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            // Mostrar pantalla de inicio de sesión
           LoginForm login = new LoginForm();
            login.Show();
            this.Close();
        }

        private void btnReportesOcupacion_Click(object sender, EventArgs e)
        {
            ReporteOcupacion reporteOcupacion = new ReporteOcupacion();
            reporteOcupacion.Show();
            this.Close();
        }

        private void btnGestionClientes_Click(object sender, EventArgs e)
        {
            GestionClientes gestionClientes = new GestionClientes();
            gestionClientes.Show();
            this.Close();
        }
    }
}
