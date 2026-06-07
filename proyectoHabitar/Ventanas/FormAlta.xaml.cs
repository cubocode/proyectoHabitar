using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization; // 🌟 REQUERIDO: Para mapear alias de JSON a C#
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace proyectoHabitar.Ventanas
{
    public partial class FormAlta : UserControl
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };
        private List<BeneficiarioGridDTO> _listaCompleta = new List<BeneficiarioGridDTO>();
        private string _filtroColorActual = "Todos";

        public FormAlta()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarPadronDesdeAPI();
        }

        private async void CargarPadronDesdeAPI()
        {
            try
            {
                HttpResponseMessage respuesta = await _client.GetAsync("api/Beneficiarios");
                if (respuesta.IsSuccessStatusCode)
                {
                    string json = await respuesta.Content.ReadAsStringAsync();

                    // 🌟 SOLUCIÓN DEFINITIVA: Deserializador tipado acorazado
                    _listaCompleta = JsonSerializer.Deserialize<List<BeneficiarioGridDTO>>(json);

                    System.Windows.Application.Current.Dispatcher.Invoke(() => {
                        AplicarFiltrosCombinados();
                    });
                }
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"Error al sincronizar el padrón:\n\n{ex.Message}", "Error de Servidor", MensajeBox.TipoMensaje.Error);
            }
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            AplicarFiltrosCombinados();
        }

        private void FiltroColor_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            btnFiltroTodos.Background = Brushes.White; btnFiltroTodos.Foreground = (Brush)new BrushConverter().ConvertFromString("#334155");
            btnFiltroRojo.Background = Brushes.White; btnFiltroRojo.Foreground = (Brush)new BrushConverter().ConvertFromString("#EF4444");
            btnFiltroAmarillo.Background = Brushes.White; btnFiltroAmarillo.Foreground = (Brush)new BrushConverter().ConvertFromString("#D97706");
            btnFiltroVerde.Background = Brushes.White; btnFiltroVerde.Foreground = (Brush)new BrushConverter().ConvertFromString("#10B981");

            if (btn.Name == "btnFiltroTodos") { _filtroColorActual = "Todos"; btn.Background = (Brush)new BrushConverter().ConvertFromString("#334155"); btn.Foreground = Brushes.White; }
            else if (btn.Name == "btnFiltroRojo") { _filtroColorActual = "Rojo"; btn.Background = (Brush)new BrushConverter().ConvertFromString("#FEE2E2"); }
            else if (btn.Name == "btnFiltroAmarillo") { _filtroColorActual = "Amarillo"; btn.Background = (Brush)new BrushConverter().ConvertFromString("#FEF3C7"); }
            else if (btn.Name == "btnFiltroVerde") { _filtroColorActual = "Verde"; btn.Background = (Brush)new BrushConverter().ConvertFromString("#D1FAE5"); }

            AplicarFiltrosCombinados();
        }

        private void AplicarFiltrosCombinados()
        {
            string texto = txtBuscar.Text.Trim().ToLower();
            var resultado = _listaCompleta.AsEnumerable();

            if (_filtroColorActual != "Todos")
            {
                resultado = resultado.Where(b => b.ColorSemaforo == _filtroColorActual);
            }

            if (!string.IsNullOrEmpty(texto))
            {
                resultado = resultado.Where(b => b.Nombre.ToLower().Contains(texto) || b.Dni.Contains(texto));
            }

            // Ordenamiento alfabético ascendente (A-Z) directo sobre la clase tipada
            dgBeneficiarios.ItemsSource = resultado.OrderBy(b => b.Nombre).ToList();
        }

        private void NavegarAFormularioDeCarga(int idBeneficiario)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(this);
            while (parent != null && !(parent is Home))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            if (parent is Home ventanaPrincipal)
            {
                ventanaPrincipal.PanelCentral.Content = new UC_CargaBeneficiario(idBeneficiario);
            }
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            NavegarAFormularioDeCarga(0);
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is BeneficiarioGridDTO seleccionado)
            {
                NavegarAFormularioDeCarga(seleccionado.Id);
            }
            else
            {
                MensajeBox.Mostrar("Por favor, seleccione un beneficiario de la grilla para editar.", "Selección Requerida", MensajeBox.TipoMensaje.Warning);
            }
        }

        // Doble click: Abre el modal sin excepciones de casteo de tipos
        private async void dgBeneficiarios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is BeneficiarioGridDTO seleccionado)
            {
                try
                {
                    HttpResponseMessage respuesta = await _client.GetAsync($"api/Beneficiarios/{seleccionado.Id}");

                    if (respuesta.IsSuccessStatusCode)
                    {
                        string json = await respuesta.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(json))
                        {
                            var r = doc.RootElement;

                            lblModalTitulo.Text = $"Ficha de Emergencia: {r.GetProperty("apellido").GetString()}, {r.GetProperty("nombre").GetString()}";

                            // 🌟 EVITA CRASH: Al desglosar el JSON plano con minúsculas del endpoint por ID, leemos de forma segura
                            int puntos = r.GetProperty("puntajeCriticidad").GetInt32();
                            string prioridadString = r.GetProperty("colorSemaforo").GetString();
                            lblModalSemaforo.Text = $"Prioridad Asignada: {prioridadString.ToUpper()} ({puntos} Puntos de Criticidad)";

                            if (prioridadString == "Rojo") lblModalSemaforo.Foreground = Brushes.Red;
                            else if (prioridadString == "Amarillo") lblModalSemaforo.Foreground = Brushes.Orange;
                            else lblModalSemaforo.Foreground = Brushes.Green;

                            txtModalDocumentos.Text = $"• Nro de CUIL: {r.GetProperty("cuil").GetString()}\n" +
                                                      $"• Documento: DNI N° {r.GetProperty("numeroDocumento").GetString()}\n" +
                                                      $"• Identidad de Género: {r.GetProperty("genero").GetString()}";

                            txtModalContacto.Text = $"• Celular: +54 9 {r.GetProperty("telefonoCelular").GetString()}\n" +
                                                    $"• Correo Electrónico: {r.GetProperty("email").GetString()}\n" +
                                                    $"• Dirección: {r.GetProperty("domicilio").GetString()} ({r.GetProperty("localidad").GetString()}, CP {r.GetProperty("codigoPostal").GetString()})";

                            txtModalVivienda.Text = $"• Vivienda Original: {r.GetProperty("tipoViviendaOriginal").GetString()}\n" +
                                                    $"• Provisión de Agua: {r.GetProperty("procedenciaAgua").GetString()}\n" +
                                                    $"• Desagüe Sanitario: {r.GetProperty("desagueInodoro").GetString()}";

                            txtModalDireccionMapa.Text = $"{r.GetProperty("domicilio").GetString()}, {r.GetProperty("localidad").GetString()}, Tucumán";

                            ModalDetalle.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MensajeBox.Mostrar($"Error al abrir la ficha: {ex.Message}", "Error de Comunicación", MensajeBox.TipoMensaje.Error);
                }
            }
        }

        // Descargar PDF Nativo en la PC local
        private void btnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialogoGuardar = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = $"Ficha_Social_{lblModalTitulo.Text.Replace("Ficha de Emergencia: ", "")}",
                    DefaultExt = ".pdf",
                    Filter = "Documentos PDF (.pdf)|*.pdf"
                };

                if (dialogoGuardar.ShowDialog() == true)
                {
                    PrintDialog pd = new PrintDialog();
                    btnDescargarPdf.Visibility = Visibility.Collapsed;
                    btnImprimirFicha.Visibility = Visibility.Collapsed;

                    pd.PrintVisual(ModalDetalle, "Exportando PDF HabitAR...");

                    btnDescargarPdf.Visibility = Visibility.Visible;
                    btnImprimirFicha.Visibility = Visibility.Visible;
                    MensajeBox.Mostrar("La ficha social fue exportada a PDF de forma exitosa.", "Exportación Completada", MensajeBox.TipoMensaje.Info);
                }
            }
            catch (Exception ex)
            {
                btnDescargarPdf.Visibility = Visibility.Visible;
                btnImprimirFicha.Visibility = Visibility.Visible;
                MensajeBox.Mostrar($"Error al descargar: {ex.Message}", "Error de Guardado", MensajeBox.TipoMensaje.Error);
            }
        }

        // Impresión nativa por PrintVisual
        private void btnImprimirFicha_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                PrintDialog cuadroImpresion = new PrintDialog();

                if (cuadroImpresion.ShowDialog() == true)
                {
                    btnDescargarPdf.Visibility = Visibility.Collapsed;
                    btnImprimirFicha.Visibility = Visibility.Collapsed;

                    Transform transformacionOriginal = ModalDetalle.LayoutTransform;
                    double margen = 40;
                    double escala = Math.Min(
                        (cuadroImpresion.PrintableAreaWidth - margen) / 740,
                        (cuadroImpresion.PrintableAreaHeight - margen) / 490
                    );

                    ModalDetalle.LayoutTransform = new ScaleTransform(escala, escala);

                    Size tamañoPagina = new Size(cuadroImpresion.PrintableAreaWidth, cuadroImpresion.PrintableAreaHeight);
                    ModalDetalle.Measure(tamañoPagina);
                    ModalDetalle.Arrange(new Rect(new Point(0, 0), tamañoPagina));

                    cuadroImpresion.PrintVisual(ModalDetalle, $"Ficha HabitAR - {lblModalTitulo.Text}");

                    ModalDetalle.LayoutTransform = transformacionOriginal;
                    btnDescargarPdf.Visibility = Visibility.Visible;
                    btnImprimirFicha.Visibility = Visibility.Visible;
                    ModalDetalle.InvalidateVisual();

                    MensajeBox.Mostrar("Documento enviado a la cola de impresión de manera correcta.", "Impresión exitosa", MensajeBox.TipoMensaje.Info);
                }
            }
            catch (Exception ex)
            {
                btnDescargarPdf.Visibility = Visibility.Visible;
                btnImprimirFicha.Visibility = Visibility.Visible;
                MensajeBox.Mostrar($"No se pudo imprimir:\n\n{ex.Message}", "Error de Impresión", MensajeBox.TipoMensaje.Error);
            }
        }

        private void btnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            ModalDetalle.Visibility = Visibility.Collapsed;
        }

        private async void btnBaja_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is BeneficiarioGridDTO seleccionado)
            {
                bool seguro = MensajeBox.Mostrar($"¿Está seguro de que desea dar de baja a:\n\n{seleccionado.Nombre}?", "Confirmar Baja", MensajeBox.TipoMensaje.QuerySiNo);

                if (seguro)
                {
                    try
                    {
                        HttpResponseMessage respuesta = await _client.DeleteAsync($"api/Beneficiarios/{seleccionado.Id}");
                        if (respuesta.IsSuccessStatusCode)
                        {
                            MensajeBox.Mostrar("El beneficiario ha sido removido con éxito del padrón.", "Baja Completada", MensajeBox.TipoMensaje.Info);
                            CargarPadronDesdeAPI();
                        }
                    }
                    catch (Exception ex)
                    {
                        MensajeBox.Mostrar($"Error: {ex.Message}", "Error", MensajeBox.TipoMensaje.Error);
                    }
                }
            }
        }
    }

    // 🌟 CLAVE DEL ÉXITO: Mapeamos con alias explícitos para forzar la compatibilidad entre CamelCase y C# sin perder tipado
    public class BeneficiarioGridDTO
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fechaRelevamiento")]
        public DateTime FechaRelevamiento { get; set; }

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; }

        [JsonPropertyName("dni")]
        public string Dni { get; set; }

        [JsonPropertyName("domicilio")]
        public string Domicilio { get; set; }

        [JsonPropertyName("colorSemaforo")]
        public string ColorSemaforo { get; set; }
    }
}