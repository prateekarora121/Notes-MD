# Runtime C# Script Engine — DLL Plugin Pattern (Detailed Notes)

> **Purpose:** Explain how a system can **compile and run C# scripts at runtime** — where the script is stored as plain **source text** (e.g., in a database) and executed on demand, without rebuilding or redeploying the application. This uses the "DLL runtime plugin" pattern: a separate engine DLL is loaded at runtime, and inside it the incoming script text is compiled in-memory and executed.
>
> After reading this, you should understand: which libraries are used, how a script is compiled, how it is executed, and the pros and cons.

---

## 1. The Big Picture (one line)

Business/mapping logic is stored as **C# source code text** (not part of the compiled project). At runtime the source arrives **as a string**, and the system **compiles and runs it on the fly**. Changing behaviour just means updating the script text — no application redeploy.

Three logical layers work together:

| Layer | Responsibility | Framework |
|---|---|---|
| **Web API host** | Receives the request, calls the orchestration layer | ASP.NET Core (`net8.0`) |
| **Orchestrator / caller** | Fetches the script text, **loads the engine DLL at runtime**, and invokes it | `net8.0` |
| **Script Engine (plugin DLL)** | Does the actual **compile + run** (Roslyn based) | `net8.0` |

---

## 2. What "DLL Runtime Plugin" means here

Two separate "runtime loading" mechanisms happen — both matter, don't confuse them:

### (a) Loading the engine DLL as a plugin
The caller does **not** reference the engine directly. It picks the DLL from disk at runtime:

- Loading: `Assembly.LoadFrom(enginePath)` — the engine DLL is loaded like a **plugin**
- Calling: via **reflection** — `GetType(...)` → `Activator.CreateInstance(...)` → `GetMethod("ExecuteScript").Invoke(...)`

**Why reflection / LoadFrom instead of a direct reference?**
- Keeps the engine **loosely coupled / hot-swappable** — the DLL can be replaced to change behaviour.
- Controls the **binding context** — loading by absolute path avoids the runtime resolving an unexpected version.

### (b) Compiling and loading the script at runtime
Inside the engine, the C# source string is **compiled in-memory using Roslyn**, and the freshly built assembly is loaded into the process via `Assembly.Load(bytes)`. This is the real "run scripts at runtime" part (Section 5).

---

## 3. The Full Flow (step by step)

```
HTTP request (Web API controller)
        │
        ▼
Orchestrator  ── fetches the script text (+ builds an args dictionary)
        │
        │  Assembly.LoadFrom("ScriptEngine.dll")   ← load plugin DLL
        │  Reflection: engine.ExecuteScript(script, args)
        ▼
Script Engine.ExecuteScript
        │
        ├── 1) Compatibility fix-ups on the source text (see Section 5)
        │
        ├── 2) CompileCode(script)                 ← ROSLYN COMPILE
        │        ParseText → MetadataReferences → CSharpCompilation → Emit(memory)
        │        Assembly.Load(bytes)   ← compiled script into memory
        │
        └── 3) RunScript(assembly, args)           ← REFLECTION EXECUTE
                 find the entry class (implementing the script interface)
                 create instance → .RunScript(argList)
                 returns Dictionary<string, object>
        │
        ▼
Result dictionary → back up to the controller → HTTP response
```

Both compile and run happen inside a single `ExecuteScript` call.

---

## 4. The Script "Contract" (what a script must look like)

Every script is a **complete C# source file** (as a string). It must follow a fixed shape, else the engine can't run it:

- It must contain a well-known **entry class** (e.g., a class named `ScriptClass`)
- That class must implement a shared **script interface** (e.g., `IScript`)
- The interface exposes a single method: `Dictionary<string, object> RunScript(Dictionary<string, object> argList)`
- Inputs and options arrive via the `argList` dictionary; results are returned in a dictionary too.

### Special comment "markers" (a lightweight convention)
Some engines let the script carry metadata as specially-formatted **comments**. These look like normal C# comments but the engine parses them as instructions. Typical uses:

| Example marker | Role |
|---|---|
| `//SCRIPTTYPE:[CSharp]` | Declares the script language / that it's a valid script |
| `//REFERENCE:[a.dll\|b.dll]` | Extra DLL references the script needs (path-based) |
| `//DEBUG:[YES]` | Route to an in-process engine for step-through debugging |
| section/merge markers | Mark regions to be merged with a shared/base template before compiling |

**Note:** Marker-based references let a script declare its own dependencies — flexible, but risky (see Cons).

---

## 5. How compilation works (Roslyn)

This is the core. A Roslyn-based `CompileCode(source)` typically does:

1. **Parse:** Turn the source text into a syntax tree — `CSharpSyntaxTree.ParseText(source)`.
2. **Gather references (key step):** The new assembly needs to know which DLLs are available. A common trick is to take the **currently loaded assemblies in the AppDomain**, filter them by name to the relevant set (core BCL, JSON library, the dynamic-language runtime, the engine itself, and any domain DLLs), and turn each into a `MetadataReference` via `MetadataReference.CreateFromFile(...)`. In short: "hand the script whatever is already loaded in the process."
3. **Compile:** Build `CSharpCompilation.Create(...)` with those references; output kind = **DLL (`DynamicallyLinkedLibrary`)**.
4. **Emit:** `compilation.Emit(memoryStream)` — the compiled bytes go straight into **memory**; no `.dll` is written to disk.
5. **Error handling:** On failure, collect the error diagnostics and throw a descriptive exception. This lets a validator catch a broken script before deployment.
6. **Load:** On success, `Assembly.Load(bytes)` — the newly compiled assembly is now in the process, ready to use.

### Two common "hacks" worth understanding
- **Runtime text rewrite for legacy references:** Old scripts may reference an outdated, framework-specific build of a library (e.g., a custom-named JSON assembly) that won't load under a modern .NET runtime. Instead of editing thousands of scripts, the engine does a **string replace** on the source at runtime to point at the modern package. Effective, but "magic" behaviour that's easy to miss.
- **Forcing a dependency into memory:** Some references (e.g., the expression/`dynamic` infrastructure) aren't loaded until something actually uses them. Engines work around this by touching a `dynamic`/expression value early (in a static constructor or a dummy arg) so the reference is present when the script compiles.

---

## 6. How execution works (Reflection)

After compilation, `RunScript` does:

1. From the compiled assembly's exported types, find the **entry class** that implements the **script interface**.
2. Get its **public parameterless constructor** and invoke it to build an instance.
3. Cast the instance to the interface and call `.RunScript(argList)`.
4. Return the resulting `Dictionary<string, object>`.

### Graceful exceptions via a flag
A useful pattern: if the caller passes a flag (e.g., a `"ReturnNoException"` key) in `argList`, the engine **doesn't rethrow** exceptions from inside the script — it returns them inside the result dictionary (message, stack, exception object). The caller then decides how to handle them.

**Why?** These scripts run in a separate context; letting exceptions bubble up freely can be mislabelled as "unhandled" by APM/monitoring tools and pollute the logs.

---

## 7. Validation before deploy

A validator component can **compile a script without running it** to check it's deployable. Beyond a plain compile check, a validator often handles **dependency-aware merging**:

- Some scripts aren't self-contained — they must be merged with a **shared/base template** before they'll compile.
- Merge points are identified by section markers: placeholder regions in the base template are replaced by the corresponding regions from the specific script.
- The validator should use the **same engine** as runtime, so "passed validation but failed at runtime" surprises are minimized.
- The validator may **preload all DLLs from the bin folder** into the AppDomain first, so every needed reference is available at compile time.

---

## 8. Two compiler approaches (legacy vs modern)

| | **Legacy (.NET Framework)** | **Modern (.NET 8)** |
|---|---|---|
| Compiler tech | **CodeDom** — `CSharpCodeProvider` + `CompilerParameters` + `CompileAssemblyFromSource` (invokes the real `csc`) | **Roslyn** — `Microsoft.CodeAnalysis.CSharp` (in-process, in-memory) |
| Reference resolution | From the script's path-based `//REFERENCE:` markers | By name-filtering the already-loaded AppDomain assemblies |
| Output | In-memory assembly (`GenerateInMemory = true`) | In-memory bytes → `Assembly.Load(bytes)` |
| Path assumptions | Fixed drive/server paths from the old hosting model | Path-based refs largely dropped; framework DLL paths differ on modern .NET |

**Why migrate:** CodeDom / `CSharpCodeProvider` isn't supported on modern .NET the way Roslyn is. Moving to Linux containers on .NET 8 makes Roslyn the natural choice.

---

## 9. Libraries / Packages used

### Script Engine (the plugin) — `net8.0`
| Package | Role |
|---|---|
| **Microsoft.CodeAnalysis.CSharp** (Roslyn) | The heart — compiles the script (SyntaxTree, Compilation, Emit) |
| **Microsoft.CSharp** | `dynamic` type support (scripts commonly use `dynamic` / `ExpandoObject`) |
| **Microsoft.AspNetCore.SystemWebAdapters** | Shim so old `System.Web.HttpContext`-style code compiles/runs on .NET 8 |

### Orchestrator / caller — `net8.0`
| Package | Role |
|---|---|
| Newtonsoft.Json | JSON parse/serialize (scripts frequently use it) |
| Microsoft.CSharp | `dynamic` support |
| An APM agent (e.g., NewRelic API) | Observability / error logging |
| Microsoft.AspNetCore.SystemWebAdapters | `System.Web` compatibility shim |
| System.Configuration.ConfigurationManager | Config access |

### Web API host — ASP.NET Core `net8.0`
| Package | Role |
|---|---|
| Newtonsoft.Json | JSON |
| Microsoft.CSharp | `dynamic` |
| Swashbuckle.AspNetCore | Swagger/OpenAPI |
| An APM agent | Monitoring |
| Microsoft.AspNetCore.SystemWebAdapters | `System.Web` shim |
| System.Web.Http / System.Net.Http.Formatting | Legacy Web API compatibility surface |

### Reference-assembly DLLs shipped alongside the app
Old framework reference assemblies (`System.dll`, `System.Core.dll`, `System.Data.dll`, `System.Xml.dll`, `System.Xml.Linq.dll`, `Microsoft.CSharp.dll`) may be copied into the publish output because some legacy scripts explicitly reference them and they must be present at compile time.

---

## 10. Pros

- **No redeploy for logic changes:** Logic lives as data — change the script text, get new behaviour on the next request. Fast turnaround.
- **Per-tenant customization:** Each tenant/variant can have its own script; a single service handles many variations.
- **In-memory compilation:** No temp `.dll` on disk — clean and read-only-friendly for containers.
- **Validation = runtime consistency:** Validating with the same engine minimizes "worked in validation, broke at runtime" surprises.
- **Loose coupling (plugin model):** The engine DLL is separate — easy to replace/upgrade; reflection avoids binding-context issues.
- **Graceful errors:** The "return-no-exception" pattern keeps failures out of the unhandled-error logs and under control.
- **Legacy-friendly:** Runtime text rewrites and compatibility shims let large volumes of old scripts run on modern .NET without a rewrite.

## 11. Cons / Risks

- **Performance — compile on every call:** Naively, the source is **recompiled each call** (Roslyn compilation is expensive). Caching the compiled assembly is possible but must be done deliberately; the default path often doesn't. High-volume paths can bottleneck.
- **Security — arbitrary code execution:** The engine compiles and runs *any* C# (file I/O, network, etc.). If the script source is compromised, arbitrary code runs on the server. The script store and authoring path must be tightly controlled.
- **Fragile, name-based reference resolution:** Pulling references from "already-loaded assemblies" means a script fails with `type/namespace not found` if a needed DLL isn't loaded yet — hence workarounds like preloading and dependency warm-ups.
- **Hidden coupling via string replace:** Runtime source rewrites are non-obvious magic — hard for a newcomer to discover and debug.
- **Reflection everywhere:** Both the engine and the script are reached via reflection — no compile-time type safety; mistakes only appear at runtime.
- **No assembly unload:** Assemblies loaded via `Assembly.Load(bytes)` land in the default load context and aren't unloaded until the process exits. Compiling many distinct scripts over time can slowly **grow memory** (assembly leak). (A collectible `AssemblyLoadContext` is the usual fix.)
- **Low observability:** Nothing on disk, everything in memory — tracing/debugging is harder. An in-process debug path (e.g., a `//DEBUG:[YES]` route) helps.
- **Multiple engines during migration:** Keeping both a legacy and a modern engine risks duplication and drift.

---

## 12. One-line summary

> Scripts are stored **as C# source text**. At runtime a **plugin DLL is loaded via `Assembly.LoadFrom` + reflection**; that engine **compiles the source in-memory with Roslyn** (`CSharpCompilation.Emit` → `Assembly.Load`), then uses **reflection** to instantiate the entry class and call its `RunScript(argList)`, returning a result dictionary. A validator compile-checks with the same engine before deploy. Benefit = change logic without redeploy; risks = per-call compile cost, arbitrary-code execution, and slow memory growth from unremovable assemblies.
