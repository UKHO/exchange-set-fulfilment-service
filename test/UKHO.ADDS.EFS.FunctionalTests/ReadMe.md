# UKHO.ADDS.EFS.FunctionalTests

## Overview

This project contains functional tests for the Exchange Set Fulfilment Service (EFS). These tests validate the behavior of the EFS API endpoints and orchestrator components through end-to-end testing with a fully functional Aspire environment. The tests cover the S100 exchange set API endpoints including:

- **Product Names Endpoint** (`/v2/exchangeSet/s100/productNames`) - Tests product name-based exchange set requests
- **Product Versions Endpoint** (`/v2/exchangeSet/s100/productVersions`) - Tests product version-specific exchange set requests  
- **Updates Since Endpoint** (`/v2/exchangeSet/s100/updatesSince`) - Tests incremental update requests
- **Job Management** (`/jobs`) - Tests job submission, monitoring, and status checking

The tests validate input validation, job orchestration, build processes, and exchange set generation with comprehensive error handling and response validation.

## Test Architecture

The test architecture uses a combination of patterns to optimize both resource usage and test execution speed:

### 1. Shared Aspire Environment

Tests share a single Aspire environment instance to avoid the significant overhead of starting/stopping the environment for each test class:

- `AspireTestHost` (Infrastructure): Thread-safe singleton that creates and manages a single Aspire environment for all tests. Detects if running in CI/CD pipeline and configures HTTP clients accordingly.
- `StartupFixture` & `StartupCollection`: xUnit collection fixtures to share this environment across test classes via `[Collection("Startup Collection")]`.

### 2. Parallel Test Execution

Tests are configured to run in parallel for faster execution:

- `Meziantou.Xunit.ParallelTestFramework`: Enables advanced parallel test execution.
- `xunit.runner.json`: Configures theory test pre-enumeration for parallel execution of individual theory test cases.
- `[EnableParallelization]` attribute: Enables parallelization within test classes.
- `[DisableParallelization]` attribute: Used selectively on theory tests where test cases need sequential execution.

### 3. Environment Detection

The architecture automatically detects execution environment:
- **Local Development**: Starts full Aspire application with orchestrator and mock services
- **CI/CD Pipeline**: Uses environment variables (`ORCHESTRATOR_URL`, `ADDSMOCK_URL`) for external service endpoints

### 4. Retry and Resilience

Tests include retry mechanisms for reliability:
- `xRetry` framework with `[RetryTheory]` for handling transient failures
- Configurable retry counts and delays for flaky test scenarios

## Namespaces and Folders

Support code is organized by responsibility to improve discoverability and clarity:

- UKHO.ADDS.EFS.FunctionalTests.Scenarios
  - Feature-focused test classes (e.g., `JobsFunctionalTests`, `ProductNamesFunctionalTests`, `ProductVersionsFunctionalTests`, `UpdateSinceFunctionalTests`)
- UKHO.ADDS.EFS.FunctionalTests.Framework
  - `FunctionalTestBase`: shared base for test classes
- UKHO.ADDS.EFS.FunctionalTests.Infrastructure
  - `AspireTestHost`, `AssemblySetup`, `StartupFixture`
- UKHO.ADDS.EFS.FunctionalTests.Http
  - `OrchestratorClient`: HTTP helpers for orchestrator endpoints
- UKHO.ADDS.EFS.FunctionalTests.IO
  - `MockFilesClient`: helpers for downloading files from the mock host
- UKHO.ADDS.EFS.FunctionalTests.Assertions
  - `ExchangeSetApiAssertions`: API response validation
  - `ZipArchiveAssertions`: ZIP structure/content validation
  - `CallbackResponseAssertions`: callback payload validation
- UKHO.ADDS.EFS.FunctionalTests.Diagnostics
  - `TestOutput`: ambient test logging (xUnit output helper)
  - `BuilderStepsParser` (if present): parsing/analysis of builder steps

## Key Components

### Framework and Diagnostics

#### FunctionalTestBase
Abstract base class providing:
- Shared access to `StartupFixture` and `ITestOutputHelper`
- Ambient test output context via a disposable scope
- Implements `IDisposable` to restore logging context on teardown


#### TestOutput (Diagnostics)
Static context class that:
- Provides ambient access to xUnit's `ITestOutputHelper` across the test project
- Allows static utility classes to write to test output without direct dependencies
- Thread-safe with `AsyncLocal<T>` storage for parallel test execution
- `BeginScope(ITestOutputHelper)`: safely sets and restores Current (prevents leakage across tests)
- `Clear()`: resets Current to null (used in teardown)
- `WriteLine(FormattableString)`: uses invariant culture formatting
- `WriteLine(Exception ex, string? message)`: logs optional context + full exception details

Typical usage:
- In a test: `using var _ = TestOutput.BeginScope(output);`
- Log formatted text: `TestOutput.WriteLine($"Count: {count}");`
- Log errors: `TestOutput.WriteLine(ex, "Operation failed");`
- In teardown: `TestOutput.Clear()`

Notes:
- xUnit injects `ITestOutputHelper` into each test class constructor.
- The base class sets the ambient logger using `TestOutput.BeginScope(output)` and restores it on dispose.
- Static helpers that log via `TestOutput.WriteLine(...)` (e.g., `ZipArchiveAssertions`, `MockFilesClient`) write to the same xUnit output for the currently executing test.
- Parallel test safety: `TestOutput` uses `AsyncLocal<ITestOutputHelper?>`, so the Current value flows with the async context of each test, avoiding cross-test leakage when tests run concurrently.

### Support classes by area

#### Http
- `OrchestratorClient`
  - `PostRequestAsync()`: Submits jobs to various API endpoints
  - `WaitForJobCompletionAsync()`: Monitors job status with timeout handling
  - `GetBuildStatusAsync()`: Retrieves detailed build status information

#### Assertions
- `ExchangeSetApiAssertions`
  - `CheckCustomExSetReqResponse`: Validates custom exchange set request responses
  - `CheckJobCompletionStatus()`: Validates job completion and build state
  - `CheckBuildStatus()`: Validates builder steps and exit codes
- `ZipArchiveAssertions`
  - `CompareZipFilesExactMatch()`: Compares ZIP file structures and validates product inclusion
- `CallbackResponseAssertions`
  - `CompareCallbackResponse()`: Validates JSON response data against expected structure

#### IO
- `MockFilesClient`
  - `DownloadExchangeSetAsZipAsync()`, `DownloadCallbackTextAsync()`

#### Infrastructure
- `AspireTestHost`: Aspire orchestration and environment bootstrapping
- `AssemblySetup`, `StartupFixture`: xUnit collection fixture wiring

## Test Classes (Scenarios)

#### ProductNamesFunctionalTests
Tests the `/v2/exchangeSet/s100/productNames` endpoint:
- Input validation with valid and invalid product names
- Callback URI handling (with and without callbacks)
- Exchange set generation and structure validation


#### JobsFunctionalTests

Core job management tests:
- General job submission and monitoring
- Build process validation
- End-to-end exchange set generation workflows


#### ProductVersionsFunctionalTests, UpdateSinceFunctionalTests
- Endpoint-specific happy-path and validation scenarios similar to ProductNames tests

## Assertion Management

We rely on AwesomeAssertions' built‑in `AssertionScope` behavior.

Core fact: AwesomeAssertions already supports soft (aggregated) assertions natively. A single outer `AssertionScope` created at the start of the test accumulates all failures produced inside (including from helper/service classes). Only when that outer scope is disposed (test method exits) does it throw once with the combined message. Any nested scopes just merge their failures upward; they never throw independently while a parent scope is active.

Pattern:
```
[Fact]
public async Task Product_journey()
{
    using var scope = new AssertionScope();   // root scope

    await Step1_GetProducts();
    await Step2_ValidateMetadata();
    await Step3_DownloadFiles();

    // Leaving the method disposes 'scope' and throws once if any assertion failed.
}
```


Helper methods do not need to (and generally should not) create their own `AssertionScope` unless you want to locally group or label messages. Just write normal AwesomeAssertions:
```
response.StatusCode.Should().Be(HttpStatusCode.OK);
json.RootElement.GetProperty("jobId").GetString().Should().Be(jobId);
```
These failures will be aggregated by the root scope.

### When to Use Assert vs Should

- Use `Assert.*` (xUnit) for critical preconditions where continuing the test makes no sense (e.g., HTTP call failed, required file missing). These fail immediately.
- Use `Should()` fluent assertions for everything you want aggregated and reported together at the end.

### Immediate Failure (Opt-Out)
If you want a helper to fail immediately (not soft) just do NOT create a root `AssertionScope` before calling it. Without an active outer scope each `Should()` failure throws immediately.

- No need to manually collect or report failures.
- Ensure each test method creates exactly one root `AssertionScope` if you want soft behavior.

## Using Assertions in Tests (Examples)
```
[Fact]
public async Task EndToEnd()
{
    using var scope = new AssertionScope();

    var submit = await OrchestratorClient.PostRequestAsync(requestId, payload, endpoint);
    submit.StatusCode.Should().Be(HttpStatusCode.Accepted);

    var statusResponse = await OrchestratorClient.WaitForJobCompletionAsync(jobId);
    var assertions = new ExchangeSetApiAssertions();
    await assertions.CheckJobCompletionStatus(statusResponse);

    var buildResponse = await OrchestratorClient.GetBuildStatusAsync(jobId);
    await assertions.CheckBuildStatus(buildResponse);

    // All failures (if any) are reported together here.
}
```

## Creating a New Test Class

1. Add collection attribute: `[Collection("Startup Collection")]`
2. Enable parallelization: `[EnableParallelization]` if appropriate
3. Inherit from `FunctionalTestBase`
4. Create a single root `AssertionScope` inside each test method needing soft aggregation

```
using UKHO.ADDS.EFS.FunctionalTests.Framework;

[Collection("Startup Collection")]
[EnableParallelization]
public class MyNewFunctionalTests : FunctionalTestBase
{
    public MyNewFunctionalTests(StartupFixture startup, ITestOutputHelper output) : base(startup, output) { }

    [Fact]
    public async Task MyTest()
    {
        using var scope = new AssertionScope();

        value1.Should().NotBeNull();
        value2.Should().BeGreaterThan(0);
        value3.Should().Be(expected);
    }
}
```

## Theory Tests with Retry
```
[RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
[DisableParallelization] // Optional if sequential execution needed
[InlineData("param1", "param2", HttpStatusCode.Accepted, "")]
public async Task MyTestMethod(string p1, string p2, HttpStatusCode expectedStatus, string expectedError)
{
    using var scope = new AssertionScope();

    var response = await OrchestratorClient.PostRequestAsync(...);

    response.StatusCode.Should().Be(expectedStatus);

    if (!string.IsNullOrEmpty(expectedError))
        (await response.Content.ReadAsStringAsync()).Should().Contain(expectedError);
}
```

## Job Submission and Monitoring Pattern

1. Submit job: `var response = await OrchestratorClient.PostRequestAsync(requestId, payload, endpoint);`
2. Wait for completion: `var jobStatusResponse = await OrchestratorClient.WaitForJobCompletionAsync(jobId);`
3. Validate status: `await new ExchangeSetApiAssertions().CheckJobCompletionStatus(jobStatusResponse);`
4. (Optional) Build details: `var buildResponse = await OrchestratorClient.GetBuildStatusAsync(jobId);`
5. Validate build: `await new ExchangeSetApiAssertions().CheckBuildStatus(buildResponse);`
6. (Optional) Validate exchange set contents: `ZipArchiveAssertions.CompareZipFilesExactMatch(expectedZip, downloadedZip)`

## Environment Variables (CI/CD)

When running in CI/CD pipelines, set these environment variables:
- `ORCHESTRATOR_URL`: Base URL for the EFS orchestrator service
- `ADDSMOCK_URL`: Base URL for the mock services

## Test Data

Test data files are located in the `TestData/` folder and include:
- ZIP files for structure comparison testing
- Product-specific test datasets
- Exchange set validation samples
- Any changes in the actual exchange set zip file will need updates to the corresponding test data files.

Use `[CopyToOutputDirectory]` in the project file to ensure test data is available at runtime.

