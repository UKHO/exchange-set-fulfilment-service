namespace UKHO.ADDS.EFS.Domain.Products
{
    /// <summary>
    ///     Represents a product version with edition and update numbers
    /// </summary>
    public partial struct ProductVersion
    {
        /// <summary>
        ///     The unique product identifier
        /// </summary>
        public required ProductName ProductName { get; set; }

        /// <summary>
        ///     The edition number
        /// </summary>
        public required EditionNumber EditionNumber { get; set; }

        /// <summary>
        ///     The update number, if applicable
        /// </summary>
        public UpdateNumber UpdateNumber { get; set; }
    }
}
