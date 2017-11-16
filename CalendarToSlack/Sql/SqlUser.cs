namespace CalendarToSlack.Sql
{
    public class SqlUser
    {
        public string EmailAddress { get; set; }
        public string SlackAuthToken { get; set; }
        public string DefaultStatusText { get; set; }
        public string DefaultStatusEmoji { get; set; }
    }
}
