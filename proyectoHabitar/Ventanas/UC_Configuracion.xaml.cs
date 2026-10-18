using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class UC_Configuracion : UserControl
    {
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

        private async void btnRecalcular_Click(object sender, RoutedEventArgs e)
        {
            bool seguro = MensajeBox.Mostrar("Se va a recalcular el puntaje y el color de semáforo de TODAS las fichas con el algoritmo vigente.\n\n¿Continuar?",
                                             "Recalcular puntajes", MensajeBox.TipoMensaje.QuerySiNo);
            if (!seguro) return;

            btnRecalcular.IsEnabled = false;
            try
            {
                RespuestaApi r = await ApiClient.PostAsync("api/Beneficiarios/recalcular-puntajes", null);
                MensajeBox.Mostrar(r.Mensaje, "Recalcular puntajes", MensajeBox.TipoMensaje.Info);
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar(ex.Message, "Error", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                btnRecalcular.IsEnabled = true;
            }
        }

        private async Task CargarUsuariosGrid()
        {
            try
            {
                dgUsuarios.ItemsSource = await ApiClient.GetAsync<List<UsuarioDto>>("api/Usuarios");
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"No se pudieron recuperar los usuarios:\n\n{ex.Message}", "Error de API", MensajeBox.TipoMensaje.Error);
            }
        }

        private async void btnNuevo_Click(object sender, RoutedEventArgs e)
        {
            FormUsuarioWindow frm = new FormUsuarioWindow { Owner = Window.GetWindow(this) };

            if (frm.ShowDialog() == true)
            {
                await CargarUsuariosGrid();
            }
        }

        private void dgUsuarios_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (dgUsuarios.SelectedItem != null) btnEditar_Click(sender, e);
        }

        private async void btnEditar_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is not UsuarioDto usuarioSeleccionado)
            {
                MensajeBox.Mostrar("Por favor, seleccioná un usuario de la grilla para editar.", "Atención", MensajeBox.TipoMensaje.Warning);
                return;
            }

            FormUsuarioWindow frm = new FormUsuarioWindow(usuarioSeleccionado) { Owner = Window.GetWindow(this) };

            if (frm.ShowDialog() == true)
            {
                await CargarUsuariosGrid();
            }
        }

        private async void btnEliminar_Click(object sender, RoutedEventArgs e)
        {
            if (dgUsuarios.SelectedItem is not UsuarioDto usuarioSeleccionado)
            {
                MensajeBox.Mostrar("Por favor, seleccioná un usuario para dar de baja.", "Atención", MensajeBox.TipoMensaje.Warning);
                return;
            }

            bool seguro = MensajeBox.Mostrar($"¿Estás seguro de que querés dar de baja al usuario '{usuarioSeleccionado.Usuario}'?\n\nNo podrá volver a iniciar sesión hasta que se lo reactive.",
                                             "Confirmar Baja", MensajeBox.TipoMensaje.QuerySiNo);
            if (!seguro) return;

            try
            {
                RespuestaApi r = await ApiClient.DeleteAsync($"api/Usuarios/{usuarioSeleccionado.Id}");
                MensajeBox.Mostrar(r.Mensaje, "Éxito", MensajeBox.TipoMensaje.Info);
                await CargarUsuariosGrid();
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar(ex.Message, "No se pudo dar de baja", MensajeBox.TipoMensaje.Error);
            }
        }
    }
}
