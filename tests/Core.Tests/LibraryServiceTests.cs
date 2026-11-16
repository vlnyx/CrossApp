using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Domain;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public class LibraryServiceTests
{
    [Fact]
    public void ReturnCopy_WhenNotIssued_ThrowsDomainException()
    {
        var store = new InMemoryBookStore();
        var service = new LendingService(store);
        var book = BookCopy.Create("B1", "ISBN-1", "Test Book", "Author");
        store.Add(book);

        Assert.Throws<DomainException>(() => service.ReturnCopy(book.Id));
    }

    [Fact]
    public void IssueCopy_WhenAlreadyIssued_ThrowsDomainException()
    {
        var store = new InMemoryBookStore();
        var service = new LendingService(store);
        var book = BookCopy.Create("B2", "ISBN-2", "Test Book 2", "Author");
        store.Add(book);
        service.IssueCopy(book.Id);

        Assert.Throws<DomainException>(() => service.IssueCopy(book.Id));
    }

    [Fact]
    public async Task ImportAsync_RaisesChangedEvent()
    {
        var service = new LibraryService();
        var events = new List<BookCopy>();
        service.Changed += (_, e) => events.Add(e);
        
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "[{\"Id\":\"B3\",\"Title\":\"Book 3\"}]");
            await service.ImportAsync(new JsonCatalogImporter(), path, progress: null, ct: CancellationToken.None);
            
            Assert.NotEmpty(events);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ImportAsync_WhenAlreadyCancelled_Throws()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LibraryService();
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.ImportAsync(new JsonCatalogImporter(), "any.json", progress: null, ct: cts.Token));
    }

    [Fact]
    public async Task ImportAsync_WithInvalidJson_ThrowsDomainException()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "invalid json");
            var service = new LibraryService();
            
            await Assert.ThrowsAsync<DomainException>(
                () => service.ImportAsync(new JsonCatalogImporter(), path, progress: null, ct: CancellationToken.None));
        }
        finally
        {
            File.Delete(path);
        }
    }
}