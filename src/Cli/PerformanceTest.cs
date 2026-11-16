using System;
using System.Diagnostics;
using System.Linq;
using Core.Domain;

namespace Cli;

public static class PerformanceTest
{
    public static void Run()
    {
        Console.WriteLine("Генерація 100 000 записів...");
        var largeData = Enumerable.Range(1, 100_000)
            .Select(i =>
            {
                var book = BookCopy.Create(i.ToString(), $"ISBN-{i % 50:D3}");
                if (i % 10 == 0)
                    book.Issue();
                return book;
            })
            .ToList();

        var sw = new Stopwatch();

        sw.Start();
        var streamedReport = largeData
            .Where(book => !book.IsIssued)
            .GroupBy(book => book.Isbn)
            .Select(group => new { Isbn = group.Key, Copies = group.Count() })
            .ToList();
        sw.Stop();
        var streamedTime = sw.ElapsedMilliseconds;

        sw.Restart();
        var materializedReport = largeData
            .Where(book => !book.IsIssued)
            .ToList()
            .GroupBy(book => book.Isbn)
            .Select(group => new { Isbn = group.Key, Copies = group.Count() })
            .ToList();
        sw.Stop();
        var materializedTime = sw.ElapsedMilliseconds;

        if (!streamedReport.SequenceEqual(materializedReport))
            throw new InvalidOperationException("Результати LINQ-звітів відрізняються.");

        Console.WriteLine($"Час БЕЗ проміжного ToList: {streamedTime} мс");
        Console.WriteLine($"Час З проміжним ToList: {materializedTime} мс");
    }
}