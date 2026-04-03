using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Artemis.Shared.Models;

const int port = 5005;

Console.WriteLine("Judge is started");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {e.Cancel = true; cts.Cancel();};

using var listener = new UdpClient(port);

long count = 0;
long sum = 0;
int min = int.MaxValue;
int max = int.MinValue;

var lastSequences = new Dictionary<int, long>();
var currentVotes = new Dictionary<int, double>();

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

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // DYNAMICZNA REJESTRACJA: Jeśli to pierwszy raz widzimy to ID, dodaj je do bazy
        if (!lastSequences.ContainsKey(measurement.ActorId))
        {
            Console.WriteLine($"[NOWY AKTOR] Wykryto jednostkę ID: {measurement.ActorId}. Rozpoczynam monitorowanie.");
            lastSequences.Add(measurement.ActorId, 0);
        }

        // 1. Sprawdź czy pakiet nie jest "przestarzały" (np. > 200ms)
        if (now - measurement.Timestamp > 200) {
            Console.WriteLine($"[DROP] Aktor {measurement.ActorId}: Pakiet zbyt stary ({now - measurement.Timestamp}ms)");
            continue;
        }

        // 2. Sprawdź czy pakiet nie przyszedł w złej kolejności
        if (measurement.Sequence <= lastSequences[measurement.ActorId]) {
            Console.WriteLine($"[DROP] Aktor {measurement.ActorId}: Odrzucono stary numer sekwencji {measurement.Value}");
            continue;
        }

        lastSequences[measurement.ActorId] = measurement.Sequence;
        currentVotes[measurement.ActorId] = measurement.Value;
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

        Console.WriteLine($"Parsed Measurement: Id={measurement.Id}, Date={measurement.Timestamp}, Value={measurement.Value}");
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

