using Dapper;
using MauiPetsApp.Application.Interfaces.Repositories;
using MauiPetsApp.Core.Application.Interfaces.DapperContext;
using MauiPetsApp.Core.Application.ViewModels.Despesas;
using MauiPetsApp.Core.Application.ViewModels.LookupTables;
using MauiPetsApp.Core.Domain;
using Serilog;
using System.Globalization;
using System.Text;

namespace MauiPetsApp.Infrastructure
{
    public class DespesaRepository : IDespesaRepository
    {
        private readonly IDapperContext _context;

        public DespesaRepository(IDapperContext context)
        {
            _context = context;
        }

        private static bool TryParseDate(string input, out DateOnly parsed)
        {
            parsed = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            // Prefer ISO (DB canonical)
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

        public async Task<int> InsertAsync(Despesa expense)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("INSERT INTO Despesa (");
            sb.Append("DataMovimento, ValorPago, Descricao, IdTipoDespesa, ");
            sb.Append("IdCategoriaDespesa, Notas, DataCriacao, TipoMovimento  ) ");
            sb.Append(" VALUES(");
            sb.Append("@DataMovimento, @ValorPago, @Descricao, @IdTipoDespesa, ");
            sb.Append("@IdCategoriaDespesa, @Notas, @DataCriacao, @TipoMovimento");
            sb.Append(");");
            sb.Append("SELECT last_insert_rowid()");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    // Normalize DataMovimento to ISO yyyy-MM-dd if possible
                    string dbDataMovimento = expense.DataMovimento ?? string.Empty;
                    if (TryParseDate(expense.DataMovimento, out var parsedMovimento))
                    {
                        dbDataMovimento = parsedMovimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }

                    // Normalize DataCriacao: prefer provided value, otherwise set now, stored as ISO
                    string dbDataCriacao = expense.DataCriacao ?? string.Empty;
                    if (TryParseDate(expense.DataCriacao, out var parsedCriacao))
                    {
                        dbDataCriacao = parsedCriacao.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        dbDataCriacao = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }

                    var parameters = new
                    {
                        DataMovimento = dbDataMovimento,
                        ValorPago = expense.ValorPago,
                        Descricao = expense.Descricao,
                        IdTipoDespesa = expense.IdTipoDespesa,
                        IdCategoriaDespesa = expense.IdCategoriaDespesa,
                        Notas = expense.Notas,
                        DataCriacao = dbDataCriacao,
                        TipoMovimento = expense.TipoMovimento
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


        public async Task<bool> UpdateAsync(int id, Despesa expense)
        {

            // Normalize DataMovimento to ISO when possible
            string dbDataMovimento = expense.DataMovimento ?? string.Empty;
            if (TryParseDate(expense.DataMovimento, out var parsedMovimento))
            {
                dbDataMovimento = parsedMovimento.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Id", expense.Id);
            dynamicParameters.Add("@DataMovimento", dbDataMovimento);
            dynamicParameters.Add("@ValorPago", expense.ValorPago);
            dynamicParameters.Add("@Descricao", expense.Descricao);
            dynamicParameters.Add("@IdTipoDespesa", expense.IdTipoDespesa);
            dynamicParameters.Add("@IdCategoriaDespesa", expense.IdCategoriaDespesa);
            dynamicParameters.Add("@TipoMovimento", expense.TipoMovimento);
            dynamicParameters.Add("@Notas", expense.Notas);

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE Despesa SET ");
            sb.Append("DataMovimento = @DataMovimento, ");
            sb.Append("ValorPago = @ValorPago, ");
            sb.Append("Descricao = @Descricao, ");
            sb.Append("IdTipoDespesa = @IdTipoDespesa, ");
            sb.Append("IdCategoriaDespesa = @IdCategoriaDespesa, ");
            sb.Append("TipoMovimento = @TipoMovimento, ");
            sb.Append("Notas = @Notas ");
            sb.Append("WHERE Id = @Id");


            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var updateOk = await connection.ExecuteAsync(sb.ToString(), param: dynamicParameters);
                    return updateOk > 0;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
        }

        public async Task DeleteAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM Despesa ");
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

        public async Task<IEnumerable<Despesa>?> GetAllAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Despesa.Id, DataCriacao, DataMovimento, ValorPago, ");
            sb.Append("Descricao, IdTipoDespesa, IdCategoriaDespesa, Notas, TipoMovimento ");
            sb.Append("FROM Despesa");
            using (var connection = _context.CreateConnection())
            {
                var expenses = await connection.QueryAsync<Despesa>(sb.ToString());
                if (expenses != null)
                {
                    return expenses;
                }
                else
                {
                    return null;
                }
            }
        }
        public async Task<Despesa?> GetByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Despesa.Id, DataCriacao, DataMovimento, ValorPago, ");
            sb.Append("Descricao, IdTipoDespesa, IdCategoriaDespesa, Notas, TipoMovimento ");
            sb.Append("FROM Despesa ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var expense = await connection.QuerySingleOrDefaultAsync<Despesa>(sb.ToString(), new { Id });
                    if (expense != null)
                    {
                        return expense;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        public List<DespesaVM> Query_ByYear(string sAno)
        {
            throw new NotImplementedException();
        }

        public decimal TotalDespesas(int iTipoDespesa = 0)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<TipoDespesa>?> GetTipoDespesa_ByCategoriaDespesa(int Id)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("SELECT id, Descricao, IdCategoriaDespesa ");
                sb.Append("FROM TipoDespesa ");
                sb.Append("WHERE IdCategoriaDespesa = @Id");


                using (var connection = _context.CreateConnection())
                {
                    var output = await connection.QueryAsync<TipoDespesa>(sb.ToString(), new { Id }) ?? null;
                    return output;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        public async Task<IEnumerable<TipoDespesa>?> GetTipoDespesas()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Id, Descricao, IdCategoriaDespesa ");
            sb.Append("FROM TipoDespesa ");

            using (var connection = _context.CreateConnection())
            {
                var output = await connection.QueryAsync<TipoDespesa>(sb.ToString());
                return output;
            }
        }

        public async Task<DespesaVM?> GetVMByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Despesa.Id, DataMovimento, ValorPago, ");
            sb.Append("Despesa.Descricao, Despesa.IdTipoDespesa, Despesa.IdCategoriaDespesa, Notas, TipoMovimento, ");
            sb.Append("CD.Descricao AS [DescricaoCategoriaDespesa], ");
            sb.Append("TD.Descricao AS [DescricaoTipoDespesa] ");
            sb.Append("FROM Despesa ");
            sb.Append("INNER JOIN CategoriaDespesa CD ON ");
            sb.Append("Despesa.IdCategoriaDespesa = CD.Id ");
            sb.Append("INNER JOIN TipoDespesa TD ON ");
            sb.Append("Despesa.IdTipoDespesa = TD.Id ");
            sb.Append("WHERE Despesa.Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var expenseVM = await connection.QueryFirstOrDefaultAsync<DespesaVM>(sb.ToString(), new { Id });
                    if (expenseVM != null)
                    {
                        return expenseVM;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return null;
            }
        }

        public async Task<IEnumerable<DespesaVM>?> GetAllVMAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Despesa.Id, DataMovimento, ValorPago, ");
            sb.Append("Despesa.Descricao, Despesa.IdTipoDespesa, Despesa.IdCategoriaDespesa, Notas, TipoMovimento, ");
            sb.Append("CD.Descricao AS [DescricaoCategoriaDespesa], ");
            sb.Append("TD.Descricao AS [DescricaoTipoDespesa] ");
            sb.Append("FROM Despesa ");
            sb.Append("INNER JOIN CategoriaDespesa CD ON ");
            sb.Append("Despesa.IdCategoriaDespesa = CD.Id ");
            sb.Append("INNER JOIN TipoDespesa TD ON ");
            sb.Append("Despesa.IdTipoDespesa = TD.Id ");
            sb.Append("ORDER BY date(Despesa.DataMovimento) DESC");

            using (var connection = _context.CreateConnection())
            {
                var expensesVM = await connection.QueryAsync<DespesaVM>(sb.ToString());
                if (expensesVM != null)
                {
                    return expensesVM;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<IEnumerable<DespesaVM>?> GetExpensesByYearAsync(int year)
        {
            var yearExpenses = (await GetAllVMAsync())?.ToList();
            if (yearExpenses == null) return Enumerable.Empty<DespesaVM>();

            return yearExpenses.Where(w =>
            {
                if (TryParseDate(w.DataMovimento, out var d))
                    return d.Year == year;
                return false;
            });
        }

        public async Task<IEnumerable<DespesaVM>?> GetExpensesByMonthAsync(int year, int month)
        {
            var yearExpenses = (await GetAllVMAsync())?.ToList();
            if (yearExpenses == null) return Enumerable.Empty<DespesaVM>();

            return yearExpenses.Where(w =>
            {
                if (TryParseDate(w.DataMovimento, out var d))
                    return d.Year == year && d.Month == month;
                return false;
            });
        }

        public async Task<LookupTableVM> GetDescricaoCategoriaDespesa(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT Id, Descricao ");
            sb.Append("FROM CategoriaDespesa ");
            sb.Append("WHERE Id = @Id");

            using (var connection = _context.CreateConnection())
            {
                var output = await connection.QuerySingleOrDefaultAsync<LookupTableVM>(sb.ToString(), new { Id });
                return output ?? new LookupTableVM();
            }
        }
    }
}
