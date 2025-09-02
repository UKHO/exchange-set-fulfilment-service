# Test Execute Prompt (Unit Tests)

Execute one step at a time and ask me to continue or stop.

Purpose
- Execute a Unit Test Plan produced by test.plan.prompt.md to generate and standardize unit tests in this repository.
- Do not change production behavior; only introduce minimal testability seams if strictly required (e.g., IClock) and called out by the plan.

Context and Guardrails
- Tech: .NET 9 / .NET Standard 2.0. Use xUnit + Shouldly. Do not use FluentAssertions.
- Use the GivenWhenThenTest base class for tests that fit the Given/When/Then pattern as detailed below.
- Strict rule: One test class per production class, mirrored folder/namespace. File name: {ClassName}Tests.cs.
- Prefer fakes over mocks; no live I/O or network.
- Blazor components: use bUnit with xUnit (assert with Shouldly). Worker services: test BackgroundService lifecycle.

Outputs
- New/updated test project(s) and files as specified by the plan.
- Updated test csproj(s) to standard packages.
- Passing build/test run and coverage artifacts.

Pre-work (required)
1) Read the Unit Test Plan end-to-end. Extract:
   - Target classes/methods, dependencies to fake, and checklist per method
   - File/project placement and naming
   - Quality gates (coverage thresholds, flakiness policy)
2) Read and understand the documentation listed in the References section deeply. Capture key idioms (pipelines, results, branching/short-circuiting, structured result properties) and preferred assertion patterns for use in tests.
3) Inspect existing tests in the repository for the same area to align naming and structure where appropriate (no FluentAssertions; prefer Shouldly).

References
- UKHO.ADDS.Infrastructure.Pipelines
  - Namespace: UKHO.ADDS.Infrastructure.Pipelines
  - Documentation: https://github.com/UKHO/UKHO.ADDS.Infrastructure/wiki/Pipelines
  - Testing guidance:
    - Identify pipeline step abstractions, composition, ordering, and short-circuit semantics.
    - Verify that failures stop further processing as specified and that branching logic is exercised.
    - Assert interactions via injected dependencies and observable effects; avoid relying on log text.
    - Exercise and assert cancellation propagation across steps.
- UKHO.ADDS.Infrastructure.Results
  - Namespace: UKHO.ADDS.Infrastructure.Results
  - Documentation: https://github.com/UKHO/UKHO.ADDS.Infrastructure/wiki/Results
  - Testing guidance:
    - Prefer asserting result state (success/failure), codes, and structured data over raw message text.
    - Validate mapping/conversion helpers, aggregation/combination semantics, and deconstruction patterns.
    - When exceptions are converted to results, assert the correct mapping and properties; do not assert exact message strings.

Exclusions
- Do not test that Exception messages are verbatim. Assert exception type and relevant structured properties/codes instead.
- Do not use real external services or live network calls.
- Do not add integration or end-to-end tests.

High-level Workflow
1) Read and validate plan
- Confirm the plan follows the strict template.
- Extract: target scope, mapping table, work items, coverage gates.

2) Determine target test project(s)
- For scope src/[Project], expected test project: test/[Project].UnitTests.
- If missing, create a new test project with:
  - TargetFramework net9.0; IsPackable=false; Nullable=enable.
  - Packages: xunit, xunit.runner.visualstudio (PrivateAssets=all), Microsoft.NET.Test.Sdk, Shouldly, coverlet.collector (PrivateAssets=all).
  - For Blazor tests: add bunit when the source project is Blazor.

3) Standardize existing test csproj(s)
- Remove packages/usings for: NUnit, NUnit3TestAdapter, NUnit.Analyzers, and FluentAssertions.
- Remove <Using Include="NUnit.Framework" /> entries from csproj files and any global using statements referencing NUnit.*.
- Remove cross-test ProjectReference(s).
- Ensure required packages present as above (add FsCheck.Xunit only if the plan needs property-based tests).

3a) Remove NUnit tests and migrate (mandatory)
- Coverage pre-check before migration:
  - For each NUnit test file, compare the behaviors it covers against the approved plan’s mapping and existing xUnit/Shouldly tests.
  - If the NUnit test’s behaviors are already covered by the plan/tests, delete the NUnit file instead of migrating it.
  - If not covered, migrate the NUnit test following the rules below.
- Identify NUnit usage across the repository:
  - Namespaces: using NUnit.Framework; using NUnit; aliases to NUnit Assert/Constraints
  - Attributes: [Test], [TestCase], [TestCaseSource], [Values], [Range], [SetUp], [TearDown], [OneTimeSetUp], [OneTimeTearDown], [Category], [Ignore], [TestFixture]
  - Assertions: Assert.That, Assert.AreEqual/AreNotEqual, Assert.IsTrue/IsFalse, Assert.Throws, CollectionAssert, StringAssert
- Migrate to xUnit + Shouldly:
  - [Test] -> [Fact]
  - [TestCase]/[TestCaseSource]/[Values]/[Range] -> [Theory] + [InlineData]/[MemberData]
  - [SetUp]/[TearDown] -> constructor/IDisposable or xUnit fixtures (IClassFixture/CollectionFixture)
  - [OneTimeSetUp]/[OneTimeTearDown] -> class fixture or collection fixture
  - [Category("X")] -> [Trait("Category", "X")] (optional; keep minimal)
  - [Ignore("reason")] -> add Skip = "reason" on [Fact]/[Theory] only when unavoidable
  - NUnit Assert -> Shouldly equivalents:
    - Assert.AreEqual(expected, actual) -> actual.ShouldBe(expected)
    - Assert.AreNotEqual(notExpected, actual) -> actual.ShouldNotBe(notExpected)
    - Assert.IsTrue(cond) -> cond.ShouldBeTrue()
    - Assert.IsFalse(cond) -> cond.ShouldBeFalse()
    - Assert.Throws<T>(() => ...) -> Should.Throw<T>(() => ...)
    - StringAssert.Contains(sub, str) -> str.ShouldContain(sub)
    - CollectionAssert.Contains(collection, item) -> collection.ShouldContain(item)
- Prefer fakes over mocks when regenerating or migrating tests:
  - Replace mocking frameworks with simple, explicit fakes/test doubles where practical.
  - Only retain mocking packages if still used elsewhere; otherwise remove them from the test csproj.
- Delete remaining NUnit usings and any NUnit-specific base classes/helpers.
- After migration/removal, run a repository-wide search to ensure no NUnit references remain in code or project files.

4) Enforce one-to-one class/test mapping
- For each row in the mapping table:
  - Create directories under test project mirroring the source path beyond the project root.
  - Create/rename the file to {ClassName}Tests.cs if missing or misnamed.
  - Set namespace to match the source relative namespace, rooted under the test project default namespace.

5) Scaffold test class contents
- Add file header usings: Shouldly; Xunit; any required namespaces for the class under test.
- Create a public sealed test class named {ClassName}Tests.
- Enumerate public methods/ctors of the production class (from plan details or by reading the class) and create placeholders/tests following the coverage checklist:
  1) Happy path example(s)
  2) Boundaries (min/max/empty/null)
  3) Error cases with exact exception type and, if applicable, codes/structured properties; do not assert verbatim message text
  4) Async/cancellation behavior
  5) Idempotency/reentrancy (if applicable)
  6) Concurrency safety (if applicable)
  7) Culture/formatting variants (if applicable)
  8) Feature flag or configuration branches (if applicable)
  9) Float assertions with tolerance (use epsilon; e.g., Math.Abs(actual - expected).ShouldBeLessThanOrEqualTo(epsilon))
  10) Serialization round-trip (if applicable) using System.Text.Json
  11) Property-based tests for key invariants (add FsCheck.Xunit package if used)
- Prefer [Fact] for simple cases and [Theory]/[InlineData]/[MemberData] for boundaries and equivalence classes.

6) Patterns by project type
- Value objects (e.g., Vogen): test .From, .TryFrom, equality, ToString, and System.Text.Json round-trips.
- BackgroundService: verify StartAsync/ExecuteAsync/StopAsync behavior with CancellationToken; control time with fakes.
- Blazor components: render with bUnit TestContext, assert state/parameters/callbacks with Shouldly.
- HTTP/serialization: use HttpMessageHandler fakes; assert status/error handling and contract round-trips.
- Infrastructure results/pipelines (see References): assert result states, codes, and structured properties; validate pipeline step ordering, branching, and short-circuit behavior; verify cancellation propagation; do not assert raw message text.

7) Shared fixtures/utilities
- Add deterministic helpers: TestClock, TestGuidProvider; builders for common domain objects.
- Use xUnit collection/class fixtures when sharing expensive setup across tests.

8) Apply plan work items
- Execute the plan’s checklist tasks (scaffold files, move/rename, package cleanup).

9) Validate locally
- dotnet restore
- dotnet build -c Debug
- dotnet test -c Debug --collect:"XPlat Code Coverage" (ensure coverlet.collector is present)
- If coverage gates are specified, generate reports via reportgenerator (if configured) and assert thresholds.

10) Finalization
- Ensure no FluentAssertions or NUnit usings remain and no NUnit packages (NUnit, NUnit3TestAdapter, NUnit.Analyzers) in any csproj.
- For migrated tests, ensure mocking frameworks were replaced with simple fakes where practical; remove unused mocking packages from the test csproj.
- Ensure all mapped files exist and compile.
- Prepare a concise commit message summarizing created files and standardization.

Failure Handling
- If a step fails, fix the issue before proceeding (missing package, broken compile, mismatched namespaces).
- If a production change is needed for testability, prefer minimal, non-functional seams and record the change.

Template for scaffolding a test file (conceptual)
- Namespace: [TestProjectNamespace].[RelativeNamespace]
- Class: public sealed class {ClassName}Tests
- Usings: Xunit; Shouldly; [Source namespaces]; System.Text.Json (if needed)
- Methods: One region per public method with tests covering the checklist above.

Authoring Guidance
- Keep tests deterministic and isolated.
- Prefer precise assertions; avoid implementation-coupled checks.
- Keep tests small; one behavior per test.

We have a base test class in the project UKHO.ADDS.EFS.UnitTests called GivenWhenThenTest. Tests should follow the Given/When/Then pattern and use this base class to generate test code unless the test does not fit this pattern. Here is an example using GivenWhenThenTest.

using Xunit;
using Xunit.Abstractions;

namespace UKHO.ADDS.EFS.UnitTests.Steps.Tests
{
    public class StepTests : GivenWhenThenTest
    {
        public StepTests(ITestOutputHelper output) : base(output) { }

        [Fact]
        public async Task It_Works_With_Sync_Arrange_And_Async_Act_And_Sync_Assert()
        {
            await Given("I create the SUT", () => new Sut())
                .When("I do something async", async sut => await sut.DoSomethingAsync())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }

        [Fact]
        public async Task It_Works_With_All_Async_Steps()
        {
            await Given("I create SUT async", async () =>
                {
                    await Task.Delay(10);
                    return new Sut();
                })
                .When("I do something async", async sut => await sut.DoSomethingAsync())
                .Then("It should be ready", async sut =>
                {
                    await Task.Delay(10);
                    Assert.True(sut.IsReady);
                });
        }

        [Fact]
        public async Task It_Works_With_All_Sync_Steps()
        {
            await Given("I create the SUT", () => new Sut())
                .When("I do something", sut => sut.DoSomething())
                .Then("It should be ready", sut => Assert.True(sut.IsReady));
        }
    }
}

You should find descriptive names for the steps that describe exactly what the test does.
