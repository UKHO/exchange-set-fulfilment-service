using System.Text.RegularExpressions;
using FluentValidation;

namespace UKHO.ADDS.EFS.Orchestrator.Validators;

/// <summary>
/// Validator for S100 product names according to S100 specifications
/// </summary>
internal class S100ProductNameValidator : AbstractValidator<string>
{
    // S-100 Product standards
    private static readonly string[] ValidProductCodes = ["101", "102", "104", "111"];
    
    // Character rules for S-101 and S-102: A-Z (uppercase), 0-9, underscore
    private static readonly Regex S101S102UniqueCodeRegex = new(@"^[A-Z0-9_]+$", RegexOptions.Compiled);
    
    // Character rules for S-104 and S-111: A-Z, a-z, 0-9, hyphen, underscore  
    private static readonly Regex S104S111UniqueCodeRegex = new(@"^[A-Za-z0-9_-]+$", RegexOptions.Compiled);
    
    // UTF-8 validation - reject characters outside valid ranges
    private static readonly Regex InvalidUtf8Regex = new(@"[^\u0020-\u007E\u00A0-\u00FF\u0100-\u017F\u0180-\u024F\u1E00-\u1EFF]", RegexOptions.Compiled);

    // Valid extensions for each product type
    private static readonly string[] S101ValidExtensions = ["000", "001", "002", "003", "004", "005", "006", "007", "008", "009"];
    private static readonly string[] S102ValidExtensions = ["h5"];
    private static readonly string[] S104ValidExtensions = ["h5"];
    private static readonly string[] S111ValidExtensions = ["h5"];

    public S100ProductNameValidator()
    {
        RuleFor(productName => productName)
            .NotNull()
            .WithMessage("Product name cannot be null")
            .NotEmpty()
            .WithMessage("Product name cannot be empty");

        RuleFor(productName => productName)
            .Must(HasValidUtf8Encoding)
            .WithMessage("Product name contains invalid UTF-8 characters")
            .When(productName => !string.IsNullOrEmpty(productName));

        RuleFor(productName => productName)
            .Must(HasValidExtension)
            .WithMessage(productName => GetExtensionValidationMessage(productName))
            .When(productName => !string.IsNullOrEmpty(productName));

        RuleFor(productName => productName)
            .Must(HasValidProductCode)
            .WithMessage("Product name must start with a valid S-100 product code: 101, 102, 104, or 111")
            .When(productName => !string.IsNullOrEmpty(productName));

        RuleFor(productName => productName)
            .Must(HasValidProducerCode)
            .WithMessage("Product name must have a valid 4-character alphanumeric producer code after the product code")
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName));

        RuleFor(productName => productName)
            .Must(HasValidUniqueCode)
            .WithMessage("Product name must have at least one character for the unique code portion")
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName) && HasValidProducerCode(productName));

        RuleFor(productName => productName)
            .Must(HasValidLength)
            .WithMessage(productName => GetLengthValidationMessage(productName))
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName));

        RuleFor(productName => productName)
            .Must(HasValidUniqueCodeCharacters)
            .WithMessage(productName => GetCharacterValidationMessage(productName))
            .When(productName => !string.IsNullOrEmpty(productName) && HasValidProductCode(productName) && HasValidProducerCode(productName) && HasValidUniqueCode(productName));
    }

    private static string GetFilenameWithoutExtension(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return string.Empty;

        var lastDotIndex = productName.LastIndexOf('.');
        return lastDotIndex > 0 ? productName[..lastDotIndex] : productName;
    }

    private static string GetFileExtension(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return string.Empty;

        var lastDotIndex = productName.LastIndexOf('.');
        return lastDotIndex > 0 && lastDotIndex < productName.Length - 1 
            ? productName[(lastDotIndex + 1)..] 
            : string.Empty;
    }

    private static bool HasValidExtension(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        var extension = GetFileExtension(productName);

        if (string.IsNullOrEmpty(extension))
            return false;

        if (filenameWithoutExtension.Length < 3)
            return false;

        var productCode = filenameWithoutExtension[..3];
        
        return productCode switch
        {
            "101" => S101ValidExtensions.Contains(extension) || (extension.Length == 3 && extension.All(char.IsDigit) && int.Parse(extension) <= 999),
            "102" => S102ValidExtensions.Contains(extension),
            "104" => S104ValidExtensions.Contains(extension),
            "111" => S111ValidExtensions.Contains(extension),
            _ => false
        };
    }

    private static bool HasValidProductCode(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        
        if (filenameWithoutExtension.Length < 3)
            return false;

        var productCode = filenameWithoutExtension[..3];
        return ValidProductCodes.Contains(productCode);
    }

    private static bool HasValidProducerCode(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        
        if (filenameWithoutExtension.Length < 7)
            return false;

        var producerCode = filenameWithoutExtension.Substring(3, 4);
        return producerCode.All(char.IsLetterOrDigit);
    }

    private static bool HasValidUniqueCode(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        
        if (filenameWithoutExtension.Length < 8)
            return false;

        var uniqueCode = filenameWithoutExtension[7..];
        return uniqueCode.Length >= 1;
    }

    private static bool HasValidLength(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

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

    private static bool HasValidUtf8Encoding(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

        return !InvalidUtf8Regex.IsMatch(productName);
    }

    private static bool HasValidUniqueCodeCharacters(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return false;

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

    private static string GetExtensionValidationMessage(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return "Product name must have a valid file extension";

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        var extension = GetFileExtension(productName);

        if (string.IsNullOrEmpty(extension))
            return "Product name must have a file extension";

        if (filenameWithoutExtension.Length < 3)
            return "Product name must have a valid file extension";

        var productCode = filenameWithoutExtension[..3];
        
        return productCode switch
        {
            "101" => "S-101 files must have extension .000-.999 (e.g., .000, .001, .002)",
            "102" => "S-102 files must have extension .h5",
            "104" => "S-104 files must have extension .h5",
            "111" => "S-111 files must have extension .h5",
            _ => "Product name must have a valid file extension for the specific product type"
        };
    }

    private static string GetLengthValidationMessage(string? productName)
    {
        if (string.IsNullOrEmpty(productName))
            return "Product name must be between 8 and maximum allowed characters for the specific product type";

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
        if (string.IsNullOrEmpty(productName))
            return "Unique code portion contains invalid characters for the specific product type";

        var filenameWithoutExtension = GetFilenameWithoutExtension(productName);
        
        if (filenameWithoutExtension.Length < 3)
            return "Unique code portion contains invalid characters for the specific product type";

        var productCode = filenameWithoutExtension[..3];
        return productCode switch
        {
            "101" or "102" => "For S-101/S-102, unique code must contain only uppercase letters (A-Z), digits (0-9), and underscores (_)|",
            "104" or "111" => "For S-104/S-111, unique code must contain only letters (A-Z, a-z), digits (0-9), hyphens (-), and underscores (_)",
            _ => "Unique code portion contains invalid characters for the specific product type"
        };
    }
}
