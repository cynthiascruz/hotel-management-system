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
    public partial class Cancelaciones : Form
    {

        // Variables de clase
        private int idReservacionActual = 0;
        private Guid codigoReservacionActual;
        private DateTime fechaCheckIn;
        private decimal montoAnticipo = 0;
        private bool puedeReembolsar = false;
        // Añadir esta variable estática a la clase:
        private static bool cancelacionesAutomaticasVerificadasHoy = false;

        public Cancelaciones()
        {
            InitializeComponent();
        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void Cancelaciones_Load(object sender, EventArgs e)
        {
            // Verificamos que solo los administradores puedan acceder
            if (Session.TipoUsuario != "Administrador")
            {
                MessageBox.Show("Solo los administradores pueden acceder a la gestión de usuarios.",
                                "Acceso denegado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Close();
                return;
            }

            // Verificar cancelaciones automáticas
            Cancelaciones.VerificarCancelacionesAutomaticas();

            // Limpiar y preparar la pantalla
            LimpiarPantalla();
        }

        private void LimpiarPantalla()
        {
            // Limpiar campos
            txtCodigoReservacion.Clear();
            txtMotivoCancelacion.Clear();

            // Limpiar labels de información
            lblCliente.Text = string.Empty;
            lblHotel.Text = string.Empty;
            lblFechaReservacion.Text = string.Empty;
            lblTipoHabitacion.Text = string.Empty;
            lblCheckIn.Text = string.Empty;
            lblCheckOut.Text = string.Empty;
            lblAnticipoPagado.Text = string.Empty;
            lblEstado.Text = string.Empty;
            lblInfoMontoReembolso.Text = string.Empty;

            // Deshabilitar paneles hasta que se haga una búsqueda exitosa
            panelDatosReservacion.Enabled = false;
            panelMotivoCancelacion.Enabled = false;
            panelReembolso.Enabled = false;
            btnConfirmarCancelacion.Enabled = false;

            // Resetear variables
            idReservacionActual = 0;
            codigoReservacionActual = Guid.Empty;
            fechaCheckIn = DateTime.MinValue;
            montoAnticipo = 0;
            puedeReembolsar = false;

            // Establecer el foco inicial
            txtCodigoReservacion.Focus();
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string codigoReservacion = txtCodigoReservacion.Text.Trim();

            // Validar que el código no esté vacío
            if (string.IsNullOrEmpty(codigoReservacion))
            {
                MessageBox.Show("Por favor, ingrese el código de reservación.",
                    "Código requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigoReservacion.Focus();
                return;
            }

            // Validar formato GUID
            if (!ValidarFormatoGUID(codigoReservacion))
            {
                MessageBox.Show("El código de reservación debe tener un formato válido.",
                    "Formato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigoReservacion.Focus();
                return;
            }

            // Buscar la reservación
            BuscarReservacionPorCodigo(codigoReservacion);
        }

        // Validar que el código tenga formato de GUID
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
                // Limpiar datos anteriores
                LimpiarPantalla();

                // Obtener el GUID
                codigoReservacionActual = new Guid(codigo);

                // Consulta principal para obtener datos de la reservación
                string query = @"
                    SELECT r.IdReservacion, r.CodigoReservacion, 
                           CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                           h.Nombre AS NombreHotel,
                           r.FechaReservacion, r.FechaCheckIn, r.FechaCheckOut, 
                           r.MontoAnticipo, r.EstadoReservacion
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

                    // Obtener datos básicos
                    idReservacionActual = Convert.ToInt32(row["IdReservacion"]);
                    string estadoReservacion = row["EstadoReservacion"].ToString();
                    fechaCheckIn = Convert.ToDateTime(row["FechaCheckIn"]);
                    montoAnticipo = Convert.ToDecimal(row["MontoAnticipo"]);

                    // Validar el estado de la reservación
                    if (!ValidarEstadoReservacion(estadoReservacion))
                    {
                        return;
                    }

                    // Mostrar los datos de la reservación
                    MostrarDatosReservacion(row);

                    // Obtener tipos de habitación
                    ObtenerTiposHabitacion(idReservacionActual);

                    // Verificar si puede recibir reembolso (3 días antes de check-in)
                    VerificarReembolso();

                    // Habilitar paneles y controles
                    panelDatosReservacion.Enabled = true;
                    panelMotivoCancelacion.Enabled = true;
                    panelReembolso.Enabled = true;
                    btnConfirmarCancelacion.Enabled = true;
                }
                else
                {
                    MessageBox.Show("No se encontró ninguna reservación con el código proporcionado.",
                        "Reservación no encontrada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LimpiarPantalla();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar la reservación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LimpiarPantalla();
            }
        }

        // Validar el estado de la reservación
        private bool ValidarEstadoReservacion(string estadoReservacion)
        {
            // Para cancelación, solo se permiten reservaciones con estado "Confirmada"
            if (estadoReservacion != "Confirmada")
            {
                string mensaje;

                if (estadoReservacion == "Cancelada")
                {
                    mensaje = "Esta reservación ya ha sido cancelada.";
                }
                else if (estadoReservacion == "CheckIn" || estadoReservacion == "CheckOut")
                {
                    mensaje = "No se puede cancelar una reservación que ya ha realizado el check-in o check-out.";
                }
                else
                {
                    mensaje = $"Esta reservación tiene un estado no válido para cancelación: {estadoReservacion}";
                }

                MessageBox.Show(mensaje, "Estado incorrecto",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                LimpiarPantalla();
                return false;
            }

            return true;
        }

        // Mostrar los datos de la reservación
        private void MostrarDatosReservacion(DataRow row)
        {
            lblCliente.Text = row["NombreCliente"].ToString();
            lblHotel.Text = row["NombreHotel"].ToString();
            lblFechaReservacion.Text = Convert.ToDateTime(row["FechaReservacion"]).ToString("dd-MMM-yyyy");
            lblCheckIn.Text = Convert.ToDateTime(row["FechaCheckIn"]).ToString("dd-MMM-yyyy");
            lblCheckOut.Text = Convert.ToDateTime(row["FechaCheckOut"]).ToString("dd-MMM-yyyy");
            lblAnticipoPagado.Text = string.Format("${0:#,##0.00}", montoAnticipo);
            lblEstado.Text = row["EstadoReservacion"].ToString();
        }

        // Obtener los tipos de habitación de la reservación
        private void ObtenerTiposHabitacion(int idReservacion)
        {
            try
            {
                string query = @"
                    SELECT DISTINCT th.Nombre AS TipoHabitacion
                    FROM DetalleReservaciones dr
                    INNER JOIN Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
                    INNER JOIN TiposHabitacion th ON h.IdTipoHabitacion = th.IdTipoHabitacion
                    WHERE dr.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacion)
                };

                DataTable dt = Database.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (DataRow row in dt.Rows)
                    {
                        if (sb.Length > 0) sb.Append(", ");  // Añadir coma ANTES del elemento, excepto para el primero
                        sb.Append(row["TipoHabitacion"]);
                    }

                    // Remover la última coma y espacio
                    if (sb.Length > 2)
                    {
                        sb.Length -= 2;
                    }

                    lblTipoHabitacion.Text = sb.ToString();
                }
                else
                {
                    lblTipoHabitacion.Text = "Sin tipos de habitación registrados";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener tipos de habitación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblTipoHabitacion.Text = "Error al cargar tipos de habitación";
            }
        }

        // Verificar si aplica reembolso (3 días antes del check-in)
        private void VerificarReembolso()
        {
            // Calcular días para el check-in
            int diasParaCheckIn = (fechaCheckIn - DateTime.Today).Days;

            // Determinar si aplica reembolso (3 días o más)
            puedeReembolsar = diasParaCheckIn >= 3;

            // Mostrar mensaje correspondiente
            if (puedeReembolsar)
            {
                lblInfoMontoReembolso.Text = $"Se realizará un reembolso de ${montoAnticipo:#,##0.00} al cliente.";
            }
            else
            {
                lblInfoMontoReembolso.Text = "No aplica reembolso (menos de 3 días para el check-in).";
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            // Este botón es para validar el motivo de cancelación
            if (string.IsNullOrWhiteSpace(txtMotivoCancelacion.Text.Trim()))
            {
                MessageBox.Show("Por favor, ingrese un motivo de cancelación.",
                    "Motivo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtMotivoCancelacion.Focus();
            }
            else
            {
                // Podría mostrar un mensaje de confirmación o simplemente preparar para la cancelación final
                MessageBox.Show("Motivo de cancelación registrado. Por favor, confirme la cancelación con el botón 'Confirmar cancelación'.",
                    "Motivo registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnConfirmarCancelacion_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar que haya un motivo de cancelación
                string motivoCancelacion = txtMotivoCancelacion.Text.Trim();
                if (string.IsNullOrWhiteSpace(motivoCancelacion))
                {
                    MessageBox.Show("Por favor, ingrese un motivo de cancelación.",
                        "Motivo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtMotivoCancelacion.Focus();
                    return;
                }

                // Confirmar la cancelación
                DialogResult result = MessageBox.Show(
                    "¿Está seguro de cancelar esta reservación?\n" +
                    (puedeReembolsar ? $"Se realizará un reembolso de ${montoAnticipo:#,##0.00} al cliente." : "No se realizará reembolso al cliente."),
                    "Confirmar cancelación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.No)
                    return;

                // Proceder con la cancelación
                RealizarCancelacion(motivoCancelacion);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al confirmar la cancelación: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        private void RealizarCancelacion(string motivo)
        {
            try
            {
                // Iniciar una transacción para todas las operaciones de cancelación
                using (SqlConnection connection = new SqlConnection(Database.ConnectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // 1. Actualizar el estado de la reservación
                        string queryReservacion = @"
                            UPDATE Reservaciones
                            SET EstadoReservacion = 'Cancelada',
                                FechaCancelacion = GETDATE(),
                                MotivosCancelacion = @Motivo,
                                UsuarioModificacion = @UsuarioId,
                                FechaModificacion = GETDATE()
                            WHERE IdReservacion = @IdReservacion";

                        SqlCommand cmdReservacion = new SqlCommand(queryReservacion, connection, transaction);
                        cmdReservacion.Parameters.AddWithValue("@Motivo", motivo);
                        cmdReservacion.Parameters.AddWithValue("@UsuarioId", Session.IdUsuario);
                        cmdReservacion.Parameters.AddWithValue("@IdReservacion", idReservacionActual);
                        cmdReservacion.ExecuteNonQuery();

                        // 2. Liberar las habitaciones (actualizar su estado a Disponible)
                        string queryHabitaciones = @"
                            UPDATE h
                            SET h.Estado = 'Disponible',
                                h.UsuarioModificacion = @UsuarioId,
                                h.FechaModificacion = GETDATE()
                            FROM Habitaciones h
                            INNER JOIN DetalleReservaciones dr ON h.IdHabitacion = dr.IdHabitacion
                            WHERE dr.IdReservacion = @IdReservacion";

                        SqlCommand cmdHabitaciones = new SqlCommand(queryHabitaciones, connection, transaction);
                        cmdHabitaciones.Parameters.AddWithValue("@UsuarioId", Session.IdUsuario);
                        cmdHabitaciones.Parameters.AddWithValue("@IdReservacion", idReservacionActual);
                        cmdHabitaciones.ExecuteNonQuery();

                        // Confirmar la transacción
                        transaction.Commit();

                        // Mostrar mensaje de éxito
                        string mensaje = "La reservación ha sido cancelada exitosamente.";
                        if (puedeReembolsar)
                        {
                            mensaje += $"\nSe debe realizar un reembolso de ${montoAnticipo:#,##0.00} al cliente.";
                        }
                        else
                        {
                            mensaje += "\nNo se realizará reembolso al cliente debido a cancelación tardía.";
                        }

                        MessageBox.Show(mensaje, "Cancelación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Limpiar la pantalla para una nueva búsqueda
                        LimpiarPantalla();
                    }
                    catch (Exception ex)
                    {
                        // Si hay error, revertir la transacción
                        transaction.Rollback();
                        throw new Exception($"Error en la transacción de cancelación: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al realizar la cancelación: {ex.Message}");
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            // Limpiar la pantalla y cancelar la operación
            LimpiarPantalla();
        }

        // Método para verificar cancelaciones automáticas
        // Este método se llamaría desde el formulario principal o al iniciar la aplicación
        public static void VerificarCancelacionesAutomaticas()
        {
            // Evitar verificar múltiples veces en el mismo día
            if (cancelacionesAutomaticasVerificadasHoy)
                return;

            try
            {
                // Consulta para identificar reservaciones que debían hacer check-in ayer o antes y no lo hicieron
                string query = @"
                    UPDATE Reservaciones
                    SET EstadoReservacion = 'Cancelada',
                        FechaCancelacion = GETDATE(),
                        MotivosCancelacion = 'Cancelación automática por no presentarse al check-in.'
                    WHERE FechaCheckIn < CAST(GETDATE() AS DATE)
                    AND EstadoReservacion = 'Confirmada'";

                // Ejecutar la consulta
                int filas = Database.ExecuteNonQuery(query, null);

                // Si se cancelaron reservaciones, actualizar estados de habitaciones
                if (filas > 0)
                {
                    string queryHabitaciones = @"
                        UPDATE h
                        SET h.Estado = 'Disponible'
                        FROM Habitaciones h
                        INNER JOIN DetalleReservaciones dr ON h.IdHabitacion = dr.IdHabitacion
                        INNER JOIN Reservaciones r ON dr.IdReservacion = r.IdReservacion
                        WHERE r.EstadoReservacion = 'Cancelada'
                        AND r.MotivosCancelacion = 'Cancelación automática por no presentarse al check-in.'
                        AND r.FechaCancelacion = CAST(GETDATE() AS DATE)";

                    Database.ExecuteNonQuery(queryHabitaciones, null);

                    // Registrar en el log o mostrar mensaje
                    Console.WriteLine($"Se cancelaron automáticamente {filas} reservaciones por no presentarse al check-in.");
                }
                cancelacionesAutomaticasVerificadasHoy = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar cancelaciones automáticas: {ex.Message}");
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

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            LoginForm login = new LoginForm();
            login.Show();
            this.Close();
        }
    }
}
