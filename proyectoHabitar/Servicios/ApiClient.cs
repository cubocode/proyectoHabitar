using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace proyectoHabitar.Servicios
{
    /// <summary>Error de la API ya traducido a un mensaje para mostrarle al usuario.</summary>
    public class ApiException : Exception
    {
        public HttpStatusCode? Estado { get; }

        public ApiException(string mensaje, HttpStatusCode? estado = null) : base(mensaje)
        {
            Estado = estado;
        }
    }

    /// <summary>Respuesta estándar de los POST/PUT/DELETE del servidor.</summary>
    public class RespuestaApi
    {
        public int Id { get; set; }
        public string Mensaje { get; set; } = "";
        public int PuntosCalculados { get; set; }
        public string PrioridadAsignada { get; set; } = "";
    }

    /// <summary>
    /// Un único HttpClient para toda la app (antes cada pantalla creaba el suyo con la URL repetida).
    /// La URL del servidor se lee de config.json, al lado del .exe.
    /// Todas las llamadas llevan el token de la sesión y los errores llegan como ApiException con el mensaje del servidor.
    /// </summary>
    public static class ApiClient
    {
        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private static readonly HttpClient Http = new()
        {
            BaseAddress = new Uri(LeerUrlServidor()),
            Timeout = TimeSpan.FromSeconds(120)
        };

        public static void EstablecerToken(string? token)
        {
            Http.DefaultRequestHeaders.Authorization = token == null ? null : new AuthenticationHeaderValue("Bearer", token);
        }

        public static async Task<T> GetAsync<T>(string url)
        {
            using var respuesta = await EnviarAsync(new HttpRequestMessage(HttpMethod.Get, url));
            return await LeerCuerpoAsync<T>(respuesta);
        }

        public static async Task<T> PostAsync<T>(string url, object? cuerpo)
        {
            using var respuesta = await EnviarAsync(new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(cuerpo, options: Json) });
            return await LeerCuerpoAsync<T>(respuesta);
        }

        public static Task<RespuestaApi> PostAsync(string url, object? cuerpo) => PostAsync<RespuestaApi>(url, cuerpo);

        public static async Task<RespuestaApi> PutAsync(string url, object cuerpo)
        {
            using var respuesta = await EnviarAsync(new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(cuerpo, options: Json) });
            return await LeerCuerpoAsync<RespuestaApi>(respuesta);
        }

        public static async Task<RespuestaApi> DeleteAsync(string url)
        {
            using var respuesta = await EnviarAsync(new HttpRequestMessage(HttpMethod.Delete, url));
            return await LeerCuerpoAsync<RespuestaApi>(respuesta);
        }

        private static async Task<HttpResponseMessage> EnviarAsync(HttpRequestMessage pedido)
        {
            HttpResponseMessage respuesta;
            try
            {
                respuesta = await Http.SendAsync(pedido);
            }
            catch (HttpRequestException ex)
            {
                throw new ApiException($"No se pudo conectar con el servidor de HabitAR ({Http.BaseAddress}).\n\n¿Está iniciada la API?\n\nDetalle: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new ApiException("El servidor tardó demasiado en responder. Intentá de nuevo.");
            }

            if (respuesta.IsSuccessStatusCode)
                return respuesta;

            string mensaje = await LeerMensajeErrorAsync(respuesta);
            var estado = respuesta.StatusCode;
            respuesta.Dispose();
            throw new ApiException(mensaje, estado);
        }

        private static async Task<T> LeerCuerpoAsync<T>(HttpResponseMessage respuesta)
        {
            if (respuesta.Content.Headers.ContentLength == 0)
                return Activator.CreateInstance<T>();

            return await respuesta.Content.ReadFromJsonAsync<T>(Json) ?? Activator.CreateInstance<T>();
        }

        /// <summary>El servidor responde los errores como { "mensaje": "..." }; si no, se arma uno según el código.</summary>
        private static async Task<string> LeerMensajeErrorAsync(HttpResponseMessage respuesta)
        {
            string texto = await respuesta.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    using var doc = JsonDocument.Parse(texto);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        if (doc.RootElement.TryGetProperty("mensaje", out var m) && m.ValueKind == JsonValueKind.String)
                            return m.GetString()!;
                        if (doc.RootElement.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                            return t.GetString()!;
                    }
                }
                catch (JsonException)
                {
                    if (texto.Length < 300) return texto;
                }
            }

            return respuesta.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Tu sesión expiró. Cerrá sesión y volvé a ingresar.",
                HttpStatusCode.Forbidden => "Tu rol no tiene permiso para realizar esta acción.",
                HttpStatusCode.NotFound => "El recurso solicitado no existe.",
                _ => $"El servidor respondió con un error ({(int)respuesta.StatusCode})."
            };
        }

        private static string LeerUrlServidor()
        {
            const string porDefecto = "https://localhost:7000/";
            try
            {
                string ruta = Path.Combine(AppContext.BaseDirectory, "config.json");
                if (!File.Exists(ruta)) return porDefecto;

                using var doc = JsonDocument.Parse(File.ReadAllText(ruta));
                string? url = doc.RootElement.TryGetProperty("ApiUrl", out var valor) ? valor.GetString() : null;
                if (string.IsNullOrWhiteSpace(url)) return porDefecto;
                return url.EndsWith("/") ? url : url + "/";
            }
            catch
            {
                return porDefecto;
            }
        }
    }
}
