using BlumBotFarm.Database.Interfaces;
using Dapper;
using Microsoft.Data.Sqlite;
using Task = BlumBotFarm.Core.Models.Task;

namespace BlumBotFarm.Database.Repositories
{
    public class TaskRepository : IRepository<Task>
    {
        private static readonly object _lock = new();

        private readonly string _connectionString;

        public TaskRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IEnumerable<Task> GetAll()
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.Query<Task>("SELECT * FROM Tasks").ToList();
                }
            }
        }

        public Task? GetById(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    return db.QuerySingleOrDefault<Task>("SELECT * FROM Tasks WHERE Id = @Id", new { Id = id });
                }
            }
        }

        public int Add(Task task)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "INSERT INTO Tasks (AccountId, TaskType, MinScheduleSeconds, MaxScheduleSeconds, NextRunTime) VALUES " +
                              "(@AccountId, @TaskType, @MinScheduleSeconds, @MaxScheduleSeconds, @NextRunTime); " +
                              "SELECT last_insert_rowid();";
                    return db.ExecuteScalar<int>(sql, task);
                }
            }
        }

        public void Update(Task task)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "UPDATE Tasks SET AccountId = @AccountId, TaskType = @TaskType, MinScheduleSeconds = @MinScheduleSeconds, MaxScheduleSeconds = @MaxScheduleSeconds, NextRunTime = @NextRunTime WHERE Id = @Id";
                    db.Execute(sql, task);
                }
            }
        }

        public void Delete(int id)
        {
            lock (_lock)
            {
                using (var db = Database.CreateConnection(_connectionString))
                {
                    var sql = "DELETE FROM Tasks WHERE Id = @Id";
                    db.Execute(sql, new { Id = id });
                }
            }
        }
    }
}