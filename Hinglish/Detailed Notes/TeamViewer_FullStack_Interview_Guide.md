# TeamViewer Interview Guide: Full-Stack Engineer (C# & Angular)

This guide compiles comprehensive answers to key technical interview questions frequently asked at TeamViewer for Full-Stack positions. It covers .NET/C# backend design, Angular frontend performance, full-stack networking foundations, and common algorithmic traps.

---

## 1. C# & .NET Backend Architecture

### Q1: Explain inheritance vs encapsulation. Compare their differences, similarities, and how they impact code maintainability.
*   **Inheritance** is an **"Is-A" relationship** where a derived class inherits behavior and structure from a base class (e.g., `Manager : Employee`). It promotes code reuse but creates a strong coupling between classes.
*   **Encapsulation** is the practice of **hiding internal state** and requiring all interaction to occur through a well-defined public interface (using access modifiers like `private`, `protected`, `public`). It isolates changes to an object's internal workings from the rest of the application.
*   **Comparison**: 
    *   *Similarity*: Both are fundamental pillars of Object-Oriented Programming (OOP) designed to modularize code.
    *   *Difference*: Inheritance focuses on extending behavior vertically down a tree, whereas encapsulation focuses on hiding implementation details horizontally.
*   **Maintainability Impact**: Overusing inheritance can lead to rigid, fragile hierarchies (the "fragile base class" problem). Encapsulation is generally preferred for long-term maintainability because it keeps components loosely coupled; changing a private field inside a class doesn't break external consumers.

### Q2: How does the `async/await` state machine handle threading behind the scenes in .NET? What is the performance impact of executing a CPU-bound task synchronously vs asynchronously using `Task.Run()`?
*   **State Machine Mechanics**: When the compiler encounters `async/await`, it transforms the method into a managed state machine (a hidden `struct`). When an `await` point is reached on an incomplete operation, the state machine captures the current execution context, hooks up a continuation, and returns control to the caller. The current thread is freed to do other work. Once the asynchronous operation finishes, the runtime schedules the continuation to run on a thread from the `ThreadPool` (or back to the original `SynchronizationContext`, if applicable).
*   **CPU-bound Impact (`Task.Run`)**:
    *   *Synchronous Execution*: Blocks the executing thread (e.g., an ASP.NET request thread) until the heavy computation finishes. Under high load, this causes **ThreadPool Starvation**, degrading throughput.
    *   *Asynchronous Execution via `Task.Run`*: Offloads the work to a *different* ThreadPool thread. In a web server environment, doing this simply yields the original thread while consuming another one immediately, which provides **no net architectural benefit** and adds overhead due to context switching. `Task.Run` for CPU-bound tasks is best utilized in client applications (Desktop/Mobile) to keep the UI thread responsive, but should be avoided on high-scale web APIs unless true parallel computing is intended.

### Q3: Explain the exact roles of constructors and finalizers/deconstructors in C#. How do you manage unmanaged resources securely using the `IDisposable` pattern?
*   **Constructors**: Methods called when an object is instantiated to initialize its state, allocate memory, and inject dependencies.
*   **Finalizers (`~ClassName`)**: Methods invoked implicitly by the Garbage Collector (GC) before an object's memory is reclaimed. They act as a safety net to clean up unmanaged resources if the developer forgot to do so explicitly. They add overhead because objects with finalizers survive the first GC collection pass (moved to the freachabile queue).
*   **Secure Resource Management (`IDisposable`)**: Unmanaged resources (like raw file handles, database connections, or socket streams) must be disposed of explicitly using the **Disposable Pattern**.

```csharp
public class ResourceManager : IDisposable
{
    private IntPtr _unmanagedResource; // Example raw pointer
    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Tells GC not to call the finalizer
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            // Clean up managed resources here (e.g., fields implementing IDisposable)
        }

        // Clean up unmanaged resources here
        if (_unmanagedResource != IntPtr.Zero)
        {
            // Free unmanaged handle
        }

        _disposed = true;
    }

    ~ResourceManager()
    {
        Dispose(false); // Safety net
    }
}
```

### Q4: In a system where multiple remote agents or users update the same record simultaneously, how do you handle optimistic vs pessimistic concurrency in Entity Framework Core?
*   **Optimistic Concurrency**: Assumes conflicts are rare. It allows multiple users to read and attempt to write records without locking the database. It is implemented in EF Core using a **Concurrency Token** (e.g., a `[Timestamp]` or `RowVersion` column). When saving changes, EF Core appends a `WHERE RowVersion = @OriginalVersion` clause. If the row was modified by someone else, zero rows are updated, and EF Core throws a `DbUpdateConcurrencyException`. You catch this exception to merge changes, notify the user, or retry.
*   **Pessimistic Concurrency**: Assumes conflicts are highly likely. It explicitly locks the database rows upon reading, blocking other operations until the transaction finishes. In EF Core, this is typically handled by issuing raw SQL or using specific extension methods to apply explicit locks (e.g., `FromSqlRaw("SELECT * FROM Table WITH (XLOCK, ROWLOCK) WHERE Id = {0}", id)`). This reduces application throughput but guarantees data isolation.

### Q5: Given a `Dictionary<string, int>`, write a clean method to sort it by values in ascending/descending order.
```csharp
using System;
using System.Collections.Generic;
using System.Linq;

public class SortUtility
{
    public static Dictionary<string, int> SortDictionary(Dictionary<string, int> source, bool ascending = true)
    {
        if (source == null) return new Dictionary<string, int>();

        return ascending 
            ? source.OrderBy(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            : source.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
```

---

## 2. Angular & TypeScript Frontend

### Q1: Explain the execution order of `ngOnInit`, `ngOnChanges`, and `ngAfterViewInit`. In what scenario will `ngOnChanges` not fire when an `@Input` property updates?
*   **Execution Order**:
    1.  `ngOnChanges`: Fires before `ngOnInit` and whenever one or more data-bound `@Input` properties change.
    2.  `ngOnInit`: Called once after the first `ngOnChanges` to initialize component data.
    3.  `ngAfterViewInit`: Invoked once after Angular fully initializes a component's views and child views.
*   **The Non-Firing Scenario**: `ngOnChanges` only triggers if the **reference** of the `@Input` property changes. If the `@Input` is an object or array, mutating an internal property inside that object (e.g., `this.user.name = 'New Name'`) **will not** trigger `ngOnChanges` because the object's reference address remains identical.

### Q2: How do you effectively manage memory leaks when handling long-lived RxJS Observables in Angular services? Explain the difference between `switchMap`, `mergeMap`, and `exhaustMap`.
*   **Memory Leak Prevention**: Long-lived subscriptions (like routing events or custom global services) must be unsubscribed when components destroy. The cleanest way is using the `takeUntil` pattern combined with a destroying subject:
```typescript
private destroy$ = new Subject<void>();

ngOnInit() {
  this.dataService.getStream()
    .pipe(takeUntil(this.destroy$))
    .subscribe(data => this.process(data));
}

ngOnDestroy() {
  this.destroy$.next();
  this.destroy$.complete();
}
```
*   **Flattening Operators**:
    *   `switchMap`: Cancels the current internal subscription if a new value arrives on the outer observable. (Ideal for search autocomplete).
    *   `mergeMap`: Concurrently executes all inner subscriptions simultaneously without canceling anything. (Ideal for independent save actions).
    *   `exhaustMap`: Ignores all incoming outer values until the active inner subscription completes. (Ideal for preventing double-submits on button clicks).

### Q3: How does `ChangeDetectionStrategy.OnPush` improve rendering performance? How do you force a template refresh when operating outside the Angular Zone?
*   **OnPush Optimization**: By default (`Default`), Angular scans the entire component tree from top to bottom during every change detection cycle. `OnPush` disables this constant scanning for that component. Angular will only check the component if:
    1.  An `@Input` reference changes.
    2.  An event originates from inside the component or its children.
    3.  An observable bound via the `async` pipe emits a new value.
*   **Outside the Zone Refreshing**: If you run code asynchronously outside the zone (via `NgZone.runOutsideAngular()`), Angular's automatic rendering framework won't notice it. To force a refresh, inject `ChangeDetectorRef` and call `cdr.detectChanges()` to manually trigger verification on that component instance immediately.

### Q4: How does Angular automatically handle cross-site scripting (XSS), and how would you bypass it safely using `DomSanitizer`?
*   **Automatic Protection**: Angular treats all values as untrusted by default. When values are inserted into the DOM via interpolation or property bindings, Angular automatically sanitizes them, stripping out unsafe scripts, stylesheets, or executable HTML tags.
*   **Safe Bypassing**: If you explicitly trust a secure external string (e.g., rendering trusted system diagnostic frames), inject the `DomSanitizer` utility and call methods like `bypassSecurityTrustHtml` or `bypassSecurityTrustResourceUrl`.
```typescript
constructor(private sanitizer: DomSanitizer) {}

getSafeHtml(rawHtml: string) {
  // Ensure rawHtml is deeply validated or sanitized beforehand to prevent XSS vulnerability
  return this.sanitizer.bypassSecurityTrustHtml(rawHtml);
}
```

---

## 3. Full-Stack Integration, Web Basics, & System Design

### Q1: What happens behind the scenes when you enter a website name in a browser?
1.  **DNS Resolution**: The browser checks its local cache, OS cache, router cache, and finally queries ISP/Root/Authoritative DNS servers to map the domain name (e.g., `teamviewer.com`) to an IP address.
2.  **TCP Handshake**: The browser initiates a 3-way handshake (`SYN`, `SYN-ACK`, `ACK`) with the target server IP to open a reliable TCP connection channel.
3.  **TLS/SSL Handshake**: Over the TCP channel, cryptographic keys are exchanged, identity certificates are validated, and an encrypted layer is established.
4.  **HTTP Request & Server Handling**: The browser sends an HTTP `GET` request. The request hits load balancers or reverse proxies (like Nginx/IIS), which forward it to the backend application server pipeline.
5.  **Browser Rendering**: The server responds with HTML, CSS, and JS assets. The browser parses the DOM tree, fetches linked assets, executes scripts, and renders the visual viewport page.

### Q2: If you are connecting an Angular SPA to an ASP.NET Core Web API, what security strategy (JWT vs Cookies) would you choose, and how do you securely handle silent token refreshes?
*   **Strategy Choice**: **HttpOnly SameSite Cookies** are highly recommended over local-storage stored JWTs for browser environments because they are inherently immune to Cross-Site Scripting (XSS) extraction.
*   **Secure Implementation with Refresh Tokens**: 
    1.  When a user logs in, the API returns a short-lived Access Token (can be in-memory or a cookie) and a long-lived Refresh Token stored as an `HttpOnly`, `Secure`, `SameSite=Strict` cookie.
    2.  An **Angular HTTP Interceptor** intercepts outgoing requests. If the access token expires, it temporarily pauses the request queue and hits a `/refresh` endpoint.
    3.  The backend reads the secure refresh cookie, validates it against the database, issues a new access token, and the interceptor retries the failed requests seamlessly without logging out the user.

### Q3: Write an optimal JavaScript/TypeScript function to flatten deeply nested configuration dictionaries into dot-delimited paths.
```typescript
function flattenObject(obj: any, prefix = '', res: any = {}): any {
  for (const key in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, key)) {
      const propName = prefix ? `${prefix}.${key}` : key;
      if (typeof obj[key] === 'object' && obj[key] !== null && !Array.isArray(obj[key])) {
        flattenObject(obj[key], propName, res);
      } else {
        res[propName] = obj[key];
      }
    }
  }
  return res;
}

// Example usage:
// flattenObject({ a: { b: 1, c: { d: 2 } } }) -> { "a.b": 1, "a.c.d": 2 }
```

### Q4: If a real-time web socket dashboard or REST API begins lagging heavily after deployment under high concurrent loads, what structured sequence do you follow to identify the bottleneck?
1.  **Isolate the Tier**: Check system infrastructure logs to determine if the lag originates from the Frontend (DOM rendering bottleneck), Network Layer (WebSockets connection limits), Backend API application, or Database layer.
2.  **Analyze Infrastructure Metrics**: Check CPU utilization, Memory usage (leaks/GC pressure), and Network I/O on the host servers.
3.  **Database Profiling**: Turn on slow-query logging or APM tools (Application Insights, Datadog) to verify if unindexed queries or connection pool exhaustion are locking up database operations.
4.  **Backend Application Diagnostics**: Look for thread pool starvation or excessive blocking synchronous operations (`.Result` or `.Wait()`) within asynchronous controller routines.
5.  **Frontend/Network Validation**: Use browser DevTools performance profiles to look for long tasks, unnecessary Angular change detection loops, or excessive socket frame re-transmissions.
