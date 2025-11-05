using Dapper;
using MauiPetsApp.Core.Application.Formatting;
using MauiPetsApp.Core.Application.Interfaces.DapperContext;
using MauiPetsApp.Core.Application.Interfaces.Repositories.TodoManager;
using MauiPetsApp.Core.Application.TodoManager;
using MauiPetsApp.Core.Domain.TodoManager;
using Serilog;
using System.Globalization;
using System.Text;

namespace MauiPetsApp.Infrastructure.TodoManager
{
    public class ToDoRepository : IToDoRepository
    {
        private readonly IDapperContext _context;

        public ToDoRepository(IDapperContext context)
        {
            _context = context;
        }

        // Mirror vaccines approach: try to parse incoming date and store a short date string
        string FormatToShortDateOrKeep(string? date)
        {
            if (string.IsNullOrWhiteSpace(date))
                return string.Empty;

            var s = date.Trim();

            // Use the tolerant DataFormat helper (tries multiple formats)
            var dt = DataFormat.DateParse(s);
            if (dt != DateTime.MinValue)
                return dt.ToShortDateString();

            // As a last attempt, try invariant/en-US/current explicit parses
            if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out dt)
                || DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out dt)
                || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal | DateTimeStyles.AllowWhiteSpaces, out dt))
            {
                return dt.ToShortDateString();
            }

            // Unable to parse — keep the trimmed original to avoid data loss
            return s;
        }

        public async Task<int> InsertAsync(ToDo toDo)
        {

            string convertedDbStartDate = Convert.ToDateTime(toDo.StartDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string convertedDbEndtDate = Convert.ToDateTime(toDo.EndDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder();

            sb.Append("INSERT INTO Todo (");
            sb.Append("Description, StartDate, EndDate, Completed, CategoryId, Generated) ");
            sb.Append(" VALUES(");
            sb.Append("@Description, @StartDate, @EndDate, @Completed, @CategoryId, 0");
            sb.Append(");");
            sb.Append("SELECT last_insert_rowid()");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var param = new
                    {
                        toDo.Description,
                        StartDate = convertedDbStartDate,
                        EndDate = convertedDbEndtDate,
                        toDo.Completed,
                        toDo.CategoryId
                    };

                    var result = await connection.QueryFirstAsync<int>(sb.ToString(), param: param);
                    return result;
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                return -1;
            }

        }

        public async Task UpdateAsync(int Id, ToDo toDo)
        {
            string convertedDbStartDate = Convert.ToDateTime(toDo.StartDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string convertedDbEndtDate = Convert.ToDateTime(toDo.EndDate).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            DynamicParameters dynamicParameters = new DynamicParameters();
            dynamicParameters.Add("@Id", toDo.Id);
            dynamicParameters.Add("@Description", toDo.Description);
            dynamicParameters.Add("@StartDate", convertedDbStartDate);
            dynamicParameters.Add("@EndDate", convertedDbEndtDate);
            dynamicParameters.Add("@Completed", toDo.Completed);
            dynamicParameters.Add("@CategoryId", toDo.CategoryId);

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE ToDo SET ");
            sb.Append("Description = @Description, ");
            sb.Append("StartDate = @StartDate, ");
            sb.Append("EndDate = @EndDate, ");
            sb.Append("Completed = @Completed, ");
            sb.Append("CategoryId = @CategoryId ");
            sb.Append("WHERE Id = @Id");

            using (var connection = _context.CreateConnection())
            {
                await connection.ExecuteAsync(sb.ToString(), param: dynamicParameters);
            }
        }

        public async Task DeleteAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM ToDo ");
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

        public async Task<ToDo> FindByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ToDo ");
            sb.Append($"WHERE Id = @Id");

            try
            {
                using (var connection = _context.CreateConnection())
                {
                    var pet = await connection.QuerySingleOrDefaultAsync<ToDo>(sb.ToString(), new { Id });
                    if (pet != null)
                    {
                        return pet;
                    }
                    else
                    {
                        return new ToDo();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString(), ex);
                return new ToDo();
            }
        }

        public async Task<IEnumerable<ToDo>> GetAllAsync()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ToDo ");
            sb.Append("ORDER BY date(StartDate) DESC");
            using (var connection = _context.CreateConnection())
            {
                var toDos = await connection.QueryAsync<ToDo>(sb.ToString());
                if (toDos != null)
                {
                    return toDos;
                }
                else
                {
                    return Enumerable.Empty<ToDo>();
                }
            }
        }

        public async Task<IEnumerable<ToDoDto>> GetAllVMAsync()
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ToDo.Id, ToDo.Description, StartDate, EndDate, Completed, Generated, ");
            sb.Append("TodoCategories.Id as [CategoryId], TodoCategories.Descricao AS [CategoryDescription] ");
            sb.Append("FROM ToDo ");
            sb.Append("INNER JOIN ToDoCategories ON ");
            sb.Append("ToDo.CategoryId = ToDoCategories.Id");

            using (var connection = _context.CreateConnection())
            {
                var result = await connection.QueryAsync<ToDoDto>(sb.ToString());
                if (result != null)
                {
                    return result;
                }
                else
                {
                    return Enumerable.Empty<ToDoDto>();
                }
            }
        }

        string ParseDate(string dateString, CultureInfo culture)
        {
            var dateParts = dateString.Split('-');
            if (dateParts.Length == 3)
            {
                int day = int.Parse(dateParts[0]);
                int month = int.Parse(dateParts[1]);
                int year = int.Parse(dateParts[2]);
                return $"{day:D2}-{month:D2}-{year:D4}"; // Format as "dd-MM-yyyy"
            }
            return string.Empty; // Handle parsing failure as needed
        }
        public async Task<ToDoDto> GetToDoVM_ByIdAsync(int Id)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ToDo.Id, ToDo.Description, StartDate, EndDate, Completed, Generated, ");
            sb.Append("TodoCategories.Id, TodoCategories.Descricao AS [CategoryDescription] ");
            sb.Append("FROM ToDo ");
            sb.Append("INNER JOIN ToDoCategories ON ");
            sb.Append("ToDo.CategoryId = ToDoCategories.Id ");
            sb.Append("WHERE ToDo.Id = @Id");


            using (var connection = _context.CreateConnection())
            {
                var ToDoVM = await connection.QueryFirstOrDefaultAsync<ToDoDto>(sb.ToString(), new { Id });
                if (ToDoVM != null)
                {
                    return ToDoVM;
                }
                else
                {
                    return new ToDoDto();
                }
            }
        }

        public async Task<IEnumerable<ToDoDto>> GetPending()
        {
            var query = GetSelectByCompletedString(1);

            using (var connection = _context.CreateConnection())
            {
                var pendingToDos = await connection.QueryAsync<ToDoDto>(query);
                if (pendingToDos != null)
                {
                    return pendingToDos;
                }
                else
                {
                    return Enumerable.Empty<ToDoDto>();
                }
            }
        }

        public async Task<IEnumerable<ToDoDto>> GetCompleted()
        {
            var query = GetSelectByCompletedString(2);

            using (var connection = _context.CreateConnection())
            {
                var completedToDos = await connection.QueryAsync<ToDoDto>(query);
                if (completedToDos != null)
                {
                    return completedToDos;
                }
                else
                {
                    return Enumerable.Empty<ToDoDto>();
                }
            }
        }

        private string GetSelectByCompletedString(int Completed)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT  ToDo.Id, ToDo.Description, StartDate, EndDate, ");
            sb.Append("Completed, Generated, TodoCategories.Descricao AS [CategoryDescription] ");
            sb.Append("FROM ToDo ");
            sb.Append("INNER JOIN ToDoCategories ON ");
            sb.Append("ToDo.CategoryId = ToDoCategories.Id ");
            sb.Append("WHERE ToDo.Id = @Id AND ");
            sb.Append($"Todo.Completed = {Completed}");

            return sb.ToString();

        }

        public async Task<IEnumerable<ToDoDto>> SearchTodosByTextAsync(string filter)
        {
            if (string.IsNullOrEmpty(filter))
                return await GetAllVMAsync();

            var todos = (await GetAllVMAsync())
                .ToList().
                Where(c => c.Description!.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
            return todos;
        }
    }
}