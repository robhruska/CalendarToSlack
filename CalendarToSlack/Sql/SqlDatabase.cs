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
            var filePath = Path.Combine(_dbDirectory, "somedb3.db");
            _connection = new SQLiteConnection($"Data Source={filePath}; Version=3");
            _connection.Open();
        }

        public void Bootstrap()
        {
            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Users (
                    EmailAddress VARCHAR(100) NOT NULL,
                    SlackAuthToken VARCHAR(50) NOT NULL,
                    DefaultStatusText VARCHAR(100),
                    DefaultStatusEmoji VARCHAR(100),
                    PRIMARY KEY (EmailAddress)
                )");

            _connection.Execute(@"
                CREATE TABLE IF NOT EXISTS Statuses (
                    EmailAddress VARCHAR(100) NOT NULL,
                    Match VARCHAR(100) NOT NULL,
                    Text VARCHAR(100),
                    Emoji VARCHAR(102),
                    FOREIGN KEY (EmailAddress) REFERENCES Users(EmailAddress) ON DELETE CASCADE
                )");
        }

        // TODO caller should set status text / status emoji to defaults if a new user
        public void CreateOrUpdateUser(SqlUser user)
        {
            if (GetUserByEmail(user.EmailAddress) == null)
            {
                _connection.Execute(@"
                    INSERT INTO Users (EmailAddress, SlackAuthToken, DefaultStatusText, DefaultStatusEmoji)
                    VALUES (@EmailAddress, @SlackAuthToken, @DefaultStatusText, @DefaultStatusEmoji)", user);
                return;
            }

            // On update, don't update the status text/emoji, assume they've already set it to something they want
            // If this is called for an existing user, it probably means they're re-authing the Slack integration,
            // so we're only updating their SlackAuthToken.
            _connection.Execute(@"
                UPDATE Users SET
                    SlackAuthToken=@SlackAuthToken
                WHERE EmailAddress=@EmailAddress", user);
        }

        public List<SqlUser> GetUsers()
        {
            return _connection.Query<SqlUser>("SELECT * FROM Users").ToList();
        }

        public SqlUser GetUserByEmail(string email)
        {
            return _connection.Query<SqlUser>("SELECT * FROM Users WHERE EmailAddress = @Email", new { Email = email }).FirstOrDefault();
        }

        public void SetStatus(SqlStatus status)
        {
            RemoveStatus(status);
            _connection.Execute(@"
                INSERT INTO Statuses (EmailAddress, Match, Text, Emoji)
                VALUES (@EmailAddress, @Match, @Text, @Emoji)", status);
        }

        public void RemoveStatus(SqlStatus status)
        {
            _connection.Execute(@"DELETE FROM Statuses WHERE EmailAddress=@EmailAddress AND Match=@Match", status);
        }

        public void Dispose()
        {
            _connection.Dispose(); 
        }
    }
}
