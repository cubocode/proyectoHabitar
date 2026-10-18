using System;
using System.Collections.Generic;

namespace proyectoHabitar.Modelos
{
    // Espejo de los DTOs del servidor (proyectoHabitar-server/Modelos). Antes cada pantalla
    // leía el JSON a mano con GetProperty("..."), que explotaba ante cualquier campo faltante.

    public class LoginResponse
    {
        public string Token { get; set; } = "";
        public DateTime Expira { get; set; }
        public int Id { get; set; }
        public string Usuario { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Rol { get; set; } = "";
    }

    public class UsuarioDto
    {
        public int Id { get; set; }
        public string Usuario { get; set; } = "";
        public string Email { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string DNI { get; set; } = "";
        public string Rol { get; set; } = "";
        public bool Activo { get; set; }

        public string ActivoTexto => Activo ? "🟢 Activo" : "🔴 Inactivo";
    }

    public class BeneficiarioListaDto
    {
        public int Id { get; set; }
        public DateTime? FechaRelevamiento { get; set; }
        public string Nombre { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Domicilio { get; set; } = "";
        public string Localidad { get; set; } = "";
        public int PuntajeCriticidad { get; set; }
        public string ColorSemaforo { get; set; } = "";
        public bool TieneUbicacion { get; set; }

        public string DomicilioCompleto => string.IsNullOrWhiteSpace(Localidad) ? Domicilio : $"{Domicilio} ({Localidad})";
    }

    public class BeneficiarioDto
    {
        public int Id { get; set; }
        public string Cuil { get; set; } = "";
        public string NumeroDocumento { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Nombre { get; set; } = "";
        public DateTime? FechaNacimiento { get; set; }
        public string Genero { get; set; } = "";
        public string EstadoCivil { get; set; } = "";
        public string TelefonoCelular { get; set; } = "";
        public string Email { get; set; } = "";
        public string Domicilio { get; set; } = "";
        public string Localidad { get; set; } = "";
        public string Provincia { get; set; } = "Tucumán";
        public string CodigoPostal { get; set; } = "";
        public string NivelEducativo { get; set; } = "";
        public string CondicionLaboral { get; set; } = "";
        public decimal IngresosMensuales { get; set; }
        public bool PoseePlanSocial { get; set; }
        public string OrganismoPlanSocial { get; set; } = "";
        public int CantidadConvivientes { get; set; } = 1;
        public int CantidadHijosMenores { get; set; }
        public int CantidadAdultosMayores { get; set; }
        public bool TieneIntegrantesDiscapacidad { get; set; }
        public string TipoViviendaOriginal { get; set; } = "";
        public string RegimenTenenciaTerreno { get; set; } = "";
        public string MaterialPredominantePiso { get; set; } = "";
        public string MaterialPredominanteTecho { get; set; } = "";
        public string ProcedenciaAgua { get; set; } = "";
        public string DesagueInodoro { get; set; } = "";
        public double? Latitud { get; set; }
        public double? Longitud { get; set; }
        public bool UbicacionAproximada { get; set; }

        public DateTime? FechaRelevamiento { get; set; }
        public int PuntajeCriticidad { get; set; }
        public string ColorSemaforo { get; set; } = "";
    }

    public class BeneficiarioMapaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = "";
        public string Domicilio { get; set; } = "";
        public string Localidad { get; set; } = "";
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public bool UbicacionAproximada { get; set; }
        public int PuntajeCriticidad { get; set; }
        public string ColorSemaforo { get; set; } = "";
        public int CantidadConvivientes { get; set; }
        public int CantidadHijosMenores { get; set; }
        public bool TieneIntegrantesDiscapacidad { get; set; }
    }

    public class MapaRiesgoDto
    {
        public List<BeneficiarioMapaDto> Ubicados { get; set; } = new();
        public int SinUbicar { get; set; }
        public int PuntajeMaximo { get; set; } = 150;
    }

    public class GeocodificacionDto
    {
        public double Latitud { get; set; }
        public double Longitud { get; set; }
        public string DireccionEncontrada { get; set; } = "";
        public bool Aproximada { get; set; }
    }

    public class ResultadoLoteGeocodificacion
    {
        public int Procesados { get; set; }
        public int Ubicados { get; set; }
        public int Aproximados { get; set; }
        public int NoEncontrados { get; set; }
        public int Restantes { get; set; }
    }

    public class MovimientoDto
    {
        public int Id { get; set; }
        public DateTime FechaHora { get; set; }
        public string Tipo { get; set; } = "";
        public string Categoria { get; set; } = "";
        public string Concepto { get; set; } = "";
        public string Detalle { get; set; } = "";
        public decimal Monto { get; set; }
        public string MedioPago { get; set; } = "";
        public string Comprobante { get; set; } = "";
        public string UsuarioNombre { get; set; } = "";
        public DateTime? FechaRegistro { get; set; }
        public bool Anulado { get; set; }
        public string MotivoAnulacion { get; set; } = "";
        public DateTime? FechaAnulacion { get; set; }
        public string UsuarioAnulacion { get; set; } = "";

        public bool EsIngreso => Tipo == "Ingreso";
        public decimal MontoConSigno => EsIngreso ? Monto : -Monto;
        public string Estado => Anulado ? "ANULADO" : "Vigente";
    }

    public class TotalCategoriaDto
    {
        public string Tipo { get; set; } = "";
        public string Categoria { get; set; } = "";
        public decimal Total { get; set; }
        public int Cantidad { get; set; }
    }

    public class ResumenFondosDto
    {
        public decimal SaldoAnterior { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEgresos { get; set; }
        public decimal SaldoPeriodo { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal SaldoActual { get; set; }
        public int CantidadMovimientos { get; set; }
        public List<TotalCategoriaDto> PorCategoria { get; set; } = new();
    }
}
