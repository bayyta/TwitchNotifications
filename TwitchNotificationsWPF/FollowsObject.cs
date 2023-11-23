namespace TwitchNotificationsWPF
{
    class FollowsObject
    {
        public System.Collections.Generic.List<User> follows { get; set; }
        public class User
        {
            public Channel channel { get; set; }
            public class Channel
            {
                public string display_name { get; set; }
                public string name { get; set; }
                public string url { get; set; }
            }
        }
    }
}