using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MinimalJT808Server
{
    public class JT808Server : BackgroundService
    {
        private readonly JT808SessionManager _sessionManager;
        private readonly JT1078TriggerService _trigger;
        private readonly ILogger<JT808Server> _logger;
        private TcpListener? _listener;

        public JT808Server(JT808SessionManager sessionManager, JT1078TriggerService trigger, ILogger<JT808Server> logger)
        {
            _sessionManager = sessionManager;
            _trigger = trigger;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _listener = new TcpListener(IPAddress.Any, 18090);
            _listener.Start();
            _logger.LogInformation("JT808 Server started on port 18090");

            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(stoppingToken);
                _ = Task.Run(() => HandleClientAsync(client));
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            string message = Encoding.ASCII.GetString(buffer, 0, bytes);

            // You can replace this with actual JT808 message parsing
            var phone = "9667163125";
            _sessionManager.Register(phone, client);

            _logger.LogInformation($"Device {phone} connected, sending 0x9101...");
            await _trigger.SendVideoStart(phone);
        }
    }
}
