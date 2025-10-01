# UKHO.FakePenrose.S100SampleExchangeSets

A .NET library for generating sample S-100 Exchange Set files for maritime hydrographic data testing and development purposes.

## Overview

This package provides an API to generate single change S-100 Sample Exchange Sets for testing and development. It supports S-101, S-102, S-104, and S-111 product standards with pre-canned sample data.

## Supported Standards

- **S-101**: Electronic Navigational Charts (ENC)
- **S-102**: Bathymetric Surface
- **S-104**: Water Level Information for Surface Navigation
- **S-111**: Surface Currents

## Installation

```bash
dotnet add package UKHO.FakePenrose.S100SampleExchangeSets
```

## Usage

### Basic Example

```csharp
using UKHO.FakePenrose.S100SampleExchangeSets.SampleFileSources;

// Create the file source
var fileSource = new S100FileSource();

// Generate individual files for an S-101 exchange set
var files = fileSource.GetFiles("101GB12345678", editionNumber: 1, productUpdateNumber: 0);

foreach (var file in files)
{
    Console.WriteLine($"File: {file.FileName}, Type: {file.MimeType}");
    // Process file.FileStream as needed
}

// Generate a complete ZIP archive
var zipFile = fileSource.GetZipFile("101GB12345678", editionNumber: 1, productUpdateNumber: 0);
using var fileStream = File.Create("sample-exchange-set.zip");
zipFile.FileStream.CopyTo(fileStream);
```

### Product Name Format

Product names should follow the S-100 naming convention:
- **S-101**: `101[CountryCode][CellIdentifier]` (e.g., `101GB12345678`)
- **S-102**: `102[CountryCode][Identifier]` (e.g., `102GBTD5N5050W00120`)
- **S-104**: `104[CountryCode][Identifier]` (e.g., `104GB00`)
- **S-111**: `111[CountryCode][Identifier]` (e.g., `111GB00`)

### API Reference

#### IS100FileSource Interface

```csharp
public interface IS100FileSource
{
    IEnumerable<S100SampleFileInfo> GetFiles(string productName, int editionNumber, int productUpdateNumber);
    IEnumerable<S100SampleFileInfo> GetFiles(string productName, int editionNumber, int productUpdateNumber, int dataProviderIndexOverride);
    S100SampleFileInfo GetZipFile(string productName, int editionNumber, int productUpdateNumber);
    S100SampleFileInfo GetZipFile(string productName, int editionNumber, int productUpdateNumber, int dataProviderIndexOverride);
}
```

#### S100SampleFileInfo Class

```csharp
public class S100SampleFileInfo
{
    public string FileName { get; set; }
    public string? OriginalFileName { get; set; }
    public string MimeType { get; set; }
    public Stream FileStream { get; set; }
}
```

### Examples by Product Type

#### S-101 (Electronic Navigational Chart)
```csharp
var s101Files = fileSource.GetFiles("101GB00501186", 1, 0);
// Generates: CATALOG.XML, dataset files (.000), and support files (.TXT)

// Using dataProviderIndexOverride
var s101FilesWithOverride = fileSource.GetFiles("101GB00501186", 1, 0, dataProviderIndexOverride: 0);
```

#### S-102 (Bathymetric Surface)
```csharp
var s102Files = fileSource.GetFiles("102GBTD5N5050W00120", 1, 0);
// Generates: CATALOG.XML and HDF5 dataset files (.h5)

// Using dataProviderIndexOverride
var s102FilesWithOverride = fileSource.GetFiles("102GBTD5N5050W00120", 1, 0, dataProviderIndexOverride: 1);
```

#### S-104 (Water Level Information)
```csharp
var s104Files = fileSource.GetFiles("104GB00", 1, 0);
// Generates: CATALOG.XML and HDF5 dataset files (.h5)

// Using dataProviderIndexOverride
var s104FilesWithOverride = fileSource.GetFiles("104GB00", 1, 0, dataProviderIndexOverride: 0);
```

#### S-111 (Surface Currents)
```csharp
var s111Files = fileSource.GetFiles("111GB00", 1, 0);
// Generates: CATALOG.XML and HDF5 dataset files (.h5)

// Using dataProviderIndexOverride
var s111FilesWithOverride = fileSource.GetFiles("111GB00", 1, 0, dataProviderIndexOverride: 1);
```

## File Types Generated

- **CATALOG.XML**: S-100 catalog file with product metadata
- **Dataset Files**: 
  - `.000/.001/.002` files for S-101
  - `.h5` (HDF5) files for S-102, S-104, S-111
- **Support Files**: Text files (`.TXT`) for additional product information

## Notes

- All sample data is embedded as resources within the package
- Product names are dynamically replaced in catalog files
- File extensions are automatically adjusted based on update numbers
- Generated files are suitable for testing but not for production navigation

## License

MIT License - see the [GitHub repository](https://github.com/UKHO/fake-penrose) for full details.

## Contributing

Visit the [GitHub repository](https://github.com/UKHO/fake-penrose) to report issues or contribute to the project.