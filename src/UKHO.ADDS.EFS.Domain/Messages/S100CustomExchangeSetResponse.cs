namespace UKHO.ADDS.EFS.Messages;

/// <summary>
/// Response model for S100 exchange set endpoints
/// </summary>
public class S100CustomExchangeSetResponse
{
    /// <summary>
    /// Version of the response
    /// </summary>
    public int Version { get; init; } = 2;

    /// <summary>
    /// Links to related resources
    /// </summary>
    public required S100ExchangeSetLinks Links { get; set; }

    /// <summary>
    /// The expiry date and time for the exchange set URL
    /// </summary>
    public required DateTime ExchangeSetUrlExpiryDateTime { get; set; }

    /// <summary>
    /// Number of products explicitly requested
    /// </summary>
    public int RequestedProductCount { get; set; }

    /// <summary>
    /// Number of products that have data included in the produced Exchange Set
    /// </summary>
    public int ExchangeSetProductCount { get; set; }

    /// <summary>
    /// Number of requested products that are already up to date
    /// </summary>
    public int RequestedProductsAlreadyUpToDateCount { get; set; }

    /// <summary>
    /// Products that were requested but not included in the exchange set
    /// </summary>
    public List<S100ProductNotInExchangeSet> RequestedProductsNotInExchangeSet { get; set; } = new();

    /// <summary>
    /// The FSS Batch ID associated with the exchange set
    /// </summary>
    public string? FssBatchId { get; set; }
}

/// <summary>
/// Links to related resources for S100 exchange set
/// </summary>
public class S100ExchangeSetLinks
{
    /// <summary>
    /// Link to exchange set batch status
    /// </summary>
    public required S100Link ExchangeSetBatchStatusUri { get; set; }

    /// <summary>
    /// Link to exchange set batch details
    /// </summary>
    public required S100Link ExchangeSetBatchDetailsUri { get; set; }

    /// <summary>
    /// Link to exchange set file (optional)
    /// </summary>
    public S100Link? ExchangeSetFileUri { get; set; }
}

/// <summary>
/// Represents a link with href
/// </summary>
public class S100Link
{
    /// <summary>
    /// The URL reference
    /// </summary>
    public required string Href { get; set; }
}

/// <summary>
/// Represents a product that was requested but not included in the exchange set
/// </summary>
public class S100ProductNotInExchangeSet
{
    /// <summary>
    /// The product name
    /// </summary>
    public required string ProductName { get; set; }

    /// <summary>
    /// The reason why the product was not included
    /// </summary>
    public required S100ProductNotIncludedReason Reason { get; set; }
}

/// <summary>
/// Reasons why a product might not be included in the exchange set
/// </summary>
public enum S100ProductNotIncludedReason
{
    /// <summary>
    /// The product has been withdrawn from the S-100 Service
    /// </summary>
    ProductWithdrawn,

    /// <summary>
    /// The product is not part of the S-100 Service (invalid or unknown product)
    /// </summary>
    InvalidProduct,

    /// <summary>
    /// The product has been cancelled and is beyond the retention period
    /// </summary>
    NoDataAvailableForCancelledProduct
}
