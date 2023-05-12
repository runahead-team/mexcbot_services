using System.Threading.Tasks;
using Dapper;
using MySqlConnector;
using sp.Core.Token;
using mexcbot.Api.Infrastructure;

namespace mexcbot.Api.Services.SubService
{
  public class TokenManager : BaseTokenManager
  {
    public TokenManager(string hashKey) : base(hashKey)
    {
    }

    protected override async Task SaveToken(string id, string data)
    {
      await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

      await sqlConnection.ExecuteAsync("INSERT INTO Tokens(Id,`Data`) VALUES(@Id,@Data)", new
      {
        Id = id,
        Data = data
      });
    }

    protected override async Task<string> GetToken(string id)
    {
      await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

      return await sqlConnection.ExecuteScalarAsync<string>("SELECT `Data` FROM Tokens WHERE Id = @Id", new
      {
        Id = id
      });
    }

    protected override async Task DeleteToken(string id)
    {
      await using var sqlConnection = new MySqlConnection(Configurations.DbConnectionString);

      await sqlConnection.ExecuteScalarAsync<string>("DELETE FROM Tokens WHERE Id = @Id", new
      {
        Id = id
      });
    }
  }
}