using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DnDManager.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DnDManager.Services;

public class WebServerService : IWebServerService {
    private readonly INetworkService _networkService;
    private WebApplication? _app;
    private Func<WebEncounterState>? _stateProvider;

    public string? Url { get; private set; }
    public int Port { get; private set; }
    public bool IsRunning { get; private set; }
    public IHubContext<EncounterHub>? HubContext { get; private set; }

    public event Action<string>? UrlChanged;
    public event Action<bool>? RunningChanged;

    public WebServerService(INetworkService networkService) {
        _networkService = networkService;
    }

    public void SetStateProvider(Func<WebEncounterState> stateProvider) {
        _stateProvider = stateProvider;
    }

    public async Task StartAsync() {
        if (IsRunning) return;

        var lanIp = _networkService.GetLanIpAddress();
        var cert = GenerateSelfSignedCert(lanIp);
        Port = SelectRandomPort();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => {
            options.Listen(IPAddress.Any, Port, listenOptions => {
                listenOptions.UseHttps(cert);
            });
        });

        builder.Services.AddSignalR();
        builder.Services.AddSingleton<Func<WebEncounterState>>(
            _ => _stateProvider ?? (() => new WebEncounterState()));

        // Suppress ASP.NET Core console logging
        builder.Logging.ClearProviders();

        _app = builder.Build();

        _app.MapHub<EncounterHub>("/encounter-hub");

        MapStaticContent(_app);

        await _app.StartAsync();

        HubContext = _app.Services.GetRequiredService<IHubContext<EncounterHub>>();

        var host = lanIp?.ToString() ?? "localhost";
        Url = $"https://{host}:{Port}";
        IsRunning = true;

        UrlChanged?.Invoke(Url);
        RunningChanged?.Invoke(true);
    }

    public async Task StopAsync() {
        if (_app != null) {
            await _app.StopAsync();
            await _app.DisposeAsync();
            _app = null;
        }

        IsRunning = false;
        Url = null;
        RunningChanged?.Invoke(false);
    }

    public async ValueTask DisposeAsync() {
        await StopAsync();
        GC.SuppressFinalize(this);
    }

    private static void MapStaticContent(WebApplication app) {
        var assembly = Assembly.GetExecutingAssembly();
        var prefix = "DnDManager.Web.StaticContent.";

        app.MapGet("/", () => ServeEmbeddedResource(assembly, $"{prefix}index.html", "text/html"));
        app.MapGet("/style.css", () => ServeEmbeddedResource(assembly, $"{prefix}style.css", "text/css"));
        app.MapGet("/app.js", () => ServeEmbeddedResource(assembly, $"{prefix}app.js", "application/javascript"));
        app.MapGet("/signalr.min.js", () => ServeEmbeddedResource(assembly, $"{prefix}signalr.min.js", "application/javascript"));
    }

    private static IResult ServeEmbeddedResource(Assembly assembly, string resourceName, string contentType) {
        var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return Results.NotFound();

        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        return Results.Content(content, contentType);
    }

    private static int SelectRandomPort() {
        // Bind to port 0 to let the OS assign a free port, then read it
        using var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static X509Certificate2 GenerateSelfSignedCert(IPAddress? lanIp) {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=DnDManager-Local", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddIpAddress(IPAddress.Loopback);
        sanBuilder.AddDnsName("localhost");
        if (lanIp != null)
            sanBuilder.AddIpAddress(lanIp);
        request.CertificateExtensions.Add(sanBuilder.Build());

        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(1));

        // Export and re-import for Kestrel compatibility on Linux
        return X509CertificateLoader.LoadPkcs12(
            cert.Export(X509ContentType.Pfx, ""),
            "",
            X509KeyStorageFlags.EphemeralKeySet);
    }
}
