using System.Runtime.Serialization;

namespace UKHO.ADDS.EFS.Domain.Products
{
    /// <summary>
    ///     Reasons why a product might not be included in the exchange set
    /// </summary>
    public enum MissingProductReason
    {
        [EnumMember(Value = "none")] None = -1,
        [EnumMember(Value = "productWithdrawn")] ProductWithdrawn,
        [EnumMember(Value = "invalidProduct")] InvalidProduct,
        [EnumMember(Value = "noDataAvailableForCancelledProduct")] NoDataAvailableForCancelledProduct,
        [EnumMember(Value = "duplicateProduct")] DuplicateProduct
    }
}
