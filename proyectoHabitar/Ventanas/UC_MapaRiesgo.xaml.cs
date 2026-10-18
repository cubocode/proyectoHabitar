using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    /// <summary>
    /// Mapa de Riesgo: muestra las familias relevadas sobre un mapa (OpenStreetMap / satélite, estilo Google Maps)
    /// con su color de prioridad y un mapa de calor de las zonas críticas, ponderado por puntaje.
    /// El mapa es una página web (Mapa/mapa.html con Leaflet) dentro de un WebView2; se hablan por mensajes JSON.
    /// </summary>
    public partial class UC_MapaRiesgo : UserControl
    {
        private const string HostVirtual = "habitar.local";

        private MapaRiesgoDto _datos = new MapaRiesgoDto();
        private bool _mapaListo = false;
        private bool _inicializado = false;

        public UC_MapaRiesgo()
        {
            InitializeComponent();
        }

        public class ZonaCritica
        {
            public string Localidad { get; set; } = "";
            public int Total { get; set; }
            public int Rojos { get; set; }
            public double Promedio { get; set; }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_inicializado) return;
            _inicializado = true;

            await InicializarMapaAsync();
            await CargarDatosAsync();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            // Libera el proceso del navegador embebido al salir de la pantalla.
            webMapa.Dispose();
        }

        private async Task InicializarMapaAsync()
        {
            try
            {
                string carpetaDatos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HabitAR", "WebView2");
                var entorno = await CoreWebView2Environment.CreateAsync(null, carpetaDatos);
                await webMapa.EnsureCoreWebView2Async(entorno);

                // Sirve la carpeta Mapa como https://habitar.local/ (OpenStreetMap exige un origen web válido para entregar el mapa).
                string carpetaMapa = Path.Combine(AppContext.BaseDirectory, "Mapa");
                webMapa.CoreWebView2.SetVirtualHostNameToFolderMapping(HostVirtual, carpetaMapa, CoreWebView2HostResourceAccessKind.Allow);
                webMapa.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webMapa.CoreWebView2.Settings.IsStatusBarEnabled = false;
                webMapa.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                // Los links del mapa (atribuciones) se abren en el navegador del sistema.
                webMapa.CoreWebView2.NewWindowRequested += (s, args) =>
                {
                    args.Handled = true;
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args.Uri) { UseShellExecute = true });
                };

                webMapa.CoreWebView2.Navigate($"https://{HostVirtual}/mapa.html");
            }
            catch (WebView2RuntimeNotFoundException)
            {
                lblEstadoMapa.Text = "Falta el componente Microsoft Edge WebView2 Runtime.\nDescargalo de https://go.microsoft.com/fwlink/p/?LinkId=2124703 e instalalo para ver el mapa.";
            }
            catch (Exception ex)
            {
                lblEstadoMapa.Text = $"No se pudo iniciar el mapa: {ex.Message}";
            }
        }

        private async Task CargarDatosAsync(bool reencuadrar = true)
        {
            btnRecargar.IsEnabled = false;
            try
            {
                _datos = await ApiClient.GetAsync<MapaRiesgoDto>("api/Beneficiarios/mapa");
                ActualizarPanelLateral();
                EnviarDatosAlMapa(reencuadrar);
            }
            catch (ApiException ex)
            {
                lblResumen.Text = "No se pudieron cargar los relevamientos.";
                MensajeBox.Mostrar($"Error al cargar el mapa de riesgo:\n\n{ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                btnRecargar.IsEnabled = true;
            }
        }

        private void ActualizarPanelLateral()
        {
            var ubicados = _datos.Ubicados;
            int rojos = ubicados.Count(b => b.ColorSemaforo == "Rojo");
            int amarillos = ubicados.Count(b => b.ColorSemaforo == "Amarillo");
            int verdes = ubicados.Count(b => b.ColorSemaforo == "Verde");

            lblResumen.Text = $"{ubicados.Count} familias en el mapa" +
                              (_datos.SinUbicar > 0 ? $"  ·  {_datos.SinUbicar} sin ubicación (no se muestran)" : "");
            lblCantRojo.Text = $"Alta (Rojo) — {rojos}";
            lblCantAmarillo.Text = $"Media (Amarillo) — {amarillos}";
            lblCantVerde.Text = $"Baja (Verde) — {verdes}";

            btnUbicarPendientes.Visibility = _datos.SinUbicar > 0 ? Visibility.Visible : Visibility.Collapsed;
            btnUbicarPendientes.Content = $"📍 Ubicar pendientes ({_datos.SinUbicar})";

            lstZonas.ItemsSource = ubicados
                .GroupBy(b => string.IsNullOrWhiteSpace(b.Localidad) ? "(sin localidad)" : b.Localidad.Trim(), StringComparer.CurrentCultureIgnoreCase)
                .Select(g => new ZonaCritica
                {
                    Localidad = g.Key,
                    Total = g.Count(),
                    Rojos = g.Count(b => b.ColorSemaforo == "Rojo"),
                    Promedio = g.Average(b => b.PuntajeCriticidad)
                })
                .OrderByDescending(z => z.Rojos)
                .ThenByDescending(z => z.Promedio)
                .Take(8)
                .ToList();

            lstCriticos.ItemsSource = ubicados
                .OrderByDescending(b => b.PuntajeCriticidad)
                .Take(10)
                .ToList();
        }

        private void EnviarDatosAlMapa(bool reencuadrar)
        {
            if (!_mapaListo) return;

            Enviar(new
            {
                tipo = "datos",
                ubicados = _datos.Ubicados,
                puntajeMaximo = _datos.PuntajeMaximo,
                reencuadrar
            });
            EnviarFiltros();
        }

        private void EnviarFiltros()
        {
            if (!_mapaListo) return;

            var colores = new List<string>();
            if (chkRojo.IsChecked == true) colores.Add("Rojo");
            if (chkAmarillo.IsChecked == true) colores.Add("Amarillo");
            if (chkVerde.IsChecked == true) colores.Add("Verde");

            string modo = rbCalor.IsChecked == true ? "calor" : rbMarcadores.IsChecked == true ? "marcadores" : "ambos";
            Enviar(new { tipo = "filtros", colores, modo });
        }

        private void Enviar(object mensaje)
        {
            webMapa.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(mensaje, ApiClient.Json));
        }

        private void webMapa_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            using var doc = JsonDocument.Parse(e.WebMessageAsJson);
            string tipo = doc.RootElement.TryGetProperty("tipo", out var t) ? t.GetString() ?? "" : "";

            switch (tipo)
            {
                case "listo":
                    _mapaListo = true;
                    panelCargandoMapa.Visibility = Visibility.Collapsed;
                    EnviarDatosAlMapa(reencuadrar: true);
                    break;

                case "abrirFicha":
                    if (doc.RootElement.TryGetProperty("id", out var id))
                        Home.NavegarDesde(this, new UC_CargaBeneficiario(id.GetInt32()));
                    break;
            }
        }

        private void Filtros_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded) EnviarFiltros();
        }

        private void lstZonas_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstZonas.SelectedItem is ZonaCritica zona)
            {
                Enviar(new { tipo = "enfocarZona", localidad = zona.Localidad });
                lstCriticos.SelectedItem = null;
            }
        }

        private void lstCriticos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstCriticos.SelectedItem is BeneficiarioMapaDto b)
            {
                Enviar(new { tipo = "enfocar", id = b.Id });
                lstZonas.SelectedItem = null;
            }
        }

        private async void btnRecargar_Click(object sender, RoutedEventArgs e)
        {
            await CargarDatosAsync(reencuadrar: false);
        }

        private async void btnUbicarPendientes_Click(object sender, RoutedEventArgs e)
        {
            bool seguro = MensajeBox.Mostrar(
                $"Se van a buscar en el mapa las direcciones de hasta 15 relevamientos sin ubicación (quedan {_datos.SinUbicar}).\n\n" +
                "El servicio gratuito de mapas permite una consulta por segundo, así que puede tardar alrededor de medio minuto.\n\n¿Continuar?",
                "Ubicar pendientes", MensajeBox.TipoMensaje.QuerySiNo);
            if (!seguro) return;

            btnUbicarPendientes.IsEnabled = false;
            btnUbicarPendientes.Content = "⏳ Buscando direcciones...";
            try
            {
                var r = await ApiClient.PostAsync<ResultadoLoteGeocodificacion>("api/Beneficiarios/geocodificar-pendientes?maximo=15", null);

                string mensaje = $"Procesados: {r.Procesados}\n" +
                                 $"✅ Ubicados con la dirección exacta: {r.Ubicados}\n" +
                                 $"⚠️ Ubicados en el centro de la localidad (aproximado): {r.Aproximados}\n" +
                                 $"❌ No encontrados: {r.NoEncontrados}\n\n" +
                                 (r.Restantes > 0 ? $"Quedan {r.Restantes} sin ubicar. Podés volver a presionar el botón, o cargarlos a mano desde la ficha." : "¡Todo el padrón está ubicado!");
                MensajeBox.Mostrar(mensaje, "Resultado", MensajeBox.TipoMensaje.Info);
                await CargarDatosAsync(reencuadrar: true);
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar(ex.Message, "No se pudo ubicar", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                btnUbicarPendientes.IsEnabled = true;
                btnUbicarPendientes.Content = $"📍 Ubicar pendientes ({_datos.SinUbicar})";
            }
        }
    }
}
