param (
    [Parameter(Mandatory = $true)] [string] $BuildNumber,
    [Parameter(Mandatory = $true)] [string] $SolutionDirectory,
    [Parameter(Mandatory = $true)] [string] $UkhoAssemblyCompany,
    [Parameter(Mandatory = $true)] [string] $UkhoAssemblyCopyright,
    [Parameter(Mandatory = $true)] [string] $UkhoAssemblyVersionPrefix,
    [Parameter(Mandatory = $true)] [string] $UkhoAssemblyProduct,
    [Parameter(Mandatory = $true)] [string] $SourceRevisionId
)

Write-Host "Build number: " $BuildNumber

$buildNumberRegex = "(.+)_202([0-9]{3,5})\.([0-9]{1,2})"
$validBuildNumber = $BuildNumber -match $buildNumberRegex

if ($validBuildNumber -eq $false) {
    $errorMessage = "Build number passed in must be in the following format: (BuildDefinitionName)_.(date:yyyyMMdd)(rev:.r)"
    Write-Error $errorMessage
    throw $errorMessage
}

# Magic var $Matches comes from the above regex match statement: $BuildNumber -match $buildNumberRegex
$versionPrefix = $UkhoAssemblyVersionPrefix + $Matches.2
$versionSuffix = "alpha." + $Matches.3
#$versionSuffix = "beta." + $Matches.3
#$versionSuffix = $Matches.3
$assemblyVersion = $versionPrefix + "." + $Matches.3
$versionFull = $versionPrefix + "." + $versionSuffix
Write-Host "##vso[task.setvariable variable=NuGetVersion;isOutput=true]$($versionFull)"

$assemblyValues = @{
    "Company"           = $UkhoAssemblyCompany;
    "Copyright"         = $UkhoAssemblyCopyright;
    "Description"       = $UkhoAssemblyProduct;
    "Product"           = $UkhoAssemblyProduct;
    "AssemblyVersion"   = $assemblyVersion;
    "FileVersion"       = $assemblyVersion;
    "VersionPrefix"     = $versionPrefix;
    "VersionSuffix"     = $versionSuffix;
    "SourceRevisionId"  = $SourceRevisionId;
}

function UpdateOrAddAttribute($xmlContent, $assemblyKey, $newValue, $namespace) {
    $propertyGroup = $xmlContent.Project.PropertyGroup
    if ($propertyGroup -is [array]) {
        $propertyGroup = $propertyGroup[0]
    }

    $propertyGroupNode = $propertyGroup.$assemblyKey

    if ($null -ne $propertyGroupNode) {
        Write-Host "Assembly key $assemblyKey has been located in source file - updating with value: " $newValue
        $propertyGroup.$assemblyKey = $newValue
        return $xmlContent
    }

    Write-Host "Assembly key $assemblyKey could not be located in source file - appending value " $newValue

    $newChild = $xmlContent.CreateElement($assemblyKey, $namespace)
    $newChild.InnerText = $newValue
    $propertyGroup.AppendChild($newChild)

    return $propertyGroupNode
}

(Get-ChildItem -Path $SolutionDirectory -File -Filter "*.csproj" -Recurse) | ForEach-Object {
    $file = $_

    Write-Host "Updating assembly file at path: $file"
    [xml]$xmlContent = (Get-Content $file.FullName)

    $assemblyValues.Keys | ForEach-Object {
        $key = $_

        UpdateOrAddAttribute $xmlContent $key $assemblyValues[$key] $xmlContent.DocumentElement.NamespaceURI
    }

    $xmlContent.Save($file.FullName)
}
