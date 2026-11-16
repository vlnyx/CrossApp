using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;
using Core.Services;
using Xunit;

namespace Core.Tests;

public class LinkedTokenTests
{
    private class SlowMockImporter : ICatalogImporter
    {
        public async IAsyncEnumerable<BookCopy> LoadAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
        {
            int id = 1;
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(100, ct);
                var currentId = id++;
                yield return BookCopy.Create(
                    currentId.ToString(),
                    $"ISBN-{currentId}",
                    "Book",
                    "Test");
            }
        }
    }

    [Fact]
    public async Task ImportAsync_WithLinkedToken_FailsSpecificallyDueToTimeout()
    {
        using var userTokenSource = new CancellationTokenSource();
        using var timeoutTokenSource = new CancellationTokenSource();
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(userTokenSource.Token, timeoutTokenSource.Token);

        var service = new LibraryService();
        var mockImporter = new SlowMockImporter();

        timeoutTokenSource.CancelAfter(TimeSpan.FromMilliseconds(200));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(mockImporter, "dummy.json", progress: null, ct: linkedTokenSource.Token));

        Assert.True(timeoutTokenSource.IsCancellationRequested);
        Assert.False(userTokenSource.IsCancellationRequested);
    }
}