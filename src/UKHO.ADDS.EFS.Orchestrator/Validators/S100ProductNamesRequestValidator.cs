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
            .Must(HaveUniqueProductNamesByProducer)
            .WithMessage("Product names with the same unique code from different producers are not allowed")
            .When(request => request.ProductNames is not null);
    }

    private static bool HaveUniqueProductNames(List<string>? productNames)
    {
        if (productNames == null)
            return true;

        var distinctCount = productNames.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        return distinctCount == productNames.Count;
    }

    private static bool HaveUniqueProductNamesByProducer(List<string>? productNames)
    {
        if (productNames == null)
            return true;

        // Group products by producer code and check for unique codes within each producer
        var producerGroups = productNames
            .Where(name => !string.IsNullOrEmpty(name) && name.Length >= 8)
            .GroupBy(name => new
            {
                ProductCode = name.Length >= 3 ? name[..3] : "",
                ProducerCode = name.Length >= 7 ? name.Substring(3, 4) : "",
                UniqueCode = name.Length >= 8 ? name[7..] : ""
            })
            .Where(g => !string.IsNullOrEmpty(g.Key.ProductCode) &&
                       !string.IsNullOrEmpty(g.Key.ProducerCode) &&
                       !string.IsNullOrEmpty(g.Key.UniqueCode));

        // Check if any group has more than one product (indicating duplicate unique codes from same producer)
        return !producerGroups.Any(g => g.Count() > 1);
    }
}
