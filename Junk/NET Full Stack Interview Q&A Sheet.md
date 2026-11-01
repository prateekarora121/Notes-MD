# SOTI .NET Full Stack Interview Q&A Sheet

Oct 16, 2026 · @Parteek

## How to use this sheet

This sheet has 184 questions with model answers, ordered by how heavily the SOTI job description leans on each topic: C#/.NET, MVC and Web API, SQL Server, Angular 19, design patterns, testing/BDD, compliance, then AWS and full-stack scenarios (sections are numbered 1 to 9 below).

**What the role asks for (from the job description):**

| Requirement | Where it is covered |
| --- | --- |
| 5+ years in software, product-company experience | Section 9 (scenarios, behavioural) |
| ASP.NET, MVC 4.0, Web API, .NET Framework 3.5+, C#, SQL Server | Sections 1 to 3 |
| Angular 19 and REST APIs | Section 4 |
| Design patterns, SOLID, best practices | Section 5 |
| Write code, unit tests and BDD independently | Section 6 |
| GDPR, HIPAA (good to have) | Section 7 |
| AWS (commonly asked for .NET full stack roles) | Section 8 |
| Polymorphism, abstraction, inheritance (OOP depth) | Part A (most detailed answers) |
| Middleware, DI and service lifetimes (Core and Framework) | Part B (most detailed answers) |

**Tip:** answer in the order *definition, when/why, a small example from your own project*. Replace the generic examples here with real ones from your work; interviewers at product companies probe the example, not the definition.

Answers are written for a 5+ year candidate, so they go beyond textbook definitions and include trade-offs.

## Part A. Object-Oriented Programming: Polymorphism, Abstraction, Inheritance

Interviewers at product companies almost always start here, because weak OOP fundamentals show up later as weak design. For every answer below, use this shape: define it in one sentence, explain *why it exists* (the problem it solves), show a short code example, then mention a trade-off or a real project where you used it.

**Q1.1. What are the four pillars of OOP? Explain each in your own words.**

| Pillar | What it means | Problem it solves | C# mechanism |
| --- | --- | --- | --- |
| Encapsulation | Bundle data with the code that operates on it and hide internal state behind a controlled interface | Stops outside code from putting an object into an invalid state | `private` fields, properties, access modifiers |
| Abstraction | Expose *what* an object does and hide *how* it does it | Lets callers depend on a simple contract instead of messy details | `interface`, `abstract class` |
| Inheritance | A class acquires the members of another class and can specialise them | Reuse of common behaviour; models "is-a" relationships | `class Dog : Animal` |
| Polymorphism | One interface, many implementations; the right behaviour is chosen based on the actual object | Lets you add new types without changing the code that uses them | `virtual`/`override`, interfaces, overloading, generics |

Good talking point: the pillars work together. A `PaymentProcessor` abstraction hides gateway details (abstraction), each gateway keeps its credentials private (encapsulation), gateways share a base class for logging and retries (inheritance), and checkout code calls `Process()` without caring which gateway it holds (polymorphism).

**Q1.2. What is polymorphism? What are its types?** Polymorphism means "many forms": the same call can behave differently depending on the object it is made on. It exists so that calling code can be written once against a general type and keep working as new specific types are added (this is the heart of the Open/Closed Principle).

Types in C#:

- **Compile-time (static) polymorphism**: the compiler picks the method. Achieved through method overloading (same name, different parameter lists), operator overloading, and generics.
- **Run-time (dynamic) polymorphism**: the CLR picks the method based on the actual type of the object at run time. Achieved through `virtual`/`abstract` methods that are `override`n in derived classes, and through interface implementations.
- **Parametric polymorphism**: generics such as `List<T>` work uniformly over many types.

```csharp
public abstract class Notification
{
    public abstract void Send(string message);   // contract
}
public class EmailNotification : Notification
{
    public override void Send(string message) => Console.WriteLine($"Email: {message}");
}
public class SmsNotification : Notification
{
    public override void Send(string message) => Console.WriteLine($"SMS: {message}");
}

List<Notification> channels = new() { new EmailNotification(), new SmsNotification() };
foreach (var c in channels) c.Send("Order shipped");   // right Send() chosen at run time
```

Adding `PushNotification` later requires no change to the loop. That is the real-world payoff.

**Q1.3. How does run-time polymorphism work internally? What is a virtual method table?** When a class has virtual methods, each type gets a method table that holds a slot per virtual method. Each object carries a hidden pointer to its type's method table. When you call `c.Send()` on a `Notification` reference, the runtime looks at the *object's* method table (not the variable's declared type), finds the slot for `Send`, and jumps to whichever implementation is stored there. This is called virtual dispatch or late binding. Non-virtual calls are resolved at compile time (early binding), which is slightly faster and can be inlined; that is one reason methods are non-virtual by default in C#.

**Q1.4. Method overloading vs method overriding?**

|  | Overloading | Overriding |
| --- | --- | --- |
| Where | Same class (or derived) | Derived class redefines a base method |
| Signature | Same name, *different* parameters | Same name and *same* signature |
| Binding | Compile time | Run time |
| Keywords | none | `virtual` / `abstract` in base, `override` in derived |
| Return type alone can differ? | No, it cannot be the only difference | Must match (covariant returns allowed since C# 9) |

Interview trap: overload resolution uses the *declared* (compile-time) types of the arguments, not the run-time types. Overriding uses the run-time type of the object the method is called on.

**Q1.5. `virtual`, `override`, `new` and `sealed`: what is the difference? (The classic output question.)**

- `virtual` in the base class allows derived classes to override.
- `override` replaces the base implementation; calls through a base reference reach the derived version.
- `new` (method hiding) declares a *separate* method that merely has the same name; calls through a base reference still reach the base version.
- `sealed override` stops further overriding down the chain; a `sealed` class cannot be inherited.

```csharp
class A { public virtual void Show() => Console.WriteLine("A"); }
class B : A { public override void Show() => Console.WriteLine("B"); }
class C : A { public new void Show() => Console.WriteLine("C"); }

A x = new B(); x.Show();   // prints B  (override: runtime type decides)
A y = new C(); y.Show();   // prints A  (hiding: declared type decides)
C z = new C(); z.Show();   // prints C
```

Explain it as: override changes the *slot* in the method table; `new` adds a *new* slot and leaves the old one untouched. Hiding is usually a code smell and the compiler warns about it; mention it only to show you understand the difference.

**Q1.6. What is abstraction, and how is it different from encapsulation?** Abstraction is about *design*: showing only the essential features (the contract) and hiding implementation complexity, so callers depend on `IPaymentGateway.Charge()` and not on HTTP calls, retries and XML parsing. Encapsulation is about *protection*: keeping an object's data private and exposing it only through controlled members so invariants hold (for example `Balance` can only change through `Deposit` and `Withdraw`). A handy one-liner: abstraction hides complexity at the *design* level; encapsulation hides data at the *implementation* level. You achieve abstraction with interfaces and abstract classes, and encapsulation with access modifiers.

**Q1.7. Abstract class vs interface: how do you choose? Give a decision rule.**

|  | Abstract class | Interface |
| --- | --- | --- |
| Purpose | Shared base with common state and partial implementation ("is-a") | Pure capability contract ("can-do") |
| Inheritance | A class can inherit only one | A class can implement many |
| State (fields) | Yes | No instance fields (static members allowed) |
| Constructors | Yes (called by derived constructors) | No |
| Access modifiers on members | Any | Public by default; C# 8+ allows others on default members |
| Implementation | Mix of abstract and concrete members | Members abstract; C# 8+ allows default implementations |
| Versioning | Adding a concrete member is non-breaking | Adding an abstract member breaks implementers (default methods soften this) |

Decision rule: if types share *code and state* and are closely related, use an abstract class (for example `BaseRepository` with a shared `DbContext`). If unrelated types need to share a *capability* (`IDisposable`, `IComparable`, `ILogger`), or you need multiple inheritance of contract or easy mocking, use an interface. In practice, depend on interfaces and use abstract classes as optional helpers that implement them. A common combo: `interface IReport` plus `abstract class ReportBase : IReport` with the boilerplate.

**Q1.8. Can an abstract class have a constructor? Can you instantiate it? Can an interface have a constructor?** An abstract class can (and often does) have constructors; they run when a derived class is created, via `base(...)`, to initialise shared state. You cannot instantiate it directly with `new`, but you can hold a reference of its type that points to a derived object. An interface cannot have instance constructors because it has no state to initialise. An abstract class can also contain non-abstract methods, properties, fields, and even be fully concrete in members while still marked `abstract` to prevent instantiation.

**Q1.9. What is inheritance and what are its types? Why does C# not allow multiple class inheritance?** Inheritance lets a derived class reuse and extend a base class, modelling an "is-a" relationship. Forms: single (`B : A`), multilevel (`C : B : A`), and hierarchical (many classes derive from one base). C# supports **single class inheritance plus multiple interface implementation**. Multiple class inheritance is avoided because of the *diamond problem*: if `D` inherits from `B` and `C`, which both inherit `A` and override the same method, it becomes ambiguous which version `D` gets, and object layout and constructor order get complicated. Interfaces avoid the issue because they carry no state, and when two interfaces clash you resolve it with explicit implementation:

```csharp
interface IA { void Run(); }
interface IB { void Run(); }
class Worker : IA, IB
{
    void IA.Run() => Console.WriteLine("IA");
    void IB.Run() => Console.WriteLine("IB");
}
```

**Q1.10. In what order do constructors run in an inheritance chain? What do `base` and `this` do?** Base-class constructors run first, then derived: for `new C()` where `C : B : A`, the order is A, B, C. Field initialisers run before the constructor body, and derived field initialisers run before the base constructor. `base(args)` selects which base constructor to call (if none is written, the parameterless one is called, and compilation fails if it does not exist). `this(args)` chains to another constructor in the same class. Pitfall: calling a virtual method from a base constructor runs the *derived* override before the derived constructor has finished, so derived fields may not be initialised yet. Avoid it.

**Q1.11. Inheritance vs composition: which do you prefer and why?** Prefer composition ("has-a") over inheritance ("is-a") unless there is a true, stable is-a relationship. Inheritance creates tight coupling: derived classes depend on base internals (the *fragile base class* problem), hierarchies become deep and hard to change, and you inherit members you may not want. Composition keeps classes small and swappable at run time through injected dependencies. Example: instead of `class LoggingOrderService : OrderService`, wrap an `IOrderService` in a decorator that adds logging. Use inheritance when you genuinely need substitutability and shared state, such as `ApiController` derived classes or a template base class; keep hierarchies shallow (two or three levels).

**Q1.12. What is the Liskov Substitution Principle in relation to inheritance? Give a violation.** Anywhere a base type is expected, any derived type must work without surprising behaviour. Classic violation: `Square : Rectangle` where setting `Width` also changes `Height`; code that assumes width and height are independent breaks. Another: a derived class throwing `NotSupportedException` for a base method (`ReadOnlyCollection` posing as a writable list). Smells to mention: derived classes that strengthen preconditions, weaken postconditions, or throw new exceptions. Fix: redesign the hierarchy (separate `IShape` with `Area`) or use composition.

**Q1.13. Explain upcasting, downcasting, and the `is` / `as` / pattern-matching operators.** Upcasting (derived to base) is implicit and always safe. Downcasting (base to derived) needs an explicit cast and can fail with `InvalidCastException`. `as` returns `null` instead of throwing; `is` tests the type, and C# 7+ pattern matching combines test and cast:

```csharp
if (shape is Circle c) Console.WriteLine(c.Radius);
var area = shape switch { Circle c => Math.PI * c.Radius * c.Radius, Rectangle r => r.W * r.H, _ => 0 };
```

A heavy use of downcasting usually signals that polymorphism is missing: move the type-specific behaviour into a virtual method instead of switching on type.

**Q1.14. What is the Template Method pattern and how does it use inheritance and abstraction?** The base class defines the skeleton of an algorithm as a non-virtual method and leaves specific steps as abstract or virtual hooks for subclasses. It enforces the order of steps while allowing variation.

```csharp
public abstract class ReportGenerator
{
    public string Generate()            // the fixed algorithm
    {
        var data = Load();
        var body = Format(data);
        return Header() + body;
    }
    protected abstract IEnumerable<string> Load();
    protected abstract string Format(IEnumerable<string> data);
    protected virtual string Header() => "Report\n";   // optional hook
}
```

ASP.NET gives you this style in many places (`Controller` lifecycle methods, `DbContext.OnModelCreating`).

**Q1.15. What are access modifiers, and how does `protected` differ from `private protected` and `internal`?** `public`: everywhere. `private`: same class only. `protected`: the class and its derived classes (even in other assemblies). `internal`: same assembly. `protected internal`: derived classes *or* same assembly. `private protected` (C# 7.2): derived classes *and* in the same assembly. Principle: expose the minimum necessary. Public surface is a promise you must keep.

**Q1.16. What do static, sealed, partial and abstract classes mean? Can they be combined?** `static`: cannot be instantiated or inherited, only static members (utility and extension methods). `sealed`: cannot be inherited (can be instantiated); good for security and slight performance gains. `abstract`: cannot be instantiated, meant to be inherited. `partial`: class split across files (designer-generated code). `abstract` and `sealed` together are contradictory and not allowed; `static` classes are implicitly sealed and abstract.

**Q1.17. How do properties, auto-properties, `init` and `records` fit with encapsulation?** Properties let you keep a private backing field while exposing validated get/set logic and change implementation later without breaking callers. `init`-only setters allow assignment only during object initialisation, producing immutable-after-construction objects. `record` types give value-based equality and non-destructive mutation with `with`, ideal for DTOs and value objects. Prefer immutability for shared and multi-threaded data.

**Q1.18. How did you apply OOP in a real project? (Prepare this one yourself.)** Template answer to adapt: "In our notification module, we defined `INotificationChannel` with `Send`. `EmailChannel`, `SmsChannel` and `PushChannel` implemented it, and a `NotificationService` received `IEnumerable<INotificationChannel>` by constructor injection, so adding WhatsApp meant adding one class and one DI registration, with no change to existing code. Shared retry and logging lived in an abstract `ChannelBase`, and the `Template Method` kept the send pipeline consistent. Each channel was unit tested in isolation with fakes." Then mention what you would change in hindsight (for example, replace the base class with a decorator for retries).

## Part B. Middleware, Dependency Injection and Service Lifetimes

These topics decide whether an interviewer sees you as someone who has only used ASP.NET, or someone who understands how the request actually flows and how objects live and die. Expect them in almost every .NET full-stack round. The job description mentions MVC 4.0 and .NET Framework 3.5+, so the answers cover both the modern ASP.NET Core model and the classic Framework equivalents (HTTP modules, message handlers, OWIN).

### B1. Middleware

**Q2.1. What is middleware in ASP.NET Core, and how does the request pipeline work?** Middleware is a component that sits in the HTTP request pipeline and can inspect, modify, or short-circuit a request, and then do work on the way back out as the response travels in reverse. The pipeline is a chain: each component receives the `HttpContext` and a `next` delegate. Calling `await next(context)` passes control to the following component; code written *after* that call runs when the response flows back. If a component does not call `next`, the pipeline is *short-circuited*: nothing further down runs, and the response is produced right there (this is how static files, CORS preflight responses, and authorization failures can end a request early).

A good way to explain it in an interview is the "onion" or "Russian dolls" picture: the request goes inward through each layer, reaches the endpoint (your controller or minimal API), and the response unwinds outward through the same layers in reverse order. Every layer therefore gets two chances to act: before the endpoint (request phase) and after it (response phase).

```csharp
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();          // request phase
    await next(context);                    // hand over to the next middleware
    sw.Stop();                              // response phase
    app.Logger.LogInformation("{Path} took {Ms} ms", context.Request.Path, sw.ElapsedMilliseconds);
});
```

**Q2.2. Explain `Use`, `Run` and `Map` (and `UseWhen`).**

| Method | What it does | When to use |
| --- | --- | --- |
| `app.Use(...)` | Adds middleware that can call `next` to continue the pipeline | Cross-cutting work: logging, headers, timing |
| `app.Run(...)` | Terminal middleware; has no `next`, so it always ends the pipeline | Final fallback handler, quick demos |
| `app.Map("/path", ...)` | Branches the pipeline for requests whose path starts with the given prefix; the branch does not rejoin the main pipeline | Mounting a separate mini-pipeline, e.g. `/health` or `/admin` |
| `app.UseWhen(predicate, ...)` | Branches on any condition, but *rejoins* the main pipeline afterwards | Apply extra middleware only to some requests (e.g. API calls) |

The key difference to state clearly: `Map` branches are terminal (they never rejoin), while `UseWhen` branches rejoin the main pipeline once the branch finishes.

**Q2.3. Why does the order of middleware matter? Give the recommended order.** Middleware runs in exactly the order it is registered on the request path, and in reverse on the response path. Order is therefore a correctness and security concern, not a style choice. Examples of what goes wrong: placing `UseAuthorization` before `UseAuthentication` means the user is never identified before the policy is checked; placing `UseCors` after authentication can cause preflight `OPTIONS` requests to be rejected with 401 because they carry no credentials; placing the exception handler last means it cannot catch errors thrown by earlier components.

A typical safe order is:

1. Exception handling (`UseExceptionHandler`, or `UseDeveloperExceptionPage` in development) so it wraps everything
2. HSTS and HTTPS redirection
3. Static files (short-circuits cheaply before heavier work)
4. Routing (`UseRouting`) so endpoint metadata is known
5. CORS (`UseCors`), after routing and before auth
6. Authentication (`UseAuthentication`), then Authorization (`UseAuthorization`)
7. Rate limiting, response caching and compression where used
8. Endpoints (`MapControllers`, `MapGet`, etc.)

In .NET 6+ minimal hosting, routing and endpoint middleware are added implicitly, but you still control where auth, CORS and your own middleware sit relative to them.

**Q2.4. How do you write custom middleware? What are the different styles?** There are three ways, and naming all three shows depth.

```csharp
// 1. Inline lambda: fine for something tiny
app.Use(async (ctx, next) => { ctx.Response.Headers["X-Frame-Options"] = "DENY"; await next(ctx); });

// 2. Convention-based class: the most common style
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;   // singleton-like: created once

    public async Task InvokeAsync(HttpContext ctx, ILogger<CorrelationIdMiddleware> log)  // scoped services go here
    {
        var id = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        ctx.Response.Headers["X-Correlation-Id"] = id;
        using (log.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = id }))
            await _next(ctx);
    }
}
app.UseMiddleware<CorrelationIdMiddleware>();

// 3. Factory-based: implements IMiddleware, resolved from DI per request
public class AuditMiddleware : IMiddleware
{
    private readonly IAuditService _audit;
    public AuditMiddleware(IAuditService audit) => _audit = audit;          // can be scoped
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next) { await next(ctx); await _audit.WriteAsync(ctx); }
}
builder.Services.AddScoped<AuditMiddleware>();   // must be registered
app.UseMiddleware<AuditMiddleware>();
```

The important lifetime point: a convention-based middleware class is constructed **once** at startup, so its constructor can only safely take singleton-compatible dependencies. Per-request (scoped) services must be taken as parameters of `InvokeAsync`, which is resolved per request. Alternatively use `IMiddleware`, which the container creates for each request and therefore allows scoped dependencies in the constructor.

**Q2.5. Middleware vs filters vs endpoint handlers: what is the difference, and when do you choose which?**

| Aspect | Middleware | MVC / API filters |
| --- | --- | --- |
| Scope | The whole app: static files, MVC, SignalR, minimal APIs | Only MVC controllers and Razor Pages |
| Knows about | `HttpContext` only (raw request and response) | Model binding, action arguments, `ActionResult`, `ModelState`, route data |
| Runs | Early, before routing to the endpoint (for most) | Inside the MVC pipeline, after the endpoint is chosen |
| Typical use | Logging, CORS, auth, compression, global exception handling, correlation IDs | Per-action authorization, validation, result formatting, action-specific caching |

Rule of thumb: if the concern applies to *every* request regardless of what handles it, use middleware; if it needs MVC knowledge (which action, which model, which result), use a filter. Global exception handling is a good example of a judgment call: middleware catches everything (including failures outside MVC), while an exception filter only sees exceptions thrown inside controllers and actions.

**Q2.6. How do you handle exceptions globally?** In ASP.NET Core, the standard approach is exception-handling middleware registered first so it wraps the entire pipeline: `app.UseExceptionHandler(...)`, or in .NET 8 an `IExceptionHandler` implementation registered with `AddExceptionHandler<T>()` plus `AddProblemDetails()`, which returns RFC 7807 `application/problem+json` responses. The handler should log once with a correlation ID, map known exception types to proper status codes (validation to 400, not found to 404, conflict to 409), and never leak stack traces to the client in production. In Web API 2 on the Framework, the equivalent is `IExceptionHandler`/`IExceptionLogger` registered on `HttpConfiguration.Services`, and in MVC 4/5 an `OnException` override or a global `HandleErrorAttribute` plus `Application_Error` in `Global.asax` as a last resort.

**Q2.7. What is the classic .NET Framework equivalent of middleware? Explain HTTP modules, HTTP handlers, message handlers and OWIN.** Since the role uses MVC 4.0, Web API and .NET Framework, this is a likely question. The pieces map as follows:

| Concept | Where it lives | Role |
| --- | --- | --- |
| HTTP module (`IHttpModule`) | ASP.NET / IIS pipeline | Hooks into pipeline events (`BeginRequest`, `AuthenticateRequest`, `EndRequest`); applies to every request. Closest match to middleware in the old IIS integrated pipeline |
| HTTP handler (`IHttpHandler`) | ASP.NET / IIS pipeline | The endpoint that actually produces the response (.aspx, .ashx). Like the terminal `Run` middleware |
| Web API message handler (`DelegatingHandler`) | Web API pipeline | Wraps request and response for Web API only; chained, each can call `base.SendAsync` or short-circuit. Used for API keys, logging, correlation IDs |
| OWIN middleware (Katana) | `Startup.cs` with `app.Use(...)` | Host-independent middleware for the Framework; the model ASP.NET Core adopted. Used for authentication (JWT, cookies, OAuth) |
| MVC filters | MVC pipeline | Per-action cross-cutting logic (authorize, action, result, exception) |

A strong closing line: ASP.NET Core unified HTTP modules, handlers and message handlers into one middleware model, which is why migrating a legacy app usually means rewriting each module or handler as middleware.

**Q2.8. Scenario: write middleware that logs every request with a correlation ID and handles unexpected errors. What pitfalls do you watch for?** Place it near the top so it observes everything. Read or create the correlation ID, add it to response headers *before* calling `next` (headers cannot be changed after the response has started), wrap `await next(ctx)` in try/catch, log with the ID, and return a problem-details body with status 500 when nothing has been written yet (`if (!ctx.Response.HasStarted)`). Pitfalls:

- Writing to `Response` or setting headers after `next` has already started the response throws; always check `HasStarted`.
- Reading `Request.Body` consumes the stream; call `Request.EnableBuffering()` and rewind the position if later components also need it.
- Replacing `Response.Body` with a `MemoryStream` to capture output must be restored afterwards, and costs memory on large responses.
- Do not hold per-request state in middleware fields (the instance is shared across requests); keep it in local variables or `HttpContext.Items`.
- Never swallow exceptions without logging; never log passwords, tokens or other PII, which also matters for GDPR and HIPAA.

### B2. Dependency Injection and Service Lifetimes

**Q2.9. What are the three service lifetimes, and how do you choose?** The built-in container registers each service with a lifetime that controls how long an instance lives and who shares it.

| Lifetime | Registration | Instance behaviour | Typical use |
| --- | --- | --- | --- |
| Transient | `AddTransient<I, T>()` | A new instance every time it is requested from the container | Lightweight, stateless services; validators, mappers, small helpers |
| Scoped | `AddScoped<I, T>()` | One instance per scope; in a web app a scope is one HTTP request, so everything in that request shares it | `DbContext`, unit of work, per-request user context, repositories |
| Singleton | `AddSingleton<I, T>()` | One instance for the whole application lifetime, shared by all requests and threads | Configuration, caches, `HttpClient` factories, stateless services that are expensive to build |

How to choose: start from *state and thread-safety*. If the service holds per-request state (like a `DbContext`, which is not thread-safe) use scoped. If it is stateless and cheap, transient is the safe default. If it is stateless or carefully thread-safe and expensive to create, or it deliberately holds shared state like a cache, use singleton. In interviews, always mention that a singleton **must be thread-safe** because many requests use it simultaneously.

**Q2.10. What is a captive dependency, and how do you avoid it?** A captive dependency occurs when a longer-lived service holds on to a shorter-lived one. The classic case: a singleton that takes a scoped service (for example a `DbContext`) in its constructor. The scoped instance gets "captured" by the singleton and effectively lives forever, shared across all requests and threads, causing stale data, concurrency errors (`DbContext` is not thread-safe) and memory growth. The rule is that a service may only depend on services with the **same or longer** lifetime: transient can depend on anything, scoped can depend on scoped or singleton, singleton should depend only on singletons.

How to avoid it: ASP.NET Core can detect it for you; in development the host enables `ValidateScopes` and `ValidateOnBuild` by default, throwing an error like "Cannot consume scoped service from singleton". You can force this in any environment with `new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true })`. If a singleton genuinely needs a scoped service, inject `IServiceScopeFactory` and create a scope for the unit of work:

```csharp
public class ReportWorker : BackgroundService          // hosted services are singletons
{
    private readonly IServiceScopeFactory _scopeFactory;
    public ReportWorker(IServiceScopeFactory f) => _scopeFactory = f;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();             // new scope = new DbContext
            var repo = scope.ServiceProvider.GetRequiredService<IReportRepository>();
            await repo.GenerateAsync(ct);
            await Task.Delay(TimeSpan.FromMinutes(5), ct);
        }
    }
}
```

**Q2.11. Who disposes services, and what are the rules?** The container disposes the `IDisposable`/`IAsyncDisposable` services it *creates*, when their scope ends: scoped and transient instances are disposed at the end of the request scope, and singletons when the application shuts down. It does **not** dispose instances you create yourself and register by passing an instance (`AddSingleton(new MyService())`), because you own them. Transient disposables are a subtle trap: they are held until the scope ends, so resolving many transient disposable objects inside a long-lived singleton scope leaks memory. Never call `Dispose` yourself on something the container gave you.

**Q2.12. How should `DbContext` and `HttpClient` be registered, and why?** `DbContext` should be scoped (`AddDbContext<T>` does this by default): one instance per request keeps change tracking consistent within the request and avoids sharing a non-thread-safe object. For work outside a request, such as background jobs, use `AddDbContextFactory<T>` or create a scope. `HttpClient` should not be created per call with `new` (this can exhaust sockets) nor kept as a naive singleton forever (this ignores DNS changes). Use `IHttpClientFactory` (`AddHttpClient`), which pools and rotates the underlying handlers, supports named and typed clients, and integrates with Polly for retries and circuit breakers.

**Q2.13. `IOptions`, `IOptionsSnapshot` and `IOptionsMonitor`: what is the difference?**

| Interface | Lifetime | Picks up config changes? | Use for |
| --- | --- | --- | --- |
| `IOptions<T>` | Singleton | No; values are read once at startup | Settings that never change at runtime |
| `IOptionsSnapshot<T>` | Scoped | Yes, re-read per request | Settings you want fresh on each request; cannot be injected into singletons |
| `IOptionsMonitor<T>` | Singleton | Yes, with change notifications (`OnChange`) | Singletons or background services needing live config |

This is a favourite follow-up because it tests lifetime knowledge: `IOptionsSnapshot` is scoped, so injecting it into a singleton is itself a captive dependency.

**Q2.14. What are the different ways to register services, and what are keyed services?**

- Interface to implementation: `AddScoped<IOrderService, OrderService>()`.
- Concrete type only: `AddScoped<OrderService>()`.
- Factory delegate when construction needs logic: `AddScoped<IClock>(sp => new Clock(sp.GetRequiredService<IOptions<ClockOptions>>().Value))`.
- Open generics: `AddScoped(typeof(IRepository<>), typeof(Repository<>))` resolves `IRepository<Order>` automatically.
- Multiple implementations of one interface: the last registration wins for `GetService<T>`, but injecting `IEnumerable<T>` gives all of them in registration order (useful for the Strategy pattern, such as several notification channels).
- `TryAdd...` registers only if nothing is registered yet, which library authors use to let apps override defaults.
- Keyed services (.NET 8): `AddKeyedScoped<IPayment, StripePayment>("stripe")` and `[FromKeyedServices("stripe")]` select an implementation by key, replacing hand-written factory lookups.

Follow-up warning: injecting `IServiceProvider` and calling `GetService` inside your classes is the *service locator* anti-pattern; it hides dependencies and makes testing harder. Use constructor injection, and keep `IServiceProvider` for factories, middleware infrastructure, and scope creation.

**Q2.15. How is dependency injection done in ASP.NET MVC 4 / Web API 2 on .NET Framework, which have no built-in container?** There is no default container, so you plug one in: implement or use an adapter for `IDependencyResolver` (MVC: `DependencyResolver.SetResolver`, Web API: `GlobalConfiguration.Configuration.DependencyResolver`) backed by Autofac, Unity, Ninject or Simple Injector. Constructor injection into controllers then works. Lifetime names differ by container, so know the mapping:

| .NET Core term | Autofac | Unity | Ninject |
| --- | --- | --- | --- |
| Transient | `InstancePerDependency()` (default) | `TransientLifetimeManager` | `InTransientScope()` |
| Scoped (per request) | `InstancePerRequest()` | `HierarchicalLifetimeManager` per child container | `InRequestScope()` |
| Singleton | `SingleInstance()` | `ContainerControlledLifetimeManager` | `InSingletonScope()` |

Per-request lifetime must be released correctly: the resolver has to create a child scope per request and dispose it at the end, otherwise per-request services leak. This is also why migrating to ASP.NET Core is attractive: container, lifetimes and scope validation come built in.

**Q2.16. A bug report says "user A sometimes sees user B's data". How would service lifetimes explain it, and how do you debug it?** This is a classic scenario question. The likely cause is state that is shared across requests: a service holding the current user, a cached entity, or a `DbContext` registered as a singleton (or captured by one), or a static field or singleton storing per-request data. Because singletons live for the whole app, the first user's data is visible to the next user. Debug by checking registrations for anything per-user registered as singleton, enabling `ValidateScopes`/`ValidateOnBuild`, searching for `static` mutable state, and adding the correlation ID plus user ID to logs to confirm which request touched which instance. The fix is to register per-user state as scoped (or derive it from `HttpContext` through `IHttpContextAccessor` inside a scoped service), and to make any singleton immutable or truly thread-safe.

**Q2.17. How do you test code that depends on service lifetimes or middleware?** For middleware, write unit tests using `DefaultHttpContext` with a stub `next` delegate and assert on headers, status codes and whether `next` was called; for end-to-end behaviour use `WebApplicationFactory<TEntryPoint>` to run the real pipeline in memory and send requests with `HttpClient`. For DI, test classes with fakes passed to the constructor (no container needed), and add one integration test that builds the real `ServiceProvider` with `ValidateOnBuild = true` and resolves your key services, which catches missing registrations and captive dependencies before production.

## 1. C# and .NET Framework

**Q1. Value types vs reference types? Where do they live in memory?** Value types (int, struct, enum, DateTime) hold data directly and are copied on assignment; reference types (class, string, array, delegate) hold a reference to a heap object. Value-type locals usually sit on the stack, but a struct that is a field of a class lives inside that object on the heap. Boxing wraps a value type in an object on the heap, and costs an allocation; avoid it in hot paths (e.g. non-generic ArrayList).

**Q2. How does garbage collection work in .NET?** The GC is generational: Gen 0 (short-lived), Gen 1 (buffer), Gen 2 (long-lived). New objects start in Gen 0; survivors are promoted. Objects of 85,000 bytes or more go to the Large Object Heap, which is collected only with Gen 2. The GC is non-deterministic, so unmanaged resources need `IDisposable`.

**Q3. Dispose vs Finalize? Explain the dispose pattern.** Finalize (the `~Class` destructor) is called by the GC at an unknown time and delays collection of the object. `Dispose` is called deterministically, usually via `using`. The pattern: implement `IDisposable`, have `Dispose(bool disposing)` free managed resources only when `disposing` is true and unmanaged resources always, call `GC.SuppressFinalize(this)`, and add a finalizer only if you hold unmanaged resources directly.

**Q4. `string` vs `StringBuilder`?** Strings are immutable, so every concatenation creates a new object. Use `StringBuilder` for many concatenations in a loop; for a few, plain `+` or interpolation is fine.

**Q5. `==` vs `Equals()` vs `ReferenceEquals`?** `ReferenceEquals` compares identity. `==` is overloadable: for classes it defaults to reference equality, but `string` overloads it to compare values. `Equals` is virtual and can be overridden for value equality; if you override it, also override `GetHashCode`.

**Q6. `abstract class` vs `interface`? What changed with default interface methods (C# 8)?** Abstract class: can hold state, constructors and partial implementation; single inheritance. Interface: a contract; multiple implementation. C# 8 allows default implementations in interfaces, but they are mainly for evolving APIs without breaking implementers, not a replacement for abstract classes.

**Q7. `ref`, `out` and `in` parameters?** `ref` passes by reference and the variable must be initialised first. `out` must be assigned by the called method. `in` passes by reference read-only (avoids copying large structs).

**Q8. `const` vs `readonly` vs `static readonly`?** `const` is a compile-time constant baked into the calling assembly (changing it requires recompiling consumers). `readonly` is assigned at declaration or in the constructor, evaluated at runtime. `static readonly` is one value per type, set at runtime.

**Q9. What are delegates, `Func`, `Action`, `Predicate` and events?** A delegate is a type-safe function pointer. `Func<T,TResult>` returns a value, `Action<T>` returns void, `Predicate<T>` returns bool. An event is a delegate with restricted access: outside code can only subscribe or unsubscribe, not invoke it. Forgetting to unsubscribe is a classic memory leak.

**Q10. Explain LINQ deferred vs immediate execution.** Queries like `Where` and `Select` are deferred: they run when enumerated (`foreach`, `ToList`, `Count`, `First`). Calling `ToList()` or `ToArray()` forces immediate execution. Danger: enumerating a deferred query twice runs it twice (and hits the database twice with EF). `IEnumerable<T>` filters in memory; `IQueryable<T>` builds an expression tree that the provider translates to SQL.

**Q11. `async`/`await`: how does it work, and what is the deadlock risk?** The compiler turns an async method into a state machine; `await` releases the thread until the task completes and then resumes on the captured context. In ASP.NET (classic) or UI apps, calling `.Result` or `.Wait()` on an async call blocks the context thread that the continuation needs, causing a deadlock. Fix: `await` all the way up, and use `ConfigureAwait(false)` in library code. In .NET Framework 4.5+ async works in MVC 4+ and Web API.

**Q12. `Task` vs `Thread`? What is `ThreadPool`?** A `Thread` is an OS thread (expensive). A `Task` is a unit of work scheduled on the thread pool and supports continuations, cancellation (`CancellationToken`) and return values. Async I/O tasks do not consume a thread while waiting.

**Q13. `IEnumerable` vs `ICollection` vs `IList` vs `List<T>`?** `IEnumerable` = forward iteration only. `ICollection` adds Count/Add/Remove. `IList` adds index access. `List<T>` is the concrete implementation. Accept the narrowest interface you need as a parameter; return an interface from public APIs to keep flexibility.

**Q14. What are generics, and what are constraints?** Generics give type safety and avoid boxing/casting. Constraints (`where T : class, new()`, `where T : IComparable<T>`) tell the compiler what operations are allowed on `T`. Covariance (`out`) and contravariance (`in`) apply to generic interfaces and delegates.

**Q15. Extension methods, `var`, `dynamic`, anonymous types: what are they?** Extension methods are static methods in static classes, called as instance methods (`this` first parameter). `var` is compile-time inferred typing. `dynamic` defers type resolution to runtime. Anonymous types are compiler-generated immutable classes.

**Q16. Explain the .NET Framework architecture: CLR, CLI, CTS, CLS, JIT, assemblies.** Source compiles to IL in an assembly; the CLR JIT-compiles IL to native code at runtime and manages memory, security and exceptions. CTS defines the common type system; CLS the subset of features all .NET languages support. The GAC stores shared strong-named assemblies.

**Q17. What is the difference between .NET Framework and .NET (Core / 5+)?** .NET Framework is Windows-only, tied to IIS/System.Web. Modern .NET is cross-platform, open source, faster, modular (NuGet), with built-in DI and the Kestrel server, and is where new features ship. For a legacy MVC 4 / .NET Framework codebase, a migration path is: .NET Upgrade Assistant, move shared logic to .NET Standard libraries, and strangle the monolith piece by piece.

**Q18. Exception handling best practices.** Catch only what you can handle; rethrow with `throw;` (not `throw ex;`, which resets the stack trace); use specific exception types; use `finally` or `using` for cleanup; never swallow exceptions silently; log once at the boundary; don't use exceptions for normal control flow.

**Q19. What is the difference between `Task.Run`, `Task.Factory.StartNew`, and `Parallel.For`?** `Task.Run` is the simple way to push CPU-bound work to the thread pool. `StartNew` has more options but pitfalls (default scheduler, unwrapping). `Parallel.For` partitions a CPU-bound loop across cores. None of these are appropriate for I/O-bound work; use real async APIs.

**Q20. What are `IEnumerable` `yield return` and lazy evaluation?** `yield return` makes the compiler generate an iterator state machine, producing items one at a time on demand without building the full collection in memory.

**Q21. Write code: reverse a string, then check for a palindrome, in C#.**

```csharp
public static bool IsPalindrome(string s)
{
    if (string.IsNullOrEmpty(s)) return true;
    int i = 0, j = s.Length - 1;
    while (i < j)
    {
        while (i < j && !char.IsLetterOrDigit(s[i])) i++;
        while (i < j && !char.IsLetterOrDigit(s[j])) j--;
        if (char.ToLowerInvariant(s[i++]) != char.ToLowerInvariant(s[j--])) return false;
    }
    return true;
}
```

Two pointers, O(n) time, O(1) extra space; no string reversal needed.

**Q22. Write code: find the second highest salary using LINQ, and group employees by department.**

```csharp
var second = employees.Select(e => e.Salary).Distinct()
                      .OrderByDescending(s => s).Skip(1).FirstOrDefault();

var byDept = employees.GroupBy(e => e.Department)
                      .Select(g => new { Dept = g.Key, Count = g.Count(), Avg = g.Average(e => e.Salary) });
```

## 2. ASP.NET MVC 4.0 and Web API

**Q23. Explain the MVC pattern and the request lifecycle in ASP.NET MVC.** Model = data and business rules, View = UI rendering, Controller = handles input and picks the response. Lifecycle: request hits IIS, then `UrlRoutingModule` matches a route, `MvcHandler` creates the controller via the controller factory, the action invoker runs authentication/authorisation filters, model binding, action filters, the action method, the result filters, then the `ActionResult` executes (view rendered with Razor).

**Q24. What are the types of filters in MVC? Order of execution?** Authentication (MVC 5), Authorization, Action, Result, Exception. Order: Authorization, Action (OnActionExecuting), the action, Action (OnActionExecuted), Result (OnResultExecuting), result, Result (OnResultExecuted). Exception filters run if an unhandled exception occurs. Use cases: `[Authorize]`, custom logging, `[HandleError]`, caching via `[OutputCache]`.

**Q25. How does routing work? Attribute routing vs convention routing?** Convention routes are registered in `RouteConfig` (`{controller}/{action}/{id}`) and are order-sensitive, most specific first. Attribute routing (MVC 5 / Web API 2) puts `[Route("orders/{id:int}")]` on actions, which is clearer for REST-style URLs and supports constraints and route prefixes.

**Q26. ViewBag vs ViewData vs TempData?** `ViewData` is a dictionary, `ViewBag` is a dynamic wrapper over it; both live for the current request only. `TempData` survives one redirect (stored in session), ideal for Post-Redirect-Get messages. Prefer strongly typed view models over all three.

**Q27. What is a ViewModel and why use one?** A class shaped for the view: only the fields needed, plus display/validation attributes. It avoids exposing domain/EF entities (over-posting, security) and avoids lazy-loading issues in views.

**Q28. What are HTML helpers, partial views, and `RenderAction`? Layouts and sections?** Helpers (`Html.TextBoxFor`) generate markup with model binding. Partial views are reusable fragments; `Html.Action` / `RenderAction` call a child action. Layout (`_Layout.cshtml`) is the master page; `@RenderBody()` and `@RenderSection()` define slots.

**Q29. How do you validate input in MVC? Client and server side?** Data annotations (`[Required]`, `[StringLength]`, `[RegularExpression]`, `[Range]`) on the model, `ModelState.IsValid` on the server, and unobtrusive jQuery validation on the client. Never rely on client validation alone. Custom validation: `IValidatableObject` or a custom `ValidationAttribute`. Remote validation via `[Remote]`.

**Q30. Prevent CSRF and XSS in MVC.** CSRF: `@Html.AntiForgeryToken()` in the form plus `[ValidateAntiForgeryToken]` on the POST action. XSS: Razor HTML-encodes by default; avoid `@Html.Raw` on user input; validate and encode output; set a Content-Security-Policy. SQL injection: parameterised queries/ORM, never string concatenation. Over-posting: use `[Bind(Include=...)]` or view models.

**Q31. Bundling and minification? Output caching?** `BundleConfig` combines and minifies CSS/JS into fewer requests (enabled when `debug=false`). `[OutputCache(Duration=60, VaryByParam="id")]` caches rendered output; donut caching via child actions.

**Q32. What is the difference between `ActionResult`, `ViewResult`, `JsonResult`, `RedirectToAction` and `PartialViewResult`?** `ActionResult` is the abstract base. `ViewResult` renders a view, `PartialViewResult` a fragment, `JsonResult` serialises JSON (AJAX), `RedirectToAction` issues HTTP 302 (PRG pattern), `FileResult` returns a file, `HttpStatusCodeResult` returns a status.

**Q33. What are Areas in MVC? Custom model binders? Custom route constraints?** Areas split a large app into functional modules, each with its own controllers/views/routes. A custom model binder (`IModelBinder`) controls how request data becomes a complex type. A route constraint (`IRouteConstraint`) restricts which URLs match.

**Q34. Dependency injection in MVC 4/5 (no built-in container)?** Implement `IDependencyResolver` and register it in `Application_Start` (`DependencyResolver.SetResolver`) with a container such as Autofac, Unity or Ninject, so controllers get constructor-injected services. Prefer constructor injection over service locator.

**Q35. What is ASP.NET Web API, and how is it different from MVC and WCF?** Web API builds HTTP services (REST) for any client, content-negotiating JSON/XML, with no views. MVC returns HTML views (it can return JSON, but that's not its focus). WCF is SOAP/protocol-agnostic and heavier; Web API is lightweight and HTTP-centric. In Web API controllers derive from `ApiController` and action selection is by HTTP verb.

**Q36. Which HTTP verbs and status codes do you use in REST design?** GET (read, safe, idempotent), POST (create, not idempotent), PUT (replace, idempotent), PATCH (partial update), DELETE (idempotent). Codes: 200 OK, 201 Created (with Location header), 204 No Content, 400 Bad Request, 401 Unauthorized (not authenticated), 403 Forbidden (not authorised), 404 Not Found, 409 Conflict, 422, 429 Too Many Requests, 500, 503.

**Q37. How do you secure a Web API?** HTTPS everywhere; token-based auth (JWT / OAuth 2.0 bearer, OpenID Connect) validated by a message handler or OWIN middleware; `[Authorize]` with roles/claims; CORS configured narrowly; input validation; rate limiting; avoid leaking exception detail; secrets outside source control. Basic auth only over TLS and rarely.

**Q38. What is CORS and how do you enable it in Web API?** Browsers block cross-origin requests unless the server allows them via headers. In Web API 2: install `Microsoft.AspNet.WebApi.Cors`, call `config.EnableCors()`, and use `[EnableCors(origins, headers, methods)]` or a global policy. Allow only known origins; never `*` with credentials. Preflight OPTIONS requests are handled by the framework.

**Q39. Explain Web API message handlers, filters and content negotiation.** Message handlers (`DelegatingHandler`) sit in the pipeline earlier than routing, for cross-cutting concerns (logging, API keys, correlation IDs). Filters (authorization, action, exception) apply per controller/action. Content negotiation selects the formatter from the `Accept` header (JSON via Json.NET, XML).

**Q40. How do you handle errors globally in Web API?** Implement `ExceptionFilterAttribute` or, better, `IExceptionHandler` / `IExceptionLogger` (Web API 2) to log centrally and return a consistent error body (`HttpResponseMessage` or `IHttpActionResult`) with the correct status code and a correlation id. Don't return stack traces.

**Q41. `IHttpActionResult` vs `HttpResponseMessage`?** `IHttpActionResult` (Web API 2) is a testable factory for responses (`Ok()`, `NotFound()`, `BadRequest()`); `HttpResponseMessage` gives full manual control. Prefer `IHttpActionResult` for cleaner, unit-testable actions.

**Q42. How do you version a Web API?** URI (`/api/v2/orders`), query string, header or media type versioning. URI versioning is the most visible and cache-friendly; header/media type is purer REST. Keep old versions until clients migrate and publish a deprecation policy.

**Q43. How do you implement paging, filtering and sorting?** Accept `page`, `pageSize`, `sort` and filter parameters; apply `Skip`/`Take` on an `IQueryable` so the database does the work; return total count and links (or a cursor for large sets). Cap `pageSize`. OData is an option for rich queries.

**Q44. How do you make a Web API performant?** Async I/O end to end, response caching (ETag / Cache-Control), compression, paging, avoid N+1 queries, projection to DTOs (`Select`) instead of loading entities, connection pooling, a distributed cache (Redis / ElastiCache) for hot data, and load testing with profiling.

**Q45. How do you consume a REST API from C# properly?** Reuse a single `HttpClient` (or `IHttpClientFactory` in modern .NET) to avoid socket exhaustion; set timeouts; use `async`; handle non-success codes; use Polly for retries with exponential backoff and circuit breaker; deserialise with Json.NET / System.Text.Json.

**Q46. What is idempotency and why does it matter for APIs?** Repeating the same request has the same effect as sending it once. PUT and DELETE are naturally idempotent; for POST (payments, orders) use an idempotency key so client retries do not create duplicates.

**Q47. What is the difference between authentication and authorization? Explain JWT structure.** Authentication verifies who you are; authorization decides what you may do. A JWT has three Base64URL parts: header (algorithm), payload (claims such as `sub`, `exp`, roles), and signature. It is signed, not encrypted, so never put secrets in it; keep access tokens short-lived and use refresh tokens.

**Q48. Session state options in ASP.NET, and why stateless APIs scale better?** InProc, StateServer, SQL Server, or a distributed cache. InProc breaks when you scale out behind a load balancer unless sticky sessions are used. Stateless APIs (token carries identity) can be served by any instance, which suits autoscaling on AWS.

**Q49. IIS and ASP.NET application pool basics; web.config transforms?** App pools isolate apps in worker processes (w3wp) with recycling and identity settings. `web.config` holds config; transforms (`Web.Release.config`) change settings per environment at publish time. Encrypt sensitive sections and keep secrets in a secrets store.

## 3. SQL Server

**Q50. Clustered vs non-clustered index?** A clustered index defines the physical order of the table's rows (one per table, usually the primary key); the leaf level is the data. A non-clustered index is a separate structure with key columns plus a row locator (the clustering key); a table can have many. Covering indexes use `INCLUDE` columns so a query is answered from the index alone, avoiding key lookups.

**Q51. How do you find and fix a slow query?** Look at the actual execution plan: scans vs seeks, key lookups, missing-index hints, estimated vs actual row mismatches (stale statistics or parameter sniffing), expensive sorts/spills. Check `SET STATISTICS IO, TIME ON`, Query Store, and DMVs (`sys.dm_exec_query_stats`). Fixes: proper indexes, sargable predicates, rewrite, update statistics, reduce selected columns, avoid functions on indexed columns.

**Q52. What is a sargable predicate? Give an example.** A predicate the optimiser can satisfy with an index seek. `WHERE YEAR(OrderDate) = 2026` is not sargable (function on the column); `WHERE OrderDate >= '2026-01-01' AND OrderDate < '2027-01-01'` is.

**Q53. Types of joins?** INNER (matching rows), LEFT/RIGHT OUTER (all rows from one side), FULL OUTER, CROSS (cartesian), plus SELF join. Physical join operators the optimiser picks: nested loops, hash, merge.

**Q54. `DELETE` vs `TRUNCATE` vs `DROP`?** `DELETE` is DML, row by row, can have a WHERE, fires triggers, fully logged. `TRUNCATE` removes all rows quickly, minimally logged, resets identity, no WHERE, cannot run if foreign keys reference the table. `DROP` removes the object itself.

**Q55. `WHERE` vs `HAVING`? `UNION` vs `UNION ALL`?** `WHERE` filters rows before grouping; `HAVING` filters groups after aggregation. `UNION` removes duplicates (a sort/distinct cost); `UNION ALL` keeps them and is faster.

**Q56. Stored procedure vs function vs view?** Stored procedures can modify data, use transactions, return multiple result sets and output parameters, and cache plans. Functions must be deterministic-friendly: scalar UDFs can hurt performance (per-row execution; inline table-valued functions are better); functions cannot change database state. Views are saved queries; indexed views materialise results.

**Q57. Temp table vs table variable vs CTE?** Temp tables (`#t`) live in tempdb, have statistics, and support indexes: better for larger sets. Table variables have no statistics (poor estimates on big data). A CTE is just a named subquery for the statement: it is not materialised, and recursive CTEs handle hierarchies.

**Q58. Explain ACID and transaction isolation levels.** Atomicity, Consistency, Isolation, Durability. Levels: Read Uncommitted (dirty reads), Read Committed (default; prevents dirty reads), Repeatable Read, Snapshot (row versioning, readers don't block writers), Serializable (strictest). `READ_COMMITTED_SNAPSHOT` reduces blocking in busy OLTP systems.

**Q59. What are deadlocks and how do you prevent them?** Two transactions each hold a lock the other needs; SQL Server kills one as the victim (error 1205). Prevent by accessing objects in the same order, keeping transactions short, proper indexing (fewer locks), and using snapshot isolation. Detect via deadlock graphs / Extended Events; retry in the application.

**Q60. What is parameter sniffing?** The plan is compiled for the first parameter values and reused; if data distribution is skewed, it is slow for other values. Fixes: `OPTION (RECOMPILE)`, `OPTIMIZE FOR`, local variables, plan guides, or Query Store plan forcing.

**Q61. Window functions: what are they? Example for ranking and the Nth highest salary.**

```sql
-- Nth highest salary per department
SELECT * FROM (
  SELECT Dept, Name, Salary,
         DENSE_RANK() OVER (PARTITION BY Dept ORDER BY Salary DESC) AS rnk
  FROM Employees) x
WHERE rnk = 2;
```

`ROW_NUMBER` gives unique numbers, `RANK` leaves gaps after ties, `DENSE_RANK` doesn't. `LAG`/`LEAD`, running totals (`SUM() OVER (ORDER BY ...)`) are common too.

**Q62. Find and delete duplicate rows.**

```sql
WITH d AS (
  SELECT *, ROW_NUMBER() OVER (PARTITION BY Email ORDER BY Id) rn FROM Users)
DELETE FROM d WHERE rn > 1;
```

**Q63. How do you prevent SQL injection?** Parameterised queries or stored procedures (never concatenate input), least-privilege DB accounts, input validation, and ORMs that parameterise. In dynamic SQL use `sp_executesql` with parameters.

**Q64. Normalisation vs denormalisation?** Normalisation (1NF to 3NF/BCNF) removes redundancy and update anomalies. Denormalise deliberately for read-heavy reporting where joins are costly, accepting redundancy and the need to keep copies in sync.

**Q65. Primary key vs unique key vs foreign key; `IDENTITY` vs `GUID` keys?** Primary key: unique, non-null, one per table. Unique: allows one NULL (in SQL Server), many per table. Foreign key enforces referential integrity. `IDENTITY` is compact and sequential (good clustered key). Random GUIDs fragment a clustered index; use `NEWSEQUENTIALID()` or a non-clustered primary key if GUIDs are needed (e.g. across distributed systems).

**Q66. Triggers, cursors, and why to avoid cursors?** Triggers run automatically on DML (AFTER / INSTEAD OF) and can hide logic and hurt performance; use sparingly (auditing). Cursors process row by row; prefer set-based operations, which are usually orders of magnitude faster.

**Q67. Entity Framework: code first vs database first; lazy vs eager loading; N+1 problem.** Code first models classes then generates the schema via migrations; database first scaffolds from an existing schema. Lazy loading fetches related data on access (causing N+1 queries in loops); eager loading uses `Include()` to join up front; explicit loading is manual. Use `AsNoTracking()` for read-only queries, projections with `Select`, and check generated SQL. Dapper is a lighter micro-ORM for hot paths.

**Q68. How would you handle high-volume data and backups at a high level?** Partitioning, archiving old data, proper indexing and maintenance (rebuild/reorganise by fragmentation), columnstore for analytics. Backups: full + differential + transaction log with tested restores; recovery model (Full for point-in-time). On AWS: RDS for SQL Server automated backups, Multi-AZ, and read replicas (Enterprise edition) for read scaling.

**Q69. Optimistic vs pessimistic concurrency?** Optimistic: no locks held; detect conflicts at save time using a `rowversion` column (EF `[Timestamp]` / `ConcurrencyCheck`) and handle `DbUpdateConcurrencyException`. Pessimistic: lock rows up front (`UPDLOCK`). Optimistic suits web apps with low conflict rates.

## 4. Angular 19 and REST API Integration

**Q70. What are the key features of recent Angular versions (up to 19)?** Standalone components by default (no NgModule needed), signals for fine-grained reactivity (`signal`, `computed`, `effect`, signal-based `input()`, `output()`, `model()`), the new built-in control flow (`@if`, `@for`, `@switch`) replacing `*ngIf`/`*ngFor`, deferrable views (`@defer`), the `inject()` function, functional guards/interceptors/resolvers, improved SSR with hydration (incremental hydration in 19), the new esbuild/Vite-based build, and zoneless change detection in progress (experimental). Check the Angular release notes before the interview for exact 19 details.

**Q71. Explain Angular architecture: components, modules, services, directives, pipes.** Components (template + class + styles) form the UI tree. Services hold logic and shared state, provided through dependency injection. Directives change DOM behaviour (structural / attribute). Pipes transform display values. Modules (NgModule) group declarations in older code; standalone components import dependencies directly.

**Q72. Component lifecycle hooks, in order?** `ngOnChanges`, `ngOnInit`, `ngDoCheck`, `ngAfterContentInit`, `ngAfterContentChecked`, `ngAfterViewInit`, `ngAfterViewChecked`, `ngOnDestroy`. Fetch initial data in `ngOnInit`; clean up subscriptions and timers in `ngOnDestroy`. Newer code also uses `afterNextRender`.

**Q73. Change detection: Default vs OnPush? What are signals?** Default checks every component on each event (Zone.js triggers it). `OnPush` checks a component only when its inputs change by reference, an event fires in it, an observable bound with `async` emits, or it is marked for check; it is much faster for large trees. Signals track dependencies precisely so only affected views update, and work well with OnPush and zoneless apps.

**Q74. Observables vs Promises; RxJS operators you use daily?** Promises are eager, single-value, not cancellable. Observables are lazy, can emit many values, are cancellable (unsubscribe), and composable. Operators: `map`, `filter`, `switchMap` (cancel previous: search typeahead), `mergeMap` (parallel), `concatMap` (sequential), `exhaustMap` (ignore while busy: submit button), `debounceTime`, `distinctUntilChanged`, `combineLatest`, `forkJoin`, `catchError`, `retry`, `takeUntil` / `takeUntilDestroyed`, `shareReplay`.

**Q75. How do you avoid memory leaks with subscriptions?** Prefer the `async` pipe (auto-unsubscribes), `takeUntilDestroyed()`, `toSignal()`, or `takeUntil(destroy$)`; unsubscribe in `ngOnDestroy`. HTTP calls complete on their own, but long-lived streams (router events, intervals, websockets) must be cleaned up.

**Q76. How do you call REST APIs in Angular? Show an `HttpClient` service with error handling.**

```typescript
@Injectable({ providedIn: 'root' })
export class OrderService {
  private http = inject(HttpClient);
  private base = '/api/orders';

  getAll(page = 1) {
    return this.http.get<Order[]>(this.base, { params: { page } }).pipe(
      retry({ count: 2, delay: 500 }),
      catchError(err => throwError(() => new Error('Could not load orders')))
    );
  }
  create(o: OrderDto) { return this.http.post<Order>(this.base, o); }
}
```

Provide `provideHttpClient(withInterceptors([authInterceptor]))` in the app config.

**Q77. What are HTTP interceptors and typical uses?** They intercept every request/response. Uses: attach the bearer token, global error handling (redirect to login on 401), loading spinners, logging, retry, correlation IDs. In recent Angular they are functions (`HttpInterceptorFn`) registered with `withInterceptors`.

**Q78. Template-driven vs reactive forms? Custom validators?** Template-driven: logic in the template, quick for simple forms. Reactive: `FormGroup`/`FormControl` defined in code, testable, scalable, dynamic (`FormArray`), supports custom and async validators (a function returning `ValidationErrors | null`). Use `valueChanges` with `debounceTime` for live validation; typed forms are available since Angular 14.

**Q79. Routing: guards, lazy loading, resolvers, route parameters.** Lazy load with `loadComponent` / `loadChildren` to shrink the initial bundle. Functional guards (`CanActivateFn`, `CanDeactivateFn`, `CanMatchFn`) protect routes (auth, unsaved changes). Resolvers pre-fetch data. Read params with `ActivatedRoute` or `withComponentInputBinding()` binding them directly to inputs.

**Q80. State management options?** Small apps: services with signals or `BehaviorSubject`. Larger apps: NgRx (Store, Effects, Selectors; signal store variant `@ngrx/signals`), Akita, or NGXS. Choose NgRx when many components share state with complex async flows and you need traceability; otherwise it is overhead.

**Q81. How do you optimise Angular performance?** OnPush + signals, `trackBy` / `track` in `@for`, lazy routes and `@defer`, pure pipes instead of template methods, virtual scrolling (CDK), image optimisation (`NgOptimizedImage`), AOT and tree shaking, bundle analysis, SSR/hydration, avoiding heavy work in templates, and debouncing high-frequency events.

**Q82. Component communication patterns?** Parent to child: `input()`; child to parent: `output()` / `EventEmitter`; siblings or unrelated: a shared service (signal/Subject), router state, or a store; `@ViewChild` for direct access (sparingly); `model()` for two-way binding.

**Q83. What is dependency injection in Angular? `providedIn: 'root'` vs component providers?** A hierarchical injector supplies instances. `providedIn: 'root'` makes a tree-shakable singleton. Component-level providers create a new instance per component instance (useful for scoped state). Use `InjectionToken` for non-class values and `inject()` in constructors or field initialisers.

**Q84. How do you secure an Angular app?** Angular sanitises template bindings against XSS by default; never bypass with `bypassSecurityTrust*` unless necessary. Store tokens carefully (HttpOnly cookies are safer than localStorage against XSS), use route guards for UX only (the server must enforce authorisation), CSRF protection with `HttpClientXsrfModule` for cookie-based auth, and a Content-Security-Policy.

**Q85. How do you test Angular code?** Unit tests with Jasmine/Karma (or Jest) and `TestBed`; mock HTTP with `HttpTestingController`; component tests with `ComponentFixture`; e2e with Cypress or Playwright. Test behaviour, not implementation; use harnesses for Angular Material.

**Q86. Explain ViewEncapsulation, content projection (`ng-content`), and the `async` pipe.** Encapsulation scopes component CSS (Emulated by default, `ShadowDom`, or `None`). `ng-content` projects parent-supplied markup into a component. The `async` pipe subscribes, renders the latest value, and unsubscribes automatically.

**Q87. REST design principles you follow?** Resources as nouns (`/orders/42/items`), correct verbs and status codes, statelessness, consistent naming and error format (RFC 7807 problem details), pagination, filtering, versioning, idempotency for retries, HATEOAS where it adds value, OpenAPI/Swagger documentation, and caching headers (`ETag`, `Cache-Control`).

**Q88. REST vs GraphQL vs gRPC?** REST is simple, cacheable, ubiquitous. GraphQL lets clients request exactly the fields needed (fewer round trips) but adds complexity (caching, N+1 on the server). gRPC is a fast binary protocol using HTTP/2 and protobuf, ideal for internal service-to-service calls.

**Q89. How would you handle file upload/download and long-running operations through an API?** Upload with `multipart/form-data` or, for large files, a pre-signed S3 URL so the file goes straight to S3. For long-running work return `202 Accepted` with a status URL (or use webhooks/SignalR), process in a background queue (SQS + worker), and let the client poll or be notified.

## 5. Design Patterns, SOLID and Best Practices

**Q90. Explain the SOLID principles with a C# example for each.**

| Principle | Meaning | Quick example |
| --- | --- | --- |
| Single Responsibility | A class has one reason to change | Split `OrderService` into `OrderValidator`, `OrderRepository`, `EmailNotifier` |
| Open/Closed | Open for extension, closed for modification | Add a new `IDiscountStrategy` implementation instead of editing an if/else chain |
| Liskov Substitution | Subtypes must be usable wherever the base type is, without breaking behaviour | A `Square : Rectangle` that breaks `SetWidth` semantics violates it |
| Interface Segregation | Many small interfaces beat one fat one | Separate `IReadRepository` and `IWriteRepository` |
| Dependency Inversion | Depend on abstractions, not concretions | Controller depends on `IOrderService`, injected via the container |

**Q91. Dependency Injection vs Inversion of Control vs Dependency Inversion?** IoC is the broad idea that a framework/container calls your code and controls object creation. Dependency Inversion is the SOLID principle (depend on abstractions). DI is the technique that implements it: dependencies are supplied (constructor, property or method injection) instead of created inside the class. Lifetimes: transient, scoped (per request), singleton. Beware of captive dependencies (a singleton holding a scoped service).

**Q92. Which design patterns have you used in real projects?** Pick three or four you can defend. Common choices:

| Pattern | Category | Where it shows up |
| --- | --- | --- |
| Repository + Unit of Work | Architectural / data access | Abstract EF behind `IRepository<T>` for testability (note: EF's `DbContext` is already a UoW, so avoid pointless wrappers) |
| Factory / Abstract Factory | Creational | Create payment gateway implementations by type |
| Singleton | Creational | Configuration, logging (prefer DI-managed singleton lifetime over static) |
| Builder | Creational | Fluent construction of complex objects / test data |
| Strategy | Behavioural | Pricing, validation or tax rules chosen at runtime |
| Observer | Behavioural | .NET events, RxJS, domain events |
| Decorator | Structural | Add caching or logging around a service without changing it |
| Adapter | Structural | Wrap a third-party API behind your own interface |
| Facade | Structural | One simple entry point over several subsystems |
| CQRS / Mediator | Architectural | MediatR separates commands from queries |

**Q93. Implement a thread-safe Singleton in C#.**

```csharp
public sealed class Config
{
    private static readonly Lazy<Config> _instance = new(() => new Config());
    public static Config Instance => _instance.Value;
    private Config() { }
}
```

`Lazy<T>` is thread-safe by default. Mention the downsides: hidden global state and hard to test, so prefer a DI singleton.

**Q94. Show the Strategy pattern replacing an if/else chain.**

```csharp
public interface IDiscount { decimal Apply(decimal amount); }
public class Festive : IDiscount { public decimal Apply(decimal a) => a * 0.9m; }
public class Loyalty : IDiscount { public decimal Apply(decimal a) => a * 0.95m; }

public class Checkout
{
    private readonly IDiscount _d;
    public Checkout(IDiscount d) => _d = d;
    public decimal Total(decimal amount) => _d.Apply(amount);
}
```

New discounts are added without modifying `Checkout` (Open/Closed).

**Q95. Repository pattern: benefits and criticism?** Benefits: decouples business logic from persistence, enables unit testing with fakes, centralises queries. Criticism: with EF, a generic repository over `DbContext` often duplicates what EF already provides and hides `IQueryable` power. A pragmatic approach: specific repositories or query objects for complex aggregates, direct `DbContext` for simple cases.

**Q96. What is Clean Architecture / layered architecture? How do you structure a .NET solution?** Layers: Presentation (MVC / Web API / Angular), Application (use cases, DTOs, interfaces), Domain (entities, business rules; no dependencies), Infrastructure (EF, external services). Dependencies point inward. Benefits: testability, replaceable infrastructure, clear boundaries.

**Q97. What is DDD in a nutshell?** Model the domain with a ubiquitous language shared with the business: entities, value objects, aggregates (consistency boundaries with a root), repositories per aggregate, domain events, bounded contexts. Use it for complex domains, not simple CRUD.

**Q98. Monolith vs microservices; when to choose which?** Start with a well-modularised monolith for small teams. Move to microservices when independent scaling, deployment cadence or team autonomy justifies the cost (distributed transactions, observability, network failures, data consistency). Patterns: API gateway, saga for distributed transactions, outbox for reliable messaging, circuit breaker, service discovery.

**Q99. Coding best practices you follow?** Meaningful naming, small methods, DRY without over-abstracting, KISS and YAGNI, early returns, immutability where practical, consistent error handling and logging, no magic numbers, code reviews, static analysis (SonarQube, Roslyn analyzers), `.editorconfig`, meaningful commit messages, and documentation for non-obvious decisions.

**Q100. How do you handle technical debt and legacy code?** Make it visible (backlog items with impact), add characterisation tests before changing code, refactor in small safe steps (boy-scout rule), apply the strangler pattern for large rewrites, and negotiate a steady percentage of each sprint for debt reduction.

## 6. Unit Testing and BDD

**Q101. What makes a good unit test? Explain the AAA pattern.** Fast, isolated, repeatable, self-validating, and focused on one behaviour (the FIRST principles). Arrange (set up inputs and fakes), Act (call the unit), Assert (verify the outcome). Name tests by behaviour: `Total_WithFestiveDiscount_Returns10PercentOff`.

```csharp
[Fact]
public void Total_WithFestiveDiscount_Returns10PercentOff()
{
    // Arrange
    var sut = new Checkout(new Festive());
    // Act
    var result = sut.Total(100m);
    // Assert
    Assert.Equal(90m, result);
}
```

**Q102. Which unit testing frameworks and mocking libraries have you used?** xUnit, NUnit, MSTest for tests; Moq, NSubstitute or FakeItEasy for mocks; FluentAssertions for readable assertions; AutoFixture / Bogus for test data; Jasmine/Karma or Jest for Angular.

**Q103. Mock vs stub vs fake vs spy vs dummy?** Dummy: passed but unused. Stub: returns canned data. Fake: a working lightweight implementation (in-memory repository). Mock: verifies interactions (was `Send` called once?). Spy: records calls for later assertion. Prefer state-based assertions; over-mocking couples tests to implementation.

**Q104. Show a test with Moq for a service that depends on a repository.**

```csharp
[Fact]
public async Task GetOrder_WhenNotFound_ReturnsNull()
{
    var repo = new Mock<IOrderRepository>();
    repo.Setup(r => r.GetByIdAsync(42)).ReturnsAsync((Order)null);
    var sut = new OrderService(repo.Object);

    var result = await sut.GetOrderAsync(42);

    Assert.Null(result);
    repo.Verify(r => r.GetByIdAsync(42), Times.Once);
}
```

**Q105. How do you unit test an MVC controller or Web API controller?** Inject mocked services through the constructor, call the action directly, and assert on the result type: for MVC `Assert.IsType<ViewResult>(result)` and the view model; for Web API 2 `Assert.IsType<OkNegotiatedContentResult<Order>>(result)` or `NotFoundResult`. This is why thin controllers and `IHttpActionResult` help. Test routes/filters separately with integration tests.

**Q106. What is TDD? Red-Green-Refactor?** Write a failing test first (red), write the minimum code to pass (green), then refactor with the tests as a safety net. Benefits: design pressure toward small, decoupled units; fewer regressions. Cost: discipline and slower start.

**Q107. What is BDD? How is it different from TDD?** Behaviour-Driven Development describes behaviour in a shared, plain-language format (Given-When-Then) that business, QA and developers all understand; the scenarios become executable specifications. TDD is developer-centred and unit-focused; BDD is collaborative and focuses on features and acceptance criteria.

**Q108. How do you write BDD tests in .NET? (SpecFlow / Reqnroll)** Write Gherkin feature files, bind each step to C# methods (step definitions), and run them via xUnit/NUnit. SpecFlow is the established tool; its community continuation is Reqnroll. Keep scenarios declarative (business language), not UI click-scripts.

```gherkin
Feature: Order discount
  Scenario: Loyalty customer gets 5% off
    Given a loyalty customer with a cart total of 200
    When the customer checks out
    Then the payable amount should be 190
```

```csharp
[Binding]
public class DiscountSteps
{
    private decimal _total, _payable;
    [Given(@"a loyalty customer with a cart total of (.*)")]
    public void GivenCart(decimal total) => _total = total;
    [When("the customer checks out")]
    public void WhenCheckout() => _payable = new Checkout(new Loyalty()).Total(_total);
    [Then(@"the payable amount should be (.*)")]
    public void ThenPayable(decimal expected) => Assert.Equal(expected, _payable);
}
```

For Angular/Node, Cucumber.js with Cypress or Playwright is the equivalent.

**Q109. The testing pyramid and where do integration tests fit?** Many fast unit tests at the base, fewer integration tests (controller to DB using a test database, `WebApplicationFactory` in modern .NET, or Testcontainers), and a few end-to-end UI tests at the top. Avoid the inverted pyramid (slow, flaky UI-heavy suites).

**Q110. What code coverage do you aim for?** Coverage is a signal, not a goal. Around 70 to 80 percent on business logic is a reasonable target, with critical paths at higher; 100 percent with weak assertions is worse than 70 percent with good ones. Add mutation testing (Stryker.NET) if you need confidence in test quality.

**Q111. How do you test code that uses `DateTime.Now`, the file system or randomness?** Abstract it behind an interface (`IClock`, `IFileSystem`) or use `TimeProvider` in modern .NET, then inject a fake with a fixed value.

**Q112. How do you test async code and exceptions?** Make the test `async Task`, `await` the call, and assert exceptions with `await Assert.ThrowsAsync<ArgumentException>(() => sut.DoAsync(null))`. Never use `.Result` in tests.

**Q113. What is CI/CD and how do tests fit into the pipeline?** Every commit triggers build, unit tests, static analysis and package creation; merges to main deploy to staging, run integration/smoke tests, then promote to production with approval or automatically (continuous delivery vs deployment). Tools: Azure DevOps, Jenkins, GitHub Actions, AWS CodePipeline/CodeBuild.

## 7. GDPR, HIPAA and Security Compliance

The job description lists these as good to have, so expect conceptual questions: what the rules require and how a developer implements them. Answer as an engineer, not a lawyer.

**Q114. What is GDPR and who does it apply to?** The EU General Data Protection Regulation governs how personal data of people in the EU is collected, processed and stored. It applies to any organisation that processes such data, regardless of where the organisation is located. Fines can reach 20 million euros or 4 percent of global annual turnover, whichever is higher.

**Q115. Key GDPR principles and data subject rights?** Principles: lawfulness, fairness and transparency; purpose limitation; data minimisation; accuracy; storage limitation; integrity and confidentiality; accountability. Rights: access, rectification, erasure ("right to be forgotten"), restriction, data portability, objection, and rights around automated decisions. Breaches must be notified to the authority within 72 hours.

**Q116. How would you implement GDPR requirements in an application?**

| Requirement | Engineering approach |
| --- | --- |
| Consent | Store consent records with purpose, timestamp and version; allow withdrawal |
| Right to erasure | Delete or anonymise across DB, caches, backups policy, logs and downstream systems; soft delete alone is not enough |
| Access / portability | Export endpoint returning the user's data in JSON/CSV |
| Data minimisation | Collect only needed fields; set retention periods and purge jobs |
| Privacy by design | Secure defaults, pseudonymisation, access controls from day one |
| Breach readiness | Audit logs, monitoring and alerts (CloudTrail, CloudWatch), incident runbook |
| Data residency | Keep EU data in EU regions (e.g. eu-west-1 / eu-central-1), control cross-border transfers |

**Q117. Personal data vs sensitive data; pseudonymisation vs anonymisation?** Personal data identifies a person directly or indirectly (name, email, IP, device ID). Special categories (health, biometrics, religion, etc.) need stronger protection. Pseudonymisation replaces identifiers with a key held separately (still personal data, reversible). Anonymisation is irreversible and takes data out of GDPR scope.

**Q118. What is HIPAA?** The US Health Insurance Portability and Accountability Act protects Protected Health Information (PHI). It applies to covered entities (providers, health plans, clearinghouses) and their business associates (including software vendors and cloud providers handling PHI). Key rules: Privacy Rule, Security Rule (administrative, physical, technical safeguards), Breach Notification Rule.

**Q119. What technical safeguards does HIPAA expect?** Access control with unique user IDs and least privilege, audit controls (who accessed what, when), integrity controls, authentication, and transmission security. Encrypt PHI at rest (AES-256) and in transit (TLS 1.2+), enable session timeouts, log access, and have backup and disaster-recovery plans. A Business Associate Agreement (BAA) is required with vendors such as AWS, and only HIPAA-eligible services may hold PHI.

**Q120. GDPR vs HIPAA: key differences?** GDPR protects all personal data of EU residents and gives individuals broad rights (erasure, portability, consent). HIPAA protects health information in the US healthcare context and focuses on safeguards and permitted uses and disclosures. Both require security, breach notification and accountability; a system handling EU health data may need both.

**Q121. How do you handle secrets, encryption and logging securely?** Keep secrets in a vault (AWS Secrets Manager / SSM Parameter Store, not in `web.config` or source), encrypt with KMS-managed keys, rotate credentials, hash passwords with a slow salted algorithm (bcrypt, PBKDF2, Argon2; never reversible encryption), and never log PII, tokens or passwords (mask them).

**Q122. What is the OWASP Top 10 and which items matter most for a web app?** It lists the most critical web risks: broken access control, cryptographic failures, injection, insecure design, security misconfiguration, vulnerable and outdated components, authentication failures, integrity failures, logging and monitoring failures, and SSRF. Mitigations: parameterised queries, output encoding, server-side authorisation checks on every request, dependency scanning (Dependabot, `npm audit`, NuGet audit), security headers, and regular pen testing.

**Q123. Role-based vs claims-based vs attribute-based access control?** RBAC grants permissions by role (Admin, Clerk). Claims-based uses assertions about the user (department, clearance) evaluated by policy. ABAC decides using attributes of the user, resource and context. For healthcare data, combine roles with resource-level checks (a clinician sees only their own patients) and break-glass auditing.

## 8. AWS for .NET Full Stack Developers

**Q124. Which AWS services have you used in a .NET application, and for what?** A strong answer maps services to roles:

| Need | AWS service |
| --- | --- |
| Host web apps / APIs | EC2 (Windows/Linux), Elastic Beanstalk (.NET on IIS), ECS/Fargate or EKS (containers), App Runner |
| Serverless compute | Lambda (.NET 8 runtime), API Gateway |
| Database | RDS for SQL Server, Aurora, DynamoDB |
| Storage | S3 (files, static Angular hosting), EFS / FSx for Windows |
| Caching | ElastiCache (Redis) |
| Messaging / async | SQS, SNS, EventBridge, Kinesis |
| Identity | IAM, Cognito |
| Secrets / keys | Secrets Manager, Parameter Store, KMS |
| Delivery | CloudFront (CDN), Route 53 (DNS), ALB |
| CI/CD | CodeCommit/CodeBuild/CodePipeline/CodeDeploy, or GitHub Actions / Azure DevOps |
| Observability | CloudWatch (logs, metrics, alarms), X-Ray, CloudTrail (audit) |

**Q125. EC2 vs Elastic Beanstalk vs Lambda vs ECS/Fargate: how do you choose?** EC2: full control, lift-and-shift of legacy .NET Framework apps (Windows required). Elastic Beanstalk: managed deployment for web apps with less ops work. Lambda: event-driven, short tasks, pay per invocation (15-minute limit, cold starts). ECS/Fargate: containers without managing servers, good for APIs on modern .NET. .NET Framework 4.x apps run only on Windows hosts; modern .NET runs on Linux containers, which are cheaper.

**Q126. How would you deploy an ASP.NET Web API plus Angular app on AWS?** Angular: build, upload to S3, serve through CloudFront (HTTPS, caching, SPA fallback to `index.html`). API: ALB in front of an Auto Scaling group of EC2 (or ECS Fargate tasks) in private subnets across two AZs; RDS SQL Server Multi-AZ in a private subnet; secrets in Secrets Manager; logs to CloudWatch; WAF on CloudFront/ALB; Route 53 for DNS; pipeline via CodePipeline or GitHub Actions with blue/green or rolling deployment.

**Q127. Explain IAM: users, roles, policies, least privilege.** IAM controls who can do what on which resource. Prefer roles over long-lived access keys: an EC2 instance, Lambda or ECS task assumes a role and gets temporary credentials automatically, so no keys appear in code or config. Policies are JSON documents with Effect/Action/Resource/Condition; start with the minimum permissions and add as needed; use MFA, SCPs and permission boundaries in larger orgs.

**Q128. How does a .NET app access S3? Show an upload.**

```csharp
using Amazon.S3;
using Amazon.S3.Transfer;

var client = new AmazonS3Client(Amazon.RegionEndpoint.APSouth1); // credentials come from the role
var transfer = new TransferUtility(client);
await transfer.UploadAsync(stream, "my-bucket", $"docs/{id}.pdf");

// Pre-signed URL so the browser uploads/downloads directly
var url = client.GetPreSignedURL(new GetPreSignedUrlRequest {
    BucketName = "my-bucket", Key = $"docs/{id}.pdf",
    Verb = HttpVerb.GET, Expires = DateTime.UtcNow.AddMinutes(10) });
```

Block public access by default, enable encryption (SSE-S3 or SSE-KMS), versioning, and lifecycle rules.

**Q129. Write a simple C# Lambda function and explain its triggers.**

```csharp
public class Function
{
    public async Task<string> Handler(SQSEvent evt, ILambdaContext ctx)
    {
        foreach (var m in evt.Records)
            ctx.Logger.LogInformation($"Processing {m.Body}");
        return "done";
    }
}
```

Triggers: API Gateway (HTTP), S3 events, SQS, SNS, EventBridge schedules, DynamoDB streams. Tips: keep packages small, reuse clients outside the handler (warm starts), set timeouts and memory, use idempotent handlers (at-least-once delivery), a dead-letter queue, and provisioned concurrency or SnapStart-style options to reduce cold starts.

**Q130. SQS vs SNS vs EventBridge vs Kinesis?** SQS: a queue; consumers pull; decouples producers and consumers; standard (at-least-once, unordered) or FIFO (ordered, exactly-once processing). SNS: pub/sub fan-out to many subscribers. EventBridge: event bus with routing rules and SaaS/AWS event sources. Kinesis: ordered streaming for high-throughput data. Common pattern: SNS fan-out to multiple SQS queues.

**Q131. RDS for SQL Server: Multi-AZ vs read replicas; backups; migration?** Multi-AZ keeps a synchronous standby in another AZ for automatic failover (availability, not read scaling). Read replicas serve read traffic (SQL Server supports them on Enterprise edition). Automated backups with point-in-time restore plus manual snapshots. Migrate using native backup/restore via S3, or AWS DMS with the Schema Conversion Tool. Remember RDS limits: no OS access and some features differ from on-premises.

**Q132. How do you make an AWS-hosted app highly available and scalable?** Multi-AZ deployment, ALB health checks, Auto Scaling policies on CPU or request count, stateless app tier (sessions in ElastiCache or tokens), managed database with failover, CloudFront caching, queues to absorb spikes, and infrastructure tested with load and failure drills. Define RTO/RPO and choose backup/pilot-light/warm-standby/multi-region accordingly.

**Q133. How do you secure workloads on AWS?** Private subnets for app and DB tiers, security groups (stateful, allow rules) as the primary firewall and NACLs (stateless) as an extra layer, WAF and Shield against web attacks, TLS via ACM, encryption at rest with KMS, VPC endpoints for private access to S3/DynamoDB, GuardDuty and Security Hub for detection, CloudTrail for audit, no open SSH/RDP (use SSM Session Manager).

**Q134. What is Infrastructure as Code? CloudFormation vs CDK vs Terraform?** Define infrastructure in version-controlled templates so environments are reproducible and reviewable. CloudFormation: AWS-native YAML/JSON. CDK: write infrastructure in C#/TypeScript, synthesising to CloudFormation (nice for .NET teams). Terraform: multi-cloud, state file based. Use per-environment parameters and drift detection.

**Q135. How do you monitor and troubleshoot a production issue on AWS?** Start with CloudWatch alarms and dashboards, then logs (structured JSON with correlation IDs, queried with Logs Insights), X-Ray traces for latency across services, ALB/target health, RDS Performance Insights and slow query logs, and CloudTrail for who changed what. Then mitigate (roll back, scale, fail over) before the deep root-cause analysis and write a blameless postmortem.

**Q136. How do you optimise AWS costs?** Right-size instances, use Savings Plans/Reserved Instances for steady load and Spot for fault-tolerant jobs, scale down off-hours, S3 lifecycle tiers, delete unattached volumes and old snapshots, use Graviton/Linux where possible, tag resources for cost allocation, and set budgets and alerts in AWS Budgets / Cost Explorer.

**Q137. Containers: how would you containerise an ASP.NET app and what are the trade-offs?** Use a multi-stage Dockerfile (SDK image to build, runtime image to run), push to ECR, deploy on ECS Fargate or EKS. Benefits: consistent environments, fast scaling, easier CI/CD. Note legacy .NET Framework apps need Windows containers (larger, costlier), so porting to modern .NET first usually pays off.

**Q138. What is the shared responsibility model?** AWS secures the cloud (hardware, facilities, managed-service infrastructure); you secure what you put in it (IAM, data, OS patching on EC2, network config, application code). For RDS and Lambda, AWS handles more of the stack than for EC2.

## 9. Scenario, System Design and Behavioural Questions

**Q139. Our legacy ASP.NET MVC 4 app is slow under load. How do you investigate?** Measure first: APM/profiler (Application Insights, New Relic, dotTrace) and IIS/CloudWatch metrics to find whether time goes to CPU, DB, I/O or external calls. Typical wins: fix N+1 and missing indexes, cache hot reads, async I/O, trim view payloads, bundle and compress static assets, tune app pool and connection pooling, then scale out (stateless sessions, load balancer). Re-measure after each change.

**Q140. Design a REST API plus Angular UI for an order management module.** Clarify requirements, then outline: entities (Customer, Order, OrderLine, Payment); endpoints (`GET /orders?page=`, `GET /orders/{id}`, `POST /orders`, `PUT /orders/{id}`, `POST /orders/{id}/cancel`); validation and idempotency key on POST; optimistic concurrency with `rowversion`; JWT auth with role policies; SQL Server schema with indexes on `CustomerId` and `CreatedOn`; Angular lazy-loaded feature module, reactive form, service with interceptor, OnPush list with pagination; unit tests plus BDD for the checkout rule; deployment to AWS (S3/CloudFront, ALB/EC2 or ECS, RDS).

**Q141. Design a system to process uploaded files and notify users when done.** Client requests a pre-signed S3 URL and uploads directly; S3 event triggers an SQS message; a worker (Lambda or ECS service) processes with retries and a dead-letter queue; status stored in SQL Server/DynamoDB; the client polls or receives a notification (SignalR/SNS/email). Add virus scanning, encryption, and audit logging for compliance.

**Q142. How do you ensure zero-downtime deployments and handle database changes?** Blue/green or rolling deployments behind a load balancer with health checks. For the database use backward-compatible migrations (expand, migrate, contract): add new columns first, deploy code that works with both, backfill, then remove old columns later. Keep rollback scripts and feature flags.

**Q143. How do you handle a production incident?** Acknowledge and communicate, assess the impact, mitigate first (roll back, disable the feature flag, scale up), gather logs and traces, find the root cause, fix and verify, then a blameless postmortem with action items. Mention a real incident and what you changed afterwards.

**Q144. You disagree with a teammate or a lead on a technical approach. What do you do?** Understand their reasoning, bring data (benchmarks, prototypes, trade-off table), discuss the options against shared goals, agree on a time-boxed spike if unclear, and then commit to the decision even if it wasn't yours; document it as an ADR.

**Q145. Tell me about a challenging project and your specific contribution.** (use STAR) Situation and task in two sentences, your actions in detail (design choices, code, mentoring, trade-offs), and a measurable result (latency cut from 4s to 800ms, defect rate down 40 percent). Prepare two or three such stories in advance: one on performance, one on a failure/lesson, one on cross-team collaboration.

**Q146. How do you ensure code quality in a team?** Pull requests with checklists, automated build/test/lint/static analysis gates in CI, agreed coding standards, pair programming for complex work, test coverage on changed code, and regular refactoring time.

**Q147. How do you stay current with technology?** Name real sources and what you applied from them: official docs and release notes (Angular, .NET), conference talks, blogs, side projects, and certifications (e.g. AWS Developer Associate or Solutions Architect Associate).

**Q148. How do you estimate work and handle changing requirements?** Break work into small tasks, estimate with ranges and call out risks and unknowns, validate with the team, track velocity, communicate early when scope changes, and negotiate trade-offs (scope, time, quality) instead of silently absorbing them. Experience with Agile/Scrum ceremonies and Jira is expected.

**Q149. Why SOTI, and why this role?** Research before the interview: SOTI builds enterprise mobility and IoT device-management products (for example MobiControl), serving customers in healthcare, logistics and retail, where reliability, security and compliance matter. Tie your experience in secure, scalable .NET/Angular systems and compliance awareness to that mission. Verify the current product details on SOTI's website.

## Quick coding round practice list

- Reverse a string / check palindrome / anagram check
- Find duplicates in an array; first non-repeating character
- FizzBuzz, Fibonacci (iterative and memoised), prime check
- Two-sum with a dictionary; merge two sorted lists; binary search
- LINQ: group by, join, top N per group, flatten with `SelectMany`
- SQL: second highest salary, duplicates, running total, employees earning more than their manager, department-wise top 3
- Implement an LRU cache; a thread-safe producer-consumer with `BlockingCollection`
- Angular: debounce search input with `switchMap`; a reusable form control; a route guard

## Last-day checklist

- [ ] Pick two projects from your resume and prepare architecture, your role, one hard bug, one performance win
- [ ] Revise Sections 1 to 3 (C#, MVC/Web API, SQL) for depth and Section 8 (AWS) for the services you truly used
- [ ] Skim Angular 19 release notes for signals, standalone and new control flow
- [ ] Prepare a 60-second SOLID and design-pattern explanation with a code example
- [ ] Know your own AWS architecture diagram end to end
- [ ] Prepare three questions for them (team structure, release process, how compliance is handled)

Good luck. If you tell me your actual projects and AWS services, I can tailor the answers into your own talking points and run a mock interview.
