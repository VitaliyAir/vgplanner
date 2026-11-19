using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VGoalsPlanner.API.Controllers
{
    /// <summary>
    /// Версия развёрнутого API — чтобы отличать сборки на стендах без доступа к Docker.
    /// Эндпоинт открыт анонимно: его дергают curl'ом сразу после деплоя, до входа в приложение.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public VersionController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public VersionResponse Get() => AppVersionInfo.Get(_environment.EnvironmentName);
    }

    /// <summary>Версия, коммит, время сборки и окружение запущенного API.</summary>
    public sealed record VersionResponse(
        string Version,
        string? Commit,
        DateTime BuiltAt,
        string Environment);

    /// <summary>
    /// Читает версию из AssemblyInformationalVersion (формат "1.0.7+a1b2c3d", см. Directory.Build.props
    /// и -p:SourceRevisionId в Dockerfile) и время сборки из даты файла ассамблеи.
    /// </summary>
    internal static class AppVersionInfo
    {
        private static readonly Lazy<(string Version, string? Commit, DateTime BuiltAt)> Current = new(Read);

        public static VersionResponse Get(string environmentName)
        {
            var (version, commit, builtAt) = Current.Value;
            return new VersionResponse(version, commit, builtAt, environmentName);
        }

        private static (string Version, string? Commit, DateTime BuiltAt) Read()
        {
            var assembly = typeof(AppVersionInfo).Assembly;

            var version = assembly.GetName().Version?.ToString(3) ?? "0.0.0";
            string? commit = null;

            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(informational))
            {
                // "1.0.7+a1b2c3d..." — суффикс появляется только когда сборка знает коммит (git или -p:SourceRevisionId).
                var separator = informational.IndexOf('+');
                if (separator > 0)
                {
                    version = informational[..separator];
                    var revision = informational[(separator + 1)..].Trim();
                    commit = revision.Length > 40 ? revision[..40] : revision;
                }
                else
                {
                    version = informational;
                }
            }

            var location = assembly.Location;
            var builtAt = string.IsNullOrEmpty(location) || !System.IO.File.Exists(location)
                ? DateTime.UtcNow
                : System.IO.File.GetLastWriteTimeUtc(location);

            return (version, commit, builtAt);
        }
    }
}
