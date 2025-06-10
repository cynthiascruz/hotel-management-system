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
    public partial class GestionClientes : Form
    {
        public GestionClientes()
        {
            InitializeComponent();
        }

        private void GestionClientes_Load(object sender, EventArgs e)
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
            inicializarComboBoxes();
            cargarClientes();
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validamos campos obligatorios
            if (string.IsNullOrEmpty(txtNombre.Text) ||
                string.IsNullOrEmpty(txtApellidoPaterno.Text) ||
                string.IsNullOrEmpty(txtApellidoMaterno.Text) ||
                string.IsNullOrEmpty(txtCorreo.Text) ||
                string.IsNullOrEmpty(txtRFC.Text) ||
                string.IsNullOrEmpty(txtCalle.Text) ||
                string.IsNullOrEmpty(txtNumero.Text) ||
                string.IsNullOrEmpty(txtColonia.Text) ||
                string.IsNullOrEmpty(txtCiudad.Text) ||
                string.IsNullOrEmpty(txtEstado.Text) ||
                string.IsNullOrEmpty(txtPais.Text) ||
                string.IsNullOrEmpty(txtCP.Text) ||
                string.IsNullOrEmpty(txtTelefonoCasa.Text) ||
                string.IsNullOrEmpty(txtTelefonoCelular.Text) ||
                cboEstadoCivil.SelectedIndex < 0 ||
                cboRegimen.SelectedIndex < 0 ||
                cboCFDI.SelectedIndex < 0)
            {
                MessageBox.Show("Por favor, complete todos los campos obligatorios", "Campos incompletos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validamos formato de correo electrónico
            if (!IsValidEmail(txtCorreo.Text))
            {
                MessageBox.Show("Por favor, ingrese un correo electrónico válido", "Formato inválido",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCorreo.Focus();
                return;
            }

            // Validamos formato de RFC (13 caracteres para personas físicas, 12 para morales)
            if (txtRFC.Text.Length != 13 && txtRFC.Text.Length != 12)
            {
                MessageBox.Show("El RFC debe tener 12 caracteres para personas morales o 13 para personas físicas",
                               "Formato inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRFC.Focus();
                return;
            }

            // Validamos formato de código postal (5 dígitos en México)
            if (txtCP.Text.Length != 5 || !txtCP.Text.All(char.IsDigit))
            {
                MessageBox.Show("El código postal debe ser de 5 dígitos", "Formato inválido",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCP.Focus();
                return;
            }

            // Validamos que la fecha de nacimiento sea razonable (no en el futuro y no demasiado en el pasado)
            if (dtpFechaNacimiento.Value > DateTime.Now)
            {
                MessageBox.Show("La fecha de nacimiento no puede ser en el futuro", "Fecha inválida",
                               MessageBoxButtons.OK, MessageBoxIcon.Warning);
                dtpFechaNacimiento.Focus();
                return;
            }

            try
            {
                // Obtenemos los códigos de régimen fiscal y uso CFDI
                string codigoRegimenFiscal = cboRegimen.SelectedItem.ToString().Substring(0, 3);
                string codigoUsoCFDI = cboCFDI.SelectedItem.ToString().Substring(0, 3);

                // Construimos el domicilio completo
                string domicilio = $"{txtCalle.Text} {txtNumero.Text}, Col. {txtColonia.Text}";

                if (modoEdicion)
                {
                    // Verificamos si los datos únicos no están duplicados
                    if (!verificarDatosUnicosEdicion())
                        return;

                    // Preparamos la consulta SQL para actualizar
                    string query = @"
                    UPDATE Clientes SET
                        Nombre = @Nombre,
                        ApellidoPaterno = @ApellidoPaterno,
                        ApellidoMaterno = @ApellidoMaterno,
                        Domicilio = @Domicilio,
                        Ciudad = @Ciudad,
                        Estado = @Estado,
                        Pais = @Pais,
                        CodigoPostal = @CodigoPostal,
                        RFC = @RFC,
                        Correo = @Correo,
                        TelefonoCasa = @TelefonoCasa,
                        TelefonoCelular = @TelefonoCelular,
                        FechaNacimiento = @FechaNacimiento,
                        EstadoCivil = @EstadoCivil,
                        RegimenFiscal = @RegimenFiscal,
                        UsoCFDI = @UsoCFDI,
                        FechaModificacion = GETDATE(),
                        UsuarioModificacion = @UsuarioModificacion
                    WHERE IdCliente = @IdCliente";

                    SqlParameter[] parameters = new SqlParameter[]
                    {
                        new SqlParameter("@IdCliente", idClienteSeleccionado),
                        new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                        new SqlParameter("@ApellidoPaterno", txtApellidoPaterno.Text.Trim()),
                        new SqlParameter("@ApellidoMaterno", txtApellidoMaterno.Text.Trim()),
                        new SqlParameter("@Domicilio", domicilio),
                        new SqlParameter("@Ciudad", txtCiudad.Text.Trim()),
                        new SqlParameter("@Estado", txtEstado.Text.Trim()),
                        new SqlParameter("@Pais", txtPais.Text.Trim()),
                        new SqlParameter("@CodigoPostal", txtCP.Text.Trim()),
                        new SqlParameter("@RFC", txtRFC.Text.Trim().ToUpper()),
                        new SqlParameter("@Correo", txtCorreo.Text.Trim().ToLower()),
                        new SqlParameter("@TelefonoCasa", string.IsNullOrEmpty(txtTelefonoCasa.Text) ? DBNull.Value : (object)txtTelefonoCasa.Text.Trim()),
                        new SqlParameter("@TelefonoCelular", txtTelefonoCelular.Text.Trim()),
                        new SqlParameter("@FechaNacimiento", dtpFechaNacimiento.Value.Date),
                        new SqlParameter("@EstadoCivil", cboEstadoCivil.SelectedItem.ToString()),
                        new SqlParameter("@RegimenFiscal", codigoRegimenFiscal),
                        new SqlParameter("@UsoCFDI", codigoUsoCFDI),
                        new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                    };

                    // Ejecutamos la consulta
                    Data.Database.ExecuteNonQuery(query, parameters);

                    MessageBox.Show("Cliente actualizado exitosamente", "Éxito",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Restablecemos estado
                    modoEdicion = false;
                    idClienteSeleccionado = 0;
                    btnGuardar.Text = "Guardar";
                }
                else
                {
                    string query = @"
                    INSERT INTO Clientes (
                        Nombre, ApellidoPaterno, ApellidoMaterno, Domicilio, Ciudad, Estado, Pais,
                        CodigoPostal, RFC, Correo, TelefonoCasa, TelefonoCelular, FechaNacimiento,
                        EstadoCivil, RegimenFiscal, UsoCFDI, UsuarioRegistro)
                    VALUES (
                        @Nombre, @ApellidoPaterno, @ApellidoMaterno, @Domicilio, @Ciudad, @Estado, @Pais,
                        @CodigoPostal, @RFC, @Correo, @TelefonoCasa, @TelefonoCelular, @FechaNacimiento,
                        @EstadoCivil, @RegimenFiscal, @UsoCFDI, @UsuarioRegistro)";

                    SqlParameter[] parameters = new SqlParameter[]
                    {
                        new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                        new SqlParameter("@ApellidoPaterno", txtApellidoPaterno.Text.Trim()),
                        new SqlParameter("@ApellidoMaterno", txtApellidoMaterno.Text.Trim()),
                        new SqlParameter("@Domicilio", domicilio),
                        new SqlParameter("@Ciudad", txtCiudad.Text.Trim()),
                        new SqlParameter("@Estado", txtEstado.Text.Trim()),
                        new SqlParameter("@Pais", txtPais.Text.Trim()),
                        new SqlParameter("@CodigoPostal", txtCP.Text.Trim()),
                        new SqlParameter("@RFC", txtRFC.Text.Trim().ToUpper()),
                        new SqlParameter("@Correo", txtCorreo.Text.Trim().ToLower()),
                        new SqlParameter("@TelefonoCasa", string.IsNullOrEmpty(txtTelefonoCasa.Text) ? DBNull.Value : (object)txtTelefonoCasa.Text.Trim()),
                        new SqlParameter("@TelefonoCelular", txtTelefonoCelular.Text.Trim()),
                        new SqlParameter("@FechaNacimiento", dtpFechaNacimiento.Value.Date),
                        new SqlParameter("@EstadoCivil", cboEstadoCivil.SelectedItem.ToString()),
                        new SqlParameter("@RegimenFiscal", codigoRegimenFiscal),
                        new SqlParameter("@UsoCFDI", codigoUsoCFDI),
                        new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                    };

                    Data.Database.ExecuteNonQuery(query, parameters);

                    MessageBox.Show("Cliente guardado exitosamente", "Éxito",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                limpiarCampos();
                cargarClientes();
            }

            catch (SqlException ex)
            {
                // Manejamos errores específicos de SQL
                if (ex.Number == 2627) // Error de clave duplicada
                {
                    if (ex.Message.Contains("RFC"))
                        MessageBox.Show("Ya existe un cliente con este RFC", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else if (ex.Message.Contains("Correo"))
                        MessageBox.Show("Ya existe un cliente con este correo electrónico", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                    else
                        MessageBox.Show("Ya existe un cliente con estos datos", "Error",
                                       MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show("Error de base de datos: " + ex.Message, "Error",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al procesar el cliente: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool verificarDatosUnicosEdicion()
        {
            try
            {
                // Verificamos RFC
                string queryRfc = "SELECT COUNT(*) FROM Clientes WHERE RFC = @RFC AND IdCliente <> @IdCliente";
                SqlParameter[] paramsRfc = new SqlParameter[]
                {
            new SqlParameter("@RFC", txtRFC.Text.Trim().ToUpper()),
            new SqlParameter("@IdCliente", idClienteSeleccionado)
                };

                int cuentaRfc = Convert.ToInt32(Data.Database.ExecuteScalar(queryRfc, paramsRfc));
                if (cuentaRfc > 0)
                {
                    MessageBox.Show("Ya existe otro cliente con este RFC", "Error",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtRFC.Focus();
                    return false;
                }

                // Verificamos Correo
                string queryCorreo = "SELECT COUNT(*) FROM Clientes WHERE Correo = @Correo AND IdCliente <> @IdCliente";
                SqlParameter[] paramsCorreo = new SqlParameter[]
                {
            new SqlParameter("@Correo", txtCorreo.Text.Trim().ToLower()),
            new SqlParameter("@IdCliente", idClienteSeleccionado)
                };

                int cuentaCorreo = Convert.ToInt32(Data.Database.ExecuteScalar(queryCorreo, paramsCorreo));
                if (cuentaCorreo > 0)
                {
                    MessageBox.Show("Ya existe otro cliente con este correo electrónico", "Error",
                                   MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtCorreo.Focus();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al verificar datos únicos: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        // Método para validar el formato del correo electrónico
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // Método para limpiar los campos del formulario
        private void limpiarCampos()
        {
            txtNombre.Text = "";
            txtApellidoPaterno.Text = "";
            txtApellidoMaterno.Text = "";
            txtCorreo.Text = "";
            txtRFC.Text = "";
            txtCalle.Text = "";
            txtNumero.Text = "";
            txtColonia.Text = "";
            txtCiudad.Text = "";
            txtEstado.Text = "";
            txtPais.Text = "";
            txtCP.Text = "";
            txtTelefonoCasa.Text = "";
            txtTelefonoCelular.Text = "";
            dtpFechaNacimiento.Value = DateTime.Now.AddYears(-18); // Por defecto 18 años atrás

            if (cboEstadoCivil.Items.Count > 0) cboEstadoCivil.SelectedIndex = 0;
            if (cboRegimen.Items.Count > 0) cboRegimen.SelectedIndex = 0;
            if (cboCFDI.Items.Count > 0) cboCFDI.SelectedIndex = 0;

            modoEdicion = false;
            idClienteSeleccionado = 0;
            btnGuardar.Text = "Guardar";
        }

        private void estiloDataGridView()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvClientes.BorderStyle = BorderStyle.None;
            dgvClientes.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvClientes.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            // Cambiar el color de selección a un tono de gris claro
            dgvClientes.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvClientes.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvClientes.BackgroundColor = Color.White;
            // Estilo para el encabezado
            dgvClientes.EnableHeadersVisualStyles = false;
            dgvClientes.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvClientes.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100);
            dgvClientes.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvClientes.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 10, FontStyle.Bold);
            dgvClientes.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvClientes.ColumnHeadersHeight = 40;
            // Estilo para las filas y celdas
            dgvClientes.RowTemplate.Height = 35;
            dgvClientes.DefaultCellStyle.Font = new Font("Yu Gothic", 9);
            dgvClientes.DefaultCellStyle.Padding = new Padding(5);
            dgvClientes.RowHeadersVisible = false;
            // Hacer que el control se ajuste a su contenedor
            dgvClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            // Añadir un borde fino alrededor de la tabla
            dgvClientes.BorderStyle = BorderStyle.FixedSingle;
            dgvClientes.GridColor = Color.FromArgb(220, 220, 220);
            // Configurar la selección de filas completas
            dgvClientes.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvClientes.MultiSelect = false; // Permitir seleccionar solo una fila a la vez
        }

        // Método para cargar todos los clientes en el DataGridView
        private void cargarClientes()
        {
            try
            {
                string query = @"
                SELECT 
                    IdCliente,
                    Nombre, 
                    ApellidoPaterno, 
                    ApellidoMaterno,
                    RFC,
                    Correo,
                    Ciudad,
                    Estado,
                    Pais,
                    TelefonoCelular,
                    EstadoCivil
                FROM Clientes
                ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                DataTable dtClientes = Data.Database.ExecuteQuery(query);
                dgvClientes.DataSource = dtClientes;

                // Configurar las columnas del DataGridView
                if (dgvClientes.Columns.Contains("IdCliente"))
                    dgvClientes.Columns["IdCliente"].Visible = false;

                // Aplicar formatos y ajustar anchos de columnas si es necesario
                estiloDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar clientes: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        

        // Métodos para inicializar los comboboxes
        private void inicializarComboBoxes()
        {
            configurarEstadoCivil();
            configurarRegimenFiscal();
            configurarUsoCFDI();
            configurarTipoBusqueda();
        }

        private void configurarEstadoCivil()
        {
            cboEstadoCivil.Items.Clear();
            cboEstadoCivil.Items.Add("Soltero(a)");
            cboEstadoCivil.Items.Add("Casado(a)");
            cboEstadoCivil.Items.Add("Divorciado(a)");
            cboEstadoCivil.Items.Add("Viudo(a)");
            cboEstadoCivil.Items.Add("Unión libre");
            cboEstadoCivil.DropDownStyle = ComboBoxStyle.DropDownList;
            cboEstadoCivil.SelectedIndex = 0;
        }

        private void configurarRegimenFiscal()
        {
            cboRegimen.Items.Clear();
            cboRegimen.Items.Add("605 - Sueldos y Salarios");
            cboRegimen.Items.Add("612 - Personas Físicas con Actividades Empresariales y Profesionales");
            cboRegimen.Items.Add("601 - General de Ley Personas Morales");
            cboRegimen.Items.Add("603 - Personas Morales con Fines no Lucrativos");
            cboRegimen.DropDownStyle = ComboBoxStyle.DropDownList;
            cboRegimen.SelectedIndex = 0;
        }

        private void configurarUsoCFDI()
        {
            cboCFDI.Items.Clear();
            cboCFDI.Items.Add("G03 - Gastos en general");
            cboCFDI.Items.Add("S01 - Sin efectos fiscales");
            cboCFDI.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCFDI.SelectedIndex = 0;
        }

        private void configurarTipoBusqueda()
        {
            cboTipoBusqueda.Items.Clear();
            cboTipoBusqueda.Items.Add("Apellidos");
            cboTipoBusqueda.Items.Add("RFC");
            cboTipoBusqueda.Items.Add("Correo");
            cboTipoBusqueda.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTipoBusqueda.SelectedIndex = 0;
        }

        private void btnBuscar_Click(object sender, EventArgs e)
        {
            string criterioBusqueda = txtBusqueda.Text.Trim();

            if (string.IsNullOrEmpty(criterioBusqueda))
            {
                MessageBox.Show("Ingrese un criterio de búsqueda", "Búsqueda vacía",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string query = "";
                SqlParameter[] parameters = null;

                switch (cboTipoBusqueda.SelectedIndex)
                {
                    case 0: // Búsqueda por apellidos
                        query = @"
                    SELECT 
                        IdCliente,
                        Nombre, 
                        ApellidoPaterno, 
                        ApellidoMaterno,
                        RFC,
                        Correo,
                        Ciudad,
                        Estado,
                        Pais,
                        TelefonoCelular,
                        EstadoCivil
                    FROM Clientes
                    WHERE ApellidoPaterno LIKE @Criterio OR ApellidoMaterno LIKE @Criterio
                    ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                        parameters = new SqlParameter[] {
                    new SqlParameter("@Criterio", "%" + criterioBusqueda + "%")
                };
                        break;

                    case 1: // Búsqueda por RFC
                        query = @"
                    SELECT 
                        IdCliente,
                        Nombre, 
                        ApellidoPaterno, 
                        ApellidoMaterno,
                        RFC,
                        Correo,
                        Ciudad,
                        Estado,
                        Pais,
                        TelefonoCelular,
                        EstadoCivil
                    FROM Clientes
                    WHERE RFC LIKE @Criterio
                    ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                        parameters = new SqlParameter[] {
                    new SqlParameter("@Criterio", "%" + criterioBusqueda + "%")
                };
                        break;

                    case 2: // Búsqueda por correo electrónico
                        query = @"
                    SELECT 
                        IdCliente,
                        Nombre, 
                        ApellidoPaterno, 
                        ApellidoMaterno,
                        RFC,
                        Correo,
                        Ciudad,
                        Estado,
                        Pais,
                        TelefonoCelular,
                        EstadoCivil
                    FROM Clientes
                    WHERE Correo LIKE @Criterio
                    ORDER BY ApellidoPaterno, ApellidoMaterno, Nombre";

                        parameters = new SqlParameter[] {
                    new SqlParameter("@Criterio", "%" + criterioBusqueda + "%")
                };
                        break;
                }

                DataTable dtResultados = Data.Database.ExecuteQuery(query, parameters);
                dgvClientes.DataSource = dtResultados;

                if (dtResultados.Rows.Count == 0)
                {
                    MessageBox.Show("No se encontraron clientes con ese criterio", "Sin resultados",
                                   MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                // Ocultamos la columna ID
                if (dgvClientes.Columns.Contains("IdCliente"))
                    dgvClientes.Columns["IdCliente"].Visible = false;

                estiloDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al buscar clientes: " + ex.Message, "Error",
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnLimpiar_Click(object sender, EventArgs e)
        {
            txtBusqueda.Clear();
            cargarClientes();
        }

        private int idClienteSeleccionado = 0;
        private bool modoEdicion = false;
        private void btnEditarCliente_Click(object sender, EventArgs e)
        {
            if (dgvClientes.SelectedRows.Count > 0)
            {
                idClienteSeleccionado = Convert.ToInt32(dgvClientes.SelectedRows[0].Cells["IdCliente"].Value);
                cargarDatosCliente(idClienteSeleccionado);
                modoEdicion = true;
                btnGuardar.Text = "Actualizar";

                // Si tienes un botón cancelar, podrías hacerlo visible aquí
                // btnCancelar.Visible = true;
            }
            else
            {
                MessageBox.Show("Por favor, seleccione un cliente para editar", "Cliente no seleccionado",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void cargarDatosCliente(int idCliente)
        {
            try
            {
                string query = @"
            SELECT *
            FROM Clientes
            WHERE IdCliente = @IdCliente";

                SqlParameter[] parameters = new SqlParameter[] {
            new SqlParameter("@IdCliente", idCliente)
        };

                DataTable dtCliente = Data.Database.ExecuteQuery(query, parameters);

                if (dtCliente.Rows.Count > 0)
                {
                    DataRow row = dtCliente.Rows[0];

                    txtNombre.Text = row["Nombre"].ToString();
                    txtApellidoPaterno.Text = row["ApellidoPaterno"].ToString();
                    txtApellidoMaterno.Text = row["ApellidoMaterno"].ToString();
                    txtCorreo.Text = row["Correo"].ToString();
                    txtRFC.Text = row["RFC"].ToString();

                    // Procesamos el domicilio
                    string domicilio = row["Domicilio"].ToString();
                    string[] partesDomicilio = domicilio.Split(new string[] { ", Col. " }, StringSplitOptions.None);

                    if (partesDomicilio.Length >= 1)
                    {
                        string[] calleNumero = partesDomicilio[0].Trim().Split(' ');
                        if (calleNumero.Length >= 2)
                        {
                            // Extraemos el número (último elemento)
                            txtNumero.Text = calleNumero[calleNumero.Length - 1];
                            // Reconstruimos la calle (todo excepto el último elemento)
                            txtCalle.Text = string.Join(" ", calleNumero.Take(calleNumero.Length - 1));
                        }
                        else
                        {
                            txtCalle.Text = partesDomicilio[0];
                            txtNumero.Text = "";
                        }
                    }

                    if (partesDomicilio.Length >= 2)
                    {
                        txtColonia.Text = partesDomicilio[1].Trim();
                    }

                    txtCiudad.Text = row["Ciudad"].ToString();
                    txtEstado.Text = row["Estado"].ToString();
                    txtPais.Text = row["Pais"].ToString();
                    txtCP.Text = row["CodigoPostal"].ToString();
                    txtTelefonoCasa.Text = row["TelefonoCasa"].ToString();
                    txtTelefonoCelular.Text = row["TelefonoCelular"].ToString();

                    // Fecha de nacimiento
                    if (row["FechaNacimiento"] != DBNull.Value)
                    {
                        dtpFechaNacimiento.Value = Convert.ToDateTime(row["FechaNacimiento"]);
                    }

                    // Seleccionamos el estado civil
                    string estadoCivil = row["EstadoCivil"].ToString();
                    for (int i = 0; i < cboEstadoCivil.Items.Count; i++)
                    {
                        if (cboEstadoCivil.Items[i].ToString() == estadoCivil)
                        {
                            cboEstadoCivil.SelectedIndex = i;
                            break;
                        }
                    }

                    // Seleccionamos el régimen fiscal
                    string regimenFiscal = row["RegimenFiscal"].ToString();
                    for (int i = 0; i < cboRegimen.Items.Count; i++)
                    {
                        if (cboRegimen.Items[i].ToString().StartsWith(regimenFiscal))
                        {
                            cboRegimen.SelectedIndex = i;
                            break;
                        }
                    }

                    // Seleccionamos el uso de CFDI
                    string usoCFDI = row["UsoCFDI"].ToString();
                    for (int i = 0; i < cboCFDI.Items.Count; i++)
                    {
                        if (cboCFDI.Items[i].ToString().StartsWith(usoCFDI))
                        {
                            cboCFDI.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos del cliente: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            limpiarCampos();
            modoEdicion = false;
            idClienteSeleccionado = 0;
            btnGuardar.Text = "Guardar";
        }

        private void btnReservaciones_Click(object sender, EventArgs e)
        {
            Reservaciones reservaciones = new Reservaciones();
            reservaciones.Show();
            this.Close();
        }
    }
}

