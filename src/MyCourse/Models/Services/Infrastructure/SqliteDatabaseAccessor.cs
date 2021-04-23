using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MyCourse.Models.Options;
using Microsoft.Extensions.Logging;
using MyCourse.Models.ValueTypes;
using MyCourse.Models.Exceptions.Infrastructure;
using System.Threading;
using Polly;

namespace MyCourse.Models.Services.Infrastructure
{
    public class SqliteDatabaseAccessor : IDatabaseAccessor
    {
        //private readonly IConfiguration configuration;
        private readonly IOptionsMonitor<ConnectionStringsOptions> connectionStringOptions;
        private readonly ILogger<SqliteDatabaseAccessor> logger;

        public SqliteDatabaseAccessor(ILogger<SqliteDatabaseAccessor> logger, IOptionsMonitor<ConnectionStringsOptions> connectionStringOptions)
        {
            this.logger = logger;
            this.connectionStringOptions = connectionStringOptions;
            //this.configuration = configuration;
        }

        public async Task<int> CommandAsync(FormattableString formattableCommand, CancellationToken token)
        {
            try
            {
                using SqliteConnection conn = await GetOpenedConnection(token);
                using SqliteCommand cmd = GetCommand(formattableCommand, conn);
                int affectedRows = await cmd.ExecuteNonQueryAsync(token);
                return affectedRows;
            }
            catch (SqliteException exc) when (exc.SqliteErrorCode == 19)
            {
                throw new ConstraintViolationException(exc);
            }
        }

        public async Task<T> QueryScalarAsync<T>(FormattableString formattableQuery, CancellationToken token)
        {
            try
            {
                using SqliteConnection conn = await GetOpenedConnection(token);
                using SqliteCommand cmd = GetCommand(formattableQuery, conn);
                object result = await cmd.ExecuteScalarAsync();
                // converte l'oggetto in base al tipo stabilito dal chiamante 
                return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (SqliteException exc) when (exc.SqliteErrorCode == 19)
            {
                throw new ConstraintViolationException(exc);
            }
        }

        // UTILIZZO ASYNC STREAMS
        public async IAsyncEnumerable<IDataRecord> QueryAsync(FormattableString formattableQuery)
        {
            logger.LogInformation(formattableQuery.Format, formattableQuery.GetArguments());

            //Creiamo dei SqliteParameter a partire dalla FormattableString
            var queryArguments = formattableQuery.GetArguments();
            var sqliteParameters = new List<SqliteParameter>();
            for (var i = 0; i < queryArguments.Length; i++)
            {
                if (queryArguments[i] is Sql)
                {
                    continue;
                }
                var parameter = new SqliteParameter(i.ToString(), queryArguments[i]);
                sqliteParameters.Add(parameter);
                queryArguments[i] = "@" + i;
            }
            string query = formattableQuery.ToString();

            //Colleghiamoci al database Sqlite, inviamo la query e leggiamo i risultati
            string connectionString = connectionStringOptions.CurrentValue.Default;

            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqliteCommand(query, conn))
                {
                    //Aggiungiamo i SqliteParameters al SqliteCommand
                    cmd.Parameters.AddRange(sqliteParameters);

                    //Inviamo la query al database e otteniamo un SqliteDataReader
                    //per leggere i risultati
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            yield return reader;
                        }
                    }
                }
            }
        }

        private static SqliteCommand GetCommand(FormattableString formattableQuery, SqliteConnection conn)
        {
            //Creiamo dei SqliteParameter a partire dalla FormattableString
            //utilizzare l'interpolazione di C# per generare query parametrizzate
            var queryArguments = formattableQuery.GetArguments();
            var sqliteParameters = new List<SqliteParameter>();
            for (var i = 0; i < queryArguments.Length; i++)
            {
                if (queryArguments[i] is Sql)
                {
                    continue;
                }
                var parameter = new SqliteParameter(name: i.ToString(), value: queryArguments[i] ?? DBNull.Value);
                sqliteParameters.Add(parameter);
                queryArguments[i] = "@" + i;
            }
            string query = formattableQuery.ToString();

            var cmd = new SqliteCommand(query, conn);
            // Aggiungiamo i SqliteParameters al SqliteCommand
            cmd.Parameters.AddRange(sqliteParameters);
            return cmd;
        }

        private async Task<SqliteConnection> GetOpenedConnection(CancellationToken token)
        {
            // Colleghiamoci al database Sqlite, inviamo la query e leggiamo i risultati
            var conn = new SqliteConnection(connectionStringOptions.CurrentValue.Default);

            // Utilizziamo Polly in modo da tentare di nuovo l'apertura del db in caso di errori
            var policy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(2, retry => 
                    TimeSpan.FromMilliseconds(1000));
            await policy.ExecuteAsync(() => 
                conn.OpenAsync());

            return conn;
        }
    }
}