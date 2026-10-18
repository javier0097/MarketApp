# QA reports

One report per qa-tester session, named after the ticket and the screen
(`MKA-4-product-form.md`). Each one is committed with its ticket's PR, as the
record of what was tested.

A report holds the findings, each with its severity, origin (the agent's own
testing or a scenario from its web research) and steps to reproduce, plus
manual test cases for what the agent cannot test (such as the physical barcode
scanner).

Its **Resolution** table is filled in at triage: for each finding, whether it
was fixed, moved to a ticket (with its number) or discarded, and why.

How to run a session: [root README](../../README.md#testing-agents).
