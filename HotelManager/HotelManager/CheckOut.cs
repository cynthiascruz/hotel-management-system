using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManager.Classes;
using HotelManager.Data;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Kernel.Colors;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout.Borders;
using HotelManager.Forms;

namespace HotelManager
{
    public partial class CheckOut : Form
    {

        // Variables
        private int idReservacionActual = 0;
        private int idHotelActual = 0;
        private decimal subtotalHospedaje = 0;
        private decimal subtotalServicios = 0;
        private decimal anticipoPagado = 0;
        private decimal descuento = 0;
        private DateTime fechaActual = DateTime.Now;
        private bool serviciosGuardados = false;


        // Lista para almacenar los servicios adicionales seleccionados
        private List<ServicioAdicional> serviciosSeleccionados = new List<ServicioAdicional>();

        // Clase para gestionar los servicios adicionales
        private class ServicioAdicional
        {
            public int IdServicio { get; set; }
            public string Nombre { get; set; }
            public decimal Precio { get; set; }
            public int Cantidad { get; set; }
            public decimal Subtotal => Precio * Cantidad;
        }

        public CheckOut()
        {
            InitializeComponent();
            InicializarControles();
        }

        private void InicializarControles()
        {
            // Limpiar campos
            txtCodigoReservacion.Clear();
            txtNuevoServicio.Clear();
            txtPrecio.Clear();
            LimpiarDatosReservacion();

            // DataGridView de servicios
            ConfigurarDataGridViewServicios();

            // Estado inicial de botones
            btnBuscar.Enabled = true;
            btnGenerarFactura.Enabled = false;
            btnCancelar.Enabled = true;
            btnAgregarServicio.Enabled = false;
            btnEliminarServicio.Enabled = false;
            btnAplicarDescuento.Enabled = false;

            // Configuración de controles numéricos
            numCantidad.Minimum = 1;
            numCantidad.Maximum = 100;
            numCantidad.Value = 1;

            numDescuento.Minimum = 0;
            numDescuento.Maximum = 100;
            numDescuento.Value = 0;

            // Deshabilitar controles de servicios hasta tener una reservación
            txtNuevoServicio.Enabled = false;
            txtPrecio.Enabled = false;
            numCantidad.Enabled = false;
            cboServicios.Enabled = false;
            numDescuento.Enabled = false;

            // Establecer el foco inicial
            txtCodigoReservacion.Focus();
        }

        private void ConfigurarDataGridViewServicios()
        {
            // Configurar DataGridView para mostrar servicios
            dgvServicios.AutoGenerateColumns = false;

            // Limpiar columnas
            dgvServicios.Columns.Clear();

            // Añadir columnas
            DataGridViewTextBoxColumn colIdServicio = new DataGridViewTextBoxColumn();
            colIdServicio.DataPropertyName = "IdServicio";
            colIdServicio.HeaderText = "ID";
            colIdServicio.Visible = false;
            dgvServicios.Columns.Add(colIdServicio);

            DataGridViewTextBoxColumn colNombre = new DataGridViewTextBoxColumn();
            colNombre.DataPropertyName = "Nombre";
            colNombre.HeaderText = "Servicio";
            colNombre.Width = 200;
            dgvServicios.Columns.Add(colNombre);

            DataGridViewTextBoxColumn colPrecio = new DataGridViewTextBoxColumn();
            colPrecio.DataPropertyName = "Precio";
            colPrecio.HeaderText = "Precio";
            colPrecio.Width = 100;
            colPrecio.DefaultCellStyle.Format = "C2";
            dgvServicios.Columns.Add(colPrecio);

            DataGridViewTextBoxColumn colCantidad = new DataGridViewTextBoxColumn();
            colCantidad.DataPropertyName = "Cantidad";
            colCantidad.HeaderText = "Cantidad";
            colCantidad.Width = 70;
            dgvServicios.Columns.Add(colCantidad);

            DataGridViewTextBoxColumn colSubtotal = new DataGridViewTextBoxColumn();
            colSubtotal.DataPropertyName = "Subtotal";
            colSubtotal.HeaderText = "Subtotal";
            colSubtotal.Width = 100;
            colSubtotal.DefaultCellStyle.Format = "C2";
            dgvServicios.Columns.Add(colSubtotal);

            // Configuraciones adicionales
            dgvServicios.AllowUserToAddRows = false;
            dgvServicios.AllowUserToDeleteRows = false;
            dgvServicios.ReadOnly = true;
            dgvServicios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvServicios.MultiSelect = false;

            // Limpiar filas
            dgvServicios.Rows.Clear();
        }

        private void LimpiarDatosReservacion()
        {
            // Limpiar datos de la reservación
            lblCliente.Text = string.Empty;
            lblHotel.Text = string.Empty;
            lblHabitacion.Text = string.Empty;
            lblCheckIn.Text = string.Empty;
            lblCheckOut.Text = string.Empty;
            lblAnticipoPagado.Text = string.Empty;

            // Limpiar datos de facturación
            lblSubtotalHospedaje.Text = "$0.00";
            lblAnticipoRequerido.Text = "$0.00";
            lblSubtotalServicios.Text = "$0.00";
            lblTotalPagar.Text = "$0.00";

            // Limpiar variables
            idReservacionActual = 0;
            subtotalHospedaje = 0;
            subtotalServicios = 0;
            anticipoPagado = 0;
            descuento = 0;

            txtNuevoServicio.Clear();
            txtPrecio.Clear();

            // Limpiar lista de servicios
            serviciosSeleccionados.Clear();
            dgvServicios.Rows.Clear();

            // Resetear combos y numerics
            cboServicios.SelectedIndex = -1;
            numCantidad.Value = 1;
            numDescuento.Value = 0;

            // Deshabilitar controles
            btnAgregarServicio.Enabled = false;
            btnGenerarFactura.Enabled = false;
        }

        private void CheckOut_Load(object sender, EventArgs e)
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
                LimpiarDatosReservacion();

                // Consulta principal para obtener datos de la reservación
                string query = @"
                    SELECT r.IdReservacion, r.CodigoReservacion, 
                           CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                           h.Nombre AS NombreHotel, h.IdHotel,
                           r.FechaCheckIn, r.FechaCheckOut, 
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
                    idHotelActual = Convert.ToInt32(row["IdHotel"]);
                    string estadoReservacion = row["EstadoReservacion"].ToString();

                    // Validar el estado de la reservación
                    if (!ValidarEstadoReservacion(estadoReservacion))
                    {
                        return;
                    }

                    // Mostrar los datos de la reservación
                    MostrarDatosReservacion(row);

                    // Obtener habitaciones asignadas a la reservación
                    ObtenerHabitacionesReservacion(idReservacionActual);

                    // Cargar servicios disponibles para el hotel
                    CargarServiciosDisponibles();

                    // Cargar servicios ya consumidos (si existen)
                    CargarServiciosConsumidos(idReservacionActual);

                    // Calcular totales
                    CalcularTotales();

                    // Habilitar controles para servicios
                    btnAgregarServicio.Enabled = true;
                    btnEliminarServicio.Enabled = true;
                    btnAplicarDescuento.Enabled = true;
                    btnGenerarFactura.Enabled = true;
                    txtNuevoServicio.Enabled = true;
                    txtPrecio.Enabled = true;
                    numCantidad.Enabled = true;
                    cboServicios.Enabled = true;
                    numDescuento.Enabled = true;
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

        // Validar el estado de la reservación
        private bool ValidarEstadoReservacion(string estadoReservacion)
        {
            // Para CheckOut, solo se permiten reservaciones con estado CheckIn
            if (estadoReservacion != "CheckIn")
            {
                string mensaje;

                if (estadoReservacion == "Confirmada")
                {
                    mensaje = "Esta reservación aún no ha realizado el Check-In.\n" +
                              "Por favor, realice primero el proceso de Check-In.";
                }
                else if (estadoReservacion == "CheckOut")
                {
                    mensaje = "Esta reservación ya ha realizado el Check-Out.";
                }
                else if (estadoReservacion == "Cancelada")
                {
                    mensaje = "Esta reservación ha sido cancelada.";
                }
                else
                {
                    mensaje = $"Esta reservación tiene un estado no válido: {estadoReservacion}";
                }

                MessageBox.Show(mensaje, "Estado incorrecto",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                LimpiarDatosReservacion();
                return false;
            }

            return true;
        }

        // Mostrar los datos de la reservación
        private void MostrarDatosReservacion(DataRow row)
        {
            lblCliente.Text = row["NombreCliente"].ToString();
            lblHotel.Text = row["NombreHotel"].ToString();

            // Las habitaciones se mostrarán en el método ObtenerHabitacionesReservacion
            lblCheckIn.Text = Convert.ToDateTime(row["FechaCheckIn"]).ToString("dd-MMM-yyyy");
            lblCheckOut.Text = Convert.ToDateTime(row["FechaCheckOut"]).ToString("dd-MMM-yyyy");

            // Calcular anticipo y mostrarlo
            anticipoPagado = Convert.ToDecimal(row["MontoAnticipo"]);
            lblAnticipoPagado.Text = string.Format("${0:#,##0.00}", anticipoPagado);

            // Calcular subtotal hospedaje
            CalcularSubtotalHospedaje(idReservacionActual);
        }

        // Obtener las habitaciones asignadas a la reservación
        private void ObtenerHabitacionesReservacion(int idReservacion)
        {
            try
            {
                string query = @"
                    SELECT h.NumeroHabitacion, th.Nombre AS TipoHabitacion
                    FROM DetalleReservaciones dr
                    INNER JOIN Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
                    INNER JOIN TiposHabitacion th ON h.IdTipoHabitacion = th.IdTipoHabitacion
                    WHERE dr.IdReservacion = @IdReservacion
                    ORDER BY h.NumeroHabitacion";

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
                        sb.Append(row["TipoHabitacion"]);
                        sb.Append(" (");
                        sb.Append(row["NumeroHabitacion"]);
                        sb.Append("), ");
                    }

                    // Remover la última coma y espacio
                    if (sb.Length > 2)
                    {
                        sb.Length -= 2;
                    }

                    lblHabitacion.Text = sb.ToString();
                }
                else
                {
                    lblHabitacion.Text = "Sin habitaciones asignadas";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener habitaciones: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblHabitacion.Text = "Error al cargar habitaciones";
            }
        }

        // Calcular el subtotal de hospedaje
        private void CalcularSubtotalHospedaje(int idReservacion)
        {
            try
            {
                // Esta consulta calculará el costo total de las habitaciones por los días de estancia
                string query = @"
                    SELECT SUM(dr.PrecioPorNoche * DATEDIFF(DAY, r.FechaCheckIn, r.FechaCheckOut)) AS SubtotalHospedaje
                    FROM DetalleReservaciones dr
                    INNER JOIN Reservaciones r ON dr.IdReservacion = r.IdReservacion
                    WHERE dr.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacion)
                };

                DataTable dt = Database.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0 && dt.Rows[0]["SubtotalHospedaje"] != DBNull.Value)
                {
                    subtotalHospedaje = Convert.ToDecimal(dt.Rows[0]["SubtotalHospedaje"]);
                    lblSubtotalHospedaje.Text = string.Format("${0:#,##0.00}", subtotalHospedaje);
                }
                else
                {
                    subtotalHospedaje = 0;
                    lblSubtotalHospedaje.Text = "$0.00";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular subtotal de hospedaje: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                subtotalHospedaje = 0;
                lblSubtotalHospedaje.Text = "$0.00";
            }
        }

        // Cargar los servicios disponibles para el hotel en el ComboBox
        private void CargarServiciosDisponibles()
        {
            try
            {
                // Consulta simple para obtener TODOS los servicios
                string query = @"
            SELECT DISTINCT sa.IdServicio, sa.Nombre, sa.Precio
            FROM ServiciosAdicionales sa
            WHERE sa.Estado = 1
            ORDER BY sa.Nombre";

                DataTable dtServicios = Database.ExecuteQuery(query, null);

                // Configurar el ComboBox
                DataTable dtCombo = new DataTable();
                dtCombo.Columns.Add("IdServicio", typeof(int));
                dtCombo.Columns.Add("Nombre", typeof(string));
                dtCombo.Columns.Add("Precio", typeof(decimal));
                dtCombo.Columns.Add("DisplayText", typeof(string));

                // Añadir el ítem inicial
                DataRow rowInicial = dtCombo.NewRow();
                rowInicial["IdServicio"] = -1;
                rowInicial["Nombre"] = "-- Seleccione un servicio --";
                rowInicial["Precio"] = 0;
                rowInicial["DisplayText"] = "-- Seleccione un servicio --";
                dtCombo.Rows.Add(rowInicial);

                // Agregar todos los servicios únicos
                foreach (DataRow row in dtServicios.Rows)
                {
                    DataRow newRow = dtCombo.NewRow();
                    newRow["IdServicio"] = Convert.ToInt32(row["IdServicio"]);
                    newRow["Nombre"] = row["Nombre"].ToString();
                    newRow["Precio"] = Convert.ToDecimal(row["Precio"]);
                    newRow["DisplayText"] = $"{row["Nombre"]} - ${Convert.ToDecimal(row["Precio"]):N2}";
                    dtCombo.Rows.Add(newRow);
                }

                // Asignar al ComboBox
                cboServicios.DisplayMember = "DisplayText";
                cboServicios.ValueMember = "IdServicio";
                cboServicios.DataSource = dtCombo;
                cboServicios.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar servicios: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cargar los servicios ya consumidos
        private void CargarServiciosConsumidos(int idReservacion)
        {
            try
            {
                string query = @"
                    SELECT cs.IdServicio, sa.Nombre, cs.Precio, cs.Cantidad
                    FROM ConsumoServicios cs
                    INNER JOIN ServiciosAdicionales sa ON cs.IdServicio = sa.IdServicio
                    WHERE cs.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacion)
                };

                DataTable dtConsumos = Database.ExecuteQuery(query, parameters);

                // Limpiar lista y grid
                serviciosSeleccionados.Clear();
                dgvServicios.Rows.Clear();

                foreach (DataRow row in dtConsumos.Rows)
                {
                    // Crear servicio
                    ServicioAdicional servicio = new ServicioAdicional
                    {
                        IdServicio = Convert.ToInt32(row["IdServicio"]),
                        Nombre = row["Nombre"].ToString(),
                        Precio = Convert.ToDecimal(row["Precio"]),
                        Cantidad = Convert.ToInt32(row["Cantidad"])
                    };

                    // Añadir a la lista
                    serviciosSeleccionados.Add(servicio);

                    // Añadir al grid
                    dgvServicios.Rows.Add(
                        servicio.IdServicio,
                        servicio.Nombre,
                        servicio.Precio,
                        servicio.Cantidad,
                        servicio.Subtotal
                    );
                }

                // Calcular subtotal de servicios
                CalcularSubtotalServicios();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar servicios consumidos: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Calcular subtotal de servicios
        private void CalcularSubtotalServicios()
        {
            subtotalServicios = serviciosSeleccionados.Sum(s => s.Subtotal);
            lblSubtotalServicios.Text = string.Format("${0:#,##0.00}", subtotalServicios);
        }

        // Calcular todos los totales
        private void CalcularTotales()
        {
            // Calcular total a pagar
            decimal total = subtotalHospedaje + subtotalServicios;

            // Guardar el valor del anticipo requerido (30%)
            decimal anticipoRequerido = subtotalHospedaje * 0.3m;
            lblAnticipoRequerido.Text = string.Format("${0:#,##0.00}", anticipoRequerido);

            // Aplicar descuento
            decimal descuentoValor = 0;
            if (descuento > 0)
            {
                descuentoValor = total * (descuento / 100m);
                total -= descuentoValor;
            }

            // Restar anticipo
            total -= anticipoPagado;

            // Si el total es negativo, mostrarlo como 0
            if (total < 0)
                total = 0;

            // Mostrar el total
            lblTotalPagar.Text = string.Format("${0:#,##0.00}", total);
        }

        private void btnAgregarServicio_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si se está agregando un servicio nuevo o existente
                bool servicioNuevo = !string.IsNullOrWhiteSpace(txtNuevoServicio.Text);

                int idServicio;
                string nombreServicio;
                decimal precioServicio;
                int cantidad = (int)numCantidad.Value;

                // Validar cantidad
                if (cantidad <= 0)
                {
                    MessageBox.Show("Por favor, ingrese una cantidad válida.",
                        "Cantidad inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numCantidad.Focus();
                    return;
                }

                if (servicioNuevo)
                {
                    // Obtener datos del nuevo servicio
                    nombreServicio = txtNuevoServicio.Text.Trim();

                    // Validar nombre
                    if (string.IsNullOrWhiteSpace(nombreServicio))
                    {
                        MessageBox.Show("Por favor, ingrese el nombre del servicio.",
                            "Nombre requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtNuevoServicio.Focus();
                        return;
                    }

                    // Validar y convertir precio
                    if (!decimal.TryParse(txtPrecio.Text.Trim(), out precioServicio) || precioServicio <= 0)
                    {
                        MessageBox.Show("Por favor, ingrese un precio válido para el servicio.",
                            "Precio inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        txtPrecio.Focus();
                        return;
                    }

                    // Guardar el nuevo servicio
                    idServicio = GuardarNuevoServicio(nombreServicio, precioServicio);

                    // Limpiar campos
                    txtNuevoServicio.Clear();
                    txtPrecio.Clear();
                }
                else
                {
                    // Verificar selección en ComboBox
                    if (cboServicios.SelectedIndex <= 0)
                    {
                        MessageBox.Show("Por favor, seleccione un servicio de la lista o ingrese uno nuevo.",
                            "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // Obtener servicio seleccionado
                    DataRowView row = (DataRowView)cboServicios.SelectedItem;
                    idServicio = Convert.ToInt32(row["IdServicio"]);
                    nombreServicio = row["Nombre"].ToString();
                    precioServicio = Convert.ToDecimal(row["Precio"]);
                }

                // Guardar el consumo en memoria (NO en la base de datos todavía)
                GuardarConsumoServicio(idServicio, nombreServicio, precioServicio, cantidad);

                // Resetear cantidad
                numCantidad.Value = 1;

                // Mensaje de éxito
                MessageBox.Show($"Se ha agregado {cantidad} {nombreServicio} a la cuenta.",
                    "Servicio agregado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar servicio: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AgregarServicioAlComboBox(int idServicio, string nombre, decimal precio)
        {
            try
            {
                // Verificar si el servicio ya existe en el ComboBox
                bool existeEnCombo = false;
                DataTable dt = (DataTable)cboServicios.DataSource;

                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["IdServicio"]) == idServicio)
                    {
                        existeEnCombo = true;
                        break;
                    }
                }

                // Si no existe, añadirlo
                if (!existeEnCombo)
                {
                    DataRow newRow = dt.NewRow();
                    newRow["IdServicio"] = idServicio;
                    newRow["Nombre"] = nombre;
                    newRow["Precio"] = precio;
                    newRow["DisplayText"] = $"{nombre} - ${precio:N2}";
                    dt.Rows.Add(newRow);

                    // Actualizar el ComboBox manteniendo la selección actual
                    int currentIndex = cboServicios.SelectedIndex;
                    cboServicios.DataSource = null;
                    cboServicios.DataSource = dt;
                    cboServicios.DisplayMember = "DisplayText";
                    cboServicios.ValueMember = "IdServicio";
                    cboServicios.SelectedIndex = currentIndex;
                }
            }
            catch (Exception ex)
            {
                // Solo registrar el error, no interrumpir el flujo principal
                Console.WriteLine($"Error al agregar servicio al ComboBox: {ex.Message}");
            }
        }

        // Guardar un nuevo servicio en la base de datos
        private int GuardarNuevoServicio(string nombre, decimal precio)
        {
            try
            {
                // Verificar si ya existe un servicio con este nombre
                string queryVerificar = @"
            SELECT IdServicio 
            FROM ServiciosAdicionales 
            WHERE Nombre = @Nombre";

                SqlParameter[] parametrosVerificar = new SqlParameter[]
                {
            new SqlParameter("@Nombre", nombre)
                };

                DataTable dtExistente = Database.ExecuteQuery(queryVerificar, parametrosVerificar);

                // Si ya existe, mostrar mensaje y devolver su ID
                if (dtExistente.Rows.Count > 0)
                {
                    int idExistente = Convert.ToInt32(dtExistente.Rows[0]["IdServicio"]);
                    MessageBox.Show($"El servicio '{nombre}' ya existe en el sistema.",
                        "Servicio existente", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return idExistente;
                }

                // Obtener un ID de categoría válido
                int idCategoria = 1; // Por defecto
                string queryCat = "SELECT TOP 1 IdCategoriaServicio FROM CategoriasServiciosAdicionales";
                object resultadoCat = Database.ExecuteScalar(queryCat, null);
                if (resultadoCat != null && resultadoCat != DBNull.Value)
                {
                    idCategoria = Convert.ToInt32(resultadoCat);
                }

                // Insertar el nuevo servicio
                string query = @"
            INSERT INTO ServiciosAdicionales (
                IdHotel, IdCategoriaServicio, Nombre, 
                Descripcion, Precio, Estado, 
                UsuarioRegistro, FechaRegistro
            )
            VALUES (
                @IdHotel, @IdCategoriaServicio, @Nombre, 
                'Servicio creado desde Check-Out', @Precio, 1, 
                @UsuarioRegistro, GETDATE()
            );
            SELECT SCOPE_IDENTITY();";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@IdHotel", idHotelActual),
            new SqlParameter("@IdCategoriaServicio", idCategoria),
            new SqlParameter("@Nombre", nombre),
            new SqlParameter("@Precio", precio),
            new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                };

                // Ejecutar la consulta y obtener el ID generado
                object result = Database.ExecuteScalar(query, parameters);
                int nuevoId = Convert.ToInt32(result);

                // Mensaje de éxito
                MessageBox.Show($"Nuevo servicio '{nombre}' creado con éxito.",
                    "Servicio creado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Recargar servicios para incluir el nuevo
                CargarServiciosDisponibles();

                return nuevoId;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar el servicio: {ex.Message}");
            }
        }

        // Guardar consumo de servicio en la base de datos
        private void GuardarConsumoServicio(int idServicio, string nombreServicio, decimal precio, int cantidad)
        {
            // Buscar si el servicio ya existe en nuestra lista
            var servicioExistente = serviciosSeleccionados.FirstOrDefault(s => s.IdServicio == idServicio);

            if (servicioExistente != null)
            {
                // Si existe, aumentar la cantidad
                servicioExistente.Cantidad += cantidad;
            }
            else
            {
                // Si no existe, crear nuevo
                var servicio = new ServicioAdicional
                {
                    IdServicio = idServicio,
                    Nombre = nombreServicio,
                    Precio = precio,
                    Cantidad = cantidad
                };

                serviciosSeleccionados.Add(servicio);
            }

            // Actualizar la vista
            ActualizarGridServicios();

            // Calcular totales
            CalcularSubtotalServicios();
            CalcularTotales();
        }

        // Método para actualizar la visualización del DataGridView
        private void ActualizarGridServicios()
        {
            // Limpiar el grid
            dgvServicios.Rows.Clear();

            // Agregar cada servicio de la lista al grid
            foreach (var servicio in serviciosSeleccionados)
            {
                dgvServicios.Rows.Add(
                    servicio.IdServicio,
                    servicio.Nombre,
                    servicio.Precio,
                    servicio.Cantidad,
                    servicio.Subtotal
                );
            }
        }

        // Este método se llamará al generar la factura para guardar todos los servicios en la base de datos
        private void GuardarTodosLosConsumos()
        {
            try
            {
                // Primero, eliminar todos los consumos existentes para esta reservación
                string queryEliminar = @"
            DELETE FROM ConsumoServicios 
            WHERE IdReservacion = @IdReservacion";

                SqlParameter[] parametrosEliminar = new SqlParameter[]
                {
            new SqlParameter("@IdReservacion", idReservacionActual)
                };

                Database.ExecuteNonQuery(queryEliminar, parametrosEliminar);

                // Luego, insertar los nuevos consumos
                foreach (var servicio in serviciosSeleccionados)
                {
                    string queryInsertar = @"
                INSERT INTO ConsumoServicios (
                    IdReservacion, IdServicio, Cantidad, 
                    Precio, Fecha, UsuarioRegistro
                )
                VALUES (
                    @IdReservacion, @IdServicio, @Cantidad, 
                    @Precio, @Fecha, @UsuarioRegistro
                )";

                    SqlParameter[] parametrosInsertar = new SqlParameter[]
                    {
                new SqlParameter("@IdReservacion", idReservacionActual),
                new SqlParameter("@IdServicio", servicio.IdServicio),
                new SqlParameter("@Cantidad", servicio.Cantidad),
                new SqlParameter("@Precio", servicio.Precio),
                new SqlParameter("@Fecha", DateTime.Now),
                new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                    };

                    Database.ExecuteNonQuery(queryInsertar, parametrosInsertar);
                }

                serviciosGuardados = true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar los servicios en la base de datos: {ex.Message}");
            }
        }

        private void numDescuento_ValueChanged(object sender, EventArgs e)
        {
            descuento = numDescuento.Value;
            CalcularTotales();
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
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

        private void btnAplicarDescuento_Click(object sender, EventArgs e)
        {
            try
            {
                // Obtener el porcentaje de descuento
                decimal nuevoDescuento = numDescuento.Value;

                // Validar que sea un valor válido
                if (nuevoDescuento < 0 || nuevoDescuento > 100)
                {
                    MessageBox.Show("Por favor, ingrese un porcentaje de descuento válido (0-100).",
                        "Descuento inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    numDescuento.Focus();
                    return;
                }

                DialogResult result = MessageBox.Show(
                    $"¿Está seguro de aplicar un descuento del {nuevoDescuento}%?",
                    "Confirmar descuento", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;

                // Aplicar el descuento
                descuento = nuevoDescuento;

                // Recalcular totales
                CalcularTotales();

                // Mostrar mensaje
                MessageBox.Show($"Se ha aplicado un descuento del {descuento}% al total.",
                    "Descuento aplicado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al aplicar descuento: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnEliminarServicio_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si hay un servicio seleccionado en el DataGridView
                if (dgvServicios.SelectedRows.Count == 0)
                {
                    MessageBox.Show("Por favor, seleccione un servicio para eliminar del carrito.",
                        "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener datos del servicio seleccionado
                int rowIndex = dgvServicios.SelectedRows[0].Index;
                int idServicio = Convert.ToInt32(dgvServicios.Rows[rowIndex].Cells[0].Value);
                string nombreServicio = dgvServicios.Rows[rowIndex].Cells[1].Value.ToString();

                // Confirmar eliminación
                DialogResult result = MessageBox.Show($"¿Está seguro de eliminar el servicio '{nombreServicio}' del carrito?",
                    "Confirmar eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;

                // Eliminar de la lista de servicios seleccionados (carrito)
                ServicioAdicional servicio = serviciosSeleccionados.FirstOrDefault(s => s.IdServicio == idServicio);
                if (servicio != null)
                {
                    serviciosSeleccionados.Remove(servicio);
                }

                // Eliminar la fila del DataGridView (carrito visual)
                dgvServicios.Rows.RemoveAt(rowIndex);

                // Recalcular subtotal de servicios
                CalcularSubtotalServicios();

                // Recalcular totales
                CalcularTotales();

                // Mostrar mensaje
                MessageBox.Show($"El servicio '{nombreServicio}' ha sido eliminado del carrito.",
                    "Servicio eliminado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar servicio del carrito: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGenerarFactura_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. Validar que haya una reservación seleccionada
                if (idReservacionActual <= 0)
                {
                    MessageBox.Show("Por favor, busque una reservación primero.",
                        "Reservación no seleccionada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. Confirmar generación de factura
                DialogResult result = MessageBox.Show(
                    "¿Está seguro que desea generar la factura y completar el proceso de Check-Out?",
                    "Confirmar Check-Out", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.No)
                    return;

                // 3. Guardar los consumos de servicios si no se han guardado ya
                if (!serviciosGuardados)
                {
                    GuardarTodosLosConsumos();
                }

                // 4. Calcular totales para la factura
                decimal subtotalHospedaje = this.subtotalHospedaje;
                decimal subtotalServicios = this.subtotalServicios;
                decimal subtotal = subtotalHospedaje + subtotalServicios;
                decimal descuentoValor = subtotal * (descuento / 100m);
                decimal iva = (subtotal - descuentoValor) * 0.16m; // IVA del 16%
                decimal total = subtotal - descuentoValor + iva;
                decimal totalAPagar = total - anticipoPagado;

                // Si el total a pagar es negativo, ponerlo en 0
                if (totalAPagar < 0)
                    totalAPagar = 0;

                // 5. Generar la factura en la base de datos
                int idFactura = GenerarFacturaDB(subtotalHospedaje, subtotalServicios, descuentoValor, iva, total, totalAPagar);

                // 6. Generar el archivo PDF de la factura
                string rutaFactura = GenerarArchivoFactura(idFactura, subtotalHospedaje, subtotalServicios, descuentoValor, iva, total, totalAPagar);

                // 7. Actualizar el estado de la reservación a CheckOut
                ActualizarEstadoReservacion();

                // 8. Actualizar estado de habitaciones a Disponible
                ActualizarEstadoHabitaciones();

                // 9. Mensaje de éxito
                MessageBox.Show($"¡Check-Out completado con éxito!\n\nLa factura ha sido generada y guardada en:\n{rutaFactura}",
                    "Proceso finalizado", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // 10. Abrir la factura con el programa predeterminado
                System.Diagnostics.Process.Start(rutaFactura);

                // 11. Limpiar formulario para una nueva operación
                LimpiarDatosReservacion();
                txtCodigoReservacion.Clear();
                txtCodigoReservacion.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar la factura: {ex.Message}\n\n" +
                              $"Detalles técnicos: {ex.StackTrace}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int GenerarFacturaDB(decimal subtotalHospedaje, decimal subtotalServicios,
    decimal descuento, decimal iva, decimal total, decimal totalAPagar)
        {
            try
            {
                // Obtener datos necesarios
                DataTable dtReservacion = ObtenerDatosParaFactura();

                if (dtReservacion.Rows.Count == 0)
                    throw new Exception("No se pudieron obtener los datos necesarios para la factura.");

                DataRow row = dtReservacion.Rows[0];

                // Generar número de factura único para evitar duplicados
                string numeroFactura = "FAC-" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "-" + idReservacionActual.ToString();

                // Consulta de inserción
                string query = @"
                    INSERT INTO Facturas (
                        NumeroFactura, 
                        IdReservacion, 
                        FechaFactura,
                        ReceptorNombre, 
                        ReceptorRFC, 
                        ReceptorDomicilio, 
                        ReceptorCodigoPostal, 
                        ReceptorRegimen, 
                        ReceptorUsoCFDI,
                        SubtotalHospedaje, 
                        SubtotalServicios, 
                        Descuento,
                        IVA, 
                        Total, 
                        AnticipoPagado,
                        TotalAPagar, 
                        EstadoFactura,
                        UsuarioRegistro
                    )
                    VALUES (
                        @NumeroFactura, 
                        @IdReservacion, 
                        GETDATE(),
                        @ReceptorNombre, 
                        @ReceptorRFC, 
                        @ReceptorDomicilio, 
                        @ReceptorCodigoPostal, 
                        @ReceptorRegimen, 
                        @ReceptorUsoCFDI,
                        @SubtotalHospedaje, 
                        @SubtotalServicios, 
                        @Descuento,
                        @IVA, 
                        @Total, 
                        @AnticipoPagado,
                        @TotalAPagar, 
                        'Emitida',
                        @UsuarioRegistro
                    );
                    SELECT SCOPE_IDENTITY();";

                // Parámetros
                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@NumeroFactura", numeroFactura),
                    new SqlParameter("@IdReservacion", idReservacionActual),
                    new SqlParameter("@ReceptorNombre", row["NombreCliente"].ToString()),
                    new SqlParameter("@ReceptorRFC", row["RFC"].ToString()),
                    new SqlParameter("@ReceptorDomicilio", row["Domicilio"].ToString()),
                    new SqlParameter("@ReceptorCodigoPostal", row["CodigoPostal"].ToString()),
                    new SqlParameter("@ReceptorRegimen", row["RegimenFiscal"].ToString()),
                    new SqlParameter("@ReceptorUsoCFDI", row["UsoCFDI"].ToString()),
                    new SqlParameter("@SubtotalHospedaje", subtotalHospedaje),
                    new SqlParameter("@SubtotalServicios", subtotalServicios),
                    new SqlParameter("@Descuento", descuento),
                    new SqlParameter("@IVA", iva),
                    new SqlParameter("@Total", total),
                    new SqlParameter("@AnticipoPagado", anticipoPagado),
                    new SqlParameter("@TotalAPagar", totalAPagar),
                    new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                };

                // Ejecutar consulta
                object result = Database.ExecuteScalar(query, parameters);
                return Convert.ToInt32(result);
            }
            catch (SqlException sqlEx)
            {
                // Mensaje detallado para errores SQL
                MessageBox.Show($"Error SQL ({sqlEx.Number}): {sqlEx.Message}\n\n" +
                               "Detalles: " + sqlEx.ToString(),
                               "Error en base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar la factura: {ex.Message}\n\n" +
                               "Detalles: " + ex.StackTrace,
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        private string GenerarArchivoFactura(int idFactura, decimal subtotalHospedaje, decimal subtotalServicios,
        decimal descuento, decimal iva, decimal total, decimal totalAPagar)
        {
            try
            {
                // 1. Crear directorio para facturas
                string directorioFacturas = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Facturas");

                if (!System.IO.Directory.Exists(directorioFacturas))
                    System.IO.Directory.CreateDirectory(directorioFacturas);

                // 2. Generar nombre de archivo único
                string nombreArchivo = $"Factura_{idFactura}.pdf";
                string rutaCompleta = System.IO.Path.Combine(directorioFacturas, nombreArchivo);

                // 3. Obtener datos para la factura
                DataTable dtReservacion = ObtenerDatosParaFactura();
                DataTable dtHabitaciones = ObtenerHabitacionesParaFactura();

                if (dtReservacion.Rows.Count == 0)
                    throw new Exception("No se pudieron obtener los datos de la reservación.");

                DataRow row = dtReservacion.Rows[0];

                // 4. Crear documento PDF con formato
                using (var writer = new PdfWriter(rutaCompleta))
                {
                    using (var pdf = new PdfDocument(writer))
                    {
                        // Márgenes personalizados
                        var document = new Document(pdf, PageSize.LETTER);
                        document.SetMargins(36, 36, 36, 36); // 0.5 pulgadas en todos los márgenes

                        // Fuentes
                        PdfFont fuenteNormal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                        PdfFont fuenteNegrita = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

                        // Colores
                        DeviceRgb colorTitulo = new DeviceRgb (194, 89, 100); // Color azul similar al mostrado
                        DeviceRgb colorGris = new DeviceRgb(240, 240, 240); // Color gris claro para fondos

                        // ENCABEZADO
                        Table tablaEncabezado = new Table(2);
                        tablaEncabezado.SetWidth(UnitValue.CreatePercentValue(100));

                        // Logo/Nombre de la empresa
                        Cell celdaLogo = new Cell();
                        celdaLogo.Add(new Paragraph("HOTEL MANAGER")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(16)
                            .SetFontColor(colorTitulo));
                        celdaLogo.Add(new Paragraph(row["NombreHotel"].ToString())
                            .SetFont(fuenteNegrita)
                            .SetFontSize(14));
                        celdaLogo.SetPadding(10);
                        celdaLogo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        tablaEncabezado.AddCell(celdaLogo);

                        // Datos de factura
                        Cell celdaFactura = new Cell();
                        celdaFactura.Add(new Paragraph("FACTURA")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(16)
                            .SetFontColor(colorTitulo));
                        celdaFactura.Add(new Paragraph($"No. {idFactura}")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(12));
                        celdaFactura.Add(new Paragraph($"Fecha: {DateTime.Now:dd/MM/yyyy}")
                            .SetFont(fuenteNormal)
                            .SetFontSize(10));
                        celdaFactura.SetPadding(10);
                        celdaFactura.SetTextAlignment(TextAlignment.RIGHT);
                        celdaFactura.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        tablaEncabezado.AddCell(celdaFactura);

                        document.Add(tablaEncabezado);
                        document.Add(new Paragraph("\n"));

                        // DATOS FISCALES DEL HOTEL
                        Table tablaDatosHotel = new Table(1);
                        tablaDatosHotel.SetWidth(UnitValue.CreatePercentValue(100));

                        Cell celdaTituloHotel = new Cell();
                        celdaTituloHotel.Add(new Paragraph("DATOS FISCALES DEL EMISOR")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(12)
                            .SetFontColor(colorTitulo));
                        celdaTituloHotel.SetBackgroundColor(colorGris);
                        celdaTituloHotel.SetPadding(5);
                        tablaDatosHotel.AddCell(celdaTituloHotel);

                        Table datosHotel = new Table(2);
                        datosHotel.SetWidth(UnitValue.CreatePercentValue(100));

                        // Nombre del Hotel
                        AgregarFilaTabla(datosHotel, "Nombre del Hotel:", row["NombreHotel"].ToString(), fuenteNegrita, fuenteNormal);

                        // Razón Social
                        AgregarFilaTabla(datosHotel, "Razón Social:", row["RazonSocial"].ToString(), fuenteNegrita, fuenteNormal);

                        // RFC
                        AgregarFilaTabla(datosHotel, "RFC:", row["RfcHotel"].ToString(), fuenteNegrita, fuenteNormal);

                        // Domicilio
                        AgregarFilaTabla(datosHotel, "Domicilio:", row["DireccionHotel"].ToString(), fuenteNegrita, fuenteNormal);

                        // Ciudad, Estado, País
                        string ubicacion = $"{row["CiudadHotel"]}, {row["EstadoHotel"]}, {row["PaisHotel"]}";
                        AgregarFilaTabla(datosHotel, "Ubicación:", ubicacion, fuenteNegrita, fuenteNormal);

                        // Régimen Fiscal
                        string regimenFiscalHotel = ObtenerDescripcionRegimen(row["RegimenFiscalHotel"].ToString());
                        AgregarFilaTabla(datosHotel, "Régimen Fiscal:",
                            $"{row["RegimenFiscalHotel"]} - {regimenFiscalHotel}", fuenteNegrita, fuenteNormal);

                        Cell celdaDatosHotel = new Cell();
                        celdaDatosHotel.Add(datosHotel);
                        celdaDatosHotel.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        tablaDatosHotel.AddCell(celdaDatosHotel);

                        document.Add(tablaDatosHotel);
                        document.Add(new Paragraph("\n"));

                        // DATOS DEL CLIENTE
                        Table tablaCliente = new Table(1);
                        tablaCliente.SetWidth(UnitValue.CreatePercentValue(100));

                        Cell celdaTituloCliente = new Cell();
                        celdaTituloCliente.Add(new Paragraph("DATOS DEL CLIENTE")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(12)
                            .SetFontColor(colorTitulo));
                        celdaTituloCliente.SetBackgroundColor(colorGris);
                        celdaTituloCliente.SetPadding(5);
                        tablaCliente.AddCell(celdaTituloCliente);

                        Table datosCliente = new Table(2);
                        datosCliente.SetWidth(UnitValue.CreatePercentValue(100));

                        // Nombre
                        AgregarFilaTabla(datosCliente, "Cliente:", row["NombreCliente"].ToString(), fuenteNegrita, fuenteNormal);

                        // RFC
                        AgregarFilaTabla(datosCliente, "RFC:", row["RFC"].ToString(), fuenteNegrita, fuenteNormal);

                        // Dirección
                        AgregarFilaTabla(datosCliente, "Dirección:", row["Domicilio"].ToString(), fuenteNegrita, fuenteNormal);

                        // Código Postal
                        AgregarFilaTabla(datosCliente, "C.P.:", row["CodigoPostal"].ToString(), fuenteNegrita, fuenteNormal);

                        // Régimen Fiscal
                        string regimenDesc = ObtenerDescripcionRegimen(row["RegimenFiscal"].ToString());
                        AgregarFilaTabla(datosCliente, "Régimen Fiscal:",
                            $"{row["RegimenFiscal"]} - {regimenDesc}", fuenteNegrita, fuenteNormal);

                        // Uso CFDI
                        string usoCFDIDesc = ObtenerDescripcionUsoCFDI(row["UsoCFDI"].ToString());
                        AgregarFilaTabla(datosCliente, "Uso CFDI:",
                            $"{row["UsoCFDI"]} - {usoCFDIDesc}", fuenteNegrita, fuenteNormal);

                        Cell celdaDatosCliente = new Cell();
                        celdaDatosCliente.Add(datosCliente);
                        celdaDatosCliente.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        tablaCliente.AddCell(celdaDatosCliente);

                        document.Add(tablaCliente);
                        document.Add(new Paragraph("\n"));

                        // DATOS DE LA RESERVACIÓN
                        Table tablaReservacion = new Table(1);
                        tablaReservacion.SetWidth(UnitValue.CreatePercentValue(100));

                        Cell celdaTituloReservacion = new Cell();
                        celdaTituloReservacion.Add(new Paragraph("DATOS DE LA RESERVACIÓN")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(12)
                            .SetFontColor(colorTitulo));
                        celdaTituloReservacion.SetBackgroundColor(colorGris);
                        celdaTituloReservacion.SetPadding(5);
                        tablaReservacion.AddCell(celdaTituloReservacion);

                        Table datosReservacion = new Table(4);
                        datosReservacion.SetWidth(UnitValue.CreatePercentValue(100));

                        // Código
                        Cell celdaCodigoTitulo = new Cell();
                        celdaCodigoTitulo.Add(new Paragraph("Código de Reservación:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaCodigoTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosReservacion.AddCell(celdaCodigoTitulo);

                        Cell celdaCodigoValor = new Cell();
                        celdaCodigoValor.Add(new Paragraph(row["CodigoReservacion"].ToString())
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaCodigoValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaCodigoValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosReservacion.AddCell(celdaCodigoValor);

                        // Hotel
                        Cell celdaHotelTitulo = new Cell();
                        celdaHotelTitulo.Add(new Paragraph("Hotel:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaHotelTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosReservacion.AddCell(celdaHotelTitulo);

                        Cell celdaHotelValor = new Cell();
                        celdaHotelValor.Add(new Paragraph(row["NombreHotel"].ToString())
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaHotelValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaHotelValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosReservacion.AddCell(celdaHotelValor);

                        // Check-In
                        Cell celdaCheckInTitulo = new Cell();
                        celdaCheckInTitulo.Add(new Paragraph("Check-In:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaCheckInTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosReservacion.AddCell(celdaCheckInTitulo);

                        Cell celdaCheckInValor = new Cell();
                        celdaCheckInValor.Add(new Paragraph(Convert.ToDateTime(row["FechaCheckIn"]).ToString("dd-MMM-yyyy"))
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaCheckInValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaCheckInValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosReservacion.AddCell(celdaCheckInValor);

                        // Check-Out
                        Cell celdaCheckOutTitulo = new Cell();
                        celdaCheckOutTitulo.Add(new Paragraph("Check-Out:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaCheckOutTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosReservacion.AddCell(celdaCheckOutTitulo);

                        Cell celdaCheckOutValor = new Cell();
                        celdaCheckOutValor.Add(new Paragraph(Convert.ToDateTime(row["FechaCheckOut"]).ToString("dd-MMM-yyyy"))
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaCheckOutValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaCheckOutValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosReservacion.AddCell(celdaCheckOutValor);

                        // Noches de estancia
                        int noches = (Convert.ToDateTime(row["FechaCheckOut"]) - Convert.ToDateTime(row["FechaCheckIn"])).Days;
                        Cell celdaNochesTitulo = new Cell();
                        celdaNochesTitulo.Add(new Paragraph("Noches de estancia:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaNochesTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosReservacion.AddCell(celdaNochesTitulo);

                        Cell celdaNochesValor = new Cell();
                        celdaNochesValor.Add(new Paragraph(noches.ToString())
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaNochesValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaNochesValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosReservacion.AddCell(celdaNochesValor);

                        Cell celdaDatosReservacion = new Cell();
                        celdaDatosReservacion.Add(datosReservacion);
                        celdaDatosReservacion.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaDatosReservacion.SetPadding(5);
                        tablaReservacion.AddCell(celdaDatosReservacion);

                        // Obtener datos de pago
                        Dictionary<string, string> datosPago = ObtenerDatosPago();

                        // Datos fiscales adicionales para la factura
                        Table datosFiscales = new Table(4); // 4 columnas
                        datosFiscales.SetWidth(UnitValue.CreatePercentValue(100));

                        // Forma de pago
                        Cell celdaFormaPagoTitulo = new Cell();
                        celdaFormaPagoTitulo.Add(new Paragraph("Forma de pago:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaFormaPagoTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosFiscales.AddCell(celdaFormaPagoTitulo);

                        Cell celdaFormaPagoValor = new Cell();
                        celdaFormaPagoValor.Add(new Paragraph(datosPago["FormaPago"] + " - " + datosPago["DescripcionFormaPago"])
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaFormaPagoValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaFormaPagoValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosFiscales.AddCell(celdaFormaPagoValor);

                        // Método de pago
                        Cell celdaMetodoPagoTitulo = new Cell();
                        celdaMetodoPagoTitulo.Add(new Paragraph("Método de pago:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaMetodoPagoTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosFiscales.AddCell(celdaMetodoPagoTitulo);

                        Cell celdaMetodoPagoValor = new Cell();
                        celdaMetodoPagoValor.Add(new Paragraph(datosPago["MetodoPago"] + " - " + datosPago["DescripcionMetodoPago"])
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaMetodoPagoValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaMetodoPagoValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosFiscales.AddCell(celdaMetodoPagoValor);

                        // Moneda
                        Cell celdaMonedaTitulo = new Cell();
                        celdaMonedaTitulo.Add(new Paragraph("Moneda:")
                            .SetFont(fuenteNegrita).SetFontSize(10));
                        celdaMonedaTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        datosFiscales.AddCell(celdaMonedaTitulo);

                        Cell celdaMonedaValor = new Cell();
                        celdaMonedaValor.Add(new Paragraph(datosPago["Moneda"] + " - " + datosPago["DescripcionMoneda"])
                            .SetFont(fuenteNormal).SetFontSize(10));
                        celdaMonedaValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaMonedaValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
                        datosFiscales.AddCell(celdaMonedaValor);

                        // Agregar a la sección de datos de la reservación
                        Cell celdaDatosFiscales = new Cell();
                        celdaDatosFiscales.Add(datosFiscales);
                        celdaDatosFiscales.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaDatosFiscales.SetPadding(5);
                        tablaReservacion.AddCell(celdaDatosFiscales);

                        document.Add(tablaReservacion);
                        document.Add(new Paragraph("\n"));

                        // CONCEPTOS (HABITACIONES Y SERVICIOS)
                        Table tablaConceptos = new Table(1);
                        tablaConceptos.SetWidth(UnitValue.CreatePercentValue(100));

                        Cell celdaTituloConceptos = new Cell();
                        celdaTituloConceptos.Add(new Paragraph("CONCEPTOS")
                            .SetFont(fuenteNegrita)
                            .SetFontSize(12)
                            .SetFontColor(colorTitulo));
                        celdaTituloConceptos.SetBackgroundColor(colorGris);
                        celdaTituloConceptos.SetPadding(5);
                        tablaConceptos.AddCell(celdaTituloConceptos);

                        Table detalleConceptos = new Table(8); 
                        detalleConceptos.SetWidth(UnitValue.CreatePercentValue(100));

                        // Encabezados
                        AgregarCeldaTabla(detalleConceptos, "Concepto", fuenteNegrita, colorGris, TextAlignment.LEFT);
                        AgregarCeldaTabla(detalleConceptos, "Cantidad", fuenteNegrita, colorGris, TextAlignment.CENTER);
                        AgregarCeldaTabla(detalleConceptos, "Unidad", fuenteNegrita, colorGris, TextAlignment.CENTER);
                        AgregarCeldaTabla(detalleConceptos, "Clave Unidad", fuenteNegrita, colorGris, TextAlignment.CENTER);
                        AgregarCeldaTabla(detalleConceptos, "Clave Prod/Serv", fuenteNegrita, colorGris, TextAlignment.CENTER);
                        AgregarCeldaTabla(detalleConceptos, "Precio Unitario", fuenteNegrita, colorGris, TextAlignment.RIGHT);
                        AgregarCeldaTabla(detalleConceptos, "IVA (16%)", fuenteNegrita, colorGris, TextAlignment.RIGHT);
                        AgregarCeldaTabla(detalleConceptos, "Importe", fuenteNegrita, colorGris, TextAlignment.RIGHT);

                        // Habitaciones
                        foreach (DataRow habRow in dtHabitaciones.Rows)
                        {
                            string tipoHab = habRow["TipoHabitacion"].ToString();
                            string numHab = habRow["NumeroHabitacion"].ToString();
                            decimal precioNoche = Convert.ToDecimal(habRow["PrecioPorNoche"]);
                            decimal subtotalHab = precioNoche * noches;
                            decimal ivaHab = subtotalHab * 0.16m;

                            // Obtener datos del SAT para hospedaje
                            Dictionary<string, string> datosSAT = ObtenerDatosSATHabitacion();

                            AgregarCeldaTabla(detalleConceptos, $"Habitación {tipoHab} ({numHab}) - {noches} noches", fuenteNormal, null, TextAlignment.LEFT);
                            AgregarCeldaTabla(detalleConceptos, "1", fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["Unidad"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["ClaveUnidad"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["ClaveProducto"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, $"${precioNoche:N2}", fuenteNormal, null, TextAlignment.RIGHT);
                            AgregarCeldaTabla(detalleConceptos, $"${ivaHab:N2}", fuenteNormal, null, TextAlignment.RIGHT);
                            AgregarCeldaTabla(detalleConceptos, $"${subtotalHab:N2}", fuenteNormal, null, TextAlignment.RIGHT);

                            // Guardar detalles en la base de datos
                            GuardarDetalleFactura(idFactura, $"Habitación {tipoHab} ({numHab}) - {noches} noches",
                                                 1, precioNoche, subtotalHab, ivaHab, subtotalHab + ivaHab,
                                                 datosSAT["ClaveProducto"], datosSAT["ClaveUnidad"], datosSAT["Unidad"]);
                        }

                        // Servicios adicionales
                        foreach (var servicio in serviciosSeleccionados)
                        {
                            decimal subtotalServ = servicio.Subtotal;
                            decimal ivaServ = subtotalServ * 0.16m;

                            // Obtener datos del SAT basado en el nombre del servicio
                            Dictionary<string, string> datosSAT = ObtenerDatosSAT(servicio.Nombre);

                            AgregarCeldaTabla(detalleConceptos, servicio.Nombre, fuenteNormal, null, TextAlignment.LEFT);
                            AgregarCeldaTabla(detalleConceptos, servicio.Cantidad.ToString(), fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["Unidad"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["ClaveUnidad"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, datosSAT["ClaveProducto"], fuenteNormal, null, TextAlignment.CENTER);
                            AgregarCeldaTabla(detalleConceptos, $"${servicio.Precio:N2}", fuenteNormal, null, TextAlignment.RIGHT);
                            AgregarCeldaTabla(detalleConceptos, $"${ivaServ:N2}", fuenteNormal, null, TextAlignment.RIGHT);
                            AgregarCeldaTabla(detalleConceptos, $"${subtotalServ:N2}", fuenteNormal, null, TextAlignment.RIGHT);

                            // Guardar detalles en la base de datos
                            GuardarDetalleFactura(idFactura, servicio.Nombre, servicio.Cantidad, servicio.Precio,
                                                  subtotalServ, ivaServ, subtotalServ + ivaServ,
                                                  datosSAT["ClaveProducto"], datosSAT["ClaveUnidad"], datosSAT["Unidad"]);
                        }

                        Cell celdaDetalleConceptos = new Cell();
                        celdaDetalleConceptos.Add(detalleConceptos);
                        celdaDetalleConceptos.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaDetalleConceptos.SetPadding(0);
                        tablaConceptos.AddCell(celdaDetalleConceptos);

                        document.Add(tablaConceptos);
                        document.Add(new Paragraph("\n"));

                        // RESUMEN FINANCIERO
                        Table tablaResumen = new Table(2);
                        tablaResumen.SetWidth(UnitValue.CreatePercentValue(50));
                        tablaResumen.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.RIGHT);

                        // Importe con letra
                        String importeConLetra = ConvertirNumeroALetras(total);
                        Cell celdaImporteLetra = new Cell(1, 2);
                        celdaImporteLetra.Add(new Paragraph($"Importe con letra: {importeConLetra} PESOS {((total - Math.Floor(total)) * 100):00}/100 M.N.")
                            .SetFont(fuenteNormal)
                            .SetFontSize(10));
                        celdaImporteLetra.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaImporteLetra.SetPadding(5);
                        document.Add(new Paragraph($"Importe con letra: {importeConLetra} PESOS {((total - Math.Floor(total)) * 100):00}/100 M.N.")
                            .SetFont(fuenteNormal)
                            .SetFontSize(10));
                        document.Add(new Paragraph("\n"));

                        // Subtotal
                        AgregarFilaResumen(tablaResumen, "Subtotal:", $"${(subtotalHospedaje + subtotalServicios):N2}", fuenteNegrita, fuenteNormal);

                        // Descuento
                        if (descuento > 0)
                        {
                            AgregarFilaResumen(tablaResumen, "Descuento:", $"${descuento:N2}", fuenteNegrita, fuenteNormal);
                        }

                        // IVA
                        AgregarFilaResumen(tablaResumen, "IVA (16%):", $"${iva:N2}", fuenteNegrita, fuenteNormal);

                        // Total
                        AgregarFilaResumen(tablaResumen, "Total:", $"${total:N2}", fuenteNegrita, fuenteNormal);

                        // Anticipo
                        AgregarFilaResumen(tablaResumen, "Anticipo Pagado:", $"${anticipoPagado:N2}", fuenteNegrita, fuenteNormal);

                        // Total a pagar
                        Cell celdaTotalPagarTitulo = new Cell();
                        celdaTotalPagarTitulo.Add(new Paragraph("Total a Pagar:")
                            .SetFont(fuenteNegrita).SetFontSize(12).SetFontColor(colorTitulo));
                        celdaTotalPagarTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaTotalPagarTitulo.SetPadding(5);
                        tablaResumen.AddCell(celdaTotalPagarTitulo);

                        Cell celdaTotalPagarValor = new Cell();
                        celdaTotalPagarValor.Add(new Paragraph($"${totalAPagar:N2}")
                            .SetFont(fuenteNegrita).SetFontSize(12).SetFontColor(colorTitulo));
                        celdaTotalPagarValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
                        celdaTotalPagarValor.SetPadding(5);
                        celdaTotalPagarValor.SetTextAlignment(TextAlignment.RIGHT);
                        tablaResumen.AddCell(celdaTotalPagarValor);

                        document.Add(tablaResumen);
                        document.Add(new Paragraph("\n"));

                        // PIE DE PÁGINA
                        Paragraph piePagina = new Paragraph("AGRADECEMOS SU PREFERENCIA")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFont(fuenteNegrita)
                            .SetFontSize(14)
                            .SetFontColor(colorTitulo);
                        document.Add(piePagina);

                        Paragraph atendidoPor = new Paragraph($"Atendido por: {Session.NombreUsuario}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFont(fuenteNormal)
                            .SetFontSize(10);
                        document.Add(atendidoPor);

                        document.Close();
                    }
                }

                // 5. Actualizar la ruta del archivo en la base de datos
                string updateQuery = @"
                    UPDATE Facturas 
                    SET RutaArchivoFactura = @Ruta 
                    WHERE IdFactura = @IdFactura";

                SqlParameter[] updateParams = new SqlParameter[]
                {
                    new SqlParameter("@Ruta", rutaCompleta),
                    new SqlParameter("@IdFactura", idFactura)
                };

                Database.ExecuteNonQuery(updateQuery, updateParams);

                return rutaCompleta;
            }
            catch (Exception ex)
            {
                // Detalles del error
                string detallesError = $"Error: {ex.Message}\n";
                if (ex.InnerException != null)
                    detallesError += $"Inner Exception: {ex.InnerException.Message}\n";
                detallesError += $"Stack Trace: {ex.StackTrace}";

                MessageBox.Show(detallesError, "Error detallado", MessageBoxButtons.OK, MessageBoxIcon.Error);

                throw new Exception($"Error al generar el archivo de factura: {ex.Message}");
            }
        }

        // Métodos auxiliares para crear el PDF
        private void AgregarFilaTabla(Table tabla, string titulo, string valor, PdfFont fuenteTitulo, PdfFont fuenteValor)
        {
            Cell celdaTitulo = new Cell();
            celdaTitulo.Add(new Paragraph(titulo).SetFont(fuenteTitulo).SetFontSize(10));
            celdaTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            celdaTitulo.SetPadding(5);
            tabla.AddCell(celdaTitulo);

            Cell celdaValor = new Cell();
            celdaValor.Add(new Paragraph(valor).SetFont(fuenteValor).SetFontSize(10));
            celdaValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            celdaValor.SetBorderBottom(new SolidBorder(ColorConstants.LIGHT_GRAY, 1));
            celdaValor.SetPadding(5);
            tabla.AddCell(celdaValor);
        }

        private void AgregarCeldaTabla(Table tabla, string texto, PdfFont fuente, DeviceRgb colorFondo, TextAlignment alineacion)
        {
            Cell celda = new Cell();
            celda.Add(new Paragraph(texto).SetFont(fuente).SetFontSize(10));
            if (colorFondo != null)
                celda.SetBackgroundColor(colorFondo);
            celda.SetTextAlignment(alineacion);
            celda.SetPadding(5);
            tabla.AddCell(celda);
        }

        private void AgregarFilaResumen(Table tabla, string titulo, string valor, PdfFont fuenteTitulo, PdfFont fuenteValor)
        {
            Cell celdaTitulo = new Cell();
            celdaTitulo.Add(new Paragraph(titulo).SetFont(fuenteTitulo).SetFontSize(10));
            celdaTitulo.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            celdaTitulo.SetPadding(5);
            tabla.AddCell(celdaTitulo);

            Cell celdaValor = new Cell();
            celdaValor.Add(new Paragraph(valor).SetFont(fuenteValor).SetFontSize(10));
            celdaValor.SetBorder(iText.Layout.Borders.Border.NO_BORDER);
            celdaValor.SetPadding(5);
            celdaValor.SetTextAlignment(TextAlignment.RIGHT);
            tabla.AddCell(celdaValor);
        }

        // Método para obtener la descripción del régimen fiscal
        private string ObtenerDescripcionRegimen(string codigoRegimen)
        {
            switch (codigoRegimen)
            {
                case "605": return "Sueldos y Salarios";
                case "612": return "Personas Físicas con Actividades Empresariales y Profesionales";
                case "601": return "General de Ley Personas Morales";
                case "603": return "Personas Morales con Fines no Lucrativos";
                default: return " ";
            }
        }

        // Método para obtener la descripción del uso de CFDI
        private string ObtenerDescripcionUsoCFDI(string codigoUso)
        {
            switch (codigoUso)
            {
                case "G03": return "Gastos en general";
                case "S01": return "Sin efectos fiscales";
                default: return "Uso no especificado";
            }
        }

        // Para consultar las habitaciones de la reservación
        private DataTable ObtenerHabitacionesParaFactura()
        {
            try
            {
                string query = @"
                    SELECT 
                        h.NumeroHabitacion, th.Nombre AS TipoHabitacion, 
                        dr.PrecioPorNoche, dr.CantidadPersonas
                    FROM DetalleReservaciones dr
                    INNER JOIN Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
                    INNER JOIN TiposHabitacion th ON h.IdTipoHabitacion = th.IdTipoHabitacion
                    WHERE dr.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                return Database.ExecuteQuery(query, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener habitaciones para factura: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new DataTable(); // Devuelve una tabla vacía en caso de error
            }
        }

        private DataTable ObtenerDatosParaFactura()
        {
            try
            {
                string query = @"
                    SELECT 
                        r.CodigoReservacion, r.FechaCheckIn, r.FechaCheckOut, r.MontoAnticipo,
                        CONCAT(c.Nombre, ' ', c.ApellidoPaterno, ' ', c.ApellidoMaterno) AS NombreCliente,
                        c.RFC, c.Correo, c.Ciudad AS CiudadCliente, c.Estado AS EstadoCliente, c.Pais AS PaisCliente,
                        c.Domicilio, c.CodigoPostal, c.RegimenFiscal, c.UsoCFDI,
                        h.Nombre AS NombreHotel, h.Domicilio AS DireccionHotel, h.Ciudad AS CiudadHotel, 
                        h.Estado AS EstadoHotel, h.Pais AS PaisHotel,
                        h.RFC AS RfcHotel, h.RazonSocial, h.RegimenFiscal AS RegimenFiscalHotel
                    FROM Reservaciones r
                    INNER JOIN Clientes c ON r.IdCliente = c.IdCliente
                    INNER JOIN Hoteles h ON r.IdHotel = h.IdHotel
                    WHERE r.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                return Database.ExecuteQuery(query, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener datos para la factura: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }

        // Método para convertir un número a letras
        private string ConvertirNumeroALetras(decimal numero)
        {
            // Parte entera
            int entero = (int)Math.Floor(numero);

            if (entero == 0)
                return "CERO";

            string resultado = ConvertirEnteroALetras(entero);

            return resultado;
        }

        // Método auxiliar para convertir enteros a letras
        private string ConvertirEnteroALetras(int numero)
        {
            string[] unidades = { "", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE" };
            string[] decenas = { "", "DIEZ", "VEINTE", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA" };
            string[] especiales = { "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE", "DIECIOCHO", "DIECINUEVE" };
            string[] centenas = { "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS" };

            string resultado = "";

            // Millones
            if (numero >= 1000000)
            {
                int millones = numero / 1000000;
                numero %= 1000000;

                if (millones == 1)
                    resultado += "UN MILLÓN ";
                else
                    resultado += ConvertirEnteroALetras(millones) + " MILLONES ";
            }

            // Miles
            if (numero >= 1000)
            {
                int miles = numero / 1000;
                numero %= 1000;

                if (miles == 1)
                    resultado += "MIL ";
                else
                    resultado += ConvertirEnteroALetras(miles) + " MIL ";
            }

            // Centenas
            if (numero >= 100)
            {
                int cen = numero / 100;
                numero %= 100;

                if (cen == 1 && numero == 0)
                    resultado += "CIEN ";
                else
                    resultado += centenas[cen] + " ";
            }

            // Decenas y unidades
            if (numero > 0)
            {
                if (numero < 10)
                {
                    resultado += unidades[numero] + " ";
                }
                else if (numero < 20)
                {
                    resultado += especiales[numero - 10] + " ";
                }
                else
                {
                    int dec = numero / 10;
                    int uni = numero % 10;

                    if (uni == 0)
                        resultado += decenas[dec] + " ";
                    else if (dec == 2)
                        resultado += "VEINTI" + unidades[uni] + " ";
                    else
                        resultado += decenas[dec] + " Y " + unidades[uni] + " ";
                }
            }

            return resultado.Trim();
        }


        private void ActualizarEstadoReservacion()
        {
            try
            {
                string query = @"
                    UPDATE Reservaciones
                    SET 
                        EstadoReservacion = 'CheckOut',
                        FechaHoraCheckOut = @FechaHoraCheckOut,
                        FechaModificacion = @FechaModificacion,
                        UsuarioModificacion = @UsuarioModificacion
                    WHERE IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@FechaHoraCheckOut", DateTime.Now),
                    new SqlParameter("@FechaModificacion", DateTime.Now),
                    new SqlParameter("@UsuarioModificacion", Session.IdUsuario),
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                Database.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar el estado de la reservación: {ex.Message}", ex);
            }
        }

        private void ActualizarEstadoHabitaciones()
        {
            try
            {
                string query = @"
                    UPDATE h
                    SET 
                        h.Estado = 'Disponible',
                        h.FechaModificacion = @FechaModificacion,
                        h.UsuarioModificacion = @UsuarioModificacion
                    FROM Habitaciones h
                    INNER JOIN DetalleReservaciones dr ON h.IdHabitacion = dr.IdHabitacion
                    WHERE dr.IdReservacion = @IdReservacion";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@FechaModificacion", DateTime.Now),
                    new SqlParameter("@UsuarioModificacion", Session.IdUsuario),
                    new SqlParameter("@IdReservacion", idReservacionActual)
                };

                Database.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar el estado de las habitaciones: {ex.Message}", ex);
            }
        }

        private Dictionary<string, string> ObtenerDatosSAT(string nombreServicio)
        {
            Dictionary<string, string> datosSAT = new Dictionary<string, string>();

            try
            {
                // Convertir el nombre del servicio a minúsculas para búsqueda
                string nombreLower = nombreServicio.ToLower();

                // SQL para buscar coincidencias por palabra clave
                string query = @"
                    SELECT TOP 1 
                        ClaveProdServ, DescripcionProdServ, 
                        ClaveUnidad, DescripcionUnidad 
                    FROM MapeoServiciosSAT 
                    WHERE @Nombre LIKE '%' + PalabraClave + '%'
                    ORDER BY LEN(PalabraClave) DESC";

                SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Nombre", nombreLower)
        };

                DataTable dt = Database.ExecuteQuery(query, parameters);

                // Si se encuentra coincidencia, usamos esos valores
                if (dt.Rows.Count > 0)
                {
                    DataRow row = dt.Rows[0];
                    datosSAT["ClaveProducto"] = row["ClaveProdServ"].ToString();
                    datosSAT["DescripcionProducto"] = row["DescripcionProdServ"].ToString();
                    datosSAT["ClaveUnidad"] = row["ClaveUnidad"].ToString();
                    datosSAT["Unidad"] = row["DescripcionUnidad"].ToString();
                }
                else
                {
                    // Si no hay coincidencia, buscamos el valor predeterminado
                    query = "SELECT ClaveProdServ, DescripcionProdServ, ClaveUnidad, DescripcionUnidad FROM MapeoServiciosSAT WHERE PalabraClave = 'default'";
                    dt = Database.ExecuteQuery(query, null);

                    if (dt.Rows.Count > 0)
                    {
                        DataRow row = dt.Rows[0];
                        datosSAT["ClaveProducto"] = row["ClaveProdServ"].ToString();
                        datosSAT["DescripcionProducto"] = row["DescripcionProdServ"].ToString();
                        datosSAT["ClaveUnidad"] = row["ClaveUnidad"].ToString();
                        datosSAT["Unidad"] = row["DescripcionUnidad"].ToString();
                    }
                    else
                    {
                        // Valores predeterminados absolutos en caso de error
                        datosSAT["ClaveProducto"] = "90111800";
                        datosSAT["DescripcionProducto"] = "Servicios de alojamiento en hoteles";
                        datosSAT["ClaveUnidad"] = "E48";
                        datosSAT["Unidad"] = "Unidad de servicio";
                    }
                }
            }
            catch (Exception ex)
            {
                // En caso de error, usamos valores predeterminados
                Console.WriteLine($"Error al obtener datos SAT: {ex.Message}");
                datosSAT["ClaveProducto"] = "90111800";
                datosSAT["DescripcionProducto"] = "Servicios de alojamiento en hoteles";
                datosSAT["ClaveUnidad"] = "E48";
                datosSAT["Unidad"] = "Unidad de servicio";
            }

            return datosSAT;
        }

        private Dictionary<string, string> ObtenerDatosSATHabitacion()
        {
            Dictionary<string, string> datosSAT = new Dictionary<string, string>();

            // Valores específicos para habitaciones
            datosSAT["ClaveProducto"] = "90111501";
            datosSAT["DescripcionProducto"] = "Hoteles";
            datosSAT["ClaveUnidad"] = "DAY";
            datosSAT["Unidad"] = "Día";

            return datosSAT;
        }

        private Dictionary<string, string> ObtenerDatosPago()
        {
            Dictionary<string, string> datosPago = new Dictionary<string, string>();

            try
            {
                // Valores predeterminados
                datosPago["MetodoPago"] = "PUE"; // Pago en una sola exhibición
                datosPago["DescripcionMetodoPago"] = "Pago en una sola exhibición";
                datosPago["FormaPago"] = "01"; // Efectivo
                datosPago["DescripcionFormaPago"] = "Efectivo";
                datosPago["Moneda"] = "MXN";
                datosPago["DescripcionMoneda"] = "Peso Mexicano";

                // Aquí podrías intentar determinar la forma de pago real
                // basándote en los pagos registrados para esta reservación
                string queryPagos = @"
            SELECT TOP 1 MetodoPago 
            FROM Pagos 
            WHERE IdReservacion = @IdReservacion 
            ORDER BY FechaPago DESC";

                SqlParameter[] paramsPagos = new SqlParameter[] {
            new SqlParameter("@IdReservacion", idReservacionActual)
        };

                object metodoPagoResult = Database.ExecuteScalar(queryPagos, paramsPagos);

                if (metodoPagoResult != null && metodoPagoResult != DBNull.Value)
                {
                    string metodoPago = metodoPagoResult.ToString();

                    if (metodoPago.Contains("Tarjeta de Crédito"))
                    {
                        datosPago["FormaPago"] = "04";
                        datosPago["DescripcionFormaPago"] = "Tarjeta de crédito";
                    }
                    else if (metodoPago.Contains("Tarjeta de Débito"))
                    {
                        datosPago["FormaPago"] = "28";
                        datosPago["DescripcionFormaPago"] = "Tarjeta de débito";
                    }
                    else if (metodoPago.Contains("Transferencia"))
                    {
                        datosPago["FormaPago"] = "03";
                        datosPago["DescripcionFormaPago"] = "Transferencia electrónica de fondos";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener datos de pago: {ex.Message}");
            }

            return datosPago;
        }

        private void GuardarDetalleFactura(int idFactura, string concepto, int cantidad,
                                          decimal precioUnitario, decimal subtotal,
                                          decimal iva, decimal total, string claveProducto,
                                          string claveUnidad, string unidad)
        {
            try
            {
                string query = @"
                    INSERT INTO DetalleFacturas (
                        IdFactura, Concepto, Cantidad, PrecioUnitario,
                        Subtotal, IVA, Total, ClaveProductoServicio,
                        ClaveUnidad, Unidad, FechaRegistro
                    )
                    VALUES (
                        @IdFactura, @Concepto, @Cantidad, @PrecioUnitario,
                        @Subtotal, @IVA, @Total, @ClaveProductoServicio,
                        @ClaveUnidad, @Unidad, GETDATE()
                    )";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdFactura", idFactura),
                    new SqlParameter("@Concepto", concepto),
                    new SqlParameter("@Cantidad", cantidad),
                    new SqlParameter("@PrecioUnitario", precioUnitario),
                    new SqlParameter("@Subtotal", subtotal),
                    new SqlParameter("@IVA", iva),
                    new SqlParameter("@Total", total),
                    new SqlParameter("@ClaveProductoServicio", claveProducto),
                    new SqlParameter("@ClaveUnidad", claveUnidad),
                    new SqlParameter("@Unidad", unidad)
                };

                Database.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar detalle de factura: {ex.Message}");
            }
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            // Mostrar pantalla de inicio de sesión
            LoginForm login = new LoginForm();
            login.Show();
            this.Close();
        }
    }
}
