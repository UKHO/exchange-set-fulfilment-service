# Test Plan Prompt (Unit Tests)

You are a highly skilled .NET test engineer. Produce a focused, actionable Unit Test Plan for the specified scope. The Unit Test Plan should focus on
unit tests only, not integration or end-to-end tests, and must be focused on enabling high code coverage with clear, deterministic, and maintainable tests.
Follow the repository’s documentation conventions and keep the plan concise, unambiguous, and ready to execute. Use the template defined below.
Only generate the plan at this stage in Markdown format. Do not write any code or tests yet.

Context
- Tech stack: .NET 9, .NET Standard 2.0; C#, nullable enabled; mix of libraries, worker services (BackgroundService), and Blazor.
- Test projects live under tests/ and test/ folders using standard naming (e.g., *.UnitTests, *.EndToEndTests, *.FunctionalTests).
- Plans must reference specs by version and follow Baseline/Delta/Carry-over.
- Framework standardization: Use xUnit for all new/updated unit test projects. Use Shouldly for assertions. Do not use FluentAssertions.
- Class-to-test rule (STRICT): For every production class, there MUST be exactly one corresponding test class placed in an analogous folder/namespace in the mapped UnitTests project. File naming: {ClassName}Tests.cs.

Repository-wide bootstrap (high level)
When initiating high-level planning for unit tests across the repository, include this section in the plan before section 1.

0) Repository Test Inventory & Bootstrap
- Inventory
  - Enumerate all non-test projects (*.csproj) under src/, configuration/, and similar roots.
  - Exclude existing test projects (paths containing test/ or tests/, or names ending with .Tests/.UnitTests/.FunctionalTests/.EndToEndTests).
  - Classify by type: library, Blazor UI, worker/background service, console/tooling, analyzers, infrastructure helpers.
- Mapping and naming
  - For each non-test project, determine the corresponding unit test project name: [ProjectName].UnitTests.
  - Default placement: test/[ProjectName].UnitTests or tests/[ProjectName].UnitTests (match repo convention).
- Create missing unit test projects (xUnit + Shouldly)
  - New test project settings: Target net9.0; <IsPackable>false</IsPackable>; <Nullable>enable</Nullable>.
  - Add packages: xunit, xunit.runner.visualstudio (PrivateAssets=all), Microsoft.NET.Test.Sdk, Shouldly, coverlet.collector (PrivateAssets=all).
  - Add a ProjectReference to the source project.
  - Add a starter Tests.cs with an example test using Shouldly (no FluentAssertions).
- One-to-one class/test enforcement
  - For each source file src/{Project}/{RelPath}/{ClassName}.cs, create test/{Project}.UnitTests/{RelPath}/{ClassName}Tests.cs.
  - Mirror namespaces: Source namespace Foo.Bar -> Test namespace Foo.Bar (rooted under [ProjectName].UnitTests). Keep only the relative sub-namespace beyond the project root.
  - No combined test classes: do not group multiple production classes into a single test class.
- Patterns by project type
  - Library/domain: pure unit tests; mock external I/O and time; prefer constructor DI and pure functions.
  - Worker/BackgroundService: test start/stop, cancellation, delays/retries; control time; inject clocks; use CancellationTokenSource with short timeouts.
  - Blazor components: unit tests via bUnit with xUnit; keep assertions with Shouldly; isolate JS interop; prefer rendering small components.
  - HTTP clients/integrations: use HttpMessageHandler test doubles; no live network; verify serialization and error handling.
- Output of this section
  - Table mapping each source class -> test class (existing/missing), with actions to create where missing.
  - Add corresponding Work Items in section 9 for each missing test class and/or project.

Inputs you will receive (assume or ask to gather as needed)
- Scope under test (component, namespace, class/methods)
- Related spec(s) and version(s)
- Acceptance criteria/user stories and edge/corner cases
- Existing tests and known gaps/flaky areas
- External dependencies to mock/stub (HTTP, storage, time, random, environment)

Output format
Create a markdown plan named plan-tests-[scope]_v[version].md under docs/plans/tests. The plan must include the sections below in order.

1) Plan Metadata (Fill-in)
- Title: Unit Test Plan — [Scope]
- Version: v[version]
- Date: [YYYY-MM-DD]
- Authors/Reviewers: [Names]
- Based on: [spec-..._vX.Y.md]
- Related Plans: [links]

2) Objectives and Non-Objectives (Scope Only)
- List target namespaces/classes to cover in this plan.
- List exclusions (if any).

3) Baseline / Delta / Carry-over (Actions)
- Baseline: List existing test classes and gaps.
- Delta: List new test classes/files to add.
- Carry-over: List pending items from prior plans.

4) Test Strategy (Execution Rules)
- Use xUnit + Shouldly only; forbid FluentAssertions.
- Enforce one test class per production class; mirror folders/namespaces; name files {ClassName}Tests.cs.
- Use fakes by default; mocks only where necessary.
- Make tests deterministic (clock/guid/env injection).
- Naming: MethodName_Should_Expected_When_Condition; AAA/BDD structure.

5) Test Design and Coverage Matrix (Per-Method Checklist)
- Some classes are internal in scope. You must include internal classes in the plan if they are declared in the project you are given to generate tests for. 
- Provide a table/bullets per class mapping methods -> test cases.
- For EACH PUBLIC METHOD include:
  1) Happy path example(s). 
  2) Boundaries (min/max/empty/null)
  3) Error cases with exact exception type and message
  4) Async/cancellation behavior
  5) Idempotency/reentrancy (if applicable)
  6) Concurrency safety (if applicable)
  7) Culture/formatting variants (if applicable)
  8) Feature flag or configuration branches (if applicable)
  9) Float assertions with tolerance
  10) Serialization round-trip (if applicable)
  11) Property-based tests for key invariants

6) Mocking/Stubbing Plan (Actions)
- List collaborators to fake/mock.
- List interfaces/adapters to introduce.
- List test doubles to implement/reuse.

7) Fixtures and Test Utilities (Actions)
- List shared fixtures and lifetimes.
- List builders/factories/helpers to add.

8) File/Project Placement (Actions)
- Name target test project(s) and folder layout.
- List new files to create with full relative paths.

9) Work Items / Tasks / Steps (Checklist)
- Create missing test classes from mapping.
- Add/verify packages: xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk, Shouldly, coverlet.collector.
- Add builders/utilities and shared fixtures.
- Remove any FluentAssertions usages.
- Add coverage report generation in CI.

10) Quality Gates and Tooling (Settings)
- Set coverage thresholds: [line%]/[branch%]; fail CI if below.
- Enable coverlet + reportgenerator in CI.
- Define flakiness policy (timeouts, retries disabled, quarantine tag if needed).

11) Risks and Mitigations (Minimal)
- List technical blockers and immediate mitigations.

12) Definition of Done (Binary)
- All mapped test classes exist and compile.
- All per-method checklist items implemented or marked N/A.
- CI green with coverage thresholds met.
- No FluentAssertions references remain.

Appendix A: Existing Gaps and References
- Link to existing tests, notable gaps, and relevant issues/PRs

Authoring Guidance (for the generator)
- Prefer precise bullets over prose
- Keep sections short and actionable
- Use code snippets only when necessary to remove ambiguity
- If information is missing, propose assumptions explicitly and proceed

Template (STRICT) — Test Planning and Class Mapping

```
# Unit Test Plan — [Scope]

Version: v[version]
Date: [YYYY-MM-DD]
Authors/Reviewers: [Names]
Based on: [spec-..._vX.Y.md]
Related Plans: [links]

0) Inventory & Class/Test Mapping

| Source Class (path) | Test Class (path) | Exists? | Action |
| - | - | - | - |
| src/[Project]/[RelPath]/Foo.cs | test/[Project].UnitTests/[RelPath]/FooTests.cs | [Yes/No] | [Create/Audit/Fix]

Notes:
- Enforce 1:1 mapping. Do not combine multiple production classes in one test class.
- Mirror folder/namespace structure beyond project root.

1) Plan Metadata (Fill-in)
- Title, Version, Date, Authors, Based on, Related Plans

2) Scope
- Targets: [namespaces/classes]
- Exclusions: [if any]

3) Baseline/Delta/Carry-over
- Baseline: [existing tests/gaps]
- Delta: [new files/classes]
- Carry-over: [items]

4) Strategy (Execution Rules)
- xUnit + Shouldly only; no FluentAssertions
- One test class per production class; mirrored paths; {ClassName}Tests.cs
- Deterministic tests (clock/guid/env)
- Prefer fakes; AAA/BDD; naming as specified

5) Per-Class Design

## [Namespace].[ClassName]
- Test File: test/[Project].UnitTests/[RelPath]/[ClassName]Tests.cs
- Class Type: [public/internal]
- Public API:
  - [methods/ctors]
- Dependencies to fake:
  - [IClock, IGuidProvider, ...]
- Coverage Checklist per method:
  - [MethodName]
    1) Happy path
    2) Boundaries (min/max/empty/null)
    3) Error type + message
    4) Async/cancellation
    5) Idempotency/reentrancy (if applicable)
    6) Concurrency safety (if applicable)
    7) Culture/formatting (if applicable)
    8) Feature flags/config (if applicable)
    9) Float tolerance
    10) Serialization round-trip (if applicable)
    11) Property-based invariants
- Files to create:
  - test/[Project].UnitTests/[RelPath]/[ClassName]Tests.cs

## [Repeat per class]

6) Fixtures & Utilities
- Fixtures: [list]
- Builders/Helpers: [list]

7) Placement Summary
- Test Project: test/[Project].UnitTests
- New Files: [list]

8) Work Items (Checklist)
- [ ] Create missing test classes
- [ ] Add fixtures/builders
- [ ] Configure packages (xUnit, Shouldly, coverlet)
- [ ] Remove FluentAssertions
- [ ] Enable coverage in CI

9) Quality Gates
- Coverage: [line%]/[branch%]
- Flakiness policy: [notes]

10) Risks
- [list]

11) Done Criteria
- [binary checklist]
