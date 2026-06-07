using System;
using System.Net.Http; //pa los endpoínts
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace proyectoHabitar.Ventanas
{
    public partial class Login : Window
    {
        
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };

        public Login()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                txtUsuario.Focus();
            };
        }

        private async void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuarioInput = txtUsuario.Text.Trim();
            string contrasenaInput = txtPassword.Password;

          
            if (string.IsNullOrEmpty(usuarioInput) || string.IsNullOrEmpty(contrasenaInput))
            {
                MensajeBox.Mostrar("Por favor, ingresá usuario y contraseña.", "Campos incompletos", MensajeBox.TipoMensaje.Warning);
                return;
            }

            var datosLogin = new { Usuario = usuarioInput, Contrasena = contrasenaInput };
            string json = JsonSerializer.Serialize(datosLogin);
            StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
       
                HttpResponseMessage respuesta = await _client.PostAsync("api/Auth/login", contenido);

                if (respuesta.IsSuccessStatusCode)
                {
                    
                    btnIngresar.IsEnabled = false;
                    btnSalir.IsEnabled = false;
                    progresoCarga.Visibility = Visibility.Visible;

                  
                    await Task.Delay(2500);

                 
                    proyectoHabitar.Ventanas.Home ventanaHome = new proyectoHabitar.Ventanas.Home();
                    ventanaHome.Show();
                    this.Close();
                }
                else
                {
                   
                    MensajeBox.Mostrar("Usuario o contraseña incorrectos.", "Error de Autenticación", MensajeBox.TipoMensaje.Error);
                }
            }
            catch (Exception ex)
            {
           
                MensajeBox.Mostrar($"No se pudo conectar con el servidor de la app:\n\nDetalle: {ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
              
                btnIngresar.IsEnabled = true;
                btnSalir.IsEnabled = true;
                progresoCarga.Visibility = Visibility.Collapsed;
            }
        }

        private bool _isPasswordVisible = false;

        private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (_isPasswordVisible)
            {
              
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "🙈"; 
            }
            else
            {
                
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnTogglePassword.Content = "👁";
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

        private void Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
           
            if (e.Key == System.Windows.Input.Key.Enter)
            {
               
                btnIngresar_Click(this, new RoutedEventArgs());
            }
        }
    }
}