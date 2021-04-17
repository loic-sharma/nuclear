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
                    .WithExample(new[] { "delete", "my.package", "3.*-*" });
            });

            await app.RunAsync(args);

            // await AnsiConsole.Progress()
            //     .StartAsync(async ctx => 
            //     {
            //         // Define tasks
            //         var task1 = ctx.AddTask("Discovering packages");
            //         var task2 = ctx.AddTask("Deleting packages");

            //         while (!ctx.IsFinished) 
            //         {
            //             await Task.Delay(TimeSpan.FromSeconds(0.1));

            //             task1.Increment(5);
            //             task2.Increment(2);
            //         }
            //     });

            // AnsiConsole.Markup("[bold green]Done![/]");
        }
    }
}
