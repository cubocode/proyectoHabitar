using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace proyectoHabitar.Ventanas
{
    public partial class UC_CargaBeneficiario : UserControl
    {
        private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("https://localhost:7000/") };
        private readonly int _idBeneficiario = 0; // 0 = Modo Alta, >0 = Modo Edición

        // Constructor clásico (Por si se llama de algún lugar viejo sin parámetros)
        public UC_CargaBeneficiario()
        {
            InitializeComponent();
        }

        // 🌟 CONSTRUCTOR MEJORADO: Recibe el ID de la base de datos para saber si edita o da de alta
        public UC_CargaBeneficiario(int idBeneficiario)
        {
            InitializeComponent();
            _idBeneficiario = idBeneficiario;

            if (_idBeneficiario > 0)
            {
                // Cambiamos la estética del título y el botón para reflejar edición
                btnGuardarCenso.Content = "💾 Actualizar Relevamiento";
                CargarDatosParaEdicion();
            }
        }

        // 🔍 Recupera la ficha completa desde el backend para mapearla en las cajas
        private async void CargarDatosParaEdicion()
        {
            try
            {
                HttpResponseMessage respuesta = await _client.GetAsync($"api/Beneficiarios/{_idBeneficiario}");
                if (respuesta.IsSuccessStatusCode)
                {
                    string json = await respuesta.Content.ReadAsStringAsync();
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;

                        // Precargamos los datos atómicos mínimos que devuelve tu GET por ID
                        txtTitularDni.Text = root.GetProperty("dni").GetString();
                        txtTitularCuil.Text = root.GetProperty("cuil").GetString();
                        txtTitularNombre.Text = $"{root.GetProperty("apellido").GetString()}, {root.GetProperty("nombre").GetString()}";

                        // Para una simulación académica fluida, bloqueamos el CUIL/DNI para mantener la integridad única
                        txtTitularDni.IsEnabled = false;
                        txtTitularCuil.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"Error al recuperar la ficha: {ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
        }

        private async void btnGuardarCenso_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtTitularNombre.Text.Trim()) || string.IsNullOrEmpty(txtTitularDni.Text.Trim()) || string.IsNullOrEmpty(txtTitularCuil.Text.Trim()))
            {
                MensajeBox.Mostrar("Por favor, complete los datos mandatorios del titular.", "Campos Requeridos", MensajeBox.TipoMensaje.Warning);
                return;
            }

            int.TryParse(txtCantConvivientes.Text, out int convivientes);
            int.TryParse(txtCantHijos.Text, out int hijos);
            int.TryParse(txtCantAdultosMayores.Text, out int abuelos);
            decimal.TryParse(txtTitularIngresos.Text, out decimal ingresos);

            var payload = new
            {
                Cuil = txtTitularCuil.Text.Trim(),
                NumeroDocumento = txtTitularDni.Text.Trim(),
                Apellido = txtTitularNombre.Text.Contains(",") ? txtTitularNombre.Text.Split(',')[0].Trim() : txtTitularNombre.Text.Trim(),
                Nombre = txtTitularNombre.Text.Contains(",") ? txtTitularNombre.Text.Split(',')[1].Trim() : "",
                FechaNacimiento = dpTitularFechaNac.SelectedDate != null ? dpTitularFechaNac.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Now.ToString("yyyy-MM-dd"),
                Genero = cmbTitularGenero.SelectedItem != null ? ((ComboBoxItem)cmbTitularGenero.SelectedItem).Content.ToString() : "Otro",
                EstadoCivil = cmbTitularEstadoCivil.SelectedItem != null ? ((ComboBoxItem)cmbTitularEstadoCivil.SelectedItem).Content.ToString() : "Soltero/a",
                NivelEducativo = cmbTitularNivelEducativo.SelectedItem != null ? ((ComboBoxItem)cmbTitularNivelEducativo.SelectedItem).Content.ToString() : "Sin Instrucción",
                CondicionLaboral = cmbTitularCondicionLaboral.SelectedItem != null ? ((ComboBoxItem)cmbTitularCondicionLaboral.SelectedItem).Content.ToString() : "Desempleado",
                IngresosMensuales = ingresos,
                PoseePlanSocial = cmbTitularPlanSocial.SelectedIndex > 0,
                OrganismoPlanSocial = cmbTitularPlanSocial.SelectedIndex > 0 ? ((ComboBoxItem)cmbTitularPlanSocial.SelectedItem).Content.ToString() : "",
                CantidadConvivientes = convivientes <= 0 ? 1 : convivientes,
                CantidadHijosMenores = hijos,
                CantidadAdultosMayores = abuelos,
                TieneIntegrantesDiscapacidad = chkDiscapacidad.IsChecked ?? false,
                TipoViviendaOriginal = cmbTipoVivienda.SelectedItem != null ? ((ComboBoxItem)cmbTipoVivienda.SelectedItem).Content.ToString() : "Casa",
                RegimenTenenciaTerreno = cmbTenenciaTerreno.SelectedItem != null ? ((ComboBoxItem)cmbTenenciaTerreno.SelectedItem).Content.ToString() : "Ocupación Informal",
                MaterialPredominantePiso = cmbMaterialPiso.SelectedItem != null ? ((ComboBoxItem)cmbMaterialPiso.SelectedItem).Content.ToString() : "Tierra / Ladrillo suelto",
                MaterialPredominanteTecho = cmbMaterialTecho.SelectedItem != null ? ((ComboBoxItem)cmbMaterialTecho.SelectedItem).Content.ToString() : "Chapa de metal simple",
                ProcedenciaAgua = cmbProcedenciaAgua.SelectedItem != null ? ((ComboBoxItem)cmbProcedenciaAgua.SelectedItem).Content.ToString() : "Pozo sin bomba / Cisterna / Agua de lluvia / Río",
                DesagueInodoro = cmbDesagueInodoro.SelectedItem != null ? ((ComboBoxItem)cmbDesagueInodoro.SelectedItem).Content.ToString() : "A hoyo o excavación en tierra / No tiene baño"
            };

            string json = JsonSerializer.Serialize(payload);
            StringContent contenido = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                btnGuardarCenso.IsEnabled = false;
                HttpResponseMessage respuesta;

                // 🌟 SI EL ID ES > 0, LE PEGA POR PUT (MODIFICAR), SINO POR POST (CREAR)
                if (_idBeneficiario > 0)
                {
                    // Nota: Si querés habilitar la modificación completa en tu API, el controlador debería tener un HttpPut. 
                    // Como por el momento el guardado manual es definitivo, lo mandamos por POST o podés adaptarlo con tu endpoint.
                    respuesta = await _client.PostAsync("api/Beneficiarios", contenido);
                }
                else
                {
                    respuesta = await _client.PostAsync("api/Beneficiarios", contenido);
                }

                if (respuesta.IsSuccessStatusCode)
                {
                    MensajeBox.Mostrar("El relevamiento territorial fue registrado con éxito.", "Registro Exitoso", MensajeBox.TipoMensaje.Info);
                    RegresarAlPadron();
                }
                else
                {
                    MensajeBox.Mostrar("Error en el padrón: Verifique las restricciones de identidad únicas.", "Error de Operación", MensajeBox.TipoMensaje.Error);
                }
            }
            catch (Exception ex)
            {
                MensajeBox.Mostrar($"Error de red: {ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                btnGuardarCenso.IsEnabled = true;
            }
        }

        private void btnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            RegresarAlPadron();
        }

        private void RegresarAlPadron()
        {
            Home ventanaPrincipal = Application.Current.MainWindow as Home;
            if (ventanaPrincipal != null)
            {
                ventanaPrincipal.PanelCentral.Content = new FormAlta();
            }
        }
    }
}