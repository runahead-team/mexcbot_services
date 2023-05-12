using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace mexcbot.Api.Infrastructure
{
    public static class DbConnections
    {
        public static async Task ExecAsync(Func<IDbConnection, Task> dbAction)
        {
            await using var dbConnection = new MySqlConnection(Configurations.DbConnectionString);
            await dbConnection.OpenAsync();

            await dbAction(dbConnection);

            if (dbConnection.State != ConnectionState.Closed)
                await dbConnection.CloseAsync();
        }
    }
}