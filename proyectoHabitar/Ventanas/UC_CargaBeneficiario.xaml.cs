using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using proyectoHabitar.Modelos;
using proyectoHabitar.Servicios;

namespace proyectoHabitar.Ventanas
{
    public partial class UC_CargaBeneficiario : UserControl
    {
        private static readonly CultureInfo CulturaAR = new CultureInfo("es-AR");

        private readonly int _idBeneficiario = 0;
        private bool _ubicacionAproximada = false;
        private bool _cargandoDatos = false;

        public UC_CargaBeneficiario() : this(0)
        {
        }

        public UC_CargaBeneficiario(int idBeneficiario)
        {
            InitializeComponent();
            AsignarValidadoresDeTipeo();
            _idBeneficiario = idBeneficiario;

            dpTitularFechaNac.DisplayDateEnd = DateTime.Today;

            if (_idBeneficiario > 0)
            {
                btnGuardarCenso.Content = "💾 Actualizar Relevamiento";
                CargarDatosParaEdicion();
            }
        }

        private void AsignarValidadoresDeTipeo()
        {
            txtTitularDni.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtTitularCuil.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtTitularTelefono.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtTitularCodigoPostal.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtCantConvivientes.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtCantHijos.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtCantAdultosMayores.PreviewTextInput += SoloNumeros_PreviewTextInput;
            txtTitularIngresos.PreviewTextInput += SoloNumerosYDecimales_PreviewTextInput;

            txtTitularApellido.PreviewTextInput += SoloLetrasYEspacios_PreviewTextInput;
            txtTitularNombre.PreviewTextInput += SoloLetrasYEspacios_PreviewTextInput;
            txtTitularLocalidad.PreviewTextInput += SoloLetrasYEspacios_PreviewTextInput;

            txtTitularDni.PreviewKeyDown += BloquearEspacio_PreviewKeyDown;
            txtTitularCuil.PreviewKeyDown += BloquearEspacio_PreviewKeyDown;
            txtTitularTelefono.PreviewKeyDown += BloquearEspacio_PreviewKeyDown;
            txtTitularCodigoPostal.PreviewKeyDown += BloquearEspacio_PreviewKeyDown;

            txtTitularDni.MaxLength = 8;
            txtTitularCuil.MaxLength = 11;

            // Si se edita a mano la coordenada, deja de ser "aproximada".
            txtLatitud.TextChanged += (s, e) => { if (!_cargandoDatos) _ubicacionAproximada = false; };
            txtLongitud.TextChanged += (s, e) => { if (!_cargandoDatos) _ubicacionAproximada = false; };
        }

        private void SoloNumeros_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, "[^0-9]");
        }

        private void SoloNumerosYDecimales_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            e.Handled = Regex.IsMatch(e.Text, "[^0-9,]") || (e.Text == "," && textBox.Text.Contains(","));
        }

        private void SoloLetrasYEspacios_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = Regex.IsMatch(e.Text, @"[^a-zA-ZáéíóúÁÉÍÓÚüÜñÑ' ]");
        }

        private void BloquearEspacio_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space) e.Handled = true;
        }

        private async void CargarDatosParaEdicion()
        {
            try
            {
                BeneficiarioDto b = await ApiClient.GetAsync<BeneficiarioDto>($"api/Beneficiarios/{_idBeneficiario}");
                _cargandoDatos = true;

                txtTitularDni.Text = b.NumeroDocumento;
                txtTitularCuil.Text = b.Cuil;
                txtTitularApellido.Text = b.Apellido;
                txtTitularNombre.Text = b.Nombre;
                dpTitularFechaNac.SelectedDate = b.FechaNacimiento;

                SeleccionarItemCombo(cmbTitularGenero, b.Genero);
                SeleccionarItemCombo(cmbTitularEstadoCivil, b.EstadoCivil);
                SeleccionarItemCombo(cmbTitularNivelEducativo, b.NivelEducativo);
                SeleccionarItemCombo(cmbTitularCondicionLaboral, b.CondicionLaboral);

                txtTitularTelefono.Text = b.TelefonoCelular;
                txtTitularEmail.Text = b.Email;
                txtTitularDomicilio.Text = b.Domicilio;
                txtTitularLocalidad.Text = b.Localidad;
                txtTitularCodigoPostal.Text = b.CodigoPostal;

                txtTitularIngresos.Text = b.IngresosMensuales.ToString("0.##", CulturaAR);

                if (!b.PoseePlanSocial || string.IsNullOrEmpty(b.OrganismoPlanSocial)) cmbTitularPlanSocial.SelectedIndex = 0;
                else SeleccionarItemCombo(cmbTitularPlanSocial, b.OrganismoPlanSocial);

                txtCantConvivientes.Text = b.CantidadConvivientes.ToString();
                txtCantHijos.Text = b.CantidadHijosMenores.ToString();
                txtCantAdultosMayores.Text = b.CantidadAdultosMayores.ToString();
                chkDiscapacidad.IsChecked = b.TieneIntegrantesDiscapacidad;

                SeleccionarItemCombo(cmbTipoVivienda, b.TipoViviendaOriginal);
                SeleccionarItemCombo(cmbProcedenciaAgua, b.ProcedenciaAgua);
                SeleccionarItemCombo(cmbDesagueInodoro, b.DesagueInodoro);
                SeleccionarItemCombo(cmbTenenciaTerreno, b.RegimenTenenciaTerreno);
                SeleccionarItemCombo(cmbMaterialPiso, b.MaterialPredominantePiso);
                SeleccionarItemCombo(cmbMaterialTecho, b.MaterialPredominanteTecho);

                if (b.Latitud.HasValue && b.Longitud.HasValue)
                {
                    txtLatitud.Text = b.Latitud.Value.ToString("0.######", CultureInfo.InvariantCulture);
                    txtLongitud.Text = b.Longitud.Value.ToString("0.######", CultureInfo.InvariantCulture);
                    _ubicacionAproximada = b.UbicacionAproximada;
                    txtEstadoUbicacion.Text = b.UbicacionAproximada
                        ? "⚠️ Ubicación aproximada (centro de la localidad). Si podés, corregila con las coordenadas exactas."
                        : "✅ Ubicación cargada.";
                }

                txtTitularDni.IsEnabled = false;
                txtTitularCuil.IsEnabled = false;
            }
            catch (ApiException ex)
            {
                MensajeBox.Mostrar($"Error al recuperar la ficha socioambiental: {ex.Message}", "Error de Conexión", MensajeBox.TipoMensaje.Error);
            }
            finally
            {
                _cargandoDatos = false;
            }
        }

        private void SeleccionarItemCombo(ComboBox combo, string valorBuscado)
        {
            if (string.IsNullOrEmpty(valorBuscado)) return;
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (combo.Items[i] is ComboBoxItem item && item.Content.ToString()!.Trim().ToLower() == valorBuscado.Trim().ToLower())
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private static string Seleccion(ComboBox combo) =>
            (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

        // ------------------------------------------------------------ ubicación

        private async void btnUbicar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitularLocalidad.Text))
            {
                MensajeBox.Mostrar("Completá al menos la localidad (y si podés el domicilio) para buscar la ubicación.", "Falta la localidad", MensajeBox.TipoMensaje.Warning);
                return;
            }

            btnUbicar.IsEnabled = false;
            txtEstadoUbicacion.Text = "Buscando en el mapa...";
            try
            {
                string url = "api/Beneficiarios/geocodificar" +
                             $"?domicilio={Uri.EscapeDataString(txtTitularDomicilio.Text.Trim())}" +
                             $"&localidad={Uri.EscapeDataString(txtTitularLocalidad.Text.Trim())}" +
                             "&provincia=Tucum%C3%A1n";
                GeocodificacionDto r = await ApiClient.GetAsync<GeocodificacionDto>(url);

                _cargandoDatos = true;
                txtLatitud.Text = r.Latitud.ToString("0.######", CultureInfo.InvariantCulture);
                txtLongitud.Text = r.Longitud.ToString("0.######", CultureInfo.InvariantCulture);
                _cargandoDatos = false;
                _ubicacionAproximada = r.Aproximada;

                txtEstadoUbicacion.Text = r.Aproximada
                    ? $"⚠️ No se encontró la calle exacta; se ubicó en el centro de la localidad.\nEncontrado: {r.DireccionEncontrada}\nRevisalo con \"Ver en Google Maps\" y, si hace falta, pegá las coordenadas exactas."
                    : $"✅ Encontrado: {r.DireccionEncontrada}\nVerificalo con \"Ver en Google Maps\".";
            }
            catch (ApiException ex)
            {
                txtEstadoUbicacion.Text = ex.Estado == HttpStatusCode.NotFound
                    ? "❌ " + ex.Message
                    : "❌ No se pudo consultar el mapa: " + ex.Message;
            }
            finally
            {
                _cargandoDatos = false;
                btnUbicar.IsEnabled = true;
            }
        }

        private void btnVerGoogleMaps_Click(object sender, RoutedEventArgs e)
        {
            var (lat, lng, _) = LeerCoordenadas();
            string direccion = $"{txtTitularDomicilio.Text.Trim()}, {txtTitularLocalidad.Text.Trim()}, Tucumán, Argentina";
            Mapas.AbrirEnGoogleMaps(lat, lng, direccion);
        }

        /// <summary>Permite pegar "-26.83, -65.20" (como lo copia Google Maps) en cualquiera de los dos campos.</summary>
        private void Coordenadas_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox origen) return;

            var partes = origen.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 2
                && double.TryParse(partes[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double lat)
                && double.TryParse(partes[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double lng))
            {
                txtLatitud.Text = lat.ToString("0.######", CultureInfo.InvariantCulture);
                txtLongitud.Text = lng.ToString("0.######", CultureInfo.InvariantCulture);
                txtEstadoUbicacion.Text = "✅ Coordenadas cargadas a mano.";
            }
        }

        /// <summary>Devuelve las coordenadas cargadas, o un error si están a medias o fuera de rango.</summary>
        private (double? Lat, double? Lng, string? Error) LeerCoordenadas()
        {
            string latTxt = txtLatitud.Text.Trim().Replace(',', '.');
            string lngTxt = txtLongitud.Text.Trim().Replace(',', '.');

            if (latTxt == "" && lngTxt == "") return (null, null, null);
            if (latTxt == "" || lngTxt == "") return (null, null, "Cargá latitud y longitud juntas (o dejá ambas vacías).");

            if (!double.TryParse(latTxt, NumberStyles.Float, CultureInfo.InvariantCulture, out double lat) || lat < -90 || lat > 90)
                return (null, null, "La latitud no es válida (ej: -26.83241).");
            if (!double.TryParse(lngTxt, NumberStyles.Float, CultureInfo.InvariantCulture, out double lng) || lng < -180 || lng > 180)
                return (null, null, "La longitud no es válida (ej: -65.20380).");

            return (lat, lng, null);
        }

        // ------------------------------------------------------------ guardar

        /// <summary>
        /// Valida todo antes de mandar. Ya no se completan combos vacíos con la "peor" opción:
        /// eso le sumaba puntos de criticidad a fichas incompletas (una ficha vacía quedaba en Amarillo).
        /// </summary>
        private List<string> Validar()
        {
            var errores = new List<string>();

            if (!Regex.IsMatch(txtTitularDni.Text.Trim(), @"^\d{7,8}$")) errores.Add("DNI: debe tener 7 u 8 dígitos.");
            if (!Regex.IsMatch(txtTitularCuil.Text.Trim(), @"^\d{11}$")) errores.Add("CUIL: debe tener 11 dígitos, sin guiones.");
            else if (txtTitularDni.Text.Trim().Length >= 7 && !txtTitularCuil.Text.Trim().Contains(txtTitularDni.Text.Trim()))
                errores.Add("CUIL: no coincide con el DNI cargado.");
            if (string.IsNullOrWhiteSpace(txtTitularApellido.Text)) errores.Add("Apellido.");
            if (string.IsNullOrWhiteSpace(txtTitularNombre.Text)) errores.Add("Nombre.");
            if (dpTitularFechaNac.SelectedDate == null) errores.Add("Fecha de nacimiento.");
            else if (dpTitularFechaNac.SelectedDate > DateTime.Today) errores.Add("Fecha de nacimiento: no puede ser futura.");

            if (string.IsNullOrWhiteSpace(txtTitularDomicilio.Text)) errores.Add("Domicilio.");
            if (string.IsNullOrWhiteSpace(txtTitularLocalidad.Text)) errores.Add("Localidad.");
            if (string.IsNullOrWhiteSpace(txtTitularCodigoPostal.Text)) errores.Add("Código postal.");

            string email = txtTitularEmail.Text.Trim();
            if (email != "" && !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) errores.Add("Email: formato inválido.");

            var combos = new (ComboBox Combo, string Nombre)[]
            {
                (cmbTitularGenero, "Identidad de género"),
                (cmbTitularEstadoCivil, "Estado civil"),
                (cmbTitularNivelEducativo, "Nivel educativo"),
                (cmbTitularCondicionLaboral, "Condición laboral"),
                (cmbTipoVivienda, "Tipo de vivienda"),
                (cmbTenenciaTerreno, "Régimen de tenencia del terreno"),
                (cmbMaterialPiso, "Material del piso"),
                (cmbMaterialTecho, "Material del techo"),
                (cmbProcedenciaAgua, "Procedencia del agua"),
                (cmbDesagueInodoro, "Desagüe / inodoro")
            };
            errores.AddRange(combos.Where(c => c.Combo.SelectedItem == null).Select(c => $"{c.Nombre}: seleccioná una opción."));

            if (!string.IsNullOrWhiteSpace(txtTitularIngresos.Text) && !decimal.TryParse(txtTitularIngresos.Text, NumberStyles.Number, CulturaAR, out _))
                errores.Add("Ingresos mensuales: número inválido.");

            int.TryParse(txtCantConvivientes.Text, out int convivientes);
            int.TryParse(txtCantHijos.Text, out int hijos);
            int.TryParse(txtCantAdultosMayores.Text, out int mayores);
            if (convivientes < 1) errores.Add("Total de integrantes del hogar: mínimo 1.");
            else if (hijos + mayores > convivientes) errores.Add("Hijos menores + adultos mayores no puede superar el total de integrantes.");

            string? errorCoordenadas = LeerCoordenadas().Error;
            if (errorCoordenadas != null) errores.Add(errorCoordenadas);

            return errores;
        }

        private async void btnGuardarCenso_Click(object sender, RoutedEventArgs e)
        {
            var errores = Validar();
            if (errores.Count > 0)
            {
                MensajeBox.Mostrar("Revisá estos datos antes de guardar:\n\n• " + string.Join("\n• ", errores), "Campos Requeridos", MensajeBox.TipoMensaje.Warning);
                return;
            }

            int.TryParse(txtCantConvivientes.Text, out int convivientes);
            int.TryParse(txtCantHijos.Text, out int hijos);
            int.TryParse(txtCantAdultosMayores.Text, out int mayores);
            decimal.TryParse(txtTitularIngresos.Text, NumberStyles.Number, CulturaAR, out decimal ingresos);
            var (lat, lng, _) = LeerCoordenadas();

            var payload = new BeneficiarioDto
            {
                Id = _idBeneficiario,
                Cuil = txtTitularCuil.Text.Trim(),
                NumeroDocumento = txtTitularDni.Text.Trim(),
                Apellido = txtTitularApellido.Text.Trim(),
                Nombre = txtTitularNombre.Text.Trim(),
                FechaNacimiento = dpTitularFechaNac.SelectedDate!.Value.Date,
                Genero = Seleccion(cmbTitularGenero),
                EstadoCivil = Seleccion(cmbTitularEstadoCivil),
                TelefonoCelular = txtTitularTelefono.Text.Trim(),
                Email = txtTitularEmail.Text.Trim(),
                Domicilio = txtTitularDomicilio.Text.Trim(),
                Localidad = txtTitularLocalidad.Text.Trim(),
                Provincia = "Tucumán",
                CodigoPostal = txtTitularCodigoPostal.Text.Trim(),
                NivelEducativo = Seleccion(cmbTitularNivelEducativo),
                CondicionLaboral = Seleccion(cmbTitularCondicionLaboral),
                IngresosMensuales = ingresos,
                PoseePlanSocial = cmbTitularPlanSocial.SelectedIndex > 0,
                OrganismoPlanSocial = cmbTitularPlanSocial.SelectedIndex > 0 ? Seleccion(cmbTitularPlanSocial) : "",
                CantidadConvivientes = convivientes,
                CantidadHijosMenores = hijos,
                CantidadAdultosMayores = mayores,
                TieneIntegrantesDiscapacidad = chkDiscapacidad.IsChecked ?? false,
                TipoViviendaOriginal = Seleccion(cmbTipoVivienda),
                RegimenTenenciaTerreno = Seleccion(cmbTenenciaTerreno),
                MaterialPredominantePiso = Seleccion(cmbMaterialPiso),
                MaterialPredominanteTecho = Seleccion(cmbMaterialTecho),
                ProcedenciaAgua = Seleccion(cmbProcedenciaAgua),
                DesagueInodoro = Seleccion(cmbDesagueInodoro),
                Latitud = lat,
                Longitud = lng,
                UbicacionAproximada = lat.HasValue && _ubicacionAproximada
            };

            try
            {
                btnGuardarCenso.IsEnabled = false;

                RespuestaApi respuesta = _idBeneficiario > 0
                    ? await ApiClient.PutAsync($"api/Beneficiarios/{_idBeneficiario}", payload)
                    : await ApiClient.PostAsync("api/Beneficiarios", payload);

                string aviso = lat.HasValue ? "" : "\n\nℹ️ No se cargó la ubicación: esta familia no aparecerá en el Mapa de Riesgo hasta que se la ubique.";
                MensajeBox.Mostrar($"{respuesta.Mensaje}\n\nPrioridad asignada: {respuesta.PrioridadAsignada.ToUpper()} ({respuesta.PuntosCalculados} puntos).{aviso}",
                                   "Operación Exitosa", MensajeBox.TipoMensaje.Info);
                RegresarAlPadron();
            }
            catch (ApiException ex)
            {
                // El servidor ya explica el motivo (DNI/CUIL repetido, dato inválido, etc.).
                string titulo = ex.Estado == HttpStatusCode.Conflict ? "DNI o CUIL ya registrado" : "No se pudo guardar";
                MensajeBox.Mostrar(ex.Message, titulo, MensajeBox.TipoMensaje.Error);
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
            Home.NavegarDesde(this, new FormAlta());
        }
    }
}
