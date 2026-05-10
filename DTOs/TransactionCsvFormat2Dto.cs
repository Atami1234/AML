using CsvHelper.Configuration.Attributes;
using System;

namespace AmlDetectionApi.DTOs
{
    /// <summary>
    /// Maps to the HI-Small / IBM synthetic AML dataset CSV format.
    /// Headers: Timestamp, From Bank, Account, To Bank, Account.1,
    ///          Amount Received, Receiving Currency, Amount Paid, Payment Currency, Payment Format
    /// </summary>
    public class TransactionCsvFormat2Dto
    {
        [Name("Timestamp")]
        public string Timestamp { get; set; } = string.Empty;

        [Name("From Bank")]
        public string FromBank { get; set; } = string.Empty;

        [Name("Account")]
        public string Account { get; set; } = string.Empty;

        [Name("To Bank")]
        public string ToBank { get; set; } = string.Empty;

        [Name("Account.1")]
        public string Account1 { get; set; } = string.Empty;

        [Name("Amount Received")]
        public decimal AmountReceived { get; set; }

        [Name("Receiving Currency")]
        public string ReceivingCurrency { get; set; } = string.Empty;

        [Name("Amount Paid")]
        public decimal AmountPaid { get; set; }

        [Name("Payment Currency")]
        public string PaymentCurrency { get; set; } = string.Empty;

        [Name("Payment Format")]
        public string PaymentFormat { get; set; } = string.Empty;
    }
}
