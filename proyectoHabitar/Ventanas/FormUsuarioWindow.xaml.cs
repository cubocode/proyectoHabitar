using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace proyectoHabitar.Ventanas
{
    public partial class FormUsuarioWindow : Window
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };
        private int? _idUsuarioEditar = null; 

     
        public FormUsuarioWindow()
        {
            InitializeComponent();
            cmbRol.SelectedIndex = 1; 
        }

      
        public FormUsuarioWindow(UC_Configuracion.UsuarioData usuario) : this()
        {
            _idUsuarioEditar = usuario.Id;
            lblTitulo.Text = "Editar Usuario";

          
            txtUsuario.Text = usuario.Usuario;
            txtUsuario.IsEnabled = false; 
            PanelContrasena.Visibility = Visibility.Collapsed; 

            txtNombre.Text = usuario.Nombre;
            txtApellido.Text = usuario.Apellido;
            txtDni.Text = usuario.DNI;
            txtEmail.Text = usuario.Email;
            cmbRol.Text = usuario.Rol;

          
            PanelActivo.Visibility = Visibility.Visible;
            chkActivo.IsChecked = usuario.Activo;
        }

        private async void btnGuardar_Click(object sender, RoutedEventArgs e)
        {
      
            if (string.IsNullOrEmpty(txtUsuario.Text) || string.IsNullOrEmpty(txtNombre.Text) || string.IsNullOrEmpty(txtApellido.Text))
            {
                MessageBox.Show("Por favor, completá los campos obligatorios (Usuario, Nombre y Apellido).", "HabitAR", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            
            var datos = new
            {
                Usuario = txtUsuario.Text.Trim(),
                Contrasena = txtPassword.Password,
                Nombre = txtNombre.Text.Trim(),
                Apellido = txtApellido.Text.Trim(),
                DNI = txtDni.Text.Trim(),
                Email = txtEmail.Text.Trim(),
                Rol = (cmbRol.SelectedItem as ComboBoxItem)?.Content.ToString(),
                Activo = chkActivo.IsChecked ?? true
            };

            string json = JsonSerializer.Serialize(datos);
            StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage respuesta;

                if (_idUsuarioEditar == null)
                {
                   
                    if (string.IsNullOrEmpty(txtPassword.Password))
                    {
                        MessageBox.Show("Debes asignarle una contraseña al nuevo usuario.", "HabitAR", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    respuesta = await _client.PostAsync("api/Usuarios", contenido);
                }
                else
                {
                
                    respuesta = await _client.PutAsync($"api/Usuarios/{_idUsuarioEditar}", contenido);
                }

                if (respuesta.IsSuccessStatusCode)
                {
                    MessageBox.Show("Datos guardados con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true; 
                    this.Close();
                }
                else
                {
                    MessageBox.Show("El servidor rechazó la solicitud. Verificá los datos.", "Error de guardado", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al conectar con el servidor:\n{ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}