using System;

namespace HotelManager.Classes
{
    public static class Session
    {
        public static int IdUsuario { get; set; }
        public static string NombreUsuario { get; set; }
        public static string TipoUsuario { get; set; }
        public static string CorreoUsuario { get; set; }

        // Método para verificar si el usuario es administrador
        public static bool EsAdministrador()
        {
            return TipoUsuario.Equals("Administrador", StringComparison.OrdinalIgnoreCase);
        }

        // Método para verificar si el usuario es operativo
        public static bool EsOperativo()
        {
            return TipoUsuario.Equals("Operativo", StringComparison.OrdinalIgnoreCase);
        }

        // Método para limpiar los datos de sesión al cerrar sesión
        public static void CerrarSesion()
        {
            IdUsuario = 0;
            NombreUsuario = string.Empty;
            TipoUsuario = string.Empty;
            CorreoUsuario = string.Empty;
        }
    }
}