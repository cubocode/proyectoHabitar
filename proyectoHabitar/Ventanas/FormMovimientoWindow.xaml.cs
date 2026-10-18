using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    /// <summary>Alta y edición de un ingreso o egreso de caja.</summary>
    public partial class FormMovimientoWindow : Window
    {
        private static readonly CultureInfo AR = new CultureInfo("es-AR");

        private readonly Dictionary<string, List<string>> _categorias;
        private readonly int? _idEditar;

        /// <summary>Nuevo movimiento del tipo indicado ("Ingreso" o "Egreso").</summary>
        public FormMovimientoWindow(string tipo, Dictionary<string, List<string>> categorias)
        {
            InitializeComponent();
            _categorias = categorias;

            txtMonto.PreviewTextInput += (s, e) =>
                e.Handled = Regex.IsMatch(e.Text, "[^0-9,]") || (e.Text == "," && txtMonto.Text.Contains(","));
            txtHora.PreviewTextInput += (s, e) => e.Handled = Regex.IsMatch(e.Text, "[^0-9:]");

            dpFecha.SelectedDate = DateTime.Today;
            dpFecha.DisplayDateEnd = DateTime.Today;
            txtHora.Text = DateTime.Now.ToString("HH:mm");

            if (tipo == "Egreso") rbEgreso.IsChecked = true;
            else rbIngreso.IsChecked = true;

            lblTitulo.Text = tipo == "Egreso" ? "Registrar egreso" : "Registrar ingreso";
            Loaded += (s, e) => txtConcepto.Focus();
        }

        /// <summary>Edición de un movimiento existente.</summary>
        public FormMovimientoWindow(MovimientoDto movimiento, Dictionary<string, List<string>> categorias)
            : this(movimiento.Tipo, categorias)
        {
            _idEditar = movimiento.Id;
            lblTitulo.Text = $"Editar {movimiento.Tipo.ToLower()} #{movimiento.Id}";

            dpFecha.SelectedDate = movimiento.FechaHora.Date;
            txtHora.Text = movimiento.FechaHora.ToString("HH:mm");
            cmbCategoria.Text = movimiento.Categoria;
            txtConcepto.Text = movimiento.Concepto;
            txtDetalle.Text = movimiento.Detalle;
            txtMonto.Text = movimiento.Monto.ToString("0.00", AR);
            cmbMedioPago.Text = movimiento.MedioPago;
            txtComprobante.Text = movimiento.Comprobante;
        }

        private string TipoSeleccionado => rbEgreso.IsChecked == true ? "Egreso" : "Ingreso";

        private void Tipo_Checked(object sender, RoutedEventArgs e)
        {
            // Las categorías sugeridas cambian según sea ingreso o egreso; se conserva lo escrito.
            string actual = cmbCategoria.Text;
            cmbCategoria.ItemsSource = _categorias != null && _categorias.TryGetValue(TipoSeleccionado, out var lista) ? lista : new List<string>();
            cmbCategoria.Text = actual;

            if (_idEditar == null)
                lblTitulo.Text = TipoSeleccionado == "Egreso" ? "Registrar egreso" : "Registrar ingreso";
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
            var errores = new List<string>();

            if (dpFecha.SelectedDate == null) errores.Add("Fecha.");
            if (!TimeSpan.TryParseExact(txtHora.Text.Trim(), new[] { @"h\:mm", @"hh\:mm" }, CultureInfo.InvariantCulture, out TimeSpan hora) || hora.TotalHours >= 24)
                errores.Add("Hora: usá el formato HH:mm (ej: 14:30).");
            if (string.IsNullOrWhiteSpace(cmbCategoria.Text)) errores.Add("Categoría.");
            if (string.IsNullOrWhiteSpace(txtConcepto.Text)) errores.Add("Concepto.");
            if (!decimal.TryParse(txtMonto.Text, NumberStyles.Number, AR, out decimal monto) || monto <= 0)
                errores.Add("Monto: debe ser un número mayor a cero (ej: 15000,50).");

            DateTime fechaHora = (dpFecha.SelectedDate ?? DateTime.Today).Date + hora;
            if (errores.Count == 0 && fechaHora > DateTime.Now.AddMinutes(10))
                errores.Add("No se pueden registrar movimientos con fecha u hora futura.");

            if (errores.Count > 0)
            {
                MensajeBox.Mostrar("Revisá estos datos:\n\n• " + string.Join("\n• ", errores), "Datos incompletos", MensajeBox.TipoMensaje.Warning);
                return;
            }

            var datos = new
            {
                FechaHora = fechaHora,
                Tipo = TipoSeleccionado,
                Categoria = cmbCategoria.Text.Trim(),
                Concepto = txtConcepto.Text.Trim(),
                Detalle = txtDetalle.Text.Trim(),
                Monto = Math.Round(monto, 2),
                MedioPago = (cmbMedioPago.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? cmbMedioPago.Text,
                Comprobante = txtComprobante.Text.Trim()
            };

            btnGuardar.IsEnabled = false;
            try
            {
                RespuestaApi r = _idEditar == null
                    ? await ApiClient.PostAsync("api/Fondos/movimientos", datos)
                    : await ApiClient.PutAsync($"api/Fondos/movimientos/{_idEditar}", datos);

                MensajeBox.Mostrar(r.Mensaje, "Fondos", MensajeBox.TipoMensaje.Info);
                DialogResult = true;
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar(ex.Message, "No se pudo guardar", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                btnGuardar.IsEnabled = true;
            }
        }
    }
}
