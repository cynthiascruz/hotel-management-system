using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManager.Classes;
using HotelManager.Data;
using HotelManager.Forms;

namespace HotelManager
{
    public partial class CheckIn : Form
    {

        // Variables de clase
        private int idReservacionActual = 0;
        private DateTime fechaActual = DateTime.Now;
        private List<HabitacionAsignada> habitacionesAsignadas = new List<HabitacionAsignada>();

        // Clase para gestionar las habitaciones asignadas
        private class HabitacionAsignada
        {
            public int IdHabitacion { get; set; }
            public string NumeroHabitacion { get; set; }
            public string TipoHabitacion { get; set; }
            public int Piso { get; set; }
            public int CantidadPersonas { get; set; }
        }

        public CheckIn()
        {
            InitializeComponent();
            InicializarControles();
        }

        // Método para inicializar y configurar controles
        private void InicializarControles()
        {
            // Limpiamos campos
            txtCodigoReservacion.Clear();
            LimpiarDatosReservacion();

            // Estado inicial de botones
            btnBuscar.Enabled = true;
            btnCompletarCheckIn.Enabled = false;
            btnCancelar.Enabled = true;

            ConfigurarDataGridView();

            txtCodigoReservacion.MaxLength = 36; // Longitud de un GUID
        }

        // Configuramos el DataGridView para mostrar habitaciones
        private void ConfigurarDataGridView()
        {
            // Limpiar columnas existentes
            dgvHabitaciones.Columns.Clear();
            dgvHabitaciones.DataSource = null;

            // Añadir columnas al DataGridView
            dgvHabitaciones.Columns.Add("IdHabitacion", "ID");
            dgvHabitaciones.Columns.Add("NumeroHabitacion", "Número");
            dgvHabitaciones.Columns.Add("TipoHabitacion", "Tipo");
            dgvHabitaciones.Columns.Add("Piso", "Piso");
            dgvHabitaciones.Columns.Add("CantidadPersonas", "Capacidad");

            // Configurar propiedades del DataGridView
            dgvHabitaciones.AllowUserToAddRows = false;
            dgvHabitaciones.AllowUserToDeleteRows = false;
            dgvHabitaciones.ReadOnly = true;
            dgvHabitaciones.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHabitaciones.MultiSelect = false;
            dgvHabitaciones.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Ocultar la columna ID
            dgvHabitaciones.Columns["IdHabitacion"].Visible = false;

            // Limpiar filas
            dgvHabitaciones.Rows.Clear();
        }

        // Limpiamos los campos de información de reservación
        private void LimpiarDatosReservacion()
        {
            lblCliente.Text = string.Empty;
            lblHotel.Text = string.Empty;
            lblTipoHabitacion.Text = string.Empty;
            lblCantidadPersonas.Text = string.Empty;
            lblCheckIn.Text = string.Empty;
            lblCheckOut.Text = string.Empty;
            lblAnticipoPagado.Text = string.Empty;
            lblEstadoActual.Text = string.Empty;
            idReservacionActual = 0;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string codigoReservacion = txtCodigoReservacion.Text.Trim();

            if (string.IsNullOrEmpty(codigoReservacion))
            {
                MessageBox.Show("Por favor, ingrese el código de reservación.",
                    "Código requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigoReservacion.Focus();
                return;
            }

            // Validamos formato GUID
            if (!ValidarFormatoGUID(codigoReservacion))
            {
                MessageBox.Show("El código de reservación debe tener un formato válido.",
                    "Formato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigoReservacion.Focus();
                return;
            }

            // Buscamos la reservación
            BuscarReservacionPorCodigo(codigoReservacion);
        }

        // Validamos que el código tenga formato de GUID
        private bool ValidarFormatoGUID(string codigo)
        {
            try
            {
                Guid guid = new Guid(codigo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Método principal para buscar la reservación por código
        private void BuscarReservacionPorCodigo(string codigo)
        {
            try
            {
                // Consulta principal para obtener datos de la reservación
                string query = @"
                    SELECT r.IdReservacion, r.CodigoReservacion, 
                           CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                           h.Nombre AS NombreHotel, h.IdHotel,
                           r.FechaCheckIn, r.FechaCheckOut, 
                           r.CantidadPersonas, r.MontoAnticipo, r.EstadoReservacion
                    FROM Reservaciones r
                    INNER JOIN Clientes c ON r.IdCliente = c.IdCliente
                    INNER JOIN Hoteles h ON r.IdHotel = h.IdHotel
                    WHERE r.CodigoReservacion = @Codigo";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Codigo", codigo)
                };

                DataTable dtReservacion = Database.ExecuteQuery(query, parameters);

                if (dtReservacion.Rows.Count > 0)
                {
                    DataRow row = dtReservacion.Rows[0];

                    // Obtenemos datos básicos
                    idReservacionActual = Convert.ToInt32(row["IdReservacion"]);
                    string estadoReservacion = row["EstadoReservacion"].ToString();
                    DateTime fechaCheckIn = Convert.ToDateTime(row["FechaCheckIn"]);

                    // Validamos el estado de la reservación
                    if (!ValidarEstadoReservacion(estadoReservacion, fechaCheckIn))
                    {
                        return;
                    }

                    // Mostramos los datos de la reservación
                    MostrarDatosReservacion(row);

                    // Obtenemos y mostramos los tipos de habitación reservados
                    ObtenerTiposHabitacionReservados();

                    // Cargamos las habitaciones en el DataGridView
                    CargarHabitacionesAsignadas();

                    // Habilitamos botón para completar check-in
                    btnCompletarCheckIn.Enabled = true;
                }
                else
                {
                    MessageBox.Show("No se encontró ninguna reservación con el código proporcionado.",
                        "Reservación no encontrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarDatosReservacion();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar la reservación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LimpiarDatosReservacion();
            }
        }

        // Validamos el estado de la reservación
        private bool ValidarEstadoReservacion(string estadoReservacion, DateTime fechaCheckIn)
        {
            // Validamos si ya tiene check-in o check-out
            if (estadoReservacion == "CheckIn" || estadoReservacion == "CheckOut")
            {
                MessageBox.Show("Esta reservación ya tiene un Check-In o Check-Out registrado.",
                    "Proceso ya realizado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LimpiarDatosReservacion();
                return false;
            }

            // Validamos si está cancelada
            if (estadoReservacion == "Cancelada")
            {
                MessageBox.Show("Esta reservación ha sido cancelada y no se puede proceder con el Check-In.",
                    "Reservación cancelada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                LimpiarDatosReservacion();
                return false;
            }

            // Validamos la fecha de check-in (si es la fecha correcta)
            if (fechaCheckIn.Date != fechaActual.Date)
            {
                // Si la fecha actual es posterior a la fecha de check-in, la reservación debería cancelarse automáticamente
                if (fechaActual.Date > fechaCheckIn.Date)
                {
                    MessageBox.Show("La fecha de Check-In programada ya pasó. " +
                        "Esta reservación debería ser cancelada automáticamente por el sistema.",
                        "Fecha incorrecta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    // Aquí podríamos implementar la lógica para cancelar automáticamente
                    LimpiarDatosReservacion();
                    return false;
                }
                else // La fecha actual es anterior a la fecha de check-in
                {
                    MessageBox.Show($"La fecha de Check-In programada es {fechaCheckIn.ToString("dd-MMM-yyyy")}. " +
                        "No se puede realizar el Check-In antes de la fecha programada.",
                        "Fecha incorrecta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LimpiarDatosReservacion();
                    return false;
                }
            }

            return true; // El estado es válido para proceder con el check-in
        }

        // Mostramos los datos básicos de la reservación
        private void MostrarDatosReservacion(DataRow row)
        {
            lblCliente.Text = row["NombreCliente"].ToString();
            lblHotel.Text = row["NombreHotel"].ToString();
            // El tipo de habitación se cargará en un método separado
            lblCantidadPersonas.Text = row["CantidadPersonas"].ToString();
            lblCheckIn.Text = Convert.ToDateTime(row["FechaCheckIn"]).ToString("dd-MMM-yyyy");
            lblCheckOut.Text = Convert.ToDateTime(row["FechaCheckOut"]).ToString("dd-MMM-yyyy");
            lblAnticipoPagado.Text = string.Format("${0:#,##0.00}", Convert.ToDecimal(row["MontoAnticipo"]));
            lblEstadoActual.Text = row["EstadoReservacion"].ToString();
        }

        // Obtenemos los tipos de habitación reservados
        private void ObtenerTiposHabitacionReservados()
        {
            try
            {
                // Consulta para obtener los tipos de habitación reservados
                string query = @"
                    SELECT th.Nombre AS TipoHabitacion, COUNT(dr.IdHabitacion) AS CantidadHabitaciones
                    FROM DetalleReservaciones dr
                    INNER JOIN Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
                    INNER JOIN TiposHabitacion th ON h.IdTipoHabitacion = th.IdTipoHabitacion
                    WHERE dr.IdReservacion = @IdReservacion
                    GROUP BY th.Nombre";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                DataTable dtTiposHabitacion = Database.ExecuteQuery(query, parameters);

                StringBuilder tiposHabitacion = new StringBuilder();

                foreach (DataRow row in dtTiposHabitacion.Rows)
                {
                    string tipo = row["TipoHabitacion"].ToString();
                    int cantidad = Convert.ToInt32(row["CantidadHabitaciones"]);

                    tiposHabitacion.Append(tipo);
                    if (cantidad > 1)
                        tiposHabitacion.Append($" ({cantidad})");

                    tiposHabitacion.Append(", ");
                }

                // Removemos la última coma y espacio
                if (tiposHabitacion.Length > 2)
                    tiposHabitacion.Length -= 2;

                lblTipoHabitacion.Text = tiposHabitacion.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener los tipos de habitación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cargar las habitaciones asignadas a la reservación
        private void CargarHabitacionesAsignadas()
        {
            try
            {
                // Limpiar el DataGridView y la lista de habitaciones
                dgvHabitaciones.Rows.Clear();
                habitacionesAsignadas.Clear();

                // Consultamos para obtener las habitaciones reservadas
                string query = @"
                    SELECT h.IdHabitacion, h.NumeroHabitacion, th.Nombre AS TipoHabitacion, 
                           h.Piso, th.CapacidadPersonas
                    FROM DetalleReservaciones dr
                    INNER JOIN Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
                    INNER JOIN TiposHabitacion th ON h.IdTipoHabitacion = th.IdTipoHabitacion
                    WHERE dr.IdReservacion = @IdReservacion
                    ORDER BY h.Piso, h.NumeroHabitacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                DataTable dtHabitaciones = Database.ExecuteQuery(query, parameters);

                foreach (DataRow row in dtHabitaciones.Rows)
                {
                    // Añadir a la lista de habitaciones asignadas
                    var habitacion = new HabitacionAsignada
                    {
                        IdHabitacion = Convert.ToInt32(row["IdHabitacion"]),
                        NumeroHabitacion = row["NumeroHabitacion"].ToString(),
                        TipoHabitacion = row["TipoHabitacion"].ToString(),
                        Piso = Convert.ToInt32(row["Piso"]),
                        CantidadPersonas = Convert.ToInt32(row["CapacidadPersonas"])
                    };
                    habitacionesAsignadas.Add(habitacion);

                    // Añadir al DataGridView
                    dgvHabitaciones.Rows.Add(
                        habitacion.IdHabitacion,
                        habitacion.NumeroHabitacion,
                        habitacion.TipoHabitacion,
                        habitacion.Piso,
                        habitacion.CantidadPersonas
                    );
                }

                // Verificamos si se encontraron habitaciones
                if (habitacionesAsignadas.Count == 0)
                {
                    MessageBox.Show("No se encontraron habitaciones asignadas a esta reservación.",
                        "Sin habitaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las habitaciones asignadas: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            InicializarControles();
        }

        private void btnCompletarCheckIn_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificamos que haya habitaciones asignadas
                if (habitacionesAsignadas.Count == 0)
                {
                    MessageBox.Show("No hay habitaciones asignadas para esta reservación. No se puede completar el Check-In.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Confirmamos la operación
                DialogResult confirmar = MessageBox.Show(
                    "¿Está seguro de que desea completar el Check-In para esta reservación?",
                    "Confirmar Check-In", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmar == DialogResult.No)
                    return;

                // Iniciamos una transacción para asegurar integridad
                using (SqlConnection connection = new SqlConnection(Database.ConnectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // 1. Actualizamos el estado de la reservación a "CheckIn"
                        string updateReservacion = @"
                            UPDATE Reservaciones
                            SET EstadoReservacion = 'CheckIn',
                                FechaHoraCheckIn = @FechaHora,
                                UsuarioModificacion = @UsuarioModificacion,
                                FechaModificacion = @FechaHora
                            WHERE IdReservacion = @IdReservacion";

                        SqlCommand cmdReservacion = new SqlCommand(updateReservacion, connection, transaction);
                        cmdReservacion.Parameters.AddWithValue("@FechaHora", DateTime.Now);
                        cmdReservacion.Parameters.AddWithValue("@UsuarioModificacion", Session.IdUsuario);
                        cmdReservacion.Parameters.AddWithValue("@IdReservacion", idReservacionActual);

                        int filasAfectadas = cmdReservacion.ExecuteNonQuery();
                        if (filasAfectadas == 0)
                        {
                            throw new Exception("No se pudo actualizar el estado de la reservación.");
                        }

                        // 2. Actualizamos el estado de las habitaciones a "Ocupada"
                        string updateHabitaciones = @"
                            UPDATE Habitaciones
                            SET Estado = 'Ocupada',
                                UsuarioModificacion = @UsuarioModificacion,
                                FechaModificacion = @FechaHora
                            WHERE IdHabitacion IN (
                                SELECT IdHabitacion FROM DetalleReservaciones 
                                WHERE IdReservacion = @IdReservacion
                            )";

                        SqlCommand cmdHabitaciones = new SqlCommand(updateHabitaciones, connection, transaction);
                        cmdHabitaciones.Parameters.AddWithValue("@FechaHora", DateTime.Now);
                        cmdHabitaciones.Parameters.AddWithValue("@UsuarioModificacion", Session.IdUsuario);
                        cmdHabitaciones.Parameters.AddWithValue("@IdReservacion", idReservacionActual);

                        cmdHabitaciones.ExecuteNonQuery();

                        // Confirmamos la transacción
                        transaction.Commit();

                        // Mostramos mensaje de éxito
                        MessageBox.Show("¡Check-In completado exitosamente!\n\n" +
                            $"Cliente: {lblCliente.Text}\n" +
                            $"Hotel: {lblHotel.Text}\n" +
                            $"Habitaciones asignadas: {habitacionesAsignadas.Count}\n" +
                            $"Fecha de Check-In: {DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss")}",
                            "Check-In Exitoso", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Limpiamos la pantalla para un nuevo check-in
                        InicializarControles();
                    }
                    catch (Exception ex)
                    {
                        // Revertimos la transacción en caso de error
                        transaction.Rollback();
                        throw new Exception("Error al completar el Check-In: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
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

        private void btnReporteOcupacion_Click(object sender, EventArgs e)
        {
            ReporteOcupacion reporteOcupacion = new ReporteOcupacion();
            reporteOcupacion.Show();
            this.Close();
        }

        private void btnReporteVentas_Click(object sender, EventArgs e)
        {
            ReporteVentas reporteVentas = new ReporteVentas();
            reporteVentas.Show();
            this.Close();
        }

        private void btnHistorialClientes_Click(object sender, EventArgs e)
        {
            HistorialClientes historialClientes = new HistorialClientes();
            historialClientes.Show();
            this.Close();
        }

        private void btnCancelaciones_Click(object sender, EventArgs e)
        {
            Cancelaciones cancelaciones = new Cancelaciones();
            cancelaciones.Show();
            this.Close();
        }

        private void btnGestionClientes_Click(object sender, EventArgs e)
        {
            GestionClientes gestionClientes = new GestionClientes();
            gestionClientes.Show();
            this.Close();
        }

        private void btnReservaciones_Click(object sender, EventArgs e)
        {
            Reservaciones reservaciones = new Reservaciones();
            reservaciones.Show();
            this.Close();
        }

        private void btnCheckOut_Click(object sender, EventArgs e)
        {
            CheckOut checkOut = new CheckOut();
            checkOut.Show();
            this.Close();
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            // Mostrar pantalla de inicio de sesión
            LoginForm login = new LoginForm();
            login.Show();
            this.Close();
        }

        private void CheckIn_Load(object sender, EventArgs e)
        {
            // Verificamos que solo los operativos puedan acceder
            if (Session.TipoUsuario != "Operativo")
            {
                MessageBox.Show("Solo los operativos pueden acceder a la gestión de clientes.",
                                "Acceso denegado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Close();
                return;
            }
        }
    }
}
