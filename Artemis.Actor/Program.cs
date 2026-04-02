using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Artemis.Shared.Models;

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

        Measurement measurement = new Measurement(number);

        var jsonSerializerOptionsons = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        string jsonMeasurement = JsonSerializer.Serialize(measurement, jsonSerializerOptionsons);
        var dataMeasurement = Encoding.UTF8.GetBytes(jsonMeasurement);

        await client.SendAsync(dataMeasurement, dataMeasurement.Length, endpoint);

        Console.WriteLine($"Sent JSON: {jsonMeasurement}");

        await Task.Delay(intervalMs, cts.Token);
    }
}
catch (OperationCanceledException) { /* normal exit */ }
