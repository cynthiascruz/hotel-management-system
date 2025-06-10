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
    public partial class Reservaciones : Form
    {
        // Declara la variable para el cliente actual
        private Cliente clienteActual;

        // Lista para almacenar las habitaciones seleccionadas
        private List<TipoHabitacion> habitacionesSeleccionadas = new List<TipoHabitacion>();

        private string metodoPagoSeleccionado = "";
        private string referenciaSeleccionada = "";
        private bool anticipoPagado = false;


        public Reservaciones()
        {
            InitializeComponent();
        }

        private void Reservaciones_Load(object sender, EventArgs e)
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

            // Configurar el ComboBox de tipo de búsqueda
            cboTipoBusqueda.Items.Add("RFC");
            cboTipoBusqueda.Items.Add("Apellidos");
            cboTipoBusqueda.Items.Add("Correo");
            cboTipoBusqueda.SelectedIndex = 0; // Por defecto, seleccionar RFC

            // En el método Reservaciones_Load, agrega:
            cboMetodoPago.Items.Clear();
            cboMetodoPago.Items.AddRange(new object[] {
                "Efectivo",
                "Tarjeta de crédito",
                "Tarjeta de débito",
                "Transferencia bancaria"
            });
            cboMetodoPago.SelectedIndex = 0;

            // Configurar el DataGridView para los resultados
            ConfigurarDataGridViewResultados();
            EstiloDataGridViewResultadosClientes();

            // Inicializar fechas
            dtpCheckIn.Value = DateTime.Today;
            dtpCheckOut.Value = DateTime.Today.AddDays(1);

            // Configurar el DataGridView para habitaciones reservadas
            ConfigurarDataGridViewHabitacionesReservadas();
            EstiloDataGridViewHabitacionesReservadas();

            // Aplicar estilo al DataGridView de habitaciones disponibles aunque esté vacío
            EstiloDataGridViewHabitacionesDisponibles();

            // Cargar ciudades para el combo
            CargarCiudades();

            // Calcular noches
            CalcularNoches();

            // Cargar todos los clientes por defecto
            cargarClientes();
        }

        // Método para cargar todos los clientes
        private void cargarClientes()
        {
            try
            {
                string query = @"
            SELECT TOP 100 IdCliente, Nombre, ApellidoPaterno, ApellidoMaterno, 
                   Ciudad, Estado, Pais, RFC, Correo, 
                   TelefonoCasa, TelefonoCelular, FechaNacimiento, EstadoCivil
            FROM Clientes
            ORDER BY Nombre, ApellidoPaterno, ApellidoMaterno";

                DataTable dtClientes = Database.ExecuteQuery(query);

                List<Cliente> clientes = new List<Cliente>();

                foreach (DataRow row in dtClientes.Rows)
                {
                    Cliente cliente = new Cliente
                    {
                        IdCliente = Convert.ToInt32(row["IdCliente"]),
                        Nombre = row["Nombre"].ToString(),
                        ApellidoPaterno = row["ApellidoPaterno"].ToString(),
                        ApellidoMaterno = row["ApellidoMaterno"].ToString(),
                        Ciudad = row["Ciudad"].ToString(),
                        Estado = row["Estado"].ToString(),
                        Pais = row["Pais"].ToString(),
                        RFC = row["RFC"].ToString(),
                        Correo = row["Correo"].ToString(),
                        TelefonoCasa = row["TelefonoCasa"].ToString(),
                        TelefonoCelular = row["TelefonoCelular"].ToString(),
                        FechaNacimiento = Convert.ToDateTime(row["FechaNacimiento"]),
                        EstadoCivil = row["EstadoCivil"].ToString()
                    };

                    clientes.Add(cliente);
                }

                dgvResultadosClientes.DataSource = clientes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar clientes: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        private void ConfigurarDataGridViewResultados()
        {
            // Configurar el DataGridView
            dgvResultadosClientes.AutoGenerateColumns = false;

            // Limpiar columnas existentes
            dgvResultadosClientes.Columns.Clear();

            // Añadir columnas
            DataGridViewTextBoxColumn colNombre = new DataGridViewTextBoxColumn();
            colNombre.DataPropertyName = "NombreCompleto";
            colNombre.HeaderText = "Nombre completo";
            colNombre.Width = 200;
            dgvResultadosClientes.Columns.Add(colNombre);

            DataGridViewTextBoxColumn colRFC = new DataGridViewTextBoxColumn();
            colRFC.DataPropertyName = "RFC";
            colRFC.HeaderText = "RFC";
            colRFC.Width = 100;
            dgvResultadosClientes.Columns.Add(colRFC);

            DataGridViewTextBoxColumn colCorreo = new DataGridViewTextBoxColumn();
            colCorreo.DataPropertyName = "Correo";
            colCorreo.HeaderText = "Correo";
            colCorreo.Width = 200;
            dgvResultadosClientes.Columns.Add(colCorreo);

            DataGridViewTextBoxColumn colTelefono = new DataGridViewTextBoxColumn();
            colTelefono.DataPropertyName = "TelefonoCelular";
            colTelefono.HeaderText = "Teléfono";
            colTelefono.Width = 100;
            dgvResultadosClientes.Columns.Add(colTelefono);

            // Configuraciones adicionales
            dgvResultadosClientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResultadosClientes.AllowUserToAddRows = false;
            dgvResultadosClientes.AllowUserToDeleteRows = false;
            dgvResultadosClientes.ReadOnly = true;
            dgvResultadosClientes.MultiSelect = false;

            // Asignar el evento de selección
            dgvResultadosClientes.SelectionChanged += dgvResultadosClientes_SelectionChanged;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBusqueda.Text))
            {
                MessageBox.Show("Por favor ingrese un texto para buscar", "Búsqueda vacía",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string tipoBusqueda = cboTipoBusqueda.SelectedItem.ToString();
            string textoBusqueda = txtBusqueda.Text.Trim();

            try
            {
                // Obtener resultados según el tipo de búsqueda
                List<Cliente> resultados = BuscarClientes(tipoBusqueda, textoBusqueda);

                // Mostrar resultados en el DataGridView
                dgvResultadosClientes.DataSource = resultados;

                if (resultados.Count == 0)
                {
                    MessageBox.Show("No se encontraron resultados para la búsqueda.",
                                    "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else if (resultados.Count == 1)
                {
                    // Si solo hay un resultado, seleccionarlo automáticamente
                    dgvResultadosClientes.Rows[0].Selected = true;
                    SeleccionarCliente(resultados[0]);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar clientes: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<Cliente> BuscarClientes(string tipoBusqueda, string textoBusqueda)
        {
            List<Cliente> clientes = new List<Cliente>();

            string whereClause;

            // Definir la cláusula WHERE según el tipo de búsqueda
            switch (tipoBusqueda)
            {
                case "RFC":
                    whereClause = "RFC LIKE @Texto + '%'";
                    break;
                case "Apellidos":
                    whereClause = "ApellidoPaterno LIKE @Texto + '%' OR ApellidoMaterno LIKE @Texto + '%'";
                    break;
                case "Correo":
                    whereClause = "Correo LIKE @Texto + '%'";
                    break;
                default:
                    whereClause = "1=0"; // Condición imposible si el tipo no es válido
                    break;
            }

            string query = @"
                SELECT IdCliente, Nombre, ApellidoPaterno, ApellidoMaterno, 
                       Ciudad, Estado, Pais, RFC, Correo, 
                       TelefonoCasa, TelefonoCelular, FechaNacimiento, EstadoCivil
                FROM Clientes
                WHERE " + whereClause;

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Texto", textoBusqueda)
            };

            DataTable dtClientes = Database.ExecuteQuery(query, parameters);

            foreach (DataRow row in dtClientes.Rows)
            {
                Cliente cliente = new Cliente
                {
                    IdCliente = Convert.ToInt32(row["IdCliente"]),
                    Nombre = row["Nombre"].ToString(),
                    ApellidoPaterno = row["ApellidoPaterno"].ToString(),
                    ApellidoMaterno = row["ApellidoMaterno"].ToString(),
                    Ciudad = row["Ciudad"].ToString(),
                    Estado = row["Estado"].ToString(),
                    Pais = row["Pais"].ToString(),
                    RFC = row["RFC"].ToString(),
                    Correo = row["Correo"].ToString(),
                    TelefonoCasa = row["TelefonoCasa"].ToString(),
                    TelefonoCelular = row["TelefonoCelular"].ToString(),
                    FechaNacimiento = Convert.ToDateTime(row["FechaNacimiento"]),
                    EstadoCivil = row["EstadoCivil"].ToString()
                };

                clientes.Add(cliente);
            }

            return clientes;
        }

        // Corregido el nombre del evento para que coincida con el control
        private void dgvResultadosClientes_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvResultadosClientes.SelectedRows.Count > 0)
            {
                Cliente clienteSeleccionado = (Cliente)dgvResultadosClientes.SelectedRows[0].DataBoundItem;
                SeleccionarCliente(clienteSeleccionado);
            }
        }

        // Método nuevo para cargar las reservaciones existentes
        private void CargarReservacionesExistentes(int idCliente)
        {
            try
            {
                // Consulta para obtener reservaciones activas (confirmadas o en check-in)
                string query = @"
            SELECT r.IdReservacion, r.CodigoReservacion, r.EstadoReservacion,
                   r.FechaCheckIn, r.FechaCheckOut, h.Nombre AS NombreHotel,
                   th.IdTipoHabitacion, th.Nombre AS TipoHabitacion, 
                   c.Nombre AS TipoCama, th.PrecioPorNoche, dr.CantidadPersonas,
                   hab.NumeroHabitacion
            FROM Reservaciones r
            INNER JOIN Hoteles h ON r.IdHotel = h.IdHotel
            INNER JOIN DetalleReservaciones dr ON r.IdReservacion = dr.IdReservacion
            INNER JOIN Habitaciones hab ON dr.IdHabitacion = hab.IdHabitacion
            INNER JOIN TiposHabitacion th ON hab.IdTipoHabitacion = th.IdTipoHabitacion
            INNER JOIN CatalogoTiposCama c ON th.IdTipoCama = c.IdTipoCama
            WHERE r.IdCliente = @IdCliente
            AND r.EstadoReservacion IN ('Confirmada', 'CheckIn')
            AND r.FechaCheckOut >= @FechaActual
            ORDER BY r.FechaCheckIn";

                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdCliente", idCliente),
            new SqlParameter("@FechaActual", DateTime.Today)
        };

                DataTable dtReservaciones = Database.ExecuteQuery(query, parameters);

                // Lista para habitaciones ya reservadas (no se pueden quitar)
                List<TipoHabitacion> habitacionesYaReservadas = new List<TipoHabitacion>();

                foreach (DataRow row in dtReservaciones.Rows)
                {
                    TipoHabitacion habitacionReservada = new TipoHabitacion
                    {
                        IdTipoHabitacion = Convert.ToInt32(row["IdTipoHabitacion"]),
                        Nombre = row["TipoHabitacion"].ToString(),
                        TipoCama = row["TipoCama"].ToString(),
                        PrecioPorNoche = Convert.ToDecimal(row["PrecioPorNoche"]),
                        PersonasAsignadas = Convert.ToInt32(row["CantidadPersonas"]),
                        // Propiedades adicionales para identificar que ya está reservada
                        ReservacionExistente = true,
                        NumeroHabitacion = row["NumeroHabitacion"].ToString(),
                        CodigoReservacion = row["CodigoReservacion"].ToString(),
                        FechaCheckIn = Convert.ToDateTime(row["FechaCheckIn"]),
                        FechaCheckOut = Convert.ToDateTime(row["FechaCheckOut"]),
                        EstadoReservacion = row["EstadoReservacion"].ToString(),
                        NombreHotel = row["NombreHotel"].ToString()
                    };

                    habitacionesYaReservadas.Add(habitacionReservada);
                }

                // Agregar a la lista principal pero marcarlas como existentes
                if (habitacionesYaReservadas.Count > 0)
                {
                    // Agregar a la lista principal sin mostrar mensaje
                    habitacionesSeleccionadas.AddRange(habitacionesYaReservadas);

                    // Actualizar la vista
                    ActualizarDataGridViewHabitacionesReservadas();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar reservaciones existentes: {ex.Message}",
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SeleccionarCliente(Cliente cliente)
        {
            // PRIMERO: Limpiar TODA la interfaz de reservación anterior
            // Esto incluye: habitaciones seleccionadas, precios, campos de resumen, etc.

            // Limpiar la lista de habitaciones seleccionadas
            habitacionesSeleccionadas.Clear();

            // Limpiar la vista de habitaciones reservadas
            ActualizarDataGridViewHabitacionesReservadas();

            // Limpiar la vista de habitaciones disponibles
            dgvHabitacionesDisponibles.DataSource = null;

            // Reiniciar los valores de la interfaz
            anticipoPagado = false;
            metodoPagoSeleccionado = "";
            referenciaSeleccionada = "";

            // Reiniciar los controles de pago
            btnPagarAnticipo.Text = "Pagar anticipo";
            btnPagarAnticipo.BackColor = SystemColors.Control;
            btnPagarAnticipo.ForeColor = SystemColors.ControlText;
            cboMetodoPago.Enabled = true;
            cboMetodoPago.SelectedIndex = 0;
            txtReferencia.Clear();
            txtReferencia.Enabled = false;

            // Reiniciar el resumen de precios
            lblSubtotalHospedaje.Text = "$0.00";
            lblAnticipoRequerido.Text = "$0.00";
            lblTotalCheckOut.Text = "$0.00";
            lblCodigoReservacion.Text = "";

            // Reiniciar el botón de confirmar
            btnConfirmar.Enabled = false;
            btnPagarAnticipo.Enabled = false;
            btnConfirmar.Text = "Confirmar reservación";
            btnConfirmar.BackColor = SystemColors.Control;
            btnConfirmar.ForeColor = SystemColors.ControlText;

            // IMPORTANTE: No deshabilitamos btnAgregar y btnQuitarHabitacion aquí,
            // se habilitarán/deshabilitarán según sea necesario en otros métodos

            // SEGUNDO: Establecer los datos del nuevo cliente
            lblCliente.Text = cliente.NombreCompleto;
            lblRFC.Text = cliente.RFC;
            lblCorreo.Text = cliente.Correo;

            // Guardar el cliente seleccionado para usarlo en la reservación
            clienteActual = cliente;

            // TERCERO: Habilitar controles para la nueva reservación
            // Habilitar la sección de selección de destino y fecha
            HabilitarSeleccionDestino(true);
            btnBuscarDestino.Enabled = true;
            dtpCheckIn.Enabled = true;
            dtpCheckOut.Enabled = true;

            // CUARTO: Cargar las reservaciones existentes del nuevo cliente
            CargarReservacionesExistentes(cliente.IdCliente);
        }

        // Método para habilitar o deshabilitar la sección de selección de destino
        private void HabilitarSeleccionDestino(bool habilitar)
        {
            // Habilitar o deshabilitar controles de selección de destino
            cboCiudad.Enabled = habilitar;
            cboHotel.Enabled = habilitar;
            dtpCheckIn.Enabled = habilitar;
            dtpCheckOut.Enabled = habilitar;
            txtNoches.Enabled = habilitar;
            btnBuscarDestino.Enabled = habilitar;

            // Si se deshabilita, limpiar los controles
            if (!habilitar)
            {
                cboCiudad.SelectedIndex = -1;
                cboHotel.SelectedIndex = -1;
                txtNoches.Text = string.Empty;
                // Limpiar la tabla de habitaciones si existe
                // ...
            }
        }

        // Método para calcular el número de noches entre check-in y check-out
        private void CalcularNoches()
        {
            try
            {
                // Calcular la diferencia en días
                TimeSpan diferencia = dtpCheckOut.Value.Date - dtpCheckIn.Value.Date;
                int noches = diferencia.Days;

                // Validar que no sea negativo
                if (noches < 0)
                {
                    MessageBox.Show("La fecha de Check-Out debe ser posterior a la fecha de Check-In",
                                  "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    // Corregir la fecha de Check-Out automáticamente
                    dtpCheckOut.Value = dtpCheckIn.Value.AddDays(1);
                    noches = 1;
                }

                // Actualizar el campo de noches
                txtNoches.Text = noches.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al calcular noches: {ex.Message}",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Manejador para el botón de buscar destino/hoteles
        private void btnBuscarDestino_Click_1(object sender, EventArgs e)
        {
            // Verificar que se haya seleccionado ciudad y hotel
            if (cboCiudad.SelectedIndex == -1 || cboHotel.SelectedIndex == -1)
            {
                MessageBox.Show("Por favor seleccione ciudad y hotel", "Datos incompletos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que las fechas sean válidas
            if (dtpCheckIn.Value.Date < DateTime.Today)
            {
                MessageBox.Show("La fecha de check-in no puede ser anterior a hoy",
                                "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Aquí implementarías la búsqueda de habitaciones disponibles
            BuscarHabitacionesDisponibles();
        }


        // Método para buscar habitaciones disponibles
        // Implementación del método para buscar habitaciones disponibles
        private void BuscarHabitacionesDisponibles()
        {
            try
            {
                // Obtener los datos seleccionados
                if (cboHotel.SelectedIndex <= 0)
                {
                    MessageBox.Show("Por favor seleccione un hotel", "Hotel no seleccionado",
                                   MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener el ID del hotel seleccionado
                string nombreHotel = cboHotel.SelectedItem.ToString();
                int idHotel = hotelesDisponibles.FirstOrDefault(h => h.Value == nombreHotel).Key;

                DateTime fechaCheckIn = dtpCheckIn.Value.Date;
                DateTime fechaCheckOut = dtpCheckOut.Value.Date;

                // Validar fechas
                if (fechaCheckIn < DateTime.Today)
                {
                    MessageBox.Show("La fecha de check-in no puede ser anterior a hoy",
                                   "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (fechaCheckOut <= fechaCheckIn)
                {
                    MessageBox.Show("La fecha de check-out debe ser posterior a la fecha de check-in",
                                   "Fecha inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Consulta para encontrar habitaciones disponibles
                string query = @"
        SELECT 
            th.IdTipoHabitacion,
            th.Nombre AS TipoHabitacion,
            th.Descripcion AS Caracteristicas,
            c.Nombre AS TipoCama,
            n.Nombre AS Nivel,
            th.PrecioPorNoche,
            th.CapacidadPersonas,
            th.Ubicacion,
            (
                SELECT COUNT(h.IdHabitacion)
                FROM Habitaciones h
                WHERE h.IdTipoHabitacion = th.IdTipoHabitacion
                AND h.Estado = 'Disponible'
                AND h.IdHabitacion NOT IN (
                    -- Habitaciones ocupadas en el rango de fechas
                    SELECT dr.IdHabitacion
                    FROM DetalleReservaciones dr
                    JOIN Reservaciones r ON dr.IdReservacion = r.IdReservacion
                    WHERE r.IdHotel = @IdHotel
                    AND r.EstadoReservacion IN ('Confirmada', 'CheckIn')
                    AND (
                        (r.FechaCheckIn <= @FechaCheckOut AND r.FechaCheckOut >= @FechaCheckIn)
                    )
                )
            ) AS HabitacionesDisponibles
        FROM TiposHabitacion th
        INNER JOIN CatalogoTiposCama c ON th.IdTipoCama = c.IdTipoCama
        INNER JOIN CatalogoNivelesHabitacion n ON th.IdNivel = n.IdNivel
        WHERE th.IdHotel = @IdHotel
        AND th.EstadoActivo = 1
        ORDER BY th.PrecioPorNoche";

                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@FechaCheckIn", fechaCheckIn),
            new SqlParameter("@FechaCheckOut", fechaCheckOut)
        };

                DataTable dtHabitaciones = Database.ExecuteQuery(query, parameters);

                // Configurar el DataGridView de habitaciones
                ConfigurarDataGridViewHabitaciones();

                EstiloDataGridViewHabitacionesDisponibles();

                // Crear lista de tipos de habitación disponibles
                List<TipoHabitacion> tiposHabitacion = new List<TipoHabitacion>();

                foreach (DataRow row in dtHabitaciones.Rows)
                {
                    TipoHabitacion tipo = new TipoHabitacion
                    {
                        IdTipoHabitacion = Convert.ToInt32(row["IdTipoHabitacion"]),
                        Nombre = row["TipoHabitacion"].ToString(),
                        Caracteristicas = row["Caracteristicas"].ToString(),
                        TipoCama = row["TipoCama"].ToString(),
                        Nivel = row["Nivel"].ToString(),
                        PrecioPorNoche = Convert.ToDecimal(row["PrecioPorNoche"]),
                        CapacidadPersonas = Convert.ToInt32(row["CapacidadPersonas"]),
                        HabitacionesDisponibles = Convert.ToInt32(row["HabitacionesDisponibles"]),
                        CantidadSeleccionada = 0,
                        PersonasAsignadas = 0
                    };

                    tiposHabitacion.Add(tipo);
                }

                // Ajustar la disponibilidad considerando las habitaciones ya seleccionadas pero aún no reservadas formalmente (las que están en nuestra selección local)
                foreach (var tipo in tiposHabitacion)
                {
                    // Contar cuántas habitaciones de este tipo ya tenemos seleccionadas pero que aún no son reservaciones existentes (no están en la BD)
                    int cantidadYaSeleccionada = habitacionesSeleccionadas
                        .Where(h => !h.ReservacionExistente && h.IdTipoHabitacion == tipo.IdTipoHabitacion)
                        .Count();

                    // Restar de la disponibilidad real
                    tipo.HabitacionesDisponibles -= cantidadYaSeleccionada;

                    // Asegurarse de que no sea negativo
                    if (tipo.HabitacionesDisponibles < 0)
                        tipo.HabitacionesDisponibles = 0;
                }

                // Asignar al DataGridView
                dgvHabitacionesDisponibles.DataSource = tiposHabitacion;

                // Habilitar el botón de agregar si hay habitaciones disponibles
                btnAgregar.Enabled = tiposHabitacion.Any(t => t.HabitacionesDisponibles > 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar habitaciones disponibles: {ex.Message}",
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarDataGridViewHabitaciones()
        {
            // Configurar el DataGridView
            dgvHabitacionesDisponibles.AutoGenerateColumns = false;

            // Limpiar columnas existentes
            dgvHabitacionesDisponibles.Columns.Clear();

            // Añadir columnas
            DataGridViewTextBoxColumn colTipo = new DataGridViewTextBoxColumn();
            colTipo.DataPropertyName = "Nombre";
            colTipo.HeaderText = "Tipo de habitación";
            colTipo.Width = 120;
            dgvHabitacionesDisponibles.Columns.Add(colTipo);

            DataGridViewTextBoxColumn colCaracteristicas = new DataGridViewTextBoxColumn();
            colCaracteristicas.DataPropertyName = "Caracteristicas";
            colCaracteristicas.HeaderText = "Características";
            colCaracteristicas.Width = 150;
            dgvHabitacionesDisponibles.Columns.Add(colCaracteristicas);

            DataGridViewTextBoxColumn colTipoCama = new DataGridViewTextBoxColumn();
            colTipoCama.DataPropertyName = "TipoCama";
            colTipoCama.HeaderText = "Tipo de Cama";
            colTipoCama.Width = 100;
            dgvHabitacionesDisponibles.Columns.Add(colTipoCama);

            DataGridViewTextBoxColumn colNivel = new DataGridViewTextBoxColumn();
            colNivel.DataPropertyName = "Nivel";
            colNivel.HeaderText = "Nivel";
            colNivel.Width = 100;
            dgvHabitacionesDisponibles.Columns.Add(colNivel);

            DataGridViewTextBoxColumn colPrecio = new DataGridViewTextBoxColumn();
            colPrecio.DataPropertyName = "PrecioPorNoche";
            colPrecio.HeaderText = "Precio/noche";
            colPrecio.Width = 100;
            colPrecio.DefaultCellStyle.Format = "C2";
            dgvHabitacionesDisponibles.Columns.Add(colPrecio);

            DataGridViewTextBoxColumn colCapacidad = new DataGridViewTextBoxColumn();
            colCapacidad.DataPropertyName = "CapacidadPersonas";
            colCapacidad.HeaderText = "Capacidad";
            colCapacidad.Width = 80;
            dgvHabitacionesDisponibles.Columns.Add(colCapacidad);

            DataGridViewTextBoxColumn colDisponibles = new DataGridViewTextBoxColumn();
            colDisponibles.DataPropertyName = "HabitacionesDisponibles";
            colDisponibles.HeaderText = "Disponibles";
            colDisponibles.Width = 80;
            dgvHabitacionesDisponibles.Columns.Add(colDisponibles);

            // Configuraciones adicionales
            dgvHabitacionesDisponibles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHabitacionesDisponibles.AllowUserToAddRows = false;
            dgvHabitacionesDisponibles.AllowUserToDeleteRows = false;
            dgvHabitacionesDisponibles.ReadOnly = true; // Todo es de solo lectura ahora
            dgvHabitacionesDisponibles.MultiSelect = false;


        }

        private void ConfigurarDataGridViewHabitacionesReservadas()
        {
            // Configurar el DataGridView
            dgvHabitacionesReservadas.AutoGenerateColumns = false;

            // Limpiar columnas existentes
            dgvHabitacionesReservadas.Columns.Clear();

            // Añadir columnas
            DataGridViewTextBoxColumn colTipo = new DataGridViewTextBoxColumn();
            colTipo.DataPropertyName = "Nombre";
            colTipo.HeaderText = "Tipo de habitación";
            colTipo.Width = 120;
            dgvHabitacionesReservadas.Columns.Add(colTipo);

            DataGridViewTextBoxColumn colTipoCama = new DataGridViewTextBoxColumn();
            colTipoCama.DataPropertyName = "TipoCama";
            colTipoCama.HeaderText = "Tipo de Cama";
            colTipoCama.Width = 100;
            dgvHabitacionesReservadas.Columns.Add(colTipoCama);

            DataGridViewTextBoxColumn colPrecio = new DataGridViewTextBoxColumn();
            colPrecio.DataPropertyName = "PrecioPorNoche";
            colPrecio.HeaderText = "Precio/noche";
            colPrecio.Width = 80;
            colPrecio.DefaultCellStyle.Format = "C2";
            dgvHabitacionesReservadas.Columns.Add(colPrecio);

            DataGridViewTextBoxColumn colPersonas = new DataGridViewTextBoxColumn();
            colPersonas.DataPropertyName = "PersonasAsignadas";
            colPersonas.HeaderText = "Personas";
            colPersonas.Width = 70;
            dgvHabitacionesReservadas.Columns.Add(colPersonas);

            // Configuraciones adicionales
            dgvHabitacionesReservadas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHabitacionesReservadas.AllowUserToAddRows = false;
            dgvHabitacionesReservadas.AllowUserToDeleteRows = false;
            dgvHabitacionesReservadas.ReadOnly = true;
            dgvHabitacionesReservadas.MultiSelect = false;

            // Al final del método:
            EstiloDataGridViewHabitacionesReservadas();


        }

        // Otros manejadores de eventos que ya tenías
        private void label22_Click(object sender, EventArgs e)
        {
            // Si lo quito, deja de funcionarxd
        }

        private void panel5_Paint(object sender, PaintEventArgs e)
        {
            // Si lo quito, deja de funcionar
        }

        private void btnLimpiarFiltro_Click(object sender, EventArgs e)
        {
            txtBusqueda.Text = string.Empty;
            cargarClientes();
        }

        // Método para cargar las ciudades en el ComboBox
        private void CargarCiudades()
        {
            try
            {
                string query = @"
            SELECT DISTINCT Ciudad 
            FROM Hoteles 
            WHERE EstadoActivo = 1 
            ORDER BY Ciudad";

                DataTable dtCiudades = Database.ExecuteQuery(query);

                // Limpiar y configurar el ComboBox
                cboCiudad.Items.Clear();
                cboCiudad.Items.Add("-- Seleccione una ciudad --");

                foreach (DataRow row in dtCiudades.Rows)
                {
                    cboCiudad.Items.Add(row["Ciudad"].ToString());
                }

                cboCiudad.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ciudades: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }

        private void btnGestionClientes_Click(object sender, EventArgs e)
        {
            GestionClientes gestionClientes = new GestionClientes();
            gestionClientes.Show();
            this.Close();
        }

        // Diccionario para almacenar los IDs de los hoteles
        private Dictionary<int, string> hotelesDisponibles = new Dictionary<int, string>();

        private void cboCiudad_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Limpiamos el combo de hoteles
            cboHotel.Items.Clear();

            // Si no es la primera opción
            if (cboCiudad.SelectedIndex > 0)
            {
                string ciudadSeleccionada = cboCiudad.SelectedItem.ToString();
                cargarHotelesPorCiudad(ciudadSeleccionada);
            }
            else
            {
                cboHotel.Items.Add("-- Seleccione un hotel --");
                cboHotel.SelectedIndex = 0;
            }
        }

        private void cargarHotelesPorCiudad(string ciudad)
        {
            try
            {
                string query = @"
            SELECT IdHotel, Nombre 
            FROM Hoteles 
            WHERE Ciudad = @Ciudad AND EstadoActivo = 1
            ORDER BY Nombre";

                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@Ciudad", ciudad)
        };

                DataTable dtHoteles = Database.ExecuteQuery(query, parameters);

                // Configuramos el ComboBox
                cboHotel.Items.Add("-- Seleccione un hotel --");

                // Limpiamos el diccionario actual
                hotelesDisponibles.Clear();

                foreach (DataRow row in dtHoteles.Rows)
                {
                    int idHotel = Convert.ToInt32(row["IdHotel"]);
                    string nombreHotel = row["Nombre"].ToString();

                    hotelesDisponibles.Add(idHotel, nombreHotel);
                    cboHotel.Items.Add(nombreHotel);
                }

                cboHotel.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar hoteles: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void dtpCheckIn_ValueChanged(object sender, EventArgs e)
        {
            // Si la fecha de check-in es posterior o igual a la de check-out, ajustar la fecha de check-out
            if (dtpCheckIn.Value.Date >= dtpCheckOut.Value.Date)
            {
                dtpCheckOut.Value = dtpCheckIn.Value.AddDays(1);
            }

            CalcularNoches();
        }

        private void dtpCheckOut_ValueChanged(object sender, EventArgs e)
        {
            // Si la fecha de check-out es anterior o igual a la de check-in, ajustar la fecha de check-in
            if (dtpCheckOut.Value.Date <= dtpCheckIn.Value.Date)
            {
                dtpCheckIn.Value = dtpCheckOut.Value.AddDays(-1);
            }

            CalcularNoches();
        }  

        private void ActualizarDataGridViewHabitacionesReservadas()
        {
            dgvHabitacionesReservadas.DataSource = null;
            dgvHabitacionesReservadas.DataSource = new BindingList<TipoHabitacion>(habitacionesSeleccionadas);
            EstiloDataGridViewHabitacionesReservadas();
        }

        private void ActualizarResumenReservacion()
        {
            int totalPersonas = 0;
            decimal subtotalHospedaje = 0;
            int noches = int.Parse(txtNoches.Text);

            // Lista temporal para habitaciones no existentes (nuevas)
            List<TipoHabitacion> habitacionesNuevas = habitacionesSeleccionadas
                .Where(h => !h.ReservacionExistente)
                .ToList();

            foreach (TipoHabitacion tipo in habitacionesNuevas)
            {
                totalPersonas += tipo.PersonasAsignadas;
                subtotalHospedaje += tipo.PrecioPorNoche * noches;
            }

            // Actualizar la sección de resumen
            lblSubtotalHospedaje.Text = subtotalHospedaje.ToString("C2");

            // Calcular anticipo (30%)
            decimal anticipo = Math.Round(subtotalHospedaje * 0.3m, 2);
            lblAnticipoRequerido.Text = anticipo.ToString("C2");

            // Calcular total al check-out
            decimal totalCheckOut = subtotalHospedaje - anticipo;
            lblTotalCheckOut.Text = totalCheckOut.ToString("C2");

            // Habilitar o deshabilitar controles según estado
            // Solo habilitar si hay habitaciones nuevas (no reservadas anteriormente)
            btnPagarAnticipo.Enabled = habitacionesNuevas.Count > 0;
            btnConfirmar.Enabled = habitacionesNuevas.Count > 0 && anticipoPagado;

            // Si se quitaron todas las habitaciones nuevas y ya se había pagado el anticipo, reiniciar
            if (habitacionesNuevas.Count == 0 && anticipoPagado)
            {
                anticipoPagado = false;
                btnPagarAnticipo.Text = "Pagar anticipo";
                btnPagarAnticipo.BackColor = SystemColors.Control;
                btnPagarAnticipo.ForeColor = SystemColors.ControlText;
                cboMetodoPago.Enabled = true;
                cboMetodoPago.SelectedIndex = 0;
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            // Verificar que haya una fila seleccionada
            if (dgvHabitacionesDisponibles.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione una habitación de la lista.",
                                "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener el tipo de habitación seleccionado
            TipoHabitacion tipoOriginal = (TipoHabitacion)dgvHabitacionesDisponibles.SelectedRows[0].DataBoundItem;

            // Verificar que haya habitaciones disponibles
            if (tipoOriginal.HabitacionesDisponibles <= 0)
            {
                MessageBox.Show("No hay habitaciones disponibles de este tipo.",
                                "Sin disponibilidad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que se haya ingresado un número de personas
            if (numericUpDownTotal.Value <= 0)
            {
                MessageBox.Show("Por favor, indique el número de personas a hospedar.",
                                "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar que no exceda la capacidad de la habitación
            if (numericUpDownTotal.Value > tipoOriginal.CapacidadPersonas)
            {
                MessageBox.Show($"La habitación de tipo {tipoOriginal.Nombre} sólo tiene capacidad para {tipoOriginal.CapacidadPersonas} personas.\n\nNo puede asignar {numericUpDownTotal.Value} personas a esta habitación.",
                               "Capacidad excedida", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Ajustar automáticamente al máximo permitido
                numericUpDownTotal.Value = tipoOriginal.CapacidadPersonas;
                return;
            }

            TipoHabitacion tipoSeleccionado = new TipoHabitacion
            {
                IdTipoHabitacion = tipoOriginal.IdTipoHabitacion,
                Nombre = tipoOriginal.Nombre,
                Caracteristicas = tipoOriginal.Caracteristicas,
                TipoCama = tipoOriginal.TipoCama,
                Nivel = tipoOriginal.Nivel,
                Ubicacion = tipoOriginal.Ubicacion,
                PrecioPorNoche = tipoOriginal.PrecioPorNoche,
                CapacidadPersonas = tipoOriginal.CapacidadPersonas,
                HabitacionesDisponibles = tipoOriginal.HabitacionesDisponibles,
                CantidadSeleccionada = 1,
                PersonasAsignadas = (int)numericUpDownTotal.Value
            };

            // Agregamos a la lista de seleccionados
            habitacionesSeleccionadas.Add(tipoSeleccionado);

            // Actualizamos disponibilidad en la tabla original
            tipoOriginal.HabitacionesDisponibles -= 1;

            // Actualizamos las vistas
            dgvHabitacionesDisponibles.Refresh();
            ActualizarDataGridViewHabitacionesReservadas();

            // Calculamos totales
            ActualizarResumenReservacion();

            // Mostramos mensaje de confirmación
            MessageBox.Show($"Se ha agregado una habitación {tipoSeleccionado.Nombre} para {tipoSeleccionado.PersonasAsignadas} personas.",
                           "Habitación agregada", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private string GenerarCodigoReservacion()
        {
            return Guid.NewGuid().ToString();
        }

        private void btnConfirmar_Click(object sender, EventArgs e)
        {
            try
            {
                // Filtrar solo las habitaciones nuevas
                List<TipoHabitacion> habitacionesNuevas = habitacionesSeleccionadas
                    .Where(h => !h.ReservacionExistente)
                    .ToList();

                // Verificar que haya habitaciones seleccionadas
                if (habitacionesNuevas.Count == 0)
                {
                    MessageBox.Show("Por favor, seleccione al menos una habitación nueva para reservar.",
                                   "Sin selección", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verificar que se haya pagado el anticipo
                if (!anticipoPagado)
                {
                    MessageBox.Show("Por favor, realice el pago del anticipo antes de confirmar la reservación.",
                                   "Anticipo pendiente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener datos necesarios
                string nombreHotel = cboHotel.SelectedItem.ToString();
                int idHotel = hotelesDisponibles.FirstOrDefault(h => h.Value == nombreHotel).Key;

                DateTime fechaCheckIn = dtpCheckIn.Value.Date;
                DateTime fechaCheckOut = dtpCheckOut.Value.Date;

                // Calcular total de personas y monto de anticipo
                int totalPersonas = 0;
                decimal subtotalHospedaje = 0;
                int noches = int.Parse(txtNoches.Text);

                foreach (TipoHabitacion tipo in habitacionesNuevas)
                {
                    totalPersonas += tipo.PersonasAsignadas;
                    subtotalHospedaje += tipo.PrecioPorNoche * noches;
                }

                decimal montoAnticipo = Math.Round(subtotalHospedaje * 0.3m, 2);

                // Confirmar con el usuario
                DialogResult confirmacion = MessageBox.Show(
                    $"Se realizará una reservación para {clienteActual.NombreCompleto} en {nombreHotel}.\n" +
                    $"Fechas: del {fechaCheckIn.ToString("dd/MM/yyyy")} al {fechaCheckOut.ToString("dd/MM/yyyy")}.\n" +
                    $"Total de personas: {totalPersonas}\n" +
                    $"Anticipo pagado: {montoAnticipo.ToString("C2")}\n" +
                    $"Método de pago: {metodoPagoSeleccionado}\n" +
                    (string.IsNullOrEmpty(referenciaSeleccionada) ? "" : $"Referencia: {referenciaSeleccionada}\n") +
                    "\n¿Desea confirmar la reservación?",
                    "Confirmar reservación",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (confirmacion != DialogResult.Yes)
                {
                    return;
                }

                // Generar código GUID para la reservación
                string codigoReservacion = GenerarCodigoReservacion();

                // Ejecutar en una transacción
                using (SqlConnection connection = new SqlConnection(Database.ConnectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Insertar la reservación
                        string queryReservacion = @"
                        INSERT INTO Reservaciones (
                            CodigoReservacion, IdCliente, IdHotel, 
                            FechaCheckIn, FechaCheckOut, CantidadPersonas,
                            MontoAnticipo, EstadoReservacion, UsuarioRegistro
                        )
                        VALUES (
                            @CodigoReservacion, @IdCliente, @IdHotel,
                            @FechaCheckIn, @FechaCheckOut, @CantidadPersonas,
                            @MontoAnticipo, 'Confirmada', @UsuarioRegistro
                        );
                        SELECT SCOPE_IDENTITY();";

                        SqlCommand cmdReservacion = new SqlCommand(queryReservacion, connection, transaction);
                        cmdReservacion.Parameters.AddWithValue("@CodigoReservacion", codigoReservacion);
                        cmdReservacion.Parameters.AddWithValue("@IdCliente", clienteActual.IdCliente);
                        cmdReservacion.Parameters.AddWithValue("@IdHotel", idHotel);
                        cmdReservacion.Parameters.AddWithValue("@FechaCheckIn", fechaCheckIn);
                        cmdReservacion.Parameters.AddWithValue("@FechaCheckOut", fechaCheckOut);
                        cmdReservacion.Parameters.AddWithValue("@CantidadPersonas", totalPersonas);
                        cmdReservacion.Parameters.AddWithValue("@MontoAnticipo", montoAnticipo);
                        cmdReservacion.Parameters.AddWithValue("@UsuarioRegistro", Session.IdUsuario);

                        // Ejecutar y obtener el ID de la reservación
                        int idReservacion = Convert.ToInt32(cmdReservacion.ExecuteScalar());

                        // Registrar el pago del anticipo
                        string queryPago = @"
                        INSERT INTO Pagos (
                            IdReservacion, TipoPago, Monto, MetodoPago, 
                            NumeroReferencia, UsuarioRegistro
                        )
                        VALUES (
                            @IdReservacion, 'Anticipo', @Monto, @MetodoPago,
                            @NumeroReferencia, @UsuarioRegistro
                        )";

                        SqlCommand cmdPago = new SqlCommand(queryPago, connection, transaction);
                        cmdPago.Parameters.AddWithValue("@IdReservacion", idReservacion);
                        cmdPago.Parameters.AddWithValue("@Monto", montoAnticipo);
                        cmdPago.Parameters.AddWithValue("@MetodoPago", metodoPagoSeleccionado);
                        cmdPago.Parameters.AddWithValue("@NumeroReferencia",
                            string.IsNullOrEmpty(referenciaSeleccionada) ? DBNull.Value : (object)referenciaSeleccionada);
                        cmdPago.Parameters.AddWithValue("@UsuarioRegistro", Session.IdUsuario);

                        cmdPago.ExecuteNonQuery();

                        // Insertar los detalles de cada habitación nueva
                        foreach (TipoHabitacion tipo in habitacionesNuevas)
                        {
                            // Buscar habitaciones disponibles de este tipo
                            string queryHabitaciones = @"
                            SELECT TOP 1 h.IdHabitacion
                            FROM Habitaciones h
                            WHERE h.IdTipoHabitacion = @IdTipoHabitacion
                            AND h.Estado = 'Disponible'
                            AND h.IdHabitacion NOT IN (
                                SELECT dr.IdHabitacion
                                FROM DetalleReservaciones dr
                                JOIN Reservaciones r ON dr.IdReservacion = r.IdReservacion
                                WHERE r.EstadoReservacion IN ('Confirmada', 'CheckIn')
                                AND (
                                    (r.FechaCheckIn <= @FechaCheckOut AND r.FechaCheckOut >= @FechaCheckIn)
                                )
                            )";

                            SqlCommand cmdHabitacion = new SqlCommand(queryHabitaciones, connection, transaction);
                            cmdHabitacion.Parameters.AddWithValue("@IdTipoHabitacion", tipo.IdTipoHabitacion);
                            cmdHabitacion.Parameters.AddWithValue("@FechaCheckIn", fechaCheckIn);
                            cmdHabitacion.Parameters.AddWithValue("@FechaCheckOut", fechaCheckOut);

                            int idHabitacion = 0;
                            using (SqlDataReader reader = cmdHabitacion.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    idHabitacion = Convert.ToInt32(reader["IdHabitacion"]);
                                }
                                else
                                {
                                    throw new Exception($"No se encontró una habitación disponible del tipo {tipo.Nombre}");
                                }
                            }

                            // Insertar el detalle de reservación
                            string queryDetalle = @"
                            INSERT INTO DetalleReservaciones (
                                IdReservacion, IdHabitacion, PrecioPorNoche, CantidadPersonas, UsuarioRegistro
                            )
                            VALUES (
                                @IdReservacion, @IdHabitacion, @PrecioPorNoche, @CantidadPersonas, @UsuarioRegistro
                            )";

                            SqlCommand cmdDetalle = new SqlCommand(queryDetalle, connection, transaction);
                            cmdDetalle.Parameters.AddWithValue("@IdReservacion", idReservacion);
                            cmdDetalle.Parameters.AddWithValue("@IdHabitacion", idHabitacion);
                            cmdDetalle.Parameters.AddWithValue("@PrecioPorNoche", tipo.PrecioPorNoche);
                            cmdDetalle.Parameters.AddWithValue("@CantidadPersonas", tipo.PersonasAsignadas);
                            cmdDetalle.Parameters.AddWithValue("@UsuarioRegistro", Session.IdUsuario);

                            cmdDetalle.ExecuteNonQuery();
                        }

                        // Confirmar la transacción
                        transaction.Commit();

                        // Actualizar el código de reservación en la UI
                        lblCodigoReservacion.Text = codigoReservacion;

                        // Mensaje de éxito
                        MessageBox.Show(
                            $"¡Reservación creada exitosamente!\n\n" +
                            $"Código de reservación: {codigoReservacion}\n" +
                            $"Este código será necesario para el check-in.\n\n" +
                            $"Anticipo pagado: {montoAnticipo.ToString("C2")}\n" +
                            $"Método de pago: {metodoPagoSeleccionado}" +
                            (string.IsNullOrEmpty(referenciaSeleccionada) ? "" : $"\nReferencia: {referenciaSeleccionada}"),
                            "Reservación confirmada",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        // Marcar las habitaciones recién reservadas como existentes
                        foreach (TipoHabitacion tipo in habitacionesNuevas)
                        {
                            tipo.ReservacionExistente = true;
                            tipo.CodigoReservacion = codigoReservacion;
                            tipo.FechaCheckIn = fechaCheckIn;
                            tipo.FechaCheckOut = fechaCheckOut;
                            tipo.EstadoReservacion = "Confirmada";
                            tipo.NombreHotel = nombreHotel;
                        }

                        // Deshabilitar controles para evitar cambios
                        btnConfirmar.Enabled = false;
                        btnPagarAnticipo.Enabled = false;
                        btnAgregar.Enabled = false;
                        btnQuitarHabitacion.Enabled = false;

                        // Actualizar la vista
                        ActualizarDataGridViewHabitacionesReservadas();
                        ActualizarResumenReservacion();

                        // Limpiar la vista de habitaciones disponibles para evitar que se sigan agregando
                        dgvHabitacionesDisponibles.DataSource = null;

                        // Deshabilitar el botón de búsqueda para evitar que se busquen más habitaciones
                        btnBuscarDestino.Enabled = false;

                        // Deshabilitar los controles de fechas para evitar cambios
                        dtpCheckIn.Enabled = false;
                        dtpCheckOut.Enabled = false;

                        // Cambiar el texto del botón para indicar que la reservación ya se completó
                        btnConfirmar.Text = "Reservación confirmada ✓";
                        btnConfirmar.BackColor = Color.Green;
                        btnConfirmar.ForeColor = Color.White;

                        // Resetear variables para próximas reservaciones
                        anticipoPagado = false;
                        metodoPagoSeleccionado = "";
                        referenciaSeleccionada = "";
                    }
                    catch (Exception ex)
                    {
                        // Si hay error, revertir la transacción
                        transaction.Rollback();
                        throw ex;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al crear reservación: {ex.Message}",
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnQuitarHabitacion_Click(object sender, EventArgs e)
        {
            // Verificar que haya una fila seleccionada
            if (dgvHabitacionesReservadas.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione una habitación de la lista para quitar.",
                               "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtener el tipo de habitación seleccionado
            TipoHabitacion tipoSeleccionado = (TipoHabitacion)dgvHabitacionesReservadas.SelectedRows[0].DataBoundItem;

            // Verificar si es una reservación existente
            if (tipoSeleccionado.ReservacionExistente)
            {
                MessageBox.Show(
                    "No se puede quitar esta habitación porque pertenece a una reservación ya confirmada.\n\n" +
                    "Si desea cancelar esta reservación, utilice la opción de 'Cancelaciones' en el menú principal.",
                    "Reservación existente",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // Preguntar para confirmar
            DialogResult result = MessageBox.Show(
                $"¿Está seguro de quitar la habitación {tipoSeleccionado.Nombre}?",
                "Confirmar eliminación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // Buscar el tipo original en el DataGridView de disponibles para restaurar la disponibilidad
                foreach (DataGridViewRow row in dgvHabitacionesDisponibles.Rows)
                {
                    TipoHabitacion tipoOriginal = (TipoHabitacion)row.DataBoundItem;
                    if (tipoOriginal.IdTipoHabitacion == tipoSeleccionado.IdTipoHabitacion)
                    {
                        // Restaurar la disponibilidad
                        tipoOriginal.HabitacionesDisponibles += 1;
                        break;
                    }
                }

                // Quitar de la lista de seleccionados
                habitacionesSeleccionadas.Remove(tipoSeleccionado);

                // Actualizar las vistas
                dgvHabitacionesDisponibles.Refresh();
                ActualizarDataGridViewHabitacionesReservadas();

                // Actualizar el resumen
                ActualizarResumenReservacion();

                MessageBox.Show("La habitación ha sido quitada de la reservación.",
                               "Habitación quitada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
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

        private void btnPagarAnticipo_Click(object sender, EventArgs e)
        {
            // Verificar que hay habitaciones seleccionadas
            if (habitacionesSeleccionadas.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione al menos una habitación antes de pagar el anticipo.",
                               "Sin habitaciones", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Verificar si ya se pagó el anticipo
            if (anticipoPagado)
            {
                MessageBox.Show("El anticipo ya ha sido pagado.",
                               "Anticipo registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Validar método de pago
            if (cboMetodoPago.SelectedIndex == -1)
            {
                MessageBox.Show("Por favor seleccione un método de pago",
                               "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboMetodoPago.Focus();
                return;
            }

           
            if (cboMetodoPago.SelectedItem.ToString() != "Efectivo" &&
                string.IsNullOrWhiteSpace(txtReferencia.Text))
            {
                MessageBox.Show("Por favor ingrese el número de referencia o los últimos 4 dígitos",
                               "Dato requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtReferencia.Focus();
                return;
            }

            // Confirmar el pago
            DialogResult confirmacion = MessageBox.Show(
                $"¿Confirmar el pago del anticipo por {lblAnticipoRequerido.Text} con {cboMetodoPago.SelectedItem}?",
                "Confirmar pago",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion == DialogResult.Yes)
            {
                // Guardar datos
                metodoPagoSeleccionado = cboMetodoPago.SelectedItem.ToString();
                referenciaSeleccionada = txtReferencia.Text;
                anticipoPagado = true;

                // Cambiar texto del botón
                btnPagarAnticipo.Text = "Anticipo pagado ✓";
                btnPagarAnticipo.BackColor = Color.Green;
                btnPagarAnticipo.ForeColor = Color.White;

                // Deshabilitar los controles de método de pago
                cboMetodoPago.Enabled = false;
                txtReferencia.Enabled = false;

                // Habilitar botón confirmar
                btnConfirmar.Enabled = true;

                MessageBox.Show($"Anticipo registrado mediante {metodoPagoSeleccionado}\n" +
                               (string.IsNullOrEmpty(referenciaSeleccionada) ? "" : $"Referencia: {referenciaSeleccionada}"),
                               "Pago registrado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnCopiarCodigo_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar que haya un código para copiar
                if (string.IsNullOrEmpty(lblCodigoReservacion.Text))
                {
                    MessageBox.Show("No hay código de reservación para copiar.",
                        "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Copiar el texto al portapapeles
                Clipboard.SetText(lblCodigoReservacion.Text);

                // Mostrar confirmación
                MessageBox.Show("Código de reservación copiado al portapapeles.",
                    "Copiado", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al copiar al portapapeles: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cboMetodoPago_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Habilitar/deshabilitar el campo de referencia según el método de pago
            bool requiereReferencia = cboMetodoPago.SelectedItem != null &&
                                     cboMetodoPago.SelectedItem.ToString() != "Efectivo";

            txtReferencia.Enabled = requiereReferencia;


            if (!requiereReferencia)
                txtReferencia.Clear();
        }

        private void dgvHabitacionesDisponibles_SelectionChanged(object sender, EventArgs e)
        {
            // Actualizar el NumericUpDown de personas según la capacidad de la habitación
            if (dgvHabitacionesDisponibles.SelectedRows.Count > 0)
            {
                TipoHabitacion tipoSeleccionado = (TipoHabitacion)dgvHabitacionesDisponibles.SelectedRows[0].DataBoundItem;
                numericUpDownTotal.Maximum = tipoSeleccionado.CapacidadPersonas;
                numericUpDownTotal.Value = 1; // Por defecto, 1 persona

                // Habilitar el botón de agregar si hay habitaciones disponibles
                btnAgregar.Enabled = tipoSeleccionado.HabitacionesDisponibles > 0;
            }
            else
            {
                // Si no hay ninguna fila seleccionada, deshabilitamos el botón
                btnAgregar.Enabled = false;
            }
        }

        private void EstiloDataGridViewResultadosClientes()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvResultadosClientes.BorderStyle = BorderStyle.None;
            dgvResultadosClientes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvResultadosClientes.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvResultadosClientes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvResultadosClientes.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvResultadosClientes.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvResultadosClientes.EnableHeadersVisualStyles = false;
            dgvResultadosClientes.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvResultadosClientes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvResultadosClientes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResultadosClientes.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvResultadosClientes.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvResultadosClientes.ColumnHeadersHeight = 30; // Reducido de 40 a 30

            // Estilo para las filas y celdas - reducidas para mejor visualización
            dgvResultadosClientes.RowTemplate.Height = 25; // Reducido de 35 a 25
            dgvResultadosClientes.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvResultadosClientes.DefaultCellStyle.Padding = new Padding(3); // Reducido de 5 a 3
            dgvResultadosClientes.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvResultadosClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvResultadosClientes.BorderStyle = BorderStyle.FixedSingle;
            dgvResultadosClientes.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvResultadosClientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResultadosClientes.MultiSelect = false; // Permitir seleccionar solo una fila a la vez
        }

        private void EstiloDataGridViewHabitacionesDisponibles()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvHabitacionesDisponibles.BorderStyle = BorderStyle.None;
            dgvHabitacionesDisponibles.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 240, 240);
            dgvHabitacionesDisponibles.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Color de selección rojizo claro
            dgvHabitacionesDisponibles.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvHabitacionesDisponibles.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvHabitacionesDisponibles.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvHabitacionesDisponibles.EnableHeadersVisualStyles = false;
            dgvHabitacionesDisponibles.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvHabitacionesDisponibles.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvHabitacionesDisponibles.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHabitacionesDisponibles.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvHabitacionesDisponibles.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHabitacionesDisponibles.ColumnHeadersHeight = 30; // Reducido de 40 a 30
                                                                 // Estilo para las filas y celdas
            dgvHabitacionesDisponibles.RowTemplate.Height = 25; // Reducido de 35 a 25
            dgvHabitacionesDisponibles.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvHabitacionesDisponibles.DefaultCellStyle.Padding = new Padding(3); // Reducido de 5 a 3
            dgvHabitacionesDisponibles.RowHeadersVisible = false;
            // Hacer que el control se ajuste a su contenedor
            dgvHabitacionesDisponibles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // Añadir un borde fino alrededor de la tabla
            dgvHabitacionesDisponibles.BorderStyle = BorderStyle.FixedSingle;
            dgvHabitacionesDisponibles.GridColor = Color.FromArgb(220, 220, 220);
            // Configurar la selección de filas completas
            dgvHabitacionesDisponibles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHabitacionesDisponibles.MultiSelect = false;

            // Estilo específico para mostrar precio en formato de moneda
            // Aplicar si existe una columna de precio
            for (int i = 0; i < dgvHabitacionesDisponibles.Columns.Count; i++)
            {
                if (dgvHabitacionesDisponibles.Columns[i].Name.Contains("Precio") ||
                    dgvHabitacionesDisponibles.Columns[i].HeaderText.Contains("Precio"))
                {
                    dgvHabitacionesDisponibles.Columns[i].DefaultCellStyle.Format = "C2";
                    dgvHabitacionesDisponibles.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }

        private void EstiloDataGridViewHabitacionesReservadas()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvHabitacionesReservadas.BorderStyle = BorderStyle.None;
            dgvHabitacionesReservadas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvHabitacionesReservadas.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Color de selección gris claro
            dgvHabitacionesReservadas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvHabitacionesReservadas.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvHabitacionesReservadas.BackgroundColor = Color.White;

            // Estilo para el encabezado - TODO rojizo como solicitado
            dgvHabitacionesReservadas.EnableHeadersVisualStyles = false;
            dgvHabitacionesReservadas.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvHabitacionesReservadas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo uniforme
            dgvHabitacionesReservadas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHabitacionesReservadas.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 7, FontStyle.Bold); // Fuente aún más pequeña
            dgvHabitacionesReservadas.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHabitacionesReservadas.ColumnHeadersHeight = 20; // Altura más reducida

            // Estilo para las filas y celdas - extremadamente reducidas
            dgvHabitacionesReservadas.RowTemplate.Height = 18; // Altura mínima funcional
            dgvHabitacionesReservadas.DefaultCellStyle.Font = new Font("Yu Gothic", 7); // Fuente aún más pequeña
            dgvHabitacionesReservadas.DefaultCellStyle.Padding = new Padding(1); // Padding mínimo
            dgvHabitacionesReservadas.RowHeadersVisible = false;

            // Configuración para que se adapte al contenido
            dgvHabitacionesReservadas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Configuraciones adicionales para visualización ultra compacta
            dgvHabitacionesReservadas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHabitacionesReservadas.AllowUserToAddRows = false;
            dgvHabitacionesReservadas.AllowUserToDeleteRows = false;
            dgvHabitacionesReservadas.ReadOnly = true;
            dgvHabitacionesReservadas.MultiSelect = false;
            dgvHabitacionesReservadas.ScrollBars = ScrollBars.Vertical; // Permitir scrollbar vertical para ver más filas

            // Formatear la columna de precio como moneda
            for (int i = 0; i < dgvHabitacionesReservadas.Columns.Count; i++)
            {
                if (dgvHabitacionesReservadas.Columns[i].Name.Contains("Precio") ||
                    dgvHabitacionesReservadas.Columns[i].HeaderText.Contains("Precio"))
                {
                    dgvHabitacionesReservadas.Columns[i].DefaultCellStyle.Format = "C2";
                    dgvHabitacionesReservadas.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }

            // Eliminar cualquier margen adicional para mejor ajuste
            dgvHabitacionesReservadas.Margin = new Padding(0);

            // Añadir un borde fino alrededor de la tabla
            dgvHabitacionesReservadas.BorderStyle = BorderStyle.FixedSingle;
            dgvHabitacionesReservadas.GridColor = Color.FromArgb(220, 220, 220);

            // Calcular altura extremadamente reducida
            int maxVisibleRows = Math.Min(2, dgvHabitacionesReservadas.Rows.Count); // Máximo 2 filas visibles
            int calculatedHeight = dgvHabitacionesReservadas.ColumnHeadersHeight +
                                  (dgvHabitacionesReservadas.RowTemplate.Height * Math.Max(1, maxVisibleRows));

            // Establecer altura mínima y máxima muy reducidas
            int minHeight = dgvHabitacionesReservadas.ColumnHeadersHeight + dgvHabitacionesReservadas.RowTemplate.Height;
            int maxHeight = 60; // Altura máxima muy reducida (antes 80)

            dgvHabitacionesReservadas.Height = Math.Min(maxHeight, Math.Max(minHeight, calculatedHeight));

            // Forzar la actualización del diseño
            dgvHabitacionesReservadas.Refresh();
        }

        private void btnCerrarSesion_Click(object sender, EventArgs e)
        {
            LoginForm login = new LoginForm();
            login.Show();
            this.Close();
        }
    }
}