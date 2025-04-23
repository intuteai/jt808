using System;
using System.Text;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MinimalJT808Server
{
    public class JT1078TriggerService
    {
        private readonly JT808SessionManager _sessionManager;
        private readonly ILogger<JT1078TriggerService> _logger;

        public JT1078TriggerService(JT808SessionManager sessionManager, ILogger<JT1078TriggerService> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
        }

        public async Task SendVideoStart(string phone)
        {
            if (_sessionManager.TryGet(phone, out var client))
            {
                var stream = client.GetStream();
                var cmd = Build9101Message(phone);
                await stream.WriteAsync(cmd, 0, cmd.Length);
                _logger.LogInformation($"Sent 0x9101 to {phone}");
            }
        }

        private byte[] BCDEncode(string number)
{
    if (number.Length % 2 != 0)
        number = "0" + number;

    byte[] result = new byte[number.Length / 2];
    for (int i = 0; i < number.Length; i += 2)
    {
        string byteStr = number.Substring(i, 2);
        result[i / 2] = Convert.ToByte(byteStr, 16);
    }
    return result;
}

        private byte[] Build9101Message(string phone)
{
    byte msgIdHigh = 0x91;
    byte msgIdLow = 0x01;

    byte[] terminalPhoneBCD = BCDEncode(phone); // BCD encode the phone number
    byte[] serverIpBytes = Encoding.ASCII.GetBytes("148.66.155.196");
    byte ipLen = (byte)serverIpBytes.Length;

    byte[] body = new byte[15 + ipLen];

    body[0] = 0x01; // Channel ID
    body[1] = 0x00; // AVType: 0 = audio+video
    body[2] = 0x01; // StreamType: 1 = main
    body[3] = ipLen;

    Array.Copy(serverIpBytes, 0, body, 4, ipLen);
    body[4 + ipLen] = 0x46; // TCP Port High (18091 = 0x46B3)
    body[5 + ipLen] = 0xB3; // TCP Port Low
    body[6 + ipLen] = 0x46; // UDP Port High
    body[7 + ipLen] = 0xB3; // UDP Port Low
    body[8 + ipLen] = 0x01; // Logical Channel No

    int bodyLen = 9 + ipLen;

    // Message Properties (body length, encrypted flag etc.)
    ushort props = (ushort)(bodyLen & 0x03FF); // no encryption, subpackage, etc.

    byte[] header = new byte[12];
    header[0] = msgIdHigh;
    header[1] = msgIdLow;
    header[2] = (byte)(props >> 8);
    header[3] = (byte)(props & 0xFF);
    Array.Copy(terminalPhoneBCD, 0, header, 4, 6);
    header[10] = 0x00; // flow ID high
    header[11] = 0x01; // flow ID low

    byte[] full = new byte[1 + header.Length + bodyLen + 1];
    full[0] = 0x7E; // Start frame
    Array.Copy(header, 0, full, 1, header.Length);
    Array.Copy(body, 0, full, 1 + header.Length, bodyLen);

    // Checksum: XOR of everything except 0x7E
    byte cs = 0;
    for (int i = 1; i < full.Length - 1; i++)
        cs ^= full[i];
    full[full.Length - 1] = cs;

    return full;
}
}
}
