using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Nuclear
{
    public class ListVersionsSettings : CommandSettings
    {
        [CommandArgument(0, "<PACKAGE>")]
        [Description("The package ID whose versions should be listed.")]
        public string PackageId {get; set; }

        [CommandArgument(1, "[VERSIONS]")]
        [Description("Optional. Filters results using floating version notation.")]
        [TypeConverter(typeof(NuclearVersionRangeConverter))]
        public NuclearVersionRange VersionRange { get; set; }

        [CommandOption("-s|--source <SOURCE>")]
        [Description("The NuGet package source URL.")]
        [DefaultValue("https://api.nuget.org/v3/index.json")]
        public string PackageSource { get; set; }

        [CommandOption("-o|--output <OUTPUT>")]
        [Description("Set the output format. Allowed values are t[grey]ty[/], or j[grey]son[/].")]
        [TypeConverter(typeof(OutputConverter))]
        [DefaultValue(Output.Tty)]
        public Output Output { get; set; }
    }

    [Description("Lists package versions.")]
    public class ListVersionsCommand : AsyncCommand<ListVersionsSettings>
    {
        public override async Task<int> ExecuteAsync([NotNull] CommandContext context, [NotNull] ListVersionsSettings settings)
        {
            var logger = NullLogger.Instance;
            var cancellationToken = CancellationToken.None;

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(settings.PackageSource);
            var resource = await repository.GetResourceAsync<PackageMetadataResource>();

            var packages = await resource.GetMetadataAsync(
                settings.PackageId,
                includePrerelease: true,
                includeUnlisted: true,
                cache,
                logger,
                cancellationToken);

            if (settings.VersionRange != null)
            {
                packages = packages.Where(p => settings.VersionRange.Includes(p.Identity.Version));
            }

            var results = packages
              .Select(package => new
              {
                  Id = package.Identity.Id,
                  Version = package.Identity.Version.ToNormalizedString(),
                  IsListed = package.IsListed
              })
              .ToList();

            if (settings.Output == Output.Tty)
            {
                AnsiConsole.MarkupLine($"Found [green]{results.Count}[/] results.");

                var table = new Table();

                table.AddColumn("Package Id");
                table.AddColumn("Version");
                table.AddColumn("Listed?");

                foreach (var result in results)
                {
                    table.AddRow(result.Id, result.Version, result.IsListed ? "Yes": "No");
                }

                AnsiConsole.Render(table);
                return 0;
            }

            if (settings.Output == Output.Json)
            {
                Console.WriteLine(JsonSerializer.Serialize(results));
                return 0;
            }

            return 1;
        }
    }

    public enum Output
    {
        Tty,
        Json
    }

    public class OutputConverter : TypeConverter
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = value as string;

            return stringValue?.ToLowerInvariant() switch
            {
                "t" => Output.Tty,
                "tty" => Output.Tty,
                "j" => Output.Json,
                "json" => Output.Json,

                _ => throw new NotSupportedException($"Unsupported output type {value}")
            };
        }
    }
}
