# GitHub Copilot Guidance for Exchange Set Fulfilment Service (EFS)

This document provides guidance for GitHub Copilot when assisting with the Exchange Set Fulfilment Service (EFS) project. Copilot should follow these guidelines when suggesting code, answering questions, or providing assistance.

## Project Overview

The Exchange Set Fulfilment Service (EFS) is a .NET-based application that processes and fulfills exchange set requests. The project uses:
- C# and .NET 8/ASP.NET Core
- Azure Storage (Blob, Tables, Queues)
- Docker containerization
- Microservice architecture with Aspire orchestration

## Coding Standards

### General C# Coding Guidelines

Follow Microsoft's C# coding conventions:

1. **Use consistent naming conventions:**
   - Use PascalCase for class names, method names, and public members
   - Use camelCase for local variables and method parameters
   - Prefix interface names with 'I'
   - Avoid Hungarian notation

2. **Code Organization:**
   - Place using directives outside the namespace declarations
   - Use file-scoped block namespace declarations: `namespace UKHO.ADDS.EFS.Domain;`
   - Organize members by accessibility (public, then internal, then private)

3. **Code Style:**
   - Use four spaces for indentation, not tabs
   - Use Allman style for braces (on new lines)
   - Limit line length to a reasonable number of characters
   - Add at least one blank line between method definitions

4. **Language Features:**
   - Use string interpolation instead of string concatenation
   - Use collection expressions where appropriate: `string[] vowels = ["a", "e", "i", "o", "u"];`
   - Use implicit typing (var) where possible but maintain clarity
   - Use async/await for asynchronous operations
   - Use using statements for resources that implement IDisposable
   - Use LINQ when it makes collection manipulation clearer, with proper indentation
   - Use null conditional operators (?.) and null coalescing operators (??) where appropriate

5. **Error Handling:**
   - Use specific exception types instead of general ones
   - Only catch exceptions that can be properly handled
   - Include meaningful error messages
   - Use the try-catch statement for most exception handling
   - Avoid empty catch blocks

### UKHO-Specific Standards

1. **Structure and Naming:**
   - Follow the existing project structure and naming conventions
   - Use the UKHO.ADDS.EFS namespace prefix for all new code
   - Name domain entities descriptively based on their purpose

2. **Testing:**
   - Write unit tests for all new functionality
   - Follow the existing test structure (using xUnit)
   - Test both success and failure paths
   - Use meaningful test names that describe the scenario being tested

3. **Documentation:**
   - Use XML comments for public APIs
   - Include summaries, param descriptions, and returns
   - Document exceptions that might be thrown
   - Comment complex algorithms or business logic

## Security Standards

### Microsoft Security Practices

1. **Input Validation:**
   - Validate all inputs, especially those from external sources
   - Use parameterized queries or ORMs to prevent SQL injection
   - Sanitize inputs to prevent XSS attacks

2. **Authentication and Authorization:**
   - Use proper authentication mechanisms
   - Implement proper authorization checks
   - Never hardcode credentials or secrets

3. **Data Protection:**
   - Use encryption for sensitive data
   - Use secure communication protocols (HTTPS)
   - Follow the principle of least privilege

### UKHO Security Policies

1. **Secret Management:**
   - Never include secrets in code or configuration files
   - Use Azure Key Vault for storing and accessing secrets
   - Use dependency injection for configuration

2. **Secure Development:**
   - Follow the UKHO Secure Development policy
   - Conduct threat modeling during development
   - Report security vulnerabilities to UKHO-ITSO@gov.co.uk
   - Use logging for security events and errors
   - Include appropriate exception handling

3. **Code Review:**
   - All security-related code should be reviewed
   - Check for hardcoded credentials, keys, or other secrets
   - Verify proper input validation
   - Ensure secure configuration

## Best Practices

1. **Cloud Services:**
   - Follow Azure best practices for storage services (Blob, Queue, Table)
   - Use appropriate storage options based on data characteristics
   - Implement proper error handling for cloud service operations

2. **API Design:**
   - Design RESTful APIs following standard conventions
   - Use appropriate HTTP methods and status codes
   - Include proper validation and error responses
   - Use versioning for APIs

3. **Microservices:**
   - Keep services focused on specific business capabilities
   - Use proper communication patterns between services
   - Implement resilience patterns (retry, circuit breaker)
   - Use distributed logging and tracing

4. **Technical Debt:**
   - Actively identify and address technical debt
   - Follow the UKHO Technical Debt guidance
   - Document known issues and planned improvements

## Contribution Guidelines

1. **Process:**
   - For minor improvements, submit pull requests
   - For significant changes, open an issue for discussion first
   - Follow the process outlined in CONTRIBUTING.md

2. **Code Review:**
   - All code must be reviewed before merging
   - Address all review comments before merging
   - Ensure tests pass and code meets quality standards

3. **Open Source:**
   - Follow UKHO's Open Source Contribution Policy
   - Include appropriate license information
   - Respect third-party licenses and attributions

## Performance Considerations

1. **Database and Storage:**
   - Use asynchronous operations for all I/O bound operations
   - Implement efficient querying with appropriate indexes
   - Use pagination for large result sets
   - Consider data partitioning strategies for Azure Tables and Blobs

2. **Memory Management:**
   - Be cautious with large in-memory collections
   - Dispose of resources properly using `IDisposable` or `using` statements
   - Consider using memory-efficient data structures for large datasets
   - Use string builders for string concatenation operations that involve loops

3. **Processing:**
   - Use parallel processing for CPU-bound operations when appropriate
   - Consider using background services for long-running operations
   - Use caching strategies where appropriate (in-memory, distributed)
   - Implement proper cancellation token support for long-running operations

4. **Logging and Telemetry:**
   - Follow appropriate logging levels
   - Avoid excessive logging in hot paths
   - Be cautious when logging the 'happy path' to avoid performance overhead
   - Use structured logging for better analysis
   - Implement proper application insights instrumentation

## Dependency Management

1. **NuGet Packages:**
   - Use explicit versioning for packages to ensure reproducibility
   - Keep dependencies up to date, especially for security fixes
   - Use internal UKHO package sources when appropriate
   - Prefer official, well-maintained packages with active communities

2. **Package Organization:**
   - Group related packages in Directory.Build.props when appropriate
   - Use centralized package version management
   - Document any non-standard or custom packages
   - Regularly review for deprecated or vulnerable packages

3. **Dependency Injection:**
   - Follow the dependency injection pattern throughout the application
   - Register services with appropriate lifetimes (singleton, scoped, transient)
   - Use constructor injection over property injection
   - Avoid service locator pattern

## CI/CD Integration

1. **Build Pipeline:**
   - Ensure all code builds cleanly in the CI pipeline before merging
   - Pay attention to compiler warnings and static analysis results
   - Follow the established branching strategy
   - Use feature branches for development

2. **Testing:**
   - All tests must pass in the pipeline before merging
   - Add appropriate test coverage for new code
   - Don't disable or ignore tests without proper justification
   - Include integration tests for critical paths

3. **Deployment:**
   - Be aware of the deployment stages (dev, vNextIAT, vNextE2E, IAT, PreProd, Production)
   - Understand the rollback procedures
   - Test deployment-specific configurations
   - Review deployment logs after changes

## Accessibility Standards

1. **General Principles:**
   - Follow WCAG 2.1 guidelines where applicable
   - Ensure proper color contrast for text and UI elements
   - Provide alternative text for images and non-text elements
   - Ensure keyboard navigability for web interfaces

2. **Documentation:**
   - Provide accessible documentation in standard formats
   - Use descriptive names for files and resources
   - Use clear language in user-facing instructions
   - Provide alternative formats when necessary

## Contact

For questions or issues about this guidance, please contact the technical lead for the project.

For security concerns, report to UKHO-ITSO@gov.co.uk.
