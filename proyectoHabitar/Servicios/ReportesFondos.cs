using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using proyectoHabitar.Modelos;
using Colors = QuestPDF.Helpers.Colors;

namespace proyectoHabitar.Servicios
{
    /// <summary>Datos que definen un reporte de caja: período, filtros aplicados y resultados.</summary>
    public class DatosReporteFondos
    {
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string DescripcionFiltros { get; set; } = "";
        public ResumenFondosDto Resumen { get; set; } = new();
        public List<MovimientoDto> Movimientos { get; set; } = new();

        public string Periodo =>
            Desde.HasValue && Hasta.HasValue ? $"{Desde:dd/MM/yyyy} al {Hasta:dd/MM/yyyy}"
            : Desde.HasValue ? $"Desde el {Desde:dd/MM/yyyy}"
            : Hasta.HasValue ? $"Hasta el {Hasta:dd/MM/yyyy}"
            : "Todos los movimientos";
    }

    /// <summary>Reporte de caja en tres formatos: PDF (QuestPDF), impresión directa (FlowDocument) y CSV para Excel.</summary>
    public static class ReportesFondos
    {
        private static readonly CultureInfo AR = new CultureInfo("es-AR");

        public static string Pesos(decimal monto) => monto.ToString("C2", AR);

        // ------------------------------------------------------------------ PDF

        public static void GenerarPdf(string ruta, DatosReporteFondos d)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            Document.Create(doc =>
            {
                doc.Page(pagina =>
                {
                    pagina.Size(PageSizes.A4.Landscape());
                    pagina.Margin(1.5f, Unit.Centimetre);
                    pagina.DefaultTextStyle(t => t.FontSize(9).FontColor(Colors.Grey.Darken4));

                    pagina.Header().Column(col =>
                    {
                        col.Item().Row(fila =>
                        {
                            fila.RelativeItem().Column(c =>
                            {
                                c.Item().Text("HabitAR — Reporte de Caja (Fondos)").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                                c.Item().Text($"Período: {d.Periodo}").FontSize(11).SemiBold();
                                if (!string.IsNullOrWhiteSpace(d.DescripcionFiltros))
                                    c.Item().Text($"Filtros: {d.DescripcionFiltros}").FontSize(9).FontColor(Colors.Grey.Darken1);
                            });
                            fila.ConstantItem(200).AlignRight().Column(c =>
                            {
                                c.Item().AlignRight().Text($"Emitido: {DateTime.Now:dd/MM/yyyy HH:mm}");
                                c.Item().AlignRight().Text($"Por: {Sesion.NombreCompleto}");
                            });
                        });
                        col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    pagina.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Spacing(12);

                        // Tarjetas de resumen
                        col.Item().Row(fila =>
                        {
                            fila.Spacing(8);
                            Tarjeta(fila, "Saldo anterior", Pesos(d.Resumen.SaldoAnterior), Colors.Grey.Darken2);
                            Tarjeta(fila, "Ingresos del período", Pesos(d.Resumen.TotalIngresos), Colors.Green.Darken2);
                            Tarjeta(fila, "Egresos del período", Pesos(d.Resumen.TotalEgresos), Colors.Red.Darken2);
                            Tarjeta(fila, "Saldo al cierre", Pesos(d.Resumen.SaldoFinal), Colors.Blue.Darken2);
                        });

                        // Totales por categoría
                        if (d.Resumen.PorCategoria.Count > 0)
                        {
                            col.Item().Text("Totales por categoría").FontSize(11).Bold();
                            col.Item().Table(tabla =>
                            {
                                tabla.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(70);
                                    c.RelativeColumn();
                                    c.ConstantColumn(70);
                                    c.ConstantColumn(110);
                                });
                                tabla.Header(h =>
                                {
                                    foreach (var titulo in new[] { "Tipo", "Categoría", "Cant.", "Total" })
                                        h.Cell().Background(Colors.Grey.Lighten2).Padding(4).Text(titulo).Bold();
                                });
                                foreach (var c in d.Resumen.PorCategoria)
                                {
                                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Tipo)
                                        .FontColor(c.Tipo == "Ingreso" ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(c.Categoria);
                                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(c.Cantidad.ToString());
                                    tabla.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4).AlignRight().Text(Pesos(c.Total));
                                }
                            });
                        }

                        // Detalle de movimientos
                        col.Item().Text($"Detalle de movimientos ({d.Movimientos.Count})").FontSize(11).Bold();
                        col.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(62);  // Fecha
                                c.ConstantColumn(34);  // Hora
                                c.ConstantColumn(48);  // Tipo
                                c.RelativeColumn(1.3f); // Categoría
                                c.RelativeColumn(2.2f); // Concepto / detalle
                                c.RelativeColumn(1f);   // Medio
                                c.ConstantColumn(60);  // Comprobante
                                c.RelativeColumn(1.2f); // Usuario
                                c.ConstantColumn(85);  // Monto
                            });
                            tabla.Header(h =>
                            {
                                foreach (var titulo in new[] { "Fecha", "Hora", "Tipo", "Categoría", "Concepto / Detalle", "Medio", "Comprob.", "Registró", "Monto" })
                                    h.Cell().Background(Colors.Blue.Lighten4).Padding(3).Text(titulo).Bold();
                            });

                            foreach (var m in d.Movimientos)
                            {
                                string colorTexto = m.Anulado ? Colors.Grey.Medium : Colors.Grey.Darken4;
                                IContainer Celda(IContainer c) => c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);

                                tabla.Cell().Element(Celda).Text(m.FechaHora.ToString("dd/MM/yyyy")).FontColor(colorTexto);
                                tabla.Cell().Element(Celda).Text(m.FechaHora.ToString("HH:mm")).FontColor(colorTexto);
                                tabla.Cell().Element(Celda).Text(m.Anulado ? "ANULADO" : m.Tipo)
                                    .FontColor(m.Anulado ? Colors.Grey.Medium : m.EsIngreso ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                tabla.Cell().Element(Celda).Text(m.Categoria).FontColor(colorTexto);
                                tabla.Cell().Element(Celda).Column(c =>
                                {
                                    c.Item().Text(m.Concepto).FontColor(colorTexto);
                                    if (!string.IsNullOrWhiteSpace(m.Detalle))
                                        c.Item().Text(m.Detalle).FontSize(8).FontColor(Colors.Grey.Darken1);
                                    if (m.Anulado)
                                        c.Item().Text($"Anulado: {m.MotivoAnulacion}").FontSize(8).Italic().FontColor(Colors.Red.Medium);
                                });
                                tabla.Cell().Element(Celda).Text(m.MedioPago).FontColor(colorTexto);
                                tabla.Cell().Element(Celda).Text(m.Comprobante).FontColor(colorTexto);
                                tabla.Cell().Element(Celda).Text(m.UsuarioNombre).FontSize(8).FontColor(colorTexto);
                                var monto = tabla.Cell().Element(Celda).AlignRight().Text(Pesos(m.MontoConSigno))
                                    .FontColor(m.Anulado ? Colors.Grey.Medium : m.EsIngreso ? Colors.Green.Darken2 : Colors.Red.Darken2);
                                if (m.Anulado) monto.Strikethrough();
                            }
                        });

                        col.Item().AlignRight().Text($"Saldo actual de la caja: {Pesos(d.Resumen.SaldoActual)}").FontSize(10).Bold();
                    });

                    pagina.Footer().Row(fila =>
                    {
                        fila.RelativeItem().Text("Sistema HabitAR — Gestión de Viviendas").FontSize(8).FontColor(Colors.Grey.Medium);
                        fila.RelativeItem().AlignRight().Text(t =>
                        {
                            t.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Medium));
                            t.Span("Página ");
                            t.CurrentPageNumber();
                            t.Span(" de ");
                            t.TotalPages();
                        });
                    });
                });
            }).GeneratePdf(ruta);
        }

        private static void Tarjeta(RowDescriptor fila, string titulo, string valor, string color)
        {
            fila.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(c =>
            {
                c.Item().Text(titulo).FontSize(8).FontColor(Colors.Grey.Darken1);
                c.Item().Text(valor).FontSize(13).Bold().FontColor(color);
            });
        }

        // ------------------------------------------------------------------ Impresión

        /// <summary>Documento para PrintDialog (impresión directa sin pasar por el PDF).</summary>
        public static FlowDocument CrearDocumentoImpresion(DatosReporteFondos d, System.Windows.Size areaImprimible)
        {
            var documento = new FlowDocument
            {
                PageWidth = areaImprimible.Width,
                PageHeight = areaImprimible.Height,
                PagePadding = new Thickness(40),
                ColumnWidth = double.PositiveInfinity,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10
            };

            documento.Blocks.Add(new Paragraph(new Run("HabitAR — Reporte de Caja (Fondos)")) { FontSize = 18, FontWeight = FontWeights.Bold, Foreground = Brushes.DarkBlue, Margin = new Thickness(0) });
            documento.Blocks.Add(new Paragraph(new Run($"Período: {d.Periodo}    ·    Emitido: {DateTime.Now:dd/MM/yyyy HH:mm} por {Sesion.NombreCompleto}")) { Margin = new Thickness(0, 2, 0, 0) });
            if (!string.IsNullOrWhiteSpace(d.DescripcionFiltros))
                documento.Blocks.Add(new Paragraph(new Run($"Filtros: {d.DescripcionFiltros}")) { Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0) });

            documento.Blocks.Add(new Paragraph(new Run(
                $"Saldo anterior: {Pesos(d.Resumen.SaldoAnterior)}     Ingresos: {Pesos(d.Resumen.TotalIngresos)}     " +
                $"Egresos: {Pesos(d.Resumen.TotalEgresos)}     Saldo al cierre: {Pesos(d.Resumen.SaldoFinal)}"))
            { FontWeight = FontWeights.SemiBold, FontSize = 11, Margin = new Thickness(0, 10, 0, 10) });

            var tabla = new Table { CellSpacing = 0, BorderBrush = Brushes.LightGray, BorderThickness = new Thickness(0.5) };
            double[] anchos = { 70, 40, 60, 110, 230, 90, 70, 90 };
            foreach (double ancho in anchos) tabla.Columns.Add(new TableColumn { Width = new GridLength(ancho) });

            var grupo = new TableRowGroup();
            tabla.RowGroups.Add(grupo);

            var encabezado = new TableRow { Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xDB, 0xEA, 0xFE)), FontWeight = FontWeights.Bold };
            foreach (var titulo in new[] { "Fecha", "Hora", "Tipo", "Categoría", "Concepto / Detalle", "Medio", "Comprob.", "Monto" })
                encabezado.Cells.Add(CeldaTexto(titulo));
            grupo.Rows.Add(encabezado);

            foreach (var m in d.Movimientos)
            {
                Brush color = m.Anulado ? Brushes.Gray : m.EsIngreso ? Brushes.DarkGreen : Brushes.DarkRed;
                string concepto = m.Concepto + (string.IsNullOrWhiteSpace(m.Detalle) ? "" : $" — {m.Detalle}") + (m.Anulado ? $" [ANULADO: {m.MotivoAnulacion}]" : "");

                var fila = new TableRow();
                fila.Cells.Add(CeldaTexto(m.FechaHora.ToString("dd/MM/yyyy")));
                fila.Cells.Add(CeldaTexto(m.FechaHora.ToString("HH:mm")));
                fila.Cells.Add(CeldaTexto(m.Anulado ? "Anulado" : m.Tipo, color));
                fila.Cells.Add(CeldaTexto(m.Categoria));
                fila.Cells.Add(CeldaTexto(concepto));
                fila.Cells.Add(CeldaTexto(m.MedioPago));
                fila.Cells.Add(CeldaTexto(m.Comprobante));
                fila.Cells.Add(CeldaTexto(Pesos(m.MontoConSigno), color, TextAlignment.Right));
                grupo.Rows.Add(fila);
            }

            documento.Blocks.Add(tabla);
            documento.Blocks.Add(new Paragraph(new Run($"Saldo actual de la caja: {Pesos(d.Resumen.SaldoActual)}"))
            { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Right, Margin = new Thickness(0, 10, 0, 0) });

            return documento;
        }

        private static TableCell CeldaTexto(string texto, Brush? color = null, TextAlignment alineacion = TextAlignment.Left)
        {
            return new TableCell(new Paragraph(new Run(texto)) { Margin = new Thickness(0), TextAlignment = alineacion, Foreground = color ?? Brushes.Black })
            {
                Padding = new Thickness(4, 2, 4, 2),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0, 0, 0, 0.5)
            };
        }

        // ------------------------------------------------------------------ CSV (Excel)

        /// <summary>CSV separado por ";" y con BOM UTF-8: Excel en español lo abre directo en columnas y con acentos.</summary>
        public static void ExportarCsv(string ruta, DatosReporteFondos d)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Fecha;Hora;Tipo;Categoría;Concepto;Detalle;Medio de pago;Comprobante;Monto;Registró;Estado;Motivo anulación");

            foreach (var m in d.Movimientos)
            {
                sb.AppendLine(string.Join(";", new[]
                {
                    m.FechaHora.ToString("dd/MM/yyyy"),
                    m.FechaHora.ToString("HH:mm"),
                    m.Tipo,
                    m.Categoria,
                    m.Concepto,
                    m.Detalle,
                    m.MedioPago,
                    m.Comprobante,
                    m.MontoConSigno.ToString("0.00", AR),
                    m.UsuarioNombre,
                    m.Estado,
                    m.MotivoAnulacion
                }.Select(Csv)));
            }

            sb.AppendLine();
            sb.AppendLine($"Período;{Csv(d.Periodo)}");
            sb.AppendLine($"Saldo anterior;{d.Resumen.SaldoAnterior.ToString("0.00", AR)}");
            sb.AppendLine($"Total ingresos;{d.Resumen.TotalIngresos.ToString("0.00", AR)}");
            sb.AppendLine($"Total egresos;{d.Resumen.TotalEgresos.ToString("0.00", AR)}");
            sb.AppendLine($"Saldo al cierre;{d.Resumen.SaldoFinal.ToString("0.00", AR)}");

            File.WriteAllText(ruta, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        private static string Csv(string? valor)
        {
            valor ??= "";
            // Evita que Excel interprete textos como fórmulas.
            if (valor.Length > 0 && "=+-@".Contains(valor[0]) && !decimal.TryParse(valor, NumberStyles.Number, AR, out _))
                valor = "'" + valor;
            return valor.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0
                ? "\"" + valor.Replace("\"", "\"\"") + "\""
                : valor;
        }
    }
}
