# API reference

This folder is the reference for the public API of every onegame package. Start at [index.md](index.md), or look a type up by name in [types.md](types.md).

The pages are generated from the built assemblies and the XML documentation comments in the source. Nothing in this folder is edited by hand except this file and `sdk.md` (the MSBuild SDK's properties and items, which are not .NET types; the index links it once it exists). To change a page, change the doc comment on the type or member in `src/` and regenerate.

## Layout

```text
docs/api/
  index.md                          every package, with namespace, type and member counts
  types.md                          every public type, A to Z
  sdk.md                            MSBuild SDK properties and items (hand-written; linked from the index once it exists)
  <Assembly>/index.md               one package: its namespaces, each with a table of its types
  <Assembly>/<Namespace>/<Type>.md  one page per public type
```

- `<Assembly>` is the project name, such as `OneGame.Core`; the NuGet package is `RecursiveAI.` followed by the same name.
- A generic type's file name ends in `-` and its number of type parameters: `List<T>` is `List-1.md`. A nested type's name carries its outer types: `Outer.Inner.md`. Types in the global namespace sit in a `_global` folder.
- A type page has the declaration, the type's documentation, then a table per kind of member (constructors, fields, properties, events, methods, operators, nested types) and one section per member. Every member section has a stable anchor, so a link such as `Type.md#m-add-int-int` (for `Add(int, int)`) keeps working until that member's parameter types change.
- Only the public surface is listed: public and protected members of public types. Members inherited from a base type appear on the base type's page; the page names the base types and interfaces.

## Links

- A reference to another onegame type or member in a doc comment or signature links to its page.
- Types from .NET link to [learn.microsoft.com](https://learn.microsoft.com/dotnet/api/).
- Types from the sibling libraries (OneFont, OneImage, OneYaml, OneAudio, OneShader, OneRender) and any reference that does not resolve are shown as plain code.

## Doc comment tags

The generator renders these XML documentation tags. Write them the way the C# compiler documentation describes them.

| Tag | Rendered as |
|---|---|
| `summary` | The first paragraph under the declaration; its first sentence is the description in the tables |
| `remarks` | Plain paragraphs under the summary |
| `param`, `typeparam` | A list of parameters or type parameters with their types |
| `returns`, `value` | A "Returns" or "Value" paragraph |
| `exception` | A list of exception types, linked, with the condition |
| `example` | An "Example" section; put the code in a `code` element |
| `code` | A fenced `csharp` block, with the common indentation removed |
| `c` | Inline code |
| `see cref`, `seealso cref` | A link to the type or member (`seealso` also lists under "See also") |
| `see href`, `see langword` | A link to the address, or inline code for the keyword |
| `paramref`, `typeparamref` | Inline code |
| `para` | A paragraph |
| `list` | A bullet list, a numbered list, or a table (`type="table"` with `listheader`) |
| `inheritdoc` | The documentation of the overridden or implemented member, or of the `cref` it names |

Plain text is escaped, so a stray `*` or `<` in a summary shows as typed. Other XML elements are shown as their text.

## Regenerating

Build the solution in Release, then run the generator from the repository root:

```sh
dotnet build OneGame.slnx -c Release
dotnet run --project tools/OneGame.ApiDocs -c Release -- --out docs/api
```

The output is deterministic: running it twice, or on Linux and on Windows, writes the same bytes. `--check` writes nothing and exits with 1 if the files on disk differ from what would be generated; the CI build runs it. Other options: `--bin <dir>` (build output, default `$ONEGAME_ARTIFACTS/bin` or `artifacts/bin`), `--src <dir>` (default `src`).
