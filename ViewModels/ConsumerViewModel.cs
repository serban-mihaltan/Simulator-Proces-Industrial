using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.AspNetCore.SignalR.Client;

namespace MonitorConsumer
{
    public class ConsumerViewModel : INotifyPropertyChanged, IDisposable
    {
        public ObservableCollection<LogRecord> Logs { get; } = new ObservableCollection<LogRecord>();
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly HttpClient httpClient;
        private readonly HubConnection hubConnection;
        private readonly DispatcherTimer refreshTimer;
        private const string ApiBase = "https://localhost:59578";
        private bool isDisposed;

        public ConsumerViewModel()
        {
            httpClient = new HttpClient();

            // Initial load
            _ = RefreshLogsAsync();

            // SignalR for real-time updates
            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{ApiBase}/hubs/logs")
                .WithAutomaticReconnect()
                .Build();

            hubConnection.On<LogRecord>("ReceiveLog", dto =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Insert(0, new LogRecord { State = dto.State, Timestamp = dto.Timestamp });
                });
            });
            _ = StartSignalRAsync();

            // Polling timer
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            refreshTimer.Tick += async (s, e) => await RefreshLogsAsync();
            refreshTimer.Start();
        }

        private async Task RefreshLogsAsync()
        {
            try
            {
                var response = await httpClient.GetAsync($"{ApiBase}/api/silo");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var list = JsonSerializer.Deserialize<List<LogRecord>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Clear();
                    foreach (var record in list)
                        Logs.Add(record);
                });
            }
            catch (Exception ex)
            {
                // Log or handle error as needed
                Console.WriteLine($"[Error] RefreshLogsAsync: {ex.Message}");
            }
        }

        private async Task StartSignalRAsync()
        {
            try
            {
                await hubConnection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] SignalR connection: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;

            refreshTimer.Stop();
            hubConnection?.StopAsync();
            hubConnection?.DisposeAsync();
            httpClient?.Dispose();
        }
    }
}
