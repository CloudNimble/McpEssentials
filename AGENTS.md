# General

Please write a high quality, general purpose solution. Implement a solution that works correctly for all valid inputs, not just the test cases. Do not hard-code values or create solutions that only work for specific test inputs. Instead, implement the actual logic that solves the problem generally.

Focus on understanding the problem requirements and implementing the correct algorithm. Tests are there to verify correctness, not to define the solution. Provide a principled implementation that follows best practices and software design principles.

If the task is unreasonable or infeasible, or if any of the tests are incorrect, please tell me. The solution should be robust, maintainable, and extendable.

For maximum efficiency, whenever you need to perform multiple independent operations, invoke all relevant tools simultaneously rather than sequentially. After receiving tool results, carefully reflect on their quality and determine optimal next steps before proceeding. Use your thinking to plan and iterate based on this new information, and then take the best next action.

If you create any temporary new files, scripts, or helper files for iteration, clean up these files by removing them at the end of the task.

## Guidelines

* Make only high confidence suggestions when reviewing code changes.
* Always use the latest C# for the SDK pinned in `src/global.json` (`LangVersion` is `latest`).
* Never change `global.json` unless explicitly asked to.
* Always prefer `.IsNullOrWhiteSpace()` over `.IsNullOrEmpty()` for strings.
* Always prefer `ArgumentException.ThrowIfNullOrWhiteSpace()` over standard string parameter null checks.
* Always prefer `ArgumentNullException.ThrowIfNull()` over standard object parameter null checks.
* Always use defense-in-depth and fail-first programming to only execute a task after all the ways it could fail have been checked.
* Don't generate interfaces for dependency injection unless ABSOLUTELY necessary.

# MCP protocol

McpEssentials supports **only the current Model Context Protocol revision**. Today that is `2026-07-28` (SEP-2663 tasks, MRTR elicitation, Streamable HTTP). When the spec moves, we move.

The official MCP C# SDK still speaks older revisions so existing clients can connect. That compatibility is SDK necessity. It is **not** our product surface, and it is not a reason to keep older behavior in this repo.

* Hosts are Streamable HTTP with `WithHttpTransport(o => o.Stateless = true)`.
* Elicitation on a normal tool is MRTR: stamp `[FormElicitation("ship")]` and call `context.RequireForm("ship")`. `WithTasksPlus` keeps that tool `Synchronous`. A task-backed tool may `await server.ElicitAsync` because `WithTasks` parks the form on the task store.
* Task isolation is per **user**, via the identity factory on `WithTasksPlus` / `With*Tasks`.
* Stateful HTTP is unsupported. Do not design, document, test, or explain it. Do not use it as contrast, background, or an SDK-internals lesson. Anyone building for it is building for the past.
* Do not add fallbacks that keep older protocol behavior alive in this repo.

# Formatting

* Apply code-formatting style defined in `.editorconfig`.
* Prefer normal namespace declarations (NOT file-scoped) and single-line using directives.
* Insert a newline before the opening curly brace of any code block (e.g., after `if`, `for`, `while`, `foreach`, `using`, `try`, etc.).
* Ensure that the final return statement of a method is on its own line.
* Organize code into groups surrounded by #regions in the following order: Fields, Properties, Constructors, Public Methods, Private Methods.
  - Region instructions should be surrounded by blank lines.
* Members should be ordered by visibility, with public members first, followed by protected, then internal members.
* No member should be private unless it is a field. We need to be able to unit test all methods in a class.
* Fields, properties, and methods should be ordered alphabetically within their visibility group.
* Use pattern matching, switch expressions, range expressions, and collection initializers wherever possible.
* Use `nameof` instead of string literals when referring to member names.
* Ensure that extensive XML doc comments are created for any APIs.
  * When applicable, include `<example>` and `<code>` documentation in the comments.
  * Only `<param>` tags should be on the same line as content.
  * The `<remarks>` tag should be the last one before the member declaration.
* Public types stay unsealed unless sealing is required. One type per file.
* Project files: `Reference` / `PackageReference` / `ProjectReference` `ItemGroup`s come first after the leading `PropertyGroup`, each kind in its own group, entries alphabetical.

## Nullable Reference Types

* Declare variables non-nullable, and check for `null` at entry points.
* Always use `is null` or `is not null` instead of `== null` or `!= null`.
* Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.

# Testing

## Guidelines

* Test projects are `CloudNimble.McpEssentials.Tests.*` next to the production project they cover.
* We use MSTest, Breakdance, and FluentAssertions. No mocking. Ever.
* Do not emit "Act", "Arrange" or "Assert" comments.
* Copy existing style in nearby files for test method names and capitalization.
* Always prefer `.NotBeNullOrWhiteSpace()` over `.NotBeNullOrEmpty()` for testing strings.
* Run tests from `src` so `global.json` selects Microsoft.Testing.Platform (`dotnet exec <dll> --ignore-exit-code 8`, or `dotnet test` with cwd at `src`). Do not run `dotnet test` from the repo root.

# Documentation

## Source System

* We're using DotNetDocs.com for documentation
  * The user instructions are at https://dotnetdocs.com/llms-full.txt
  * We're using Mintlify for hosting the docs. Formatting instructions for building guide documentation is at https://mintlify.com/docs/llms-full.txt
  * The schema for docs.json files is here: https://leaves.mintlify.com/schema/docs.json

## Process

* API documentation is created automatically by using the `DotNetDocs.Sdk` referenced in `src/CloudNimble.McpEssentials.Docs/CloudNimble.McpEssentials.Docs.docsproj`.
* XML documentation comments must be consistent, succinct, and as useful as possible.
* NEVER modify generated API `.mdx` files under `src/CloudNimble.McpEssentials.Docs/api-reference/`.
* NEVER hand-edit `docs.json`. After Core/EF type changes, wipe `api-reference` and rebuild the docsproj from `src`.
* Conceptual guides live under `src/CloudNimble.McpEssentials.Docs/guides`. Form elicitation lead path is named `AddFormElicitation` + MRTR or task-parked `ElicitAsync`. Compact XML is the OData helper, not the intro.

# Build Process

*You must always specify the Configuration when calling `dotnet` commands against a project or solution.

# Final Thoughts
You can do it! Don't hold back. Give it your all.
