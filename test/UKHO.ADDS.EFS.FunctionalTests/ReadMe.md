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

- `AspireResourceSingleton`: Implements a thread-safe singleton pattern to create and manage a single Aspire environment for all tests. Detects if running in CI/CD pipeline and configures HTTP clients accordingly.
- `StartupFixture` & `StartupCollection`: Uses xUnit's collection fixtures to share this environment across test classes via `[Collection("Startup Collection")]`.

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

## Key Components

### Core Service Classes

#### AspireResourceSingleton
A thread-safe singleton that:
- Creates and manages the Aspire distributed application only once
- Detects execution environment (local vs CI/CD pipeline)
- Configures HTTP clients with appropriate base addresses
- Starts the application and waits for the orchestrator service
- Provides cleanup on disposal

#### OrchestratorCommands
Provides helper methods for interacting with the EFS orchestrator:
- `SubmitJobAsync()`: Submits jobs to various API endpoints
- `WaitForJobCompletionAsync()`: Monitors job status with timeout handling
- `GetBuildStatusAsync()`: Retrieves detailed build status information
- `ProductNamesInCustomAssemblyPipelineSubmitJobAsync()`: Specialized job submission for product names

#### ApiResponseAssertions
Comprehensive response validation utilities:
- `checkJobCompletionStatus()`: Validates job completion and build state
- `checkBuildStatus()`: Validates builder steps and exit codes
- Detailed logging of response content and validation failures

#### ZipStructureComparer
Exchange set validation tools:
- `DownloadExchangeSetAsZipAsync()`: Downloads generated exchange sets from mock service
- `CompareZipFilesExactMatch()`: Compares ZIP file structures and validates product inclusion
- Directory structure validation for exchange set compliance

#### BuilderStepsAnalyzer
Typed analysis of build processes:
- `BuilderResponse` and `BuilderStep` models for strongly-typed JSON parsing
- Performance metrics calculation (total elapsed time)
- Build step sequence and status tracking

#### TestBase
Abstract base class providing:
- Shared access to `StartupFixture` and `ITestOutputHelper`
- Common initialization patterns for test classes

### Test Classes

#### ProductNamesFunctionalTests
Tests the `/v2/exchangeSet/s100/productNames` endpoint:
- Input validation with valid and invalid product names
- Callback URI handling (with and without callbacks)
- Exchange set generation and structure validation

#### ProductVersionsFunctionalTests  
Tests the `/v2/exchangeSet/s100/productVersions` endpoint:
- Product version-specific exchange set requests
- Edition and update number validation
- JSON payload validation

#### UpdateSinceFunctionalTests
Tests the `/v2/exchangeSet/s100/updatesSince` endpoint:
- Date/time validation (ISO 8601 format)
- Product identifier filtering
- Incremental update scenarios

#### FunctionalTests
Core job management tests:
- General job submission and monitoring
- Build process validation
- End-to-end exchange set generation workflows

### XUnit Configuration

#### AssemblySetup
Configures parallel test execution:
- `[CollectionDefinition("Startup Collection")]` for shared fixtures
- `[EnableParallelization]` for optimized test execution
- Collection behavior configuration for class-level parallelization

#### xunit.runner.json
JSON configuration enabling:
- Theory test pre-enumeration for parallel execution
- Test collection and assembly parallelization
- Advanced xUnit runner options

## Usage

### Creating a New Test Class

To create a new functional test class:

1. **Add collection attribute**: Use `[Collection("Startup Collection")]` to access the shared Aspire environment
2. **Enable parallelization**: Add `[EnableParallelization]` for optimal test execution
3. **Inherit from TestBase**: Extend `TestBase` for common functionality
4. **Constructor pattern**: Accept `StartupFixture` and `ITestOutputHelper` parameters

```csharp
[Collection("Startup Collection")]
[EnableParallelization]
public class MyNewFunctionalTests : TestBase
{
    public MyNewFunctionalTests(StartupFixture startup, ITestOutputHelper output) 
        : base(startup, output)
    {
        // Test-specific initialization
    }
}
```

### Test Method Patterns

#### Theory Tests with Retry
Use `[RetryTheory]` for theory tests that may experience transient failures:

```csharp
[RetryTheory(maxRetries: 1, delayBetweenRetriesMs: 5000)]
[DisableParallelization] // Optional: for sequential theory test case execution
[InlineData("param1", "param2", HttpStatusCode.Accepted, "")]
public async Task MyTestMethod(string param1, string param2, HttpStatusCode expectedStatus, string expectedError)
{
    // Test implementation
}
```

#### Job Submission and Monitoring
Follow the established pattern for testing API endpoints:

```csharp
// 1. Submit job
var response = await OrchestratorCommands.SubmitJobAsync(requestId, payload, endpoint);

// 2. Wait for completion
var jobStatusResponse = await OrchestratorCommands.WaitForJobCompletionAsync(jobId);

// 3. Validate response
var assertions = new ApiResponseAssertions(_output);
await assertions.checkJobCompletionStatus(jobStatusResponse);

// 4. Check build details (optional)
var buildResponse = await OrchestratorCommands.GetBuildStatusAsync(jobId);
await assertions.checkBuildStatus(buildResponse);
```

### Environment Variables (CI/CD)

When running in CI/CD pipelines, set these environment variables:
- `ORCHESTRATOR_URL`: Base URL for the EFS orchestrator service
- `ADDSMOCK_URL`: Base URL for the mock services

### Test Data

Test data files are located in the `TestData/` folder and include:
- ZIP files for structure comparison testing
- Product-specific test datasets
- Exchange set validation samples

Use `[CopyToOutputDirectory]` in the project file to ensure test data is available at runtime.

