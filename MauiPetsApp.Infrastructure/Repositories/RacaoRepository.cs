using Dapper;
using MauiPetsApp.Core.Application.Interfaces.DapperContext;
using MauiPetsApp.Core.Application.Interfaces.Repositories;
using MauiPetsApp.Core.Application.ViewModels;
using MauiPetsApp.Core.Domain;
using Serilog;
using System.Globalization;
using System.Text;

namespace MauiPetsApp.Infrastructure
{
    public class RacaoRepository : IRacaoRepository
    {
        private readonly IDapperContext _context;
        public RacaoRepository(IDapperContext context)
        {
            _context = context;
        }

        private static bool TryParseDataCompra(string input, out DateOnly parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Primary: ISO (DB canonical)
            if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            // Common user formats (pt-PT)
            if (DateOnly.TryParseExact(input, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.GetCultureInfo("pt-PT"), DateTimeStyles.None, out parsed))
                return true;

            // Fallbacks
            if (DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return true;

            if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            return false;
        }

        public async Task<int> InsertAsync(Racao racao)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("INSERT INTO Racao (");
            sb.Append("DataCompra, Marca, QuantidadeDiaria, IdPet) ");
            sb.Append(" VALUES(");
            sb.Append("@DataCompra, @Marca, @QuantidadeDiaria, @IdPet");
            sb.Append(");");
            sb.Append("SELECT last_insert_rowid()");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    // Normalize DataCompra to ISO yyyy-MM-dd when possible
                    string dbDataCompra = racao.DataCompra ?? string.Empty;
                    if (TryParseDataCompra(racao.DataCompra, out var parsed))
                    {
                        dbDataCompra = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }

                    var parameters = new
                    {
                        DataCompra = dbDataCompra,
                        Marca = racao.Marca,
                        QuantidadeDiaria = racao.QuantidadeDiaria,
                        IdPet = racao.IdPet
                    };

                    var result = await connection.QueryFirstAsync<int>(sb.ToString(), param: parameters);
                    return result;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return -1;
            }
        }


        public async Task UpdateAsync(int Id, Racao racao)
        {
            // Normalize DataCompra to ISO when possible
            string dbDataCompra = racao.DataCompra ?? string.Empty;
            if (TryParseDataCompra(racao.DataCompra, out var parsed))
            {
                dbDataCompra = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Id", racao.Id);
            dynamicParameters.Add("@DataCompra", dbDataCompra);
            dynamicParameters.Add("@Marca", racao.Marca);
            dynamicParameters.Add("@QuantidadeDiaria", racao.QuantidadeDiaria);
            dynamicParameters.Add("@IdPet", racao.IdPet);

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Racao SET ");
            sb.Append("DataCompra = @DataCompra, ");
            sb.Append("Marca = @Marca, ");
            sb.Append("QuantidadeDiaria = @QuantidadeDiaria, ");
            sb.Append("IdPet = @IdPet ");
            sb.Append("WHERE Id = @Id");

            using (var connection = _context.CreateConnection())
            {
                await connection.ExecuteAsync(sb.ToString(), param: dynamicParameters);
            }
        }

        public async Task DeleteAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM Racao ");
            sb.Append("WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    await connection.ExecuteAsync(sb.ToString(), new { Id });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        public async Task<Racao> FindByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM Racao ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var racao = await connection.QuerySingleOrDefaultAsync<Racao>(sb.ToString(), new { Id });
                    if (racao != null)
                    {
                        return racao;
                    }
                    else
                    {
                        return new Racao();
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
                return new Racao();
            }
        }

        public async Task<IEnumerable<Racao>> GetAllAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM Racao ");
            using (var connection = _context.CreateConnection())
            {
                var racoes = await connection.QueryAsync<Racao>(sb.ToString());
                if (racoes != null)
                {
                    return racoes;
                }
                else
                {
                    return Enumerable.Empty<Racao>();
                }
            }
        }

        public async Task<IEnumerable<RacaoVM>> GetAllRacoesVMAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Racao.Id, DataCompra, Marca, QuantidadeDiaria, IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM Racao ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("Racao.IdPet = Pet.Id ");


            using (var connection = _context.CreateConnection())
            {
                var RacoesVM = await connection.QueryAsync<RacaoVM>(sb.ToString());
                if (RacoesVM != null)
                {
                    return RacoesVM;
                }
                else
                {
                    return Enumerable.Empty<RacaoVM>();
                }
            }
        }

        public async Task<IEnumerable<RacaoVM>> GetRacaoVMAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Racao.Id, DataCompra, Marca, QuantidadeDiaria, IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM Racao ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("Racao.IdPet = Pet.Id ");
            sb.Append("WHERE Racao.IdPet = @Id");


            using (var connection = _context.CreateConnection())
            {
                var racaoVM = await connection.QueryAsync<RacaoVM>(sb.ToString(), new { Id });
                if (racaoVM != null)
                {
                    return racaoVM;
                }
                else
                {
                    return Enumerable.Empty<RacaoVM>();
                }
            }
        }



    }
}
