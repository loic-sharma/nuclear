using System.Threading.Tasks;
using Spectre.Console.Cli;

namespace Nuclear
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var app = new CommandApp();

            app.Configure(config =>
            {
                config.SetApplicationName("nuclear");

                config.AddCommand<ListVersionsCommand>("list")
                    .WithExample(new[] { "list", "newtonsoft.json" })
                    .WithExample(new[] { "list", "newtonsoft.json", "12.*" });

                config.AddCommand<DeleteVersionsCommand>("delete")
                    .WithExample(new[] { "delete", "my.package", "1.0.0" })
                    .WithExample(new[] { "delete", "my.package", "1.1.*" })
                    .WithExample(new[] { "delete", "my.package", "2.0.0-*" })
                    .WithExample(new[] { "delete", "my.package", "3.*-*" })
                    .WithExample(new[] { "delete", "my.package", "*-*" });
            });

            await app.RunAsync(args);
        }
    }
}
