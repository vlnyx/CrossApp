using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Core;

namespace Cli;

internal static class Program
{
    static async Task Main(string[] args)
    {
        using var userCts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            userCts.Cancel();
        };

        using var timeoutCts = new CancellationTokenSource();
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);

        var service = new LibraryService();
        string dataPath = args.Length > 0 ? args[0] : "data/sample.json";

        int expectedTotal = 15;
        var progress = new Progress<int>(added =>
        {
            int percent = expectedTotal > 0 ? (int)((added / (double)expectedTotal) * 100) : 0;
            Console.Write($"\rProgress: {percent}% ({added}/{expectedTotal})   ");
        });

        try
        {
            Console.WriteLine("Starting import...");
            Console.WriteLine("Press Ctrl+C to cancel.\n");

            var importer = new JsonCatalogImporter();
            var added = await service.ImportAsync(importer, dataPath, progress, linkedCts.Token);

            Console.WriteLine($"\n\nImport completed successfully. Added: {added}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n\nImport cancelled.");

            if (timeoutCts.IsCancellationRequested)
            {
                Console.WriteLine("Reason: Timeout.");
            }
            else if (userCts.IsCancellationRequested)
            {
                Console.WriteLine("Reason: Cancelled by user (Ctrl+C).");
            }
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"\n\nData rejected: {ex.Message}");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"\n\nFile not found: {dataPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n\nAn error occurred: {ex.Message}");
        }
    }
}