using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

const string ip = "127.0.0.1";
const int port = 5000;
const int intervalMs = 5000;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

using var client = new UdpClient();
var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
Console.WriteLine($"Actor: sending random numbers every {intervalMs}ms to {endpoint}");

try
{
    while (!cts.IsCancellationRequested)
    {
        int number = Random.Shared.Next(0, int.MaxValue);
        string message = number.ToString(CultureInfo.InvariantCulture);
        var data = Encoding.UTF8.GetBytes(message);
        await client.SendAsync(data, data.Length, endpoint);
        Console.WriteLine($"Sent: {message}");
        await Task.Delay(intervalMs, cts.Token);
    }
}
catch (OperationCanceledException) { /* normal exit */ }
