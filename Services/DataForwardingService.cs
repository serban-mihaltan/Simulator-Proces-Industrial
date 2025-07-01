using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MonitorApi.Models;

namespace MonitorApi.Services
{
    public class DataForwardingService
    {
        private readonly HttpClient _http;
        private readonly string _url;
        public DataForwardingService(IHttpClientFactory clientFactory, IOptions<ForwardingOptions> opts)
        {
            _http = clientFactory.CreateClient();
            _url = opts.Value.Url;
        }

        public async Task ForwardAsync(TransitionDto dto)
        {
            var json = JsonSerializer.Serialize(dto);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(_url, content);
            response.EnsureSuccessStatusCode();
        }
    }
}