using FluentValidation;
using UKHO.ADDS.EFS.Messages;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100ProductNamesRequest
/// </summary>
internal class S100ProductNamesRequestValidator : AbstractValidator<S100ProductNamesRequest>
{
    public S100ProductNamesRequestValidator()
    {
        RuleFor(request => request.ProductNames)
            .NotNull()
            .WithMessage("ProductNames cannot be null")
            .NotEmpty()
            .WithMessage("ProductNames cannot be empty")
            .Must(productNames => productNames.Count <= 100)
            .WithMessage("Maximum of 100 product names allowed per request");

        RuleForEach(request => request.ProductNames)
            .SetValidator(new S100ProductNameValidator());

        RuleFor(request => request.ProductNames)
            .Must(HaveUniqueProductNames)
            .WithMessage("Duplicate product names are not allowed")
            .When(request => request.ProductNames is not null);

        RuleFor(request => request.ProductNames)
            .Must(HaveUniqueUniqueCodesByProducer)
            .WithMessage("Unique codes must not be reused for different datasets from the same producer (e.g., 101GB00QWERTY and 102GB00QWERTY are not allowed)")
            .When(request => request.ProductNames is not null);
    }

    private static bool HaveUniqueProductNames(List<string>? productNames)
    {
        if (productNames == null)
            return true;

        var distinctCount = productNames.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return distinctCount == productNames.Count;
    }

    private static bool HaveUniqueUniqueCodesByProducer(List<string>? productNames)
    {
        if (productNames == null)
            return true;

        var uniqueCodesByProducer = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var productName in productNames)
        {
            if (string.IsNullOrEmpty(productName))
                continue;

            // Extract filename without extension for validation
            var lastDotIndex = productName.LastIndexOf('.');
            var filenameWithoutExtension = lastDotIndex > 0 ? productName[..lastDotIndex] : productName;

            if (filenameWithoutExtension.Length < 8)
                continue;

            var producerCode = filenameWithoutExtension.Substring(3, 4);
            var uniqueCode = filenameWithoutExtension[7..];

            if (string.IsNullOrEmpty(producerCode) || string.IsNullOrEmpty(uniqueCode))
                continue;

            // Check if this unique code has already been used by this producer
            if (!uniqueCodesByProducer.ContainsKey(producerCode))
            {
                uniqueCodesByProducer[producerCode] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (uniqueCodesByProducer[producerCode].Contains(uniqueCode))
            {
                // Unique code already exists for this producer
                return false;
            }

            uniqueCodesByProducer[producerCode].Add(uniqueCode);
        }

        return true;
    }

    // Keep the old method for backward compatibility but mark as obsolete
    [Obsolete("Use HaveUniqueUniqueCodesByProducer instead")]
    private static bool HaveUniqueProductNamesByProducer(List<string>? productNames)
    {
        return HaveUniqueUniqueCodesByProducer(productNames);
    }
}
