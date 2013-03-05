
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace Wintellect.Sterling.Server.Azure.TableStorage
{
    // https://github.com/glueckkanja/tasync/blob/master/StorageExtensions.cs

    public static class StorageExtensions
    {
        public static async Task<IList<CloudTable>> ListTablesAsync(
            this CloudTableClient client,
            CancellationToken ct = default(CancellationToken) )
        {
            var tables = new List<CloudTable>();

            TableContinuationToken token = null;

            do
            {
                TableResultSegment seg = await client.ListTablesAsync( token, ct );
                token = seg.ContinuationToken;
                tables.AddRange( seg );
            } while ( token != null && !ct.IsCancellationRequested );

            return tables;
        }

        public static Task<TableResultSegment> ListTablesAsync(
            this CloudTableClient client,
            TableContinuationToken token,
            CancellationToken ct = default(CancellationToken) )
        {
            ICancellableAsyncResult ar = client.BeginListTablesSegmented( token, null, null );
            ct.Register( ar.Cancel );

            return Task.Factory.FromAsync<TableResultSegment>( ar, client.EndListTablesSegmented );
        }

        public static Task<bool> CreateIfNotExistsAsync( this CloudTable table,
                                                        CancellationToken ct = default(CancellationToken) )
        {
            ICancellableAsyncResult ar = table.BeginCreateIfNotExists( null, null );
            ct.Register( ar.Cancel );

            return Task.Factory.FromAsync<bool>( ar, table.EndCreateIfNotExists );
        }

        public static Task<TableResult> ExecuteAsync(
            this CloudTable table,
            TableOperation operation,
            CancellationToken ct = default(CancellationToken) )
        {
            ICancellableAsyncResult ar = table.BeginExecute( operation, null, null );
            ct.Register( ar.Cancel );

            return Task.Factory.FromAsync<TableResult>( ar, table.EndExecute );
        }

        public static Task<IList<TableResult>> ExecuteBatchAsync(
            this CloudTable table,
            TableBatchOperation operation,
            CancellationToken ct = default(CancellationToken) )
        {
            ICancellableAsyncResult ar = table.BeginExecuteBatch( operation, null, null );
            ct.Register( ar.Cancel );

            return Task.Factory.FromAsync<IList<TableResult>>( ar, table.EndExecuteBatch );
        }

        public static async Task<IList<T>> ExecuteQueryAsync<T>(
            this CloudTable table,
            TableQuery<T> query,
            CancellationToken ct = default(CancellationToken),
            Action<IList<T>> onProgress = null )
            where T : ITableEntity, new()
        {
            var items = new List<T>();

            TableContinuationToken token = null;

            do
            {
                TableQuerySegment<T> seg = await table.ExecuteQueryAsync( query, token, ct );
                token = seg.ContinuationToken;
                items.AddRange( seg );
                if ( onProgress != null )
                    onProgress( items );
            } while ( token != null && !ct.IsCancellationRequested );

            return items;
        }

        public static Task<TableQuerySegment<T>> ExecuteQueryAsync<T>(
            this CloudTable table,
            TableQuery<T> query,
            TableContinuationToken token,
            CancellationToken ct = default(CancellationToken) )
            where T : ITableEntity, new()
        {
            ICancellableAsyncResult ar = table.BeginExecuteQuerySegmented( query, token, null, null );
            ct.Register( ar.Cancel );

            return Task.Factory.FromAsync<TableQuerySegment<T>>( ar, table.EndExecuteQuerySegmented<T> );
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>( this IEnumerable<T> items, int batchSize )
        {
            int i = 0;

            return from name in items
                   group name by i++ / batchSize
                       into part
                       select part.AsEnumerable();
        }
    }
}
