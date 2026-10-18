using proyectoHabitar.Modelos;

namespace proyectoHabitar.Servicios
{
    /// <summary>Usuario que inició sesión. Se completa con la respuesta del login y se limpia al cerrar sesión.</summary>
    public static class Sesion
    {
        public const string RolAdministrador = "Administrador";
        public const string RolLogistica = "Logística";

        public static LoginResponse? Usuario { get; private set; }

        public static string NombreCompleto =>
            Usuario == null ? "" : $"{Usuario.Nombre} {Usuario.Apellido}".Trim() is { Length: > 0 } nombre ? nombre : Usuario.Usuario;

        public static string Rol => Usuario?.Rol ?? "";

        public static bool EsAdministrador => Rol == RolAdministrador;

        public static bool PuedeVerFondos => EsAdministrador || Rol == RolLogistica;

        public static void Iniciar(LoginResponse datos)
        {
            Usuario = datos;
            ApiClient.EstablecerToken(datos.Token);
        }

        public static void Cerrar()
        {
            Usuario = null;
            ApiClient.EstablecerToken(null);
        }
    }
}
