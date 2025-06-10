using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HotelManager.Classes;
using HotelManager.Forms;


namespace HotelManager
{
    public partial class GestionHoteles : Form
    {
        public GestionHoteles()
        {
            InitializeComponent();
        }

        private void GestionHoteles_Load(object sender, EventArgs e)
        {
            // Verificamos que solo los administradores puedan acceder
            if (Session.TipoUsuario != "Administrador")
            {
                MessageBox.Show("Solo los administradores pueden acceder a la gestión de hoteles.",
                               "Acceso denegado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
                this.Close();
                return;
            }

            // Cargamos los catálogos necesarios para los servicios
            cargarCatalogoServicios();

            // Cargamos la lista de hoteles
            cargarHoteles();
        }

        private void cargarCatalogoServicios()
        {
            try
            {
                // Modificamos la consulta para incluir el campo RequiereCantidad
                string query = "SELECT IdServicioCatalogo, Nombre, RequiereCantidad FROM CatalogoServicios ORDER BY Nombre";
                DataTable dtServicios = Data.Database.ExecuteQuery(query);

                // Configurar los controles según los servicios disponibles
                foreach (DataRow row in dtServicios.Rows)
                {
                    int idServicio = Convert.ToInt32(row["IdServicioCatalogo"]);
                    string nombre = row["Nombre"].ToString();
                    bool requiereCantidad = Convert.ToBoolean(row["RequiereCantidad"]);

                    // Asignar los IDs de servicios a los tags de los checkboxes
                    switch (nombre)
                    {
                        case "WiFi":
                            chkWiFi.Tag = idServicio;
                            break;
                        case "Pet Friendly":
                            chkPetFriendly.Tag = idServicio;
                            break;
                        case "Estacionamiento":
                            chkEstacionamiento.Tag = idServicio;
                            break;
                        case "Áreas verdes":
                            chkAreasVerdes.Tag = idServicio;
                            break;
                        case "Business Center":
                            chkBusinessCenter.Tag = idServicio;
                            numBusinessCenter.Enabled = chkBusinessCenter.Checked;
                            break;
                        case "Gimnasio":
                            chkGimnasio.Tag = idServicio;
                            break;
                        case "Zona Kids":
                            chkZonaKids.Tag = idServicio;
                            break;
                        case "Salón de juegos":
                            chkSalonJuegos.Tag = idServicio;
                            break;
                        case "Recepción 24/7":
                            chkRecepcion.Tag = idServicio;
                            break;
                        case "Piscinas":
                            chkPiscinas.Tag = idServicio;
                            numPiscinas.Enabled = chkPiscinas.Checked;
                            break;
                    }
                }

                // Configurar los eventos para habilitar/deshabilitar contadores
                chkBusinessCenter.CheckedChanged += (s, e) => numBusinessCenter.Enabled = chkBusinessCenter.Checked;
                chkPiscinas.CheckedChanged += (s, e) => numPiscinas.Enabled = chkPiscinas.Checked;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar catálogo de servicios: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cargarHoteles()
        {
            try
            {
                string query = @"
                SELECT h.IdHotel, h.Nombre, h.Ciudad, h.Estado, h.Pais, h.NumeroPisos,
                       FORMAT(h.FechaInicioOperaciones, 'dd-MMM-yyyy') AS InicioOps,
                       h.RFC, h.RazonSocial, h.RegimenFiscal, h.CodigoPostal,
                       CASE WHEN h.EstadoActivo = 1 THEN 'Activo' ELSE 'Inactivo' END AS EstadoHotel,
                       u.Nombre AS UsuarioRegistro,
                       FORMAT(h.FechaRegistro, 'dd-MMM-yyyy') AS FechaRegistro
                FROM Hoteles h
                INNER JOIN Usuarios u ON h.UsuarioRegistro = u.IdUsuario
                ORDER BY h.Nombre";

                DataTable dtHoteles = Data.Database.ExecuteQuery(query);

                // Asignamos los datos al DataGridView
                dgvHoteles.DataSource = dtHoteles;

                // Ocultamos la columna ID
                if (dgvHoteles.Columns.Contains("IdHotel"))
                    dgvHoteles.Columns["IdHotel"].Visible = false;

                estiloDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar hoteles: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button12_Click(object sender, EventArgs e)
        {
            // No lo uso pero si lo quito deja de jalar mi ventana xd
        }

        private void estiloDataGridView()
        {
            // Establecemos las propiedades básicas del DataGridView
            dgvHoteles.BorderStyle = BorderStyle.None;
            dgvHoteles.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvHoteles.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro (no rojo)
            dgvHoteles.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvHoteles.DefaultCellStyle.SelectionForeColor = Color.Black;

            dgvHoteles.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvHoteles.EnableHeadersVisualStyles = false;
            dgvHoteles.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvHoteles.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100);
            dgvHoteles.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvHoteles.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 10, FontStyle.Bold);
            dgvHoteles.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvHoteles.ColumnHeadersHeight = 40;

            // Estilo para las filas y celdas
            dgvHoteles.RowTemplate.Height = 35;
            dgvHoteles.DefaultCellStyle.Font = new Font("Yu Gothic", 9);
            dgvHoteles.DefaultCellStyle.Padding = new Padding(5);
            dgvHoteles.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvHoteles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // Añadir un borde fino alrededor de la tabla
            dgvHoteles.BorderStyle = BorderStyle.FixedSingle;
            dgvHoteles.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvHoteles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHoteles.MultiSelect = false; // Permitir seleccionar solo una fila a la vez
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

        private int idHotelEdicion = 0;

        private void limpiarCampos()
        {
            txtNombre.Text = "";
            txtCiudad.Text = "";
            txtEstado.Text = "";
            txtPais.Text = "";
            txtDomicilio.Text = "";
            txtRFC.Text = "";
            txtRazonSocial.Text = "";
            txtCodigoPostal.Text = "";
            dtpFechaInicio.Value = DateTime.Now;

            // Desmarcamos todos los checkboxes
            chkWiFi.Checked = false;
            chkPetFriendly.Checked = false;
            chkEstacionamiento.Checked = false;
            chkAreasVerdes.Checked = false;
            chkBusinessCenter.Checked = false;
            chkZonaKids.Checked = false;
            chkGimnasio.Checked = false;
            chkPiscinas.Checked = false;
            chkSalonJuegos.Checked = false;
            chkRecepcion.Checked = false;

            // Reseteamos contadores
            numBusinessCenter.Value = 0;
            numPiscinas.Value = 0;
            numBusinessCenter.Enabled = false;
            numPiscinas.Enabled = false;

            // Reseteamos el ID de edición
            idHotelEdicion = 0;

            // Restauramos texto del botón
            btnGuardar.Text = "Guardar";
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validaciones básicas
            if (string.IsNullOrEmpty(txtNombre.Text) ||
                string.IsNullOrEmpty(txtCiudad.Text) ||
                string.IsNullOrEmpty(txtEstado.Text) ||
                string.IsNullOrEmpty(txtPais.Text) ||
                string.IsNullOrEmpty(txtDomicilio.Text) ||
                string.IsNullOrEmpty(txtNumeroPisos.Text) ||
                string.IsNullOrEmpty(txtRFC.Text) ||
                string.IsNullOrEmpty(txtRazonSocial.Text) ||
                string.IsNullOrEmpty(txtCodigoPostal.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos obligatorios", "Campos vacíos",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validamos que el número de pisos sea un número válido
            if (!int.TryParse(txtNumeroPisos.Text, out int numeroPisos) || numeroPisos <= 0)
            {
                MessageBox.Show("Por favor, ingrese un número de pisos válido", "Valor inválido",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtNumeroPisos.Focus();
                return;
            }

            // Validamos RFC (formato básico para México: 12 o 13 caracteres)
            if (txtRFC.Text.Length != 12 && txtRFC.Text.Length != 13)
            {
                MessageBox.Show("El RFC debe tener 12 caracteres para personas morales o 13 para personas físicas",
                              "RFC inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRFC.Focus();
                return;
            }

            // Validamos código postal (5 dígitos)
            if (txtCodigoPostal.Text.Length != 5 || !int.TryParse(txtCodigoPostal.Text, out _))
            {
                MessageBox.Show("El código postal debe ser de 5 dígitos",
                              "Código postal inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCodigoPostal.Focus();
                return;
            }

            try
            {
                // Lista de consultas para la transacción
                List<Tuple<string, SqlParameter[]>> queries = new List<Tuple<string, SqlParameter[]>>();

                if (idHotelEdicion == 0) // Nuevo hotel
                {
                    // 1. Obtenemos el ID del nuevo hotel
                    string queryInsertHotel = @"
                        INSERT INTO Hoteles (
                            Nombre, Ciudad, Estado, Pais, Domicilio, NumeroPisos,
                            FechaInicioOperaciones, RFC, RazonSocial,
                            CodigoPostal, RegimenFiscal, UsuarioRegistro
                        )
                        VALUES (
                            @Nombre, @Ciudad, @Estado, @Pais, @Domicilio, @NumeroPisos,
                            @FechaInicioOperaciones, @RFC, @RazonSocial,
                            @CodigoPostal, @RegimenFiscal, @UsuarioRegistro
                        );
                        SELECT SCOPE_IDENTITY();";

                    SqlParameter[] paramsHotel = new SqlParameter[]
                    {
                        new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                        new SqlParameter("@Ciudad", txtCiudad.Text.Trim()),
                        new SqlParameter("@Estado", txtEstado.Text.Trim()),
                        new SqlParameter("@Pais", txtPais.Text.Trim()),
                        new SqlParameter("@Domicilio", txtDomicilio.Text.Trim()),
                        new SqlParameter("@NumeroPisos", numeroPisos),     // Agregar este parámetro
                        new SqlParameter("@FechaInicioOperaciones", dtpFechaInicio.Value.Date),
                        new SqlParameter("@RFC", txtRFC.Text.Trim().ToUpper()),
                        new SqlParameter("@RazonSocial", txtRazonSocial.Text.Trim()),
                        new SqlParameter("@CodigoPostal", txtCodigoPostal.Text.Trim()),
                        new SqlParameter("@RegimenFiscal", "601 - General de Ley Personas Morales"),
                        new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                    };

                    object result = Data.Database.ExecuteScalar(queryInsertHotel, paramsHotel);
                    int idHotel = Convert.ToInt32(result);

                    // 2. Insertamos los servicios del hotel
                    InsertarServiciosHotel(idHotel);

                    MessageBox.Show("Hotel creado exitosamente", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    limpiarCampos();
                    cargarHoteles();
                }
                else // Editar hotel existente
                {
                    // 1. Actualizamos los datos del hotel
                    string queryUpdateHotel = @"
                        UPDATE Hoteles 
                        SET Nombre = @Nombre, 
                            Ciudad = @Ciudad, 
                            Estado = @Estado, 
                            Pais = @Pais, 
                            Domicilio = @Domicilio,
                            NumeroPisos = @NumeroPisos,
                            FechaInicioOperaciones = @FechaInicioOperaciones,
                            RFC = @RFC,
                            RazonSocial = @RazonSocial,
                            CodigoPostal = @CodigoPostal,
                            RegimenFiscal = @RegimenFiscal,
                            FechaModificacion = @FechaModificacion,
                            UsuarioModificacion = @UsuarioModificacion
                        WHERE IdHotel = @IdHotel";

                    SqlParameter[] paramsUpdateHotel = new SqlParameter[]
                    {
                        new SqlParameter("@IdHotel", idHotelEdicion),
                        new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                        new SqlParameter("@Ciudad", txtCiudad.Text.Trim()),
                        new SqlParameter("@Estado", txtEstado.Text.Trim()),
                        new SqlParameter("@Pais", txtPais.Text.Trim()),
                        new SqlParameter("@Domicilio", txtDomicilio.Text.Trim()),
                        new SqlParameter("@NumeroPisos", numeroPisos),     // Agregar este parámetro
                        new SqlParameter("@FechaInicioOperaciones", dtpFechaInicio.Value.Date),
                        new SqlParameter("@RFC", txtRFC.Text.Trim().ToUpper()),
                        new SqlParameter("@RazonSocial", txtRazonSocial.Text.Trim()),
                        new SqlParameter("@CodigoPostal", txtCodigoPostal.Text.Trim()),
                        new SqlParameter("@RegimenFiscal", "601 - General de Ley Personas Morales"),
                        new SqlParameter("@FechaModificacion", DateTime.Now),
                        new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                    };

                    int resultUpdate = Data.Database.ExecuteNonQuery(queryUpdateHotel, paramsUpdateHotel);

                    if (resultUpdate > 0)
                    {
                        // 2. Eliminar servicios existentes
                        string queryDeleteServices = "DELETE FROM HotelServicios WHERE IdHotel = @IdHotel";
                        SqlParameter[] paramsDeleteServices = new SqlParameter[] {
                    new SqlParameter("@IdHotel", idHotelEdicion)
                };

                        Data.Database.ExecuteNonQuery(queryDeleteServices, paramsDeleteServices);

                        // 3. Insertar los servicios actualizados
                        InsertarServiciosHotel(idHotelEdicion);

                        MessageBox.Show("Hotel actualizado exitosamente", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        limpiarCampos();
                        cargarHoteles();
                        btnGuardar.Text = "Guardar"; // Restaurar el texto del botón
                    }
                    else
                    {
                        MessageBox.Show("No se pudo actualizar el hotel", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar hotel: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InsertarServiciosHotel(int idHotel)
        {
            // Lista para almacenar todas las consultas de servicios
            List<Tuple<string, SqlParameter[]>> queriesServicios = new List<Tuple<string, SqlParameter[]>>();
            string queryTemplate = "INSERT INTO HotelServicios (IdHotel, IdServicioCatalogo, Cantidad) VALUES (@IdHotel, @IdServicio, @Cantidad)";

            // WiFi
            if (chkWiFi.Checked && chkWiFi.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@IdHotel", idHotel),
                new SqlParameter("@IdServicio", Convert.ToInt32(chkWiFi.Tag)),
                new SqlParameter("@Cantidad", 1)
            };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Pet Friendly
            if (chkPetFriendly.Checked && chkPetFriendly.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@IdHotel", idHotel),
                new SqlParameter("@IdServicio", Convert.ToInt32(chkPetFriendly.Tag)),
                new SqlParameter("@Cantidad", 1)
            };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Estacionamiento
            if (chkEstacionamiento.Checked && chkEstacionamiento.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkEstacionamiento.Tag)),
            new SqlParameter("@Cantidad", 1)
            };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Áreas verdes
            if (chkAreasVerdes.Checked && chkAreasVerdes.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkAreasVerdes.Tag)),
            new SqlParameter("@Cantidad", 1)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Business Center
            if (chkBusinessCenter.Checked && chkBusinessCenter.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkBusinessCenter.Tag)),
            new SqlParameter("@Cantidad", (int)numBusinessCenter.Value)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Gimnasio
            if (chkGimnasio.Checked && chkGimnasio.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkGimnasio.Tag)),
            new SqlParameter("@Cantidad", 1)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Zona Kids
            if (chkZonaKids.Checked && chkZonaKids.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkZonaKids.Tag)),
            new SqlParameter("@Cantidad", 1)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Salón de juegos
            if (chkSalonJuegos.Checked && chkSalonJuegos.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkSalonJuegos.Tag)),
            new SqlParameter("@Cantidad", 1)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Recepción 24/7
            if (chkRecepcion.Checked && chkRecepcion.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkRecepcion.Tag)),
            new SqlParameter("@Cantidad", 1)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Piscinas
            if (chkPiscinas.Checked && chkPiscinas.Tag != null)
            {
                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@IdServicio", Convert.ToInt32(chkPiscinas.Tag)),
            new SqlParameter("@Cantidad", (int)numPiscinas.Value)
        };
                queriesServicios.Add(new Tuple<string, SqlParameter[]>(queryTemplate, parameters));
            }

            // Ejecutar todas las consultas de servicios en una transacción
            if (queriesServicios.Count > 0)
            {
                Data.Database.ExecuteTransaction(queriesServicios);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            // Preguntar al usuario si está seguro de cancelar la operación
            DialogResult result = MessageBox.Show("¿Está seguro que desea cancelar? Se perderán los cambios no guardados.",
                                                 "Confirmar cancelación",
                                                 MessageBoxButtons.YesNo,
                                                 MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                limpiarCampos();
            }
        }

        private void chkBuffet_CheckedChanged(object sender, EventArgs e)
        {
            // Si lo quito, deja de jalar mi programa xdd
        }

        private void btnEditarHotel_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificamos si hay alguna fila seleccionada
                if (dgvHoteles.CurrentRow == null)
                {
                    MessageBox.Show("Por favor, seleccione un hotel para editar", "Selección requerida",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener el ID del hotel seleccionado
                idHotelEdicion = Convert.ToInt32(dgvHoteles.CurrentRow.Cells["IdHotel"].Value);

                // Cargar datos básicos del hotel
                string queryHotel = @"
                    SELECT Nombre, Ciudad, Estado, Pais, Domicilio, NumeroPisos,
                           FechaInicioOperaciones, RFC, RazonSocial, CodigoPostal
                    FROM Hoteles
                    WHERE IdHotel = @IdHotel";

                SqlParameter[] parametersHotel = new SqlParameter[]
                {
                    new SqlParameter("@IdHotel", idHotelEdicion)
                };

                DataTable dtHotel = Data.Database.ExecuteQuery(queryHotel, parametersHotel);
                if (dtHotel.Rows.Count > 0)
                {
                    // Llenar los campos con los datos del hotel
                    txtNombre.Text = dtHotel.Rows[0]["Nombre"].ToString();
                    txtCiudad.Text = dtHotel.Rows[0]["Ciudad"].ToString();
                    txtEstado.Text = dtHotel.Rows[0]["Estado"].ToString();
                    txtPais.Text = dtHotel.Rows[0]["Pais"].ToString();
                    txtDomicilio.Text = dtHotel.Rows[0]["Domicilio"].ToString();
                    txtNumeroPisos.Text = dtHotel.Rows[0]["NumeroPisos"].ToString();
                    txtRFC.Text = dtHotel.Rows[0]["RFC"].ToString();
                    txtRazonSocial.Text = dtHotel.Rows[0]["RazonSocial"].ToString();
                    txtCodigoPostal.Text = dtHotel.Rows[0]["CodigoPostal"].ToString();

                    if (dtHotel.Rows[0]["FechaInicioOperaciones"] != DBNull.Value)
                        dtpFechaInicio.Value = Convert.ToDateTime(dtHotel.Rows[0]["FechaInicioOperaciones"]);

                    // Desmarcar todos los checkboxes primero
                    chkWiFi.Checked = false;
                    chkPetFriendly.Checked = false;
                    chkEstacionamiento.Checked = false;
                    chkAreasVerdes.Checked = false;
                    chkBusinessCenter.Checked = false;
                    chkZonaKids.Checked = false;
                    chkGimnasio.Checked = false;
                    chkPiscinas.Checked = false;
                    chkSalonJuegos.Checked = false;
                    chkRecepcion.Checked = false;

                    // Resetear contadores
                    numBusinessCenter.Value = 0;
                    numPiscinas.Value = 0;

                    // Cargar servicios del hotel desde la tabla HotelServicios
                    string queryServicios = @"
                        SELECT hs.IdServicioCatalogo, cs.Nombre, hs.Cantidad 
                        FROM HotelServicios hs
                        JOIN CatalogoServicios cs ON hs.IdServicioCatalogo = cs.IdServicioCatalogo
                        WHERE hs.IdHotel = @IdHotel";

                    SqlParameter[] parametersServicios = new SqlParameter[]
                    {
                        new SqlParameter("@IdHotel", idHotelEdicion)
                    };

                    DataTable dtServicios = Data.Database.ExecuteQuery(queryServicios, parametersServicios);

                    // Marcar los checkboxes correspondientes según los servicios asociados
                    foreach (DataRow row in dtServicios.Rows)
                    {
                        string nombreServicio = row["Nombre"].ToString();
                        int cantidad = Convert.ToInt32(row["Cantidad"]);

                        switch (nombreServicio)
                        {
                            case "WiFi":
                                chkWiFi.Checked = true;
                                break;
                            case "Pet Friendly":
                                chkPetFriendly.Checked = true;
                                break;
                            case "Estacionamiento":
                                chkEstacionamiento.Checked = true;
                                break;
                            case "Áreas verdes":
                                chkAreasVerdes.Checked = true;
                                break;
                            case "Business Center":
                                chkBusinessCenter.Checked = true;
                                numBusinessCenter.Value = cantidad;
                                break;
                            case "Gimnasio":
                                chkGimnasio.Checked = true;
                                break;
                            case "Zona Kids":
                                chkZonaKids.Checked = true;
                                break;
                            case "Salón de juegos":
                                chkSalonJuegos.Checked = true;
                                break;
                            case "Recepción 24/7":
                                chkRecepcion.Checked = true;
                                break;
                            case "Piscinas":
                                chkPiscinas.Checked = true;
                                numPiscinas.Value = cantidad;
                                break;
                        }
                    }

                    // Cambiamos el texto del botón
                    btnGuardar.Text = "Actualizar";
                }
                else
                {
                    MessageBox.Show("No se encontró el hotel seleccionado", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                    idHotelEdicion = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos del hotel: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                idHotelEdicion = 0;
            }
        }

        private void btnActivarDesactivar_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificamos si hay alguna fila seleccionada
                if (dgvHoteles.CurrentRow == null)
                {
                    MessageBox.Show("Por favor, seleccione un hotel para cambiar su estado",
                                    "Selección requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtenemos el ID del hotel seleccionado
                int idHotel = Convert.ToInt32(dgvHoteles.CurrentRow.Cells["IdHotel"].Value);
                string nombreHotel = dgvHoteles.CurrentRow.Cells["Nombre"].Value.ToString();

                // Verificamos el estado actual del hotel
                string queryEstado = "SELECT EstadoActivo FROM Hoteles WHERE IdHotel = @IdHotel";
                SqlParameter[] parametrosEstado = new SqlParameter[] { new SqlParameter("@IdHotel", idHotel) };

                object resultado = Data.Database.ExecuteScalar(queryEstado, parametrosEstado);
                bool estadoActual = Convert.ToBoolean(resultado);
                bool nuevoEstado = !estadoActual;

                // Confirmamos la acción con el usuario
                string mensaje = nuevoEstado
                    ? $"¿Está seguro que desea activar el hotel '{nombreHotel}'?"
                    : $"¿Está seguro que desea desactivar el hotel '{nombreHotel}'?";

                DialogResult confirmar = MessageBox.Show(mensaje, "Confirmar cambio de estado",
                                                     MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirmar != DialogResult.Yes)
                    return;

                // Actualizar el estado del hotel
                string queryUpdate = @"
                UPDATE Hoteles 
                SET EstadoActivo = @EstadoActivo,
                    FechaModificacion = @FechaModificacion,
                    UsuarioModificacion = @UsuarioModificacion
                WHERE IdHotel = @IdHotel";

                SqlParameter[] parametrosUpdate = new SqlParameter[]
                {
            new SqlParameter("@IdHotel", idHotel),
            new SqlParameter("@EstadoActivo", nuevoEstado),
            new SqlParameter("@FechaModificacion", DateTime.Now),
            new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                };

                int filasAfectadas = Data.Database.ExecuteNonQuery(queryUpdate, parametrosUpdate);

                if (filasAfectadas > 0)
                {
                    string mensajeExito = nuevoEstado
                        ? $"El hotel '{nombreHotel}' ha sido activado correctamente."
                        : $"El hotel '{nombreHotel}' ha sido desactivado correctamente.";

                    MessageBox.Show(mensajeExito, "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Recargar la lista de hoteles para reflejar el cambio
                    cargarHoteles();
                }
                else
                {
                    MessageBox.Show("No se pudo cambiar el estado del hotel",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cambiar el estado del hotel: " + ex.Message,
                               "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void panel6_Paint(object sender, PaintEventArgs e)
        {
            // Si lo quito, deja de funcionar mi programa
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
