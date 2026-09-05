# Business sample data

The generator creates fictional Northstar Business Services records for Finance,
Marketing, Legal, Human Resources, Operations, and Sales. Folders follow
department → team → manager → initiative → workstream → records, subject to the
configured depth and folder count. Small runs may include only part of the catalog.

Each team has a business scenario with consistent facts, risks, decisions, and
actions. Documents include an analysis or brief, status update, risk register,
decision record, and action plan. Names and text share the initiative, reporting
period, department, team, owner, and folder context. Department-level documents
cover that department's teams. All documents remain plain `.txt` files for indexing.

Example searches:

- Why was the operating budget exceeded by 7 percent?
- What is the cost per qualified lead for the enterprise webinar?
- Which contract renewal has an unresolved liability cap?
- How much could we save by removing unused software licenses?
- Who owns the customer success renewal recovery plans?

Configure `SampleData` in `appsettings.json` (or user secrets), then run
`dotnet run --project utils/FileBrowser.SampleDataGenerator` from the repository root.
Choose `Provider: Local` and an `OutputPath` for local generation, or configure
the Azure destination. Existing entries are preserved with collision-safe names.
Use a fresh destination/prefix to avoid mixing earlier random samples into search.

File totals, per-folder limits, and minimum/maximum depth retain their existing
meaning: the minimum depth is reached by at least one branch. A seed reproduces
the same data in an empty destination. Reporting periods begin in January 2025
and do not depend on the current date. Scenario facts are synthetic fixtures,
not a simulation of changing monthly business performance or legal guidance.
