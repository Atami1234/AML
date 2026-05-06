using System.Security.Cryptography;
using System.Text;
using AmlDetectionApi.Models;

namespace AmlDetectionApi.Helpers
{
    public static class HashHelper
    {
        public static string ComputeSha256Hash(string input)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public static string BuildAlertInputString(Alert alert)
        {
            return $"{alert.AlertId}|{alert.AccountId}|{alert.RiskScore}|{alert.RiskLevel}|{alert.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }

        public static string BuildTransactionInputString(Transaction tx)
        {
            return $"{tx.TransactionId}|{tx.FromAccountId}|{tx.ToAccountId}|{tx.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}|{tx.Currency}|{tx.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss")}|{tx.Channel}|{tx.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }
    }
}
