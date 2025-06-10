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
    public partial class ReporteVentas : Form
    {
        // Lista para almacenar los IDs de hoteles relacionados con los nombres
        private Dictionary<string, int> hotelIds = new Dictionary<string, int>();
        private bool cargaInicial = true;

        public ReporteVentas()
        {
            InitializeComponent();
            ConfigurarControles();

            // Inicializamos los datos en el constructor
            CargarDatosIniciales();
        }

        private void ReporteVentas_Load(object sender, EventArgs e)
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
        private void ConfigurarControles()
        {
            // Configurar el DataGridView
            dgvReporteVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReporteVentas.AllowUserToAddRows = false;
            dgvReporteVentas.AllowUserToDeleteRows = false;
            dgvReporteVentas.ReadOnly = true;
            dgvReporteVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReporteVentas.MultiSelect = false;
            EstiloDataGridViewReporteVentas();

            // Configurar los ComboBox
            cmbPais.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCiudad.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAnio.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHotel.DropDownStyle = ComboBoxStyle.DropDownList;

            // Deshabilitar los ComboBox que dependen de una selección previa
            cmbCiudad.Enabled = false;
            cmbHotel.Enabled = false;

            // Configurar botones
            btnGenerar.Enabled = true;
            btnLimpiar.Enabled = true;
        }

        // Método centralizado para cargar los datos iniciales
        private void CargarDatosIniciales()
        {
            cargaInicial = true;
            CargarAnios();
            CargarPaises();
            cargaInicial = false;
        }

        // Método para cargar países desde la base de datos
        private void CargarPaises()
        {
            try
            {
                string query = "SELECT DISTINCT Pais FROM Hoteles ORDER BY Pais";
                DataTable dtPaises = Database.ExecuteQuery(query);

                cmbPais.Items.Clear();
                foreach (DataRow row in dtPaises.Rows)
                {
                    cmbPais.Items.Add(row["Pais"].ToString());
                }

                if (cmbPais.Items.Count > 0)
                    cmbPais.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar países: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para cargar ciudades según el país seleccionado
        private void CargarCiudades(string pais)
        {
            try
            {
                string query = "SELECT DISTINCT Ciudad FROM Hoteles WHERE Pais = @Pais ORDER BY Ciudad";
                SqlParameter[] parameters = new SqlParameter[] {
                    new SqlParameter("@Pais", pais)
                };

                DataTable dtCiudades = Database.ExecuteQuery(query, parameters);

                cmbCiudad.Items.Clear();
                foreach (DataRow row in dtCiudades.Rows)
                {
                    cmbCiudad.Items.Add(row["Ciudad"].ToString());
                }

                cmbCiudad.Enabled = true;
                if (cmbCiudad.Items.Count > 0)
                    cmbCiudad.SelectedIndex = 0;
                else
                    cmbHotel.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar ciudades: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para cargar hoteles según la ciudad seleccionada
        private void CargarHoteles(string ciudad, string pais)
        {
            try
            {
                string query = "SELECT IdHotel, Nombre FROM Hoteles WHERE Ciudad = @Ciudad AND Pais = @Pais ORDER BY Nombre";
                SqlParameter[] parameters = new SqlParameter[] {
                    new SqlParameter("@Ciudad", ciudad),
                    new SqlParameter("@Pais", pais)
                };

                DataTable dtHoteles = Database.ExecuteQuery(query, parameters);

                cmbHotel.Items.Clear();
                hotelIds.Clear();
                cmbHotel.Items.Add("Todos");

                foreach (DataRow row in dtHoteles.Rows)
                {
                    string nombreHotel = row["Nombre"].ToString();
                    cmbHotel.Items.Add(nombreHotel);
                    hotelIds[nombreHotel] = Convert.ToInt32(row["IdHotel"]);
                }

                cmbHotel.Enabled = true;
                cmbHotel.SelectedIndex = 0; // Seleccionar "Todos" por defecto
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar hoteles: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cargar años (últimos 5 años más el actual)
        private void CargarAnios()
        {
            int anioActual = DateTime.Now.Year;
            for (int i = anioActual - 5; i <= anioActual; i++)
            {
                cmbAnio.Items.Add(i);
            }
            cmbAnio.SelectedItem = anioActual;
        }

        private void cmbPais_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbPais.SelectedIndex >= 0)
            {
                CargarCiudades(cmbPais.SelectedItem.ToString());
                // La carga de hoteles ahora se realiza dentro de CargarCiudades
            }
        }

        private void cmbCiudad_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCiudad.SelectedIndex >= 0 && cmbPais.SelectedIndex >= 0)
            {
                CargarHoteles(cmbCiudad.SelectedItem.ToString(), cmbPais.SelectedItem.ToString());
            }
        }

        private void btnGenerar_Click(object sender, EventArgs e)
        {
            GenerarReporte();
        }

        private void GenerarReporte()
        {
            try
            {
                // Validar que se hayan seleccionado todos los filtros
                if (cmbPais.SelectedIndex < 0 || cmbCiudad.SelectedIndex < 0 ||
                    cmbAnio.SelectedIndex < 0 || cmbHotel.SelectedIndex < 0)
                {
                    MessageBox.Show("Por favor, seleccione todos los filtros antes de generar el reporte.",
                        "Filtros incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string pais = cmbPais.SelectedItem.ToString();
                int anio = Convert.ToInt32(cmbAnio.SelectedItem);
                string ciudad = cmbCiudad.SelectedItem.ToString();
                string hotelSeleccionado = cmbHotel.SelectedItem.ToString();

                // Preparamos la consulta SQL
                StringBuilder query = new StringBuilder();

                // Primero creamos una subconsulta que genera los 12 meses
                query.Append(@"
                WITH Meses AS (
                    SELECT 1 AS Mes UNION SELECT 2 UNION SELECT 3 UNION SELECT 4 UNION SELECT 5 UNION SELECT 6 
                    UNION SELECT 7 UNION SELECT 8 UNION SELECT 9 UNION SELECT 10 UNION SELECT 11 UNION SELECT 12
                ),
                HotelesDisponibles AS (
                    SELECT h.IdHotel, h.Nombre, h.Ciudad
                    FROM Hoteles h
                    WHERE h.Pais = @Pais AND h.Ciudad = @Ciudad
                    ");

                // Si se seleccionó un hotel específico, agregamos el filtro
                if (hotelSeleccionado != "Todos")
                {
                    query.Append("AND h.IdHotel = @IdHotel ");
                }

                query.Append(@"
                )
                SELECT 
                    hd.Ciudad,
                    hd.Nombre AS NombreHotel,
                    @Anio AS Anio,
                    m.Mes,
                    DATENAME(MONTH, DATEFROMPARTS(@Anio, m.Mes, 1)) AS NombreMes,
                    ISNULL(SUM(f.SubtotalHospedaje), 0) AS IngresosHospedaje,
                    ISNULL(SUM(f.SubtotalServicios), 0) AS IngresosServicios,
                    ISNULL(SUM(f.Total), 0) AS IngresosTotales
                FROM 
                    HotelesDisponibles hd
                CROSS JOIN 
                    Meses m
                LEFT JOIN 
                    Reservaciones r ON hd.IdHotel = r.IdHotel 
                        AND YEAR(r.FechaHoraCheckOut) = @Anio 
                        AND MONTH(r.FechaHoraCheckOut) = m.Mes
                        AND r.EstadoReservacion = 'CheckOut'
                LEFT JOIN 
                    Facturas f ON r.IdReservacion = f.IdReservacion
                GROUP BY 
                    hd.Ciudad, hd.Nombre, m.Mes
                ORDER BY 
                    hd.Ciudad, hd.Nombre, m.Mes");

                // Preparamos los parámetros
                List<SqlParameter> parameters = new List<SqlParameter>
                {
                    new SqlParameter("@Pais", pais),
                    new SqlParameter("@Ciudad", ciudad),
                    new SqlParameter("@Anio", anio)
                };

                // Si se seleccionó un hotel específico, agregamos el parámetro
                if (hotelSeleccionado != "Todos")
                {
                    parameters.Add(new SqlParameter("@IdHotel", hotelIds[hotelSeleccionado]));
                }

                // Ejecutamos la consulta
                DataTable dtResultados = Database.ExecuteQuery(query.ToString(), parameters.ToArray());

                // Configuramos las columnas del DataGridView si no existen
                if (dgvReporteVentas.Columns.Count == 0)
                {
                    dgvReporteVentas.Columns.Add("Ciudad", "Ciudad");
                    dgvReporteVentas.Columns.Add("NombreHotel", "Hotel");
                    dgvReporteVentas.Columns.Add("Anio", "Año");
                    dgvReporteVentas.Columns.Add("NombreMes", "Mes");
                    dgvReporteVentas.Columns.Add("IngresosHospedaje", "Ingresos por Hospedaje");
                    dgvReporteVentas.Columns.Add("IngresosServicios", "Ingresos por Servicios");
                    dgvReporteVentas.Columns.Add("IngresosTotales", "Ingresos Totales");

                    // Configurar formato de moneda para las columnas numéricas
                    dgvReporteVentas.Columns["IngresosHospedaje"].DefaultCellStyle.Format = "C2";
                    dgvReporteVentas.Columns["IngresosServicios"].DefaultCellStyle.Format = "C2";
                    dgvReporteVentas.Columns["IngresosTotales"].DefaultCellStyle.Format = "C2";
                }

                // Limpiamos las filas existentes
                dgvReporteVentas.Rows.Clear();

                // Si no hay resultados
                if (dtResultados.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron datos para los filtros seleccionados.",
                        "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Agregamos las filas al DataGridView
                foreach (DataRow row in dtResultados.Rows)
                {
                    dgvReporteVentas.Rows.Add(
                        row["Ciudad"],
                        row["NombreHotel"],
                        row["Anio"],
                        row["NombreMes"],
                        row["IngresosHospedaje"],
                        row["IngresosServicios"],
                        row["IngresosTotales"]
                    );
                }

                // Mostrar mensaje de éxito
                MessageBox.Show("Reporte generado exitosamente.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el reporte: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LimpiarReporte()
        {
            // Limpiar el DataGridView
            dgvReporteVentas.Rows.Clear();

            // Reiniciamos los datos usando nuestro método centralizado
            CargarDatosIniciales();

        }

        

        private void btnLimpiar_Click_1(object sender, EventArgs e)
        {
            LimpiarReporte();
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

        private void EstiloDataGridViewReporteVentas()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvReporteVentas.BorderStyle = BorderStyle.None;
            dgvReporteVentas.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvReporteVentas.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvReporteVentas.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvReporteVentas.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvReporteVentas.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvReporteVentas.EnableHeadersVisualStyles = false;
            dgvReporteVentas.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvReporteVentas.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvReporteVentas.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReporteVentas.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvReporteVentas.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReporteVentas.ColumnHeadersHeight = 30;

            // Estilo para las filas y celdas
            dgvReporteVentas.RowTemplate.Height = 25;
            dgvReporteVentas.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvReporteVentas.DefaultCellStyle.Padding = new Padding(3);
            dgvReporteVentas.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvReporteVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvReporteVentas.BorderStyle = BorderStyle.FixedSingle;
            dgvReporteVentas.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvReporteVentas.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReporteVentas.MultiSelect = false;

            // Estilo específico para columnas de moneda
            if (dgvReporteVentas.Columns.Count > 0)
            {
                // Verificar si las columnas ya están creadas
                if (dgvReporteVentas.Columns.Contains("IngresosHospedaje"))
                {
                    dgvReporteVentas.Columns["IngresosHospedaje"].DefaultCellStyle.Format = "C2";
                    dgvReporteVentas.Columns["IngresosHospedaje"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                if (dgvReporteVentas.Columns.Contains("IngresosServicios"))
                {
                    dgvReporteVentas.Columns["IngresosServicios"].DefaultCellStyle.Format = "C2";
                    dgvReporteVentas.Columns["IngresosServicios"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }

                if (dgvReporteVentas.Columns.Contains("IngresosTotales"))
                {
                    dgvReporteVentas.Columns["IngresosTotales"].DefaultCellStyle.Format = "C2";
                    dgvReporteVentas.Columns["IngresosTotales"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                    // Destacar la columna de totales
                    dgvReporteVentas.Columns["IngresosTotales"].DefaultCellStyle.Font = new Font("Yu Gothic", 8, FontStyle.Bold);
                    dgvReporteVentas.Columns["IngresosTotales"].DefaultCellStyle.BackColor = Color.FromArgb(245, 235, 235);
                }
            }

            // Forzar la actualización del diseño
            dgvReporteVentas.Refresh();
        }
    }
}
