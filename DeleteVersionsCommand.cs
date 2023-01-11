using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nuclear
{
    public class DeleteVersionsSettings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGE>")]
        [Description("The package ID whose versions should be deleted.")]
        public string PackageId { get; set; }

        [CommandArgument(1, "<VERSIONS>")]
        [Description("The version range that should be deleted.")]
        [TypeConverter(typeof(NuclearVersionRangeConverter))]
        public NuclearVersionRange VersionRange { get; set; }

        [CommandOption("-s|--source <SOURCE>")]
        [Description("The NuGet package source URL.")]
        [DefaultValue("https://api.nuget.org/v3/index.json")]
        public string PackageSource { get; set; }

        [CommandOption("-k|--api-key <APIKEY>")]
        [Description("The API key to authenticate on the package source.")]
        [DefaultValue("https://api.nuget.org/v3/index.json")]
        public string ApiKey { get; set; }

        [CommandOption("--dry-run")]
        [Description("Show what would be deleted")]
        public bool DryRun { get; set; }
    }

    [Description("Delete package versions.")]
    public class DeleteVersionsCommand : AsyncCommand<DeleteVersionsSettings>
    {
        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] DeleteVersionsSettings settings)
        {
            var logger = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(settings.PackageSource);
            var metadataResource = await repository.GetResourceAsync<PackageMetadataResource>();
            var updateResource = await repository.GetResourceAsync<PackageUpdateResource>();

            var packages = await metadataResource.GetMetadataAsync(
                settings.PackageId,
                includePrerelease: true,
                includeUnlisted: true,
                cache,
                logger,
                cancellationToken);

            if (!packages.Any())
            {
                AnsiConsole.MarkupLine($"[yellow]Warning:[/] Could not find package named [green]{settings.PackageId}[/].");
                return 0;
            }

            var pending = packages
                .Where(p => settings.VersionRange.Includes(p.Identity.Version))
                .ToList();

            if (!pending.Any())
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]Warning:[/] Could not find versions that satisfies version range [green]{settings.VersionRange}[/].");
                return 0;
            }

            await AnsiConsole.Progress()
                .StartAsync(async ctx => 
                {
                    var task = ctx.AddTask("Deleting packages");

                    for (var i = 0; i < pending.Count; i++)
                    {
                        var package = pending[i];
                        var packageId = package.Identity.Id;
                        var packageVersion = package.Identity.Version.ToNormalizedString();

                        AnsiConsole.MarkupLine(
                          $"Deleting package [green]{packageId} {packageVersion}[/]..." +
                          (pending.Count > 1 ? $" [grey]({i + 1}/{pending.Count})[/]" : ""));

                        if (!settings.DryRun)
                        {
                            await updateResource.Delete(
                                packageId,
                                packageVersion,
                                getApiKey: packageSource => settings.ApiKey,
                                confirm: packageSource => true,
                                noServiceEndpoint: false,
                                logger);
                        }

                        task.Increment(100.0 / pending.Count);
                    }

                    task.Value = task.MaxValue;
                    task.StopTask();

                    AnsiConsole.WriteLine("Done!");
                });

            return 0;
        }
    }
}
