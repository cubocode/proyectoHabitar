using System;
using System.Diagnostics;
using System.Globalization;
using proyectoHabitar.Modelos;

namespace proyectoHabitar.Servicios
{
    public static class Mapas
    {
        /// <summary>Abre Google Maps en el navegador: por coordenadas si las hay, si no por dirección.</summary>
        public static void AbrirEnGoogleMaps(double? latitud, double? longitud, string direccion)
        {
            string consulta = latitud.HasValue && longitud.HasValue
                ? $"{latitud.Value.ToString(CultureInfo.InvariantCulture)},{longitud.Value.ToString(CultureInfo.InvariantCulture)}"
                : direccion;

            string url = "https://www.google.com/maps/search/?api=1&query=" + Uri.EscapeDataString(consulta);
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public static string DireccionCompleta(BeneficiarioDto b) =>
            $"{b.Domicilio}, {b.Localidad}, {(string.IsNullOrWhiteSpace(b.Provincia) ? "Tucumán" : b.Provincia)}, Argentina";
    }
}
