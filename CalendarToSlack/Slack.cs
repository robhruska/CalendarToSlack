using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Web.Helpers;
using log4net;
using Newtonsoft.Json;

namespace CalendarToSlack
{
    class Slack
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (Slack).Name);

        private readonly HttpClient _http;
        private readonly string _slackbotPostIconurl;

        public Slack(string slackbotPostIconUrl = null)
        {
            _slackbotPostIconurl = slackbotPostIconUrl;
            _http = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5),
            };
        }

        //public Presence GetPresence(string authToken)
        //{
        //    var result = _http.GetAsync(string.Format("https://slack.com/api/users.getPresence?token={0}", authToken)).Result;
        //    LogSlackApiResult("users.getPresence", result);

        //    if (!result.IsSuccessStatusCode)
        //    {
        //        Log.ErrorFormat("Unsuccessful response status for users.getPresence: {0}", result.StatusCode);
        //        return;
        //    }

        //    var content = result.Content.ReadAsStringAsync().Result;
        //    var data = Json.Decode(content);
        //    return (string.Equals(data.presence, "away", StringComparison.OrdinalIgnoreCase) ? Presence.Away : Presence.Auto);
        //}

        public void SetPresence(string authToken, Presence presence)
        {
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "token", authToken },
                { "presence", (presence == Presence.Auto ? "auto" : "away") }
            });
            var result = _http.PostAsync("https://slack.com/api/users.setPresence", content).Result;
            LogSlackApiResult("users.setPresence", result);
            
            if (!result.IsSuccessStatusCode)
            {
                Log.ErrorFormat("Unsuccessful response status for users.setPresence: {0}", result.StatusCode);
            }

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);
        }


        // When we add a new user, we only have their auth token, and need to get their email address
        // to associate it with an exchange account. This method is mainly for that.
        public SlackUserInfo GetUserInfo(string authToken)
        {
            var result = _http.GetAsync(string.Format("https://slack.com/api/auth.test?token={0}", authToken)).Result;
            LogSlackApiResult("auth.test", result);
            result.EnsureSuccessStatusCode();
            
            var content = result.Content.ReadAsStringAsync().Result;

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);

            var data = Json.Decode(content);
            var info = GetUserInfo(authToken, data.user_id);

            return info;
        }

        public SlackUserInfo GetUserInfo(string authToken, string userId)
        {
            var result = _http.GetAsync(string.Format("https://slack.com/api/users.info?token={0}&user={1}", authToken, userId)).Result;
            LogSlackApiResult("users.info " + userId, result);
            result.EnsureSuccessStatusCode();
            
            var content = result.Content.ReadAsStringAsync().Result;

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);

            var data = Json.Decode(content);
            return new SlackUserInfo
            {
                FirstName = data.user.profile.first_name,
                LastName = data.user.profile.last_name,
                Username = data.user.name,
                Email = data.user.profile.email,
                UserId = data.user.id,
            };
        }

        public void PostSlackbotMessage(string authToken, string username, string message)
        {
            Log.InfoFormat("Posting message to @{0}'s slackbot: {1}", username, message);

            var options = new Dictionary<string, string>
            {
                { "token", authToken },
                { "channel", "@" + username },
                { "as_user", "false" },
                { "text", message },
                { "username", "Calendar To Slack" },
            };

            if (!string.IsNullOrWhiteSpace(_slackbotPostIconurl))
            {
                options["icon_url"] = _slackbotPostIconurl;
            }

            var content = new FormUrlEncodedContent(options);

            var result = _http.PostAsync("https://slack.com/api/chat.postMessage", content).Result;
            LogSlackApiResult("chat.postMessage " + username, result);

            if (!result.IsSuccessStatusCode)
            {
                Log.ErrorFormat("Unsuccessful response status for chat.postMessage: {0}", result.StatusCode);
            }

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);
        }

        public void UpdateProfileWithStatus(RegisteredUser user, string message, string emoji)
        {
            // Slack's support for status/presence (i.e. only auto/away) is limited, and one of
            // our conventions for broadcasting more precise status is to change our last name
            // to something like "Rob Hruska | Busy" or "Rob Hruska | OOO til Mon".

            // The users.profile.set API endpoint (which isn't public, but is used by the webapp
            // version of Slack) requires the `post` scope, but applications can't request/authorize
            // that scope because it's deprecated.
            // 
            // The "full access" token (from the Web API test page) does support post, but I don't
            // want to manage those within the app here. I've temporarily allowed it for myself,
            // but it'll be removed in the future.
            //
            // The current plan is to wait for Slack to either 1) expose a formal users.profile.set
            // API, or 2) introduce custom away status messages.

            if (string.IsNullOrWhiteSpace(user.HackyPersonalFullAccessSlackToken))
            {
                // Can't update without the full token.
                return;
            }

            var profile = $"{{\"status_text\":\"{message}\",\"status_emoji\":\"{emoji}\"}}";

            Log.Info($"Changed profile status text to {message} and emoji to {emoji}");
            
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "profile", profile },
                { "token", user.HackyPersonalFullAccessSlackToken } // TODO switch to auth token. see comments above in this method
            });
            
            var result = _http.PostAsync("https://slack.com/api/users.profile.set", content).Result;
            LogSlackApiResult("users.profile.set " + user.SlackUserInfo.Username, result);

            if (!result.IsSuccessStatusCode)
            {
                Log.ErrorFormat("Unsuccessful response status for users.profile.set: {0}", result.StatusCode);
            }

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);
        }

        public List<SlackUserInfo> ListUsers(string authToken)
        {
            var result = _http.GetAsync(string.Format("https://slack.com/api/users.list?token={0}&presence=1", authToken)).Result;
            LogSlackApiResult("users.list", result, false);
            result.EnsureSuccessStatusCode();

            var content = result.Content.ReadAsStringAsync().Result;

            // TODO temporary hack to avoid Slack's rate limit. a longer-term solution is being investigated.
            Thread.Sleep(1500);

            var results = new List<SlackUserInfo>();
            
            var data = (dynamic)JsonConvert.DeserializeObject(content);
            var members = data.members;
            foreach (var member in members)
            {
                // startup presence = member.presence
                // 
                // This assumes that the custom status of the user at startup is their desired default, 
                // but if the app starts when the user has a meeting or OOO-related status set, that will be
                // used as the default. TODO: add manual default status setting: https://github.com/robhruska/CalendarToSlack/issues/17
                results.Add(new SlackUserInfo
                {
                    UserId = member.id,
                    Username = member.name,
                    FirstName = member.profile.first_name,
                    LastName = member.profile.last_name,
                    Email = member.profile.email,
                    DefaultStatusText = member.profile.status_text,
                    DefaultStatusEmoji = member.profile.status_emoji,
                });
            }
            return results;
        }

        private static void LogSlackApiResult(string action, HttpResponseMessage response, bool logContent = true)
        {
            try
            {
                Log.DebugFormat("Slack API result ({0}): Status={1} Content={2}", action, response.StatusCode, (logContent ? response.Content.ReadAsStringAsync().Result : "<omitted>"));
            }
            catch (Exception e)
            {
                Log.ErrorFormat("Error logging Slack API result: " + e.Message);
            }
        }
    }

    class SlackUserInfo
    {
        public string UserId { get; set; }
        public string Username { get; set; }
        
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }

        public string DefaultStatusText { get; set; }
        public string DefaultStatusEmoji { get; set; }

        public string ActualLastName { get { return LastName.Split('|')[0].Trim(); } }
    }

    enum Presence
    {
        Away,
        Auto,
    }
}