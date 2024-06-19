using Dapper;
using System.Data;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class TaskRepository
    {
        private static readonly object dbLock = new object();

        private readonly IDbConnection _db;

        public TaskRepository(IDbConnection db)
        {
            _db = db;
        }

        public IEnumerable<Task> GetAll()
        {
            lock (dbLock)
            {
                return _db.Query<Task>("SELECT * FROM Tasks").ToList();
            }
        }

        public Task? GetById(int id)
        {
            lock (dbLock)
            {
                return _db.QuerySingleOrDefault<Task>("SELECT * FROM Tasks WHERE Id = @Id", new { Id = id });
            }
        }

        public void Add(Task task)
        {
            lock (dbLock)
            {
                var sql = "INSERT INTO Tasks (AccountId, TaskType, ScheduleSeconds, NextRunTime) VALUES (@AccountId, @TaskType, @ScheduleSeconds, @NextRunTime)";
                _db.Execute(sql, task);
            }
        }

        public void Update(Task task)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Tasks SET AccountId = @AccountId, TaskType = @TaskType, ScheduleSeconds = @ScheduleSeconds, NextRunTime = @NextRunTime WHERE Id = @Id";
                _db.Execute(sql, task);
            }
        }

        public void Delete(int id)
        {
            lock (dbLock)
            {
                var sql = "DELETE FROM Tasks WHERE Id = @Id";
                _db.Execute(sql, new { Id = id });
            }
        }
    }
}
