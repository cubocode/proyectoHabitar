using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace proyectoHabitar.Ventanas
{
    public partial class UC_Configuracion : UserControl
    {
      
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };

        public UC_Configuracion()
        {
            InitializeComponent();
        }

      
        private async void btnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            GridMenuConfig.Visibility = Visibility.Collapsed;
            GridUsuarios.Visibility = Visibility.Visible;
            await CargarUsuariosGrid();
        }

        
        private void btnVolver_Click(object sender, RoutedEventArgs e)
        {
            GridUsuarios.Visibility = Visibility.Collapsed;
            GridMenuConfig.Visibility = Visibility.Visible;
        }

        
        private async System.Threading.Tasks.Task CargarUsuariosGrid()
        {
            try
            {
                HttpResponseMessage respuesta = await _client.GetAsync("api/Usuarios");
                if (respuesta.IsSuccessStatusCode)
                {
                    string json = await respuesta.Content.ReadAsStringAsync();

              
                    var listaUsuarios = JsonSerializer.Deserialize<List<UsuarioData>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    dgUsuarios.ItemsSource = listaUsuarios;
                }
                else
                {
                    MessageBox.Show("No se pudieron recuperar los usuarios del servidor.", "Error de API", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error de conexión con el backend:\n{ex.Message}", "Error de Conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

      
        private async void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            FormUsuarioWindow frm = new FormUsuarioWindow();
            frm.Owner = Window.GetWindow(this); 

           
            if (frm.ShowDialog() == true)
            {
                await CargarUsuariosGrid();
            }
        }

       
        private async void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            var usuarioSeleccionado = dgUsuarios.SelectedItem as UsuarioData;

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Por favor, seleccioná un usuario de la grilla para editar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

          
            FormUsuarioWindow frm = new FormUsuarioWindow(usuarioSeleccionado);
            frm.Owner = Window.GetWindow(this);

            if (frm.ShowDialog() == true)
            {
                await CargarUsuariosGrid(); 
            }
        }

       
        private async void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            var usuarioSeleccionado = dgUsuarios.SelectedItem as UsuarioData;

            if (usuarioSeleccionado == null)
            {
                MessageBox.Show("Por favor, seleccioná un usuario para eliminar.", "Atención", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var resultado = MessageBox.Show($"¿Estás seguro de que querés dar de baja al usuario '{usuarioSeleccionado.Usuario}'?",
                                            "Confirmar Baja", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                try
                {
                   
                    HttpResponseMessage respuesta = await _client.DeleteAsync($"api/Usuarios/{usuarioSeleccionado.Id}");

                    if (respuesta.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Usuario dado de baja con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                        await CargarUsuariosGrid(); 
                    }
                    else
                    {
                        MessageBox.Show("No se pudo eliminar el usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error de conexión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

           public class UsuarioData
        {
            public int Id { get; set; }
            public string Usuario { get; set; }
            public string Email { get; set; }
            public string Nombre { get; set; }
            public string Apellido { get; set; }
            public string DNI { get; set; }
            public string Rol { get; set; }
            public bool Activo { get; set; }

            
            public string ActivoTexto => Activo ? "🟢 Activo" : "🔴 Inactivo";
        }
    }
}