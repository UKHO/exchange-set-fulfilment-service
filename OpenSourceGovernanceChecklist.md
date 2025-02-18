# Open Source Governance Checklist

## Identifer

ERP Facade

## Technical owner

Technical Lead Team Abzu

## Description of functionality

See README.md.

## Security

*How has security been considered?*

Has application code been scanned with security tooling and issues corrected?
> The project pipeline will be configured to apply scanning using:
> 1. Coverity - the UKHO adopted static analysis tool
> 2. OWASP Dependency Checker - the UKHO adopted dependency checking tool

Has the application been threat modelled during development and the evidence captured within TFS (or similar)?
> The standard secure development practice is applied to this development, every PBI will be threat modelled as part of its development and evidence captured as part of the PBI.

Has the code been double-checked for security credentials, keys etc.?
> All code submitted to this Repo will be checked using a PR for security credentials, keys etc

Is a disclosure process in place and linked from the codebase?
> The UK Hydrographic Office (UKHO) supplies hydrographic information to protect lives at sea. Maintaining the confidentially, integrity and availability of our services is paramount. Found a security bug? Please report it to us at UKHO-ITSO@gov.co.uk

## Quality

*How has code quality been considered?*

Does the quality of the code reflect our ambitions for high quality code, in terms of being clean, well-tested etc.
> Yes

Has all code been reviewed?
> Yes

Has the open-sourced codebase had its history removed?  If not, have all check-in comments been reviewed?
> N/A

Does the codebase contain all documentation and configuration elements required to build and verify the software?
> The code will be suitable for checkout and build locally.

> The full CI Build is defined within the repository's azure-pipelines.yml. Build steps of this pipeline will be suitable for reuse, however there are components of the CI Pipeline that may not work for 3rd parties.

> Secrets for deployment are injected through Azure Pipelines, so the deployment pipeline is unlikely to be suitable for 3rd parties.

## Contributions

*Has the handling of contributions considered?*

Are contributions explicitly encouraged in the codebase
> See CONTRIBUTING.md

Is a process for responding to issues defined?
> During the life of the project, the PO will monitor and review new issues on a weekly basis. They will respond where they can in the first instance, or escalate to the development team as required.

> Team Abzu will monitor new PRs on a regular basis. 3rd party PRs will be initially reviewed by Team Abzu and if small contributions, they will be accepted. The CONTRIBUTING.md requests that larger changes are proposed through an issue before submitting a PR to allow us to collaborate and confirm that the proposed work is in line with the direction of the project. If they are, they will be added to the sprint to support the validation and acceptance of the 3rd party work.

> At the end of the project, the team will change the CONTRIBUTING.md to indicate that we are not actively monitoring the repository for new PRs or new Issues.

How is this process resourced?
> During the project it will form a part of the project so the project team will provide the resources required. At the end of the project the team will change the CONTRIBUTING.md to indicate that we are no longer seeking active contributions and no longer monitoring for new contributions.
