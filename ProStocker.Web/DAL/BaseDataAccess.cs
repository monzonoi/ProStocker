using System.Data.SQLite;

namespace ProStocker.DAL
{
    public abstract class BaseDataAccess
    {
        private readonly string _connectionString;

        public BaseDataAccess(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        protected SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }
    }
}