using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class UC_Dashboard : UserControl
    {
        public UC_Dashboard()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CargarMetricasDesdeAPI();
        }

        private async void CargarMetricasDesdeAPI()
        {
            try
            {
                List<BeneficiarioListaDto> lista = await ApiClient.GetAsync<List<BeneficiarioListaDto>>("api/Beneficiarios");

                int total = lista.Count;
                int rojos = lista.Count(b => string.Equals(b.ColorSemaforo, "Rojo", StringComparison.OrdinalIgnoreCase));
                int amarillos = lista.Count(b => string.Equals(b.ColorSemaforo, "Amarillo", StringComparison.OrdinalIgnoreCase));
                int verdes = lista.Count(b => string.Equals(b.ColorSemaforo, "Verde", StringComparison.OrdinalIgnoreCase));

                lblTotalCensados.Text = total.ToString();
                lblVulnerabilidadAlta.Text = rojos.ToString();
                lblVulnerabilidadMedia.Text = amarillos.ToString();
                lblVulnerabilidadBaja.Text = verdes.ToString();

                lblValorBarraRojo.Text = rojos.ToString();
                lblValorBarraAmarillo.Text = amarillos.ToString();
                lblValorBarraVerde.Text = verdes.ToString();

                double maxValor = Math.Max(1, Math.Max(rojos, Math.Max(amarillos, verdes)));
                double alturaMaximaGrafico = 120.0;

                barraRojo.Height = Math.Max(8, (rojos / maxValor) * alturaMaximaGrafico);
                barraAmarillo.Height = Math.Max(8, (amarillos / maxValor) * alturaMaximaGrafico);
                barraVerde.Height = Math.Max(8, (verdes / maxValor) * alturaMaximaGrafico);

                double porcRojo = total > 0 ? (rojos * 100.0 / total) : 0;
                double porcAmarillo = total > 0 ? (amarillos * 100.0 / total) : 0;
                double porcVerde = total > 0 ? (verdes * 100.0 / total) : 0;

                txtPorcentajeRojo.Text = $"Alto: {rojos} ({porcRojo:F1}%)";
                txtPorcentajeAmarillo.Text = $"Medio: {amarillos} ({porcAmarillo:F1}%)";
                txtPorcentajeVerde.Text = $"Bajo: {verdes} ({porcVerde:F1}%)";

                lblDonaTotal.Text = total.ToString();
                DibujarDona((rojos, "#EF4444"), (amarillos, "#F59E0B"), (verdes, "#10B981"));
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"Error al cargar métricas del dashboard:\n\n{ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
        }

        /// <summary>Dibuja el anillo "Proporción de Criticidad" con un arco por color, empezando arriba y en sentido horario.</summary>
        private void DibujarDona(params (int Cantidad, string Color)[] segmentos)
        {
            canvasDona.Children.Clear();

            int total = segmentos.Sum(s => s.Cantidad);
            if (total == 0) return;

            const double centro = 50, radio = 45, grosor = 12;
            double anguloInicio = -90;

            foreach (var (cantidad, color) in segmentos)
            {
                if (cantidad == 0) continue;

                var pincel = (Brush)new BrushConverter().ConvertFromString(color)!;
                double barrido = 360.0 * cantidad / total;

                if (barrido >= 359.99)
                {
                    var anillo = new Ellipse { Width = radio * 2, Height = radio * 2, Stroke = pincel, StrokeThickness = grosor };
                    Canvas.SetLeft(anillo, centro - radio);
                    Canvas.SetTop(anillo, centro - radio);
                    canvasDona.Children.Add(anillo);
                    return;
                }

                Point inicio = PuntoEnCirculo(centro, radio, anguloInicio);
                Point fin = PuntoEnCirculo(centro, radio, anguloInicio + barrido);

                var figura = new PathFigure { StartPoint = inicio, IsClosed = false };
                figura.Segments.Add(new ArcSegment(fin, new Size(radio, radio), 0, barrido > 180, SweepDirection.Clockwise, true));

                canvasDona.Children.Add(new Path
                {
                    Data = new PathGeometry(new[] { figura }),
                    Stroke = pincel,
                    StrokeThickness = grosor,
                    ToolTip = $"{cantidad} ({100.0 * cantidad / total:F1}%)"
                });

                anguloInicio += barrido;
            }
        }

        private static Point PuntoEnCirculo(double centro, double radio, double anguloGrados)
        {
            double rad = anguloGrados * Math.PI / 180.0;
            return new Point(centro + radio * Math.Cos(rad), centro + radio * Math.Sin(rad));
        }
    }
}
