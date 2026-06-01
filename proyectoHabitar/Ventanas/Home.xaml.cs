using System;
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
        }

      
        private void btnToggleMenu_Click(object sender, RoutedEventArgs e)
        {
            if (_menuAbierto)
            {
                
                Storyboard sb = (Storyboard)this.FindResource("CerrarMenu");
                sb.Begin();
                btnToggleMenu.Content = "☰";
            }
            else
            {
               
                Storyboard sb = (Storyboard)this.FindResource("AbrirMenu");
                sb.Begin();
                btnToggleMenu.Content = "🔀";
            }

         
            _menuAbierto = !_menuAbierto;
        }

 
        private void Menu_Dashboard_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abriendo Dashboard...");
        }

        private void Menu_Censos_Click(object sender, RoutedEventArgs e)
        {
            PanelCentral.Content = new FormAlta();
        }

        private void Menu_Mapa_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abriendo Opcion 2...");
        }

        private void Menu_Algoritmo_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abriendo Opcion 3...");
        }
        private void Menu_Configuracion_Click(object sender, RoutedEventArgs e)
        {
           
            PanelCentral.Content = new UC_Configuracion();
        }
    }
}