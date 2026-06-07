using System.Windows;

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

            // Configuración de tipos (Mantené el switch que armamos antes...)
            switch (tipo)
            {
                case TipoMensaje.Info: lblIcono.Text = "ℹ️"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.Warning: lblIcono.Text = "⚠️"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.Error: lblIcono.Text = "❌"; btnAceptar.Content = "Aceptar"; break;
                case TipoMensaje.QuerySiNo: lblIcono.Text = "❓"; btnAceptar.Content = "Sí"; btnCancelar.Content = "No"; btnCancelar.Visibility = Visibility.Visible; break;
            }

            // 🌟 LE CLAVAMOS EL FOCO AL BOTÓN APENAS LA VENTANA SE DIBUJE
            this.Loaded += (s, e) =>
            {
                btnAceptar.Focus();
            };
        }


        public static bool Mostrar(string mensaje, string titulo = "HabitAR", TipoMensaje tipo = TipoMensaje.Info)
        {
            MensajeBox box = new MensajeBox(mensaje, titulo, tipo);
            box.ShowDialog();
            return box._resultado;
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
    }
}