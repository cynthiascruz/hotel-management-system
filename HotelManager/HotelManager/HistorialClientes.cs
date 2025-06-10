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

namespace HotelManager
{
    public partial class HistorialClientes : Form
    {
        private int idClienteSeleccionado = 0;
        public HistorialClientes()
        {
            InitializeComponent();
            ConfigurarControles();
            CargarTodosLosClientes();
        }

        private void ConfigurarControles()
        {
            // Configurar ComboBox de años
            cboAño.Items.Clear();
            cboAño.Items.Add("-- Todos los años --");

            // Agregar años desde 2020 hasta el año actual
            int añoActual = DateTime.Now.Year;
            for (int año = añoActual; año >= 2020; año--)
            {
                cboAño.Items.Add(año.ToString());
            }

            cboAño.SelectedIndex = 0; // Seleccionar "Todos los años" por defecto

            // Configurar radio buttons
            rbAñoActual.Checked = true;
            rbHistorialCompleto.Checked = false;

            // Configuración de los DataGridViews
            ConfigurarDataGridViewResultados();
            ConfigurarDataGridViewHistorial();

            // Aplicar estilos
            EstiloDataGridViewResultados();
            EstiloDataGridViewHistorial();

            // Deshabilitar botones inicialmente
            btnBuscarHistorial.Enabled = false;
        }

        // Método para cargar todos los clientes al iniciar
        private void CargarTodosLosClientes()
        {
            try
            {
                string query = @"
                    SELECT TOP 500
                        IdCliente,
                        CONCAT(Nombre, ' ', ApellidoPaterno, ' ', ApellidoMaterno) AS NombreCompleto,
                        RFC,
                        Correo
                    FROM Clientes
                    ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                DataTable dtClientes = Database.ExecuteQuery(query, null);

                // Mostrar resultados en el DataGridView
                dgvResultados.DataSource = dtClientes;

                // Volver a aplicar estilos después de cargar datos
                EstiloDataGridViewResultados();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar la lista de clientes: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarDataGridViewResultados()
        {
            dgvResultados.AutoGenerateColumns = false;
            dgvResultados.ReadOnly = true;
            dgvResultados.AllowUserToAddRows = false;
            dgvResultados.AllowUserToDeleteRows = false;
            dgvResultados.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResultados.MultiSelect = false;
            dgvResultados.RowHeadersVisible = false;
            dgvResultados.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Limpiar columnas existentes
            dgvResultados.Columns.Clear();

            // Agregar columnas
            DataGridViewTextBoxColumn colNombre = new DataGridViewTextBoxColumn();
            colNombre.DataPropertyName = "NombreCompleto";
            colNombre.HeaderText = "Nombre Completo";
            colNombre.Name = "NombreCompleto";
            colNombre.Width = 200;
            dgvResultados.Columns.Add(colNombre);

            DataGridViewTextBoxColumn colRFC = new DataGridViewTextBoxColumn();
            colRFC.DataPropertyName = "RFC";
            colRFC.HeaderText = "RFC";
            colRFC.Name = "RFC";
            colRFC.Width = 100;
            dgvResultados.Columns.Add(colRFC);

            DataGridViewTextBoxColumn colCorreo = new DataGridViewTextBoxColumn();
            colCorreo.DataPropertyName = "Correo";
            colCorreo.HeaderText = "Correo Electrónico";
            colCorreo.Name = "Correo";
            colCorreo.Width = 200;
            dgvResultados.Columns.Add(colCorreo);

            // Columna oculta para el ID
            DataGridViewTextBoxColumn colIdCliente = new DataGridViewTextBoxColumn();
            colIdCliente.DataPropertyName = "IdCliente";
            colIdCliente.Name = "IdCliente";
            colIdCliente.Visible = false;
            dgvResultados.Columns.Add(colIdCliente);
        }

        private void ConfigurarDataGridViewHistorial()
        {
            dgvHistorial.AutoGenerateColumns = false;
            dgvHistorial.ReadOnly = true;
            dgvHistorial.AllowUserToAddRows = false;
            dgvHistorial.AllowUserToDeleteRows = false;
            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorial.MultiSelect = false;
            dgvHistorial.RowHeadersVisible = false;
            dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Limpiar columnas existentes
            dgvHistorial.Columns.Clear();

            // Agregar las columnas requeridas
            dgvHistorial.Columns.Add(CreateColumn("NombreCliente", "Nombre del Cliente", 150));
            dgvHistorial.Columns.Add(CreateColumn("Ciudad", "Ciudad", 80));
            dgvHistorial.Columns.Add(CreateColumn("Hotel", "Hotel", 100));

            // Columna para todas las habitaciones concatenadas (TipoHabitacion ahora contiene todas)
            DataGridViewTextBoxColumn colHabitaciones = CreateColumn("TipoHabitacion", "Habitaciones", 250);
            dgvHistorial.Columns.Add(colHabitaciones);

            // Eliminamos la columna de número de habitación ya que está incluida en la lista
            // La mantenemos vacía en el query para compatibilidad, pero la ocultamos
            DataGridViewTextBoxColumn colNumHab = CreateColumn("NumeroHabitacion", "Número", 50);
            colNumHab.Visible = false;
            dgvHistorial.Columns.Add(colNumHab);

            dgvHistorial.Columns.Add(CreateColumn("NumeroPersonas", "Personas", 60));
            dgvHistorial.Columns.Add(CreateColumn("CodigoReservacion", "Código de Reserva", 150));

            // Columnas de fechas
            DataGridViewTextBoxColumn colFechaReservacion = CreateColumn("FechaReservacion", "Fecha Reserva", 100);
            colFechaReservacion.DefaultCellStyle.Format = "dd-MMM-yyyy";
            dgvHistorial.Columns.Add(colFechaReservacion);

            DataGridViewTextBoxColumn colFechaCheckIn = CreateColumn("FechaCheckIn", "Fecha Check-In", 100);
            colFechaCheckIn.DefaultCellStyle.Format = "dd-MMM-yyyy";
            dgvHistorial.Columns.Add(colFechaCheckIn);

            DataGridViewTextBoxColumn colFechaCheckOut = CreateColumn("FechaCheckOut", "Fecha Check-Out", 100);
            colFechaCheckOut.DefaultCellStyle.Format = "dd-MMM-yyyy";
            dgvHistorial.Columns.Add(colFechaCheckOut);

            // Columnas adicionales
            dgvHistorial.Columns.Add(CreateColumn("EstadoReservacion", "Estatus", 80));

            // Columnas monetarias
            DataGridViewTextBoxColumn colAnticipo = CreateColumn("MontoAnticipo", "Anticipo", 80);
            colAnticipo.DefaultCellStyle.Format = "$#,##0.00";
            colAnticipo.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvHistorial.Columns.Add(colAnticipo);

            DataGridViewTextBoxColumn colMontoHospedaje = CreateColumn("MontoHospedaje", "Monto Hospedaje", 100);
            colMontoHospedaje.DefaultCellStyle.Format = "$#,##0.00";
            colMontoHospedaje.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvHistorial.Columns.Add(colMontoHospedaje);

            DataGridViewTextBoxColumn colMontoServicios = CreateColumn("MontoServicios", "Servicios Adicionales", 100);
            colMontoServicios.DefaultCellStyle.Format = "$#,##0.00";
            colMontoServicios.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvHistorial.Columns.Add(colMontoServicios);

            DataGridViewTextBoxColumn colTotalFactura = CreateColumn("TotalFactura", "Total Factura", 100);
            colTotalFactura.DefaultCellStyle.Format = "$#,##0.00";
            colTotalFactura.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            dgvHistorial.Columns.Add(colTotalFactura);

            // Columna oculta para el ID de reservación
            DataGridViewTextBoxColumn colIdReservacion = new DataGridViewTextBoxColumn();
            colIdReservacion.DataPropertyName = "IdReservacion";
            colIdReservacion.Name = "IdReservacion";
            colIdReservacion.Visible = false;
            dgvHistorial.Columns.Add(colIdReservacion);
        }

        private DataGridViewTextBoxColumn CreateColumn(string propertyName, string headerText, int width)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.DataPropertyName = propertyName;
            column.HeaderText = headerText;
            column.Name = propertyName;
            column.Width = width;
            return column;
        }

        private void LimpiarDatos()
        {
            // Limpiar campos de búsqueda
            txtRFC.Clear();
            txtApellidos.Clear();
            txtCorreo.Clear();

            // Limpiar campos del cliente seleccionado
            lblCliente.Text = "";
            lblRFC.Text = "";
            lblCorreo.Text = "";

            // Limpiar DataGridView de historial
            dgvHistorial.DataSource = null;

            // Resetear ID del cliente seleccionado
            idClienteSeleccionado = 0;

            // Deshabilitar botón de búsqueda de historial
            btnBuscarHistorial.Enabled = false;

            // Volver a cargar todos los clientes
            CargarTodosLosClientes();
        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void panel4_Paint(object sender, PaintEventArgs e)
        {

        }

        private void HistorialClientes_Load(object sender, EventArgs e)
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
        }

        private void btnBuscarCliente_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar que al menos un campo de búsqueda tenga valor
                if (string.IsNullOrWhiteSpace(txtRFC.Text) &&
                    string.IsNullOrWhiteSpace(txtApellidos.Text) &&
                    string.IsNullOrWhiteSpace(txtCorreo.Text))
                {
                    MessageBox.Show("Por favor, ingrese al menos un criterio de búsqueda.",
                        "Criterio requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Construir la consulta SQL base
                string query = @"
                    SELECT 
                        IdCliente,
                        CONCAT(Nombre, ' ', ApellidoPaterno, ' ', ApellidoMaterno) AS NombreCompleto,
                        RFC,
                        Correo
                    FROM Clientes
                    WHERE 1=1";

                List<SqlParameter> parameters = new List<SqlParameter>();

                // Agregar condiciones según los campos completados
                if (!string.IsNullOrWhiteSpace(txtRFC.Text))
                {
                    query += " AND RFC LIKE @RFC";
                    parameters.Add(new SqlParameter("@RFC", "%" + txtRFC.Text.Trim() + "%"));
                }

                if (!string.IsNullOrWhiteSpace(txtApellidos.Text))
                {
                    query += " AND (ApellidoPaterno LIKE @Apellido OR ApellidoMaterno LIKE @Apellido)";
                    parameters.Add(new SqlParameter("@Apellido", "%" + txtApellidos.Text.Trim() + "%"));
                }

                if (!string.IsNullOrWhiteSpace(txtCorreo.Text))
                {
                    query += " AND Correo LIKE @Correo";
                    parameters.Add(new SqlParameter("@Correo", "%" + txtCorreo.Text.Trim() + "%"));
                }

                query += " ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                // Ejecutar la consulta
                DataTable dtClientes = Database.ExecuteQuery(query, parameters.ToArray());

                // Verificar si se encontraron resultados
                if (dtClientes.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron clientes con los criterios especificados.",
                        "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Mostrar resultados en el DataGridView
                dgvResultados.DataSource = dtClientes;

                // Si solo hay un cliente, seleccionarlo automáticamente
                if (dtClientes.Rows.Count == 1)
                {
                    SeleccionarCliente(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar clientes: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dgvResultados_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                SeleccionarCliente(e.RowIndex);
            }
        }

        private void SeleccionarCliente(int rowIndex)
        {
            try
            {
                // Guardar ID del cliente seleccionado
                idClienteSeleccionado = Convert.ToInt32(dgvResultados.Rows[rowIndex].Cells["IdCliente"].Value);

                // Mostrar datos del cliente seleccionado
                lblCliente.Text = dgvResultados.Rows[rowIndex].Cells["NombreCompleto"].Value.ToString();
                lblRFC.Text = dgvResultados.Rows[rowIndex].Cells["RFC"].Value.ToString();
                lblCorreo.Text = dgvResultados.Rows[rowIndex].Cells["Correo"].Value.ToString();

                // Habilitar botón de búsqueda de historial
                btnBuscarHistorial.Enabled = true;

                // Cargar historial de reservaciones automáticamente
                CargarHistorialReservaciones();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar cliente: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnBuscarHistorial_Click(object sender, EventArgs e)
        {
            if (idClienteSeleccionado > 0)
            {
                CargarHistorialReservaciones();
            }
            else
            {
                MessageBox.Show("Por favor, primero seleccione un cliente.",
                    "Cliente no seleccionado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CargarHistorialReservaciones()
        {
            try
            {
                // Verificar si hay un cliente seleccionado
                if (idClienteSeleccionado <= 0)
                {
                    return;
                }

                // Consulta para obtener reservaciones con todas sus habitaciones concatenadas
                string query = @"
            WITH ReservacionesCliente AS (
                SELECT 
                    r.IdReservacion,
                    r.CodigoReservacion,
                    CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                    c.Ciudad,
                    h.Nombre AS Hotel,
                    r.CantidadPersonas AS NumeroPersonas,
                    r.FechaReservacion,
                    r.FechaCheckIn,
                    r.FechaCheckOut,
                    r.EstadoReservacion,
                    r.MontoAnticipo
                FROM Reservaciones r
                INNER JOIN Clientes c ON r.IdCliente = c.IdCliente
                INNER JOIN Hoteles h ON r.IdHotel = h.IdHotel
                WHERE r.IdCliente = @IdCliente
            ),
            HabitacionesAgrupadas AS (
                SELECT 
                    dr.IdReservacion,
                    STUFF((
                        SELECT ', ' + th.Nombre + ' (' + hab.NumeroHabitacion + ')'
                        FROM DetalleReservaciones dr2
                        INNER JOIN Habitaciones hab ON dr2.IdHabitacion = hab.IdHabitacion
                        INNER JOIN TiposHabitacion th ON hab.IdTipoHabitacion = th.IdTipoHabitacion
                        WHERE dr2.IdReservacion = dr.IdReservacion
                        ORDER BY hab.NumeroHabitacion
                        FOR XML PATH(''), TYPE
                    ).value('.', 'VARCHAR(MAX)'), 1, 2, '') AS HabitacionesInfo,
                    SUM(dr.PrecioPorNoche * DATEDIFF(DAY, r.FechaCheckIn, r.FechaCheckOut)) AS MontoHospedaje
                FROM DetalleReservaciones dr
                INNER JOIN Habitaciones hab ON dr.IdHabitacion = hab.IdHabitacion
                INNER JOIN TiposHabitacion th ON hab.IdTipoHabitacion = th.IdTipoHabitacion
                INNER JOIN Reservaciones r ON dr.IdReservacion = r.IdReservacion
                GROUP BY dr.IdReservacion
            ),
            ServiciosAgrupados AS (
                SELECT 
                    IdReservacion,
                    SUM(Precio * Cantidad) AS MontoServicios
                FROM ConsumoServicios
                GROUP BY IdReservacion
            )
            SELECT 
                rc.IdReservacion,
                rc.NombreCliente,
                rc.Ciudad,
                rc.Hotel,
                ha.HabitacionesInfo AS TipoHabitacion,
                '' AS NumeroHabitacion, -- No se necesita columna separada
                rc.NumeroPersonas,
                CONVERT(VARCHAR(50), rc.CodigoReservacion) AS CodigoReservacion,
                rc.FechaReservacion,
                rc.FechaCheckIn,
                rc.FechaCheckOut,
                rc.EstadoReservacion,
                rc.MontoAnticipo,
                ha.MontoHospedaje,
                ISNULL(sa.MontoServicios, 0) AS MontoServicios,
                ISNULL(f.Total, 0) AS TotalFactura
            FROM ReservacionesCliente rc
            LEFT JOIN HabitacionesAgrupadas ha ON rc.IdReservacion = ha.IdReservacion
            LEFT JOIN ServiciosAgrupados sa ON rc.IdReservacion = sa.IdReservacion
            LEFT JOIN Facturas f ON rc.IdReservacion = f.IdReservacion";

                // Versión alternativa para SQL Server más antiguo (sin STUFF y FOR XML)
                string queryAlternativa = @"
            WITH ReservacionesInfo AS (
                SELECT 
                    r.IdReservacion,
                    r.CodigoReservacion,
                    CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                    c.Ciudad,
                    h.Nombre AS Hotel,
                    r.CantidadPersonas AS NumeroPersonas,
                    r.FechaReservacion,
                    r.FechaCheckIn,
                    r.FechaCheckOut,
                    r.EstadoReservacion,
                    r.MontoAnticipo,
                    (SELECT 
                        SUM(dr.PrecioPorNoche * DATEDIFF(DAY, r.FechaCheckIn, r.FechaCheckOut)) 
                     FROM DetalleReservaciones dr 
                     WHERE dr.IdReservacion = r.IdReservacion) AS MontoHospedaje,
                    (SELECT 
                        SUM(cs.Precio * cs.Cantidad) 
                     FROM ConsumoServicios cs 
                     WHERE cs.IdReservacion = r.IdReservacion) AS MontoServicios,
                    (SELECT TOP 1 
                        f.Total 
                     FROM Facturas f 
                     WHERE f.IdReservacion = r.IdReservacion) AS TotalFactura,
                    -- Concatenamos las primeras 3 habitaciones para evitar strings demasiado largos
                    (SELECT TOP 3
                        th.Nombre + ' (' + hab.NumeroHabitacion + ')' + CASE WHEN ROW_NUMBER() OVER (ORDER BY hab.NumeroHabitacion) < 3 THEN ', ' ELSE '' END
                     FROM DetalleReservaciones dr
                     INNER JOIN Habitaciones hab ON dr.IdHabitacion = hab.IdHabitacion
                     INNER JOIN TiposHabitacion th ON hab.IdTipoHabitacion = th.IdTipoHabitacion
                     WHERE dr.IdReservacion = r.IdReservacion
                     ORDER BY hab.NumeroHabitacion
                     FOR JSON PATH) AS HabitacionesJSON
                FROM Reservaciones r
                INNER JOIN Clientes c ON r.IdCliente = c.IdCliente
                INNER JOIN Hoteles h ON r.IdHotel = h.IdHotel
                WHERE r.IdCliente = @IdCliente
            )
            SELECT 
                ri.IdReservacion,
                ri.NombreCliente,
                ri.Ciudad,
                ri.Hotel,
                
                COALESCE(
                    (SELECT 
                        STRING_AGG(CONCAT(th.Nombre, ' (', hab.NumeroHabitacion, ')'), ', ') 
                     FROM DetalleReservaciones dr
                     INNER JOIN Habitaciones hab ON dr.IdHabitacion = hab.IdHabitacion
                     INNER JOIN TiposHabitacion th ON hab.IdTipoHabitacion = th.IdTipoHabitacion
                     WHERE dr.IdReservacion = ri.IdReservacion), 
                    'Sin habitaciones'
                ) AS TipoHabitacion,
                '' AS NumeroHabitacion,
                ri.NumeroPersonas,
                CONVERT(VARCHAR(50), ri.CodigoReservacion) AS CodigoReservacion,
                ri.FechaReservacion,
                ri.FechaCheckIn,
                ri.FechaCheckOut,
                ri.EstadoReservacion,
                ri.MontoAnticipo,
                ISNULL(ri.MontoHospedaje, 0) AS MontoHospedaje,
                ISNULL(ri.MontoServicios, 0) AS MontoServicios,
                ISNULL(ri.TotalFactura, 0) AS TotalFactura
            FROM ReservacionesInfo ri";

                List<SqlParameter> parameters = new List<SqlParameter>();
                parameters.Add(new SqlParameter("@IdCliente", idClienteSeleccionado));

                // Aplicar filtro de año si es necesario
                if (rbAñoActual.Checked)
                {
                    query += " WHERE YEAR(rc.FechaReservacion) = @Año";
                    queryAlternativa += " WHERE YEAR(ri.FechaReservacion) = @Año";
                    parameters.Add(new SqlParameter("@Año", DateTime.Now.Year));
                }
                else if (cboAño.SelectedIndex > 0)
                {
                    query += " WHERE YEAR(rc.FechaReservacion) = @Año";
                    queryAlternativa += " WHERE YEAR(ri.FechaReservacion) = @Año";
                    parameters.Add(new SqlParameter("@Año", Convert.ToInt32(cboAño.SelectedItem)));
                }

                // Ordenar por fecha de reservación descendente
                query += " ORDER BY rc.FechaReservacion DESC";
                queryAlternativa += " ORDER BY ri.FechaReservacion DESC";

                // Ejecutar la consulta (probando primero la principal y luego la alternativa si falla)
                DataTable dtHistorial;
                try
                {
                    dtHistorial = Database.ExecuteQuery(query, parameters.ToArray());
                }
                catch (Exception ex1)
                {
                    // Si la primera consulta falla, intentar con la alternativa
                    try
                    {
                        dtHistorial = Database.ExecuteQuery(queryAlternativa, parameters.ToArray());
                    }
                    catch (Exception ex2)
                    {
                        throw new Exception($"Error al ejecutar consultas: {ex1.Message}\nConsulta alternativa: {ex2.Message}");
                    }
                }

                // Mostrar resultados en el DataGridView
                dgvHistorial.DataSource = dtHistorial;

                // Volver a aplicar estilos después de cargar datos
                EstiloDataGridViewHistorial();

                // Ajustar ancho de columna de tipos de habitación para que quepan todas
                if (dgvHistorial.Columns["TipoHabitacion"] != null)
                {
                    dgvHistorial.Columns["TipoHabitacion"].Width = 250;
                }

                // Verificar si hay resultados
                if (dtHistorial.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron reservaciones para este cliente con los filtros aplicados.",
                        "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar el historial de reservaciones: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarDatos();
        }

        private void rbAñoActual_CheckedChanged(object sender, EventArgs e)
        {
            cboAño.Enabled = !rbAñoActual.Checked;

            // Si está seleccionado "Año actual", deshabilitamos el ComboBox
            if (rbAñoActual.Checked)
            {
                cboAño.SelectedIndex = 0;
            }

            // Si hay un cliente seleccionado, recargar el historial
            if (idClienteSeleccionado > 0)
            {
                CargarHistorialReservaciones();
            }
        }

        private void rbHistorialCompleto_CheckedChanged(object sender, EventArgs e)
        {
            cboAño.Enabled = !rbAñoActual.Checked;

            // Si hay un cliente seleccionado, recargar el historial
            if (idClienteSeleccionado > 0)
            {
                CargarHistorialReservaciones();
            }
        }

        private void cboAño_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Si hay un cliente seleccionado, recargar el historial
            if (idClienteSeleccionado > 0)
            {
                CargarHistorialReservaciones();
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

        private void btnReportesOcupacion_Click(object sender, EventArgs e)
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

        private void EstiloDataGridViewResultados()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvResultados.BorderStyle = BorderStyle.None;
            dgvResultados.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvResultados.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvResultados.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvResultados.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvResultados.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvResultados.EnableHeadersVisualStyles = false;
            dgvResultados.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvResultados.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvResultados.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResultados.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvResultados.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvResultados.ColumnHeadersHeight = 30;

            // Estilo para las filas y celdas
            dgvResultados.RowTemplate.Height = 25;
            dgvResultados.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvResultados.DefaultCellStyle.Padding = new Padding(3);
            dgvResultados.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvResultados.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvResultados.BorderStyle = BorderStyle.FixedSingle;
            dgvResultados.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvResultados.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResultados.MultiSelect = false;
        }

        private void EstiloDataGridViewHistorial()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvHistorial.BorderStyle = BorderStyle.None;
            dgvHistorial.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvHistorial.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvHistorial.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvHistorial.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvHistorial.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvHistorial.EnableHeadersVisualStyles = false;
            dgvHistorial.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvHistorial.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvHistorial.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHistorial.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 8, FontStyle.Bold); // Fuente más pequeña para encabezados
            dgvHistorial.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHistorial.ColumnHeadersHeight = 28; // Altura ligeramente reducida para esta tabla que tiene muchas columnas

            // Estilo para las filas y celdas
            dgvHistorial.RowTemplate.Height = 24; // Altura ligeramente reducida
            dgvHistorial.DefaultCellStyle.Font = new Font("Yu Gothic", 7.5f); // Fuente más pequeña para el historial
            dgvHistorial.DefaultCellStyle.Padding = new Padding(2);
            dgvHistorial.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvHistorial.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells; // Usar DisplayedCells para tablas con muchas columnas

            // Añadir un borde fino alrededor de la tabla
            dgvHistorial.BorderStyle = BorderStyle.FixedSingle;
            dgvHistorial.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvHistorial.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHistorial.MultiSelect = false;

            // Ajustar estilos específicos para cada tipo de columna
            foreach (DataGridViewColumn col in dgvHistorial.Columns)
            {
                // Ajustar columnas de moneda
                if (col.Name.Contains("Monto") || col.Name.Contains("Total") ||
                    col.Name.Contains("Anticipo") || col.Name.Contains("Hospedaje") ||
                    col.Name.Contains("Adicional") || col.Name.Contains("Factura") ||
                    col.Name.Contains("Servicios"))
                {
                    col.DefaultCellStyle.Format = "C2";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                    // Destacar la columna de Total Factura
                    if (col.Name.Contains("TotalFactura"))
                    {
                        col.DefaultCellStyle.Font = new Font("Yu Gothic", 7.5f, FontStyle.Bold);
                        col.DefaultCellStyle.BackColor = Color.FromArgb(245, 235, 235);
                    }
                }

                // Ajustar columnas de fecha
                if (col.Name.Contains("Fecha"))
                {
                    col.DefaultCellStyle.Format = "dd-MMM-yyyy";
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Ajustar columna de estatus
                if (col.Name.Contains("Estatus"))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }

                // Ajustar columna de personas
                if (col.Name.Contains("Personas"))
                {
                    col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            }

            // Asegurar que columnas críticas sean visibles y tengan ancho adecuado
            if (dgvHistorial.Columns.Contains("TipoHabitacion"))
            {
                dgvHistorial.Columns["TipoHabitacion"].MinimumWidth = 180;
            }

            if (dgvHistorial.Columns.Contains("CodigoReservacion"))
            {
                dgvHistorial.Columns["CodigoReservacion"].MinimumWidth = 120;
            }
        }
    }
}
