using System;
using System.Net.Http; //pa los endpoínts
using System.Text;
using System.Text.Json;
using System.Windows;

namespace proyectoHabitar.Ventanas
{
    public partial class Login : Window
    {
        
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };

        public Login()
        {
            InitializeComponent();
        }

        private async void btnIngresar_Click(object sender, RoutedEventArgs e)
        {
            string usuarioInput = txtUsuario.Text.Trim();
            string contrasenaInput = txtPassword.Password; 

            if (string.IsNullOrEmpty(usuarioInput) || string.IsNullOrEmpty(contrasenaInput))
            {
                MessageBox.Show("Por favor, ingresá usuario y contraseña.", "Campos incompletos", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    proyectoHabitar.Ventanas.Home ventanaHome = new proyectoHabitar.Ventanas.Home();
                    ventanaHome.Show();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Usuario o contraseña incorrectos.", "Error de Autenticación", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo conectar con el servidor de la app:\n\nDetalle: {ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnSalir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}