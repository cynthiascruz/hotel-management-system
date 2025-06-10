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

namespace HotelManager
{
    public partial class ConfigHabitaciones : Form
    {
        public ConfigHabitaciones()
        {
            InitializeComponent();
        }

        private void ConfigHabitaciones_Load(object sender, EventArgs e)
        {
            // Verificar acceso de administrador
            if (Session.TipoUsuario != "Administrador")
            {
                MessageBox.Show("Solo los administradores pueden acceder a la configuración de habitaciones.",
                               "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Close();
                return;
            }

            cargarCatalogosTiposCama();
            cargarCatalogosNiveles();
            cargarCatalogoAmenidades();
            cargarUbicaciones();
            cargarHoteles();
        }

        private void cargarCatalogosTiposCama()
        {
            try
            {
                string query = "SELECT IdTipoCama, Nombre FROM CatalogoTiposCama ORDER BY Nombre";
                DataTable dtTiposCama = Data.Database.ExecuteQuery(query);

                cboTipoCama.DataSource = dtTiposCama;
                cboTipoCama.DisplayMember = "Nombre";
                cboTipoCama.ValueMember = "IdTipoCama";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo de tipos de cama: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarCatalogosNiveles()
        {
            try
            {
                string query = "SELECT IdNivel, Nombre FROM CatalogoNivelesHabitacion ORDER BY Nombre";
                DataTable dtNiveles = Data.Database.ExecuteQuery(query);

                cboNivel.DataSource = dtNiveles;
                cboNivel.DisplayMember = "Nombre";
                cboNivel.ValueMember = "IdNivel";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo de niveles: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarCatalogoAmenidades()
        {
            try
            {
                string query = "SELECT IdAmenidad, Nombre, Descripcion FROM CatalogoAmenidades ORDER BY Nombre";
                DataTable dtAmenidades = Data.Database.ExecuteQuery(query);

                // Configuración de los checkboxes de amenidades según la base de datos
                foreach (DataRow row in dtAmenidades.Rows)
                {
                    int idAmenidad = Convert.ToInt32(row["IdAmenidad"]);
                    string nombre = row["Nombre"].ToString();

                    // Asignar IDs a los tags de los checkboxes
                    switch (nombre)
                    {
                        case "Caja fuerte":
                            chkCajaFuerte.Tag = idAmenidad;
                            break;
                        case "Minibar":
                            chkMiniBar.Tag = idAmenidad;
                            break;
                        case "Cafetera":
                            chkCafetera.Tag = idAmenidad;
                            break;
                        case "TV":
                            chkTV.Tag = idAmenidad;
                            break;
                        case "Jacuzzi":
                            chkJacuzzi.Tag = idAmenidad;
                            break;
                        case "Balcón privado":
                            chkBalcon.Tag = idAmenidad;
                            break;
                        case "Servicio a cuarto":
                            chkServicioCuarto.Tag = idAmenidad;
                            break;
                        case "Escritorio":
                            chkEscritorio.Tag = idAmenidad;
                            break;
                        case "Vista a jardín":
                            chkVistaJardin.Tag = idAmenidad;
                            break;
                        case "Vista a playa":
                            chkVistaPlaya.Tag = idAmenidad;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo de amenidades: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarUbicaciones()
        {
            try
            {
                // Limpiar combobox primero
                cboUbicacion.Items.Clear();

                // Añadir ubicaciones según las que se mencionan en el comentario de la definición de la tabla
                cboUbicacion.Items.Add("Interior");
                cboUbicacion.Items.Add("Frente a piscina");
                cboUbicacion.Items.Add("Frente a jardín");
                cboUbicacion.Items.Add("Frente a playa");
                cboUbicacion.Items.Add("Vista al mar");

                // Seleccionar la primera por defecto
                if (cboUbicacion.Items.Count > 0)
                    cboUbicacion.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ubicaciones: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarHoteles()
        {
            try
            {
                // Solo cargamos hoteles activos
                string query = "SELECT IdHotel, Nombre FROM Hoteles WHERE EstadoActivo = 1 ORDER BY Nombre";
                DataTable dtHoteles = Data.Database.ExecuteQuery(query);

                cboHotel.DataSource = dtHoteles;
                cboHotel.DisplayMember = "Nombre";
                cboHotel.ValueMember = "IdHotel";

                if (dtHoteles.Rows.Count == 0)
                {
                    MessageBox.Show("No hay hoteles activos en el sistema. Por favor, primero registre un hotel.",
                                    "Sin hoteles", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar hoteles: " + ex.Message,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarTiposHabitacion()
        {
            try
            {
                // Obtener el ID del hotel seleccionado
                if (cboHotel.SelectedValue == null) return;
                int idHotel = Convert.ToInt32(cboHotel.SelectedValue);

                // Modificar la consulta para no traer el campo Estado
                string query = @"
                SELECT t.IdTipoHabitacion, t.Nombre, t.NumeroCamas, 
                       tc.Nombre AS TipoCama, t.PrecioPorNoche, 
                       t.CapacidadPersonas, n.Nombre AS Nivel,
                       t.Ubicacion, t.CantidadHabitaciones
                FROM TiposHabitacion t
                INNER JOIN CatalogoTiposCama tc ON t.IdTipoCama = tc.IdTipoCama
                INNER JOIN CatalogoNivelesHabitacion n ON t.IdNivel = n.IdNivel
                WHERE t.IdHotel = @IdHotel
                ORDER BY t.Nombre";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@IdHotel", idHotel)
                };

                DataTable dtTipos = Data.Database.ExecuteQuery(query, parameters);

                // Asignar al DataGridView
                dgvTiposHabitacion.DataSource = dtTipos;

                // Ocultar columnas que no quieras mostrar
                if (dgvTiposHabitacion.Columns.Contains("IdTipoHabitacion"))
                    dgvTiposHabitacion.Columns["IdTipoHabitacion"].Visible = false;

                // Dar formato a columnas numéricas
                if (dgvTiposHabitacion.Columns.Contains("PrecioPorNoche"))
                    dgvTiposHabitacion.Columns["PrecioPorNoche"].DefaultCellStyle.Format = "C2";

                // Renombrar encabezados de columnas para hacerlos más cortos
                if (dgvTiposHabitacion.Columns.Contains("Nombre"))
                    dgvTiposHabitacion.Columns["Nombre"].HeaderText = "Nombre";

                if (dgvTiposHabitacion.Columns.Contains("NumeroCamas"))
                    dgvTiposHabitacion.Columns["NumeroCamas"].HeaderText = "Camas";

                if (dgvTiposHabitacion.Columns.Contains("TipoCama"))
                    dgvTiposHabitacion.Columns["TipoCama"].HeaderText = "Tipo Cama";

                if (dgvTiposHabitacion.Columns.Contains("PrecioPorNoche"))
                    dgvTiposHabitacion.Columns["PrecioPorNoche"].HeaderText = "Precio";

                if (dgvTiposHabitacion.Columns.Contains("CapacidadPersonas"))
                    dgvTiposHabitacion.Columns["CapacidadPersonas"].HeaderText = "Capacidad";

                if (dgvTiposHabitacion.Columns.Contains("CantidadHabitaciones"))
                    dgvTiposHabitacion.Columns["CantidadHabitaciones"].HeaderText = "Habitaciones";

                // Aplicar estilos al grid
                estiloDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar tipos de habitación: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cboHotel_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void estiloDataGridView()
        {
            // Establecer propiedades visuales del DataGridView
            dgvTiposHabitacion.BorderStyle = BorderStyle.None;
            dgvTiposHabitacion.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvTiposHabitacion.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvTiposHabitacion.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvTiposHabitacion.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvTiposHabitacion.BackgroundColor = Color.White;

            // Configurar encabezados
            dgvTiposHabitacion.EnableHeadersVisualStyles = false;
            dgvTiposHabitacion.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvTiposHabitacion.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100);
            dgvTiposHabitacion.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTiposHabitacion.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 10, FontStyle.Bold);
            dgvTiposHabitacion.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvTiposHabitacion.ColumnHeadersHeight = 40;

            // Estilo para las filas
            dgvTiposHabitacion.RowTemplate.Height = 35;
            dgvTiposHabitacion.DefaultCellStyle.Font = new Font("Yu Gothic", 9);
            dgvTiposHabitacion.DefaultCellStyle.Padding = new Padding(5);
            dgvTiposHabitacion.RowHeadersVisible = false;

            // Configuración adicional
            dgvTiposHabitacion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTiposHabitacion.BorderStyle = BorderStyle.FixedSingle;
            dgvTiposHabitacion.GridColor = Color.FromArgb(220, 220, 220);
            dgvTiposHabitacion.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTiposHabitacion.MultiSelect = false;

            // Asegurar que el DataGridView tenga scroll
            dgvTiposHabitacion.ScrollBars = ScrollBars.Both;
            
            // Hacer que los encabezados de columna se ajusten correctamente
            dgvTiposHabitacion.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTiposHabitacion.ColumnHeadersHeight = 45;
            dgvTiposHabitacion.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        private int idTipoHabitacionSeleccionado = 0;

        private void dgvTiposHabitacion_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dgvTiposHabitacion.CurrentRow != null)
                {
                    idTipoHabitacionSeleccionado = Convert.ToInt32(dgvTiposHabitacion.CurrentRow.Cells["IdTipoHabitacion"].Value);
                    // MessageBox.Show("ID seleccionado: " + idTipoHabitacionSeleccionado);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en selección: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validar campos
            if (string.IsNullOrEmpty(txtNombre.Text) ||
                string.IsNullOrEmpty(txtPrecio.Text) ||
                string.IsNullOrEmpty(txtNumeroCamas.Text) ||
                string.IsNullOrEmpty(txtCapacidad.Text) ||
                string.IsNullOrEmpty(txtCantidadHabitaciones.Text) ||
                cboTipoCama.SelectedIndex < 0 ||
                cboNivel.SelectedIndex < 0 ||
                cboUbicacion.SelectedIndex < 0)
            {
                MessageBox.Show("Por favor, complete todos los campos obligatorios", "Campos vacíos",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar valores numéricos
            if (!decimal.TryParse(txtPrecio.Text, out decimal precio) || precio <= 0 ||
                !int.TryParse(txtNumeroCamas.Text, out int numeroCamas) || numeroCamas <= 0 ||
                !int.TryParse(txtCapacidad.Text, out int capacidad) || capacidad <= 0 ||
                !int.TryParse(txtCantidadHabitaciones.Text, out int cantidadHabitaciones) || cantidadHabitaciones <= 0)
            {
                MessageBox.Show("Por favor, ingrese valores numéricos válidos", "Valores inválidos",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // Usar transacción para asegurar integridad de datos
                using (SqlConnection conn = new SqlConnection(Data.Database.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Obtener el ID del hotel seleccionado
                            int idHotel = Convert.ToInt32(cboHotel.SelectedValue);

                            // Obtener IDs de catálogos
                            int idTipoCama = Convert.ToInt32(cboTipoCama.SelectedValue);
                            int idNivel = Convert.ToInt32(cboNivel.SelectedValue);
                            string ubicacion = cboUbicacion.Text;

                            // Verificar si estamos en modo edición o creación
                            if (idTipoHabitacionSeleccionado > 0)
                            {
                                // ACTUALIZAR TIPO DE HABITACIÓN EXISTENTE
                                string queryUpdate = @"
                                UPDATE TiposHabitacion SET
                                    Nombre = @Nombre,
                                    NumeroCamas = @NumeroCamas,
                                    IdTipoCama = @IdTipoCama,
                                    PrecioPorNoche = @PrecioPorNoche,
                                    CapacidadPersonas = @CapacidadPersonas,
                                    IdNivel = @IdNivel,
                                    Ubicacion = @Ubicacion,
                                    CantidadHabitaciones = @CantidadHabitaciones,
                                    Descripcion = @Descripcion,
                                    FechaModificacion = @FechaModificacion,
                                    UsuarioModificacion = @UsuarioModificacion
                                WHERE IdTipoHabitacion = @IdTipoHabitacion";

                                SqlCommand cmdUpdate = new SqlCommand(queryUpdate, conn, transaction);
                                cmdUpdate.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                                cmdUpdate.Parameters.AddWithValue("@NumeroCamas", numeroCamas);
                                cmdUpdate.Parameters.AddWithValue("@IdTipoCama", idTipoCama);
                                cmdUpdate.Parameters.AddWithValue("@PrecioPorNoche", precio);
                                cmdUpdate.Parameters.AddWithValue("@CapacidadPersonas", capacidad);
                                cmdUpdate.Parameters.AddWithValue("@IdNivel", idNivel);
                                cmdUpdate.Parameters.AddWithValue("@Ubicacion", ubicacion);
                                cmdUpdate.Parameters.AddWithValue("@CantidadHabitaciones", cantidadHabitaciones);
                                cmdUpdate.Parameters.AddWithValue("@Descripcion", txtCaracteristicas.Text.Trim());
                                cmdUpdate.Parameters.AddWithValue("@FechaModificacion", DateTime.Now);
                                cmdUpdate.Parameters.AddWithValue("@UsuarioModificacion", Session.IdUsuario);
                                cmdUpdate.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacionSeleccionado);

                                cmdUpdate.ExecuteNonQuery();

                                // Eliminar las amenidades existentes para este tipo de habitación
                                string queryDeleteAmenidades = "DELETE FROM TipoHabitacionAmenidades WHERE IdTipoHabitacion = @IdTipoHabitacion";
                                SqlCommand cmdDeleteAmenidades = new SqlCommand(queryDeleteAmenidades, conn, transaction);
                                cmdDeleteAmenidades.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacionSeleccionado);
                                cmdDeleteAmenidades.ExecuteNonQuery();

                                // Insertar las nuevas amenidades seleccionadas
                                insertarAmenidades(idTipoHabitacionSeleccionado, conn, transaction);

                                transaction.Commit();
                                // Actualizar habitaciones individuales
                                generarHabitacionesIndividuales(idTipoHabitacionSeleccionado, idHotel, cantidadHabitaciones);
                                MessageBox.Show("Tipo de habitación actualizado exitosamente", "Éxito",
                                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            else
                            {
                                // INSERTAR UN NUEVO TIPO DE HABITACIÓN
                                string query = @"
                                INSERT INTO TiposHabitacion (
                                    IdHotel, Nombre, NumeroCamas, IdTipoCama, PrecioPorNoche, 
                                    CapacidadPersonas, IdNivel, Ubicacion, CantidadHabitaciones, 
                                    Descripcion, FechaRegistro, UsuarioRegistro)
                                VALUES (
                                    @IdHotel, @Nombre, @NumeroCamas, @IdTipoCama, @PrecioPorNoche,
                                    @CapacidadPersonas, @IdNivel, @Ubicacion, @CantidadHabitaciones,
                                    @Descripcion, @FechaRegistro, @UsuarioRegistro);
                                SELECT SCOPE_IDENTITY();";

                                SqlCommand cmdInsert = new SqlCommand(query, conn, transaction);
                                cmdInsert.Parameters.AddWithValue("@IdHotel", idHotel);
                                cmdInsert.Parameters.AddWithValue("@Nombre", txtNombre.Text.Trim());
                                cmdInsert.Parameters.AddWithValue("@NumeroCamas", numeroCamas);
                                cmdInsert.Parameters.AddWithValue("@IdTipoCama", idTipoCama);
                                cmdInsert.Parameters.AddWithValue("@PrecioPorNoche", precio);
                                cmdInsert.Parameters.AddWithValue("@CapacidadPersonas", capacidad);
                                cmdInsert.Parameters.AddWithValue("@IdNivel", idNivel);
                                cmdInsert.Parameters.AddWithValue("@Ubicacion", ubicacion);
                                cmdInsert.Parameters.AddWithValue("@CantidadHabitaciones", cantidadHabitaciones);
                                cmdInsert.Parameters.AddWithValue("@Descripcion", txtCaracteristicas.Text.Trim());
                                cmdInsert.Parameters.AddWithValue("@FechaRegistro", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@UsuarioRegistro", Session.IdUsuario);

                                // Ejecutar y obtener el ID generado
                                int idTipoHabitacion = Convert.ToInt32(cmdInsert.ExecuteScalar());

                                // Insertar relaciones con amenidades
                                insertarAmenidades(idTipoHabitacion, conn, transaction);

                                transaction.Commit();

                                // Generar habitaciones individuales
                                generarHabitacionesIndividuales(idTipoHabitacion, idHotel, cantidadHabitaciones);
                                MessageBox.Show("Tipo de habitación creado exitosamente", "Éxito",
                                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }

                            LimpiarCampos();
                            cargarTiposHabitacion(); // Recargar la lista
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Error en transacción: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar tipo de habitación: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void insertarAmenidades(int idTipoHabitacion, SqlConnection conn, SqlTransaction transaction)
        {
            string queryInsertAmenidad = "INSERT INTO TipoHabitacionAmenidades (IdTipoHabitacion, IdAmenidad) VALUES (@IdTipoHabitacion, @IdAmenidad)";

            // Verificar cada checkbox y agregar la amenidad si está marcado
            if (chkCajaFuerte.Checked && chkCajaFuerte.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkCajaFuerte.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkMiniBar.Checked && chkMiniBar.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkMiniBar.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkCafetera.Checked && chkCafetera.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkCafetera.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkTV.Checked && chkTV.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkTV.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkJacuzzi.Checked && chkJacuzzi.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkJacuzzi.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkBalcon.Checked && chkBalcon.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkBalcon.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkServicioCuarto.Checked && chkServicioCuarto.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkServicioCuarto.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkEscritorio.Checked && chkEscritorio.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkEscritorio.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkVistaJardin.Checked && chkVistaJardin.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkVistaJardin.Tag));
                cmd.ExecuteNonQuery();
            }

            if (chkVistaPlaya.Checked && chkVistaPlaya.Tag != null)
            {
                SqlCommand cmd = new SqlCommand(queryInsertAmenidad, conn, transaction);
                cmd.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                cmd.Parameters.AddWithValue("@IdAmenidad", Convert.ToInt32(chkVistaPlaya.Tag));
                cmd.ExecuteNonQuery();
            }
        }

        // aki me kede
        private void LimpiarCampos()
        {
            // Limpiar campos de texto
            txtNombre.Text = "";
            txtPrecio.Text = "";
            txtNumeroCamas.Text = "";
            txtCapacidad.Text = "";
            txtCantidadHabitaciones.Text = "";
            txtCaracteristicas.Text = "";

            // Restablecer combos a su primer elemento
            if (cboTipoCama.Items.Count > 0) cboTipoCama.SelectedIndex = 0;
            if (cboNivel.Items.Count > 0) cboNivel.SelectedIndex = 0;
            if (cboUbicacion.Items.Count > 0) cboUbicacion.SelectedIndex = 0;

            // Desmarcar todos los checkboxes de amenidades
            chkCajaFuerte.Checked = false;
            chkCafetera.Checked = false;
            chkBalcon.Checked = false;
            chkEscritorio.Checked = false;
            chkVistaJardin.Checked = false;
            chkMiniBar.Checked = false;
            chkTV.Checked = false;
            chkJacuzzi.Checked = false;
            chkServicioCuarto.Checked = false;
            chkVistaPlaya.Checked = false;

            // Habilitar el combo de hotel en caso de que se hubiera deshabilitado
            cboHotel.Enabled = true;

            // Restablecer el ID de edición y el botón
            idTipoHabitacionSeleccionado = 0;
            btnGuardar.Text = "Guardar";
        }

        private void btnEditar_Click(object sender, EventArgs e)
        {
            if (dgvTiposHabitacion.CurrentRow == null)
            {
                MessageBox.Show("Por favor, seleccione un tipo de habitación para editar", "Selección requerida",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            idTipoHabitacionSeleccionado = Convert.ToInt32(dgvTiposHabitacion.CurrentRow.Cells["IdTipoHabitacion"].Value);

            try
            {
                // Cargamos datos básicos del tipo de habitación
                string queryTipo = @"
                SELECT t.Nombre, t.NumeroCamas, t.IdTipoCama, tc.Nombre AS TipoCama, 
                       t.PrecioPorNoche, t.CapacidadPersonas, t.IdNivel, n.Nombre AS Nivel, 
                       t.Ubicacion, t.CantidadHabitaciones, t.Descripcion, t.EstadoActivo
                FROM TiposHabitacion t
                INNER JOIN CatalogoTiposCama tc ON t.IdTipoCama = tc.IdTipoCama
                INNER JOIN CatalogoNivelesHabitacion n ON t.IdNivel = n.IdNivel
                WHERE t.IdTipoHabitacion = @IdTipoHabitacion";

                SqlParameter[] paramsTipo = new SqlParameter[]
                {
                new SqlParameter("@IdTipoHabitacion", idTipoHabitacionSeleccionado)
                };

                DataTable dtTipo = Data.Database.ExecuteQuery(queryTipo, paramsTipo);
                if (dtTipo.Rows.Count > 0)
                {
                    // Llenar los campos de texto
                    txtNombre.Text = dtTipo.Rows[0]["Nombre"].ToString();
                    txtNumeroCamas.Text = dtTipo.Rows[0]["NumeroCamas"].ToString();
                    txtPrecio.Text = dtTipo.Rows[0]["PrecioPorNoche"].ToString();
                    txtCapacidad.Text = dtTipo.Rows[0]["CapacidadPersonas"].ToString();
                    txtCantidadHabitaciones.Text = dtTipo.Rows[0]["CantidadHabitaciones"].ToString();
                    txtCaracteristicas.Text = dtTipo.Rows[0]["Descripcion"].ToString();

                    // Seleccionar tipo de cama en el combobox
                    cboTipoCama.SelectedValue = dtTipo.Rows[0]["IdTipoCama"];

                    // Seleccionar nivel en el combobox
                    cboNivel.SelectedValue = dtTipo.Rows[0]["IdNivel"];

                    // Seleccionar ubicación en el combobox
                    string ubicacion = dtTipo.Rows[0]["Ubicacion"].ToString();
                    for (int i = 0; i < cboUbicacion.Items.Count; i++)
                    {
                        if (cboUbicacion.Items[i].ToString() == ubicacion)
                        {
                            cboUbicacion.SelectedIndex = i;
                            break;
                        }
                    }

                    // Desmarcar todos los checkboxes primero
                    chkCajaFuerte.Checked = false;
                    chkCafetera.Checked = false;
                    chkBalcon.Checked = false;
                    chkEscritorio.Checked = false;
                    chkVistaJardin.Checked = false;
                    chkMiniBar.Checked = false;
                    chkTV.Checked = false;
                    chkJacuzzi.Checked = false;
                    chkServicioCuarto.Checked = false;
                    chkVistaPlaya.Checked = false;

                    // Consultar y marcar las amenidades asociadas
                    string queryAmenidades = @"
                    SELECT tha.IdAmenidad, a.Nombre
                    FROM TipoHabitacionAmenidades tha
                    INNER JOIN CatalogoAmenidades a ON tha.IdAmenidad = a.IdAmenidad
                    WHERE tha.IdTipoHabitacion = @IdTipoHabitacion";

                    SqlParameter[] paramsAmenidades = new SqlParameter[]
                    {
                    new SqlParameter("@IdTipoHabitacion", idTipoHabitacionSeleccionado)
                    };

                    DataTable dtAmenidades = Data.Database.ExecuteQuery(queryAmenidades, paramsAmenidades);

                    // Marcar los checkboxes según las amenidades asociadas
                    foreach (DataRow row in dtAmenidades.Rows)
                    {
                        int idAmenidad = Convert.ToInt32(row["IdAmenidad"]);
                        string nombreAmenidad = row["Nombre"].ToString();

                        // Marcar el checkbox correspondiente
                        switch (nombreAmenidad)
                        {
                            case "Caja fuerte":
                                chkCajaFuerte.Checked = true;
                                break;
                            case "Minibar":
                                chkMiniBar.Checked = true;
                                break;
                            case "Cafetera":
                                chkCafetera.Checked = true;
                                break;
                            case "TV":
                                chkTV.Checked = true;
                                break;
                            case "Jacuzzi":
                                chkJacuzzi.Checked = true;
                                break;
                            case "Balcón privado":
                                chkBalcon.Checked = true;
                                break;
                            case "Servicio a cuarto":
                                chkServicioCuarto.Checked = true;
                                break;
                            case "Escritorio":
                                chkEscritorio.Checked = true;
                                break;
                            case "Vista a jardín":
                                chkVistaJardin.Checked = true;
                                break;
                            case "Vista a playa":
                                chkVistaPlaya.Checked = true;
                                break;
                        }
                    }

                    // Cambiar el texto del botón Guardar
                    btnGuardar.Text = "Actualizar";

                    // Deshabilitar campos que no deberían cambiar durante la edición
                    cboHotel.Enabled = false; // Si el tipo de habitación no puede cambiar de hotel

                    
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos para edición: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                idTipoHabitacionSeleccionado = 0;
            }
        }

        private void generarHabitacionesIndividuales(int idTipoHabitacion, int idHotel, int cantidadHabitaciones)
        {
            try
            {
                // 1. Obtenemos el número de pisos del hotel
                string queryPisos = "SELECT NumeroPisos FROM Hoteles WHERE IdHotel = @IdHotel";
                SqlParameter[] paramsPisos = new SqlParameter[] {
                new SqlParameter("@IdHotel", idHotel)
                };

                DataTable dtPisos = Data.Database.ExecuteQuery(queryPisos, paramsPisos);
                if (dtPisos.Rows.Count == 0)
                {
                    MessageBox.Show("No se pudo obtener la información del hotel", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int numeroPisos = Convert.ToInt32(dtPisos.Rows[0]["NumeroPisos"]);

                // 2. Obtenemos cuántas habitaciones ya existen de este tipo
                string queryExistentes = "SELECT COUNT(*) FROM Habitaciones WHERE IdTipoHabitacion = @IdTipoHabitacion";
                SqlParameter[] paramsExistentes = new SqlParameter[] {
                new SqlParameter("@IdTipoHabitacion", idTipoHabitacion)
                };

                int habitacionesExistentes = Convert.ToInt32(Data.Database.ExecuteScalar(queryExistentes, paramsExistentes));

                // 3. Calculamos cuántas nuevas habitaciones hay que crear
                int nuevasHabitaciones = cantidadHabitaciones - habitacionesExistentes;

                if (nuevasHabitaciones <= 0)
                {
                    // No hay nuevas habitaciones que crear
                    return;
                }

                // 4. Creamos las nuevas habitaciones
                using (SqlConnection conn = new SqlConnection(Data.Database.ConnectionString))
                {
                    conn.Open();
                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            // Distribuir habitaciones entre pisos
                            int habitacionesPorPiso = nuevasHabitaciones / numeroPisos;
                            int habitacionesRestantes = nuevasHabitaciones % numeroPisos;

                            // Obtener el último número usado por piso
                            for (int piso = 1; piso <= numeroPisos; piso++)
                            {
                                // Obtener el último número usado en este piso
                                string queryUltimoNumero = @"
                                SELECT TOP 1 NumeroHabitacion 
                                FROM Habitaciones 
                                WHERE IdTipoHabitacion = @IdTipoHabitacion 
                                AND Piso = @Piso 
                                ORDER BY NumeroHabitacion DESC";

                                SqlCommand cmdUltimoNumero = new SqlCommand(queryUltimoNumero, conn, transaction);
                                cmdUltimoNumero.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                                cmdUltimoNumero.Parameters.AddWithValue("@Piso", piso);

                                object ultimoNumero = cmdUltimoNumero.ExecuteScalar();
                                int numeroInicial = 1;

                                if (ultimoNumero != null && ultimoNumero != DBNull.Value)
                                {
                                    string strUltimoNumero = ultimoNumero.ToString();
                                    // Extraer solo el número
                                    string numeroStr = strUltimoNumero.Substring(1); // Quitar el primer dígito (piso)
                                    numeroInicial = int.Parse(numeroStr) + 1;
                                }

                                // Calcular cuántas habitaciones crear en este piso
                                int habitacionesEnEstePiso = habitacionesPorPiso;
                                if (piso <= habitacionesRestantes)
                                {
                                    habitacionesEnEstePiso++; // Distribuir las habitaciones restantes
                                }

                                // Crear habitaciones en este piso
                                for (int i = 0; i < habitacionesEnEstePiso; i++)
                                {
                                    string numeroHabitacion = piso.ToString() + numeroInicial.ToString().PadLeft(2, '0');

                                    string queryInsert = @"
                                    INSERT INTO Habitaciones (
                                        IdTipoHabitacion, NumeroHabitacion, Piso, Estado, FechaRegistro, UsuarioRegistro)
                                    VALUES (
                                        @IdTipoHabitacion, @NumeroHabitacion, @Piso, 'Disponible', GETDATE(), @UsuarioRegistro)";

                                    SqlCommand cmdInsert = new SqlCommand(queryInsert, conn, transaction);
                                    cmdInsert.Parameters.AddWithValue("@IdTipoHabitacion", idTipoHabitacion);
                                    cmdInsert.Parameters.AddWithValue("@NumeroHabitacion", numeroHabitacion);
                                    cmdInsert.Parameters.AddWithValue("@Piso", piso);
                                    cmdInsert.Parameters.AddWithValue("@UsuarioRegistro", Session.IdUsuario);

                                    cmdInsert.ExecuteNonQuery();
                                    numeroInicial++;
                                }
                            }

                            transaction.Commit();
                            MessageBox.Show($"Se crearon {nuevasHabitaciones} habitaciones nuevas", "Éxito",
                                           MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw new Exception("Error al crear habitaciones: " + ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error en la generación de habitaciones: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void chkAireAcondicionado_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button16_Click(object sender, EventArgs e)
        {
            LimpiarCampos();
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

        private void btnFiltrar_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar que el combo tenga un hotel seleccionado
                if (cboHotel.SelectedItem == null)
                {
                    MessageBox.Show("Por favor, seleccione un hotel", "Hotel requerido",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Cargar los tipos de habitación del hotel seleccionado
                cargarTiposHabitacion();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al filtrar: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLimpiarFiltro_Click(object sender, EventArgs e)
        {
            try
            {
                // Limpiar el DataGridView
                dgvTiposHabitacion.DataSource = null;

                // Seleccionar el primer elemento del combo de hoteles
                if (cboHotel.Items.Count > 0)
                    cboHotel.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al limpiar filtro: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
