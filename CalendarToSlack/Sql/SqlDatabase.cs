using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalendarToSlack.Sql
{
    public class SqlDatabase : IDisposable
    {
        private SQLiteConnection _conn;
        private string _dbDirectory;

        public SqlDatabase(string dbDirectory)
        {
            _dbDirectory = dbDirectory;
        }

        public void Start()
        {
            var filePath = Path.Combine(_dbDirectory, "somedb.db");
            _conn = new SQLiteConnection($"Data Source={filePath}; Version=3");
            _conn.Open();
        }

        public List<SqlUser> Bootstrap()
        {
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    @"CREATE TABLE UserInfo (EmailAddress varchar(50), SlackAuthToken varchar(50), DefaultStatusText varchar(100), DefaultStatusEmoji varchar(100))";

                cmd.ExecuteNonQuery();
            }

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText =
                    @"INSERT INTO UserInfo (EmailAddress, SlackAuthToken, DefaultStatusText, DefaultStatusEmoji) values ('jordan.degner@hudl.com', '', 'test', ':mindblown:')";

                cmd.ExecuteNonQuery();
            }

            return GetUsers();
        }

        public List<SqlUser> GetUsers()
        {
            var users = new List<SqlUser>();

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM UserInfo";
                cmd.CommandType = CommandType.Text;

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    users.Add(new SqlUser
                    {
                        EmailAddress = reader["EmailAddress"] as string,
                        SlackAuthToken = reader["SlackAuthToken"] as string,
                        DefaultStatusText = reader["DefaultStatusText"] as string,
                        DefaultStatusEmoji = reader["DefaultStatusEmoji"] as string
                    });
                }
            }

            return users;
        }

        public SqlUser GetUser()
        {
            SqlUser user = null;

            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT * FROM UserInfo";

                var reader = cmd.ExecuteReader();
                reader.Read();

                user = new SqlUser
                {
                    EmailAddress = reader["EmailAddress"] as string,
                    SlackAuthToken = reader["SlackAuthToken"] as string,
                    DefaultStatusText = reader["DefaultStatusText"] as string,
                    DefaultStatusEmoji = reader["DefaultStatusEmoji"] as string
                };
            }

            return user;
        }

        public void Dispose()
        {
            _conn.Dispose(); 
        }
    }
}
