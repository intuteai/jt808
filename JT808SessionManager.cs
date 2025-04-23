using System.Net.Sockets;
using System.Collections.Concurrent;

namespace MinimalJT808Server
{
    public class JT808SessionManager
    {
        public ConcurrentDictionary<string, TcpClient> Sessions { get; } = new();

        public void Register(string phone, TcpClient client)
        {
            Sessions[phone] = client;
        }

        public bool TryGet(string phone, out TcpClient client)
        {
            return Sessions.TryGetValue(phone, out client);
        }
    }
}
