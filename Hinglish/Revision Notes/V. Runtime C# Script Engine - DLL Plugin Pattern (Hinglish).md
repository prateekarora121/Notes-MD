# Runtime C# Script Engine — DLL Plugin Pattern (Detailed Notes)

> **Purpose:** Samjhna ki ek system kaise **C# scripts ko runtime pe compile aur run** kar sakta hai — jahan script plain **source text** ki tarah store hoti hai (jaise database mein) aur on demand execute hoti hai, bina application ko rebuild ya redeploy karne ke. Ismein "DLL runtime plugin" pattern use hota hai: ek alag engine DLL runtime pe load hoti hai, aur uske andar incoming script text in-memory compile hoke execute hoti hai.
>
> Yeh padhne ke baad aapko samajh aa jayega: kaunsi libraries use hoti hain, script kaise compile hoti hai, kaise execute hoti hai, aur iske pros/cons kya hain.

---

## 1. The Big Picture (one line)

Business/mapping logic **C# source code text** ki tarah store hoti hai (compiled project ka hissa nahi). Runtime pe source ek **string** ki form mein aata hai, aur system usko **on the fly compile aur run** kar deta hai. Behaviour change karna hai? Bas script text update kar do — application redeploy ki zarurat nahi.

Teen logical layers milke kaam karti hain:

| Layer | Responsibility | Framework |
|---|---|---|
| **Web API host** | Request receive karta hai, orchestration layer ko call karta hai | ASP.NET Core (`net8.0`) |
| **Orchestrator / caller** | Script text fetch karta hai, **engine DLL ko runtime pe load** karta hai, aur usko invoke karta hai | `net8.0` |
| **Script Engine (plugin DLL)** | Actual **compile + run** ka kaam karta hai (Roslyn based) | `net8.0` |

---

## 2. What "DLL Runtime Plugin" means here

Do alag-alag "runtime loading" mechanisms hote hain — dono important hain, inko confuse mat karna:

### (a) Loading the engine DLL as a plugin
Caller engine ko **directly reference nahi karta**. Woh DLL ko runtime pe disk se uthata hai:

- Loading: `Assembly.LoadFrom(enginePath)` — engine DLL ek **plugin** ki tarah load hoti hai
- Calling: **reflection** ke through — `GetType(...)` → `Activator.CreateInstance(...)` → `GetMethod("ExecuteScript").Invoke(...)`

**Direct reference ki jagah reflection / LoadFrom kyun?**
- Engine **loosely coupled / hot-swappable** rehti hai — behaviour badalne ke liye DLL replace kar sakte ho.
- **Binding context** control mein rehta hai — absolute path se load karne se runtime koi unexpected version resolve nahi karta.

### (b) Compiling and loading the script at runtime
Engine ke andar, C# source string **Roslyn se in-memory compile** hoti hai, aur naya bana assembly `Assembly.Load(bytes)` ke through process mein load ho jata hai. Yeh actual "runtime pe scripts chalana" wala hissa hai (Section 5).

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

Compile aur run — dono ek hi `ExecuteScript` call ke andar hote hain.

---

## 4. The Script "Contract" (what a script must look like)

Har script ek **complete C# source file** hoti hai (string ki form mein). Usko ek fixed shape follow karna padta hai, warna engine usko run nahi kar payegi:

- Usmein ek well-known **entry class** hona chahiye (jaise `ScriptClass` naam ki class)
- Woh class ek shared **script interface** implement kare (jaise `IScript`)
- Interface mein ek hi method hota hai: `Dictionary<string, object> RunScript(Dictionary<string, object> argList)`
- Inputs aur options `argList` dictionary ke through aate hain; results bhi dictionary mein hi return hote hain.

### Special comment "markers" (a lightweight convention)
Kuch engines script ko metadata specially-formatted **comments** ki form mein carry karne dete hain. Yeh normal C# comments jaise dikhte hain, par engine unko instructions ki tarah parse karti hai. Typical uses:

| Example marker | Role |
|---|---|
| `//SCRIPTTYPE:[CSharp]` | Script ki language declare karta hai / ki yeh valid script hai |
| `//REFERENCE:[a.dll\|b.dll]` | Extra DLL references jo script ko chahiye (path-based) |
| `//DEBUG:[YES]` | In-process engine pe route karo taaki step-through debugging ho sake |
| section/merge markers | Woh regions mark karte hain jo compile se pehle shared/base template ke saath merge honge |

**Note:** Marker-based references se script apni dependencies khud declare kar sakti hai — flexible hai, par risky bhi (Cons dekho).

---

## 5. How compilation works (Roslyn)

Yeh core hissa hai. Ek Roslyn-based `CompileCode(source)` typically yeh karta hai:

1. **Parse:** Source text ko syntax tree mein badlo — `CSharpSyntaxTree.ParseText(source)`.
2. **Gather references (key step):** Naye assembly ko pata hona chahiye kaunsi DLLs available hain. Ek common trick hai — **AppDomain mein currently loaded assemblies** uthao, unko naam se filter karke relevant set banao (core BCL, JSON library, dynamic-language runtime, engine khud, aur koi domain DLLs), aur har ek ko `MetadataReference.CreateFromFile(...)` se `MetadataReference` bana do. Short mein: "jo already process mein loaded hai, wahi script ko de do."
3. **Compile:** `CSharpCompilation.Create(...)` un references ke saath banao; output kind = **DLL (`DynamicallyLinkedLibrary`)**.
4. **Emit:** `compilation.Emit(memoryStream)` — compiled bytes seedhe **memory** mein jate hain; disk pe koi `.dll` nahi likhi jati.
5. **Error handling:** Fail hone pe error diagnostics collect karke ek descriptive exception throw karo. Isse ek validator broken script ko deployment se pehle catch kar sakta hai.
6. **Load:** Success pe `Assembly.Load(bytes)` — naya compiled assembly ab process mein hai, use karne ke liye ready.

### Two common "hacks" worth understanding
- **Runtime text rewrite for legacy references:** Purani scripts kisi outdated, framework-specific library build ko reference kar sakti hain (jaise ek custom-named JSON assembly) jo modern .NET runtime pe load nahi hogi. Hazaaron scripts edit karne ki jagah, engine runtime pe source pe **string replace** kar deti hai taaki modern package pe point ho jaye. Kaam karta hai, par yeh "magic" behaviour hai jo aasani se miss ho jata hai.
- **Forcing a dependency into memory:** Kuch references (jaise expression/`dynamic` infrastructure) tab tak load nahi hote jab tak koi actually unko use na kare. Engines iska workaround yeh karti hain ki ek `dynamic`/expression value ko pehle hi touch kar leti hain (static constructor mein ya dummy arg se) taaki script compile hote waqt reference maujood ho.

---

## 6. How execution works (Reflection)

Compilation ke baad, `RunScript` yeh karta hai:

1. Compiled assembly ke exported types mein se woh **entry class** dhundo jo **script interface** implement karti hai.
2. Uska **public parameterless constructor** lo aur invoke karke instance banao.
3. Instance ko interface pe cast karo aur `.RunScript(argList)` call karo.
4. Resulting `Dictionary<string, object>` return karo.

### Graceful exceptions via a flag
Ek useful pattern: agar caller `argList` mein ek flag pass karta hai (jaise `"ReturnNoException"` key), to engine script ke andar ke exceptions ko **rethrow nahi karti** — unko result dictionary ke andar return kar deti hai (message, stack, exception object). Phir caller decide karta hai ki unko kaise handle karna hai.

**Kyun?** Yeh scripts ek separate context mein chalti hain; exceptions ko freely bubble up karne dena APM/monitoring tools dwara "unhandled" label ho sakta hai aur logs ganda kar sakta hai.

---

## 7. Validation before deploy

Ek validator component script ko **run kiye bina compile** karke check kar sakta hai ki woh deployable hai ya nahi. Plain compile check ke alawa, validator often **dependency-aware merging** bhi handle karta hai:

- Kuch scripts self-contained nahi hoti — unko compile hone se pehle ek **shared/base template** ke saath merge karna padta hai.
- Merge points section markers se identify hote hain: base template ke placeholder regions specific script ke corresponding regions se replace ho jate hain.
- Validator ko **wahi engine** use karna chahiye jo runtime use karta hai, taaki "validation pass hua par runtime pe fail" jaise surprises minimize hon.
- Validator pehle **bin folder ki saari DLLs AppDomain mein preload** kar sakta hai, taaki compile time pe har needed reference available ho.

---

## 8. Two compiler approaches (legacy vs modern)

| | **Legacy (.NET Framework)** | **Modern (.NET 8)** |
|---|---|---|
| Compiler tech | **CodeDom** — `CSharpCodeProvider` + `CompilerParameters` + `CompileAssemblyFromSource` (real `csc` invoke karta hai) | **Roslyn** — `Microsoft.CodeAnalysis.CSharp` (in-process, in-memory) |
| Reference resolution | Script ke path-based `//REFERENCE:` markers se | Already-loaded AppDomain assemblies ko naam se filter karke |
| Output | In-memory assembly (`GenerateInMemory = true`) | In-memory bytes → `Assembly.Load(bytes)` |
| Path assumptions | Purane hosting model ke fixed drive/server paths | Path-based refs largely drop ho gaye; modern .NET pe framework DLL paths different hain |

**Migrate kyun karna:** CodeDom / `CSharpCodeProvider` modern .NET pe us tarah supported nahi hai jaise Roslyn hai. .NET 8 pe Linux containers pe move karne se Roslyn natural choice ban jati hai.

---

## 9. Libraries / Packages used

### Script Engine (the plugin) — `net8.0`
| Package | Role |
|---|---|
| **Microsoft.CodeAnalysis.CSharp** (Roslyn) | Dil — script compile karta hai (SyntaxTree, Compilation, Emit) |
| **Microsoft.CSharp** | `dynamic` type support (scripts commonly `dynamic` / `ExpandoObject` use karti hain) |
| **Microsoft.AspNetCore.SystemWebAdapters** | Shim, taaki purana `System.Web.HttpContext`-style code .NET 8 pe compile/run ho jaye |

### Orchestrator / caller — `net8.0`
| Package | Role |
|---|---|
| Newtonsoft.Json | JSON parse/serialize (scripts isko frequently use karti hain) |
| Microsoft.CSharp | `dynamic` support |
| An APM agent (jaise NewRelic API) | Observability / error logging |
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
Purane framework reference assemblies (`System.dll`, `System.Core.dll`, `System.Data.dll`, `System.Xml.dll`, `System.Xml.Linq.dll`, `Microsoft.CSharp.dll`) publish output mein copy ho sakti hain, kyunki kuch legacy scripts unko explicitly reference karti hain aur compile time pe woh present hona zaruri hai.

---

## 10. Pros

- **Logic changes ke liye redeploy nahi:** Logic data ki tarah rehta hai — script text change karo, next request pe naya behaviour mil jayega. Fast turnaround.
- **Per-tenant customization:** Har tenant/variant ki apni script ho sakti hai; ek hi service bahut variations handle kar leti hai.
- **In-memory compilation:** Disk pe koi temp `.dll` nahi — clean, aur containers ke read-only setup ke liye friendly.
- **Validation = runtime consistency:** Same engine se validate karne se "validation mein chala, runtime pe toot gaya" jaise surprises minimize hote hain.
- **Loose coupling (plugin model):** Engine DLL separate hai — replace/upgrade karna aasan; reflection binding-context issues bacha leta hai.
- **Graceful errors:** "Return-no-exception" pattern failures ko unhandled-error logs se bahar aur control mein rakhta hai.
- **Legacy-friendly:** Runtime text rewrites aur compatibility shims se purani scripts ka bada volume modern .NET pe bina rewrite chal jata hai.

## 11. Cons / Risks

- **Performance — har call pe compile:** Naive implementation mein source **har call pe recompile** hota hai (Roslyn compilation expensive hai). Compiled assembly cache karna possible hai par deliberately karna padta hai; default path often nahi karta. High-volume paths bottleneck ban sakte hain.
- **Security — arbitrary code execution:** Engine *koi bhi* C# compile aur run kar deti hai (file I/O, network, etc.). Agar script source compromise ho gaya, to server pe arbitrary code chalega. Script store aur authoring path tightly controlled hona chahiye.
- **Fragile, name-based reference resolution:** "Already-loaded assemblies" se references uthane ka matlab hai ki agar koi needed DLL abhi load nahi hui to script `type/namespace not found` se fail hogi — isiliye preloading aur dependency warm-ups jaise workarounds aate hain.
- **String replace se hidden coupling:** Runtime source rewrites non-obvious magic hain — naye bande ke liye discover aur debug karna mushkil.
- **Reflection har jagah:** Engine aur script dono reflection se reach hote hain — koi compile-time type safety nahi; galtiyan sirf runtime pe dikhti hain.
- **Assembly unload nahi hota:** `Assembly.Load(bytes)` se load hui assemblies default load context mein jati hain aur process exit tak unload nahi hoti. Time ke saath bahut distinct scripts compile karne se **memory dheere-dheere badh** sakti hai (assembly leak). (Collectible `AssemblyLoadContext` iska usual fix hai.)
- **Low observability:** Disk pe kuch nahi, sab memory mein — tracing/debugging mushkil. Ek in-process debug path (jaise `//DEBUG:[YES]` route) help karta hai.
- **Migration ke dauraan multiple engines:** Legacy aur modern dono engines rakhne se duplication aur drift ka risk rehta hai.

---

## 12. One-line summary

> Scripts **C# source text** ki tarah store hoti hain. Runtime pe ek **plugin DLL `Assembly.LoadFrom` + reflection** se load hoti hai; woh engine **source ko Roslyn se in-memory compile** karti hai (`CSharpCompilation.Emit` → `Assembly.Load`), phir **reflection** se entry class ka instance banake uska `RunScript(argList)` call karti hai, aur ek result dictionary return karti hai. Ek validator deploy se pehle same engine se compile-check karta hai. Benefit = bina redeploy logic change; risks = per-call compile cost, arbitrary-code execution, aur unremovable assemblies se dheeri memory growth.
