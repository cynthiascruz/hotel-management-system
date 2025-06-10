using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using HotelManager.Classes;
using static System.Collections.Specialized.BitVector32;

namespace HotelManager.Forms
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();

            // Configuramos TextBox de contraseña para ocultar los caracteres
            txtPassword.PasswordChar = '•';
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string correo = txtCorreo.Text.Trim();
            string contrasena = txtPassword.Text;

            // Validaciones básicas
            if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(contrasena))
            {
                MessageBox.Show("Por favor, ingrese correo electrónico y contraseña", "Campos vacíos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validar formato de correo
            if (!EsCorreoValido(correo))
            {
                MessageBox.Show("Por favor, ingrese un correo electrónico válido", "Formato inválido",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Intentar realizar el login
            if (ValidarUsuario(correo, contrasena, out int idUsuario, out string tipoUsuario, out string nombreUsuario))
            {
                // Guardar información del usuario en propiedades estáticas para usarla en toda la aplicación
                Session.IdUsuario = idUsuario;
                Session.NombreUsuario = nombreUsuario;
                Session.TipoUsuario = tipoUsuario;
                Session.CorreoUsuario = correo;

                // Abrir el formulario Dashboard
                this.Hide();
                Dashboard dashboard = new Dashboard();
                dashboard.Show();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos", "Error de acceso",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void label3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void LoginWindow_Load(object sender, EventArgs e)
        {

        }

        private bool EsCorreoValido(string correo)
        {
            // Validación simple de formato de correo
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(correo, pattern);
        }

        private bool ValidarUsuario(string correo, string contrasena, out int idUsuario, out string tipoUsuario, out string nombreUsuario)
        {
            idUsuario = 0;
            tipoUsuario = string.Empty;
            nombreUsuario = string.Empty;

            try
            {
                // Consulta SQL para validar usuario
                string query = @"
                    SELECT IdUsuario, TipoUsuario, Nombre
                    FROM Usuarios
                    WHERE Correo = @Correo AND Contrasena = @Contrasena AND Estado = 1";

                SqlParameter[] parameters = new SqlParameter[]
                {
                    new SqlParameter("@Correo", correo),
                    new SqlParameter("@Contrasena", contrasena)
                };

                // Ejecutar la consulta
                DataTable result = Data.Database.ExecuteQuery(query, parameters);

                if (result.Rows.Count > 0)
                {
                    idUsuario = Convert.ToInt32(result.Rows[0]["IdUsuario"]);
                    tipoUsuario = result.Rows[0]["TipoUsuario"].ToString();
                    nombreUsuario = result.Rows[0]["Nombre"].ToString();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al validar usuario: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
