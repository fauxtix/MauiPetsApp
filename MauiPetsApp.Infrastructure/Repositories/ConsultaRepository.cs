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
    public class ConsultaRepository : IConsultaRepository
    {
        private readonly IDapperContext _context;
        public ConsultaRepository(IDapperContext context)
        {
            _context = context;
        }

        private static bool TryParseDate(string input, out DateOnly parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // DB canonical: ISO date
            if (DateOnly.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            // Common user formats (pt-PT)
            if (DateOnly.TryParseExact(input, new[] { "dd/MM/yyyy", "d/M/yyyy" }, CultureInfo.GetCultureInfo("pt-PT"), DateTimeStyles.None, out parsed))
                return true;

            // Fallbacks: current culture then invariant
            if (DateOnly.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
                return true;

            if (DateOnly.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return true;

            return false;
        }

        public async Task<int> InsertAsync(ConsultaVeterinario Consulta)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("INSERT INTO ConsultaVeterinario (");
            sb.Append("DataConsulta, Motivo, Diagnostico, Tratamento, Notas, IdPet) ");
            sb.Append(" VALUES(");
            sb.Append("@DataConsulta, @Motivo, @Diagnostico, @Tratamento, @Notas, @IdPet");
            sb.Append(");");
            sb.Append("SELECT last_insert_rowid()");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    // Normalize date to ISO (yyyy-MM-dd) if possible to avoid culture-dependent parsing later
                    string dbDataConsulta = Consulta.DataConsulta ?? string.Empty;
                    if (TryParseDate(Consulta.DataConsulta, out var parsed))
                    {
                        dbDataConsulta = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }

                    var parameters = new
                    {
                        DataConsulta = dbDataConsulta,
                        Motivo = Consulta.Motivo,
                        Diagnostico = Consulta.Diagnostico,
                        Tratamento = Consulta.Tratamento,
                        Notas = Consulta.Notas,
                        IdPet = Consulta.IdPet
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

        public async Task UpdateAsync(int Id, ConsultaVeterinario Consulta)
        {
            // Normalize date before saving
            string dbDataConsulta = Consulta.DataConsulta ?? string.Empty;
            if (TryParseDate(Consulta.DataConsulta, out var parsed))
            {
                dbDataConsulta = parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Id", Consulta.Id);
            dynamicParameters.Add("@DataConsulta", dbDataConsulta);
            dynamicParameters.Add("@Motivo", Consulta.Motivo);
            dynamicParameters.Add("@Diagnostico", Consulta.Diagnostico);
            dynamicParameters.Add("@Tratamento", Consulta.Tratamento);
            dynamicParameters.Add("@Notas", Consulta.Notas);
            dynamicParameters.Add("@IdPet", Consulta.IdPet);

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE ConsultaVeterinario SET ");
            sb.Append("DataConsulta = @DataConsulta, ");
            sb.Append("Motivo = @Motivo, ");
            sb.Append("Diagnostico = @Diagnostico, ");
            sb.Append("Tratamento = @Tratamento, ");
            sb.Append("Notas = @Notas, ");
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
            sb.Append("DELETE FROM ConsultaVeterinario ");
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

        public async Task<ConsultaVeterinario> FindByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ConsultaVeterinario ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var pet = await connection.QuerySingleOrDefaultAsync<ConsultaVeterinario>(sb.ToString(), new { Id });
                    if (pet != null)
                    {
                        return pet;
                    }
                    else
                    {
                        return new ConsultaVeterinario();
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
                return new ConsultaVeterinario();
            }
        }

        public async Task<IEnumerable<ConsultaVeterinario>> GetAllAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ConsultaVeterinario ");
            using (var connection = _context.CreateConnection())
            {
                var Consultas = await connection.QueryAsync<ConsultaVeterinario>(sb.ToString());
                if (Consultas != null)
                {
                    return Consultas;
                }
                else
                {
                    return Enumerable.Empty<ConsultaVeterinario>();
                }
            }
        }

        public async Task<IEnumerable<ConsultaVeterinarioVM>> GetAllConsultaVMAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ConsultaVeterinario.Id, DataConsulta, Motivo, ");
            sb.Append("Diagnostico, Tratamento, IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM ConsultaVeterinario ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("ConsultaVeterinario.IdPet = Pet.Id");


            using (var connection = _context.CreateConnection())
            {
                var ConsultasVM = await connection.QueryAsync<ConsultaVeterinarioVM>(sb.ToString());
                if (ConsultasVM != null)
                {
                    return ConsultasVM;
                }
                else
                {
                    return Enumerable.Empty<ConsultaVeterinarioVM>();
                }
            }
        }

        public async Task<IEnumerable<ConsultaVeterinarioVM>> GetConsultaVMAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ConsultaVeterinario.Id, DataConsulta, Motivo, ");
            sb.Append("Diagnostico, Tratamento, Notas, ");
            sb.Append("IdPet, Pet.Nome AS [NomePet] ");
            sb.Append("FROM ConsultaVeterinario ");
            sb.Append("INNER JOIN Pet ON ");
            sb.Append("ConsultaVeterinario.IdPet = Pet.Id ");
            sb.Append("WHERE ConsultaVeterinario.IdPet = @Id");


            using (var connection = _context.CreateConnection())
            {
                var ConsultaVM = await connection.QueryAsync<ConsultaVeterinarioVM>(sb.ToString(), new { Id });
                if (ConsultaVM != null)
                {
                    return ConsultaVM;
                }
                else
                {
                    return Enumerable.Empty<ConsultaVeterinarioVM>();
                }
            }
        }

    }
}
