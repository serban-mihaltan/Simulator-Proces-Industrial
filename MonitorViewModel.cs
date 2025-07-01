using System;
using System.ComponentModel;
using Communicator;
using DataModel;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Monitor
{
    public class MonitorViewModel : INotifyPropertyChanged, IDisposable
    {
        public ObservableCollection<LogEntry> Logs { get; } = new ObservableCollection<LogEntry>();
        private const string ApiEndpoint = "https://localhost:59578/api/silo";
        

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private string currentState;
        public string CurrentState
        {
            get => currentState;
            private set
            {
                if (currentState != value)
                {
                    currentState = value;
                    OnPropertyChanged(nameof(CurrentState));
                }
            }
        }

        private readonly Receiver receiver;
        private readonly HttpClient httpClient;
        private readonly AsyncRetryPolicy<HttpResponseMessage> retryPolicy;
        private string lastSentState;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();

        public MonitorViewModel(HttpClient httpClient = null)
        {
            // Inițializare HttpClient cu timeout
            this.httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

            // Configurare retry exponențială pentru erori tranzitorii
            retryPolicy = Policy.Handle<HttpRequestException>()
                                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                                .WaitAndRetryAsync(
                                    retryCount: 3,
                                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                                    onRetryAsync: async (outcome, timespan, retryCount, context) =>
                                    {
                                        // Log retry intern (nu-l adăugăm în Logs)
                                        Console.WriteLine($"[Retry] Încercarea {retryCount} după {timespan.TotalSeconds}s");
                                    });

            CurrentState = "Așteptare...";
            receiver = new Receiver("127.0.0.1", 3000);
            receiver.DataReceived += OnDataReceived;
            receiver.Start();
        }

        private void OnDataReceived(byte data)
            => Application.Current.Dispatcher.Invoke(async () =>
            {
                if (!Enum.IsDefined(typeof(ProcessState), (int)data))
                    return;

                var state = (ProcessState)data;
                var stateString = state.ToString();

                // Actualizăm starea curentă și logăm doar tranziția
                CurrentState = stateString;
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    State = stateString
                };
                Logs.Add(logEntry);

                // Trimitem doar când starea s-a schimbat
                await SendToWebApiAsync(logEntry, cts.Token).ConfigureAwait(false);
            });

        private async Task SendToWebApiAsync(LogEntry log, CancellationToken token)
        {
            if (log.State == lastSentState)
                return;

            lastSentState = log.State;

            var payload = new
            {
                state = log.State,
                timestamp = log.Timestamp.ToString("o")
            };
            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                // Executăm request-ul cu retry
                var response = await retryPolicy.ExecuteAsync(
                    ct => httpClient.PostAsync(ApiEndpoint, content, ct), token
                    ).ConfigureAwait(false);


                response.EnsureSuccessStatusCode();
            }
            catch (TaskCanceledException) when (!token.IsCancellationRequested)
            {
                // Timeout intern, nu în Logs UI
                Console.WriteLine("[Error] WebAPI request timed out");
            }
            catch (Exception ex)
            {
                // Eroare internă, doar în consolă
                Console.WriteLine($"[Error] Eroare la trimitere WebAPI: {ex.Message}");
            }

        }

        public void Dispose()
        {
            cts.Cancel();
            receiver?.Stop();
            httpClient?.Dispose();
            cts.Dispose();
        }
    }
}
