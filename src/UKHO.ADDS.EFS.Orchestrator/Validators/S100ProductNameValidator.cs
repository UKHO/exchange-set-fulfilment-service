using System.Text.RegularExpressions;
using FluentValidation;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100 product names according to S100 specifications
/// </summary>
internal class S100ProductNameValidator : AbstractValidator<string>
{
    private const string ProductNamesPropertyName = "ProductNames";
    // S-100 Product standards
    private static readonly string[] ValidProductCodes = ["101", "102", "104", "111"];

    // S-101/S-102: A-Z (uppercase), 0-9, underscore
    private static readonly Regex S101S102UniqueCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);

    // S-104/S-111: A-Z, a-z, 0-9, hyphen, underscore
    private static readonly Regex S104S111UniqueCodeRegex = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);

    // Producer code validation: alphanumeric only
    private static readonly Regex ProducerCodeRegex = new(@"^[A-Za-z0-9]{4}$", RegexOptions.Compiled);

    // Reject all non-ASCII characters anywhere in the filename (e.g., accented characters like é)
    private static readonly Regex NonAsciiCharsRegex = new(@"[^\u0000-\u007F]", RegexOptions.Compiled);

    public S100ProductNameValidator()
    {
        RuleFor(productName => productName)
            .NotNull()
            .WithMessage("Product name cannot be null")
            .WithName(ProductNamesPropertyName)
            .NotEmpty()
            .WithMessage("Product name cannot be empty")
            .WithName(ProductNamesPropertyName);

        RuleFor(productName => productName)
            .Must(HasOnlyAsciiCharacters)
            .WithMessage(productName => $"{productName} - Product name contains invalid characters. Only ASCII characters are allowed (A-Z, a-z, 0-9, underscore, hyphen, and dot)")
            .WithName(ProductNamesPropertyName)
            .When(productName => !string.IsNullOrEmpty(productName));

        RuleFor(productName => productName)
            .Custom((productName, context) =>
            {
                if (!HasValidProductCode(productName))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(
                        ProductNamesPropertyName, // Custom property name instead of the actual product name value
                        $"{productName} - Product name must start with a valid S-100 product code: 101, 102, 104, or 111"
                    ));
                }
            })
            .When(productName => !string.IsNullOrEmpty(productName));

        RuleFor(productName => productName)
            .Custom((productName, context) =>
            {
                if (!HasValidProducerCode(productName))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(
                        ProductNamesPropertyName, // Custom property name instead of the actual product name value
                        $"{productName} - Product name must have a valid 4-character alphanumeric producer code after the product code"
                    ));
                }
            })
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName));

        RuleFor(productName => productName)
            .Custom((productName, context) =>
            {
                if (!HasValidUniqueCode(productName))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(
                        ProductNamesPropertyName, // Custom property name instead of the actual product name value
                        $"{productName} - Product name must have at least one character for the unique code portion"
                    ));
                }
            })
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName) && HasValidProducerCode(productName));

        RuleFor(productName => productName)
            .Custom((productName, context) =>
            {
                if (!HasValidLength(productName))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(
                        ProductNamesPropertyName, // Custom property name instead of the actual product name value
                        $"{productName} - {GetLengthValidationMessage(productName)}"
                    ));
                }
            })
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName));

        RuleFor(productName => productName)
            .Custom((productName, context) =>
            {
                if (!HasValidUniqueCodeCharacters(productName))
                {
                    context.AddFailure(new FluentValidation.Results.ValidationFailure(
                        ProductNamesPropertyName, // Custom property name instead of the actual product name value
                        $"{productName} - {GetCharacterValidationMessage(productName)}"
                    ));
                }
            })
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName) && HasValidProducerCode(productName) && HasValidUniqueCode(productName));
    }

    private static string GetFilenameWithoutExtension(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return string.Empty;

        var lastDotIndex = productName.LastIndexOf('.');
        return lastDotIndex > 0 ? productName[..lastDotIndex] : productName;
    }

    private static bool HasValidProductCode(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 3)
            return false;

        var productCode = filenameWithoutExtension[..3];
        return productCode.All(char.IsDigit) && ValidProductCodes.Contains(productCode);
    }

    private static bool HasValidProducerCode(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 7)
            return false;

        var producerCode = filenameWithoutExtension.Substring(3, 4);
        return ProducerCodeRegex.IsMatch(producerCode);
    }

    private static bool HasValidUniqueCode(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 8)
            return false;

        var uniqueCode = filenameWithoutExtension[7..];
        return uniqueCode.Length >= 1;
    }

    private static bool HasValidLength(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 3)
            return false;

        var productCode = filenameWithoutExtension[..3];
        return productCode switch
        {
            "101" => filenameWithoutExtension.Length is >= 8 and <= 17,
            "102" => filenameWithoutExtension.Length is >= 8 and <= 19,
            "104" => filenameWithoutExtension.Length is >= 8 and <= 61,
            "111" => filenameWithoutExtension.Length is >= 8 and <= 61,
            _ => false
        };
    }

    private static bool HasOnlyAsciiCharacters(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;
        return !NonAsciiCharsRegex.IsMatch(productName);
    }

    private static bool HasValidUniqueCodeCharacters(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 8)
            return false;

        var productCode = filenameWithoutExtension[..3];
        var uniqueCode = filenameWithoutExtension[7..];

        return productCode switch
        {
            "101" or "102" => S101S102UniqueCodeRegex.IsMatch(uniqueCode),
            "104" or "111" => S104S111UniqueCodeRegex.IsMatch(uniqueCode),
            _ => false
        };
    }

    private static string GetLengthValidationMessage(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 3)
            return "Product name must be between 8 and maximum allowed characters for the specific product type";

        var productCode = filenameWithoutExtension[..3];
        return productCode switch
        {
            "101" => "S-101 product name (without extension) must be between 8 and 17 characters long",
            "102" => "S-102 product name (without extension) must be between 8 and 19 characters long",
            "104" => "S-104 product name (without extension) must be between 8 and 61 characters long",
            "111" => "S-111 product name (without extension) must be between 8 and 61 characters long",
            _ => "Product name must be between 8 and maximum allowed characters for the specific product type"
        };
    }

    private static string GetCharacterValidationMessage(string? productName)
    {
        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        if (filenameWithoutExtension.Length < 3)
            return "Unique code portion contains invalid characters for the specific product type";

        var productCode = filenameWithoutExtension[..3];
        return productCode switch
        {
            "101" or "102" => "For S-101/S-102, unique code must contain only uppercase letters (A-Z), digits (0-9), and underscores (_)",
            "104" or "111" => "For S-104/S-111, unique code must contain only letters (A-Z, a-z), digits (0-9), hyphens (-), and underscores (_)",
            _ => "Unique code portion contains invalid characters for the specific product type"
        };
    }
}
