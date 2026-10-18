# Interview Prep Master Q&A — Hinglish

> **Maqsad:** Is master sheet mein workspace ke interview-prep material ko ek jagah consolidate kiya gaya hai: .NET/C#, full stack, databases, cloud, system design, DevOps, coding, behavioral questions, aur monolith-to-microservices migration. Answers Hinglish mein hain; har main question ke saath common pitfall, practical scenario, aur answered follow-ups diye gaye hain.
>
> **Answer dene ka formula:** Pehle 1–2 line ka direct answer, phir *kyun / kab*, phir trade-off ya pitfall, aur akhir mein apne kaam ka sachcha example. `[apna example]` ko apne real experience se replace karein; numbers, ownership, aur outcomes invent na karein.

## Is guide ko kaise padhein

- **High-frequency fundamentals** ko pehle revise karein; senior interviews mein definition ke baad trade-offs, production behavior, failure modes, aur debugging par drill hota hai.
- "Commonly asked" ka matlab public question banks aur interview-preparation material mein baar-baar dikhne wale question patterns hain. Yeh kisi interviewer ke exact question ya kisi specific company ke interview ki guarantee nahin hai.
- **Reported interview question** tabhi kahiye jab linked public interview report us claim ko support kare. Company-specific reports anecdotal hote hain aur sample bias ho sakta hai.
- Har service, framework, ya tool ke liye wahi depth claim karein jo aapne actually use ki ho. Conceptual familiarity aur hands-on experience alag batayein.
- Workspace ke detailed source guides mein niche/implementation-level subtopics aur code examples hain. Neeche ka coverage map unko cross-reference karta hai; master sheet ka focus interview-ready synthesis hai.

## Contents

1. [C# aur .NET fundamentals](#1-c-aur-net-fundamentals)
2. [OOP, SOLID aur design patterns](#2-oop-solid-aur-design-patterns)
3. [ADO.NET, LINQ aur data access](#3-adonet-linq-aur-data-access)
4. [ASP.NET Core aur Web API](#4-aspnet-core-aur-web-api)
5. [EF Core](#5-ef-core)
6. [Authentication, authorization aur application security](#6-authentication-authorization-aur-application-security)
7. [SQL Server](#7-sql-server)
8. [Microservices, messaging aur migration](#8-microservices-messaging-aur-migration)
9. [System design aur performance](#9-system-design-aur-performance)
10. [Logging, monitoring aur observability](#10-logging-monitoring-aur-observability)
11. [AWS](#11-aws)
12. [Angular](#12-angular)
13. [TypeScript](#13-typescript)
14. [GitHub Actions, deployment aur Artifactory](#14-github-actions-deployment-aur-artifactory)
15. [Testing, compliance aur quality](#15-testing-compliance-aur-quality)
16. [Agile aur team collaboration](#16-agile-aur-team-collaboration)
17. [Coding, DSA aur problem solving](#17-coding-dsa-aur-problem-solving)
18. [Behavioral aur resume deep dive](#18-behavioral-aur-resume-deep-dive)
19. [Converted .NET Full Stack question bank](#19-converted-net-full-stack-question-bank)
20. [Recent company interview reports: evidence aur converted practice](#20-recent-company-interview-reports-evidence-aur-converted-practice)
21. [Coverage audit: source guides ke topics](#21-coverage-audit-source-guides-ke-topics)
22. [JavaScript language aur runtime](#22-javascript-language-aur-runtime)
23. [References aur research limits](#23-references-aur-research-limits)

---

## 1. C# aur .NET fundamentals

### Q1. C# code execute kaise hota hai? CLR, IL aur JIT ka role kya hai?

**Answer:** C# compiler source ko Intermediate Language (IL) aur metadata wale assembly mein compile karta hai. Runtime par CLR assembly load karke type safety, exceptions, garbage collection aur threading jaise services deta hai; JIT hot methods ke IL ko current machine ke native code mein compile karta hai. AOT publishing kuch ya saara compilation deployment se pehle kar sakta hai.

**Pitfall:** "C# hamesha interpreter se chalta hai" ya "JIT poora app startup par ek hi baar compile karta hai" sahi generalization nahin. AOT, tiered compilation, runtime version aur deployment model behavior badal sakte hain.

**Scenario:** Cold-start sensitive serverless service mein startup aur memory important hain. AOT consider kar sakte hain, lekin reflection/dynamic-code limitations, compatibility aur publish size validate karke.

**Follow-ups**
- **JIT ka fayda?** Runtime profiling ke baad hot code optimize ho sakta hai; cost startup par compilation aur runtime dependency hai.
- **Managed code kya hai?** Aisa code jo CLR ke runtime services ke under execute hota hai; unmanaged native code ko memory/resources ke liye aksar explicit lifetime management chahiye.

### Q2. Value type aur reference type mein kya antar hai?

**Answer:** Value type variable mein value khud hoti hai; assignment/call ke waqt value semantics ke mutabik copy ho sakti hai. Reference type variable object ka reference rakhta hai; reference copy karne par dono variables same object ko point kar sakte hain. Actual storage location sirf "stack vs heap" se decide nahin hoti—JIT, object layout aur lifetime matter karte hain.

**Pitfall:** "Struct hamesha stack par aur class hamesha heap par" interview mein oversimplification hai. Mutable, large structs copy-cost aur confusing semantics de sakte hain; reference types bhi value/reference behavior ke context mein samjhein.

**Follow-ups**
- **`record` class vs `record struct`?** Pehla reference type, doosra value type; dono value-oriented equality syntax dete hain. Record immutable automatically nahin hota—`init`/`readonly` aur exposed mutable members check karein.
- **Boxing kya hai?** Value type ko `object` ya compatible interface reference ke roop mein use karne par boxing ho sakti hai; hot path mein allocations/performance inspect karein.

### Q3. `string` immutable kyun hai, aur `StringBuilder` kab use karna chahiye?

**Answer:** .NET `string` immutable hai: banne ke baad uska content change nahin hota. Repeated concatenation loop mein naye strings ban sakte hain; bahut saare incremental changes hon to `StringBuilder` useful hai. Few values concatenate karte waqt compiler/runtime optimizations ki wajah se simple `+` aksar behtar readability deta hai.

**Pitfall:** Har concatenation ke liye `StringBuilder` banana zaroori nahin. `StringBuilder` ko `string` ki jagah public API type samajhna bhi galat hai; final result aam taur par `ToString()` se nikalta hai.

**Follow-ups**
- **`StringBuilder` sealed hai?** Haan, .NET API definition mein `StringBuilder` sealed class hai.
- **String equality?** `==` string contents compare karta hai; culture-sensitive comparison/search ke liye requirement ke mutabik `StringComparison` explicitly choose karein.

### Q4. Constructor inheritance chain mein kis order mein chalte hain?

**Answer:** `new C()` mein, agar `C : B : A`, constructor bodies pehle `A`, phir `B`, phir `C` chalte hain. Har class ke instance field initializers us class ke constructor body se pehle execute hote hain; derived initializers base-constructor call se bhi pehle execute ho sakte hain. `base(args)` base constructor select karta hai; `this(args)` isi class ke doosre constructor ko chain karta hai.

**Pitfall:** Base constructor se virtual method call karne par derived override derived constructor body se pehle chal sakta hai; derived state abhi ready na ho. Aisa virtual dispatch constructors mein avoid karein.

**Follow-ups**
- **Agar `base(...)` omit ho?** Compiler implicit parameterless base constructor call karta hai; woh accessible/existent na ho to explicit constructor chain likhni hogi.
- **`this` aur `base` ka farq?** `this` current object/current class constructor ya member ko target karta hai; `base` parent implementation/constructor ko.

### Q5. `async`/`await` thread create karta hai?

**Answer:** Nahin. `async` method compiler-generated state machine banata hai; `await` incomplete asynchronous operation par method ko suspend karke control caller ko lauta sakta hai. I/O-bound kaam ke liye thread block kiye bina scalability milti hai. CPU-bound kaam ke liye alag scheduling strategy chahiye, jaise carefully chosen `Task.Run` in appropriate app types.

**Pitfall:** `.Result`/`.Wait()` se async call block karne par thread starvation/deadlock risk badhta hai. Cancellation token pass karein; fire-and-forget task exceptions/lifetime ko ignore na karein.

**Scenario:** API external service call karti hai. `HttpClient` ka async API await karein, request cancellation token propagate karein, finite timeout aur bounded concurrency set karein.

**Follow-ups**
- **`Task` vs `Thread`?** `Task` asynchronous work/completion ko represent karta hai, zaroori nahin dedicated thread ho; `Thread` execution thread hai.
- **`Task.WhenAll` kya karta hai?** Multiple tasks ko concurrently await karta hai; yeh apne aap concurrency cap nahin karta.

### Q6. GC hai to `IDisposable` kyun chahiye?

**Answer:** GC managed memory ki reachability handle karta hai, par file handles, sockets, database connections jaise deterministic cleanup ke liye `IDisposable`/`IAsyncDisposable` use hota hai. `using`/`await using` scope exit par cleanup guarantee karte hain, including exception path.

**Pitfall:** Finalizer ko normal cleanup mechanism na banayein; finalization nondeterministic hai. Disposed object ko cache karke dobara use na karein. Ownership clear rakhein—resource banane/own karne wala component hi dispose kare.

**Follow-ups**
- **`Dispose` vs finalizer?** `Dispose` deterministic caller-controlled cleanup hai; finalizer last-resort unmanaged-resource safety net hai. SafeHandle aksar raw finalizer logic se safer hai.
- **Memory leak managed code mein kaise hota hai?** Long-lived reference/event subscription/cache accidental reachability banaye rakhe to GC object collect nahin kar sakta.

### Q7. DI lifetimes—Transient, Scoped, Singleton—kya hain?

**Answer:** Transient har resolution par naya instance; Scoped ek scope (web request mein aam taur par ek request) ke andar shared; Singleton application container lifetime tak ek instance. Lifetime object sharing aur disposal behavior decide karta hai.

**Pitfall:** Singleton mein scoped dependency capture karna captive dependency hai: scoped object request boundary ke baad bhi reuse ho sakta hai. Singleton ko thread-safe banana developer ki responsibility hai. Scoped service ko background worker se use karte waqt explicit scope create karein.

**Scenario:** `DbContext` usually scoped hota hai. Singleton cache ko data chahiye ho to scoped service ko constructor mein inject na karein; operation ke dauran scope/factory ke zariye resolve karein.

**Follow-ups**
- **Transient vs Scoped kab?** Stateless lightweight dependencies transient ho sakti hain; request/unit-of-work state ya `DbContext` scoped. Actual behavior aur thread-safety decide kare.
- **Options lifetimes?** `IOptions<T>` stable options access, `IOptionsSnapshot<T>` scoped recalculation, `IOptionsMonitor<T>` change notifications/current value—configuration reload needs ke mutabik.

### Q8. Race condition, lock, `volatile` aur `Interlocked` mein kya farq?

**Answer:** Race condition tab hoti hai jab concurrent operations ka result timing/interleaving par depend kare. Compound shared-state update ko `lock`/Monitor se mutually exclusive banayein; simple atomic counter operation ke liye `Interlocked`; visibility flag ke liye `volatile`/`Volatile` ho sakta hai, lekin `volatile` `count++` ko atomic nahin banata. `SemaphoreSlim` bounded concurrency aur async `WaitAsync` ke liye useful hai.

**Pitfall:** `lock(this)`, public object, `string` ya boxed value type par lock na karein; locks ka inconsistent order deadlock de sakta hai. Critical section short rakhein, slow I/O ke dauran lock na hold karein; `await` ko normal `lock` block mein use nahin kar sakte.

**Follow-ups**
- **Deadlock ki conditions?** Mutual exclusion, hold-and-wait, no preemption aur circular wait. Consistent lock order, short critical sections/timeouts aur unnecessary nested locks se bachna mitigation hai.
- **Mutex vs SemaphoreSlim?** Mutex exclusive ownership aur cross-process use-case de sakta hai; `SemaphoreSlim` in-process count-based concurrency limit aur async wait support karta hai.
- **`Monitor.Wait/Pulse`?** Lock release karke condition ka wait aur waiting thread ko signal; condition ko `while` loop mein re-check karein, kyunki wake-up ka matlab predicate true hona guaranteed nahin.
- **Thread pool starvation kya?** Pool threads sync-block ho jaate hain aur queued work ko workers nahin milte; sync-over-async, blocking I/O aur unbounded work inspect karein.

### Q9. Records, value-based equality, `init`, auto-properties, delegates aur events?

**Answer:** `record` value-oriented equality/printing/copy syntax provide karta hai; normal class ka default equality reference-based hota hai unless equality implement ki jaye. Auto-property compiler-generated backing storage ka concise syntax hai. `init` object initialization ke waqt assignment allow karta hai, par referenced mutable object ko deeply immutable nahin banata. Delegate method reference/type-safe callback hai; `event` publisher ko invocation control deta hai aur subscribers ko subscribe/unsubscribe karne deta hai.

**Pitfall:** Equality ke liye `Equals` aur `GetHashCode` consistent hone chahiye, khaaskar hash collection keys par. Mutable equality fields ko dictionary key mein badalne se lookup break ho sakta hai. Event subscriber unsubscribe na kare to publisher subscriber ko alive rakh sakta hai.

**Follow-ups**
- **Record ka `with` expression?** Shallow copy with selected properties changed; nested referenced objects same instance ho sakte hain.
- **`init` vs `set`?** `init` initializer/constructor context tak assignment restrict karta hai; `set` normal lifetime mein mutate ho sakta hai.
- **`event` vs public delegate?** Public delegate ko caller overwrite/invoke kar sakta hai; event publisher invocation authority protect karta hai, though lifetime cleanup ab bhi zaroori hai.
- **Record class vs record struct?** Record class reference semantics, record struct value semantics; choice type size, identity, copying aur API meaning se karein.

## 2. OOP, SOLID aur design patterns

### Q1. SOLID principles ko practical example se samjhaiye.

**Answer:** SRP: ek module ka ek coherent reason to change; OCP: extension se behavior add ho bina stable code ko baar-baar edit kiye; LSP: subtype apne base contract ko todhe bina replace ho; ISP: clients ko unused methods par depend na karna pade; DIP: high-level policy concrete details ke bajay abstractions par depend kare.

**Pitfall:** SRP ka matlab har method ke liye alag class banana nahin. OCP ka matlab har future possibility ke liye abstractions banana bhi nahin. SOLID trade-offs aur change boundaries ke tools hain.

**Scenario:** Payment providers alag hain. `IPaymentProvider` contract, provider-specific implementations, aur DI registration se provider add ho sakta hai; retry/idempotency jaise shared behavior ko arbitrary inheritance mein force na karein.

**Follow-ups**
- **LSP ka penguin example?** Agar `Bird` base type `Fly()` promise karta hai, penguin subtype us contract ko fulfill nahin kar sakta. `Bird` ko flying behavior se alag model karein, jaise `IFlyable`.
- **DIP aur DI same hain?** Nahin. DIP design principle hai; DI dependencies supply karne ki technique/framework capability hai.
- **Interface vs abstract class?** Interface capability/contract aur multiple implementation composition deta hai; abstract class shared state/default implementation aur single class inheritance. Actual shared invariants aur evolution needs se decide karein.
- **Composition over inheritance kyun?** Components ko runtime par combine/replace karna asaan; rigid base-class coupling kam. Genuine substitutable is-a relationship ho to inheritance valid.

### Q2. Factory, Strategy, Decorator, Adapter aur Repository kab use karenge?

**Answer:** Factory object creation decision ko encapsulate karta hai; Strategy interchangeable algorithm select karta hai; Decorator existing contract ke around behavior layer karta hai; Adapter incompatible interfaces ko bridge karta hai; Repository persistence details ko domain-facing abstraction ke peeche rakh sakta hai.

**Pitfall:** Pattern ka naam use karna solution ka proof nahin. Simple `switch` ya direct composition sahi ho sakta hai. Generic Repository se EF Core ke useful query capabilities ko chhupakar complexity bhi badh sakti hai.

**Scenario:** HTTP call par logging, retry aur metrics lagane ke liye decorator/handler pipeline; payment type ke hisaab se algorithm badalne ke liye Strategy.

**Follow-ups**
- **Decorator vs inheritance?** Decorator runtime composition se behavior stack kar sakta hai; inheritance static type hierarchy banata hai aur tight coupling ka risk rakhta hai.
- **Singleton pattern ka risk?** Hidden global state, testing difficulty, concurrency aur lifecycle issues. DI container se singleton lifetime register karna aksar explicit hota hai.
- **Strategy vs State?** Strategy mein client algorithm select karta hai; State mein object's internal mode uske behavior/valid transitions ko control karta hai.
- **Adapter vs Bridge?** Adapter existing incompatible interfaces ko connect karta hai; Bridge design ke waqt abstraction aur implementation ke do dimensions ko alag karta hai.
- **Decorator vs Proxy?** Decorator same interface ke through behavior add karta hai; Proxy access/lifecycle/network boundary control karta hai.
- **Command vs Strategy?** Command action/request ko object bana sakta hai (queue, log, undo); Strategy interchangeable algorithm represent karta hai.
- **Factory Method vs Abstract Factory?** Factory Method ek product creation ko defer/override karta hai; Abstract Factory related product family create karta hai.
- **Modern C# pattern alternatives?** DI lifetimes Singleton, delegates/keyed services Strategy/Factory, middleware/handlers chain/decorator, events/Observable Observer, `record with` copy-style patterns ko express kar sakte hain. Built-in use clarity de to manual pattern dobara na banayein.

### Q3. Bird/Penguin example se Liskov Substitution Principle (LSP) samjhaiye.

**Answer:** Agar caller `Bird` ko use karke `Fly()` expect karta hai, to `Penguin : Bird` ko aise method ka exception throw karke contract todna nahin chahiye. Base type ka behavior contract sab subtypes ke liye valid hona chahiye. Better model `Bird` ko general behavior tak rakhe aur `IFlyable` sirf udne wale birds implement karein.

**Pitfall:** Sirf method signatures compatible hona kaafi nahin; preconditions stronger, postconditions weaker, ya invariants break karne wala subtype bhi LSP violate kar sakta hai.

**Follow-ups**
- **Kya inheritance hamesha galat hai?** Nahin; jab genuine substitutability aur stable is-a relationship ho to useful hai. Shared implementation ke liye composition bhi consider karein.
- **Testing kaise?** Base contract ke tests ko har implementation/subtype par run karein.

### Q4. LLD interview problem ko kaise approach karenge? Logger, file system, vending machine mein kya design hoga?

**Answer:** Pehle scope aur functional/non-functional requirements clarify karein; nouns se entities identify, responsibilities/relationships draw, variation points par pattern choose, phir edge cases/thread-safety discuss karke ek scenario walkthrough karein. Assumptions bolkar rakhna premature coding se behtar hai.

- **Logger:** `LogMessage` immutable data; threshold filtering; `ILogFormatter` strategy; multiple `ILogAppender` sinks; thread-safe writes. Async appender ho to bounded queue, backpressure/drop policy, flush aur graceful shutdown define karein.
- **File system:** `File` leaf aur `Directory` composite dono `IFileSystemEntry` jaise shared contract expose kar sakte hain; directory children ko recursively contain kare. Parent/child updates, duplicate names, permissions, deletion semantics aur cycles prevent karna define karein.
- **Vending machine:** State pattern se `Idle`, `HasCredit`, `Dispensing`, `OutOfStock` transitions; inventory/payment/refund alag responsibilities. Invalid transitions, insufficient balance, change, concurrent purchase aur dispense failure handle karein.
- **Thread-safe cache:** Concurrent map ke saath expiration/eviction aur cache-stampede policy define karein; sirf collection thread-safe hone se compound check-then-create operation atomic nahin ban jata.
- **Rate limiter:** Token bucket/fixed ya sliding window jaise algorithm, per-client partition key, clock/concurrency semantics, limit response aur distributed deployment behavior specify karein.
- **Parking lot:** Vehicle/spot/ticket/payment responsibilities; concurrent spot allocation atomic ho, entry/exit idempotent ho, aur spot type/pricing rules explicit hon.

**Pitfall:** Pattern ko problem samajhne se pehle impose na karein. Logger ko static global state banana testability/configuration hurt kar sakta hai; vending machine ke states ko booleans ke combination se represent karne par invalid states badh sakte hain.

**Follow-ups**
- **State vs Strategy?** Strategy algorithm ko caller/context se interchangeable banata hai; State object ke internal state ke saath behavior aur allowed transitions badalta hai.
- **Composite kyun?** Leaf aur group ko common interface se treat karke tree recursion simple hoti hai; par leaf par nonsensical child operations ka contract carefully model karein.
- **Async logger queue full ho?** Requirement ke mutabik block, drop, sample, ya fallback; bounded capacity, dropped-log metric aur shutdown flush. Silent unbounded queue memory risk hai.
- **Design thread-safe kaise prove karenge?** Shared mutable state enumerate karein, synchronization boundary/invariant define karein, races/interleavings test karein; concurrency add karne se pehle single-threaded behavior correct rakhein.

## 3. ADO.NET, LINQ aur data access

### Q1. `DataReader`, `DataSet` aur `DataAdapter` mein kya farq hai?

**Answer:** `DataReader` forward-only, read-only aur connected/streaming style access deta hai; large results aur low overhead ke liye suitable. `DataSet` disconnected in-memory relational data representation deta hai, multiple tables/relations ke saath, par memory-heavy ho sakta hai. `DataAdapter` database aur `DataSet`/`DataTable` ke beech fill/update bridge hai.

**Pitfall:** Reader open ho to connection resource occupied rehta hai; jaldi consume/dispose karein. Disconnected `DataSet` ko har modern application ke liye default na samjhein.

**Follow-ups**
- **`ExecuteNonQuery`, `ExecuteScalar`, `ExecuteReader`?** Respectively affected-row/command result, first row ka first value, aur row stream return karte hain. `ExecuteNonQuery` ka row count har provider/statement mein business result ka reliable measure nahin.
- **Connection pooling kya karta hai?** Closed logical connections ko provider pool mein reuse karta hai; connection ko promptly dispose/close karna pool return karne ke liye zaroori hai.
- **ADO.NET vs Dapper vs EF Core?** ADO.NET maximum direct control; Dapper lightweight SQL mapping; EF Core change tracking/relationships/LINQ productivity. Query complexity, performance evidence, maintainability aur team skill ke hisaab se choose karein.
- **`SqlBulkCopy` kab?** SQL Server mein large row batches efficiently load karne ke liye; batch size, transaction, mapping, validation aur partial-failure recovery define karein.
- **MARS kya?** Multiple Active Result Sets ek connection par kuch overlapping result-set operations allow karta hai; yeh parallelism/transaction design ka substitute nahin aur open reader ko manage karne ki zaroorat khatam nahin karta.
- **Async/cancellation?** Provider ke async methods ko await karke `CancellationToken` propagate karein; async API thread ko block na karne mein help karti hai, query automatically faster nahin hoti.

### Q2. SQL injection se kaise bachenge?

**Answer:** User input ko SQL text mein concatenate na karein; parameterized commands use karein. Parameters values ko data ke roop mein bhejte hain, query syntax ke roop mein nahin. Dynamic table/column/order-by identifiers parameterize nahin hote, isliye strict allow-list mapping chahiye.

**Pitfall:** Escaping ya client-side validation parameterized SQL ka substitute nahin. Stored procedure bhi unsafe ho sakti hai agar woh dynamic SQL concatenate kare.

**Scenario:** Search API mein sort field client deta hai; accepted values ko known column expressions ke allow-list se map karein, user string ko SQL identifier ki tarah append na karein.

**Follow-ups**
- **Transaction kyun?** Related updates ko atomic consistency boundary mein rakhne ke liye. Transaction scope minimum rakhein aur external network calls ko DB transaction ke andar na rokein.
- **Isolation level?** Read phenomena aur concurrency/locking trade-off choose karta hai; use-case aur DB behavior ke mutabik test karein.

### Q3. LINQ deferred execution kya hai?

**Answer:** `Where`/`Select` jaise operators aksar query describe karte hain; enumeration (`foreach`, `ToList`, `Count`) par execution hoti hai. Isse pipeline compose hoti hai, lekin source badalne par baad ki enumeration alag result de sakti hai.

**Pitfall:** Query ko baar-baar enumerate karna duplicate database round trips ya repeated expensive work kar sakta hai. Closure variable ko query banate waqt snapshot nahin kiya jata; execution ke samay captured value use ho sakti hai.

**Follow-ups**
- **`IEnumerable<T>` vs `IQueryable<T>`?** `IEnumerable` in-process enumeration abstraction; `IQueryable` expression tree provider ko query translate karne deta hai—misal ke liye EF Core SQL. `IQueryable` ko arbitrary layer tak expose karna provider coupling de sakta hai.
- **`Select` vs `SelectMany`?** `Select` per input ek result/map; `SelectMany` nested sequences flatten karke zero/many results deta hai.
- **`First` vs `Single`?** `First` kam-se-kam ek matching item maangta hai; `Single` exactly ek. Data invariant enforce karna ho to `Single` helpful; expected absence ko `FirstOrDefault` handle karta hai.

### Q4. LINQ query slow hai—kahan se shuru karenge?

**Answer:** Pehle query execution boundary aur generated SQL/plan dekhein, phir data volume, indexes, projection, joins aur enumeration count. Sirf zaroori columns select karein; DB-side filtering rakhein; repeated enumeration aur nested `Any`/`Where` se accidental O(n²) patterns identify karein.

**Pitfall:** `ToList()` ko har operator ke baad daalna materialize/memory cost badhata hai; lekin boundary par ek baar materialize karna repeated query se sasta ho sakta hai. Benchmark/profile karein.

**Follow-ups**
- **`GroupBy` vs `ToLookup`?** `GroupBy` deferred grouping sequence deta hai; `ToLookup` immediately reusable lookup materialize karta hai.
- **PLINQ kab?** Independent CPU-bound work, enough data aur safe ordering/side effects ho tab measure karke; I/O-bound calls ko `AsParallel()` se solve nahin karte.

## 4. ASP.NET Core aur Web API

### Q1. Middleware pipeline kya hai aur order kyun important hai?

**Answer:** Middleware request ko order mein process karta hai; har component next middleware invoke, short-circuit, ya response modify kar sakta hai. Response aam taur par unwind hote waqt reverse direction mein pass hota hai. Exception handler, HTTPS, routing, CORS, authentication, authorization aur endpoints ka sahi order behavior badalta hai.

**Pitfall:** Authentication se pehle authorization chalane, endpoint routing/CORS ko galat jagah rakhne, ya broad middleware se exception chhupane se security/functionality bugs ho sakte hain.

**Follow-ups**
- **`Use`, `Run`, `Map`?** `Use` aage call kar sakta hai; `Run` terminal middleware hai; `Map` path branch karta hai. `MapWhen` condition-based branch deta hai.
- **Middleware vs filter?** Middleware broad HTTP pipeline concern hai; MVC filters action/controller execution pipeline ke specific stages par kaam karte hain.
- **Exception response?** Central exception handling + consistent Problem Details; internal stack/secret client ko leak na karein.

### Q2. REST API mein idempotency aur HTTP status codes kaise choose karte hain?

**Answer:** Idempotent request ko identical retries se final server state repeated execution ke bawajood same rehna chahiye. `GET`, `PUT`, `DELETE` semantics idempotent hote hain; `POST` aam taur par nahin, jab tak idempotency-key strategy implement na ho. Status code actual outcome bataye—`201` created, `202` accepted for async work, `204` no content, `400` invalid request, `401` unauthenticated, `403` forbidden, `404` absent, `409` conflict, `429` rate limited.

**Pitfall:** Retry logic ko non-idempotent payment/order action par bina dedupe key ke lagana duplicate effects de sakta hai. `401` aur `403` ko mix na karein.

**Scenario:** Client POST ka response lose kar deta hai aur retry karta hai. Tenant + operation ke scope mein idempotency key aur request hash store karein; same key/different payload reject karein; record retention define karein.

**Follow-ups**
- **PUT vs PATCH?** PUT resource representation ko replace/desired state set karta hai; PATCH partial modifications. PATCH format aur operations ko explicitly define/validate karein.
- **Offset vs cursor pagination?** Offset simple random page access deta hai, par bade offsets slow aur concurrent inserts se unstable; cursor large/changing datasets mein stable continuation de sakta hai, lekin opaque token/ordering contract chahiye.
- **`202 Accepted` kab?** Work accept hua hai, complete nahin. Status polling/resource URL aur eventual failure contract dein.

### Q3. Minimal APIs ya Controllers—kaunsa choose karenge?

**Answer:** Dono ASP.NET Core endpoints banate hain. Minimal APIs chhote/ focused services ke liye concise hain; controllers conventions, filters, grouping aur established MVC organization de sakte hain. Team familiarity, API complexity, validation/versioning/testing needs se choose karein.

**Pitfall:** Minimal API ka matlab automatically faster/cleaner architecture nahin. Endpoint par business logic bharna aur controller mein sab kuch jamana dono avoid karein.

**Follow-ups**
- **Kestrel kya hai?** ASP.NET Core ka cross-platform web server. Production mein reverse proxy/load balancer TLS termination, forwarding aur edge policies ke liye saath ho sakta hai; trust forwarded headers sirf trusted proxies se.
- **`IHttpClientFactory` kyun?** Handler pooling, named/typed clients aur policies manage karke socket exhaustion/DNS staleness risks address karta hai; har call par `new HttpClient()` se bachata hai.
- **Rate limiting aur throttling?** Per-client/user/tenant policy apply karein, suitable algorithm/partition choose karein, `429` aur `Retry-After` bhejein; gateway aur app layers ki responsibility clear karein.

### Q4. Production API ko resilient kaise banayenge?

**Answer:** Timeouts, cancellation propagation, bounded retries with backoff+jitter, circuit breaker, bulkhead/concurrency caps, rate limits, health checks aur observability combine karein. Retry sirf transient failures aur idempotent/replay-safe operations par; dependency-specific policy rakhein.

**Pitfall:** Retry + timeout + queue redelivery multiply hokar request storm bana sakte hain. Circuit breaker ek endpoint/dependency boundary par ho to wahi calls affect hote hain; base URL ke har endpoint par effect sirf tab hoga jab breaker shared handler/policy mein common ho.

**Follow-ups**
- **Circuit breaker states?** Closed requests pass; threshold ke baad Open fail-fast; Half-open limited probes se recovery test karta hai.
- **Health check vs readiness?** Liveness batata hai process ko restart karna hai ya nahin; readiness batata hai traffic receive karne ke liye prepared hai ya nahin. Dependency failure ko liveness mein rakhna restart loop bana sakta hai.

## 5. EF Core

### Q1. Tracking aur `AsNoTracking()` mein farq?

**Answer:** Tracking query entities ko `DbContext` change tracker mein attach karke identity resolution/change detection deti hai; update workflow mein useful. Read-only query ke liye `AsNoTracking()` tracking overhead kam karta hai. `AsNoTrackingWithIdentityResolution()` read-only results mein duplicate entity instances resolve kar sakta hai.

**Pitfall:** Read result ko modify karke save karna ho to no-tracking entity automatic update nahin hogi. `DbContext` thread-safe nahin; concurrent operations mein same context share na karein.

**Follow-ups**
- **N+1 problem?** Parent query ke baad har item ke liye child query chalna. Projection, explicit/eager loading, batching ya split query se fix karein; SQL count/plan se verify.
- **Eager vs lazy loading?** Eager related data query mein laata hai; lazy access par baad mein; explicit developer-controlled separate query. Lazy loading hidden round trips de sakta hai.
- **Split query?** Multiple related collections ko separate queries mein load karke cartesian explosion kam kar sakti hai; consistency/extra round-trip trade-off check karein.

### Q2. EF Core migration aur production schema updates kaise manage karenge?

**Answer:** Schema changes ko reviewed, version-controlled migration ke roop mein rakhein; deployment se pehle script/generation inspect karein. Expand-contract sequence: backward-compatible columns/additions, app dual-read/write ya backfill, phir old schema removal alag release mein.

**Pitfall:** Production startup par sab replicas ko migration race mein chalana, long blocking migration, ya rollback plan ke bina destructive schema change risky hai. Large table changes ko staged/backfill karein.

**Follow-ups**
- **Optimistic concurrency?** Rowversion/concurrency token update condition mein expected version verify karta hai; conflict par merge/retry/user resolution policy chahiye.
- **Compiled queries?** Repeated stable query shape mein overhead kam kar sakte hain; pehle profile karein, kyunki SQL/index/network cost aksar zyada matter karti hai.

## 6. Authentication, authorization aur application security

### Q1. Authentication, authorization, OAuth2, OIDC aur JWT ka relation?

**Answer:** Authentication user/client ki identity establish karta hai; authorization us identity ko action/resource ke liye permission deta hai. OAuth 2.0 delegated authorization framework hai; OpenID Connect uske upar identity layer hai. JWT ek token format hai—OAuth/OIDC nahin. API signature, issuer, audience, expiry aur allowed algorithm validate kare.

**Pitfall:** JWT ko encrypt samajhna galat; signed JWT payload readable ho sakta hai. Token decode karna validation nahin. `alg=none`, algorithm confusion, wrong audience/issuer aur weak key checks critical mistakes hain.

**Follow-ups**
- **Role vs policy?** Role coarse group membership; policy claims/resource/context ke multiple requirements express kar sakti hai. Resource-based check mein target resource load ke baad authorization karein.
- **OAuth flow?** User-facing public SPA/mobile clients Authorization Code + PKCE use karte hain; machine-to-machine mein client credentials. Implicit/password grants avoid/deprecated patterns hain; provider guidance verify karein.
- **SAML vs OIDC?** Dono federation/SSO standards; OIDC modern web/mobile JSON/HTTP ecosystem mein aam, SAML enterprise browser SSO mein ab bhi milta hai. IdP/client capability aur integration requirements decide karte hain.

### Q2. Access token aur refresh token ko kaise secure karte hain?

**Answer:** Access token short-lived aur API ke liye; refresh token naya access token lena. Refresh token rotation, reuse detection, revoke/logout workflow, secure storage, narrow audience/scope aur TLS implement karein. Browser app ke liye BFF + HttpOnly/Secure/SameSite cookie pattern token ko browser JavaScript se door rakh sakta hai.

**Pitfall:** Long-lived bearer token ko `localStorage` mein rakhna XSS se chori ka risk rakhta hai; cookie-based auth mein CSRF threat model bhi handle karein. JWT revocation instant nahin hoti—short expiry, denylist/version check, opaque-token introspection jaise trade-offs hain.

**Follow-ups**
- **CSRF vs XSS?** CSRF browser ko automatically attached credential ke saath unwanted request karwata hai; XSS attacker ko origin mein script chalane deta hai. HttpOnly cookie XSS token reading kam karta hai, par XSS request issue kar sakta hai.
- **API key vs OAuth client credentials?** API key ko simple identification/throttling ke liye use kiya ja sakta hai; robust machine identity, scopes, token expiry/rotation ke liye OAuth client credentials behtar hota hai.
- **Multi-tenancy?** Tenant identity trusted auth context se resolve karein; har query/cache/key/object authorization tenant scope enforce kare. Client-supplied tenant id ko blindly trust na karein.

### Q3. WebAuthn/passkeys aur mTLS kya solve karte hain?

**Answer:** Passkeys/WebAuthn public-key challenge-response authentication dete hain aur phishing-resistant ho sakte hain; mTLS transport layer par client certificate se service/client identity verify karta hai. DPoP sender-constrained token ka ek aur approach hai.

**Pitfall:** In standards ko sirf checkbox na banayein: certificate lifecycle/rotation, device recovery, origin binding, account recovery aur fallback flows design karne hote hain.

## 7. SQL Server

### Q1. Index seek vs scan aur SARGability samjhaiye.

**Answer:** Seek index key ke relevant range tak pahunch sakta hai; scan index/table ke zyada rows read karta hai. SARGable predicate index access ko enable karne layak hota hai—indexed column par function/cast lagane se seek ruk sakta hai. Actual execution plan, estimated/actual rows, reads aur workload se decision karein.

**Pitfall:** Har query mein index add karna write/storage cost badhata hai. Seek hamesha fast aur scan hamesha slow nahin—chhoti table/large result ke liye scan behtar ho sakta hai.

**Follow-ups**
- **Covering index?** Query ke filter/join/projection columns index mein available hon, so base-table lookup bach sakta hai; key/include columns carefully select karein.
- **Statistics?** Optimizer ko data distribution/cardinality estimate karne mein help karte hain. Stale/misleading stats wrong plan de sakte hain.
- **Parameter sniffing?** Compile ke waqt parameter value ka plan reuse ho kar doosre value distributions par poor ho sakta hai. Query Store, plan/parameter distribution analysis ke baad targeted solution choose karein; blanket recompilation nahin.

### Q2. Transactions, ACID aur isolation levels ka role?

**Answer:** Atomicity all-or-nothing; consistency constraints valid rakhti hai; isolation concurrent transactions ke effects control karti hai; durability committed data preserve karti hai. Isolation level dirty/non-repeatable/phantom reads ke risk aur locking/versioning/concurrency cost ke beech trade-off hai.

**Pitfall:** Long transactions locks/version store hold karte hain; external service call ko DB transaction ke beech wait na karayein. Deadlock mein victim transaction retry sirf safe/idempotent operation par karein.

**Follow-ups**
- **RCSI vs Snapshot?** Dono row versioning se readers/writers blocking kam kar sakte hain. RCSI statement-level snapshot; Snapshot transaction-level consistent view. Tempdb/version-store overhead aur app semantics validate karein.
- **DELETE vs TRUNCATE vs DROP?** DELETE rows remove karta hai aur predicate/row logging semantics; TRUNCATE table ke all rows deallocate/reset behavior ke saath; DROP object/schema remove karta hai. Permissions, FK, identity aur transaction behavior specifics check karein.
- **`WHERE` vs `HAVING`?** `WHERE` grouping se pehle rows filter karta hai; `HAVING` grouped/aggregate result filter karta hai.
- **`ROW_NUMBER`, `RANK`, `DENSE_RANK`?** `ROW_NUMBER` unique sequential ordinal; `RANK` ties share rank aur gap; `DENSE_RANK` ties share rank bina gap. Nth-highest distinct salary ke liye salary par `DENSE_RANK()` karke desired rank filter karein.
- **`UNION` vs `UNION ALL`?** `UNION` duplicates remove karta hai (extra sort/hash cost ho sakta hai); `UNION ALL` rows preserve karta hai.
- **Normalization kyun?** Update/insert/delete anomalies reduce karne ke liye data ko related tables mein structure karna; read/query workload ki measured need par denormalize kar sakte hain.

### Q3. SQL query production mein slow ho gayi—kya karenge?

**Answer:** Reproduce/identify query and parameter, Query Store/history, actual plan, wait stats, logical reads, CPU/elapsed time aur blocking/deadlocks inspect karein. Cardinality, missing/inefficient indexes, non-SARGable expressions, implicit conversion, spills/tempdb aur parameter sensitivity check karein; change ke baad representative workload se verify.

**Follow-ups**
- **CTE vs temp table?** CTE query structuring/recursion syntax hai; temp table intermediate data persist/index/statistics kar sakti hai. Optimizer/materialization behavior assume na karein—plan dekhkar choose karein.
- **OLTP vs OLAP?** OLTP frequent small transactional reads/writes; OLAP large analytical scans/aggregations. Schema/index/storage trade-offs alag.
- **Deadlock mitigation?** Access order consistent, transaction short, appropriate indexes, batch size/lock hints only evidence ke baad; deadlock graph analyze karein.

### Q4. SQL interview query drills ko kaise solve aur explain karenge?

**Answer:** Pehle output grain, tie behavior, NULL handling, duplicate policy aur boundary conditions clarify karein. Phir CTE/window function/`EXISTS` choose karein; sample data par dry run aur deterministic `ORDER BY` se result verify karein.

| Prompt | Reliable pattern | Important edge case |
|---|---|---|
| Department ke top salary tiers / top N per group | `DENSE_RANK()` for distinct salary tiers; `ROW_NUMBER()` with a deterministic tie-breaker for exactly N rows; partition by department. | “Top 2 salaries” vs “exactly 2 employees” alag outputs hain. |
| Median | `PERCENTILE_CONT(0.5)` where supported, ya ordered rows ke middle one/two positions ka average. | Even row count, NULLs, and continuous vs discrete median define karein. |
| Recursive org chart | Recursive CTE with anchor employee and manager-child join. | Cycle guard/depth limit aur missing manager/orphan rows. |
| Gaps-and-islands | `ROW_NUMBER()` sequence subtraction/group key, phir contiguous ranges group. | Duplicate dates/IDs, missing values, and definition of adjacency. |
| Sessionization | `LAG(event_time)` per user; gap threshold par boundary flag; cumulative `SUM` se session ID. | Equal timestamps, late events, timezone and threshold boundary. |
| Relational division (“all required skills/items”) | `NOT EXISTS` of a required row missing for each candidate, or grouped distinct count. | Duplicate bridge rows, empty required set, and NULL semantics. |
| Pivot / conditional summary | `SUM(CASE WHEN category=... THEN amount ELSE 0 END)`; `PIVOT` when fixed columns suit. | Dynamic categories and missing categories may need dynamic SQL or a stable output shape. |
| Deduplicate rows | `ROW_NUMBER()` partitioned by business key, ordered by keep-priority then unique key; inspect duplicates before delete. | Define canonical winner; protect deletion in a transaction and preserve audit needs. |
| Missing invoice numbers / gaps | Generate expected range with a numbers table/series, then anti-join actual invoices. | Do not infer missing values by `MAX(id)` alone; large ranges need bounded generation. |
| Revenue growth | Aggregate revenue by period first, then `LAG()` and guarded percentage formula. | Previous period zero/missing, calendar gaps, refunds and currency normalization. |

**Pitfall:** Query ko sample data ke mutabik hard-code karna; ties/duplicates ko ignore karna; window result ko filter karne ke liye same-level `WHERE` use karna; ya delete/update query ko pehle SELECT se verify na karna.

**Scenario:** “Har department ke top 2 distinct salary tiers” ke liye CTE mein `DENSE_RANK() OVER (PARTITION BY DepartmentId ORDER BY Salary DESC)` nikaal kar outer query mein rank `<= 2` filter karein; employees ko count karna ho to `ROW_NUMBER()` aur explicit tie rule choose karein.

**Follow-ups**
- **Median ka simpler portable version?** `ROW_NUMBER()` plus `COUNT(*) OVER()` se middle position(s) select karke average; exact SQL dialect and NULL policy batayein.
- **Gaps-and-islands mein row-number trick kyun?** Consecutive values ke liye value minus row number same island key deta hai; non-unit intervals mein pehle normalized sequence/threshold define karna padega.
- **Safe dedup kaise?** Candidate rows and survivor rule preview; transaction/batch; unique constraint after cleanup; archive/audit and concurrent-write handling.

## 8. Microservices, messaging aur migration

### Q1. Monolith ko microservices mein kab split karna chahiye?

**Answer:** Team/domain boundaries, independent deployment/scaling, ownership, change coupling aur operational maturity ko measure karke. Microservices tab worthwhile jab autonomy/scale benefit network, data consistency, observability, deployment aur on-call complexity se zyada ho.

**Pitfall:** "Microservices always scale better" ya database tables ke hisaab se split karna. Distributed monolith mein synchronous call chains aur shared database coupling bachi reh sakti hai.

**Scenario:** Strangler Fig: edge/gateway se selected capability ko nayi service route karein, old monolith functional rahe; traffic/data gradually migrate, reconcile, monitor, rollback path rakhein.

**Follow-ups**
- **Bounded context?** Domain model aur language ki boundary; service ownership candidate. Har bounded context ko alag deployable service banana mandatory nahin.
- **Database per service?** Data ownership/coupling kam hoti hai; cross-service queries/events aur eventual consistency handle karni padti hai.
- **Kubernetes zaroori hai?** Nahin; operational needs ke mutabik managed containers, serverless ya simpler platform kaafi ho sakte hain.

### Q2. Saga, Outbox aur idempotency kaise milkar kaam karte hain?

**Answer:** Saga multi-service business transaction ko local transactions + events/commands mein todta hai; failure par compensating action business effect ko reverse/offset karta hai. Transactional Outbox business update aur event record ko ek local DB transaction mein commit karta hai; relay baad mein publish karta hai. Consumer Inbox/dedupe key duplicate delivery ko safe banata hai.

**Pitfall:** "Exactly once" transport claim par depend na karein; practical system at-least-once delivery + idempotent processing design kare. Compensation hamesha literal rollback nahin; email bhejna ya payment settle karna reversible nahin ho sakta.

**Follow-ups**
- **Choreography vs orchestration?** Choreography mein services events par react karti hain; loose coupling par flow visibility mushkil. Orchestrator central workflow coordinate karta hai; visibility achhi, coordinator ownership/coupling ka trade-off.
- **DLQ ka use?** Repeatedly failing message ko isolate, inspect, alert aur controlled replay karne ke liye; DLQ ko silent graveyard na banayein.
- **Ordering?** Partition/aggregate key se related messages order kar sakte hain; global ordering mehngi/limited hoti hai. Version/sequence aur stale-event handling add karein.

### Q3. Queue, pub-sub aur event stream kab choose karte hain?

**Answer:** Queue work items ko consumers mein distribute karti hai; pub-sub event ko multiple subscribers tak fan-out karta hai; retained event stream ordered partitioned log hai jise consumers offset se independently read/replay karte hain. Selection retention, fan-out, ordering, throughput aur consumer replay need par.

**Pitfall:** SNS ko Saga coordinator samajhna, ya message broker ko durable business state ka automatic replacement samajhna. Visibility timeout, retention, redrive, DLQ, duplicate delivery aur poison messages configure/monitor karein.

**Follow-ups**
- **SQS Standard vs FIFO?** Standard high throughput/at-least-once with possible reordering; FIFO ordering/deduplication features ke saath, par throughput/partition design constraints check karein.
- **EventBridge vs queue?** EventBridge event routing/filtering integrations ke liye; queue consumer buffering/work distribution ke liye. Architecture mein dono saath bhi ho sakte hain.
- **Backpressure?** Consumer capacity ke hisaab se producer flow/buffer/concurrency manage karein; queue depth/age alerts aur bounded retries rakhein.

### Q4. Zero-downtime data migration kaise karenge?

**Answer:** Compatibility plan banayein; schema expand karein; backfill/bulk copy ya CDC se data sync; dual-read/write sirf carefully; checksums/counts/business invariants se reconcile; canary/cutover; old path tab remove jab rollback window aur verification pass ho.

**Pitfall:** Dual-write without source-of-truth/ordering/retry strategy divergence la sakta hai. CDC lag, duplicate events, replay idempotency aur cutover rollback criteria explicitly handle karein.

## 9. System design aur performance

### Q1. System design interview ko structure kaise karenge?

**Answer:** Requirements clarify (users, use cases, SLO, consistency/security), rough capacity estimate, APIs/data model, high-level components, bottlenecks/failure modes, scaling, observability, security aur cost explain karein. Har decision ke trade-offs aur assumptions bolo; interviewer feedback ke baad evolve karein.

**Pitfall:** Seedha technology diagram draw karna; read/write QPS, payload, retention, peak-to-average, latency/availability constraints poochhe bina design karna.

**Follow-ups**
- **Availability vs reliability?** Availability abhi service usable hai ya nahin; reliability time ke saath correct service behavior/probability. SLI, SLO, measurement window aur error budget se quantify karein.
- **CAP?** Network partition ke waqt distributed system consistency aur availability mein trade-off face karta hai; normal conditions mein "bas consistency vs availability choose" kehna incomplete hai.
- **Back-of-envelope?** Units likhein: daily users × actions/user ÷ seconds/day, peak multiplier, bytes/event × retention; estimate ko order-of-magnitude sanity check samjhein.
- **URL shortener design ka skeleton?** Create API short code issue kare, durable mapping store kare, redirect path hot key cache kare; collision, expiry/custom alias, abuse controls aur analytics ko separate discuss karein. Click analytics ko asynchronous stream/queue par bhejna redirect latency bachata hai.
- **Notification service?** Business event → preference/consent check → channel-specific worker/provider → delivery status; retry/DLQ, dedupe, rate limits, user preferences aur provider failover define karein.

### Q2. Cache, replicas, sharding aur consistent hashing ka decision?

**Answer:** Cache hot/read-heavy data aur latency reduce karta hai; read replica read scaling/DR mein help karta hai; partitioning ek database ke andar data layout; sharding data ko multiple nodes par distribute karta hai. Consistent hashing node changes par remapping reduce kar sakta hai. Pehle measured bottleneck aur access pattern establish karein.

**Pitfall:** Cache invalidation/staleness, hot keys, stampede aur tenant leaks ignore na karein. Sharding operational complexity, cross-shard queries/transactions aur rebalancing badhata hai; early shard na karein.

**Follow-ups**
- **Cache aside?** App cache miss par DB read karke cache fill; writes par invalidation/update strategy. TTL only correctness guarantee nahin.
- **Read replica stale?** Replication lag ho sakta hai; read-your-write flow primary/consistent path par route karein.
- **Consistent hashing kab?** Distributed cache/partition membership changes mein limited remapping ka fayda; virtual nodes aur uneven/hot key handling chahiye.
- **Partitioning vs sharding?** Partitioning ek logical database/table ko pieces mein organize karti hai; sharding data ko independently managed database nodes par distribute karta hai. Sharding operational aur cross-shard query cost badhata hai.
- **CAP ka practical use?** Network partition/failure scenario identify karein, phir affected operation ke liye consistency vs availability requirement batayein; theorem ko blanket database label na banayein.

### Q3. Kestrel, load balancer aur reverse proxy ka relation?

**Answer:** Kestrel app web server hai; reverse proxy/load balancer TLS termination, routing, health-based distribution, edge protections aur multiple instances ke samne entry point de sakta hai. Horizontal scale stateless app replicas se; shared session state externalize karein.

**Pitfall:** Forwarded headers ko kisi bhi client se trust karna spoofing risk hai. Thread-pool starvation, sync-over-async, oversized payload aur unbounded concurrency throughput ko throttle karte hain.

**Follow-ups**
- **Rate-limit algorithm?** Fixed window simple but boundary bursts; sliding window smoother; token bucket controlled bursts; leaky bucket steady output. Policy ko use-case se choose karein.
- **Blue-green vs canary?** Blue-green do environments ke beech switch/rollback; canary traffic ka chhota hissa new version ko deta hai aur metrics se progressively expand karta hai.

### Q4. Excel-based tax filing/auditing platform ko kaise design karenge?

**Answer:** Excel plugin ko trusted UI/client samjhein, business authority nahin. BFF/API par user/tenant authorization, schema/template version, file size/type limits, malware scanning, row validation and idempotent submission enforce karein. Import command ko durable job state ke saath queue/worker tak bhejein; parse/validate, domain command and audit result store karein. Query path alag read model/report de sakta hai (CQRS), lekin simple CRUD ko bina need commands/queries mein split na karein.

**Pitfall:** Spreadsheet values ko trusted/atomic maan lena; client-side validation par depend karna; tax calculation/file write ke saath external service/database writes ko one ACID transaction batana; audit log mein sensitive tax data rakhna; ya SQL Server vs Cosmos ko trend ke basis par choose karna.

**Scenario:** User 10,000-row return upload karta hai: API creates an idempotent submission and returns `202` + status URL; worker validates schema/business rules, records row-level errors, stages accepted data, then orchestrates downstream filing/audit steps. Status transitions and outbox events are persisted; duplicate upload returns existing result; retry-safe steps and compensation handle partial completion.

**Follow-ups**
- **Saga kahan?** Multi-service steps with independent commits/side effects and no shared transaction; persist state, retries, idempotency and compensating actions. A single database transaction may be simpler inside one bounded context.
- **CQRS at BFF ka matlab?** BFF exposes command endpoints for state changes and query endpoints/read models for screens/reports; it need not require separate databases or microservices.
- **SQL Server vs Cosmos DB?** Relational constraints, joins, transactions and ad-hoc reporting often favor SQL Server; known partition-key access patterns, scale and flexible documents may favor Cosmos. Compare consistency, query patterns, operational ownership, cost and migration—not “NoSQL is faster.”
- **Auditability?** Append-only, access-controlled event/history with actor, timestamp, correlation/submission ID and schema/rule version; minimize/redact PII and set retention/access policy.

## 10. Logging, monitoring aur observability

### Q1. Structured logging aur correlation/trace IDs kyun?

**Answer:** Structured log message ko template + named properties ke roop mein store karta hai, jisse query/filter reliable hote hain. Correlation ID related request/events ko link karta hai; distributed trace ke TraceId/SpanId request path aur timing ko represent karte hain. Metrics aggregate behavior/SLOs dikhate hain.

**Pitfall:** Full request bodies/tokens/PII log karna, high-cardinality metric labels, duplicate logging aur excessive debug volume cost/privacy risk hai. Log levels aur redaction define karein.

**Follow-ups**
- **Correlation ID vs TraceId?** Correlation ID app/business flow linking value ho sakta hai; TraceId W3C distributed tracing context standard ka hissa. Dono ka format/propagation clear rakhein.
- **Sampling?** High-volume traces/logs mein representative subset retain karein; errors/slow/high-value events ko retain karne ki policy aur SLO implications validate karein.
- **OpenTelemetry?** Vendor-neutral APIs/SDKs/protocols ke zariye traces, metrics, logs instrumentation/export ka ecosystem; backend storage/query product alag hai.

## 11. AWS

### Q1. Lambda vs containers/EC2 kab?

**Answer:** Lambda event-driven, bursty, bounded stateless work ke liye operational burden kam karta hai; containers ECS/Fargate long-running services, custom runtime aur predictable workloads ke liye; EC2 OS/network/runtime control deta hai. Workload duration, cold start, concurrency, state, cost model aur operational skill se choose karein.

**Pitfall:** Lambda mein local disk/process memory ko durable shared state na samjhein; timeout/concurrency/retries/DLQ aur duplicate event handling plan karein. VPC/networking aur cold starts bhi measure karein.

**Follow-ups**
- **Lambda retries?** Trigger par depend karte hain; at-least-once delivery/duplicate execution assume karke idempotency design karein.
- **Fargate vs ECS?** ECS orchestrator hai; Fargate uske serverless compute launch options mein se ek. EKS managed Kubernetes control plane deta hai; team ko Kubernetes capabilities/operational model chahiye.
- **EC2 EBS vs instance store?** EBS network-attached persistent block volume; instance store host-local ephemeral storage, host lifecycle se data loss risk.

### Q2. DynamoDB data model kaise design karenge?

**Answer:** Access patterns pehle likhein; partition/sort key aur secondary indexes un queries ko support karne ke liye design karein. Partition key distribution, hot-key risk, item size, consistency, conditional writes, TTL/streams aur capacity/cost model validate karein.

**Pitfall:** Relational schema ko seedha copy karna ya arbitrary ad-hoc querying expect karna. Single-table design ek option hai, har situation mein requirement nahin.

**Follow-ups**
- **Strong vs eventual read?** Strong read latest committed state guarantee ke liye, supported operations/regions ki constraints ke saath; eventual lower latency/cost/availability trade-offs de sakta hai. Business need ke mutabik choose karein.
- **Conditional write?** Version/attribute condition se concurrent update/uniqueness-style guard implement kar sakte hain; conditional failure ko expected conflict ke roop mein handle karein.
- **S3 consistency/use?** Object storage large durable blobs, static assets, backups/data lake; DB metadata aur access control strategy alag design karein.

### Q3. AWS IAM aur networking kaise secure karte hain?

**Answer:** Least-privilege IAM roles/policies, workload identity, short-lived credentials, explicit trust policy aur audit trails; public/private subnet routes, security groups, NACLs, VPC endpoints aur egress paths ko required flows ke mutabik define karein.

**Pitfall:** Code/repo mein static access keys, wildcard `*` policies, public S3/object exposure, overly broad trust relationship. Secrets rotation aur KMS encryption key policy/permissions test karein.

**Follow-ups**
- **Security group vs NACL?** Security group stateful resource-level virtual firewall; NACL subnet-level stateless rule list. Exact network need ke hisaab se apply karein.
- **CloudWatch kya dekhega?** Metrics, logs, alarms aur dashboards; meaningful SLI/error-rate/latency/queue-age alarms aur actionable runbook link rakhein.
- **ALB vs NLB?** ALB Layer 7 HTTP(S) routing features; NLB Layer 4 high-throughput/TCP/UDP use cases. Protocol, client IP, TLS aur routing requirement decide karti hai.
- **S3 ko secure/reliable kaise rakhenge?** Block Public Access, least-privilege bucket policy, encryption, versioning, lifecycle/retention aur access logging; presigned URL short-lived/narrow scope mein. Cross-region replication business RPO/DR aur cost ke mutabik.
- **VPC endpoint kyun?** Supported AWS service tak private network path; endpoint policy, route/DNS aur security controls phir bhi verify karein.

### Q4. AWS messaging aur Well-Architected trade-offs?

**Answer:** SQS buffering/work distribution; SNS fan-out; EventBridge event routing; Kinesis retained ordered partitioned stream. Well-Architected ke pillars—operational excellence, security, reliability, performance efficiency, cost optimization, sustainability—design review lens hain, product checklist nahin.

**Pitfall:** Service names ratna par delivery, ordering, retention, replay, DLQ, IAM aur cost model na samajhna. CloudFront/Route 53/Organizations/billing/migration services ko problem ke context mein explain karein.

**Follow-ups**
- **CloudFront/Route 53 ka role?** CloudFront edge CDN/cache/security delivery; Route 53 DNS/domain routing and health-check capabilities. Cache invalidation, origin protection, TTL/failover design karein.
- **Multi-account Organizations kyun?** Workload/environment separation, centralized governance/billing and policy controls; account vending, SCPs, identity and logging standards define karein.
- **Cloud migration/cost?** Data transfer bandwidth/window/encryption/integrity, cutover and rollback plan; cost mein compute, storage, requests, data egress, observability and idle capacity include karein.

### Q5. AWS data, IaC aur migration service kaise choose karenge?

**Answer:** Service ko access pattern, durability/consistency, RPO/RTO, throughput, skills and total cost se choose karein. RDS Multi-AZ availability/failover ke liye; read replica read scaling/DR ke liye; Aurora engine/compatibility and operational requirements fit hone par; RDS Proxy connection pooling/burst smoothing mein help kar sakta hai, par connection budget and limits phir bhi model karein. ElastiCache derived/hot data ke liye; Athena S3 data par serverless SQL analytics ke liye. IaC mein team skill/governance ke mutabik CloudFormation ya Terraform; deployment workflow approvals, artifact/version, drift and rollback cover kare.

**Pitfall:** Read replica ko synchronous HA samajhna; cache ko source of truth banana; RDS Proxy ko unlimited connections assume karna; database migration se pehle application compatibility/CDC lag not measure karna; IaC state/secrets expose karna.

**Scenario:** On-prem database cutover: DMS/CDC se initial load + ongoing changes, lag/validation monitor, application writes freeze or controlled cutover, row counts/checksums/business invariants reconcile, then DNS/connection routing switch and rollback window maintain. Large offline transfer/window constraints hon to Snow-family appliance; online file transfer ko DataSync; hybrid file/block access ko Storage Gateway evaluate karein—product limits and current availability verify.

**Follow-ups**
- **Terraform vs CloudFormation?** Terraform multi-provider ecosystem/HCL and state workflow; CloudFormation AWS-native resource model/integration. Team, governance, drift, state security and supportability decide.
- **RDS Multi-AZ vs read replica?** Multi-AZ is availability/failover feature; replica is a separate read target with engine-dependent lag/promotion behavior. Do not send read-after-write traffic to a lagging replica without an explicit consistency strategy.
- **Athena vs RDS?** Athena ad-hoc analytics over object-store data; RDS transactional relational workload. Query frequency, latency, data layout/partitioning and cost matter.

## 12. Angular

### Q1. Standalone components aur NgModules mein kya difference?

**Answer:** Standalone component apni dependencies/imports declare karke feature composition karta hai; modern Angular mein recommended/default direction hai. NgModules older/legacy organization aur compatibility ke liye ab bhi milte hain. Migration incremental ho sakti hai.

**Pitfall:** Standalone ka matlab application mein modules bilkul exist nahin karte ya har component ko one-file mein rakhna hai—aisa nahin. Version-specific APIs ka documentation check karein.

**Follow-ups**
- **`@if`/`@for` kyun?** Built-in template control flow; `@for` mein stable tracking expression define karna DOM efficiency ke liye important hai.
- **`inject()` vs constructor injection?** Dono DI resolve karte hain; `inject()` supported injection context mein functional guards/interceptors/factories aur field initialization mein useful.
- **Signals vs RxJS?** Signals synchronous reactive state/derived UI state ke liye simple; RxJS async streams, cancellation, composition aur event sequences ke liye. Interop possible; use-case ke mutabik.

### Q2. OnPush/change detection, RxJS aur forms kaise explain karenge?

**Answer:** Change detection component tree mein view updates check karti hai; `OnPush` checking triggers ko constrain karke efficiency improve kar sakta hai, especially immutable input/state patterns ke saath. RxJS Observables async stream abstraction hai. Reactive forms programmatic, typed, complex validation/dynamic controls ke liye strong option.

**Pitfall:** OnPush ke saath object ko in-place mutate karke same reference dena UI update expectations tod sakta hai. Subscriptions clean up karein—`async` pipe/signals/lifecycle interop use karein.

**Follow-ups**
- **Subject vs BehaviorSubject?** Subject new subscribers ko future emissions deta hai; BehaviorSubject current/latest value rakhta hai aur new subscriber ko emit karta hai. Shared global state ko indiscriminately service mein rakhna coupling la sakta hai.
- **HTTP interceptor?** Cross-cutting request/response logic jaise auth header/error mapping; token leakage, retry unsafe methods aur recursive requests se bachna.
- **SSR/hydration?** Server HTML render karta hai; hydration client-side app ko existing DOM ke saath attach karta hai. Browser-only APIs aur server/client markup mismatch handle karein.
- **Accessibility?** Semantic HTML, keyboard/focus management, labels, accessible names, contrast, ARIA only when needed; automated check ke saath manual keyboard/screen-reader validation.
- **Typed reactive forms?** Compile-time form-control/value types dete hain; runtime validators phir bhi required. Cross-field validation group-level par rakhein.
- **`track`/lazy loading/`@defer`?** Stable identity se list DOM reuse; route-level lazy load initial bundle kam; `@defer` non-critical view ko condition/trigger par load karta hai. Bundle/network waterfall measure karein.
- **NgRx kab?** Complex shared state, effects/devtools/team conventions justify karein; simple local/UI state ke liye component state/services/signals kam overhead hain.

## 13. TypeScript

### Q1. `any`, `unknown`, `never` aur `void`?

**Answer:** `any` type checking ko bypass karta hai; `unknown` value ko use karne se pehle narrow karwata hai; `never` impossible/unreachable result; `void` function meaningful return value provide nahin karta. Boundary inputs ko `unknown` ke roop mein validate/narrow karna safer hai.

**Pitfall:** TypeScript types compile-time par erase hote hain; interface/runtime validation automatically nahin hoti. API JSON ko type assertion se trusted banana validation nahin.

**Follow-ups**
- **`interface` vs `type`?** Dono object shapes describe karte hain; interface declaration merging/extension use-cases, type unions/intersections/conditional compositions ke liye flexible. Team conventions aur actual feature se choose karein.
- **Union narrowing?** `typeof`, `in`, custom type guards ya discriminant field check. Exhaustive `never` branch future case detect kar sakta hai.
- **`as const` vs `satisfies`?** `as const` literal/readonly inference narrow karta hai; `satisfies` expression ko target shape ke against check karta hai bina inferred type ko broadly replace kiye.
- **Structural vs nominal?** TS structural compatibility members par; branded type nominal-style distinction simulate karta hai, lekin runtime validation ka replacement nahin.

### Q2. Generics, variance aur strict null checks kyun important?

**Answer:** Generics reusable type-safe APIs banate hain; constraints valid operations enforce karte hain. Variance batata hai generic types subtyping mein kis direction compatible hain. `strictNullChecks` missing/null states ko type system mein explicit karta hai.

**Pitfall:** `any`/type assertion se errors chupana, mutable array covariance ke assumptions, `!` non-null assertion aur compiler flags disable karna safety ghata sakta hai.

**Follow-ups**
- **`readonly` ka matlab immutable runtime?** Nahin; TypeScript compile-time access restriction. Runtime object ko freeze/defensive copy alag concern.
- **Enums?** Runtime representation aur numeric reverse mapping/bundle details hote hain; literal unions/`as const` alternatives simple APIs mein suitable ho sakte hain.

## 14. GitHub Actions, deployment aur Artifactory

### Q1. Secure CI/CD pipeline kaise banayenge?

**Answer:** Trigger par least-privilege jobs; deterministic restore; build, tests, lint/security checks, artifact/package creation, provenance/versioning; immutable artifact ko environment approvals ke through promote karein. Deploy ke baad health/SLO verify, rollback/roll-forward strategy rakhein.

**Pitfall:** PR workflows ko secrets dena, `pull_request_target` mein untrusted checkout/code execute karna, floating third-party action tags, broad `GITHUB_TOKEN` permissions, long-lived cloud secrets aur mutable artifact tags risky hain.

**Follow-ups**
- **OIDC kyun?** GitHub job ko cloud provider ke saath short-lived federated credential exchange karne deta hai; static key store/rotate karne ki need kam. Trust conditions repo, branch/environment aur audience tak restrict karein.
- **Cache vs artifact?** Cache performance optimization, evict ho sakti hai; artifact workflow output ko reliably next job/run tak transfer/store karne ke liye.
- **Reusable workflow vs composite action?** Workflow jobs/orchestration reuse; composite action steps ko package/reuse karta hai.
- **Self-hosted runner risk?** Persistent machine compromise/lateral movement; isolation, ephemeral runners, network restrictions aur patching zaroori.

### Q2. Blue-green, canary, rolling aur feature flags?

**Answer:** Rolling instances ko batches mein update; blue-green parallel environments switch; canary small traffic new build ko; feature flag deployment se code release aur user exposure alag karta hai. DB schema backward-compatible, observability aur rollback path sab mein zaroori.

**Pitfall:** Flag stale rehne se branching complexity; percentage rollout ko health metrics ke bina automatic safe na samjhein. Rollback ke baad data/schema compatibility check karein.

### Q3. JFrog Artifactory/repository manager ka role?

**Answer:** Artifactory build artifacts aur packages ko proxy, cache, host, version aur promote kar sakta hai. Local/remote/virtual repository choices, retention, immutable release, access control, provenance/SBOM aur vulnerability scanning pipeline ke saath define karein.

**Pitfall:** Release ko mutable `latest` label se identify karna, untrusted remote packages ka blind consumption, credentials leakage, aur retention/cleanup policy na hona.

### Q4. .NET service ke liye production Docker image kaise banayenge?

**Answer:** Multi-stage Dockerfile: SDK stage restore/build/publish; minimal ASP.NET runtime stage mein sirf published output. Layer/cache order stable rakhein, pinned base image/digest and vulnerability scanning use karein, non-root user, explicit port/health behavior, resource limits, graceful shutdown and logs to stdout/stderr. Secret BuildKit/CI secret mount se build-time par pass karein, image layer/ARG mein bake na karein.

**Pitfall:** SDK/compiler/source/test files runtime image mein bhejna; root user, floating base tag, secrets in `ENV`/`ARG`, root filesystem writable, oversized image, app listening only on loopback, ya container restart ko health strategy maan lena.

**Scenario:** CI image build/test/scan karke immutable digest ECR mein push karta hai; ECS task definition isi digest aur least-privilege task role se deploy hoti hai. Health checks/load balancer gate traffic; rollback previous digest par; DB migration expand-contract compatible rahe.

**Follow-ups**
- **BuildKit/cache?** Restore inputs (project/lock files) ko source copy se pehle rakhkar dependency layer cache; BuildKit cache mount speed badhata hai, secret mount credentials ko final layer se door rakhta hai. Cache correctness/security verify karein.
- **Container persistent data?** Container filesystem ephemeral samjhein; durable objects S3/database, required stateful workload ke liye explicit volume/storage lifecycle and backup.
- **Compose network/volume?** Service-name DNS for local service communication; named volume persistent local data; production secret/network/replica/HA settings Compose defaults se infer na karein.
- **ECS/Fargate deploy?** Image → ECR; task definition CPU/memory, port/log/health, task role; service desired count, subnets/security groups and load balancer; deployment alarms, autoscaling and rollback.

## 15. Testing, compliance aur quality

### Q1. Unit, integration, contract aur end-to-end tests ka balance?

**Answer:** Unit tests isolated logic fast test karte hain; integration tests real infrastructure boundaries (DB, HTTP pipeline) validate karte hain; consumer-driven contract tests service compatibility; E2E user/business flow. Pyramid mein fast focused tests zyada, expensive E2E targeted rakhein.

**Pitfall:** Mock-heavy tests implementation ke saath change hokar behavior prove na karein. Test sirf private method structure nahin, observable behavior verify kare. Flaky tests quarantine karke root cause fix karein.

**Follow-ups**
- **BDD kya hai?** Business-readable Given/When/Then examples se behavior specify/validate karna. BDD tool use karna alone collaboration/requirements clarity guarantee nahin.
- **Test doubles?** Dummy unused parameter; stub canned response; spy calls record; mock expectations verify; fake lightweight working implementation. Har dependency mock karna zaroori nahin.
- **WebApplicationFactory?** ASP.NET Core integration-test host create/configure karke routing, middleware, DI aur HTTP behavior test kar sakte hain.

### Q2. GDPR aur HIPAA ko engineer ke roop mein kaise approach karenge?

**Answer:** Pehle product/legal/security team se applicability aur data scope confirm karein. Data minimization, purpose/access limitation, encryption, auditability, retention/deletion, incident process aur vendors/subprocessors controls implement karke evidence maintain karein.

**Pitfall:** "Encryption on hai, isliye compliant" ya coding interview mein legal certification claim karna. Compliance organizational/process responsibility hai; jurisdiction, contract aur role-specific requirements professional/legal review maangte hain.

**Follow-ups**
- **PII log ho jaye to?** Exposure scope contain, access restrict, incident procedure follow, retention/deletion policy apply, redaction/monitoring test aur root cause document karein; reporting decisions authorized privacy/security team kare.
- **Unit testing vs BDD?** Unit testing ek test scope/technique; BDD requirements ko examples aur stakeholder collaboration ke around organize karne ka approach.

## 16. Agile aur team collaboration

### Q1. Scrum aur Kanban ka practical difference?

**Answer:** Scrum time-boxed Sprint, defined accountabilities/events/artifacts aur Sprint Goal deta hai. Kanban continuous flow, WIP limits aur cycle/lead time manage karta hai. Team ka work type aur delivery constraints decide karte hain; hybrid practices ho sakti hain.

**Pitfall:** Velocity ko individual performance metric banana ya points ko time/commitment samajhna. Ceremony complete karna outcome delivery ke barabar nahin.

**Follow-ups**
- **Sprint commitment?** Team Sprint Goal ke liye forecast karti hai; changing information par scope negotiate ho sakta hai—quality bar/DoD sacrifice nahin.
- **Retrospective ka result?** Chhota measurable improvement action owner aur review date ke saath; blame session nahin.
- **Lead time vs cycle time?** Lead time request se delivery; cycle time work start se completion. Metric boundary team explicitly define kare.

## 17. Coding, DSA aur problem solving

### Q1. Algorithm interview problem kaise approach karenge?

**Answer:** Problem apne words mein restate karein, inputs/constraints/edge cases poochhein, brute-force baseline batayein, suitable data structure/algorithm choose karke correctness explain karein, complexity derive karein, phir code + tests. Assumptions aloud rakhein.

**Pitfall:** Sorted input assumption, duplicates, empty/single item, integer overflow, null, stable ordering aur off-by-one ko ignore na karein. Complexity ka sirf average case batakar worst case chhupayein nahin.

**Follow-ups**
- **Binary search kab?** Search space monotonically sorted/partitioned predicate ho. Half-open interval `[left, right)` ya inclusive bounds consistently use karein; midpoint `left + (right-left)/2`.
- **Quicksort?** Pivot around partition; average O(n log n), worst O(n²); in-place variants stack-depth/unstable behavior. Production general-purpose sort ke liye platform sort consider karein.
- **Hash map collision?** Hash same bucket mein land kar sakte hain; equality key compare karke chain/open addressing handle karta hai. Hash code unique key nahin; mutable key/hash dangerous.
- **Queue using two stacks?** Enqueue input stack par; dequeue output empty ho tab input se output mein transfer; amortized O(1) per operation.

### Q2. Coding question ko optimize kab karenge?

**Answer:** Correct simple baseline ke baad input constraints aur bottleneck ke basis par. Time/space trade-off, worst-case behavior, data size aur maintainability discuss karke optimization justify karein.

**Practice set:** Binary search, sorting/partitioning, hash map counting, stack/queue, linked-list reversal/cycle, tree traversal, sliding window/two pointers, BFS/DFS, recursion/backtracking, dynamic programming, SQL joins/window functions. Workspace ke SOTI guide mein worked problems aur C# solutions refer karein.

### Q3. SOTI coding sheet ke core problems ka approach aur complexity?

| Problem | Short answer / approach | Complexity |
|---|---|---|
| Reverse string / palindrome | Mutable char array par two pointers; string immutable ho to new result allocate hoga. | O(n) time; O(1) extra in-place array |
| Second-largest distinct | One pass mein largest/second-largest distinct track; duplicates skip. | O(n) time, O(1) space |
| Fibonacci | Iterative rolling previous two values; recursion-naive exponential calls se bachein. | O(n) time, O(1) space |
| Rotate matrix 90° clockwise | Square matrix transpose, phir har row reverse. | O(n²) time, O(1) extra in-place |
| Two Sum | Har number ka complement hash map mein check/store. | O(n) average time, O(n) space |
| Queue using two stacks | Enqueue input stack; output empty par transfer; dequeue output se. | Amortized O(1) per op |
| Longest substring without repeats | Sliding window + last-seen index; left boundary monotonically advance. | O(n) time, O(min(n, alphabet)) space |
| Fixed-size max-sum subarray | First k sum, then outgoing subtract/incoming add. | O(n) time, O(1) space |
| Maximum subarray (Kadane) | Current best ending here = max(current value, prior + value); global max track. | O(n) time, O(1) space |
| Linked-list insert/reverse | Position tak traverse; reverse mein `prev/current/next` pointers. | Insert O(n), reverse O(n); O(1) extra |
| Merge sorted lists / arrays | Dono heads compare karke smaller next output. | O(m+n); output-array case O(m+n) space |
| Merge sort | Divide, recursively sort, merge stable halves. | O(n log n) time, O(n) extra |
| Climbing stairs (1 or 2) | `ways[n] = ways[n-1] + ways[n-2]`; iterative pair. | O(n) time, O(1) space |
| Unweighted shortest path | BFS from source; predecessor reconstruct path. | O(V+E) time |
| Weighted shortest path | Non-negative edge weights par Dijkstra + priority queue. | O((V+E) log V) typical binary heap |

**Pitfall:** Complexity input representation/assumptions ke saath batayein; e.g. Unicode text ke grapheme clusters ko C# `char`/UTF-16 code unit ke barabar na samjhein, aur Dijkstra negative edges ke liye valid nahin.

### Q4. Process/thread, TCP/UDP aur “URL enter karne par kya hota hai”?

**Answer:** Process ka isolated virtual address space/resources hota hai; ek process ke threads heap share karte hain, par stacks/execution state alag hoti hai. TCP ordered reliable byte stream aur retransmission deta hai; UDP datagrams mein delivery/order guarantee nahin. URL flow mein browser URL parse/DNS resolve karta hai, connection/TLS negotiate karta hai, HTTP request bhejta hai, response leke resources load/render karta hai. HTTP/3 QUIC over UDP use karta hai; cache/connection reuse kuch steps skip kar sakte hain.

**Follow-ups**
- **Concurrency vs parallelism?** Concurrency multiple work ko interleave/manage karna; parallelism multiple cores par ek hi waqt execute karna.
- **Deadlock?** Threads ek-dusre ke held resources ka wait karein; lock ordering, chhoti critical sections aur nested locks avoid karke risk ghatayein.
- **DNS ka role?** Hostname ko network address se resolve karna; caches/TTL/proxies/CDNs observed path ko affect kar sakte hain.

### Q5. Puzzle question mein reasoning kaise dikhayenge?

**Answer:** Assumptions, allowed operations aur information constraints restate karein; brute-force possibilities se eliminate karke invariant/strategy explain karein. Interviewer ko sirf final answer nahin, reasoning sunaiye.

**Quick answer key**
- **Do uneven 60-minute ropes se 45 min:** Rope A dono ends se, rope B ek end se light. A 30 min mein khatam; us waqt B ka doosra end light, remaining B 15 min mein khatam.
- **8 balls mein ek heavier, balance scale:** 3 vs 3. Balance ho to bache 2 compare; tilt ho to heavier group ke 3 mein 1 vs 1, tie par third heavier.
- **3 switches, bulb, room mein ek entry:** Switch 1 kuch der on karke off; switch 2 on karke enter. Lit = 2; unlit/warm = 1; unlit/cold = 3. Warm-bulb assumption mention karein.
- **100 doors, 100 passes:** Door n divisor count jitni baar toggle; sirf perfect squares ke divisors odd count, isliye squares open.
- **25 horses, 5 lanes, stopwatch nahin; top 3:** 5 heats, phir group winners ki 6th race. Winner order se eliminate karne ke baad A1 overall fastest hai; A2, A3, B1, B2, C1 ki 7th race ke top two ko A1 ke saath rank karein.
- **Candle, box of tacks, matches:** Empty box ko wall par tack karke candle-holder banayein; packaging/box ko resource samajhna trick hai.

## 18. Behavioral aur resume deep dive

### Q1. “Tell me about yourself” ka jawab kaise banayenge?

**Answer framework (60 seconds):** “Main `[years/domain]` se `[core stack]` par kaam kar raha/rahi hoon. Recent role mein `[product/problem]` ki ownership `[scope/team]` ke saath thi. Mera contribution `[specific decision/action]` tha, jiska measured result `[verified outcome]` hua. Ab `[target role]` mein `[relevant strength]` apply karna chahta/chahti hoon.”

**Pitfall:** Resume ko line-by-line padhna, vague “hardworking” claims, team result ko apna sole result batana, ya unverifiable metrics.

**Follow-ups**
- **Apni contribution kaise separate karenge?** “Team ne X deliver kiya; mera ownership Y tha; maine Z decision/implementation ki; result ko W se measure kiya.”
- **Exact number yaad nahin?** Range/approximation tabhi dein jab defensible ho; warna measurement source aur limitation honestly explain karein.

### Q2. Project deep-dive ka structure?

**Answer:** Business problem → constraints/SLO → architecture/boundaries/data flow → apna role → key decision aur alternatives → failure/security/scale handling → outcome/evidence → learned trade-off. Ek diagram aur ek incident/lesson ready rakhein.

**Senior follow-up prompts + answer structure**
- **“Aapne yeh design kyun choose kiya?”** Alternatives, decision criteria, trade-off, evidence.
- **“Production mein kya fail hua?”** Impact, detection, mitigation, root cause, prevention; blame nahin.
- **“Aapne kya galat estimate kiya?”** Assumption, signal, adjustment aur learning.
- **“Agar dobara design karte?”** Current constraints ke saath specific change; hindsight ko original context se alag rakhein.
- **“Team disagreement?”** Shared goal/data, options, decision owner, disagreement ke baad commitment aur result.

### Q3. Strength/weakness, conflict, failure aur “why this role” ka jawab?

**Answer:** Behavioral story ko STAR (Situation, Task, Action, Result) mein concise rakhein; action mein “maine” kya kiya, result measurable/verified kya tha, aur learning kya hai. Weakness real but manageable ho aur improvement behavior/evidence bataye. Role-fit answer job ke actual requirements ko apne relevant experience se connect kare.

**Pitfall:** Fake weakness (“perfectionist”), blame, confidentiality breach, hypothetical ko real incident batana, ya unsupported metric. Conflict story mein respectful disagreement aur decision ke baad execution dikhayein.

**Follow-ups**
- **Failure ke baad kya badla?** Detection, containment, root cause, process/test/monitoring change aur later evidence explain karein.
- **Why are you leaving?** Future role ke positive scope/learning/impact par focus; current employer ko disparage na karein.
- **Salary/notice period?** Apni actual expectation, market/context aur availability accurately batayein; answer ko invented offer se anchor na karein.

### Q4. Resume mein runtime C# scripting/Roslyn claim ko kaise defend karein?

**Answer:** Sirf tab claim karein agar actual system mein use kiya ho. Explain karein ki scripts kyun chahiye the, simpler JSON/rule DSL/plugin alternative kyun nahin, compilation/diagnostics/cache/versioning flow kya, aur authorization/audit/rollback kaise.

**Pitfall:** Roslyn compilation ko sandbox kehna galat. Arbitrary script ko same privileged process mein chalana code-execution risk hai; allow-listing alone security boundary nahin. Resource limits/isolation, trusted authorship, review, test fixtures, versioned publish aur rollback discuss karein.

**Follow-ups**
- **Bad script se recovery?** Last-known-good version restore, cache invalidation, affected event replay/idempotency aur audit trail.
- **Compilation cost?** Script version/hash key par compiled artifact cache; bounded eviction/invalidation; cold compile latency monitor.
- **Roslyn execution flow?** Script/source ko versioned input ke roop mein validate/compile, diagnostics surface, approved references/API surface bind, then controlled runner mein execute; compilation errors, timeout and output ko request/job se correlate karein. Reflection sirf discovered type/member invoke karta hai—security sandbox nahin.
- **Isolation/security boundary?** Untrusted arbitrary code ko privileged application process mein execute na karein. Safer DSL/allow-listed operations first choice; genuine code execution ho to separate least-privilege process/container/VM, network/filesystem restrictions, CPU/memory/time limits, signed/versioned scripts and audit. Strong adversarial isolation requirements ke liye dedicated sandbox boundary evaluate karein.
- **Plugin alternative?** Versioned interface/assembly plugins deployment-time trust and static validation de sakte hain; rules/config/DSL simpler and safer ho sakte hain. Runtime compilation tabhi jab product requirement justify kare.
- **Kya details invent nahin karni?** Exact throughput, uptime, team scope, architecture ownership aur incident details sirf verified project evidence se.

### Q5. Dealer Management System (DMS) integration platform ka design kaise explain karenge?

**Answer framework:** “Platform external dealer-management systems se deal data leta hai, dealer/state-specific mapping aur business rules apply karta hai, phir normalized deal internal DMS tak bhejta hai. Main design ko correctness, failure isolation, auditability aur tenant-specific configuration ke around explain karunga/karungi.” Apni actual service boundaries, AWS components, ownership aur trade-offs add karein.

**Pitfall:** Monthly volume ko high-throughput challenge bata dena jab average rate usko support na kare. Agar documented 100K transactions/month claim use karte hain, average roughly `100,000 / (30 × 24 × 60 × 60) ≈ 0.04 transactions/second` hai. Is example mein duplicate delivery, partial failure, translation correctness aur audit zyada important discussion hain—peak load ko alag measure karke batayein.

**Follow-ups**
- **Duplicate deal kaise rokenge?** Business key/idempotency key + persisted processing state; same key/same payload par prior result return, conflicting payload par explicit conflict.
- **Same deal ke updates out of order aayein?** Aggregate version/sequence compare karke stale update reject/ignore; required ordering boundary ko deal/tenant key se define karein.
- **External DMS down ho?** Bounded retry with backoff+jitter, timeout/circuit breaker, queue buffer, DLQ/alert aur visible pending state. Dusre dealers ko bulkhead se isolate karein.
- **99.9% availability claim kaise justify karenge?** SLI (successful valid end-to-end deals / eligible deals), monthly window, metrics source, exclusions aur denominator define karein. 30-day month mein 99.9% lagbhag 43 minutes error budget hai; SLI aur data source verify kiye bina number quote na karein.
- **Import/export path?** Import: authenticate/authorize, validate schema and size, stage immutable file, create job, parse/validate rows, persist accepted/rejected outcome and audit. Export: authorize query, snapshot/consistent cutoff if required, generate bounded asynchronous artifact, encrypt/store, short-lived download link and expiry.
- **Writes across services atomic kaise?** Local DB transaction ke andar state + outbox event; consumers idempotent; multi-step external actions ko Saga/reconciliation handle karein. Cross-service ACID transaction claim na karein.
- **Multi-region DR?** State which region owns writes, replication mode/lag, failover authority, conflict policy, tested RPO/RTO, DNS/client routing, secrets/key availability and restore drill. Active-active sirf defined ownership/conflict semantics ke saath.

### Q6. Excel plugin se filing/audit service tak import/export flow kaise explain karenge?

**Answer:** UI plugin file selection, usability and user feedback tak; server owns authorization, validation, business rules and audit. Import ko durable submission/job ID dein; versioned template/schema validate, file malware/size-check and stage immutably, parse asynchronously, return row-level errors and status. Export query ko authorize and bound karein, generate as background job, store encrypted artifact with expiry and short-lived access.

**Pitfall:** Client-side formula/validation ko trusted business result samajhna; spreadsheet macros/formulas ko server par blindly execute karna; large file ko API memory mein load karna; partial writes ko atomic kehna; sensitive export ko permanent public URL dena.

**Scenario:** A tax workbook has valid rows and failures mixed: persist submission metadata/idempotency key; stream rows into staging; validate schema and domain rules; publish accepted batch via outbox; retain rejected-row reasons without overlogging PII. Status API exposes progress; retry does not duplicate accepted records; reconciliation compares submitted, accepted, rejected and downstream-acknowledged totals.

**Follow-ups**
- **Transaction boundary?** One local DB transaction can atomically persist submission state and outbox; file storage/queue/downstream service are separate boundaries. Use idempotency, Saga/compensation and reconciliation.
- **Audit trail minimum?** Actor/tenant, submission and correlation IDs, template/rule version, timestamps, outcome/counts and state transitions; content/PII retention follows policy.
- **CQRS zaroori?** Separate write command from query projection only when workflow/read-model needs justify it; one relational model and transaction is a valid simpler start.

---

## 19. Converted .NET Full Stack question bank

Yeh section original [NET Full Stack Interview Q&A Sheet](<./NET Full Stack Interview Q&A Sheet.md>) ke question set ko master guide ke approved format—**direct answer → pitfall → scenario → answered follow-ups**—mein convert karta hai. Exact duplicates ko pehle ke sections ke saath consolidate kiya hai; neeche ke additional prompts missing/legacy-specific depth bharte hain. Original source-ID crosswalk isi Section 19 ke end mein hai.

### C#/.NET aur runtime — converted additions

#### Q. `==`, `Equals()` aur `ReferenceEquals()` kya compare karte hain?

**Direct answer:** `==` operator type-specific ho sakta hai; strings value compare karti hain, jabki class default mein references compare kar sakti hai. `Equals()` logical equality define/override karta hai. `ReferenceEquals()` do references exactly same object ko point karte hain ya nahin check karta hai.

**Pitfall:** `Equals` override karne par `GetHashCode` ko consistent update na karna hash collections ko break karta hai. Equality contract transitive, symmetric, reflexive aur stable hona chahiye.

**Scenario:** `EmployeeId` value objects dictionary keys hain; equality/hash code ID par based rakhein, mutable display name par nahin.

**Follow-ups**
- **`string ==`?** String contents ordinally compare karta hai; culture-sensitive search/sort ka decision alag `StringComparison` se.
- **Struct equality?** Value equality expected ho sakti hai; hot path mein reflection/default implementation cost ko profile karein; `record struct` value semantics deta hai.

#### Q. Garbage collector generations aur LOH ka high-level behavior?

**Direct answer:** .NET GC reachable managed objects ko retain aur unreachable objects ko reclaim karta hai. Short-lived allocations Gen 0 mein; survive karne wale objects higher generations tak promote hote hain. Large Object Heap (LOH) large allocations ke liye separate area hai, jo full collections ke saath collect hota hai.

**Pitfall:** GC ko deterministic cleanup na samjhein; native resources dispose karein. Unbounded cache/event references, large temporary buffers, allocation churn aur pinned objects pressure/fragmentation la sakte hain.

**Scenario:** Memory steadily climb kare to heap snapshot/dump aur allocation profile se retention path, LOH allocations, static/event roots aur cache growth trace karein—sirf GC force karna fix nahin.

**Follow-ups**
- **`Dispose` vs finalizer?** `Dispose` explicit/deterministic ownership cleanup; finalizer last-resort nondeterministic native-resource safety. `SafeHandle` prefer karein.
- **`GC.Collect()` kab?** Application code mein routine fix ke taur par nahin; runtime heuristics disturb karta hai. Measured specialized scenario ke bina avoid.

#### Q. `ref`, `out`, `in`; `const`, `readonly`, `static readonly`?

**Direct answer:** `ref` caller-initialized variable ko by-reference read/write pass karta hai; `out` callee ko definitely assign karna hota hai; `in` by-reference read-only parameter hai. `const` compile-time constant; `readonly` instance field constructor mein set ho sakta hai; `static readonly` type-level runtime initialization ke liye.

**Pitfall:** By-reference parameters API coupling/aliasing introduce karte hain; `in` ka performance benefit type/call-site par depend karta hai. Public `const` consumer assemblies mein inline hota hai—library versioning ke liye `static readonly` consider karein.

**Scenario:** Parse operation success aur value dono return kare; `TryParse(..., out value)` established pattern hai.

**Follow-ups**
- **`readonly` field vs `init` property?** Field implementation detail; `init` public/object-initializer construction contract ko restrict karta hai. Dono referenced object ko deeply immutable nahin karte.
- **`const` enum/string?** Compile-time literal ho; cross-assembly updates ke baad consumers ko recompile ki zaroorat ho sakti hai.

#### Q. `IEnumerable`, `ICollection`, `IList` aur `List<T>` ko kaise distinguish karenge?

**Direct answer:** `IEnumerable<T>` iteration; `ICollection<T>` count/mutation; `IList<T>` index-based access/insertion; `List<T>` concrete resizable array-backed implementation. API mein smallest capability interface expose karein jo caller ko chahiye.

**Pitfall:** `IEnumerable` ko repeatable ya in-memory collection guarantee na samjhein—it may be lazy, single-use, expensive, or DB-backed. Interface-cast karne se concrete capability automatically exist nahin hoti.

**Scenario:** Read-only streaming query ko `IEnumerable<T>`/`IAsyncEnumerable<T>` return karein; caller ko list mutate karna required nahin.

**Follow-ups**
- **`yield return`?** Iterator state machine produce karta hai; values on-demand enumerate hoti hain. `using`/resource lifetime enumeration tak chal sakti hai.
- **`List<T>` vs `LinkedList<T>`?** List indexing/cache locality; linked list known-node insertion/removal O(1) par search O(n) aur allocation/pointer overhead. Workload measurement decide kare.

#### Q. `ref`/`out` se alag, `var`, `dynamic`, anonymous type aur extension method?

**Direct answer:** `var` compile-time type inference hai, runtime dynamic nahin; `dynamic` member binding runtime par karta hai; anonymous type local inferred immutable-property shape deta hai; extension method static method ko instance-call syntax deta hai.

**Pitfall:** `dynamic` compile-time safety/IDE refactoring kam karta hai. Extension methods real instance member se precedence nahin lete; namespace import unki availability decide karta hai.

**Scenario:** LINQ projection mein anonymous type internal query data shape ke liye sahi; public API boundary par named DTO zyada stable.

**Follow-ups**
- **Anonymous type public return?** Method return type infer karna awkward; stable named type/record use karein.
- **`var` kab?** RHS se type obvious ho to readability; obscure type/semantic intent ho to explicit type.

#### Q. `Task.Run`, `Task.Factory.StartNew` aur `Parallel.For` kab?

**Direct answer:** `Task.Run` thread-pool par CPU-bound work schedule karne ka simple default; `StartNew` advanced scheduler/options control deta hai aur `async` delegate par nested-task pitfalls hain; `Parallel.For` synchronous CPU-bound iterations ko parallelize karta hai.

**Pitfall:** I/O-bound async method ko `Task.Run` se wrap karna usually unnecessary; ASP.NET request ke andar CPU work queue karna capacity solve nahin karta. Shared state, cancellation, degree of parallelism aur exceptions define karein.

**Scenario:** Batch image transforms independent CPU work hain—bounded parallelism/profile ke baad parallel processing; network downloads async I/O.

**Follow-ups**
- **`Parallel.ForEachAsync`?** Async iteration ke liye concurrency cap/cancellation ke saath; unbounded fan-out avoid karein.
- **Thread pool starvation?** Blocking worker threads pool ko exhaust kar sakta hai; sync-over-async aur blocking calls remove/limit karein.

#### Q. Exception handling aur application architecture ke achhe practices?

**Direct answer:** Expected validation/business outcomes ko explicit result/validation path se; exceptional failures ko specific exceptions se represent karein. Catch wahi karein jahan recover, translate, enrich ya cleanup kar sakte hain; boundary par structured log, preserve stack, stable client error map.

**Pitfall:** `throw ex;` stack trace reset kar sakta hai; bare catch/silent swallow, normal branching ke liye exceptions, duplicate log/rethrow, aur sensitive data logs se bachein.

**Scenario:** Service exception ko API boundary `ProblemDetails` mein map kare; correlation/trace ID log kare, internal detail client response mein na de.

**Follow-ups**
- **`finally` ya `using`?** Resource lifetime ke liye `using`/`await using`; generic cleanup ke liye `finally`.
- **Framework vs modern .NET?** .NET Framework Windows-only legacy runtime/ecosystem; modern .NET cross-platform unified runtime with current releases/features. Migration dependency/hosting/API compatibility test karein.
- **CLI/CTS/CLS?** CLI standard/runtime model; CTS common type system; CLS cross-language compatibility subset. Interview mein assembly IL aur CLR roles ke saath difference explain karein.

### ASP.NET MVC 4/Web API 2 aur legacy hosting — converted additions

#### Q. Classic ASP.NET MVC request lifecycle, filters aur routing kaise kaam karte hain?

**Direct answer:** IIS/ASP.NET pipeline request leta hai; routing controller/action select karti hai; controller factory/controller execute hota hai; model binding/validation, filters, action aur ActionResult/view rendering ke baad response return hota hai. MVC filters authorization, action, result aur exception stages mein cross-cutting behavior lagate hain.

**Pitfall:** Filter order/version-specific behavior ko ASP.NET Core middleware order samajhkar apply na karein. Authorization pehle; exception filters sirf relevant MVC execution scope ke exceptions dekhte hain.

**Scenario:** Route collision ho to specific route pehle, route values/constraints verify, integration test se actual match assert karein.

**Follow-ups**
- **ViewBag/ViewData/TempData?** ViewBag dynamic wrapper over ViewData; dono current request; TempData redirect ke paar limited persistence (provider/configuration dependent). Typed view model prefer karein.
- **ViewModel kyun?** View ke liye minimal fields/validation; domain entity over-posting, accidental data exposure aur persistence coupling se bachata hai.
- **HTML helpers, partial, layout?** Helpers typed forms/HTML generate; partial reusable fragment; layout shared shell with body/section slots. Child actions/`RenderAction` legacy MVC-specific; modern ASP.NET Core equivalents alag hain.
- **Areas?** Large MVC app ko feature modules/controllers/views/routes mein organize karte hain; custom model binder request values ko complex types mein map karta hai; route constraint URL match restrict karta hai.

#### Q. MVC 4/5 mein DI, session, IIS aur config kaise handle hote the?

**Direct answer:** MVC/Web API 2 mein Autofac/Unity/Ninject jaise container ko `IDependencyResolver`/framework integration ke through wire karke constructor injection; session InProc/StateServer/SQL/distributed modes; IIS app pool worker-process isolation/recycling/identity; web.config transforms publish-time environment config ke liye.

**Pitfall:** InProc session scale-out/restart par lost ya sticky-session-dependent; secrets web.config/source control mein rakhna unsafe. Service locator/controller mein container resolve karna hidden dependency.

**Scenario:** Legacy MVC app ko multiple IIS nodes par scale karna: session ko distributed store mein migrate, machine-specific keys/identity and deployment transforms verify, health/load test.

**Follow-ups**
- **MVC vs Web API vs WCF?** MVC ka primary purpose HTML/UI action; Web API HTTP-centric client services; WCF broader SOAP/protocol/configuration stack for legacy integrations. Current .NET platform choice alag evaluate karein.
- **ASP.NET Core DI ka equivalent?** Built-in service container and middleware; legacy resolver APIs same nahin, migration mein registrations/lifetimes rewrite/test hote hain.
- **IIS app pool?** Worker process boundary, identity, recycle/start settings; recycle ke dauran in-memory state persist nahin hoti.

#### Q. DI container disposal, `DbContext`/`HttpClient` registration aur keyed services?

**Direct answer:** Container se resolve hua disposable service aam taur par us scope/container se dispose hota hai jisne create/own kiya; manually-created object ki ownership manually manage hoti hai. `DbContext` per unit-of-work/request scoped ya factory-created; modern .NET outbound HTTP ke liye `IHttpClientFactory` ke typed/named clients. Keyed DI same interface ki named/key-based implementations resolve karta hai.

**Pitfall:** Container se resolve transient disposable ko root provider se capture karna disposal der tak delay kar sakta hai. `HttpClient` per request `new` karna socket/DNS lifecycle problems la sakta hai; `IHttpClientFactory` handlers pool karta hai. Same `DbContext` concurrently share na karein.

**Scenario:** Background worker ko har message ke liye fresh unit of work chahiye: `IServiceScopeFactory` se scope create, scoped `DbContext` resolve, process, dispose; named external clients factory se.

**Follow-ups**
- **Manually `new` kiya service?** DI container ownership/disposal automatically assume na karein; caller/factory documented ownership le.
- **Keyed services kab?** Explicit finite variants (e.g. providers) ko key se resolve; dynamic business algorithm ke liye Strategy abstraction/factory often clearer.
- **Constructor injection vs service locator?** Constructor injection dependency/ownership visible, testable; service locator hidden runtime dependency.

#### Q. User A ko kabhi User B ka data dikh raha hai—DI lifetime se kaise diagnose?

**Direct answer:** Request-specific mutable state singleton/static service, shared cache key, reused `DbContext`, session affinity ya tenant scoping leak kar sakti hai. Correlation/tenant/user IDs ko safely trace karein; DI registrations, object lifetimes, cache key and data query predicates inspect karein.

**Pitfall:** Sirf auth token decode karke data authorization assume karna; cache key mein tenant/user missing; sensitive values log karna. Is incident ko privacy/security incident procedure ke saath escalate karein.

**Scenario:** Reproduce parallel users/tenants with integration tests; assert response isolation and cache isolation; singleton state remove or key by validated tenant+resource, and enforce server-side row authorization.

**Follow-ups**
- **Singleton thread-safe hona enough?** Nahin—shared state tenant/user-scoped ho to thread-safety sirf simultaneous writes protect karegi, data isolation nahin.
- **Test lifetime bug?** Host/container integration test mein multiple scopes create karein; same request reuses scoped instance, distinct requests do not, singleton sharing is intentional and safe.

#### Q. MVC/Web API input validation aur CSRF/XSS/over-posting?

**Direct answer:** Server-side validation authoritative; MVC model state/data annotations/custom validation or API validation layer. Cookie-authenticated browser POST par anti-forgery token validate karein; output contextual encoding, Razor default encoding, narrow CSP, DTO/view model allow-listed fields use karein.

**Pitfall:** Client-side validation security boundary nahin; `[Bind]`/DTO se authorization enforce nahin hoti; `Html.Raw`/dangerous DOM APIs untrusted content ko execute kara sakte hain. CORS CSRF protection nahin.

**Scenario:** User profile update DTO sirf allowed fields expose kare; server ownership/policy check, validation, antiforgery protection for cookie session; malicious extra `IsAdmin` field ignored/rejected.

**Follow-ups**
- **CSRF vs XSS?** CSRF browser ke automatically attached credentials exploit karta hai; XSS same-origin script execute karke requests/data access kar sakta hai.
- **CORS?** Browser cross-origin response access policy; explicit origins/methods/headers allow-list; credentials ke saath wildcard origin nahin.
- **MVC client validation?** Unobtrusive JS UX improve karti hai, par API/server par validation dobara mandatory.

#### Q. Classic MVC result types aur filters?

**Direct answer:** `ActionResult` common abstraction; `ViewResult` full view, `PartialViewResult` fragment, `JsonResult` JSON, redirect result PRG redirect, file/status result appropriate response deta hai. MVC filter stages authorization/action/result/exception; Web API 2 apni filter/message-handler pipeline rakhta hai.

**Pitfall:** Controller unit test se routing, auth filter, model binding, serialization/CORS prove nahin hote—unke liye integration tests chahiye.

**Scenario:** POST form par create ke baad redirect, success notice TempData; API command ke liye explicit status/body response.

**Follow-ups**
- **PRG kyun?** POST ke baad GET redirect browser refresh duplicate form submission risk kam karta hai.
- **`IHttpActionResult` vs `HttpResponseMessage`?** Pehla Web API 2 mein response factory/testable abstraction; doosra direct HTTP message control. Framework/version context mention karein.
- **Content negotiation?** `Accept`/formatters ke hisaab se response representation; supported media types document/test karein.

### REST, Web API aur full-stack request flow — converted additions

#### Q. REST design principles, API version, pagination aur error contract?

**Direct answer:** Resource-oriented URLs, standard HTTP semantics, stateless requests, representations, cache controls, consistent validation/error shape, documented OpenAPI contract. Versioning URI/header/media-type se ho sakti hai; pagination query/cursor; `ProblemDetails` machine-readable error contract deta hai.

**Pitfall:** `GET` par mutation, unbounded page size, unstable offset ordering, version without deprecation policy, `500` for validation errors aur exception details client ko return karna avoid karein.

**Scenario:** Search endpoint ko paginated/sortable banayein: server allow-list fields, stable unique tie-breaker, max page size, query DB par execute, response metadata/cursor return kare.

**Follow-ups**
- **PUT vs PATCH?** PUT target representation/state set/replace; PATCH partial update protocol specify karta hai. Repeated PUT idempotent; PATCH semantics payload par depend.
- **ETag?** Representation version validator; `If-None-Match` cache revalidation, `If-Match` optimistic concurrency.
- **Filtering/sorting?** Input validate/allow-list; arbitrary SQL or dynamic query fragments kabhi concatenate na karein.

#### Q. File upload/download aur long-running operation API kaise design karenge?

**Direct answer:** Small uploads API se stream + size/type validation; large uploads object storage ke short-lived presigned URL se direct. Async processing par `202 Accepted` with status resource/polling or callback; download authorization, content disposition/type and range support as needed.

**Pitfall:** File name/content type trusted input nahin; malware scanning, decompression bomb/size quotas, encryption, tenant ownership, retention and orphan cleanup address karein. Long task ko request thread par wait na karayein.

**Scenario:** S3 upload → event/queue → worker processing → durable status → client poll/SignalR/email. Duplicate events idempotently process; errors DLQ + operator replay.

**Follow-ups**
- **API process memory mein file kyun nahin buffer?** Large/concurrent bodies memory pressure create karte hain; streaming/object-storage direct path scale karta hai.
- **Webhook vs polling?** Webhook/callback lower latency but receiver availability/retry/security needed; polling simpler but request load/latency trade-off.

#### Q. Legacy ASP.NET MVC performance incident kaise investigate karenge?

**Direct answer:** Baseline latency/throughput/error/CPU/memory/thread count; request trace and slow route identify; SQL execution plan/IO, external dependency latency, locks, allocations, synchronous blocking, IIS queue/app-pool recycle inspect. One bottleneck fix at a time and load-test.

**Pitfall:** Blind caching, machine scaling, `async` keyword mechanically add karna, ya app-pool recycle ko permanent fix banana. Cache invalidation, session, stale data aur DB capacity verify karein.

**Scenario:** MVC page under load 5s; trace DB query par 4s and many repeated calls shows N+1. Projection/batching/index fix, before/after representative concurrent load and correctness verify.

**Follow-ups**
- **MVC legacy bundling/output cache?** Bundling/minification static asset request/size reduce; output caching generated responses reuse. Auth/personalization invalidation and cache variation key carefully set.
- **Slow only on IIS?** App-pool identity/permissions, cold startup, recycling, connection pool, DNS/TLS, proxy headers, CPU/memory limits and production config compare.

### SQL Server advanced question additions — converted

#### Q. Clustered/nonclustered index, keys aur duplicate cleanup?

**Direct answer:** Clustered index leaf level data pages/order organize karta hai, per table ek; nonclustered separate lookup structure multiple ho sakta hai. PK/unique/FK alag integrity roles; `IDENTITY` compact increasing surrogate, GUID globally generated/distributed key but random clustered GUID fragmentation risk.

**Pitfall:** "Clustered index means physical row order always" literal storage guarantee nahin; key selection affects row locators, page splits and lookups. Unique NULL behavior SQL Server constraints/index form ke mutabik validate karein.

**Scenario:** Duplicate email rows ko deterministic survivor choose karke `ROW_NUMBER() OVER (PARTITION BY Email ORDER BY Id)` assign; `rn>1` delete transaction/backup/preview ke baad.

**Follow-ups**
- **GUID kab?** Distributed generation/merge requirement par; sequential GUID or nonclustered clustered key choice workload/replication/security constraints ke saath.
- **FK kyun?** Referential integrity; child FK indexes automatically har case mein banenge assume na karein.

#### Q. Stored procedure, function, view, trigger aur cursor ka trade-off?

**Direct answer:** Stored procedure task/command and multiple outputs/transactions; function composable expression/table value with restrictions; view saved relational query; trigger DML event par implicit execute; cursor row-by-row iteration.

**Pitfall:** Stored procedure automatic secure nahin if dynamic SQL concatenates; scalar UDF/cursor/trigger hidden cost and side effects; indexed view maintenance/write overhead.

**Scenario:** Bulk update set-based statement; unavoidable row-specific workflow app/batch mein bounded; trigger sirf well-justified invariant/audit with clear transaction impact.

**Follow-ups**
- **Dynamic SQL?** `sp_executesql` + parameters for values; identifiers map from allow-list.
- **CTE vs temp table vs table variable?** CTE statement expression; temp table stats/index/materialization; table variable scope and version-dependent optimizer behavior. Plan/data size se choose.

#### Q. Window functions aur SQL coding tasks?

**Direct answer:** Window function rows preserve karte hue related set par rank/aggregate calculate karti hai. `ROW_NUMBER` unique sequence, `RANK` tie gaps, `DENSE_RANK` no gaps; `LAG/LEAD`, running totals and partitioning common.

**Pitfall:** `ORDER BY` tie-breaker na ho to row-number result nondeterministic; delete duplicate operation se pehle preview, transaction, backup and unique constraint plan.

**Scenario:** Department-wise second distinct-highest salary: `DENSE_RANK() OVER (PARTITION BY Dept ORDER BY Salary DESC)` and rank 2 filter.

**Follow-ups**
- **Temp table vs CTE?** CTE automatically materialize nahin hota; repeated use / stats / indexes needed ho to temp table test karein.
- **Backup strategy?** Full + differential + log (Full recovery model) according RPO/RTO; restore drill hi backup usability verify karta hai.
- **High-volume data?** Batch/partition/archival, indexes, columnstore analytics, retention and tested restore. Feature/edition/cloud capabilities version and SKU se verify.

### Angular 19 / Angular full-stack — converted additions

#### Q. Angular modern architecture aur Angular 19 ke interview points?

**Direct answer:** Components, templates, services/DI, directives and pipes UI architecture banate hain. Standalone components recommended modern direction; signals, built-in `@if/@for/@switch`, functional guards/interceptors aur typed reactive forms current capabilities hain. Angular 19 API/version specifics official docs se validate karein.

**Pitfall:** Angular 19 ke features ko all older Angular apps par assume na karein; zone-less/default behaviors and migration status version-specific hain. NgModule-based legacy apps ab bhi valid.

**Scenario:** Legacy feature NgModule app mein new standalone route add; lazy load, tests, shared providers and upgrade path verify before changing all modules.

**Follow-ups**
- **Lifecycle order?** Constructor/initialization, `ngOnChanges` (inputs), `ngOnInit`, content/view init/check hooks, `ngOnDestroy`; exact hook semantics and new render hooks ke liye version docs.
- **View encapsulation?** Emulated default scoping, Shadow DOM native boundary, None global CSS; component library style leakage/accessibility test karein.
- **Content projection/`ng-content`?** Parent markup child component ke declared slot mein project; projected content child template create nahin karta.

#### Q. Angular Observables, Promise, memory leaks aur REST calls?

**Direct answer:** Promise ek eventual result; Observable zero-to-many values, lazy subscription and RxJS operators. `HttpClient` typically cold Observable per subscription. `async` pipe auto subscribe/unsubscribe; imperative long-lived subscription lifecycle cleanup (`takeUntilDestroyed`, explicit unsubscribe) karein.

**Pitfall:** Same cold HTTP Observable ko multiple independent subscribe karna duplicate network calls la sakta hai. Nested subscribe, leaked subject/listeners aur careless `shareReplay` cache leaks avoid.

**Scenario:** Search box par `debounceTime`, `distinctUntilChanged`, `switchMap` latest request ke alawa previous request cancel/ignore; UI state handles loading/error.

**Follow-ups**
- **`switchMap` vs `mergeMap` vs `concatMap`?** Latest only/cancel prior; concurrent all; sequential preserve order. Business semantics decide.
- **Promise vs Observable?** One result/simple native async flow = Promise; cancellation, streams, operator composition/multiple emissions = Observable.
- **Component to parent communication?** Inputs/outputs for parent-child; shared service/signals/store for distant siblings or broader shared state; avoid global state if local suffices.

#### Q. Angular performance, security, API forms aur tests?

**Direct answer:** OnPush/immutable updates, stable tracking, lazy routes/deferred views, bundle analysis, async pipe, efficient selectors and production profiling. Reactive typed forms + server validation; HttpClient/interceptor for cross-cutting auth/error handling; Angular security relies on contextual escaping/sanitization and safe DOM APIs.

**Pitfall:** Trusting bypass-security APIs/user HTML; putting bearer tokens in unsafe storage without XSS threat model; route guard is not server authorization; frontend validation is not security boundary. Never retry non-idempotent POST blindly.

**Scenario:** Authenticated order screen: guard improves UX; API enforces authorization; interceptor attaches token only to trusted API origin; form validates client + server; component/service tests and E2E cover core flow.

**Follow-ups**
- **Angular testing?** Unit tests for pure service/component behavior, HttpTestingController for requests, router/forms tests, a few browser E2E; avoid flaky over-reliance on UI tests.
- **State management options?** Component state/signals for local; service/store for shared; NgRx when complexity, effects, replay/devtools/team scale justify boilerplate.
- **Route resolver vs lazy loading?** Resolver preloads route data; lazy loading defers code chunk. Both affect navigation latency and need loading/error UX.

### Unit tests, BDD, compliance — converted additions

#### Q. TDD/BDD, test doubles, coverage aur asynchronous tests?

**Direct answer:** TDD Red-Green-Refactor; BDD shared Given-When-Then behavior/acceptance examples; unit tests fast isolated behavior, integration tests boundaries, E2E critical user flow. AAA (Arrange-Act-Assert), deterministic clock/files/random dependencies and async `await` are practical habits.

**Pitfall:** Coverage percent quality nahin; over-mocking implementation couples tests. Async test ko `async void` na banayein; exceptions assert karein and test data/time deterministic rakhein.

**Scenario:** Order service uses repository and clock: mock/stub repository/clock for business unit tests; real DB via disposable test container for persistence integration; API end-to-end limited.

**Follow-ups**
- **Mock/stub/fake/spy/dummy?** Mock verifies interaction; stub canned answer; fake working lightweight implementation; spy records calls; dummy unused placeholder.
- **Moq example?** Setup async repository return; construct SUT with mock; await method; assert result and only verify call if interaction is contract.
- **Test MVC/API controller?** Direct unit test for action result/view model; separate integration test for route, binding, filters, auth, serialization and middleware.
- **Reqnroll/SpecFlow?** Gherkin feature + C# step definitions over business language; keep steps not tightly coupled to UI click implementation.
- **Coverage target?** Critical business paths and branch risk; use coverage as gap detector, not a universal 100% quota.

#### Q. GDPR, HIPAA, OWASP, access-control models?

**Direct answer:** GDPR personal-data processing rights/principles and HIPAA protected health information safeguards apply only when jurisdiction/role/data make them applicable. Engineering controls include data minimization, access least privilege, encryption, audit, retention/deletion, breach process and vendor controls. OWASP Top 10 is web-application risk awareness, not compliance certification.

**Pitfall:** Claiming every application falls under HIPAA, treating encryption alone as compliance, or giving legal interpretation without privacy/legal owner. Pseudonymized data can still be personal data; anonymization must resist re-identification in context.

**Scenario:** Account deletion request: identify data lineage/processors, verify identity and legal retention exception with privacy team, delete/anonymize where required, audit action without retaining deleted PII in logs/backups beyond policy.

**Follow-ups**
- **GDPR vs HIPAA?** Different jurisdictional scope, covered entities/data and obligations; confirm applicability with counsel/compliance—not interchangeable checklists.
- **RBAC/claims/ABAC?** Roles group permissions; claims describe subject/resource facts; ABAC evaluates attributes/context. Authorization must be enforced server-side at each resource boundary.
- **Sensitive logging?** Redact tokens/secrets/PII, restrict access, retention and audit; incident response follows organization policy.

### AWS architecture and DevOps — converted additions

#### Q. Deploy ASP.NET API + Angular frontend on AWS?

**Direct answer:** Angular static assets to private S3 origin served via CloudFront; API on ECS/Fargate, Lambda or suitable compute behind ALB/API Gateway; data in RDS/DynamoDB by workload; IAM roles, VPC, TLS, secrets, CI/CD, CloudWatch/OpenTelemetry, backups and WAF/rate-limit as needed.

**Pitfall:** Diagram alone is insufficient: private/public subnet, egress, CORS, auth, tenant isolation, migrations, cache invalidation, logging PII and failure/rollback must be addressed. Avoid services unrelated to requirements.

**Scenario:** Angular static app deploys immutable build; backend canary behind load balancer; expand-contract DB migration; CloudFront invalidation/versioned assets; health alarm gates traffic promotion.

**Follow-ups**
- **EC2 vs Beanstalk vs Lambda vs ECS/Fargate?** Control and OS-level customization vs managed app deployment vs event-driven bounded execution vs long-lived container service; cost/ops/runtime fit decide.
- **Shared responsibility?** AWS secures cloud infrastructure; customer secures workload configuration/data/identity/network settings according to service model.
- **IaC kyun?** Reviewable reproducible environment and drift control; state/secrets/plan, module versioning and rollback require governance.

#### Q. AWS incident, high availability, monitoring aur cost kaise approach?

**Direct answer:** Impact/SLO/region-scope identify; dashboard/log/trace/alarms, recent changes, dependency health, queue age, throttling and saturation inspect. Mitigate first (rollback/traffic shift/capacity), then preserve evidence/RCA/action owners. HA uses multi-AZ/failure-domain design, health routing, stateless compute, durable backups and tested recovery.

**Pitfall:** Blindly scaling a DB, retrying a throttled service, relying on untested backup, or optimizing cost by removing required redundancy. Distinguish Multi-AZ standby/failover from read scaling.

**Scenario:** Queue age rising with worker errors: inspect DLQ/error classes and downstream throttles; pause poisonous replay, scale bounded consumers only if dependency supports, deploy fix, replay idempotently and verify backlog/SLO.

**Follow-ups**
- **AWS cost optimization?** Tag/account allocation, right-size, autoscale/schedule, storage lifecycle, request/data-egress analysis, commitments only after stable baseline; protect SLO.
- **S3 use from .NET?** SDK and task role credentials, bucket/prefix-scoped access, multipart for large objects, encryption, cancellation and retry/idempotency; no embedded keys.
- **Lambda triggers / SQS/SNS/EventBridge/Kinesis?** Trigger-driven handler; queue distributes work, topic fans out, event bus routes events, stream retains ordered partitions/replay by offset. Delivery semantics/idempotency differ.
- **RDS Multi-AZ vs read replica?** Multi-AZ primarily availability/failover; read replica read scaling/DR depending engine/config. Verify RDS SQL Server edition/feature constraints.
- **IAM user/role/policy?** Identity principal; role assumed for temporary credentials; policy grants actions/resources/conditions. Prefer workload roles and least privilege over long-lived access keys.

### Production, leadership aur project scenarios — converted additions

#### Q. Database/API zero-downtime release kaise karenge?

**Direct answer:** Backward-compatible expand-contract: additive schema first; deploy code compatible with old/new; backfill/validate; switch reads/writes; monitor; later remove obsolete schema. Build immutable artifact once and promote through gated environments; canary/blue-green/rolling matched to app and schema.

**Pitfall:** Old and new app versions concurrently running during rollout; destructive column rename/drop; rollback to code incompatible with migrated data; unbounded dual-write divergence.

**Scenario:** Rename column: add new nullable column, dual-write/backfill/check, read new, stop old writes, wait rollback window, then drop old in later change.

**Follow-ups**
- **Feature flag?** Separate deployment from user exposure; gradual enable/kill switch, metrics, owners and expiry/removal ticket.
- **Rollback vs roll-forward?** App rollback if schema/data compatible; irreversible migration/data change may need forward fix or compensation.
- **Canary health?** Compare errors, latency, saturation and business outcomes for cohort against baseline before increasing traffic.

#### Q. Production incident, code quality, technical debt aur estimation?

**Direct answer:** Incident: assess severity/user impact, communicate, mitigate/rollback, keep timeline, verify recovery, then blameless RCA with prioritized actions. Quality: review/tests/linters/observability and explicit ownership. Debt: prioritize by risk/cost of delay; characterize legacy with tests and refactor small slices. Estimation: split, expose assumptions/dependencies/uncertainty, update forecast when facts change.

**Pitfall:** Blame individuals, hide uncertainty, promise precise estimate without scope, silently defer security/debt, or equate code coverage/velocity with quality.

**Scenario:** Legacy module risky to change: add characterization/integration tests around current contract, instrument failure/latency, migrate one endpoint/feature using Strangler, compare outcomes and keep fallback.

**Follow-ups**
- **Technical disagreement?** Clarify shared goal, compare evidence/options/trade-offs, decide via owner, commit after decision and revisit with new data.
- **Stay current?** Follow official release notes/docs, experiment in small project, share learnings; distinguish evaluated capability from production experience.
- **Why this role/company?** Role requirements, product/domain impact and one relevant verified strength connect karein; generic praise ya fabricated motivation avoid.

### Source-sheet conversion crosswalk

Source mein **184 prompts** hain: 18 OOP questions (Q1.1–Q1.18), 17 middleware/DI questions (Q2.1–Q2.17), aur 149 numbered full-stack questions (Q1–Q149). Inko master format mein merge kiya gaya; direct duplicates consolidate hain, magar listed subtopics deliberately retained hain. Full text/source IDs:

| Original sheet IDs | Converted destination in this guide |
|---|---|
| Q1.1–Q1.18; C# Q1–Q20 | Sections 1, 2, and 19 C#/.NET/runtime additions |
| C# Q21–Q22; SQL challenges | Sections 7 and 17; coding challenges are summarized in Section 17 |
| Middleware/DI Q2.1–Q2.17 | Section 4; Section 19 legacy MVC/Web API and C# runtime additions |
| MVC/Web API Q23–Q49; REST Q87–Q89 | Sections 4, 6, and 19 legacy MVC, REST, file operations and incident additions |
| SQL Q50–Q69 | Section 7 and Section 19 SQL advanced additions |
| Angular Q70–Q86 | Section 12 and Section 19 Angular 19 additions |
| Patterns/architecture Q90–Q100 | Sections 2, 8, 9, and 19 LLD/legacy scenarios |
| Testing/BDD Q101–Q113 | Section 15 and Section 19 testing additions |
| Compliance/security Q114–Q123 | Section 6, Section 15 and Section 19 compliance additions |
| AWS Q124–Q138 | Section 11 and Section 19 AWS architecture/ops additions |
| Scenarios/behavioral Q139–Q149 | Sections 9, 18 and Section 19 production/leadership additions |

---

## 20. Recent company interview reports: evidence aur converted practice

Yeh section **reported interview experience** aur **senior .NET discussion prompt** ko alag rakhta hai. Public candidate reports self-reported hote hain; title/company/date ko verify karna possible ho sakta hai, par independent corroboration ya exact role rubric aam taur par nahin. Available sources mein exact 8–10 YOE + .NET full-stack match limited hai, isliye har item ka mismatch explicitly diya hai. Neeche reported questions ko master format mein convert kiya gaya hai; wording paraphrased hai jab source wording/proprietary prompt detail insufficient ho.

### Microsoft India — Senior Software Engineer (SSE) phone/job-fitment report

**Report evidence:** Search result mein 20 Jan 2026 date ke saath ek candidate ka Microsoft India SSE phone/job-fitment experience tha. Candidate ne 45-minute round report kiya: self-introduction, behavioral questions, code review, phir DSA. Reported behavioral prompts: sabse technically challenging issue aur recurrence prevent karna; change kyun chahiye; manager authority ke bina influence kaise. Code review task Python file mein missing validation aur console par credentials print karne ki problems identify karna tha. DSA prompt *Open the Lock* tha, BFS/minimum moves ke liye.

**Match boundary:** Company aur SSE level match; report mein 8–10 years experience ya .NET-specific role verify nahin. Code-review language Python thi, isliye isko .NET interview ka proof na samjhein. Underlying post ka direct URL search result se reliably retrieve nahin hua; linked search-result lookup aur source limitation [References](#23-references-aur-research-limits) mein noted hai. **Confidence: medium for reported round/questions, low for .NET/YOE match.**

#### Q. Technical challenge, prevention aur influence-without-authority ka answer?

**Direct answer:** STAR mein ek real production problem choose karein: impact/constraints, aapki responsibility, root cause investigation, mitigation, permanent prevention (test/monitoring/design/process), outcome/evidence. Influence story mein shared goal, stakeholders, evidence/options, alignment aur follow-through batayein.

**Pitfall:** Technical challenge ko sirf technology list bana dena; team result ko apna personal result batana; “prevented forever” claim; authority ke bina influence ko escalation/pressure kehna.

**Scenario:** `[actual incident]` mein repeat failure rokne ke liye regression test, guardrail/alert/runbook add kiya; result `[verified metric]`. Metric available na ho to honest qualitative outcome and evidence explain.

**Follow-ups**
- **Kaise pata chala root cause fix hua?** Failure-mode regression test, post-deploy telemetry, recurrence window and rollback criterion.
- **Agar team aapke proposal se agree nahin hui?** Risk/options document, small experiment/production data, decision owner ki call; decide hone ke baad align and monitor.
- **Change kyun chahte hain?** Target role ke impact/domain/technical scope par positive focus; current team/company ko criticize na karein.

#### Q. Code review mein credentials print ho rahe hain aur validation missing hai—kya check karenge?

**Direct answer:** Secret ko output/log se remove; exposure hua ho to incident process, rotate/revoke credential, logs/access/retention assess. Validation ko trust boundary par add karein, malformed/empty/range/authorization cases test karein; error paths, side effects, tests and readability bhi review karein.

**Pitfall:** Sirf line delete karke credential exposure ko resolved kehna; credentials already captured/log shipped ho sakte hain. Client validation ko server-side validation/authorization ka substitute na banayein.

**Scenario:** PR review mein secret print statement milta hai: block release, log sink/retention/access investigate, key rotate, code/config secret manager par migrate; regression check add.

**Follow-ups**
- **Code-review priority?** First secret/data leakage and security/correctness; then validation/edge cases, error handling, concurrency/performance, maintainability/tests.
- **Kaise respectful feedback?** Specific risk and reproduction; question/suggestion, severity and minimal safe fix; unrelated style comments defer.

#### Q. “Open the Lock” minimum moves mein kaunsi approach aur correctness argument?

**Direct answer:** Har 4-digit state graph node; ek digit ko ±1 wrap karna edge; deadends blocked. Sab edges equal cost, isliye BFS from `"000"` minimum moves deta hai. Visited mark enqueue karte waqt karein, deadends start/target handle karein.

**Pitfall:** `"000"` deadend check na karna; same state ko repeatedly enqueue; wheel wrap (0↔9) miss; DFS ko shortest path ke liye use karna; target unreachable case omit.

**Scenario:** Queue mein `(state, distance)`; neighbors generate; blocked/visited skip; target pop/reach par distance; queue exhaust ho to `-1`.

**Follow-ups**
- **Complexity?** 10,000 possible states (0000–9999); each at most 8 neighbors. O(V+E), bounded by state-space size; O(V) visited/queue.
- **Bidirectional BFS?** Single source/target aur branching high ho to dono directions se search; frontier intersection, deadends and visited sets carefully handle; 10,000-state simple problem mein plain BFS often sufficient.
- **.NET implementation detail?** Fixed-width state string or integer encoding with zero-padding; correctness/readability pehle, optimize only if needed.

### Amazon — SDE II India interview report (May 2025)

**Report evidence:** 16 May 2025 ka public candidate report India/HYderabad location aur ~3.5 years experience batata hai. Rounds mein Course Schedule II, ATM LLD, Amazon grocery parking-lot HLD, Reorganize String, system-design MCQs aur Leadership-Principles discussion report hue.

**Match boundary:** Amazon SDE II/company and report date match; candidate ka stated experience ~3.5 years hai, 8–10 nahin, aur .NET stack mention nahin. Isko **adjacent large-company senior-style preparation**, na ki target experience ka direct benchmark, samjhein. **Confidence: medium for candidate-reported prompts; role/YOE transfer limited.**

#### Q. Course Schedule II—valid course order return karna?

**Direct answer:** Prerequisite graph build karke Kahn topological sort: indegree count; zero-indegree queue; pop course, append result, outgoing neighbors indegree decrement. Sab courses processed to order valid; warna cycle, empty result.

**Pitfall:** Edge direction reverse karna (`prerequisite -> dependent`); duplicate prerequisite edges ko indegree/adjacency mein consistently handle na karna; disconnected components skip.

**Scenario:** Courses `0..n-1`; input `[course, prerequisite]`; prerequisite se course tak edge; queue all indegree-zero nodes.

**Follow-ups**
- **Complexity?** O(V+E) time/space.
- **DFS alternative?** Three colors (unvisited/visiting/done); visiting node revisit cycle; postorder reverse topological ordering.
- **If multiple valid orders?** Problem usually any valid order; deterministic ordering chahiye to priority queue/ordered tie-break but extra cost.

#### Q. ATM ka LLD design kaise karenge?

**Direct answer:** Scope clarify (account/card/auth, cash denominations, withdraw/deposit/balance, limits, audit, ATM offline). Entities: ATM/session, card/account, transaction, cash inventory, bank gateway; state transitions explicit; cash reservation and account debit ko failure-safe/idempotent workflow banayein.

**Pitfall:** “Withdraw” ko sirf balance check + cash dispense samajhna. Cash jams, partial dispense, network timeout after debit, duplicate request, counterfeit/empty denomination and audit/reconciliation address karein. Distributed ATM/core-banking atomic transaction assume na karein.

**Scenario:** Withdrawal request unique transaction ID; authorization/hold, denomination plan and inventory reservation; dispense confirmed par settle; failure par release/reversal workflow and reconciliation record.

**Follow-ups**
- **State pattern?** Idle/Authenticated/Transaction/Dispensing/OutOfService transitions; state object useful only if transition rules meaningfully vary.
- **Concurrency?** Cash inventory and account balance atomic/reservation/version controls; distributed bank system source of truth.
- **Failure after cash dispense but before response?** Transaction status/reconciliation, idempotency key and device journal determine whether retry, compensation or manual investigation.

#### Q. Grocery parking lot HLD—service boundaries aur key trade-offs?

**Direct answer:** Clarify users/vehicles/spots/reservations/payment/entry gates, geography, peak rate and consistency. Core domain capabilities: inventory/availability, booking/ticket, payment, gate/entry, notification; transactional source for a spot, cache/search read model only as appropriate; APIs/events, observability, security and failure behavior.

**Pitfall:** “Microservices” bolkar boundaries/ownership nahin batana; two users same last spot reserve, payment succeeds but gate fails, eventual update and duplicate events ignore karna.

**Scenario:** Spot reservation atomic conditional write/transaction with expiry; payment/outbox event; gate action idempotent; saga/compensation or reservation timeout; queue and DLQ for notification.

**Follow-ups**
- **Monolith or microservices?** Start modular monolith unless independent scale/team/deployment needs justify distributed complexity.
- **Availability vs consistency?** Last-spot allocation needs strong conflict prevention at authoritative partition; browsing availability can be slightly stale.
- **Multi-region?** Define geographic ownership/partition, replication lag and failover behavior before claiming globally active-active.

#### Q. Reorganize String—adjacent high-frequency coding pattern?

**Direct answer:** Character frequencies count karke max-heap mein highest remaining frequency choose; previous character ko ek step cooldown par hold karke immediately repeat avoid; impossible if max frequency exceeds `ceil(n/2)`; otherwise assemble.

**Pitfall:** Previous character ko same iteration par heap mein wapas rakhna and immediately select it again; Unicode/code-unit assumptions; max-heap tie behavior and empty input.

**Scenario:** Heap contains `(char,count)`; each iteration release previous held char, choose highest count, append, decrement, then hold it for next step.

**Follow-ups**
- **Alternative?** Greedy fill even then odd indices from most frequent chars; useful proof for fixed alphabet; heap general scheduling-style explanation.
- **Complexity?** O(n log k) with k distinct characters; O(k) auxiliary space.

### Senior .NET-only community prompt (discussion, not employer report)

**Evidence:** 1 Nov 2025 r/dotnet thread poochta hai ki senior .NET proficiency evaluate karne ke liye kaunse questions use hon; example prompt explicitly `IEnumerable<T>`, `IQueryable<T>` and `IAsyncEnumerable<T>` compare/use-cases hai. Yeh **interviewer discussion/question suggestion** hai—actual company interview report nahin.

#### Q. `IEnumerable<T>`, `IQueryable<T>` aur `IAsyncEnumerable<T>` mein antar; kab kaunsa?

**Direct answer:** `IEnumerable<T>` synchronous enumeration; `IQueryable<T>` expression tree provider ko translation/remote execution ka mauka deta hai (EF SQL); `IAsyncEnumerable<T>` async streaming enumeration. Source/provider, buffering, cancellation and lifetime ke mutabik choose karein.

**Pitfall:** `IQueryable` par unsupported .NET method SQL mein translate na ho; premature `ToList()` DB-side filtering rok de; `IAsyncEnumerable` ko sync block karna ya context/resource lifetime dispose before enumeration.

**Scenario:** Large DB result ko projected columns + server-side filter with async iteration stream karein, request cancellation propagate; small in-memory collection `IEnumerable<T>`.

**Follow-ups**
- **`IQueryable` boundary kahan?** Persistence/query layer mein composition while provider known; public/domain API mein leak karne se provider coupling and hidden query execution.
- **`IAsyncEnumerable` vs `Task<List<T>>`?** Pehla item-by-item async stream and lower peak buffering; doosra full materialized list and simpler repeatable access.
- **Multiple enumeration risk?** Query re-execute/recompute ho sakti; once materialize if reuse and cost/consistency warrant.

---

## 21. Coverage audit: source guides ke topics

Yeh map workspace ke English aur Hinglish detailed notes/interview sheets ko is consolidated guide se connect karta hai. Source guides mein maujood niche items ko bhi topic family ke roop mein list kiya hai; direct links se implementation-level explanation, examples aur extra questions milte hain. Translation/revision duplicates ko same unique topic ke roop mein count kiya gaya hai, alag topic nahin.

| Source material | Is master mein cover kiya gaya topic scope |
|---|---|
| [C# Interview Guide](<./English/Detailed Notes/A. C%23-Interview-Guide.md>) | CLR/IL/JIT/AOT, type system, strings, constructors/inheritance, records/equality, OOP, delegates/events, collections, generics, exceptions, async/concurrency, GC/disposal, DI, testing, performance, security, EF/cache/resilience. |
| [ADO.NET Interview Guide](<./English/Detailed Notes/B. ADO.NET-Interview-Guide.md>) | Providers, connected/disconnected model, connection pooling, commands/readers/adapters/DataSet, parameters/SQL injection, transactions/isolation, async/cancellation, bulk/large data, retry, ADO.NET vs Dapper/EF. |
| [Design Patterns Guide](<./English/Detailed Notes/C. Design-Patterns-Interview-Guide.md>) | SOLID, composition/inheritance, creational/structural/behavioral patterns, DI, repository/unit-of-work, CQRS/Mediator, anti-patterns and pattern trade-offs. |
| [Authentication and Authorization Guide](<./English/Detailed Notes/D. Authentication-Authorization-Interview-Guide.md>) | JWT validation/storage/revocation, OAuth/OIDC/PKCE, SAML, roles/policies, refresh rotation, BFF/SPA, CSRF/XSS, API keys, mTLS, multi-tenancy, passkeys/DPoP, secrets. |
| [LINQ Interview Guide](<./English/Detailed Notes/E. LINQ-Interview-Guide.md>) | Query/method syntax, deferred execution/closures, operators, joins/grouping, `IEnumerable`/`IQueryable`, expression trees, EF translation/client evaluation, iterators, async LINQ, PLINQ, multiple enumeration and complexity traps. |
| [ASP.NET Core Guide](<./English/Detailed Notes/F. ASP.NET-Core-Interview-Guide.md>) | Hosting/configuration, middleware/routing/order, filters, DI lifetimes/captive dependencies/options, minimal APIs/controllers, validation, architecture/multi-tenancy, Kestrel/proxy, background services, caching, HttpClient, rate limiting, starvation, security, health, telemetry, testing. |
| [.NET Web API Guide](<./English/Detailed Notes/G. DOT-NET-Web-API-Interview-Guide.md>) | REST/HTTP/status/idempotency, binding/controllers, validation, exception/Problem Details, CORS, versioning/OpenAPI, pagination/ETags/202, auth, gateway, gRPC/GraphQL/OData, SignalR, resilience, Outbox/Saga, Docker, cache/performance/security. |
| [Microservices Guide](<./English/Detailed Notes/H. Microservices-Interview-Guide.md>) | DDD/hexagonal, sync/async, REST/gRPC, queues/event bus, gateway/composition, database-per-service, CQRS/event sourcing, Saga/Outbox/idempotency, retries/bulkheads/circuit breaker/DLQ, mTLS/secrets, tracing/logging, containers/Kubernetes/service discovery, contracts/testing/versioning. |
| [Structured Logging Guide](<./English/Detailed Notes/I. Structured-Logging-Interview-Guide.md>) | Templates/levels, Serilog sinks/enrichers/destructuring, request flow/correlation, trace context, multi-tenancy, OpenTelemetry, ELK/Seq/Loki/cloud, sampling, async logging, source-generated logging, PII redaction and cost. |
| [SQL Server Interview Guide](<./English/Detailed Notes/J. SQL-Server-Interview-Guide.md>) | Keys/normalization, DML/DDL, joins/sets/CTE/temp tables, procedures/functions/triggers/dynamic SQL, window functions, plans/SARGability/indexes/statistics/sniffing, ACID/isolation/locks/deadlocks, tempdb/Query Store, HA/backup/security/monitoring/troubleshooting. |
| [AWS Guide](<./English/AWS/README.md>) | Lambda, DynamoDB, IAM, IaC/CI-CD, S3, EC2/storage, CloudWatch, load balancing/autoscaling, ECS/ECR/Fargate, databases/cache/analytics, VPC/networking/security, messaging/streams, edge, organizations/billing/cost, migration/transfer, Well-Architected/resilience. |
| [Angular Interview Guide](<./English/Detailed Notes/L. Angular-Interview-Guide.md>) | NgModules/standalone, components/templates/binding/directives/pipes, control flow, DI/injectors, lifecycle, routing/guards, forms, HTTP/interceptors, RxJS/subjects/state/NgRx/signals, OnPush/zoneless, lazy/deferred views, build/bundles, SSR/hydration/PWA/security/accessibility. |
| [TypeScript Interview Guide](<./English/Detailed Notes/M. TypeScript-Interview-Guide.md>) | Type inference, `any`/`unknown`/`never`, unions/narrowing, interfaces/types, generics/variance, utility/conditional/mapped/template types, readonly/branded types, modules/decorators, errors, strict flags, compiler/runtime costs. |
| [System Design Guide](<./English/Detailed Notes/N. System-Design-Interview-Guide.md>) | Requirements/SLO/capacity, CAP, caching/load balancing/gateway/BFF, replicas/sharding/partitioning/consistent hashing, queues/streams, CQRS/Event Sourcing/Outbox/Saga, resilience/idempotency/rate limiting, Kestrel, performance, URL shortener/rate limiter/notification designs, tenancy/PII. |
| [GitHub Actions Guide](<./English/Detailed Notes/O. GitHub-Actions-Interview-Guide.md>) | Events/jobs/steps, runners, secrets/env/permissions, environments/approvals, dependencies/conditions, concurrency/timeouts, caching/artifacts, reusable workflows/actions, containers/AWS/IaC, .NET pipeline, release/versioning, OIDC, pinning, `pull_request_target`, monorepo/selective builds and cost. |
| [Deployment Guide](<./English/Detailed Notes/P. Deployment-Strategies-Interview-Guide.md>) | Deployment strategies, zero downtime, blue-green/canary/rolling, config/secrets, containers, database compatibility, feature flags, health/rollback and release safety. |
| [JFrog Artifactory Guide](<./English/Detailed Notes/Q. JFrog-Artifactory-Interview-Guide.md>) | Artifact/package repository model, local/remote/virtual repos, build metadata, promotion/immutability, retention, permissions, Xray/SBOM and CI integration. |
| [Agile Guide](<./English/Detailed Notes/R. Agile-Interview-Guide.md>) | Agile values, Scrum/Kanban, roles/events/artifacts, backlog/refinement, estimation, DoD, metrics, retrospectives, dependencies and team scenarios. |
| [General Interview Questions](<./English/Detailed Notes/S. Interview-Questions-Interview-Guide.md>) | Intro, project/resume deep dives, leadership, conflict, failure, prioritization, strengths/growth, scenario and HR questions. |
| [EF Core Guide](<./English/Detailed Notes/T. EF-Core-Interview-Guide.md>) | DbContext/entity states, tracking, relationships/loading, migrations, concurrency, transactions, query translation, N+1/performance, compiled/split queries, testing and production pitfalls. |
| [Ports and Hosting Guide](<./English/Detailed Notes/U. Ports-and-Hosting-Explained-Simply.md>) | Ports/adapters, hosting models and deployment/runtime boundaries; paired with Sections 2, 4, 8, 9 and 14 above. |
| [LLD and Design Patterns course](<./LLD/English/LLD-Course/00-START-HERE.md>) and [Hinglish course](<./LLD/Hinglish/LLD-Course/00-START-HERE.md>) | OOP/interface/composition foundations, SOLID, creational/structural/behavioral patterns, modern C# equivalents, pattern selection/overuse, tech-lead code review and architecture links. Covered primarily in Sections 2 and 4. |
| [Concurrency fundamentals — English](<./English/1. Concurrency Fundamentals (Beginner to Advanced).md>) | Process/thread, concurrency/parallelism, locks/Monitor/Mutex, deadlock/livelock/starvation, races, semaphores, thread pool/TPL, `volatile`, `Interlocked`, reader-writer locks and producer-consumer. Covered in Sections 1, 4 and 9. |
| [Concurrency in LLD — English](<./English/2. Phase 4 - Concurrency in LLD.md>) | Lock/Monitor, `volatile`, explicit locks, TPL, thread-safe singleton, producer-consumer, plus applying concurrency to cache, rate limiter and parking-lot designs. Covered in Sections 1, 2, 9 and 17. |
| [LLD interview problems](<./English/3. Phase 5 - Core LLD Interview Problems (Beginner).md>) | Object-oriented low-level design practice and translating requirements into classes, interfaces, interactions and testable responsibilities; paired with Sections 2 and 17. |
| [Runtime C# Script Engine / DLL Plugin Pattern](<./English/U. Runtime C%23 Script Engine - DLL Plugin Pattern (English).md>) | Runtime compilation/execution, plugin alternatives, deployment, caching, safety, isolation and operational trade-offs; paired with Section 18. |
| [Full Stack Interview Q&A Sheet](<./NET Full Stack Interview Q&A Sheet.md>) | OOP/C#/.NET, MVC/Web API, middleware/DI, SQL, Angular, design/SOLID, testing/BDD, GDPR/HIPAA, AWS, full-stack and behavioral scenarios. |
| [SOTI Interview Q&A](<./Coding questions/SOTI-Interview-Questions-and-Answers.md>) | Coding/DSA, C#/.NET/OOP, SQL, OS/networking, puzzles, testing, project and HR questions; company-specific claims should be backed by cited public reports. |
| [Monolith-to-Microservices Phase 1–11](<./Hinglish/System Design/Monolith-to-Microservices-Migration-Master-Interview-Guide-Hinglish.md>) | Decomposition/DDD/Strangler, data migration/CDC, REST/gRPC and messaging, gateway/auth, observability/resilience, backfill/cutover/reconciliation, containers/CI-CD, Saga/CQRS/Outbox, mock scenarios/testing, and resume deep-dive. |

### Migration phases ka explicit checklist

- **Phases 1–2:** monolith assessment, domain boundaries, Strangler rollout, data ownership aur migration approach.
- **Phase 3:** REST/gRPC, SQS/SNS/EventBridge, retries, idempotency, Outbox/Inbox aur messaging semantics.
- **Phase 4:** gateway/BFF, authentication/authorization, tenant isolation aur API security.
- **Phase 5:** centralized logs, metrics, tracing, correlation IDs, health aur resilience.
- **Phase 6:** backfill/CDC, dual-run/cutover, reconciliation, rollback aur zero-downtime schema change.
- **Phase 7:** containers, CI/CD, deployment strategies, feature flags aur AWS/ECS/Kubernetes options.
- **Phase 8:** end-to-end design, Saga, CQRS, Outbox aur eventual consistency.
- **Phases 9–10:** mock interview scenarios aur microservice testing strategies.
- **Phase 11:** resume narrative, DMS/integration architecture, runtime scripting/Roslyn, CI/CD/SDD-AI, data stores, leadership, Angular aur prior-role deep dive. Apne actual contribution aur verified metrics hi present karein.

### Hinglish folder coverage inventory

`Hinglish` folder mein 108 Markdown files aur `Pending topics` note audit kiya. AWS ke detailed/revision pairs aur subject-wise revision notes same subject ko reinforce karte hain; unhe duplicate topics ki tarah double-count nahin kiya. Neeche har unique guide family ka interview-ready destination diya hai; linked source notes mein additional examples/code ke liye refer karein.

| Hinglish source material | Topics covered in this master |
|---|---|
| [Excel CQRS project](<./Hinglish/Detailed Notes/0. Excel-CQRS-project.md>) | Excel plugin/file intake, BFF command/query paths, Saga, CQRS, consistency, audit and SQL Server-vs-Cosmos trade-offs — Sections 9 and 18. |
| [DMS system-design prep](<./Hinglish/Detailed Notes/1. dms-system-design-interview-prep.md>) | Import/export, idempotency, ordering, failure isolation, no cross-service atomicity, reconciliation and multi-region DR — Section 18. |
| [C# Interview Guide](<./Hinglish/Detailed Notes/A. C%23-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/A. C%23-Interview-revision-notes.md>) and [DI lifecycle exercises](<./Hinglish/Detailed Notes/A. DI-lifecycle-exercises.md>) | C#/.NET runtime, types, OOP, collections, exceptions, async/concurrency, GC/disposal, DI lifetime/captive dependency and production behavior — Sections 1–4, 17–18. |
| [ADO.NET Guide](<./Hinglish/Detailed Notes/B. ADO.NET-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/B. ADO.NET-Interview-revision-notes.md>) | Providers, connected/disconnected access, pooling, readers/adapters/DataSet, parameters, transactions, bulk/async access and ORM trade-offs — Section 3. |
| [Design Patterns Guide](<./Hinglish/Detailed Notes/C. Design-Patterns-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/C. Design-Patterns-Interview-revision-notes.md>) | SOLID, composition, creational/structural/behavioral patterns, CQRS/Mediator, repository/UoW, anti-patterns and selection trade-offs — Sections 2, 8–9. |
| [Authentication and Authorization Guide](<./Hinglish/Detailed Notes/D. Authentication-Authorization-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/D. Authentication-Authorization-Interview-revision-notes.md>) | OAuth/OIDC/JWT, PKCE, SAML, token storage/refresh/revocation, RBAC/policies, BFF/SPA, CSRF/XSS, passkeys, DPoP, mTLS and tenant boundaries — Section 6. |
| [LINQ Guide](<./Hinglish/Detailed Notes/E. LINQ-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/E. LINQ-Interview-revision-notes.md>) | Query/method syntax, deferred execution, closures, joins/grouping, iterators, expression trees, `IEnumerable`/`IQueryable`, EF translation, PLINQ and repeated enumeration — Sections 3 and 5. |
| [ASP.NET Core Guide](<./Hinglish/Detailed Notes/F. ASP.NET-Core-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/F. ASP.NET-Core-Interview-revision-notes.md>) and [.NET Web API Guide](<./Hinglish/Detailed Notes/G. DOT-NET-Web-API-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/G. DOT-NET-Web-API-Interview-revision-notes.md>) | Hosting/configuration, middleware/routing/filters/DI, controllers/minimal APIs, validation, Problem Details, HTTP/versioning/OpenAPI, CORS, pagination/ETags/202, gRPC/GraphQL/OData/SignalR, health, resilience and API security — Sections 4, 6, 8–10, 14. |
| [Microservices Guide](<./Hinglish/Detailed Notes/H. Microservices-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/H. Microservices-Interview-revision-notes.md>) | DDD/hexagonal boundaries, sync/async integration, database ownership, CQRS/event sourcing, Saga/Outbox/Inbox/idempotency, retries/circuit breakers/bulkheads/DLQ, contracts, deployment and tests — Sections 8–10, 14. |
| [Structured Logging Guide](<./Hinglish/Detailed Notes/I. Structured-Logging-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/I. Structured-Logging-Interview-revision-notes.md>) | Serilog templates/levels/sinks/enrichers, correlation and trace context, OpenTelemetry, ELK/Seq/Loki, sampling, async/source-generated logging, PII redaction and cost — Section 10. |
| [SQL Server Guide](<./Hinglish/Detailed Notes/J. SQL-Server-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/J. SQL-Server-Interview-revision-notes.md>), [SQL problem set](<./Hinglish/Detailed Notes/J.0 SQL interview problems.md>) and [solutions](<./Hinglish/Detailed Notes/J.1 sql interview solutions.md>) | Relational design, joins/CTEs/procedures/windows, indexes/plans/statistics/sniffing, transactions/locks/deadlocks/HA plus top-N, median, hierarchy, gaps-and-islands, sessionization, relational division, pivot, dedupe and missing-number drills — Sections 7 and 17. |
| [Angular 19 Guide](<./Hinglish/Detailed Notes/L. Angular-19-Interview-Guide.md>), [Angular Guide](<./Hinglish/Detailed Notes/L. Angular-Interview-Guide.md>), [alternate Angular guide](<./Hinglish/Detailed Notes/L. Angular-Interview-Guide (2).md>) + [revision notes](<./Hinglish/Revision Notes/L. Angular-Interview-revision-notes.md>) | Components/templates, standalone/NgModules, control flow, DI/lifecycle/routing/forms/HTTP, RxJS/state/NgRx/signals, change detection, SSR/hydration, PWA, security and accessibility — Section 12. |
| [JavaScript Guide](<./Hinglish/Detailed Notes/M. JavaScript-Interview-Guide.md>) | Hoisting/TDZ, coercion, `this`, prototypes/prototype pollution, event loop, microtasks/macrotasks, Promise combinators and ESM/CommonJS — Section 22. |
| [TypeScript Guide](<./Hinglish/Detailed Notes/M. TypeScript-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/M. TypeScript-Interview-revision-notes.md>) | Type inference, narrowing, `any`/`unknown`/`never`, generics/variance, utility/conditional/mapped/template types, readonly/branded types, modules and strict compiler settings — Section 13. |
| [System Design Guide](<./Hinglish/Detailed Notes/N. System-Design-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/N. System-Design-Interview-revision-notes.md>) | Requirements/SLO/capacity, caching, partition/sharding, queues/streams, CQRS/Saga/Outbox, resilience, tenancy/PII and URL-shortener/rate-limiter/notification designs; Excel/CQRS scenario — Sections 8–9, 18. |
| [GitHub Actions Guide](<./Hinglish/Detailed Notes/O. GitHub-Actions-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/O. GitHub-Actions-Interview-revision-notes.md>), [deployment guide](<./Hinglish/Detailed Notes/P. Deployment-Strategies-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/P. Deployment-Strategies-Interview-revision-notes.md>), [Artifactory Guide](<./Hinglish/Detailed Notes/Q. JFrog-Artifactory-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/Q. JFrog-Artifactory-Interview-revision-notes.md>) | Workflow security/OIDC/artifacts, .NET CI, approvals, release strategies, rollback/schema compatibility, package repositories, provenance/SBOM and promotion — Section 14. |
| [Agile Guide](<./Hinglish/Detailed Notes/R. Agile-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/R. Agile-Interview-revision-notes.md>) and [general interview guide](<./Hinglish/Detailed Notes/S. Interview-Questions-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/S. Interview-Questions-Interview-revision-notes.md>) | Scrum/Kanban, roles/events/artifacts, estimation, DoD/metrics/retrospectives, project deep dive, leadership, conflict, failure and HR scenarios — Sections 16 and 18. |
| [EF Core Guide](<./Hinglish/Detailed Notes/T. EF-Core-Interview-Guide.md>) + [revision notes](<./Hinglish/Revision Notes/T. EF-Core-Interview-revision-notes.md>) | DbContext/state/tracking, relationships/loading, migrations/concurrency/transactions, translation, N+1, split/compiled queries and testing — Section 5. |
| [Ports and Hosting Guide](<./Hinglish/Detailed Notes/U. Ports-and-Hosting-Explained-Simply.md>) + [revision notes](<./Hinglish/Revision Notes/U. Ports-and-Hosting-revision-notes.md>) | Ports/adapters, host/server/runtime boundaries and deployment model — Sections 4, 9 and 14. |
| [Docker Mastery (.NET + AWS)](<./Hinglish/Detailed Notes/V. Docker Mastery Guide Beginner to Advanced (.NET + AWS).md>) | Multi-stage .NET images, layer/build cache, BuildKit secrets, volumes/network/Compose, hardening/health/resources, ECR/ECS — Section 14. |
| [Runtime C# Script Engine / DLL Plugin revision](<./Hinglish/Revision Notes/V. Runtime C%23 Script Engine - DLL Plugin Pattern (Hinglish).md>) | Roslyn compile/diagnostics, reflection and script contract, cache/versioning, deployment alternatives, isolation and execution safety — Section 18. |
| [Pending topics](<./Hinglish/Pending topics>) | `IHttpClientFactory`, handler pooling, named/typed clients, socket exhaustion and DNS staleness — Section 4; outbound HTTP cancellation/timeouts/concurrency — Sections 1 and 4. |
| [AWS README](<./Hinglish/AWS/README.md>), [AWS Detailed folder](<./Hinglish/AWS/Detailed/>), [AWS Revision folder](<./Hinglish/AWS/Revision/>) | The matching 00–20 detail/revision pairs cover resume priority, Lambda, DynamoDB, IAM, IaC/CI-CD, S3, EC2/storage, observability, load balancing/autoscaling, ECS/Fargate, databases/cache/analytics, networking/security, messaging/streaming, edge, organizations/billing, cost/performance, migration/transfer, Well-Architected/resilience and cross-cutting updates — Sections 11 and 14. |
| [Migration master guide](<./Hinglish/System Design/Monolith-to-Microservices-Migration-Master-Interview-Guide-Hinglish.md>) and [Phase 1](<./Hinglish/System Design/Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-1.md>) through [Phase 11](<./Hinglish/System Design/Monolith-to-Microservices-Migration-Interview-Guide-Hinglish-Phase-11.md>) | Assessment/boundaries/Strangler, data ownership/CDC/cutover/reconciliation, REST/gRPC/events, gateway/auth/tenancy, observability/resilience, AWS/containers/CI-CD, Saga/CQRS/Outbox, testing/mock scenarios and resume/DMS/leadership — Sections 8–11, 14, 18. |

---

## 22. JavaScript language aur runtime

Angular/TypeScript ke saath JavaScript runtime semantics bhi interview scope hain; TypeScript annotations compile time par erase hote hain aur JavaScript execution behavior ko replace nahin karte.

### Q1. `var`, `let`, `const`, hoisting aur Temporal Dead Zone kya hain?

**Answer:** `var` function-scoped hota hai aur declaration hoist hokar `undefined` se initialize hoti hai; `let`/`const` block-scoped hote hain aur declaration se initialization tak Temporal Dead Zone mein access par `ReferenceError` dete hain. `const` binding reassign nahin hoti, par object properties automatically immutable nahin hoti.

**Pitfall:** “`let` hoist nahin hota” kehna technically misleading hai; binding scope mein exist karti hai but TDZ mein inaccessible. Loop callbacks mein `var` shared binding capture kar sakta hai; `let` per-iteration binding deta hai.

**Scenario:** `for (var i=0; i<3; i++) setTimeout(()=>console.log(i))` prints `3,3,3`; `let i` se `0,1,2`. Async closure ko explain karke fix karein, sirf output memorize na karein.

**Follow-ups**
- **Function declaration vs function expression?** Function declaration hoisted with callable definition; expression variable initialization rules follow karta hai.
- **`const` object mutate ho sakta hai?** Haan, binding constant hai, nested/object properties nahin; defensive immutability ke liye freeze/copy/type policy alag.

### Q2. JavaScript coercion, equality, `this` aur prototype chain?

**Answer:** `===` type coercion ke bina compare karta hai; `==` coercion rules apply karta hai, isliye app logic mein strict comparison prefer karein. `this` call-site par depend karta hai (method call, explicit `call/apply/bind`, constructor, strict-mode plain call); arrow function apna `this` bind nahin karti, lexical `this` capture karti hai. Property lookup prototype chain par traverse karta hai.

**Pitfall:** `this` ko function declaration ke location se permanently bound samajhna; `null`/`undefined`, `NaN`, object/string coercion edge cases; prototype property ko own property samajhna. Untrusted keys ko normal object par blindly merge karna prototype pollution risk bana sakta hai.

**Scenario:** Handler ko callback ke roop mein pass karne par `this` lose ho raha hai: arrow wrapper/bind se intentional context preserve karein; user-supplied object keys ko validate/allow-list karein and safe data structures/own-property checks use karein.

**Follow-ups**
- **`Object.create(null)` kab?** Prototype-less dictionary ke liye useful; missing built-in object methods ka trade-off hai. Modern `Map` often clearer.
- **`==` kab acceptable?** Deliberate, narrow nullish check (`x == null`) ko style/lint policy allow kar sakti hai; otherwise strict comparison avoids implicit conversions.

### Q3. Event loop, microtask aur macrotask order kaise samjhayenge?

**Answer:** JavaScript synchronous stack execute karta hai; stack empty hone par event loop queued work select karta hai. Promise reactions/`queueMicrotask` microtask queue mein; timers/most I/O callbacks task (often “macrotask”) queue mein. Current task ke baad microtasks drain hote hain before next task; browser rendering scheduling alag checkpoint par hoti hai.

**Pitfall:** “JavaScript single-threaded” se Web Worker/host I/O parallelism deny karna; exact timer timing guarantee assume karna; microtask recursively enqueue karke starvation cause karna.

**Scenario:** `console.log('A'); setTimeout(()=>console.log('timer')); Promise.resolve().then(()=>console.log('promise')); console.log('B')` typically prints A, B, promise, timer. Runtime/environment and timer delay boundaries note karein.

**Follow-ups**
- **CPU-heavy task UI freeze kyun?** Main thread event loop busy; chunk/yield or Web Worker/worker thread use karein, with serialization and cancellation trade-offs.
- **Microtask starvation?** Unbounded promise/queueMicrotask chain rendering or other tasks ko delay kar sakti; yield to a task/scheduler and bound work.

### Q4. `Promise.all`, `allSettled`, `race`, `any` ka difference?

**Answer:** `Promise.all` all fulfill par ordered results, first rejection par reject; `allSettled` sabka status/result deta hai; `race` first settled (fulfill/reject); `any` first fulfillment, sab reject par `AggregateError`. Input order results preserve hota hai, completion order nahin.

**Pitfall:** `Promise.all` rejection par baaki operations automatically cancel nahin karta. Unsafe writes ko parallel run karna, partial success ignore karna, ya `race` timeout mein loser request cancel hua samajhna.

**Scenario:** Dashboard independent read calls ke liye `allSettled` partial panels show karne deta hai; atomic “all required” result ke liye `all`; timeout race ke saath `AbortController` se cancellable fetch explicitly abort karein.

**Follow-ups**
- **Request cancellation?** `AbortController` signal supported operation ko pass karein; cancellation guarantee API/operation par depend karti hai.
- **Parallelism?** Promise concurrent asynchronous completion coordinate karti hai; synchronous CPU work automatically parallelize nahin hota.

### Q5. ESM aur CommonJS modules ka trade-off?

**Answer:** ESM static `import`/`export`, live bindings and top-level `await` support karta hai; browser-native ecosystem aur static analysis/tree-shaking mein useful. CommonJS `require`/`module.exports` runtime loading model hai, older Node ecosystem mein common. Package `type`, file extensions, bundler/runtime and dependency compatibility determine interop.

**Pitfall:** Dono module systems ko runtime par identical samajhna; named/default export interop, cyclic dependencies, module resolution and side effects ignore karna. Tree-shaking guarantee nahin—package metadata/import style/bundler matter.

**Scenario:** Node service ko ESM mein migrate karte waqt package scope, `.mjs`/`type: module`, `__dirname` replacements, test runner, build tools and transitive CommonJS dependencies verify karein; incremental boundary and CI tests rakhein.

**Follow-ups**
- **Circular imports?** Initialization order/partially initialized exports surprising behavior de sakte hain; dependency direction simplify karein and shared contract modules extract karein.
- **Frontend tree-shaking?** Static ESM graph helps bundler remove unreachable pure exports; side-effectful modules and incorrect `sideEffects` metadata removal/correctness issue create kar sakte hain.

---

## 23. References aur research limits

### How to interpret interview-question frequency

Question banks mein baar-baar listed themes—C#/OOP, ADO.NET, design patterns, LINQ, microservices, SQL, AWS, Angular, TypeScript, system design, Agile, EF aur behavioral examples—ko **frequently recurring preparation themes** kaha gaya hai. Yeh editorial question collections hain, measured hiring-frequency studies nahin; in se yeh prove nahin hota ki har company ya interviewer yahi poochta hai. ASP.NET Core security, logging, GitHub Actions, deployment aur Artifactory ke liye yahan official docs technical facts ground karte hain, par question frequency establish nahin karte.

Public interview reports anecdotal aur self-selected hote hain. Is update mein Microsoft aur Amazon ke candidate-reported rounds ko search-result excerpts ke basis par attribute kiya hai, aur original post ka direct URL independently verify na hone ko clearly mark kiya hai. **Exact 8–10 YOE + .NET full-stack + recent top-company report verify nahin hua**; Section 20 ke company reports adjacent evidence hain, exact target-profile proof nahin.

### Source list

#### Public interview-question collections

In collections ko **prep themes** ke evidence ke roop mein use kiya gaya hai—actual employer interview records ya statistical frequency ke roop mein nahin.

- [GeeksforGeeks — C# Interview Questions](https://www.geeksforgeeks.org/c-sharp/c-sharp-interview-questions/) — OOP, collections, LINQ, generics, delegates aur async jaise themes.
- [InterviewBit — .NET Interview Questions](https://www.interviewbit.com/dot-net-interview-questions/)
- [InterviewBit — ADO.NET Interview Questions](https://www.interviewbit.com/ado-net-interview-questions/) — provider, Connection, Command, DataReader, DataAdapter aur DataSet.
- [InterviewBit — Design Patterns Interview Questions](https://www.interviewbit.com/design-patterns-interview-questions/)
- [InterviewBit — ASP.NET Interview Questions](https://www.interviewbit.com/asp-net-interview-questions/) — broad ASP.NET collection; ise current ASP.NET Core-only frequency study na samjhein.
- [InterviewBit — Microservices Interview Questions](https://www.interviewbit.com/microservices-interview-questions/)
- [InterviewBit — SQL Interview Questions](https://www.interviewbit.com/sql-interview-questions/) — general SQL, SQL Server-specific study nahin.
- [InterviewBit — AWS Interview Questions](https://www.interviewbit.com/aws-interview-questions/)
- [InterviewBit — Angular Interview Questions](https://www.interviewbit.com/angular-interview-questions/)
- [InterviewBit — TypeScript Interview Questions](https://www.interviewbit.com/typescript-interview-questions/)
- [InterviewBit — System Design Interview Questions](https://www.interviewbit.com/system-design-interview-questions/)
- [InterviewBit — Agile Interview Questions](https://www.interviewbit.com/agile-interview-questions/)
- [InterviewBit — Entity Framework Interview Questions](https://www.interviewbit.com/entity-framework-interview-questions) — Entity Framework broadly, exclusively EF Core nahin.
- [InterviewBit — Behavioral Interview Questions](https://www.interviewbit.com/behavioral-interview-questions/)

#### Official technical documentation

Yeh references behavior/version-specific facts check karne ke liye hain; interview-frequency ka evidence nahin hain.

- [Microsoft Learn — C# Guide](https://learn.microsoft.com/en-us/dotnet/csharp/)
- [Microsoft Learn — ADO.NET Overview](https://learn.microsoft.com/en-us/dotnet/framework/data/adonet/ado-net-overview)
- [Microsoft Learn — ASP.NET Core security](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [Microsoft Learn — LINQ in C#](https://learn.microsoft.com/en-us/dotnet/csharp/linq/)
- [Microsoft Learn — ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-10.0)
- [Microsoft Learn — Create web APIs with ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0)
- [Microsoft Learn — .NET Microservices Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/)
- [Microsoft Learn — .NET logging overview](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging/overview)
- [Microsoft Learn — SQL Server performance monitoring and tuning](https://learn.microsoft.com/en-us/sql/relational-databases/performance/monitor-and-tune-for-performance?view=sql-server-ver17)
- [AWS — Well-Architected Framework](https://docs.aws.amazon.com/wellarchitected/latest/framework/welcome.html)
- [AWS — Lambda Developer Guide](https://docs.aws.amazon.com/lambda/latest/dg/welcome.html)
- [AWS — DynamoDB Developer Guide](https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html)
- [Angular — Overview](https://angular.dev/overview) and [Components](https://angular.dev/guide/components)
- [TypeScript — Handbook introduction](https://www.typescriptlang.org/docs/handbook/intro.html)
- [GitHub Docs — Understanding GitHub Actions](https://docs.github.com/en/actions/get-started/understand-github-actions)
- [AWS — Overview of deployment options](https://docs.aws.amazon.com/whitepapers/latest/overview-deployment-options/)
- JFrog — [Artifactory overview](https://docs.jfrog.com/artifactory/docs/jfrog-artifactory.md), [package management setup](https://docs.jfrog.com/artifactory/docs/set-up-package-management-in-artifactory.md), and [working with package management](https://docs.jfrog.com/artifactory/docs/working-with-package-management.md)
- [Manifesto for Agile Software Development](https://agilemanifesto.org/)
- [Microsoft Learn — EF Core](https://learn.microsoft.com/en-us/ef/core/)
- [OpenTelemetry documentation](https://opentelemetry.io/docs/)
- [MDN — JavaScript execution model](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Execution_model)
- [MDN — Using promises](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Using_promises)
- [MDN — JavaScript modules](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Modules)

#### Recent company interview reports and senior-.NET discussion

- [Microsoft India SSE — search-result lookup for the January 2026 job-fitment report](<https://search.brave.com/search?q=%22technical+job+fitment%22+Microsoft+SSE+%22Open+the+Lock%22+2026>) — surfaced report describes a 45-minute round, behavioral questions, Python code review and BFS *Open the Lock*. Search result did not expose a reliably retrievable original post URL; experience-years/.NET role were not stated.
- [Amazon SDE II India — search-result lookup for the May 2025 report](<https://search.brave.com/search?q=Amazon+SDE+II+India+interview+experience+May+16+2025+%223.5+YoE%22+parking+lot>) — surfaced candidate-reported experience (~3.5 years, Hyderabad), with Course Schedule II, ATM LLD, grocery parking-lot HLD and Reorganize String. This does not match 8–10 YOE or specify .NET.
- [r/dotnet — “Three interview questions to determine if somebody's a senior .NET developer?”](https://www.reddit.com/r/dotnet/comments/1olabmc/three_interview_questions_to_determine_if/) — discussion dated 1 Nov 2025; asks for senior .NET evaluation questions and gives `IEnumerable<T>` / `IQueryable<T>` / `IAsyncEnumerable<T>` as an example. This is community discussion, not a company interview report.
- [LeetCode — Open the Lock](https://leetcode.com/problems/open-the-lock/description/) and [Course Schedule II](https://leetcode.com/problems/course-schedule-ii/description/) — problem statements corresponding to reported coding prompts; these problem pages do not establish that a company asked them.
- [Deloitte — Software Developer interview-experience page](https://www.geeksforgeeks.org/interview-experiences/deloitte-interview-experience-for-software-developer/) was checked but reads as generic/sample guidance rather than a verifiable first-person question report, so it was not used as evidence for exact questions.

> Documentation links technical behavior ke liye hain; interview frequency/public-report claims alag evidence maangte hain. Framework/cloud features version ke saath evolve hote hain—interview se pehle current official docs check karein.
