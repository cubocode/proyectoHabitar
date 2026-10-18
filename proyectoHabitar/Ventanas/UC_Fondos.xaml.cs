using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    /// <summary>
    /// Módulo Fondos: caja con ingresos y egresos, filtros por fecha/tipo/categoría/texto,
    /// totales por categoría y reportes (PDF, impresión y Excel).
    /// Los movimientos no se borran: se anulan con motivo (queda registro de quién y cuándo).
    /// </summary>
    public partial class UC_Fondos : UserControl
    {
        private const string TodasLasCategorias = "Todas";

        private readonly DispatcherTimer _demoraBusqueda = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        private Dictionary<string, List<string>> _categorias = new Dictionary<string, List<string>>();
        private List<MovimientoDto> _movimientos = new List<MovimientoDto>();
        private ResumenFondosDto _resumen = new ResumenFondosDto();
        private bool _suspenderFiltros = true;
        private int _versionCarga = 0;

        public UC_Fondos()
        {
            InitializeComponent();
            _demoraBusqueda.Tick += async (s, e) =>
            {
                _demoraBusqueda.Stop();
                await CargarAsync();
            };
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_suspenderFiltros) return; // ya inicializado

            AplicarPeriodo("mes");
            await CargarCategoriasAsync();
            _suspenderFiltros = false;
            await CargarAsync();
        }

        // ------------------------------------------------------------ carga

        private async Task CargarCategoriasAsync()
        {
            try
            {
                _categorias = await ApiClient.GetAsync<Dictionary<string, List<string>>>("api/Fondos/categorias");
            }
            catch (ApiException)
            {
                _categorias = new Dictionary<string, List<string>>();
            }

            string? seleccionada = cmbCategoria.SelectedItem as string;
            var todas = new List<string> { TodasLasCategorias };
            todas.AddRange(_categorias.Values.SelectMany(c => c).Distinct().OrderBy(c => c));
            cmbCategoria.ItemsSource = todas;
            cmbCategoria.SelectedItem = seleccionada != null && todas.Contains(seleccionada) ? seleccionada : TodasLasCategorias;
        }

        private string ArmarFiltros(bool incluirListado)
        {
            var parametros = new List<string>();
            if (dpDesde.SelectedDate.HasValue) parametros.Add("desde=" + dpDesde.SelectedDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (dpHasta.SelectedDate.HasValue) parametros.Add("hasta=" + dpHasta.SelectedDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

            if (incluirListado)
            {
                string tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
                if (tipo != "Todos") parametros.Add("tipo=" + Uri.EscapeDataString(tipo));

                if (cmbCategoria.SelectedItem is string categoria && categoria != TodasLasCategorias)
                    parametros.Add("categoria=" + Uri.EscapeDataString(categoria));

                if (!string.IsNullOrWhiteSpace(txtBuscar.Text))
                    parametros.Add("texto=" + Uri.EscapeDataString(txtBuscar.Text.Trim()));

                if (chkAnulados.IsChecked == true) parametros.Add("incluirAnulados=true");
            }

            return parametros.Count == 0 ? "" : "?" + string.Join("&", parametros);
        }

        private async Task CargarAsync()
        {
            if (dpDesde.SelectedDate > dpHasta.SelectedDate)
            {
                MensajeBox.Mostrar("La fecha \"Desde\" no puede ser posterior a \"Hasta\".", "Período inválido", MensajeBox.TipoMensaje.Warning);
                return;
            }

            int version = ++_versionCarga;
            try
            {
                var tareaMovimientos = ApiClient.GetAsync<List<MovimientoDto>>("api/Fondos/movimientos" + ArmarFiltros(incluirListado: true));
                var tareaResumen = ApiClient.GetAsync<ResumenFondosDto>("api/Fondos/resumen" + ArmarFiltros(incluirListado: false));
                await Task.WhenAll(tareaMovimientos, tareaResumen);

                if (version != _versionCarga) return; // llegó una respuesta vieja; ya hay otra carga en curso

                _movimientos = tareaMovimientos.Result;
                _resumen = tareaResumen.Result;
                MostrarDatos();
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"No se pudieron cargar los movimientos:\n\n{ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
        }

        private void MostrarDatos()
        {
            dgMovimientos.ItemsSource = _movimientos;

            lblSaldoActual.Text = ReportesFondos.Pesos(_resumen.SaldoActual);
            lblIngresos.Text = ReportesFondos.Pesos(_resumen.TotalIngresos);
            lblEgresos.Text = ReportesFondos.Pesos(_resumen.TotalEgresos);
            lblSaldoFinal.Text = ReportesFondos.Pesos(_resumen.SaldoFinal);
            lblSaldoAnterior.Text = $"Saldo anterior: {ReportesFondos.Pesos(_resumen.SaldoAnterior)}";

            int cantIngresos = _resumen.PorCategoria.Where(c => c.Tipo == "Ingreso").Sum(c => c.Cantidad);
            int cantEgresos = _resumen.PorCategoria.Where(c => c.Tipo == "Egreso").Sum(c => c.Cantidad);
            lblCantIngresos.Text = $"{cantIngresos} movimiento{(cantIngresos == 1 ? "" : "s")}";
            lblCantEgresos.Text = $"{cantEgresos} movimiento{(cantEgresos == 1 ? "" : "s")}";

            lstCategorias.ItemsSource = _resumen.PorCategoria;
            lblSinCategorias.Visibility = _resumen.PorCategoria.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ------------------------------------------------------------ filtros

        private async void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            if (_suspenderFiltros || !IsLoaded) return;
            await CargarAsync();
        }

        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suspenderFiltros) return;
            _demoraBusqueda.Stop();
            _demoraBusqueda.Start();
        }

        private async void Periodo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: string periodo })
            {
                _suspenderFiltros = true;
                AplicarPeriodo(periodo);
                _suspenderFiltros = false;
                await CargarAsync();
            }
        }

        private void AplicarPeriodo(string periodo)
        {
            DateTime hoy = DateTime.Today;
            DateTime primeroDelMes = new DateTime(hoy.Year, hoy.Month, 1);

            (DateTime? desde, DateTime? hasta) = periodo switch
            {
                "hoy" => (hoy, hoy),
                "7dias" => (hoy.AddDays(-6), hoy),
                "mes" => (primeroDelMes, hoy),
                "mesAnterior" => (primeroDelMes.AddMonths(-1), primeroDelMes.AddDays(-1)),
                "anio" => (new DateTime(hoy.Year, 1, 1), hoy),
                _ => ((DateTime?)null, (DateTime?)null)
            };

            dpDesde.SelectedDate = desde;
            dpHasta.SelectedDate = hasta;
        }

        private string DescribirFiltros()
        {
            var partes = new List<string>();
            string tipo = (cmbTipo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            if (tipo != "Todos") partes.Add($"Tipo: {tipo}");
            if (cmbCategoria.SelectedItem is string cat && cat != TodasLasCategorias) partes.Add($"Categoría: {cat}");
            if (!string.IsNullOrWhiteSpace(txtBuscar.Text)) partes.Add($"Texto: \"{txtBuscar.Text.Trim()}\"");
            if (chkAnulados.IsChecked == true) partes.Add("incluye anulados");
            return string.Join(" · ", partes);
        }

        private DatosReporteFondos DatosParaReporte() => new DatosReporteFondos
        {
            Desde = dpDesde.SelectedDate,
            Hasta = dpHasta.SelectedDate,
            DescripcionFiltros = DescribirFiltros(),
            Resumen = _resumen,
            Movimientos = _movimientos.OrderBy(m => m.FechaHora).ThenBy(m => m.Id).ToList()
        };

        // ------------------------------------------------------------ altas, edición y anulación

        private async void btnIngreso_Click(object sender, RoutedEventArgs e) => await AbrirFormularioAsync(new FormMovimientoWindow("Ingreso", _categorias));

        private async void btnEgreso_Click(object sender, RoutedEventArgs e) => await AbrirFormularioAsync(new FormMovimientoWindow("Egreso", _categorias));

        private async void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgMovimientos.SelectedItem is not MovimientoDto m)
            {
                MensajeBox.Mostrar("Seleccioná un movimiento de la grilla para editarlo.", "Selección requerida", MensajeBox.TipoMensaje.Warning);
                return;
            }
            if (m.Anulado)
            {
                MostrarDetalleAnulado(m);
                return;
            }
            await AbrirFormularioAsync(new FormMovimientoWindow(m, _categorias));
        }

        private void dgMovimientos_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgMovimientos.SelectedItem is MovimientoDto) btnEditar_Click(sender, e);
        }

        private async Task AbrirFormularioAsync(FormMovimientoWindow formulario)
        {
            formulario.Owner = Window.GetWindow(this);
            if (formulario.ShowDialog() == true)
            {
                await CargarCategoriasAsync();
                await CargarAsync();
            }
        }

        private async void btnAnular_Click(object sender, RoutedEventArgs e)
        {
            if (dgMovimientos.SelectedItem is not MovimientoDto m)
            {
                MensajeBox.Mostrar("Seleccioná el movimiento que querés anular.", "Selección requerida", MensajeBox.TipoMensaje.Warning);
                return;
            }
            if (m.Anulado)
            {
                MostrarDetalleAnulado(m);
                return;
            }

            string? motivo = MensajeBox.PedirTexto(
                $"Vas a anular este {m.Tipo.ToLower()}:\n\n{m.FechaHora:dd/MM/yyyy HH:mm} · {m.Concepto} · {ReportesFondos.Pesos(m.Monto)}\n\n" +
                "El movimiento no se borra: queda registrado como anulado. Indicá el motivo:",
                "Anular movimiento");
            if (motivo == null) return;
            if (motivo.Length < 5)
            {
                MensajeBox.Mostrar("El motivo debe tener al menos 5 caracteres.", "Motivo requerido", MensajeBox.TipoMensaje.Warning);
                return;
            }

            try
            {
                RespuestaApi r = await ApiClient.PostAsync($"api/Fondos/movimientos/{m.Id}/anular", new { Motivo = motivo });
                MensajeBox.Mostrar(r.Mensaje, "Fondos", MensajeBox.TipoMensaje.Info);
                await CargarAsync();
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar(ex.Message, "No se pudo anular", MensajeBox.TipoMensaje.Error);
            }
        }

        private static void MostrarDetalleAnulado(MovimientoDto m)
        {
            MensajeBox.Mostrar($"Este movimiento está anulado y no se puede modificar.\n\n" +
                               $"Motivo: {m.MotivoAnulacion}\nAnulado por: {m.UsuarioAnulacion}\nFecha: {m.FechaAnulacion:dd/MM/yyyy HH:mm}",
                               "Movimiento anulado", MensajeBox.TipoMensaje.Info);
        }

        // ------------------------------------------------------------ reportes

        private bool HayDatosParaReporte()
        {
            if (_movimientos.Count > 0) return true;
            MensajeBox.Mostrar("No hay movimientos con los filtros actuales.", "Reporte vacío", MensajeBox.TipoMensaje.Warning);
            return false;
        }

        private string NombreArchivoBase()
        {
            string desde = dpDesde.SelectedDate?.ToString("yyyyMMdd") ?? "inicio";
            string hasta = dpHasta.SelectedDate?.ToString("yyyyMMdd") ?? "hoy";
            return $"Caja_HabitAR_{desde}-{hasta}";
        }

        private void btnPdf_Click(object sender, RoutedEventArgs e)
        {
            if (!HayDatosParaReporte()) return;

            var dialogo = new Microsoft.Win32.SaveFileDialog
            {
                FileName = NombreArchivoBase(),
                DefaultExt = ".pdf",
                Filter = "Documento PDF (*.pdf)|*.pdf"
            };
            if (dialogo.ShowDialog() != true) return;

            try
            {
                ReportesFondos.GenerarPdf(dialogo.FileName, DatosParaReporte());
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"No se pudo generar el PDF:\n\n{ex.Message}", "Error", MensajeBox.TipoMensaje.Error);
            }
        }

        private void btnImprimir_Click(object sender, RoutedEventArgs e)
        {
            if (!HayDatosParaReporte()) return;

            var dialogo = new PrintDialog();
            if (dialogo.ShowDialog() != true) return;

            try
            {
                var area = new Size(dialogo.PrintableAreaWidth, dialogo.PrintableAreaHeight);
                FlowDocument documento = ReportesFondos.CrearDocumentoImpresion(DatosParaReporte(), area);
                dialogo.PrintDocument(((IDocumentPaginatorSource)documento).DocumentPaginator, "HabitAR - Reporte de Caja");
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"No se pudo imprimir:\n\n{ex.Message}", "Error de impresión", MensajeBox.TipoMensaje.Error);
            }
        }

        private void btnExcel_Click(object sender, RoutedEventArgs e)
        {
            if (!HayDatosParaReporte()) return;

            var dialogo = new Microsoft.Win32.SaveFileDialog
            {
                FileName = NombreArchivoBase(),
                DefaultExt = ".csv",
                Filter = "CSV para Excel (*.csv)|*.csv"
            };
            if (dialogo.ShowDialog() != true) return;

            try
            {
                ReportesFondos.ExportarCsv(dialogo.FileName, DatosParaReporte());
                bool abrir = MensajeBox.Mostrar($"Archivo generado:\n{Path.GetFileName(dialogo.FileName)}\n\n¿Abrirlo ahora?", "Exportación lista", MensajeBox.TipoMensaje.QuerySiNo);
                if (abrir)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dialogo.FileName) { UseShellExecute = true });
            }
            catch (IOException ex)
            {
                MensajeBox.Mostrar($"No se pudo escribir el archivo (¿está abierto en Excel?):\n\n{ex.Message}", "Error", MensajeBox.TipoMensaje.Error);
            }
        }
    }
}
