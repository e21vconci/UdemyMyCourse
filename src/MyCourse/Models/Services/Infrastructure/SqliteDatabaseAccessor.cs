using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace MyCourse.Models.Services.Infrastructure
{
    public class SqliteDatabaseAccessor : IDatabaseAccessor
    {
        public async Task<DataSet> QueryAsync(FormattableString formattableQuery)
        {
            //utilizzare l'interpolazione di C# per generare query parametrizzate
            var queryArguments = formattableQuery.GetArguments();
            var sqliteParameters = new List<SqliteParameter>();
            for (var i = 0; i < queryArguments.Length; i++)
            {
                var parameter = new SqliteParameter(i.ToString(), queryArguments[i]);
                sqliteParameters.Add(parameter);
                queryArguments[i] = "@" + i;
            }
            string query = formattableQuery.ToString();
            //-------------------

            using(var conn = new SqliteConnection("Data Source=Data/MyCourse.db"))
            {
                await conn.OpenAsync();
                using(var cmd = new SqliteCommand(query, conn))
                {
                    cmd.Parameters.AddRange(sqliteParameters);
                    using(var reader = await cmd.ExecuteReaderAsync())
                    {
                        var dataSet = new DataSet();
                        //comando da eseguire per evitare eccezione DataSet
                        dataSet.EnforceConstraints = false;

                        do
                        {
                            var dataTable = new DataTable();
                            dataSet.Tables.Add(dataTable);
                            dataTable.Load(reader);
                        } while (!reader.IsClosed);
                        
                        return dataSet;
                        /*while(reader.Read())
                        {
                            string title = (string) reader["Title"];
                        }*/
                    }
                }
            }
            //utilizzando using il metodo Dispose() viene invocato in automatico quando l'oggetto
            //non è più disponibile
            //conn.Dispose();
        }
    }
}