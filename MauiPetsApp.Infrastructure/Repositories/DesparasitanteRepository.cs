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
    public class DesparasitanteRepository : IDesparasitanteRepository
    {
        private readonly IDapperContext _context;

        public DesparasitanteRepository(IDapperContext context)
        {
            _context = context;
        }

        private static bool TryParseDate(string input, out DateOnly parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Prefer ISO (DB canonical), then pt-PT formats, then fallbacks
            if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            if (DateOnly.TryParseExact(input, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.GetCultureInfo("pt-PT"), DateTimeStyles.None, out parsed))
                return true;

            if (DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return true;

            if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            return false;
        }

        public async Task<int> InsertAsync(Desparasitante desparasitante)
        {
            var petName = await GetPetName(desparasitante.IdPet);
            var description = $"{petName} - Desparasitante {desparasitante.Marca}";
            var categoryId = await GetDewormerTodoCategoryId("Med");

            string dbDataAplicacao = Convert.ToDateTime(desparasitante.DataAplicacao).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dbDataProximaAplicacao = Convert.ToDateTime(desparasitante.DataProximaAplicacao).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            int result;

            StringBuilder sb = new StringBuilder();

            sb.Append("INSERT INTO Desparasitante (");
            sb.Append("Tipo, Marca, DataAplicacao, DataProximaAplicacao, IdPet) ");
            sb.Append(" VALUES(");
            sb.Append("@Tipo, @Marca, @DataAplicacao, @DataProximaAplicacao, @IdPet");
            sb.Append(");");
            sb.Append("SELECT last_insert_rowid()");

            using (var connection = _context.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // If you want to create the ToDo now, use startDate/endDate values (they are culture-safe formatted strings).
                        var parameters = new
                        {
                            desparasitante.Tipo,
                            desparasitante.Marca,
                            DataAplicacao = dbDataAplicacao,
                            DataProximaAplicacao = dbDataProximaAplicacao,
                            desparasitante.IdPet
                        };

                        result = await connection.QueryFirstAsync<int>(sb.ToString(), param: parameters);
                        transaction.Commit();

                        return result;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.ToString());
                        transaction.Rollback();
                        return -1;
                    }
                }
            }
        }


        public async Task UpdateAsync(int Id, Desparasitante desparasitante)
        {
            string dbDataAplicacao = Convert.ToDateTime(desparasitante.DataAplicacao).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string dbDataProximaAplicacao = Convert.ToDateTime(desparasitante.DataProximaAplicacao).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Id", desparasitante.Id);
            dynamicParameters.Add("@DataAplicacao", dbDataAplicacao);
            dynamicParameters.Add("@DataProximaAplicacao", dbDataProximaAplicacao);
            dynamicParameters.Add("@Marca", desparasitante.Marca);
            dynamicParameters.Add("@Tipo", desparasitante.Tipo);
            dynamicParameters.Add("@IdPet", desparasitante.IdPet);

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Desparasitante SET ");
            sb.Append("DataAplicacao = @DataAplicacao, ");
            sb.Append("DataProximaAplicacao = @DataProximaAplicacao, ");
            sb.Append("Marca = @Marca, ");
            sb.Append("Tipo = @Tipo, ");
            sb.Append("IdPet = @IdPet ");
            sb.Append("WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    await connection.ExecuteAsync(sb.ToString(), param: dynamicParameters);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        public async Task DeleteAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM Desparasitante ");
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

        public async Task<Desparasitante> FindByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM Desparasitante ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var desparasitante = await connection.QuerySingleOrDefaultAsync<Desparasitante>(sb.ToString(), new { Id });
                    if (desparasitante != null)
                    {
                        return desparasitante;
                    }
                    else
                    {
                        return new Desparasitante();
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
                return new Desparasitante();
            }
        }

        public async Task<IEnumerable<Desparasitante>> GetAllAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM Desparasitante ");
            using (var connection = _context.CreateConnection())
            {
                var desparasitantes = await connection.QueryAsync<Desparasitante>(sb.ToString());
                if (desparasitantes != null)
                {
                    return desparasitantes;
                }
                else
                {
                    return Enumerable.Empty<Desparasitante>();
                }
            }
        }

        public async Task<IEnumerable<DesparasitanteVM>> GetAllDesparasitantesVMAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Desparasitante.Id, DataAplicacao, DataProximaAplicacao, ");
            sb.Append("Marca, Tipo, IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM Desparasitante ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("Desparasitante.IdPet = Pet.Id ");


            using (var connection = _context.CreateConnection())
            {
                var desparasitantesVM = await connection.QueryAsync<DesparasitanteVM>(sb.ToString());
                if (desparasitantesVM != null)
                {
                    return desparasitantesVM;
                }
                else
                {
                    return Enumerable.Empty<DesparasitanteVM>();
                }
            }
        }

        public async Task<IEnumerable<DesparasitanteVM>> GetDesparasitanteVMAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Desparasitante.Id, DataAplicacao, DataProximaAplicacao, Marca, Tipo, IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM Desparasitante ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("Desparasitante.IdPet = Pet.Id ");
            sb.Append("WHERE Desparasitante.IdPet = @Id");


            using (var connection = _context.CreateConnection())
            {
                var desparasitanteVM = await connection.QueryAsync<DesparasitanteVM>(sb.ToString(), new { Id });
                if (desparasitanteVM != null)
                {
                    return desparasitanteVM;
                }
                else
                {
                    return Enumerable.Empty<DesparasitanteVM>();
                }
            }
        }

        public async Task<string> GetPetName(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM Pet ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var pet = await connection.QuerySingleOrDefaultAsync<Pet>(sb.ToString(), new { Id });
                    if (pet != null)
                    {
                        return pet.Nome;
                    }
                    else
                    {
                        return "";
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
                return "";
            }
        }
        private async Task<int> GetDewormerTodoCategoryId(string descricao)
        {
            DynamicParameters paramCollection = new DynamicParameters();
            paramCollection.Add("@Descricao", descricao);
            string Query = $"SELECT Id FROM ToDoCategories WHERE Descricao LIKE '{descricao}%'";

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    return await connection.QueryFirstOrDefaultAsync<int>(Query, paramCollection);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return 0;
            }
        }

    }
}