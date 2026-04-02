using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Artemis.Shared.Models;

const int port = 5000;

Console.WriteLine("Judge is started");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {e.Cancel = true; cts.Cancel();};

using var listener = new UdpClient(port);

long count = 0;
long sum = 0;
int min = int.MaxValue;
int max = int.MinValue;

try
{
    while (!cts.IsCancellationRequested)
{
	var result = await listener.ReceiveAsync(cts.Token);

    if (result.Buffer.Length == 0)
    {
        Console.WriteLine("Received empty message, ignoring.");
        continue;
    }

        string received = Encoding.UTF8.GetString(result.Buffer);
        Console.WriteLine($"Received from {result.RemoteEndPoint}: {received}");

        int number;
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        Measurement? measurement = null;
        try
        {
            measurement = JsonSerializer.Deserialize<Measurement>(received, jsonOptions);
        }
        catch (JsonException)
        {
            // not JSON, will try plain int parse below
        }

        if (measurement is not null)
        {
            number = measurement.Value;
            count++;
            sum += number;
            min = Math.Min(min, number);
            max = Math.Max(max, number);
            Console.WriteLine($"Parsed Measurement: Id={measurement.Id}, Date={measurement.Date:o}, Value={measurement.Value}");
        }
        else if (int.TryParse(received, out number))
        {
            count++;
            sum += number;
            min = Math.Min(min, number);
            max = Math.Max(max, number);
        }
        else
        {
            Console.WriteLine($"Received invalid payload: {received}");
        }
}
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutting down...");
}

Console.WriteLine($"Stats: Count={count}, Sum={sum}, Min={min}, Max={max}");

