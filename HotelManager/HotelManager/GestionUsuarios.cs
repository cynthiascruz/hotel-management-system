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

namespace HotelManager
{
    public partial class GestionUsuarios : Form
    {
        public GestionUsuarios()
        {
            InitializeComponent();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // Este evento no necesita implementación si no se usa, pero si lo quito pos gg
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            // Este evento no necesita implementación si no se usa, pero si lo quito pos gg
        }

        private void GestionUsuarios_Load(object sender, EventArgs e)
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
            cargarComboBoxes();
            cargarUsuarios();
        }

        private void btnGestionUsuarios_Click(object sender, EventArgs e)
        {
            // Este evento no necesita implementación si no se usa, pero si lo quito pos gg
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }

        private void cargarComboBoxes()
        {
            try
            {
                // Llenamos el ComboBox de Tipo de Usuario
                cboTipo.Items.Clear();
                cboTipo.Items.Add("Administrador");
                cboTipo.Items.Add("Operativo");
                cboTipo.SelectedIndex = 1; // Por defecto seleccionar "Operativo"

                // Llenamos el ComboBox de Estado
                cboEstado.Items.Clear();
                cboEstado.Items.Add("Activo");
                cboEstado.Items.Add("Inactivo");
                cboEstado.SelectedIndex = 0; // Por defecto seleccionar "Activo"
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar las listas: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void cargarUsuarios()
        {
            try
            {
                // Consulta actualizada para mostrar información relevante y formato de fecha
                string query = @"
                SELECT 
                    IdUsuario, 
                    Nombre, 
                    Correo, 
                    TipoUsuario, 
                    NumeroNomina, 
                    FORMAT(FechaRegistro, 'dd-MMM-yy') AS FechaRegistro, 
                    CASE WHEN Estado = 1 THEN 'Activo' ELSE 'Inactivo' END AS Estado,
                    TelefonoCelular
                FROM Usuarios
                ORDER BY Nombre";

                DataTable dtUsuarios = Data.Database.ExecuteQuery(query);

                // Asignar los datos al DataGridView
                dgvUsuarios.DataSource = dtUsuarios;

                // Ocultar la columna ID para una mejor interfaz
                if (dgvUsuarios.Columns.Contains("IdUsuario"))
                    dgvUsuarios.Columns["IdUsuario"].Visible = false;

                // Aplicar estilos
                estiloDataGridView();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar usuarios: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private int idUsuarioEdicion = 0;
        private void btnGuardar_Click(object sender, EventArgs e)
        {
            // Validaciones de campos requeridos
            if (string.IsNullOrEmpty(txtCorreo.Text) ||
                string.IsNullOrEmpty(txtNombre.Text) ||
                string.IsNullOrEmpty(txtNomina.Text))
            {
                MessageBox.Show("Por favor, complete todos los campos obligatorios",
                                "Campos vacíos",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Validamos formato de correo electrónico
            if (!EsCorreoValido(txtCorreo.Text.Trim()))
            {
                MessageBox.Show("Por favor, ingrese un correo electrónico válido",
                                "Formato inválido",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                txtCorreo.Focus();
                return;
            }

            try
            {
                if (idUsuarioEdicion == 0) // Nuevo usuario
                {
                    // Validamos contraseña segura para nuevos usuarios
                    if (string.IsNullOrEmpty(txtContrasena.Text) || !EsContrasenaValida(txtContrasena.Text))
                    {
                        MessageBox.Show("La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un carácter especial",
                                       "Contraseña inválida",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Warning);
                        txtContrasena.Focus();
                        return;
                    }

                    // Verificamos si el correo ya existe
                    string queryVerificar = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo";
                    SqlParameter[] paramsVerificar = new SqlParameter[]
                    {
                        new SqlParameter("@Correo", txtCorreo.Text.Trim())
                    };

                    int existe = Convert.ToInt32(Data.Database.ExecuteScalar(queryVerificar, paramsVerificar));
                    if (existe > 0)
                    {
                        MessageBox.Show("El correo electrónico ya está registrado",
                                        "Correo duplicado",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                        txtCorreo.Focus();
                        return;
                    }

                    // Insertamos nuevo usuario
                    string query = @"
                    INSERT INTO Usuarios (
                        Correo, 
                        Contrasena, 
                        Nombre, 
                        NumeroNomina, 
                        FechaNacimiento, 
                        TelefonoCasa, 
                        TelefonoCelular, 
                        TipoUsuario, 
                        Estado, 
                        UsuarioRegistro, 
                        UltimaContrasena1, 
                        UltimaContrasena2
                    )
                    VALUES (
                        @Correo, 
                        @Contrasena, 
                        @Nombre, 
                        @NumeroNomina, 
                        @FechaNacimiento, 
                        @TelefonoCasa, 
                        @TelefonoCelular, 
                        @TipoUsuario, 
                        @Estado, 
                        @UsuarioRegistro,
                        NULL, 
                        NULL
                    )";

                    SqlParameter[] parameters = new SqlParameter[]
                    {
                        new SqlParameter("@Correo", txtCorreo.Text.Trim()),
                        new SqlParameter("@Contrasena", txtContrasena.Text),
                        new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                        new SqlParameter("@NumeroNomina", txtNomina.Text.Trim()),
                        new SqlParameter("@FechaNacimiento", dtpFechaNacimiento.Value.Date),
                        new SqlParameter("@TelefonoCasa", txtTelefonoCasa.Text.Trim()),
                        new SqlParameter("@TelefonoCelular", txtTelefonoCelular.Text.Trim()),
                        new SqlParameter("@TipoUsuario", cboTipo.SelectedItem.ToString()),
                        new SqlParameter("@Estado", cboEstado.SelectedIndex == 0 ? 1 : 0),
                        new SqlParameter("@UsuarioRegistro", Session.IdUsuario)
                    };

                    int result = Data.Database.ExecuteNonQuery(query, parameters);
                    if (result > 0)
                    {
                        MessageBox.Show("Usuario creado exitosamente",
                                        "Éxito",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                        limpiarCampos();
                        cargarUsuarios();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo crear el usuario",
                                        "Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                }
                else // Editar un usuario existente
                {
                    // Si se proporcionó una nueva contraseña, validarla
                    if (!string.IsNullOrEmpty(txtContrasena.Text) && !EsContrasenaValida(txtContrasena.Text))
                    {
                        MessageBox.Show("La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula y un carácter especial",
                                       "Contraseña inválida",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Warning);
                        txtContrasena.Focus();
                        return;
                    }

                    // Verificamos que la nueva contraseña no sea igual a las anteriores
                    if (!string.IsNullOrEmpty(txtContrasena.Text))
                    {
                        string queryVerificarContrasenas = @"
                        SELECT Contrasena, UltimaContrasena1, UltimaContrasena2 
                        FROM Usuarios 
                        WHERE IdUsuario = @IdUsuario";

                        SqlParameter[] paramsVerificarContrasenas = new SqlParameter[]
                        {
                            new SqlParameter("@IdUsuario", idUsuarioEdicion)
                        };

                        DataTable dtContrasenasPrevias = Data.Database.ExecuteQuery(queryVerificarContrasenas, paramsVerificarContrasenas);
                        if (dtContrasenasPrevias.Rows.Count > 0)
                        {
                            string contrasenaActual = dtContrasenasPrevias.Rows[0]["Contrasena"].ToString();
                            string ultimaContrasena1 = dtContrasenasPrevias.Rows[0]["UltimaContrasena1"] != DBNull.Value
                                ? dtContrasenasPrevias.Rows[0]["UltimaContrasena1"].ToString()
                                : "";
                            string ultimaContrasena2 = dtContrasenasPrevias.Rows[0]["UltimaContrasena2"] != DBNull.Value
                                ? dtContrasenasPrevias.Rows[0]["UltimaContrasena2"].ToString()
                                : "";

                            // Verificamos si la nueva contraseña es igual a alguna de las anteriores
                            if (txtContrasena.Text == contrasenaActual ||
                                txtContrasena.Text == ultimaContrasena1 ||
                                txtContrasena.Text == ultimaContrasena2)
                            {
                                MessageBox.Show("La nueva contraseña no puede ser igual a ninguna de las últimas dos contraseñas",
                                              "Contraseña inválida",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Warning);
                                txtContrasena.Focus();
                                return;
                            }
                        }
                    }

                    // Verificamos que el correo no esté en uso por otro usuario
                    string queryVerificar = "SELECT COUNT(*) FROM Usuarios WHERE Correo = @Correo AND IdUsuario <> @IdUsuario";
                    SqlParameter[] paramsVerificar = new SqlParameter[]
                    {
                        new SqlParameter("@Correo", txtCorreo.Text.Trim()),
                        new SqlParameter("@IdUsuario", idUsuarioEdicion)
                    };

                    int existe = Convert.ToInt32(Data.Database.ExecuteScalar(queryVerificar, paramsVerificar));
                    if (existe > 0)
                    {
                        MessageBox.Show("El correo electrónico ya está registrado por otro usuario",
                                        "Correo duplicado",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                        txtCorreo.Focus();
                        return;
                    }

                    // Actualizamos el usuario
                    string query;
                    SqlParameter[] parameters;

                    if (!string.IsNullOrEmpty(txtContrasena.Text))
                    {
                        // Primero, obtenemos la contraseña actual y la última contraseña del usuario
                        string queryObtenerContrasenas = @"
                        SELECT Contrasena, UltimaContrasena1 
                        FROM Usuarios 
                        WHERE IdUsuario = @IdUsuario";

                        SqlParameter[] paramsObtenerContrasenas = new SqlParameter[]
                        {
                            new SqlParameter("@IdUsuario", idUsuarioEdicion)
                        };

                        DataTable dtContrasenas = Data.Database.ExecuteQuery(queryObtenerContrasenas, paramsObtenerContrasenas);

                        string contrasenaActual = "";
                        string ultimaContrasena1 = "";

                        if (dtContrasenas.Rows.Count > 0)
                        {
                            contrasenaActual = dtContrasenas.Rows[0]["Contrasena"].ToString();
                            ultimaContrasena1 = dtContrasenas.Rows[0]["UltimaContrasena1"] != DBNull.Value
                                ? dtContrasenas.Rows[0]["UltimaContrasena1"].ToString()
                                : "";
                        }

                        // Actualizamos incluyendo la contraseña e historial
                        query = @"
                        UPDATE Usuarios 
                        SET Correo = @Correo, 
                            Contrasena = @Contrasena, 
                            UltimaContrasena1 = @UltimaContrasena1,
                            UltimaContrasena2 = @UltimaContrasena2,
                            Nombre = @Nombre, 
                            NumeroNomina = @NumeroNomina, 
                            FechaNacimiento = @FechaNacimiento, 
                            TelefonoCasa = @TelefonoCasa, 
                            TelefonoCelular = @TelefonoCelular, 
                            TipoUsuario = @TipoUsuario, 
                            Estado = @Estado,
                            FechaModificacion = @FechaModificacion,
                            UsuarioModificacion = @UsuarioModificacion
                        WHERE IdUsuario = @IdUsuario";

                        parameters = new SqlParameter[]
                        {
                            new SqlParameter("@IdUsuario", idUsuarioEdicion),
                            new SqlParameter("@Correo", txtCorreo.Text.Trim()),
                            new SqlParameter("@Contrasena", txtContrasena.Text),
                            new SqlParameter("@UltimaContrasena1", contrasenaActual),
                            new SqlParameter("@UltimaContrasena2", ultimaContrasena1),
                            new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                            new SqlParameter("@NumeroNomina", txtNomina.Text.Trim()),
                            new SqlParameter("@FechaNacimiento", dtpFechaNacimiento.Value.Date),
                            new SqlParameter("@TelefonoCasa", txtTelefonoCasa.Text.Trim()),
                            new SqlParameter("@TelefonoCelular", txtTelefonoCelular.Text.Trim()),
                            new SqlParameter("@TipoUsuario", cboTipo.SelectedItem.ToString()),
                            new SqlParameter("@Estado", cboEstado.SelectedIndex == 0 ? 1 : 0),
                            new SqlParameter("@FechaModificacion", DateTime.Now),
                            new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                        };
                    }
                    else
                    {
                        // Actualizamos sin cambiar la contraseña
                        query = @"
                        UPDATE Usuarios 
                        SET Correo = @Correo, 
                            Nombre = @Nombre, 
                            NumeroNomina = @NumeroNomina, 
                            FechaNacimiento = @FechaNacimiento, 
                            TelefonoCasa = @TelefonoCasa, 
                            TelefonoCelular = @TelefonoCelular, 
                            TipoUsuario = @TipoUsuario, 
                            Estado = @Estado,
                            FechaModificacion = @FechaModificacion,
                            UsuarioModificacion = @UsuarioModificacion
                        WHERE IdUsuario = @IdUsuario";

                        parameters = new SqlParameter[]
                        {
                            new SqlParameter("@IdUsuario", idUsuarioEdicion),
                            new SqlParameter("@Correo", txtCorreo.Text.Trim()),
                            new SqlParameter("@Nombre", txtNombre.Text.Trim()),
                            new SqlParameter("@NumeroNomina", txtNomina.Text.Trim()),
                            new SqlParameter("@FechaNacimiento", dtpFechaNacimiento.Value.Date),
                            new SqlParameter("@TelefonoCasa", txtTelefonoCasa.Text.Trim()),
                            new SqlParameter("@TelefonoCelular", txtTelefonoCelular.Text.Trim()),
                            new SqlParameter("@TipoUsuario", cboTipo.SelectedItem.ToString()),
                            new SqlParameter("@Estado", cboEstado.SelectedIndex == 0 ? 1 : 0),
                            new SqlParameter("@FechaModificacion", DateTime.Now),
                            new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                        };
                    }

                    int result = Data.Database.ExecuteNonQuery(query, parameters);
                    if (result > 0)
                    {
                        MessageBox.Show("Usuario actualizado exitosamente",
                                        "Éxito",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);
                        limpiarCampos();
                        idUsuarioEdicion = 0; // Reseteamos ID de edición
                        btnGuardar.Text = "Guardar"; // Restauramos el texto del botón
                        cargarUsuarios();
                    }
                    else
                    {
                        MessageBox.Show("No se pudo actualizar el usuario",
                                        "Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar usuario: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        // Método para validar formato de correo
        private bool EsCorreoValido(string correo)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, pattern);
        }

        // Método para validar contraseñas
        private bool EsContrasenaValida(string contrasena)
        {
            // Al menos 8 caracteres, una mayúscula, una minúscula y un carácter especial
            return contrasena.Length >= 8 &&
                   Regex.IsMatch(contrasena, "[A-Z]") &&
                   Regex.IsMatch(contrasena, "[a-z]") &&
                   Regex.IsMatch(contrasena, "[!@#$%^&*(),.?\":{}|<>]");
        }

        // Método para limpiar los campos del formulario
        private void limpiarCampos()
        {
            txtCorreo.Text = "";
            txtContrasena.Text = "";
            txtNombre.Text = "";
            txtNomina.Text = "";
            dtpFechaNacimiento.Value = DateTime.Now.AddYears(-25); // 25 años atrás como valor predeterminado
            txtTelefonoCasa.Text = "";
            txtTelefonoCelular.Text = "";
            cboTipo.SelectedIndex = 1; // "Operativo" por defecto
            cboEstado.SelectedIndex = 0; // "Activo" por defecto
            idUsuarioEdicion = 0; // Reseteamos el ID de edición
            btnGuardar.Text = "Guardar"; // Restauramos el texto del botón
        }

        private void estiloDataGridView()
        {
            // Establecer las propiedades básicas del DataGridView
            dgvUsuarios.BorderStyle = BorderStyle.None;
            dgvUsuarios.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 248);
            dgvUsuarios.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

            // Cambiar el color de selección a un tono de gris claro
            dgvUsuarios.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 230, 230);
            dgvUsuarios.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvUsuarios.BackgroundColor = Color.White;

            // Estilo para el encabezado
            dgvUsuarios.EnableHeadersVisualStyles = false;
            dgvUsuarios.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(194, 89, 100);
            dgvUsuarios.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsuarios.ColumnHeadersDefaultCellStyle.Font = new Font("Yu Gothic", 10, FontStyle.Bold);
            dgvUsuarios.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvUsuarios.ColumnHeadersHeight = 40;

            // Estilo para las filas y celdas
            dgvUsuarios.RowTemplate.Height = 35;
            dgvUsuarios.DefaultCellStyle.Font = new Font("Yu Gothic", 9);
            dgvUsuarios.DefaultCellStyle.Padding = new Padding(5);
            dgvUsuarios.RowHeadersVisible = false;

            // Hacer que el control se ajuste a su contenedor
            dgvUsuarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Añadir un borde fino alrededor de la tabla
            dgvUsuarios.BorderStyle = BorderStyle.FixedSingle;
            dgvUsuarios.GridColor = Color.FromArgb(220, 220, 220);

            // Configurar la selección de filas completas
            dgvUsuarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsuarios.MultiSelect = false; // Permitir seleccionar solo una fila a la vez
        }

        private void btnEditarUsuario_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar si hay alguna celda seleccionada
                if (dgvUsuarios.CurrentRow == null)
                {
                    MessageBox.Show("Por favor, seleccione un usuario para editar",
                                    "Selección requerida",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                    return;
                }

                // Obtenemos el ID del usuario de la fila actual
                idUsuarioEdicion = Convert.ToInt32(dgvUsuarios.CurrentRow.Cells["IdUsuario"].Value);

                // Cargamos los datos del usuario para edición
                string query = @"
                SELECT Correo, Contrasena, Nombre, NumeroNomina, FechaNacimiento, 
                       TelefonoCasa, TelefonoCelular, TipoUsuario, Estado
                FROM Usuarios
                WHERE IdUsuario = @IdUsuario";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdUsuario", idUsuarioEdicion)
                };

                DataTable dtUsuario = Data.Database.ExecuteQuery(query, parameters);
                if (dtUsuario.Rows.Count > 0)
                {
                    // Llenamos los campos con los datos del usuario
                    txtCorreo.Text = dtUsuario.Rows[0]["Correo"].ToString();
                    // No mostramos la contraseña por seguridad
                    txtContrasena.Text = "";
                    txtNombre.Text = dtUsuario.Rows[0]["Nombre"].ToString();
                    txtNomina.Text = dtUsuario.Rows[0]["NumeroNomina"].ToString();

                    if (dtUsuario.Rows[0]["FechaNacimiento"] != DBNull.Value)
                        dtpFechaNacimiento.Value = Convert.ToDateTime(dtUsuario.Rows[0]["FechaNacimiento"]);

                    txtTelefonoCasa.Text = dtUsuario.Rows[0]["TelefonoCasa"].ToString();
                    txtTelefonoCelular.Text = dtUsuario.Rows[0]["TelefonoCelular"].ToString();

                    // Seleccionamos el tipo de usuario
                    string tipoUsuario = dtUsuario.Rows[0]["TipoUsuario"].ToString();
                    for (int i = 0; i < cboTipo.Items.Count; i++)
                    {
                        if (cboTipo.Items[i].ToString() == tipoUsuario)
                        {
                            cboTipo.SelectedIndex = i;
                            break;
                        }
                    }

                    // Seleccionamos el estado
                    bool estado = Convert.ToBoolean(dtUsuario.Rows[0]["Estado"]);
                    cboEstado.SelectedIndex = estado ? 0 : 1; // 0 = Activo, 1 = Inactivo

                    // Cambiamos el texto del botón para indicar modo edición
                    btnGuardar.Text = "Actualizar";
                }
                else
                {
                    MessageBox.Show("No se encontró el usuario seleccionado",
                                    "Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                    idUsuarioEdicion = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar datos del usuario: " + ex.Message,
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                idUsuarioEdicion = 0;
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            // Preguntamos al usuario si está seguro de cancelar la operación
            DialogResult result = MessageBox.Show("¿Está seguro que desea cancelar? Se perderán los cambios no guardados.",
                                                 "Confirmar cancelación",
                                                 MessageBoxButtons.YesNo,
                                                 MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                limpiarCampos();
            }
        }

        private void btnActivarDesactivar_Click(object sender, EventArgs e)
        {
            // Verificamos si el usuario actual es administrador
            if (Session.TipoUsuario != "Administrador")
            {
                MessageBox.Show("Solo los administradores pueden activar/desactivar usuarios",
                               "Acceso denegado",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning);
                return;
            }

            // Verificamos si hay alguna fila seleccionada
            if (dgvUsuarios.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione un usuario para activar/desactivar",
                                "Selección requerida",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Obtenemos datos del usuario seleccionado
            int idUsuario = Convert.ToInt32(dgvUsuarios.SelectedRows[0].Cells["IdUsuario"].Value);
            string estadoActual = dgvUsuarios.SelectedRows[0].Cells["Estado"].Value.ToString();
            string nombre = dgvUsuarios.SelectedRows[0].Cells["Nombre"].Value.ToString();

            // Verificamos que no esté intentando desactivar su propio usuario
            if (idUsuario == Session.IdUsuario)
            {
                MessageBox.Show("No puedes desactivar tu propio usuario",
                                "Operación no permitida",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                return;
            }

            // Calcular el nuevo estado
            bool nuevoEstado = estadoActual.ToLower() == "inactivo"; // Si está inactivo, lo activamos

            // Confirmar con el usuario
            string mensaje = nuevoEstado ?
                             $"¿Desea activar al usuario {nombre}?" :
                             $"¿Desea desactivar al usuario {nombre}?";

            if (MessageBox.Show(mensaje, "Confirmar",
                               MessageBoxButtons.YesNo,
                               MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    // Actualizamos el estado en la base de datos
                    string query = @"
                        UPDATE Usuarios 
                        SET Estado = @Estado,
                            FechaModificacion = @FechaModificacion,
                            UsuarioModificacion = @UsuarioModificacion 
                        WHERE IdUsuario = @IdUsuario";

                    SqlParameter[] parameters = new SqlParameter[]
                    {
                        new SqlParameter("@IdUsuario", idUsuario),
                        new SqlParameter("@Estado", nuevoEstado ? 1 : 0), // 1 = Activo, 0 = Inactivo
                        new SqlParameter("@FechaModificacion", DateTime.Now),
                        new SqlParameter("@UsuarioModificacion", Session.IdUsuario)
                    };

                    int result = Data.Database.ExecuteNonQuery(query, parameters);
                    if (result > 0)
                    {
                        string accion = nuevoEstado ? "activado" : "desactivado";
                        MessageBox.Show($"Usuario {accion} exitosamente", "Éxito",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        cargarUsuarios(); // Recargar la lista
                    }
                    else
                    {
                        MessageBox.Show("No se pudo cambiar el estado del usuario", "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cambiar estado: " + ex.Message, "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnGestionHoteles_Click(object sender, EventArgs e)
        {
            GestionHoteles gestionHoteles = new GestionHoteles();
            gestionHoteles.Show();
            this.Close();
        }

        private void btnReservaciones_Click(object sender, EventArgs e)
        {
            Reservaciones reservaciones = new Reservaciones();
            reservaciones.Show();
            this.Close();
        }


    }
}
