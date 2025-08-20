namespace UKHO.ADDS.EFS.Messages;

/// <summary>
/// Request model for S100 product versions endpoint
/// </summary>
public class S100ProductVersionsRequest
{
    /// <summary>
    /// List of S100 product versions to request
    /// </summary>
    public required List<S100ProductVersion> ProductVersions { get; set; }
}

/// <summary>
/// Represents a S100 product version with edition and update numbers
/// </summary>
public class S100ProductVersion
{
    /// <summary>
    /// The unique product identifier
    /// </summary>
    public required string ProductName { get; set; }
    
    /// <summary>
    /// The edition number
    /// </summary>
    public required int EditionNumber { get; set; }
    
    /// <summary>
    /// The update number, if applicable
    /// </summary>
    public int UpdateNumber { get; set; }
}
