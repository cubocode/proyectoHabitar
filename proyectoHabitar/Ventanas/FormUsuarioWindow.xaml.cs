using System.Windows;
using System.Windows.Controls;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class FormUsuarioWindow : Window
    {
        private int? _idUsuarioEditar = null;

        public FormUsuarioWindow()
        {
            InitializeComponent();
            cmbRol.SelectedIndex = 1;
        }

        public FormUsuarioWindow(UsuarioDto usuario) : this()
        {
            _idUsuarioEditar = usuario.Id;
            lblTitulo.Text = "Editar Usuario";

            txtUsuario.Text = usuario.Usuario;
            txtUsuario.IsEnabled = false;

            // En edición la contraseña es opcional: si se completa, se blanquea.
            lblContrasena.Text = "Nueva contraseña (dejar vacío para no cambiarla)";

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
            if (string.IsNullOrWhiteSpace(txtUsuario.Text) || string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtApellido.Text))
            {
                MensajeBox.Mostrar("Por favor, completá los campos obligatorios (Usuario, Nombre y Apellido).", "HabitAR", MensajeBox.TipoMensaje.Warning);
                return;
            }

            bool esNuevo = _idUsuarioEditar == null;
            if (esNuevo && txtPassword.Password.Length < 6)
            {
                MensajeBox.Mostrar("Asignale al nuevo usuario una contraseña de al menos 6 caracteres.", "HabitAR", MensajeBox.TipoMensaje.Warning);
                return;
            }
            if (!esNuevo && txtPassword.Password.Length is > 0 and < 6)
            {
                MensajeBox.Mostrar("La nueva contraseña debe tener al menos 6 caracteres.", "HabitAR", MensajeBox.TipoMensaje.Warning);
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
                Rol = (cmbRol.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "",
                Activo = chkActivo.IsChecked ?? true
            };

            btnGuardar.IsEnabled = false;
            try
            {
                RespuestaApi r = esNuevo
                    ? await ApiClient.PostAsync("api/Usuarios", datos)
                    : await ApiClient.PutAsync($"api/Usuarios/{_idUsuarioEditar}", datos);

                MensajeBox.Mostrar(r.Mensaje, "Éxito", MensajeBox.TipoMensaje.Info);
                this.DialogResult = true;
                this.Close();
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

        private void btnCancelar_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
