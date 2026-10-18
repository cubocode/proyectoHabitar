using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class Home : Window
    {
        private bool _menuAbierto = false;

        public Home()
        {
            InitializeComponent();

            // Datos reales del usuario logueado (antes decía "Operador / Administrador" fijo).
            txtUsuarioNombre.Text = Sesion.NombreCompleto;
            txtUsuarioRol.Text = string.IsNullOrEmpty(Sesion.Rol) ? "Sin rol asignado" : Sesion.Rol;
            Title = $"HabitAR - Sistema de Gestión  ·  {Sesion.NombreCompleto} ({Sesion.Rol})";

            // Permisos por rol (el servidor también los valida): el Encuestador ve Fondos y Configuración bloqueados.
            if (!Sesion.EsAdministrador)
            {
                foreach (var (boton, candado) in new[] { (btnMenuFondos, lblCandadoFondos), (btnMenuConfiguracion, lblCandadoConfiguracion) })
                {
                    boton.Opacity = 0.45;
                    boton.ToolTip = "🔒 Solo disponible para Administradores";
                    candado.Visibility = Visibility.Visible;
                }
            }

            // Inicia mostrando el Dashboard automáticamente en pantalla
            PanelCentral.Content = new UC_Dashboard();
        }

        private void btnToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_menuAbierto)
            {
                Storyboard sb = (Storyboard)this.FindResource("CerrarMenu");
                sb.Begin();

                // Mantiene alineada la hamburguesa a la izquierda al colapsar
                btnToggleMenu.Content = new TextBlock { Text = "☰", FontSize = 20, Foreground = System.Windows.Media.Brushes.White };

                txtLogoTitle.Visibility = Visibility.Collapsed;
                UserInfoPanel.Visibility = Visibility.Collapsed;

                // Esconde el botón integrado de forma segura
                BtnCerrarSesionIntegrado.Visibility = Visibility.Collapsed;
            }
            else
            {
                Storyboard sb = (Storyboard)this.FindResource("AbrirMenu");
                sb.Begin();

                // Pone la cruz y gracias al HorizontalAlignment="Stretch" del XAML, se tira a la derecha solo
                btnToggleMenu.Content = new TextBlock { Text = "✕", FontSize = 20, Foreground = System.Windows.Media.Brushes.White };

                txtLogoTitle.Visibility = Visibility.Visible;
                UserInfoPanel.Visibility = Visibility.Visible;

                // Muestra de manera natural el botón rojo debajo del nombre de usuario
                BtnCerrarSesionIntegrado.Visibility = Visibility.Visible;
            }

            _menuAbierto = !_menuAbierto;
        }

        private async void MenuItem_CerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            bool seguro = MensajeBox.Mostrar("¿Estás seguro de que querés cerrar sesión?", "Cerrar Sesión", MensajeBox.TipoMensaje.QuerySiNo);

            if (seguro)
            {
                // Congelamos el entorno del Home e iniciamos la carga de salida
                PanelCentral.Content = null;
                PanelCentral.Visibility = Visibility.Collapsed;
                GridMenuLateral.IsEnabled = false;

                panelCierreCarga.Visibility = Visibility.Visible;

                await Task.Delay(2200);

                Sesion.Cerrar();

                Login ventanaLogin = new Login();
                ventanaLogin.Show();
                this.Close();
            }
        }

        public void Navegar(UserControl pantalla)
        {
            PanelCentral.Content = pantalla;
        }

        /// <summary>Permite que una pantalla hija cambie el contenido central (ej.: padrón → ficha).</summary>
        public static void NavegarDesde(DependencyObject origen, UserControl destino)
        {
            if (Window.GetWindow(origen) is Home home)
                home.Navegar(destino);
        }

        private void Menu_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            Navegar(new UC_Dashboard());
        }

        private void Menu_Censos_Click(object sender, RoutedEventArgs e)
        {
            Navegar(new FormAlta());
        }

        private void Menu_Mapa_Click(object sender, RoutedEventArgs e)
        {
            Navegar(new UC_MapaRiesgo());
        }

        private void Menu_Fondos_Click(object sender, RoutedEventArgs e)
        {
            if (!VerificarAdministrador("Fondos")) return;
            Navegar(new UC_Fondos());
        }

        private void Menu_Configuracion_Click(object sender, RoutedEventArgs e)
        {
            if (!VerificarAdministrador("Configuración")) return;
            Navegar(new UC_Configuracion());
        }

        private static bool VerificarAdministrador(string modulo)
        {
            if (Sesion.EsAdministrador) return true;

            MensajeBox.Mostrar($"El módulo {modulo} está bloqueado para tu rol ({Sesion.Rol}).\n\nSolo los Administradores pueden acceder.",
                               "Acceso restringido", MensajeBox.TipoMensaje.Warning);
            return false;
        }
    }
}
