using BlumBotFarm.Database.Interfaces;
using Dapper;
using System.Data;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class TaskRepository : IRepository<Task>
    {
        private static readonly object dbLock = new();

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
                var sql = "INSERT INTO Tasks (AccountId, TaskType, MinScheduleSeconds, MaxScheduleSeconds, NextRunTime) VALUES (@AccountId, @TaskType, @MinScheduleSeconds, @MaxScheduleSeconds, @NextRunTime)";
                _db.Execute(sql, task);
            }
        }

        public void Update(Task task)
        {
            lock (dbLock)
            {
                var sql = "UPDATE Tasks SET AccountId = @AccountId, TaskType = @TaskType, MinScheduleSeconds = @MinScheduleSeconds, MaxScheduleSeconds = @MaxScheduleSeconds, NextRunTime = @NextRunTime WHERE Id = @Id";
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
