using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace CalendarToSlack.Sql
{
    public class SqlDatabase : IDisposable
    {
        private SQLiteConnection _connection;
        private string _dbDirectory;

        public SqlDatabase(string dbDirectory)
        {
            _dbDirectory = dbDirectory;
        }

        public void Start()
        {
            var filePath = Path.Combine(_dbDirectory, "somedb.db");
            _connection = new SQLiteConnection($"Data Source={filePath}; Version=3");
            _connection.Open();
        }

        public void Bootstrap()
        {
            _connection.Execute(@"CREATE TABLE IF NOT EXISTS UserInfo (EmailAddress varchar(50), SlackAuthToken varchar(50), DefaultStatusText varchar(100), DefaultStatusEmoji varchar(100))");
            //_connection.Execute(@"INSERT INTO UserInfo (EmailAddress, SlackAuthToken, DefaultStatusText, DefaultStatusEmoji) values ('jordan.degner@hudl.com', '', 'test', ':mindblown:')");
        }

        public List<SqlUser> GetUsers()
        {
            return _connection.Query<SqlUser>("SELECT * FROM UserInfo").ToList();
        }

        public SqlUser GetUserByEmail(string email)
        {
            return _connection.Query<SqlUser>("SELECT * FROM UserInfo WHERE EmailAddress = @Email", new { Email = email }).FirstOrDefault();
        }

        public void Dispose()
        {
            _connection.Dispose(); 
        }
    }
}
