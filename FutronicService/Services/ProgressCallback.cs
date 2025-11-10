using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FutronicService.Services
{
    public interface IProgressCallback
    {
  Task SendProgressAsync(string eventType, string message, object data = null);
    }

    public class HttpProgressCallback : IProgressCallback
    {
        private readonly string _callbackUrl;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;

        public HttpProgressCallback(string callbackUrl, ILogger logger)
     {
            _callbackUrl = callbackUrl;
         _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
         _logger = logger;
        }

   public async Task SendProgressAsync(string eventType, string message, object data = null)
        {
  if (string.IsNullOrEmpty(_callbackUrl))
        return;

     try
       {
       var payload = new
       {
        eventType = eventType,
 message = message,
   data = data,
     timestamp = DateTime.Now.ToString("o")
          };

            var json = JsonConvert.SerializeObject(payload);
       var content = new StringContent(json, Encoding.UTF8, "application/json");

  var response = await _httpClient.PostAsync(_callbackUrl, content);
          
     if (!response.IsSuccessStatusCode)
         {
  _logger.LogWarning($"Callback failed: {response.StatusCode}");
      }
            }
     catch (Exception ex)
            {
      _logger.LogWarning(ex, $"Error sending progress callback to {_callbackUrl}");
       }
    }
    }

    public class ConsoleProgressCallback : IProgressCallback
    {
        public Task SendProgressAsync(string eventType, string message, object data = null)
        {
            Console.WriteLine($"[{eventType}] {message}");
    return Task.CompletedTask;
        }
    }
}
