using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AmlDetectionApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AmlDetectionApi.Services
{
    public interface IAiDetectionService
    {
        Task<bool?> PredictAsync(Transaction transaction);
    }

    public class AiPredictionRequest
    {
        [JsonPropertyName("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [JsonPropertyName("From Bank")]
        public int FromBank { get; set; }

        [JsonPropertyName("Account")]
        public string Account { get; set; } = string.Empty;

        [JsonPropertyName("To Bank")]
        public int ToBank { get; set; }

        [JsonPropertyName("Account.1")]
        public string Account1 { get; set; } = string.Empty;

        [JsonPropertyName("Amount Received")]
        public decimal AmountReceived { get; set; }

        [JsonPropertyName("Receiving Currency")]
        public string ReceivingCurrency { get; set; } = string.Empty;

        [JsonPropertyName("Amount Paid")]
        public decimal AmountPaid { get; set; }

        [JsonPropertyName("Payment Currency")]
        public string PaymentCurrency { get; set; } = string.Empty;

        [JsonPropertyName("Payment Format")]
        public string PaymentFormat { get; set; } = string.Empty;
    }

    public class AiPredictionResponse
    {
        [JsonPropertyName("prediction")]
        public int Prediction { get; set; }
    }

    public class AiDetectionService : IAiDetectionService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AiDetectionService> _logger;

        public AiDetectionService(HttpClient httpClient, IConfiguration configuration, ILogger<AiDetectionService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            
            var baseUrl = configuration["AiModel:BaseUrl"] ?? "http://localhost:8000";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<bool?> PredictAsync(Transaction transaction)
        {
            try
            {
                var requestData = new AiPredictionRequest
                {
                    Timestamp = transaction.TransactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    FromBank = transaction.FromAccountId,
                    Account = transaction.FromAccount?.AccountNumber ?? string.Empty,
                    ToBank = transaction.ToAccountId,
                    Account1 = transaction.ToAccount?.AccountNumber ?? string.Empty,
                    AmountReceived = transaction.Amount,
                    ReceivingCurrency = transaction.Currency,
                    AmountPaid = transaction.Amount,
                    PaymentCurrency = transaction.Currency,
                    PaymentFormat = transaction.Channel
                };

                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/predict", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<AiPredictionResponse>(responseString);
                    return result?.Prediction == 1;
                }
                else
                {
                    _logger.LogWarning($"ML API returned status code {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ML API");
                return null;
            }
        }
    }
}
