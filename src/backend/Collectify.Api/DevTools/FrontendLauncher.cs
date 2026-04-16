using System.Diagnostics;

namespace Collectify.Api.DevTools;

public sealed class FrontendLauncher(
    IConfiguration configuration,
    IHostEnvironment environment,
    IHostApplicationLifetime lifetime,
    ILogger<FrontendLauncher> logger) : IHostedService
{
    private readonly CancellationTokenSource _stoppingTokenSource = new();
    private Process? _frontendProcess;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var enabled = configuration.GetValue("Development:OpenFrontendOnStart", true);
        if (!enabled)
        {
            return Task.CompletedTask;
        }

        var frontendUrl = configuration["Development:FrontendUrl"] ?? "http://127.0.0.1:5173";
        if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var uri))
        {
            logger.LogWarning("Frontend URL '{FrontendUrl}' is not valid. Frontend launch skipped.", frontendUrl);
            return Task.CompletedTask;
        }

        lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(() => LaunchFrontendAsync(uri, _stoppingTokenSource.Token));
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _stoppingTokenSource.Cancel();

        if (_frontendProcess is { HasExited: false })
        {
            try
            {
                _frontendProcess.Kill(entireProcessTree: true);
            }
            catch (Exception exception)
            {
                logger.LogDebug(exception, "Could not stop the frontend development server.");
            }
        }

        return Task.CompletedTask;
    }

    private async Task LaunchFrontendAsync(Uri uri, CancellationToken cancellationToken)
    {
        var launchMode = configuration["Development:FrontendLaunchMode"] ?? "Electron";

        if (launchMode.Equals("Electron", StringComparison.OrdinalIgnoreCase))
        {
            await LaunchElectronAsync(uri, cancellationToken);
            return;
        }

        await LaunchBrowserAsync(uri, cancellationToken);
    }

    private async Task LaunchElectronAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (await IsFrontendReachableAsync(uri, cancellationToken))
        {
            StartFrontendProcess(configuration["Development:ElectronStartArguments"] ?? "run electron:open");
            return;
        }

        StartFrontendProcess(configuration["Development:FrontendStartArguments"] ?? "run dev");
    }

    private async Task LaunchBrowserAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!await IsFrontendReachableAsync(uri, cancellationToken))
        {
            StartFrontendProcess(configuration["Development:RendererStartArguments"] ?? "run dev:renderer");
        }

        var timeoutSeconds = configuration.GetValue("Development:FrontendStartupTimeoutSeconds", 45);
        var ready = await WaitForFrontendAsync(uri, TimeSpan.FromSeconds(timeoutSeconds), cancellationToken);

        if (!ready)
        {
            logger.LogWarning(
                "Collectify frontend is not reachable at {FrontendUrl}. Start it manually with 'npm.cmd run dev'.",
                uri);
            return;
        }

        OpenBrowser(uri);
    }

    private void StartFrontendProcess(string npmArguments)
    {
        var autoStart = configuration.GetValue("Development:AutoStartFrontend", true);
        if (!autoStart)
        {
            return;
        }

        var configuredPath = configuration["Development:FrontendProjectPath"] ?? @"..\..\frontend\collectify-desktop";
        var frontendRoot = Path.GetFullPath(Path.Combine(environment.ContentRootPath, configuredPath));
        var packageJson = Path.Combine(frontendRoot, "package.json");

        if (!File.Exists(packageJson))
        {
            logger.LogWarning("Frontend project was not found at {FrontendRoot}.", frontendRoot);
            return;
        }

        var executable = OperatingSystem.IsWindows() ? "cmd.exe" : "npm";
        var arguments = OperatingSystem.IsWindows()
            ? $"/c npm.cmd {npmArguments}"
            : npmArguments;

        try
        {
            _frontendProcess = Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = frontendRoot,
                UseShellExecute = false,
                CreateNoWindow = false
            });

            logger.LogInformation(
                "Started Collectify frontend process '{NpmArguments}' from {FrontendRoot}",
                npmArguments,
                frontendRoot);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not start the Collectify frontend process.");
        }
    }

    private static async Task<bool> WaitForFrontendAsync(Uri uri, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetTimestamp() + (long)(timeout.TotalSeconds * TimeProvider.System.TimestampFrequency);

        while (!cancellationToken.IsCancellationRequested && TimeProvider.System.GetTimestamp() < deadline)
        {
            if (await IsFrontendReachableAsync(uri, cancellationToken))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        return false;
    }

    private static async Task<bool> IsFrontendReachableAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            using var response = await httpClient.GetAsync(uri, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void OpenBrowser(Uri uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true
            });

            logger.LogInformation("Opened Collectify frontend in browser at {FrontendUrl}", uri);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Could not open Collectify frontend in browser at {FrontendUrl}", uri);
        }
    }
}
