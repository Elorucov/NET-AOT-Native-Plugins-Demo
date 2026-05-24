using System.Runtime.InteropServices;

namespace AOTUnmanagedPluginsDemo
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string pluginName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "plugin.dll" : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? "libplugin.dylib"
                : "libplugin.so";

            string pluginPath = Path.Combine(
                AppContext.BaseDirectory,
                pluginName);

            if (!File.Exists(pluginPath))
            {
                Console.Error.WriteLine($"Plugin not found: {pluginPath}");
                Console.Error.WriteLine("Build the plugin (make / cmake) and copy it next to the host.");
                Environment.Exit(1);
            }

            Console.WriteLine($"Loading plugin: {pluginPath}");

            using var plugin = new PluginLoader(pluginPath);

            int version = plugin.Version();
            Console.WriteLine($"Version: {version}");

            string greeting = plugin.Greet("World");
            Console.WriteLine($"Plugin says: {greeting}");


            Console.WriteLine("Starting long computation asynchronously (2 seconds inside the plugin)...");

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var sw = System.Diagnostics.Stopwatch.StartNew();

            Task<long> computeTask = plugin.ComputeAsync(100, 200, cts.Token);

            int dots = 0;
            while (!computeTask.IsCompleted)
            {
                await Task.Delay(250, CancellationToken.None);
                Console.Write('.');
                dots++;
            }
            if (dots > 0) Console.WriteLine();

            long result = await computeTask;

            sw.Stop();
            Console.WriteLine($"Result: {result}  (in {sw.ElapsedMilliseconds} ms)");

            Console.WriteLine("\nDemonstration of cancellation (1-second timeout for 2-second computation):");
            using var ctsFast = new CancellationTokenSource(TimeSpan.FromSeconds(1));

            try
            {
                long _ = await plugin.ComputeAsync(1, 2, ctsFast.Token);
                Console.WriteLine("Computation completed (unexpectedly fast).");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Computation canceled by timeout — host is not blocked!");
            }

            Console.WriteLine("\nDone.");
        }
    }
}
