using System.Text.Json.Serialization;

namespace UKHO.ADDS.EFS.Domain.Services.Models
{
    /// <summary>
    /// CloudEvents 1.0 specification compliant notification model
    /// </summary>
    public class CloudEventNotification
    {
        [JsonPropertyName("specversion")]
        public string SpecVersion { get; set; } = "1.0";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "uk.co.admiralty.avcsData.exchangeSetCreated.v1";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "https://exchangeset.admiralty.co.uk/avcsData";

        [JsonPropertyName("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonPropertyName("time")]
        public string Time { get; set; } = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "Requested AVCS Exchange Set Created";

        [JsonPropertyName("datacontenttype")]
        public string DataContentType { get; set; } = "application/json";

        [JsonPropertyName("data")]
        public object Data { get; set; } = new();
    }

    /// <summary>
    /// Exchange Set response data model for callback notifications
    /// </summary>
    public class CallbackExchangeSetData
    {
        [JsonPropertyName("_links")]
        public CallbackLinks Links { get; set; } = new();

        [JsonPropertyName("exchangeSetUrlExpiryDateTime")]
        public string ExchangeSetUrlExpiryDateTime { get; set; } = string.Empty;

        [JsonPropertyName("requestedProductCount")]
        public int RequestedProductCount { get; set; }

        [JsonPropertyName("exchangeSetCellCount")]
        public int ExchangeSetCellCount { get; set; }

        [JsonPropertyName("requestedProductsAlreadyUpToDateCount")]
        public int RequestedProductsAlreadyUpToDateCount { get; set; }

        [JsonPropertyName("requestedAioProductCount")]
        public int RequestedAioProductCount { get; set; }

        [JsonPropertyName("aioExchangeSetCellCount")]
        public int AioExchangeSetCellCount { get; set; }

        [JsonPropertyName("requestedAioProductsAlreadyUpToDateCount")]
        public int RequestedAioProductsAlreadyUpToDateCount { get; set; }

        [JsonPropertyName("requestedProductsNotInExchangeSet")]
        public List<CallbackMissingProduct> RequestedProductsNotInExchangeSet { get; set; } = new();

        [JsonPropertyName("fssBatchId")]
        public string FssBatchId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Links object for callback exchange set data
    /// </summary>
    public class CallbackLinks
    {
        [JsonPropertyName("exchangeSetBatchStatusUri")]
        public CallbackLink ExchangeSetBatchStatusUri { get; set; } = new();

        [JsonPropertyName("exchangeSetBatchDetailsUri")]
        public CallbackLink ExchangeSetBatchDetailsUri { get; set; } = new();

        [JsonPropertyName("exchangeSetFileUri")]
        public CallbackLink ExchangeSetFileUri { get; set; } = new();

        [JsonPropertyName("aioExchangeSetFileUri")]
        public CallbackLink AioExchangeSetFileUri { get; set; } = new();

        [JsonPropertyName("errorFileUri")]
        public CallbackLink ErrorFileUri { get; set; } = new();
    }

    /// <summary>
    /// Link object for callback notifications
    /// </summary>
    public class CallbackLink
    {
        [JsonPropertyName("href")]
        public string Href { get; set; } = string.Empty;
    }

    /// <summary>
    /// Missing product information for callback notifications
    /// </summary>
    public class CallbackMissingProduct
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
