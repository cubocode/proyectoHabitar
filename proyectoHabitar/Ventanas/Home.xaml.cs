using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace proyectoHabitar.Ventanas
{
    public partial class Home : Window
    {
        private bool _menuAbierto = false;

        public Home()
        {
            InitializeComponent();

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
            // Disparamos tu MensajeBox personalizado de HabitAR con el formato Sí/No
            bool seguro = MensajeBox.Mostrar("¿Estás seguro de que querés cerrar sesión?", "Cerrar Sesión", MensajeBox.TipoMensaje.QuerySiNo);

            if (seguro)
            {
                // Congelamos el entorno del Home e iniciamos la carga de salida
                PanelCentral.Visibility = Visibility.Collapsed;
                GridMenuLateral.IsEnabled = false;

                panelCierreCarga.Visibility = Visibility.Visible;

                // Espera asíncrona simulada de 2.2 segundos
                await Task.Delay(2200);

                // Destruimos la ventana y volvemos al Login
                proyectoHabitar.Ventanas.Login ventanaLogin = new proyectoHabitar.Ventanas.Login();
                ventanaLogin.Show();
                this.Close();
            }
        }

        private void Menu_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            PanelCentral.Content = new UC_Dashboard();
        }

        private void Menu_Censos_Click(object sender, RoutedEventArgs e)
        {
            PanelCentral.Content = new FormAlta();
        }

        private void Menu_Mapa_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abriendo Mapa de Riesgo...");
        }

        private void Menu_Algoritmo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abriendo Algoritmo de Priorización...");
        }

        private void Menu_Configuracion_Click(object sender, RoutedEventArgs e)
        {
            PanelCentral.Content = new UC_Configuracion();
        }
    }
}