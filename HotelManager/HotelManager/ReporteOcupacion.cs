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
    public partial class ReporteOcupacion : Form
    {

        // Lista para almacenar los IDs de hoteles relacionados con los nombres
        private Dictionary<string, int> hotelIds = new Dictionary<string, int>();
        // Variable para controlar si estamos en la carga inicial
        private bool cargaInicial = true;

        public ReporteOcupacion()
        {
            InitializeComponent();

            ConfigurarControles();

            // Inicializamos los datos en el constructor
            CargarDatosIniciales();
        }

        private void ConfigurarControles()
        {
            // Configurar el DataGridView principal
            dgvReporteOcupacion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvReporteOcupacion.AllowUserToAddRows = false;
            dgvReporteOcupacion.AllowUserToDeleteRows = false;
            dgvReporteOcupacion.ReadOnly = true;
            dgvReporteOcupacion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReporteOcupacion.MultiSelect = false;

            EstiloDataGridViewReporteOcupacion();

            // Configurar el DataGridView de resumen
            dgvResumen.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvResumen.AllowUserToAddRows = false;
            dgvResumen.AllowUserToDeleteRows = false;
            dgvResumen.ReadOnly = true;
            dgvResumen.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResumen.MultiSelect = false;

            EstiloDataGridViewResumen();

            // Configurar los ComboBox
            cmbPais.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCiudad.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAnio.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbHotel.DropDownStyle = ComboBoxStyle.DropDownList;

            // No deshabilitamos los ComboBox inicialmente
            cmbCiudad.Enabled = true;
            cmbHotel.Enabled = true;

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
                {
                    cmbPais.SelectedIndex = 0;
                    string paisSeleccionado = cmbPais.SelectedItem.ToString();
                    CargarCiudades(paisSeleccionado);
                }
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
                {
                    cmbCiudad.SelectedIndex = 0;
                    string ciudadSeleccionada = cmbCiudad.SelectedItem.ToString();
                    CargarHoteles(ciudadSeleccionada, pais);
                }
                else
                {
                    cmbHotel.Items.Clear();
                    cmbHotel.Enabled = false;
                }
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
            cmbAnio.Items.Clear();
            for (int i = anioActual - 5; i <= anioActual; i++)
            {
                cmbAnio.Items.Add(i);
            }
            cmbAnio.SelectedItem = anioActual;
        }



        private void ReporteOcupacion_Load(object sender, EventArgs e)
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
            if (cmbCiudad.SelectedIndex >= 0 && cmbPais.SelectedIndex >= 0 && !cargaInicial)
            {
                CargarHoteles(cmbCiudad.SelectedItem.ToString(), cmbPais.SelectedItem.ToString());
            }
        }

        private void btnGenerar_Click(object sender, EventArgs e)
        {
            GenerarReporte();
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            LimpiarReporte();
        }

        private void LimpiarReporte()
        {
            // Limpiar el DataGridView principal y el resumen
            dgvReporteOcupacion.Rows.Clear();
            dgvResumen.Rows.Clear();

            // Reiniciamos los datos usando nuestro método centralizado
            CargarDatosIniciales();
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

                // Configuramos primero las columnas de los DataGridView si no existen
                ConfigurarColumnasDataGridViews();

                // Ejecutamos la consulta para obtener los datos detallados
                GenerarReporteDetallado(pais, anio, ciudad, hotelSeleccionado);

                // Ejecutamos la consulta para obtener el resumen por mes
                GenerarResumenPorMes(pais, anio, ciudad, hotelSeleccionado);

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

        private void ConfigurarColumnasDataGridViews()
        {
            // Configuración de columnas para el DataGridView principal
            if (dgvReporteOcupacion.Columns.Count == 0)
            {
                dgvReporteOcupacion.Columns.Add("Ciudad", "Ciudad");
                dgvReporteOcupacion.Columns.Add("NombreHotel", "Hotel");
                dgvReporteOcupacion.Columns.Add("Anio", "Año");
                dgvReporteOcupacion.Columns.Add("NombreMes", "Mes");
                dgvReporteOcupacion.Columns.Add("TipoHabitacion", "Tipo de Habitación");
                dgvReporteOcupacion.Columns.Add("CantidadHabitaciones", "Cantidad de Habitaciones");
                dgvReporteOcupacion.Columns.Add("PorcentajeOcupacion", "% Ocupación");
                dgvReporteOcupacion.Columns.Add("CantidadPersonas", "Personas Hospedadas");

                // Formato de porcentaje para la columna de ocupación
                dgvReporteOcupacion.Columns["PorcentajeOcupacion"].DefaultCellStyle.Format = "0.00 %";
            }

            // Configuración de columnas para el DataGridView de resumen
            if (dgvResumen.Columns.Count == 0)
            {
                dgvResumen.Columns.Add("Ciudad", "Ciudad");
                dgvResumen.Columns.Add("NombreHotel", "Hotel");
                dgvResumen.Columns.Add("Anio", "Año");
                dgvResumen.Columns.Add("NombreMes", "Mes");
                dgvResumen.Columns.Add("PorcentajeOcupacion", "% Ocupación");

                // Formato de porcentaje para la columna de ocupación
                dgvResumen.Columns["PorcentajeOcupacion"].DefaultCellStyle.Format = "0.00 %";
            }
        }

        private void GenerarReporteDetallado(string pais, int anio, string ciudad, string hotelSeleccionado)
        {
            try
            {
                // Limpiamos las filas existentes
                dgvReporteOcupacion.Rows.Clear();

                // Preparamos la consulta SQL para el reporte detallado
                StringBuilder query = new StringBuilder();

                // Primero creamos las subconsultas
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
),
TiposHabitacionDisponibles AS (
    SELECT th.IdTipoHabitacion, th.Nombre AS TipoHabitacion, th.CantidadHabitaciones,
           hd.IdHotel, hd.Nombre AS NombreHotel, hd.Ciudad
    FROM TiposHabitacion th
    INNER JOIN HotelesDisponibles hd ON th.IdHotel = hd.IdHotel
    WHERE th.EstadoActivo = 1
),
DiasDelMes AS (
    SELECT Mes, 
           DAY(EOMONTH(DATEFROMPARTS(@Anio, Mes, 1))) AS DiasTotales
    FROM Meses
),
-- Generar todas las fechas para cada reservación
FechasReservadas AS (
    SELECT 
        r.IdReservacion,
        dr.IdHabitacion,
        h.IdTipoHabitacion,
        r.IdHotel,
        DATEADD(DAY, number, r.FechaCheckIn) AS Fecha,
        dr.CantidadPersonas
    FROM 
        Reservaciones r
    INNER JOIN 
        DetalleReservaciones dr ON r.IdReservacion = dr.IdReservacion
    INNER JOIN
        Habitaciones h ON dr.IdHabitacion = h.IdHabitacion
    CROSS APPLY 
        master.dbo.spt_values 
    WHERE 
        type = 'P' 
        AND number >= 0 
        AND number < DATEDIFF(DAY, r.FechaCheckIn, r.FechaCheckOut)
        AND r.EstadoReservacion IN ('Confirmada', 'CheckIn', 'CheckOut') 
        AND YEAR(r.FechaCheckIn) <= @Anio 
        AND YEAR(r.FechaCheckOut) >= @Anio
),
-- Agrupar por tipo de habitación, mes y contar días ocupados
OcupacionPorMes AS (
    SELECT 
        f.IdHotel,
        f.IdTipoHabitacion,
        YEAR(f.Fecha) AS Anio,
        MONTH(f.Fecha) AS Mes,
        COUNT(DISTINCT CONCAT(f.IdHabitacion, CONVERT(VARCHAR, f.Fecha, 112))) AS DiasOcupados,
        SUM(f.CantidadPersonas) AS PersonasHospedadas
    FROM 
        FechasReservadas f
    WHERE 
        YEAR(f.Fecha) = @Anio
    GROUP BY 
        f.IdHotel, f.IdTipoHabitacion, YEAR(f.Fecha), MONTH(f.Fecha)
),
-- Generar el reporte por tipo de habitación
ReporteFinal AS (
    SELECT 
        thd.Ciudad,
        thd.NombreHotel,
        @Anio AS Anio,
        m.Mes,
        DATENAME(MONTH, DATEFROMPARTS(@Anio, m.Mes, 1)) AS NombreMes,
        thd.TipoHabitacion,
        thd.CantidadHabitaciones,
        dm.DiasTotales,
        ISNULL(o.DiasOcupados, 0) AS DiasOcupados,
        ISNULL(o.PersonasHospedadas, 0) AS PersonasHospedadas,
        -- Cálculo del porcentaje de ocupación:
        -- (Días ocupados / (Cantidad de habitaciones * Días del mes)) * 100
        CASE 
            WHEN thd.CantidadHabitaciones > 0 AND dm.DiasTotales > 0
            THEN CAST(ISNULL(o.DiasOcupados, 0) AS FLOAT) / 
                 (thd.CantidadHabitaciones * dm.DiasTotales)
            ELSE 0 
        END AS PorcentajeOcupacion
    FROM 
        TiposHabitacionDisponibles thd
    CROSS JOIN 
        Meses m
    LEFT JOIN 
        DiasDelMes dm ON m.Mes = dm.Mes
    LEFT JOIN 
        OcupacionPorMes o ON thd.IdHotel = o.IdHotel 
                         AND thd.IdTipoHabitacion = o.IdTipoHabitacion 
                         AND m.Mes = o.Mes
)
SELECT 
    Ciudad,
    NombreHotel,
    Anio,
    Mes,
    NombreMes,
    TipoHabitacion,
    CantidadHabitaciones,
    PorcentajeOcupacion,
    PersonasHospedadas
FROM 
    ReporteFinal
ORDER BY 
    Ciudad, NombreHotel, Mes, TipoHabitacion");

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
                    dgvReporteOcupacion.Rows.Add(
                        row["Ciudad"],
                        row["NombreHotel"],
                        row["Anio"],
                        row["NombreMes"],
                        row["TipoHabitacion"],
                        row["CantidadHabitaciones"],
                        Convert.ToDouble(row["PorcentajeOcupacion"]),
                        row["PersonasHospedadas"]
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el reporte detallado: " + ex.Message);
            }
        }

        private void GenerarResumenPorMes(string pais, int anio, string ciudad, string hotelSeleccionado)
        {
            try
            {
                // Limpiamos las filas existentes
                dgvResumen.Rows.Clear();

                // Preparamos la consulta SQL para el resumen por mes
                StringBuilder query = new StringBuilder();

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
),
TotalHabitacionesPorHotel AS (
    SELECT 
        h.IdHotel, 
        SUM(th.CantidadHabitaciones) AS TotalHabitaciones
    FROM 
        HotelesDisponibles h
    INNER JOIN 
        TiposHabitacion th ON h.IdHotel = th.IdHotel
    WHERE
        th.EstadoActivo = 1
    GROUP BY 
        h.IdHotel
),
DiasDelMes AS (
    SELECT Mes, 
           DAY(EOMONTH(DATEFROMPARTS(@Anio, Mes, 1))) AS DiasTotales
    FROM Meses
),
-- Generar todas las fechas para cada reservación
FechasReservadas AS (
    SELECT 
        r.IdReservacion,
        r.IdHotel,
        DATEADD(DAY, number, r.FechaCheckIn) AS Fecha,
        dr.IdHabitacion
    FROM 
        Reservaciones r
    INNER JOIN 
        DetalleReservaciones dr ON r.IdReservacion = dr.IdReservacion
    CROSS APPLY 
        master.dbo.spt_values 
    WHERE 
        type = 'P' 
        AND number >= 0 
        AND number < DATEDIFF(DAY, r.FechaCheckIn, r.FechaCheckOut)
        AND r.EstadoReservacion IN ('Confirmada', 'CheckIn', 'CheckOut') 
        AND YEAR(r.FechaCheckIn) <= @Anio 
        AND YEAR(r.FechaCheckOut) >= @Anio
),
-- Agrupar por hotel, mes y contar días ocupados
OcupacionPorHotelYMes AS (
    SELECT 
        f.IdHotel,
        YEAR(f.Fecha) AS Anio,
        MONTH(f.Fecha) AS Mes,
        COUNT(DISTINCT CONCAT(f.IdHabitacion, CONVERT(VARCHAR, f.Fecha, 112))) AS DiasOcupados
    FROM 
        FechasReservadas f
    WHERE 
        YEAR(f.Fecha) = @Anio
    GROUP BY 
        f.IdHotel, YEAR(f.Fecha), MONTH(f.Fecha)
),
-- Generar el resumen por hotel y mes
ResumenFinal AS (
    SELECT 
        hd.Ciudad,
        hd.Nombre AS NombreHotel,
        @Anio AS Anio,
        m.Mes,
        DATENAME(MONTH, DATEFROMPARTS(@Anio, m.Mes, 1)) AS NombreMes,
        dm.DiasTotales,
        th.TotalHabitaciones,
        ISNULL(o.DiasOcupados, 0) AS DiasOcupados,
        -- Cálculo del porcentaje de ocupación:
        -- (Días ocupados / (Total de habitaciones * Días del mes)) * 100
        CASE 
            WHEN th.TotalHabitaciones > 0 AND dm.DiasTotales > 0
            THEN CAST(ISNULL(o.DiasOcupados, 0) AS FLOAT) / 
                 (th.TotalHabitaciones * dm.DiasTotales)
            ELSE 0 
        END AS PorcentajeOcupacion
    FROM 
        HotelesDisponibles hd
    CROSS JOIN 
        Meses m
    LEFT JOIN 
        DiasDelMes dm ON m.Mes = dm.Mes
    LEFT JOIN 
        TotalHabitacionesPorHotel th ON hd.IdHotel = th.IdHotel
    LEFT JOIN 
        OcupacionPorHotelYMes o ON hd.IdHotel = o.IdHotel AND m.Mes = o.Mes
    WHERE
        th.TotalHabitaciones > 0 -- Solo hoteles que tengan habitaciones
)
SELECT
    Ciudad,
    NombreHotel,
    Anio,
    Mes,
    NombreMes,
    PorcentajeOcupacion
FROM 
    ResumenFinal
ORDER BY 
    Ciudad, NombreHotel, Mes");

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

                // Agregamos las filas al DataGridView de resumen
                foreach (DataRow row in dtResultados.Rows)
                {
                    dgvResumen.Rows.Add(
                        row["Ciudad"],
                        row["NombreHotel"],
                        row["Anio"],
                        row["NombreMes"],
                        Convert.ToDouble(row["PorcentajeOcupacion"])
                    );
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error al generar el resumen por mes: " + ex.Message);
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

        private void EstiloDataGridViewReporteOcupacion()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvReporteOcupacion.BorderStyle = BorderStyle.None;
            dgvReporteOcupacion.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvReporteOcupacion.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvReporteOcupacion.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvReporteOcupacion.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvReporteOcupacion.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvReporteOcupacion.EnableHeadersVisualStyles = false;
            dgvReporteOcupacion.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvReporteOcupacion.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvReporteOcupacion.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvReporteOcupacion.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvReporteOcupacion.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvReporteOcupacion.ColumnHeadersHeight = 30;

            // Estilo para las filas y celdas
            dgvReporteOcupacion.RowTemplate.Height = 25;
            dgvReporteOcupacion.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvReporteOcupacion.DefaultCellStyle.Padding = new Padding(3);
            dgvReporteOcupacion.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvReporteOcupacion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvReporteOcupacion.BorderStyle = BorderStyle.FixedSingle;
            dgvReporteOcupacion.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvReporteOcupacion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvReporteOcupacion.MultiSelect = false;

            // Estilo específico para columnas numéricas o de porcentajes
            foreach (DataGridViewColumn column in dgvReporteOcupacion.Columns)
            {
                if (column.Name == "PorcentajeOcupacion" || column.HeaderText == "% Ocupación")
                {
                    column.DefaultCellStyle.Format = "0.00 %";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                else if (column.Name == "CantidadHabitaciones" || column.Name == "CantidadPersonas" ||
                        column.HeaderText.Contains("Cantidad"))
                {
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
        }

        private void EstiloDataGridViewResumen()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvResumen.BorderStyle = BorderStyle.None;
            dgvResumen.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvResumen.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvResumen.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvResumen.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvResumen.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvResumen.EnableHeadersVisualStyles = false;
            dgvResumen.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvResumen.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100); // Rojizo
            dgvResumen.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvResumen.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 9, FontStyle.Bold);
            dgvResumen.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvResumen.ColumnHeadersHeight = 30;

            // Estilo para las filas y celdas
            dgvResumen.RowTemplate.Height = 25;
            dgvResumen.DefaultCellStyle.Font = new Font("Yu Gothic", 8);
            dgvResumen.DefaultCellStyle.Padding = new Padding(3);
            dgvResumen.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvResumen.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvResumen.BorderStyle = BorderStyle.FixedSingle;
            dgvResumen.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvResumen.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvResumen.MultiSelect = false;

            // Estilo específico para la columna de porcentaje
            foreach (DataGridViewColumn column in dgvResumen.Columns)
            {
                if (column.Name == "PorcentajeOcupacion" || column.HeaderText == "% Ocupación")
                {
                    column.DefaultCellStyle.Format = "0.00 %";
                    column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

                    // Destacar esta columna con un color de fondo ligeramente diferente
                    column.DefaultCellStyle.BackColor = Color.FromArgb(245, 235, 235);
                }
            }
        }
    }
}

 
