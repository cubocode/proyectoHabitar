using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class Login : Window
    {
        private bool _isPasswordVisible = false;

        public Login()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                txtUsuario.Focus();
            };
        }

        // Se dispara con el botón o con Enter en cualquier campo (btnIngresar tiene IsDefault="True").
        private async void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            if (!btnIngresar.IsEnabled) return;

            string usuarioInput = txtUsuario.Text.Trim();
            string contrasenaInput = _isPasswordVisible ? txtPasswordVisible.Text : txtPassword.Password;

            if (string.IsNullOrEmpty(usuarioInput) || string.IsNullOrEmpty(contrasenaInput))
            {
                MensajeBox.Mostrar("Por favor, ingresá usuario y contraseña.", "Campos incompletos", MensajeBox.TipoMensaje.Warning);
                (string.IsNullOrEmpty(usuarioInput) ? (Control)txtUsuario : ContrasenaActiva()).Focus();
                return;
            }

            MostrarCargando(true);

            try
            {
                LoginResponse datos = await ApiClient.PostAsync<LoginResponse>("api/Auth/login",
                    new { Usuario = usuarioInput, Contrasena = contrasenaInput });

                Sesion.Iniciar(datos);

                await Task.Delay(2500);

                Home ventanaHome = new Home();
                ventanaHome.Show();
                this.Close();
                return;
            }
            catch (ApiException ex)
            {
                string titulo = ex.Estado switch
                {
                    HttpStatusCode.Unauthorized => "Error de Autenticación",
                    HttpStatusCode.Forbidden => "Usuario inactivo",
                    _ => "Error de Conexión"
                };
                MensajeBox.Mostrar(ex.Message, titulo, MensajeBox.TipoMensaje.Error);
            }

            MostrarCargando(false);
            txtPassword.Clear();
            txtPasswordVisible.Clear();
            ContrasenaActiva().Focus();
        }

        private void MostrarCargando(bool cargando)
        {
            btnIngresar.IsEnabled = !cargando;
            btnSalir.IsEnabled = !cargando;
            txtUsuario.IsEnabled = !cargando;
            txtPassword.IsEnabled = !cargando;
            txtPasswordVisible.IsEnabled = !cargando;
            progresoCarga.Visibility = cargando ? Visibility.Visible : Visibility.Collapsed;
        }

        private Control ContrasenaActiva() => _isPasswordVisible ? txtPasswordVisible : txtPassword;

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "🙈";
                txtPasswordVisible.Focus();
                txtPasswordVisible.CaretIndex = txtPasswordVisible.Text.Length;
            }
            else
            {
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "👁";
                txtPassword.Focus();
            }
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!_isPasswordVisible)
            {
                txtPasswordVisible.Text = txtPassword.Password;
            }
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                txtPassword.Password = txtPasswordVisible.Text;
            }
        }

        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            bool seguro = MensajeBox.Mostrar("¿Estás seguro de que querés salir de HabitAR?", "Cerrar sistema", MensajeBox.TipoMensaje.QuerySiNo);
            if (seguro)
            {
                Application.Current.Shutdown();
            }
        }
    }
}
