using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class FormAlta : UserControl
    {
        private List<BeneficiarioListaDto> _listaCompleta = new List<BeneficiarioListaDto>();
        private string _filtroColorActual = "Todos";
        private BeneficiarioDto? _fichaAbierta;

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
                _listaCompleta = await ApiClient.GetAsync<List<BeneficiarioListaDto>>("api/Beneficiarios") ?? new List<BeneficiarioListaDto>();
                AplicarFiltrosCombinados();
            }
            catch (ApiException ex)
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
            if (sender is not Button btn) return;

            btnFiltroTodos.Background = Brushes.White; btnFiltroTodos.Foreground = Pincel("#334155");
            btnFiltroRojo.Background = Brushes.White; btnFiltroRojo.Foreground = Pincel("#EF4444");
            btnFiltroAmarillo.Background = Brushes.White; btnFiltroAmarillo.Foreground = Pincel("#D97706");
            btnFiltroVerde.Background = Brushes.White; btnFiltroVerde.Foreground = Pincel("#10B981");

            if (btn.Name == "btnFiltroTodos") { _filtroColorActual = "Todos"; btn.Background = Pincel("#334155"); btn.Foreground = Brushes.White; }
            else if (btn.Name == "btnFiltroRojo") { _filtroColorActual = "Rojo"; btn.Background = Pincel("#FEE2E2"); }
            else if (btn.Name == "btnFiltroAmarillo") { _filtroColorActual = "Amarillo"; btn.Background = Pincel("#FEF3C7"); }
            else if (btn.Name == "btnFiltroVerde") { _filtroColorActual = "Verde"; btn.Background = Pincel("#D1FAE5"); }

            AplicarFiltrosCombinados();
        }

        private void AplicarFiltrosCombinados()
        {
            if (!IsInitialized) return;

            string texto = txtBuscar.Text.Trim().ToLower();
            var resultado = _listaCompleta.AsEnumerable();

            if (_filtroColorActual != "Todos")
            {
                resultado = resultado.Where(b => b.ColorSemaforo == _filtroColorActual);
            }

            if (!string.IsNullOrEmpty(texto))
            {
                resultado = resultado.Where(b => b.Nombre.ToLower().Contains(texto)
                                              || b.Dni.ToLower().Contains(texto)
                                              || b.Localidad.ToLower().Contains(texto));
            }

            dgBeneficiarios.ItemsSource = resultado.OrderBy(b => b.Nombre).ToList();
        }

        private void NavegarAFormularioDeCarga(int idBeneficiario)
        {
            Home.NavegarDesde(this, new UC_CargaBeneficiario(idBeneficiario));
        }

        private void btnAgregar_Click(object sender, RoutedEventArgs e)
        {
            NavegarAFormularioDeCarga(0);
        }

        private void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is BeneficiarioListaDto seleccionado)
            {
                NavegarAFormularioDeCarga(seleccionado.Id);
            }
            else
            {
                MensajeBox.Mostrar("Por favor, seleccione un beneficiario de la grilla para editar.", "Selección Requerida", MensajeBox.TipoMensaje.Warning);
            }
        }

        private async void dgBeneficiarios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is not BeneficiarioListaDto seleccionado) return;

            try
            {
                BeneficiarioDto r = await ApiClient.GetAsync<BeneficiarioDto>($"api/Beneficiarios/{seleccionado.Id}");
                _fichaAbierta = r;

                lblModalTitulo.Text = $"Ficha de Emergencia: {r.Apellido}, {r.Nombre}";
                lblModalSemaforo.Text = $"Prioridad Asignada: {r.ColorSemaforo.ToUpper()} ({r.PuntajeCriticidad} Puntos de Criticidad)";
                lblModalSemaforo.Foreground = r.ColorSemaforo switch
                {
                    "Rojo" => Brushes.Red,
                    "Amarillo" => Brushes.Orange,
                    _ => Brushes.Green
                };

                string edad = r.FechaNacimiento.HasValue ? $" ({CalcularEdad(r.FechaNacimiento.Value)} años)" : "";

                txtModalDocumentos.Text = $"• Nro de CUIL: {r.Cuil}\n" +
                                          $"• Documento: DNI N° {r.NumeroDocumento}\n" +
                                          $"• Fecha de Nacimiento: {r.FechaNacimiento:dd/MM/yyyy}{edad}\n" +
                                          $"• Identidad de Género: {r.Genero}";

                txtModalContacto.Text = $"• Celular: {(string.IsNullOrWhiteSpace(r.TelefonoCelular) ? "-" : "+54 9 " + r.TelefonoCelular)}\n" +
                                        $"• Correo Electrónico: {(string.IsNullOrWhiteSpace(r.Email) ? "-" : r.Email)}\n" +
                                        $"• Dirección: {r.Domicilio} ({r.Localidad}, CP {r.CodigoPostal})";

                txtModalVivienda.Text = $"• Vivienda Original: {r.TipoViviendaOriginal}\n" +
                                        $"• Tenencia del Terreno: {r.RegimenTenenciaTerreno}\n" +
                                        $"• Piso / Techo: {r.MaterialPredominantePiso} / {r.MaterialPredominanteTecho}\n" +
                                        $"• Provisión de Agua: {r.ProcedenciaAgua}\n" +
                                        $"• Desagüe Sanitario: {r.DesagueInodoro}\n" +
                                        $"• Hogar: {r.CantidadConvivientes} integrantes, {r.CantidadHijosMenores} menores, {r.CantidadAdultosMayores} adultos mayores" +
                                        (r.TieneIntegrantesDiscapacidad ? ", con discapacidad" : "");

                txtModalDireccionMapa.Text = Mapas.DireccionCompleta(r);
                txtModalCoordenadas.Text = r.Latitud.HasValue
                    ? $"{r.Latitud.Value.ToString("F5", CultureInfo.InvariantCulture)}, {r.Longitud!.Value.ToString("F5", CultureInfo.InvariantCulture)}" +
                      (r.UbicacionAproximada ? "\n(ubicación aproximada)" : "")
                    : "Sin coordenadas cargadas";

                ModalDetalle.Visibility = Visibility.Visible;
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"Error al abrir la ficha: {ex.Message}", "Error de Comunicación", MensajeBox.TipoMensaje.Error);
            }
        }

        private void btnAbrirGoogleMaps_Click(object sender, RoutedEventArgs e)
        {
            if (_fichaAbierta == null) return;
            Mapas.AbrirEnGoogleMaps(_fichaAbierta.Latitud, _fichaAbierta.Longitud, Mapas.DireccionCompleta(_fichaAbierta));
        }

        private void btnDescargarPdf_Click(object sender, RoutedEventArgs e)
        {
            if (_fichaAbierta == null) return;

            try
            {
                string nombreArchivo = $"Ficha_Social_{_fichaAbierta.Apellido}_{_fichaAbierta.Nombre}";
                foreach (char c in Path.GetInvalidFileNameChars()) nombreArchivo = nombreArchivo.Replace(c, '_');

                var dialogoGuardar = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = nombreArchivo,
                    DefaultExt = ".pdf",
                    Filter = "Documento PDF (*.pdf)|*.pdf"
                };

                if (dialogoGuardar.ShowDialog() == true)
                {
                    QuestPDF.Settings.License = LicenseType.Community;

                    string colorPrioridad = _fichaAbierta.ColorSemaforo switch
                    {
                        "Rojo" => QuestPDF.Helpers.Colors.Red.Medium,
                        "Amarillo" => QuestPDF.Helpers.Colors.Orange.Darken1,
                        _ => QuestPDF.Helpers.Colors.Green.Darken1
                    };

                    Document.Create(contenedor =>
                    {
                        contenedor.Page(pagina =>
                        {
                            pagina.Size(PageSizes.A4);
                            pagina.Margin(2, Unit.Centimetre);
                            pagina.PageColor(QuestPDF.Helpers.Colors.White);
                            pagina.DefaultTextStyle(x => x.FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken3));

                            pagina.Header()
                                .Text(lblModalTitulo.Text)
                                .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                            pagina.Content().PaddingVertical(1, Unit.Centimetre).Column(columna =>
                            {
                                columna.Spacing(15);

                                columna.Item().Text(lblModalSemaforo.Text).Bold().FontColor(colorPrioridad);

                                columna.Item().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(10).Column(sub => {
                                    sub.Item().Text("DATOS DE IDENTIFICACIÓN").Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                                    sub.Item().Text(txtModalDocumentos.Text);
                                });

                                columna.Item().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(10).Column(sub => {
                                    sub.Item().Text("DATOS DE CONTACTO Y LOCALIZACIÓN").Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                                    sub.Item().Text(txtModalContacto.Text);
                                    if (_fichaAbierta.Latitud.HasValue)
                                        sub.Item().Text($"• Coordenadas: {txtModalCoordenadas.Text.Replace("\n", " ")}");
                                });

                                columna.Item().Background(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(10).Column(sub => {
                                    sub.Item().Text("INFRAESTRUCTURA Y CRITICIDAD PREVIA").Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                                    sub.Item().Text(txtModalVivienda.Text);
                                });
                            });

                            pagina.Footer()
                                .AlignCenter()
                                .Text(x => {
                                    x.Span($"Ficha generada por el Sistema HabitAR el {DateTime.Now:dd/MM/yyyy HH:mm} por {Sesion.NombreCompleto} - Pág. ");
                                    x.CurrentPageNumber();
                                });
                        });
                    }).GeneratePdf(dialogoGuardar.FileName);

                    MensajeBox.Mostrar("PDF generado exitosamente.", "Exportación Exitosa", MensajeBox.TipoMensaje.Info);

                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialogoGuardar.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"No se pudo escribir el PDF: {ex.Message}", "Error de Escritura", MensajeBox.TipoMensaje.Error);
            }
        }

        private void btnCerrarModal_Click(object sender, RoutedEventArgs e)
        {
            ModalDetalle.Visibility = Visibility.Collapsed;
            _fichaAbierta = null;
        }

        private async void btnBaja_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiarios.SelectedItem is not BeneficiarioListaDto seleccionado)
            {
                MensajeBox.Mostrar("Seleccioná un beneficiario de la grilla para darlo de baja.", "Selección Requerida", MensajeBox.TipoMensaje.Warning);
                return;
            }

            bool seguro = MensajeBox.Mostrar($"¿Está seguro de que desea dar de baja a:\n\n{seleccionado.Nombre}?\n\n" +
                                             "El registro se conserva en la base y se reactiva si se lo vuelve a relevar.",
                                             "Confirmar Baja", MensajeBox.TipoMensaje.QuerySiNo);
            if (!seguro) return;

            try
            {
                RespuestaApi respuesta = await ApiClient.DeleteAsync($"api/Beneficiarios/{seleccionado.Id}");
                MensajeBox.Mostrar(respuesta.Mensaje, "Baja Completada", MensajeBox.TipoMensaje.Info);
                CargarPadronDesdeAPI();
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"No se pudo dar de baja:\n\n{ex.Message}", "Error", MensajeBox.TipoMensaje.Error);
            }
        }

        private static int CalcularEdad(DateTime nacimiento)
        {
            int edad = DateTime.Today.Year - nacimiento.Year;
            if (nacimiento.Date > DateTime.Today.AddYears(-edad)) edad--;
            return edad;
        }

        private static Brush Pincel(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
    }
}
