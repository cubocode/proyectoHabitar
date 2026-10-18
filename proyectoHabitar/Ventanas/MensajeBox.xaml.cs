using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace proyectoHabitar.Ventanas
{
    public partial class MensajeBox : Window
    {
        public enum TipoMensaje { Info, Warning, Error, QuerySiNo }

        private bool _resultado = false;

        public MensajeBox(string mensaje, string titulo, TipoMensaje tipo)
        {
            InitializeComponent();
            lblMensaje.Text = mensaje;
            lblTitulo.Text = titulo;

            switch (tipo)
            {
                case TipoMensaje.Info: lblIcono.Text = "ℹ️"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.Warning: lblIcono.Text = "⚠️"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.Error: lblIcono.Text = "❌"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.QuerySiNo: lblIcono.Text = "❓"; btnAceptar.Content = "Sí"; btnCancelar.Content = "No"; btnCancelar.Visibility = Visibility.Visible; break;
            }

            this.Loaded += (s, e) =>
            {
                if (txtEntrada.Visibility == Visibility.Visible) txtEntrada.Focus();
                else btnAceptar.Focus();
            };
        }

        public static bool Mostrar(string mensaje, string titulo = "HabitAR", TipoMensaje tipo = TipoMensaje.Info)
        {
            MensajeBox box = new MensajeBox(mensaje, titulo, tipo);
            AsignarDueño(box);
            box.ShowDialog();
            return box._resultado;
        }

        /// <summary>Pide un texto (por ejemplo el motivo de una anulación). Devuelve null si se cancela.</summary>
        public static string? PedirTexto(string mensaje, string titulo = "HabitAR", string valorInicial = "")
        {
            MensajeBox box = new MensajeBox(mensaje, titulo, TipoMensaje.QuerySiNo);
            box.lblIcono.Text = "✏️";
            box.btnAceptar.Content = "Aceptar";
            box.btnCancelar.Content = "Cancelar";
            box.txtEntrada.Text = valorInicial;
            box.txtEntrada.Visibility = Visibility.Visible;
            AsignarDueño(box);
            box.ShowDialog();
            return box._resultado ? box.txtEntrada.Text.Trim() : null;
        }

        /// <summary>Centra el mensaje sobre la ventana activa y lo mantiene encima de ella.</summary>
        private static void AsignarDueño(MensajeBox box)
        {
            var dueño = Application.Current?.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive && w != box);
            if (dueño != null && dueño.IsLoaded)
            {
                box.Owner = dueño;
                box.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
        }

        private void btnAceptar_Click(object sender, RoutedEventArgs e)
        {
            _resultado = true;
            this.Close();
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            _resultado = false;
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                _resultado = false;
                this.Close();
            }
            else if (e.Key == Key.Enter && txtEntrada.Visibility == Visibility.Visible)
            {
                btnAceptar_Click(sender, e);
            }
        }
    }
}
