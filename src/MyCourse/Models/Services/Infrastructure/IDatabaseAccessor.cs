using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace MyCourse.Models.Services.Infrastructure
{
    public interface IDatabaseAccessor
    {
        IAsyncEnumerable<IDataRecord> QueryAsync(FormattableString query, CancellationToken token = default(CancellationToken));
        Task<T> QueryScalarAsync<T>(FormattableString formattableQuery, CancellationToken token = default(CancellationToken));
        Task<int> CommandAsync(FormattableString formattableCommand, CancellationToken token = default(CancellationToken));
    }
}