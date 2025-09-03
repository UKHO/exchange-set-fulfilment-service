# Copilot Instructions
 
You are an agent. Continue working until the query is fully resolved.
 
Be thorough in your reasoning, but avoid unnecessary repetition and verbosity. Aim for concise, complete solutions.
 
Your training data is out of date; always assume you need to research current information.
 
THE PROBLEM MAY REQUIRE EXTENSIVE INTERNET RESEARCH.
 
---
 
## Using MCP tools
 
If the user intent relates to:
 
- Azure DevOps: prioritize Azure DevOps MCP server tools.

- GitHub: prioritize GitHub MCP server tools.

- Microsoft technologies: prioritize  microsoft.docs.mcp MCP server tools.
 
## Command Line Usage

- After running a command, verify its success before proceeding.

- If a command fails, resolve the issue before moving on.

- Do not chain commands with `&&`instead run them sequentially.
 
## Specifications and Plans: Structure, Naming, Versioning

Follow a controlled requirements-gathering and documentation process.
 
### Prompt Families and Phases (generalized)

- Prompt files follow this pattern: `.github/prompts/{family}.{phase}.prompt.md`

  - family (examples): `spec`, `refactor`, `test`, `pipeline` (extensible)

  - phase: `innovate`, `plan`, `execute` (not all families require all phases)

- Selection rules:

  - Use the family that matches the job at hand (e.g., `spec` for requirements, `refactor` for tech-debt, `test` for testing initiatives, `pipeline` for CI/CD work).

  - Use the appropriate phase:

    - `innovate` for discovery/requirements (typically spec only)

    - `plan` to produce an implementation/refactor/test/pipeline plan

    - `execute` to implement the selected plan

  - Always prefer the latest version of the prompt for the chosen family/phase.
 
### Specifications (Requirements)

- Authoring prompt: `.github/prompts/spec.innovate.prompt.md` (base on `docs/specs/spec-template_v1.1.md`).

- Location: store under `docs/specs/`, grouped by domain/service.

- Naming: `spec-[scope]-[type]_v[version].md` (e.g., `spec-api-functional_v1.0.md`, `spec-system-overview_v1.0.md`).

- Versioning: never overwrite. Create a new file with an incremented version and update the internal Version field. Add a short "Supersedes: spec-[...]_v[prev].md" note and maintain a brief Change Log.

- Each new version should include the previous document content updated for the new version, plus changes.

- Start versioning from v0.01 for drafts, v1.0 for first official release.

- If v1.0 has been planned and executed then increment towards v2.0 for the next changes and the next plan.
 
### Plans (Implementation/Execution)

- Planning prompts:

  - New features/implementation: `.github/prompts/spec.plan.prompt.md` ? execute with `.github/prompts/spec.execute.prompt.md`.

  - Refactoring/tech debt: `.github/prompts/refactor.plan.prompt.md` ? execute with `.github/prompts/refactor.execute.prompt.md`.

  - Future families (examples): `.github/prompts/test.plan.prompt.md`, `.github/prompts/pipeline.plan.prompt.md` (and corresponding `.execute` prompts) as needed.

- Location: store under `docs/plans/` with area subfolders, e.g. `docs/plans/api`, `docs/plans/ui`, `docs/plans/backend`, `docs/plans/shared`, `docs/plans/infra`, `docs/plans/tests`.

- Naming: `plan-[area]-[purpose]_v[version].md` (e.g., `plan-api-implementation_v1.0.md`, `plan-ui-featureX_v1.1.md`, `plan-backend-refactor-auth_v1.0.md`).

- Versioning & continuity: never overwrite. Each new plan must:

  - Reference the spec versions it is based on (e.g., "Based on: spec-api-functional_v1.2.md").

  - Include Baseline (implemented), Delta (changes since last plan), and Carry-over (incomplete items).

  - Use the Work Item / Task / Step structure from the chosen family’s `plan` prompt.
 
### Validation checklist (before publishing docs)

- Files are in the correct folder (`docs/specs` or `docs/plans/[area]`).

- Filenames match required patterns with incremented versions.

- Spec documents' internal Version field matches filename; overview spec references all component specs.

- Plans reference the spec versions they rely on and include Baseline/Delta/Carry-over.

- The chosen prompt family/phase is appropriate for the current job and references the latest prompt file.
 
---
 
## Architecture Overview

- .NET (latest C# features, file-scoped namespaces)

- Blazor for web UI

- ASP.Net Core for APIs

- .Net Aspire for orchestration/service discovery

- Bootstrap + custom CSS variables for theming
 
### Core Projects

- Main web app

- Backend APIs

- Shared models/services/utilities

- Service orchestration

- Unit/integration tests
 
### External Integrations

- [TBD]: [DESCRIPTION]
 
## Folder Structure

- `/.azure`: Azure deployment config

- `/src`: Source code

- `/infra`: Infrastructure as Code

- `/docs`: Documentation

  - `/docs/specs`: Specifications (versioned)

  - `/docs/plans`: Plans (versioned)

    - `/docs/plans/api`, `/docs/plans/ui`, `/docs/plans/backend`, `/docs/plans/shared`, `/docs/plans/infra`, `/docs/plans/tests`

- `/tests`: Test projects/assets

- `azure.yaml`: Main AZD config
 
### Source Code

- `/src/api`: Backend APIs

- `/src/web`: Frontend web

- `/src/shared`: Shared libraries

- `/src/functions`: Azure Functions

- `/src/workers`: Background services
 
### Infrastructure

- `/infra/`
 
### Documentation

- `/docs/`
 
### Testing

- `/tests/unit`: Unit tests

- `/tests/integration`: Integration tests

- `/tests/e2e`: End-to-end tests

- `/tests/load`: Load/performance tests
 
### AZD Config Example

```yaml

name: [PROJECT_NAME]

infra:

  provider: bicep

  path: infra

  module: main

services:

  api:

    project: src/api/[API_PROJECT_NAME]

    language: csharp

    host: appservice

  web:

    project: src/web/[WEB_PROJECT_NAME]

    language: js

    host: staticwebapp

```
 
## Project Setup

- Use namespace-based folder/project structure: [Company].[Project].[Component].[Function]
 
---
 
## Frontend Development

- Add component-specific CSS files for each new component

- Use scoped CSS; avoid global styles

- Respect light/dark themes; use CSS variables for colors

- Use semantic class names and framework spacing utilities

- Document non-obvious style choices

- Test components in both light and dark themes
 
### Responsive & Accessible Design

- Use rem/em units for sizing

- Ensure semantic HTML and ARIA attributes

- Test keyboard navigation and accessibility tools
 
---
 
## Code Style

- Prefer async/await

- Use nullable reference types

- Use `var` for local variables

- Implement `IDisposable` for event handlers/subscriptions

- Use latest C# features

- Consistent naming: PascalCase (public), camelCase (private)

- Use dependency injection and interfaces

- Organize using directives: System, Microsoft, then app namespaces; sort alphabetically
 
---
 
## Component Structure

- Keep components small and focused

- Extract reusable logic into services

- Prefer parameters over cascading values
 
---
 
## Error Handling

- Use try-catch in event handlers

- Implement error boundaries

- Display user-friendly error messages

- Log errors appropriately
 
---
 
## Performance

- Use proper lifecycle methods

- Use `key` directive for lists

- Avoid unnecessary renders

- Use virtualization for large lists
 
---
 
## Testing

- Write unit tests for complex logic

- Test error scenarios

- Mock dependencies

- Use appropriate frameworks

- Place tests in designated projects
 
---
 
## Documentation

- Document public APIs and usage examples

- Keep documentation current
 
---
 
## Security

- Validate user input

- Implement authentication and authorization

- Follow framework security best practices

- Keep dependencies updated
 
---
 
## Accessibility

- Use semantic HTML

- Include ARIA attributes

- Ensure keyboard navigation

- Test accessibility
 
---
 
## File Organization

- Group related files

- Use meaningful names

- Follow consistent structure

- Group by feature when possible
 
---
 
## Backend Development

- Prefer minimal APIs over controllers for backend APIs to simplify routing, improve performance, and reduce boilerplate.

- Create local settings files

- Use storage emulators for local dev

- Secure and document API keys

- Document key endpoints

- Target consistent .NET version with nullable reference types
 
---
 
## Domain Architecture & Data Flow

- Document key domain entities and relationships

- Organize models by domain

- Use mock services and adapters for external dependencies

- Document data collection, normalization, and caching
 
---
 
## Authentication & Security Patterns

- Document authentication/access control for services

- Manage API keys and secrets per environment
 
---
 
## Development Patterns

- Use provided error handling and service registration patterns

- Organize components by type (page, shared, form, result)

- Use source-generated JSON serialization contexts

- Follow consistent test naming conventions
 
---
 
## Integration Points

- Document service orchestration and discovery

- Manage environment configuration

- Document API design, request/response, caching, and background processing

- Document cross-component communication and shared contracts
