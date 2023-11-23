namespace TwitchNotificationsWPF
{
    class StreamObject
    {
        public Stream stream { get; set; }
        public class Stream
        {
            public Channel channel { get; set; }
            public class Channel
            {
                public string logo { get; set; }
            }
        }
    }
}
