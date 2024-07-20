using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class TaskRepository : IRepository<Task>
    {
        private readonly IDbConnection _db;

        public TaskRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Task> GetAll()
        {
            lock (_db)
            {
                return _db.Query<Task>("SELECT * FROM Tasks").ToList();
            }
        }

        public Task? GetById(int id)
        {
            lock (_db)
            {
                return _db.QuerySingleOrDefault<Task>("SELECT * FROM Tasks WHERE Id = @Id", new { Id = id });
            }
        }

        public int Add(Task task)
        {
            lock (_db)
            {
                var sql = "INSERT INTO Tasks (AccountId, TaskType, MinScheduleSeconds, MaxScheduleSeconds, NextRunTime) VALUES " +
                                            "(@AccountId, @TaskType, @MinScheduleSeconds, @MaxScheduleSeconds, @NextRunTime); " +
                          "SELECT last_insert_rowid();";
                return _db.ExecuteScalar<int>(sql, task);
            }
        }

        public void Update(Task task)
        {
            lock (_db)
            {
                var sql = "UPDATE Tasks SET AccountId = @AccountId, TaskType = @TaskType, MinScheduleSeconds = @MinScheduleSeconds, MaxScheduleSeconds = @MaxScheduleSeconds, NextRunTime = @NextRunTime WHERE Id = @Id";
                _db.Execute(sql, task);
            }
        }

        public void Delete(int id)
        {
            lock (_db)
            {
                var sql = "DELETE FROM Tasks WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
