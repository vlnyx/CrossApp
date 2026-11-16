using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public sealed class JsonCatalogImporter : ICatalogImporter
{
    public async IAsyncEnumerable<BookCopy> LoadAsync(string path, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        
        var dtos = JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto>(stream, cancellationToken: ct);
        
        await foreach (var dto in dtos.WithCancellation(ct))
        {
            if (dto is null) continue;
            
            BookCopy copy;
            try
            {
                copy = BookCopy.FromDto(dto);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Помилка валідації даних: {ex.Message}", ex);
            }
            
            yield return copy;
        }
    }
}