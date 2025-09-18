# UKHO.ADDS.EFS.FunctionalTests

## Overview

This project contains functional tests for the Exchange Set Fulfilment Service (EFS). These tests validate the behavior of the EFS API and its components through end-to-end testing with a fully functional Aspire environment.

## Test Architecture

The test architecture uses a combination of patterns to optimize both resource usage and test execution speed:

### 1. Shared Aspire Environment

Tests share a single Aspire environment instance to avoid the significant overhead of starting/stopping the environment for each test class:

- `AspireResourceSingleton`: Implements a thread-safe singleton pattern to create and manage a single Aspire environment for all tests.
- `StartupFixture` & `StartupCollection`: Uses xUnit's collection fixtures to share this environment across test classes.

### 2. Parallel Test Execution

Tests are configured to run in parallel for faster execution:

- `Meziantou.Xunit.ParallelTestFramework`: Enables advanced parallel test execution.
- `xunit.runner.json`: Configures theory test pre-enumeration for parallel execution of individual theory test cases.
- `[DisableParallelization]` attribute: Used selectively to control which test methods run sequentially.

## Key Components

### AspireResourceSingleton

A thread-safe singleton that:
- Creates the Aspire distributed application only once
- Configures HTTP clients with standard resilience handlers
- Starts the application and waits for the orchestrator service
- Provides cleanup on disposal

### XUnit Configuration

The `xunit.runner.json` configuration enables pre-enumeration of theory tests:

## Usage

To create a new test class:

1. Add the `[Collection("Startup")]` attribute to your test class
2. Access the shared Aspire environment with `AspireResourceSingleton.App`
3. Use `[DisableParallelization]` on theory tests if test cases need to run sequentially

