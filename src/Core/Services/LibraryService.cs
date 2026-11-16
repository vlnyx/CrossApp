using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Core.Abstractions;
using Core.Domain;

namespace Core.Services
{
    public class LibraryService
    {
        private readonly List<BookCopy> _store = new List<BookCopy>();
        public event EventHandler<BookCopy>? Changed;

        public async Task<int> ImportAsync(
            ICatalogImporter importer, 
            string path, 
            IProgress<int>? progress = null, 
            CancellationToken ct = default)
        {
            var added = 0;
            
            await foreach (var item in importer.LoadAsync(path, ct))
            {
                if (_store.Exists(b => b.Id == item.Id)) continue;
                
                _store.Add(item);
                Changed?.Invoke(this, item);
                added++;
                
                progress?.Report(added);
            }
            return added;
        }
    }
}