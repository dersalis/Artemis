using System.Collections.Concurrent;
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

var lastSequences = new ConcurrentDictionary<int, long>();
var lastSeen = new ConcurrentDictionary<int, DateTime>();
var currentVotes = new ConcurrentDictionary<int, double>();

try
{
    // WĄTEK MONITORUJĄCY (WATCHDOG)
    _ = Task.Run(async () => {
        while (true) {
            var now = DateTime.Now;
            foreach (var actor in lastSeen) {
                if ((now - actor.Value).TotalMilliseconds > 1000) { // Timeout 1s
                    if (lastSeen.TryRemove(actor.Key, out _)) {
                        lastSequences.TryRemove(actor.Key, out _);
                        currentVotes.TryRemove(actor.Key, out _);
                        Console.WriteLine($"\n[!!!] ALARM: Aktor {actor.Key} utracony (Timeout). System rekonfigurowany.");
                    }
                }
            }
            await Task.Delay(500); // Sprawdzaj co pół sekundy
        }
    });


    // GŁÓWNA PĘTLA ODBIERAJĄCA DANE
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

            // Aktualizacja czasu widoczności aktora
            lastSeen[measurement.ActorId] = DateTime.Now;

            // DYNAMICZNA REJESTRACJA: Jeśli to pierwszy raz widzimy to ID, dodaj je do bazy
            if (!lastSequences.ContainsKey(measurement.ActorId))
            {
                Console.WriteLine($"[NOWY AKTOR] Wykryto jednostkę ID: {measurement.ActorId}. Rozpoczynam monitorowanie.");
                lastSequences[measurement.ActorId] = 0;
            }

            // Sprawdź czy pakiet nie jest "przestarzały" (np. > 200ms)
            if (now - measurement.Timestamp > 200) {
                Console.WriteLine($"[DROP] Aktor {measurement.ActorId}: Pakiet zbyt stary ({now - measurement.Timestamp}ms)");
                continue;
            }

            // Sprawdź czy pakiet nie przyszedł w złej kolejności
            if (measurement.Sequence <= lastSequences[measurement.ActorId]) {
                Console.WriteLine($"[DROP] Aktor {measurement.ActorId}: Odrzucono stary numer sekwencji {measurement.Value}");
                continue;
            }

            lastSequences[measurement.ActorId] = measurement.Sequence;
            currentVotes[measurement.ActorId] = measurement.Value;

            // Logika głosowania (czekamy na głosy od wszystkich AKTYWNYCH aktorów)
            // if (currentVotes.Count >= lastSeen.Count && lastSeen.Count > 0)
            // {
            //     var v = currentVotes.Values.ToArray();
                
            //     if (v.Length >= 3) {
            //         // Głosowanie większościowe 2 z 3 (uproszczone)
            //         double finalDecision = 0;
            //         bool consensus = false;
                    
            //         if (Math.Abs(v[0] - v[1]) < 0.5) { finalDecision = v[0]; consensus = true; }
            //         else if (Math.Abs(v[0] - v[2]) < 0.5) { finalDecision = v[0]; consensus = true; }
            //         else if (Math.Abs(v[1] - v[2]) < 0.5) { finalDecision = v[1]; consensus = true; }

            //         if (consensus) Console.Write($"\r[OK] Decyzja: {finalDecision:F2} (Aktywnych: {lastSeen.Count})    ");
            //         else Console.Write("\r[ERR] Brak konsensusu między 3 jednostkami!          ");
            //     } 
            //     else if (v.Length == 2) {
            //         // Tryb awaryjny: dwóch aktorów
            //         if (Math.Abs(v[0] - v[1]) < 0.5) Console.Write($"\r[WARN] Tryb 2-jednostkowy. Decyzja: {v[0]:F2}    ");
            //         else Console.Write("\r[CRITICAL] Rozbieżność w trybie 2-jednostkowym!      ");
            //     }
            //     else {
            //         Console.Write($"\r[SINGLE] Ostatnia szansa (ID:{measurement.ActorId}): {measurement.Value:F2}               ");
            //     }

            //     currentVotes.Clear();
            // }
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

