using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Markup;
using proyectoHabitar.Ventanas;

namespace proyectoHabitar
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Formato argentino en toda la app: $ 1.234,56 y fechas dd/MM/yyyy, también en los Binding de XAML.
            var cultura = new CultureInfo("es-AR");
            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;
            CultureInfo.DefaultThreadCurrentCulture = cultura;
            CultureInfo.DefaultThreadCurrentUICulture = cultura;
            FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));
            // Los <Run> (dentro de un TextBlock) son FrameworkContentElement y no heredan lo anterior.
            FrameworkContentElement.LanguageProperty.OverrideMetadata(typeof(System.Windows.Documents.TextElement),
                new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(cultura.IetfLanguageTag)));

            // Último recurso: un error inesperado se muestra en lugar de cerrar la app.
            DispatcherUnhandledException += (s, args) =>
            {
                MensajeBox.Mostrar($"Ocurrió un error inesperado:\n\n{args.Exception.Message}", "Error", MensajeBox.TipoMensaje.Error);
                args.Handled = true;
            };

            base.OnStartup(e);
        }
    }
}
