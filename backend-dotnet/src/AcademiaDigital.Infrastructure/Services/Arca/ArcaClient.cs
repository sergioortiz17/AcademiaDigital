using AcademiaDigital.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace AcademiaDigital.Infrastructure.Services.Arca;

/// <summary>
/// Adaptador de <see cref="IArcaClient"/> contra los web services SOAP de ARCA (ex AFIP): WSAA para
/// autenticación (token/sign), WSFEv1 para facturación electrónica (CAE) y el padrón A4/A5 para
/// consulta de personas. Portado del módulo Arca original, respetando la arquitectura por capas:
/// es la única pieza que conoce el detalle SOAP; Application solo ve el puerto <see cref="IArcaClient"/>.
/// </summary>
public class ArcaClient : IArcaClient
{
    private readonly ArcaSettings _settings;
    private readonly ILogger<ArcaClient> _logger;

    private static readonly Dictionary<string, (string token, string sign, DateTime expiry)> _tokenCache
        = new(StringComparer.OrdinalIgnoreCase);
    private static readonly SemaphoreSlim _tokenLock = new(1, 1);

    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    public ArcaClient(ArcaSettings settings, ILogger<ArcaClient> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public int PuntoVentaPorDefecto => _settings.PuntoVenta;

    private void EnsureEnabled()
    {
        if (!_settings.Enabled)
            throw new InvalidOperationException(
                "El módulo ARCA no está habilitado. Configurá la sección \"Arca\" (Enabled, Cuit, certificado) para operar.");
    }

    // ──────────────────────────────────────────────
    // Facturación electrónica
    // ──────────────────────────────────────────────

    public async Task<ArcaAutorizacionResult> AutorizarComprobanteAsync(
        int puntoVenta, int tipoComprobante, ArcaAutorizacionRequest request, CancellationToken ct = default)
    {
        try
        {
            EnsureEnabled();
            var (token, sign) = await ObtenerTokenSignAsync("wsfe", ct);
            string urlWsfe = UrlWsfe();

            var (ultimoNro, fechaUltimoAutorizado) = await ConsultarUltimoComprobanteAsync(
                urlWsfe, token, sign, puntoVenta, tipoComprobante, ct);

            if (fechaUltimoAutorizado.HasValue
                && request.FechaEmision.Date < fechaUltimoAutorizado.Value.Date)
            {
                return new ArcaAutorizacionResult
                {
                    Autorizado = false,
                    MensajeError = $"La fecha de emisión ({request.FechaEmision:dd/MM/yyyy}) no puede ser anterior " +
                                   $"a la del último comprobante autorizado ({fechaUltimoAutorizado.Value:dd/MM/yyyy})."
                };
            }

            long nroAAutorizar = ultimoNro + 1;
            return await SolicitarCAEAsync(urlWsfe, token, sign, puntoVenta, tipoComprobante, nroAAutorizar, request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al autorizar comprobante en ARCA");
            return new ArcaAutorizacionResult
            {
                Autorizado = false,
                MensajeError = $"Excepción: {ex.Message}"
            };
        }
    }

    public async Task<long> ObtenerUltimoComprobanteAsync(int puntoVenta, int tipoComprobante, CancellationToken ct = default)
    {
        EnsureEnabled();
        var (token, sign) = await ObtenerTokenSignAsync("wsfe", ct);
        var (numero, _) = await ConsultarUltimoComprobanteAsync(UrlWsfe(), token, sign, puntoVenta, tipoComprobante, ct);
        return numero;
    }

    // ──────────────────────────────────────────────
    // Consultar condiciones IVA receptor
    // ──────────────────────────────────────────────

    public async Task<string> ObtenerCondicionesIvaReceptorAsync(CancellationToken ct = default)
    {
        EnsureEnabled();
        var (token, sign) = await ObtenerTokenSignAsync("wsfe", ct);

        string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
  <soapenv:Header/>
  <soapenv:Body>
    <ar:FEParamGetCondicionIvaReceptor>
      <ar:Auth>
        <ar:Token>{token}</ar:Token>
        <ar:Sign>{sign}</ar:Sign>
        <ar:Cuit>{_settings.Cuit}</ar:Cuit>
      </ar:Auth>
    </ar:FEParamGetCondicionIvaReceptor>
  </soapenv:Body>
</soapenv:Envelope>";

        return await EnviarSoapAsync(UrlWsfe(), soap, "FEParamGetCondicionIvaReceptor", ct);
    }

    // ──────────────────────────────────────────────
    // Padrón
    // ──────────────────────────────────────────────

    public async Task<ArcaPersonaResult?> ConsultarPersonaAsync(string documento, CancellationToken ct = default)
    {
        EnsureEnabled();
        documento = documento.Replace(" ", "").Replace("-", "").Replace(".", "");

        if (documento.Length == 11 && documento.All(char.IsDigit))
            return await ConsultarPorCuitAsync(documento, ct);

        if (documento.Length >= 7 && documento.Length <= 8 && documento.All(char.IsDigit))
            return await ConsultarPorDniAsync(documento, ct);

        throw new ArgumentException("El documento debe ser un DNI (7-8 dígitos) o CUIT/CUIL (11 dígitos).");
    }

    // ──────────────────────────────────────────────
    // WSAA: Obtener Token y Sign
    // ──────────────────────────────────────────────

    private async Task<(string token, string sign)> ObtenerTokenSignAsync(string servicio, CancellationToken ct)
    {
        await _tokenLock.WaitAsync(ct);
        try
        {
            if (_tokenCache.TryGetValue(servicio, out var cached) && DateTime.UtcNow < cached.expiry)
                return (cached.token, cached.sign);

            if (CargarTokenDesdeDisco(servicio))
            {
                var loaded = _tokenCache[servicio];
                _logger.LogInformation("Token WSAA ({Servicio}) cargado desde disco. Expira: {Exp}", servicio, loaded.expiry);
                return (loaded.token, loaded.sign);
            }

            string urlWsaa = _settings.ModoProduccion
                ? "https://wsaa.afip.gov.ar/ws/services/LoginCms"
                : "https://wsaahomo.afip.gov.ar/ws/services/LoginCms";

            string tra = GenerarTRA(servicio);
            string cmsFirmado = FirmarTRA(tra);

            string soapBody = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:wsaa=""http://wsaa.view.sua.dvadac.desein.afip.gov.ar"">
  <soapenv:Header/>
  <soapenv:Body>
    <wsaa:loginCms>
      <wsaa:in0>{cmsFirmado}</wsaa:in0>
    </wsaa:loginCms>
  </soapenv:Body>
</soapenv:Envelope>";

            string respuestaXml;
            try
            {
                respuestaXml = await EnviarSoapAsync(urlWsaa, soapBody, "loginCms", ct);
            }
            catch (Exception ex) when (ex.Message.Contains("alreadyAuthenticated"))
            {
                if (_tokenCache.TryGetValue(servicio, out var existente))
                {
                    _logger.LogWarning("ARCA reporta TA ya existente para {Servicio}, usando token cacheado.", servicio);
                    _tokenCache[servicio] = (existente.token, existente.sign, DateTime.UtcNow.AddMinutes(10));
                    return (existente.token, existente.sign);
                }
                throw new InvalidOperationException(
                    "ARCA ya tiene un Ticket de Acceso válido para este servicio pero el caché local se perdió. " +
                    "Reintentar en unos minutos o esperar que expire el TA (máx. 12hs).");
            }

            var doc = XDocument.Parse(respuestaXml);
            string taXml = doc.Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "loginCmsReturn")?.Value
                ?? throw new InvalidOperationException($"No se pudo obtener el TA de WSAA para el servicio '{servicio}'");

            var ta = XDocument.Parse(taXml);
            string token = ta.Descendants("token").First().Value;
            string sign = ta.Descendants("sign").First().Value;
            string expiracion = ta.Descendants("expirationTime").First().Value;

            var expiry = DateTime.Parse(expiracion).ToUniversalTime().AddMinutes(-5);
            _tokenCache[servicio] = (token, sign, expiry);
            GuardarTokenEnDisco(servicio, token, sign, expiry);

            _logger.LogInformation("Token WSAA ({Servicio}) obtenido. Expira: {Exp}", servicio, expiracion);
            return (token, sign);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    // ──────────────────────────────────────────────
    // Caché en disco
    // ──────────────────────────────────────────────

    private static string GetCachePath(string servicio) =>
        Path.Combine(Path.GetTempPath(), $"arca_token_cache_{servicio}.json");

    private void GuardarTokenEnDisco(string servicio, string token, string sign, DateTime expiry)
    {
        try
        {
            var data = new { token, sign, expiry };
            File.WriteAllText(GetCachePath(servicio), JsonSerializer.Serialize(data));
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo guardar el token en disco ({Servicio}): {Msg}", servicio, ex.Message);
        }
    }

    private bool CargarTokenDesdeDisco(string servicio)
    {
        try
        {
            string path = GetCachePath(servicio);
            if (!File.Exists(path)) return false;
            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var token = doc.RootElement.GetProperty("token").GetString();
            var sign = doc.RootElement.GetProperty("sign").GetString();
            var expiry = doc.RootElement.GetProperty("expiry").GetDateTime();

            if (token == null || sign == null || DateTime.UtcNow >= expiry)
                return false;

            _tokenCache[servicio] = (token, sign, expiry);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ──────────────────────────────────────────────
    // WSFEv1: FECompUltimoAutorizado
    // ──────────────────────────────────────────────

    private async Task<(long Numero, DateTime? FechaUltimo)> ConsultarUltimoComprobanteAsync(
        string urlWsfe, string token, string sign, int puntoVenta, int tipoComprobante, CancellationToken ct)
    {
        string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
  <soapenv:Header/>
  <soapenv:Body>
    <ar:FECompUltimoAutorizado>
      <ar:Auth>
        <ar:Token>{token}</ar:Token>
        <ar:Sign>{sign}</ar:Sign>
        <ar:Cuit>{_settings.Cuit}</ar:Cuit>
      </ar:Auth>
      <ar:PtoVta>{puntoVenta}</ar:PtoVta>
      <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
    </ar:FECompUltimoAutorizado>
  </soapenv:Body>
</soapenv:Envelope>";

        string respuesta = await EnviarSoapAsync(urlWsfe, soap, "FECompUltimoAutorizado", ct);

        var doc = XDocument.Parse(respuesta);
        string nro = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "CbteNro")?.Value ?? "0";
        string fch = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "CbteFch")?.Value ?? "";

        DateTime? fechaUltimo = null;
        if (fch.Length == 8 && DateTime.TryParseExact(
                fch, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var fechaParsed))
            fechaUltimo = fechaParsed;

        _logger.LogInformation("Último comprobante PV {PV} tipo {Tipo}: {Nro} fecha {Fch}",
            puntoVenta, tipoComprobante, nro, fch);

        return (long.Parse(nro), fechaUltimo);
    }

    // ──────────────────────────────────────────────
    // WSFEv1: FECAESolicitar
    // ──────────────────────────────────────────────

    private async Task<ArcaAutorizacionResult> SolicitarCAEAsync(
        string urlWsfe, string token, string sign,
        int puntoVenta, int tipoComprobante,
        long nroComprobante, ArcaAutorizacionRequest f, CancellationToken ct)
    {
        var ci = System.Globalization.CultureInfo.InvariantCulture;

        bool esFacturaC = tipoComprobante == 11 || tipoComprobante == 13;
        double impNeto = (double)f.Subtotal;
        double impIVA = esFacturaC ? 0 : Math.Round(impNeto * 0.21, 2);
        double impTrib = esFacturaC ? 0 : Math.Round((double)f.Percepcion, 2);
        double impTotal = esFacturaC
            ? (double)f.Total
            : Math.Round(impNeto + impIVA + impTrib, 2);

        string fechaComp = f.FechaEmision.ToString("yyyyMMdd");
        string fechaDesde = f.Concepto != 1 && f.FechaServicioDesde.HasValue
            ? f.FechaServicioDesde.Value.ToString("yyyyMMdd") : "";
        string fechaHasta = f.Concepto != 1 && f.FechaServicioHasta.HasValue
            ? f.FechaServicioHasta.Value.ToString("yyyyMMdd") : "";
        string fechaVto = f.Concepto != 1
            ? f.FechaVencimientoPago.ToString("yyyyMMdd") : "";

        var (tipoDoc, docNro) = DeterminarDocumentoReceptor(f.DocumentoReceptor, f.CondicionIVAReceptorId);

        string bloqueIVA = esFacturaC ? "" : $@"
            <ar:Iva>
              <ar:AlicIva>
                <ar:Id>5</ar:Id>
                <ar:BaseImp>{impNeto.ToString("F2", ci)}</ar:BaseImp>
                <ar:Importe>{impIVA.ToString("F2", ci)}</ar:Importe>
              </ar:AlicIva>
            </ar:Iva>";

        string bloqueTributos = impTrib > 0 ? $@"
            <ar:Tributos>
              <ar:Tributo>
                <ar:Id>6</ar:Id>
                <ar:Desc>Percepcion de IVA RG 5329/2023</ar:Desc>
                <ar:BaseImp>{impNeto.ToString("F2", ci)}</ar:BaseImp>
                <ar:Alic>3.00</ar:Alic>
                <ar:Importe>{impTrib.ToString("F2", ci)}</ar:Importe>
              </ar:Tributo>
            </ar:Tributos>" : "";

        string bloqueCbtesAsoc = f.ComprobanteAsociado is not null ? $@"
            <ar:CbtesAsoc>
              <ar:CbteAsoc>
                <ar:Tipo>{f.ComprobanteAsociado.Tipo}</ar:Tipo>
                <ar:PtoVta>{f.ComprobanteAsociado.PtoVta}</ar:PtoVta>
                <ar:Nro>{f.ComprobanteAsociado.Nro}</ar:Nro>
                <ar:Cuit>{_settings.Cuit}</ar:Cuit>
                <ar:CbteFch>{f.ComprobanteAsociado.CbteFch}</ar:CbteFch>
              </ar:CbteAsoc>
            </ar:CbtesAsoc>" : "";

        string fchServDesde = f.Concepto != 1 ? fechaDesde : "";
        string fchServHasta = f.Concepto != 1 ? fechaHasta : "";
        string fchVtoPago = f.Concepto != 1 ? fechaVto : "";

        string soap = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:ar=""http://ar.gov.afip.dif.FEV1/"">
  <soapenv:Header/>
  <soapenv:Body>
    <ar:FECAESolicitar>
      <ar:Auth>
        <ar:Token>{token}</ar:Token>
        <ar:Sign>{sign}</ar:Sign>
        <ar:Cuit>{_settings.Cuit}</ar:Cuit>
      </ar:Auth>
      <ar:FeCAEReq>
        <ar:FeCabReq>
          <ar:CantReg>1</ar:CantReg>
          <ar:PtoVta>{puntoVenta}</ar:PtoVta>
          <ar:CbteTipo>{tipoComprobante}</ar:CbteTipo>
        </ar:FeCabReq>
        <ar:FeDetReq>
          <ar:FECAEDetRequest>
            <ar:Concepto>{f.Concepto}</ar:Concepto>
            <ar:DocTipo>{tipoDoc}</ar:DocTipo>
            <ar:DocNro>{docNro}</ar:DocNro>
            <ar:CbteDesde>{nroComprobante}</ar:CbteDesde>
            <ar:CbteHasta>{nroComprobante}</ar:CbteHasta>
            <ar:CbteFch>{fechaComp}</ar:CbteFch>
            <ar:ImpTotal>{impTotal.ToString("F2", ci)}</ar:ImpTotal>
            <ar:ImpTotConc>0.00</ar:ImpTotConc>
            <ar:ImpNeto>{(esFacturaC ? impTotal : impNeto).ToString("F2", ci)}</ar:ImpNeto>
            <ar:ImpOpEx>0.00</ar:ImpOpEx>
            <ar:ImpIVA>{(esFacturaC ? 0 : impIVA).ToString("F2", ci)}</ar:ImpIVA>
            <ar:ImpTrib>{impTrib.ToString("F2", ci)}</ar:ImpTrib>
            <ar:FchServDesde>{fchServDesde}</ar:FchServDesde>
            <ar:FchServHasta>{fchServHasta}</ar:FchServHasta>
            <ar:FchVtoPago>{fchVtoPago}</ar:FchVtoPago>
            <ar:MonId>PES</ar:MonId>
            <ar:MonCotiz>1</ar:MonCotiz>
            <ar:CondicionIVAReceptorId>{f.CondicionIVAReceptorId}</ar:CondicionIVAReceptorId>{bloqueIVA}{bloqueCbtesAsoc}{bloqueTributos}
          </ar:FECAEDetRequest>
        </ar:FeDetReq>
      </ar:FeCAEReq>
    </ar:FECAESolicitar>
  </soapenv:Body>
</soapenv:Envelope>";

        _logger.LogDebug("Request FECAESolicitar: {Xml}", soap);

        string respuestaXml = await EnviarSoapAsync(urlWsfe, soap, "FECAESolicitar", ct);
        return ParsearRespuestaCAE(respuestaXml, nroComprobante);
    }

    // ──────────────────────────────────────────────
    // Parsear respuesta FECAESolicitar
    // ──────────────────────────────────────────────

    private ArcaAutorizacionResult ParsearRespuestaCAE(string xml, long nroComprobante)
    {
        _logger.LogDebug("Respuesta FECAESolicitar: {Xml}", xml);

        var doc = XDocument.Parse(xml);

        string resultado = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Resultado")?.Value ?? "";
        string cae = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "CAE")?.Value ?? "";
        string vencimiento = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "CAEFchVto")?.Value ?? "";

        if (resultado == "A" && !string.IsNullOrEmpty(cae))
        {
            return new ArcaAutorizacionResult
            {
                Autorizado = true,
                CAE = cae,
                CAEFechaVencimiento = DateOnly.ParseExact(vencimiento, "yyyyMMdd"),
                NumeroComprobante = nroComprobante
            };
        }

        var obs = doc.Descendants()
            .Where(e => e.Name.LocalName == "Obs")
            .Select(o => $"[{o.Elements().FirstOrDefault(x => x.Name.LocalName == "Code")?.Value}] " +
                         $"{o.Elements().FirstOrDefault(x => x.Name.LocalName == "Msg")?.Value}")
            .ToList();

        var errs = doc.Descendants()
            .Where(e => e.Name.LocalName == "Err")
            .Select(e => $"[{e.Elements().FirstOrDefault(x => x.Name.LocalName == "Code")?.Value}] " +
                         $"{e.Elements().FirstOrDefault(x => x.Name.LocalName == "Msg")?.Value}")
            .ToList();

        string mensajeError = string.Join(" | ", obs.Concat(errs));
        if (string.IsNullOrEmpty(mensajeError))
            mensajeError = $"Resultado: {resultado}. Sin detalle de error.";

        return new ArcaAutorizacionResult
        {
            Autorizado = false,
            MensajeError = mensajeError
        };
    }

    // ──────────────────────────────────────────────
    // Determinar tipo y número de documento
    // ──────────────────────────────────────────────

    private static (int tipoDoc, long docNro) DeterminarDocumentoReceptor(string? docStr, int condicionIVAReceptorId)
    {
        string doc = (docStr ?? "").Replace("-", "").Replace(" ", "").Replace(".", "").Trim();

        if (string.IsNullOrEmpty(doc) || doc == "0" || doc == "11111111111")
            return (99, 0);

        if (doc.Length >= 7 && doc.Length <= 8 && doc.All(char.IsDigit))
            return (96, long.Parse(doc));

        if (doc.Length == 11 && doc.All(char.IsDigit))
        {
            long numero = long.Parse(doc);
            // Consumidor Final (5) con 11 dígitos → CUIL (86); el resto → CUIT (80)
            return condicionIVAReceptorId == 5 ? (86, numero) : (80, numero);
        }

        return (99, 0);
    }

    // ──────────────────────────────────────────────
    // Padrón A4 (por CUIT)
    // ──────────────────────────────────────────────

    private async Task<ArcaPersonaResult?> ConsultarPorCuitAsync(string cuit, CancellationToken ct)
    {
        var (token, sign) = await ObtenerTokenSignAsync("ws_sr_padron_a4", ct);

        string url = _settings.ModoProduccion
            ? "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA4"
            : "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA4";

        string soap = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:per=""http://a4.padron.ws.afip.gov.ar/"">
  <soapenv:Header/>
  <soapenv:Body>
    <per:getPersona>
      <token>{token}</token>
      <sign>{sign}</sign>
      <cuitRepresentada>{_settings.Cuit}</cuitRepresentada>
      <idPersona>{cuit}</idPersona>
    </per:getPersona>
  </soapenv:Body>
</soapenv:Envelope>";

        string respXml = await EnviarSoapAsync(url, soap, "getPersona",
            ct, "http://a4.padron.ws.afip.gov.ar/getPersona");

        return ParsearPersonaDesdeA4(respXml);
    }

    // ──────────────────────────────────────────────
    // Padrón A5 (por DNI)
    // ──────────────────────────────────────────────

    private async Task<ArcaPersonaResult?> ConsultarPorDniAsync(string dni, CancellationToken ct)
    {
        var (token, sign) = await ObtenerTokenSignAsync("ws_sr_padron_a5", ct);

        string url = _settings.ModoProduccion
            ? "https://aws.afip.gov.ar/sr-padron/webservices/personaServiceA5"
            : "https://awshomo.afip.gov.ar/sr-padron/webservices/personaServiceA5";

        string soap = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:per=""http://a5.padron.ws.afip.gov.ar/"">
  <soapenv:Header/>
  <soapenv:Body>
    <per:getPersonasByDocumento>
      <token>{token}</token>
      <sign>{sign}</sign>
      <cuitRepresentada>{_settings.Cuit}</cuitRepresentada>
      <tipoDocumento>96</tipoDocumento>
      <documento>{dni}</documento>
    </per:getPersonasByDocumento>
  </soapenv:Body>
</soapenv:Envelope>";

        string respXml = await EnviarSoapAsync(url, soap, "getPersonasByDocumento",
            ct, "http://a5.padron.ws.afip.gov.ar/getPersonasByDocumento");

        return ParsearPersonaDesdeA5(respXml);
    }

    // ──────────────────────────────────────────────
    // Parsear Padrón A4
    // ──────────────────────────────────────────────

    private ArcaPersonaResult? ParsearPersonaDesdeA4(string xml)
    {
        var doc = XDocument.Parse(xml);

        var datosGen = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "datosGenerales");
        if (datosGen == null) return null;

        string tipoPersona = datosGen.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "tipoPersona")?.Value ?? "";

        string denominacion = tipoPersona == "JURIDICA"
            ? datosGen.Elements().FirstOrDefault(e => e.Name.LocalName == "razonSocial")?.Value ?? ""
            : $"{datosGen.Elements().FirstOrDefault(e => e.Name.LocalName == "nombre")?.Value} " +
              $"{datosGen.Elements().FirstOrDefault(e => e.Name.LocalName == "apellido")?.Value}".Trim();

        string idPersona = datosGen.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "idPersona")?.Value ?? "0";

        string estadoClave = datosGen.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "estadoClave")?.Value ?? "";

        var domFiscal = datosGen.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "domicilioFiscal");

        string? localidad = domFiscal?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "localidad")?.Value;
        string? provincia = domFiscal?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "descripcionProvincia")?.Value;
        string? cp = domFiscal?.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "codPostal")?.Value;

        string? condIva = doc.Descendants()
            .Where(e => e.Name.LocalName == "descripcionCaracterizacion")
            .Select(e => e.Value)
            .FirstOrDefault(d => d.Contains("IVA", StringComparison.OrdinalIgnoreCase)
                              || d.Contains("Mono", StringComparison.OrdinalIgnoreCase));

        return new ArcaPersonaResult
        {
            Cuit = long.TryParse(idPersona, out long cuitLong) ? cuitLong : 0,
            TipoPersona = tipoPersona,
            Denominacion = denominacion,
            EstadoClave = estadoClave,
            CondicionIva = condIva,
            Localidad = localidad,
            Provincia = provincia,
            CodigoPostal = cp
        };
    }

    // ──────────────────────────────────────────────
    // Parsear Padrón A5
    // ──────────────────────────────────────────────

    private ArcaPersonaResult? ParsearPersonaDesdeA5(string xml)
    {
        var doc = XDocument.Parse(xml);

        var personas = doc.Descendants()
            .Where(e => e.Name.LocalName == "personas")
            .ToList();

        if (personas.Count == 0) return null;

        var personaEl = personas.FirstOrDefault(p =>
            p.Elements().Any(e => e.Name.LocalName == "estadoClave" && e.Value == "ACTIVO"))
            ?? personas.First();

        string idPersona = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "idPersona")?.Value ?? "0";
        string tipoPersona = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "tipoPersona")?.Value ?? "";
        string nombre = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "nombre")?.Value ?? "";
        string apellido = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "apellido")?.Value ?? "";
        string razonSocial = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "razonSocial")?.Value ?? "";
        string estadoClave = personaEl.Elements()
            .FirstOrDefault(e => e.Name.LocalName == "estadoClave")?.Value ?? "";

        string denominacion = tipoPersona == "JURIDICA"
            ? razonSocial
            : $"{nombre} {apellido}".Trim();

        return new ArcaPersonaResult
        {
            Cuit = long.TryParse(idPersona, out long cuitLong) ? cuitLong : 0,
            TipoPersona = tipoPersona,
            Denominacion = denominacion,
            EstadoClave = estadoClave
        };
    }

    // ──────────────────────────────────────────────
    // Generar TRA
    // ──────────────────────────────────────────────

    private static string GenerarTRA(string servicio)
    {
        string generationTime = DateTime.UtcNow.AddMinutes(-10).ToString("o");
        string expirationTime = DateTime.UtcNow.AddHours(12).ToString("o");
        string uniqueId = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<loginTicketRequest version=""1.0"">
  <header>
    <uniqueId>{uniqueId}</uniqueId>
    <generationTime>{generationTime}</generationTime>
    <expirationTime>{expirationTime}</expirationTime>
  </header>
  <service>{servicio}</service>
</loginTicketRequest>";
    }

    // ──────────────────────────────────────────────
    // Firmar TRA con certificado digital
    // ──────────────────────────────────────────────

    private string FirmarTRA(string traXml)
    {
        var certPublico = new X509Certificate2(_settings.CertificadoPath);

        string keyPem = File.ReadAllText(_settings.ClavePath);
        using var rsa = RSA.Create();
        rsa.ImportFromPem(keyPem);

        using var certConClave = certPublico.CopyWithPrivateKey(rsa);

        var contentInfo = new ContentInfo(Encoding.UTF8.GetBytes(traXml));
        var signedCms = new SignedCms(contentInfo);
        var cmsSigner = new CmsSigner(certConClave);
        cmsSigner.DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"); // SHA256

        signedCms.ComputeSignature(cmsSigner);
        return Convert.ToBase64String(signedCms.Encode());
    }

    // ──────────────────────────────────────────────
    // Enviar SOAP
    // ──────────────────────────────────────────────

    private async Task<string> EnviarSoapAsync(
        string url, string soapBody, string accion, CancellationToken ct, string? soapActionOverride = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(soapBody, Encoding.UTF8, "text/xml");

        string soapAction = soapActionOverride
            ?? (url.Contains("wsaa") || url.Contains("LoginCms") ? "" : $"http://ar.gov.afip.dif.FEV1/{accion}");
        request.Headers.Add("SOAPAction", $"\"{soapAction}\"");

        var response = await _httpClient.SendAsync(request, ct);
        string contenido = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} al llamar {accion}: {contenido}");

        if (contenido.Contains("<faultstring>"))
        {
            var faultDoc = XDocument.Parse(contenido);
            string fault = faultDoc.Descendants("faultstring").FirstOrDefault()?.Value ?? "Error SOAP desconocido";
            throw new InvalidOperationException($"SOAP Fault en {accion}: {fault}");
        }

        return contenido;
    }

    // ──────────────────────────────────────────────
    // URL helpers
    // ──────────────────────────────────────────────

    private string UrlWsfe() => _settings.ModoProduccion
        ? "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
        : "https://wswhomo.afip.gov.ar/wsfev1/service.asmx";
}
