# Testing & Performance — Senior .NET Interview Guide 🆕 LATEST

> **Added:** 2026-11-11  
> **Purpose:** Q16.2a BenchmarkDotNet deep dive + Q16.3 GDPR/HIPAA compliance for engineers

---

## Table of Contents

1. [BenchmarkDotNet — Performance Measurement](#1-benchmarkdotnet--performance-measurement)
2. [Memory Profiling & Allocation Analysis](#2-memory-profiling--allocation-analysis)
3. [GDPR Compliance for Engineers](#3-gdpr-compliance-for-engineers)
4. [HIPAA Compliance for Engineers](#4-hipaa-compliance-for-engineers)
5. [Testing Best Practices Summary](#5-testing-best-practices-summary)
6. [Interview Questions](#6-interview-questions)

---

## 1. BenchmarkDotNet — Performance Measurement

### Q1.1 BenchmarkDotNet se .NET performance measure kaise karein?

**Short answer:** `[Benchmark]` attribute lagao, Release mode mein run karo — warmup, iterations, aur statistical analysis automatic hoti hai. `Stopwatch` micro-benchmarks ke liye unreliable hai.

**Answer:**

JIT warmup, GC, aur CPU branch prediction sab `Stopwatch` results distort karte hain. BenchmarkDotNet yeh sab handle karta hai aur reproducible results deta hai.

**Why NOT Stopwatch:**
```csharp
// ❌ BAD — Unreliable
var sw = Stopwatch.StartNew();
var result = SomeMethod();
sw.Stop();
Console.WriteLine($"Took {sw.ElapsedMilliseconds}ms"); // Inaccurate!
```

**Problems:**
- No JIT warmup (first run slower)
- GC can trigger mid-measurement
- No statistical analysis (single run = noise)
- No memory allocation measurement

---

### Basic BenchmarkDotNet Setup

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

[MemoryDiagnoser]  // Track allocations
[SimpleJob(RuntimeMoniker.Net80)]
public class StringBenchmarks
{
    private const string Input = "Hello, World!";
    
    [Benchmark(Baseline = true)]
    public string WithConcat()
    {
        var result = "";
        for (int i = 0; i < 100; i++)
            result += Input;
        return result;
    }
    
    [Benchmark]
    public string WithStringBuilder()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < 100; i++)
            sb.Append(Input);
        return sb.ToString();
    }
}

// Program.cs
class Program
{
    static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<StringBenchmarks>();
    }
}

// Run: dotnet run -c Release
```

**Output Example:**
```
| Method             | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated |
|------------------- |----------:|---------:|---------:|------:|-------:|----------:|
| WithConcat         | 24.50 μs  | 0.420 μs | 0.393 μs |  1.00 | 12.500 |   78 KB   |
| WithStringBuilder  |  1.85 μs  | 0.025 μs | 0.023 μs |  0.08 |  0.781 |    5 KB   |
```

**Key insights:**
- `StringBuilder` is 13x faster (0.08 ratio)
- `StringBuilder` allocates 15x less memory (5 KB vs 78 KB)
- Statistical confidence (StdDev) confirms results

---

### Key Attributes

| Attribute | Purpose | When to Use |
|---|---|---|
| `[Benchmark]` | Mark method to measure | Required on each benchmark method |
| `[MemoryDiagnoser]` | Track allocated bytes/op + GC | Always use — allocation cost critical |
| `[Baseline = true]` | Mark baseline for comparison | One benchmark as reference point |
| `[Params(100, 1000, 10000)]` | Test multiple input sizes | Variable performance across sizes |
| `[SimpleJob]` | Specify runtime/toolchain | Test .NET 8 vs .NET 9 comparison |
| `[ArgumentsSource]` | Complex parameter inputs | Non-primitive benchmark inputs |

---

### Example: Comparing LINQ vs Loop

```csharp
[MemoryDiagnoser]
public class LinqVsLoopBenchmarks
{
    private int[] _data;
    
    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 10000).ToArray();
    }
    
    [Benchmark(Baseline = true)]
    public int SumWithLoop()
    {
        int sum = 0;
        for (int i = 0; i < _data.Length; i++)
            sum += _data[i];
        return sum;
    }
    
    [Benchmark]
    public int SumWithLinq()
    {
        return _data.Sum();
    }
    
    [Benchmark]
    public int SumWithForeach()
    {
        int sum = 0;
        foreach (var item in _data)
            sum += item;
        return sum;
    }
}
```

**Typical Results:**
- `for` loop fastest (baseline)
- `foreach` similar (JIT optimizes well)
- LINQ slightly slower but negligible for most cases

---

### Span<T> Optimization Example

```csharp
[MemoryDiagnoser]
public class SpanBenchmarks
{
    private byte[] _data;
    
    [Params(100, 1000, 10000)]
    public int Size { get; set; }
    
    [GlobalSetup]
    public void Setup()
    {
        _data = new byte[Size];
        new Random(42).NextBytes(_data);
    }
    
    [Benchmark(Baseline = true)]
    public int SumWithArray()
    {
        int sum = 0;
        for (int i = 0; i < _data.Length; i++)
            sum += _data[i];
        return sum;
    }
    
    [Benchmark]
    public int SumWithSpan()
    {
        Span<byte> span = _data;
        int sum = 0;
        for (int i = 0; i < span.Length; i++)
            sum += span[i];
        return sum;
    }
}
```

**Key insight:** `Span<T>` has bounds-check elimination advantages in hot loops — measure to confirm benefit in your case.

---

### Pitfalls

| Pitfall | Explanation | Fix |
|---|---|---|
| **Debug mode** | JIT optimization disabled, 10-100x slower | Always `dotnet run -c Release` |
| **Small workload** | Benchmark overhead dominates signal | Increase work per iteration |
| **Side effects** | Compiler may eliminate dead code | Return result, use `Consume()` |
| **GC interference** | GC triggered mid-run skews results | BenchmarkDotNet handles this (multiple runs) |
| **Shared state** | Parallel benchmarks interfere | Use `[GlobalSetup]` carefully |

---

### Interview Answer Template

**Q: "How would you measure performance of a hot path?"**

**Strong answer:**
> "Pehle profile karta hoon (dotnet-trace / Visual Studio Profiler) to identify hot path. Phir BenchmarkDotNet se isolated micro-benchmark banata hoon — `[MemoryDiagnoser]` se allocations track karta hoon, `[Params]` se realistic input sizes test karta hoon. Release mode mein run karta hoon, kyunki Debug mode JIT optimizations disable karta hai. Results se baseline compare karta hoon — for example, recent optimization mein `string` concatenation ko `StringBuilder` se replace karke 80% allocation reduction aur 12x speedup mila. Production mein deploy karne se pehle load test se end-to-end impact validate karta hoon."

**Red flags:**
- ❌ "I use `Stopwatch` in a loop"
- ❌ "I measure in Debug mode"
- ❌ "I run the benchmark once and trust the number"

---

## 2. Memory Profiling & Allocation Analysis

### Understanding Memory Diagnoser Output

```
| Method          | Mean    | Gen0  | Gen1  | Allocated |
|---------------- |--------:|------:|------:|----------:|
| AllocateStrings | 2.5 ms  | 500.0 | 50.0  | 3.5 MB    |
```

**What these mean:**

- **Allocated:** Total bytes allocated per operation
- **Gen0:** Gen 0 collections per 1000 operations
- **Gen1:** Gen 1 collections per 1000 operations
- **Gen2:** Gen 2 collections per 1000 operations (if present)

**Goal:** Minimize allocations in hot paths — reduce GC pressure.

---

### Hot Path Optimization Strategy

1. **Profile** — Identify allocation hot spots (dotnet-trace, dotMemory)
2. **Measure** — BenchmarkDotNet baseline
3. **Optimize** — Apply patterns (Span, ArrayPool, struct)
4. **Verify** — Re-benchmark, confirm improvement
5. **Validate** — Production load test

**Common optimization patterns:**

| Pattern | Before | After | Savings |
|---|---|---|---|
| `string` concat → `StringBuilder` | 78 KB | 5 KB | 93% |
| `List<T>` → `ArrayPool<T>` | 8 KB | 0 B | 100% |
| `string.Substring()` → `Span<T>` | 2 KB | 0 B | 100% |
| Boxing (`object`) → Generic | 24 B/call | 0 B | 100% |

---

## 3. GDPR Compliance for Engineers

### Q3.1 GDPR ko engineer ke roop mein kaise approach karein?

**Short answer:** Pehle product/legal/security team se applicability aur data scope confirm karein. Data minimization, purpose limitation, access control, encryption, auditability, retention/deletion, incident process implement karke evidence maintain karein.

**Answer:**

GDPR (General Data Protection Regulation) — EU residents ka personal data protect karta hai.

**Personal Data kya hai:**
- Name, email, phone, address
- IP address, cookies, device IDs
- Location data
- Financial info, health data
- Anything jo individual ko identify kar sake

---

### GDPR Principles for Developers

| Principle | Engineering Implementation |
|---|---|
| **Lawfulness** | Document legal basis (consent, contract, legitimate interest) |
| **Purpose limitation** | Collect only for stated purpose; no secondary use without consent |
| **Data minimization** | Collect least data needed; don't store "just in case" |
| **Accuracy** | Allow users to update their data |
| **Storage limitation** | Retention policy + automated deletion |
| **Integrity & confidentiality** | Encryption at rest + in transit; access controls |
| **Accountability** | Audit logs, documentation, evidence |

---

### Data Subject Rights (Technical Implementation)

| Right | What It Means | Implementation |
|---|---|---|
| **Right to access** | User can request their data | Export endpoint: `GET /api/users/{id}/data-export` (JSON/CSV) |
| **Right to rectification** | User can correct their data | Update endpoints with validation |
| **Right to erasure** | "Right to be forgotten" | Soft delete + anonymization; cascade to backups |
| **Right to portability** | Export data in machine-readable format | Structured JSON/CSV export |
| **Right to object** | Stop processing for marketing, etc. | Opt-out flags; respect preferences |
| **Right to restrict** | Temporarily stop processing | Status flags (active/restricted) |

---

### Code Example: Data Export Endpoint

```csharp
[HttpGet("users/{userId}/data-export")]
[Authorize]
public async Task<IActionResult> ExportUserData(string userId)
{
    // Verify requesting user owns this data or is admin
    if (!User.IsInRole("Admin") && User.FindFirst(ClaimTypes.NameIdentifier)?.Value != userId)
        return Forbid();
    
    var userData = new
    {
        PersonalInfo = await _userService.GetPersonalInfoAsync(userId),
        Orders = await _orderService.GetUserOrdersAsync(userId),
        Preferences = await _preferencesService.GetUserPreferencesAsync(userId),
        ActivityLog = await _activityService.GetUserActivityAsync(userId)
    };
    
    var json = JsonSerializer.Serialize(userData, new JsonSerializerOptions
    {
        WriteIndented = true
    });
    
    // Audit log
    await _auditService.LogAsync($"User {userId} exported their data", userId);
    
    return File(Encoding.UTF8.GetBytes(json), "application/json", $"user-data-{userId}.json");
}
```

---

### Consent Management

```csharp
public class ConsentRecord
{
    public string UserId { get; set; }
    public string Purpose { get; set; } // "Marketing", "Analytics", "ThirdPartySharing"
    public bool Granted { get; set; }
    public DateTime Timestamp { get; set; }
    public string Version { get; set; } // Privacy policy version
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
}

// Record consent
public async Task RecordConsentAsync(string userId, string purpose, bool granted)
{
    var consent = new ConsentRecord
    {
        UserId = userId,
        Purpose = purpose,
        Granted = granted,
        Timestamp = DateTime.UtcNow,
        Version = _config["PrivacyPolicyVersion"],
        IpAddress = _httpContext.Connection.RemoteIpAddress?.ToString(),
        UserAgent = _httpContext.Request.Headers["User-Agent"]
    };
    
    await _consentRepository.AddAsync(consent);
}
```

---

### Data Retention & Deletion

```csharp
public class RetentionPolicy
{
    public async Task ApplyRetentionPolicyAsync()
    {
        var cutoffDate = DateTime.UtcNow.AddYears(-2); // 2-year retention
        
        // Soft delete + anonymize
        var staleUsers = await _db.Users
            .Where(u => u.LastActivity < cutoffDate && !u.IsDeleted)
            .ToListAsync();
        
        foreach (var user in staleUsers)
        {
            user.Email = $"deleted-{user.Id}@anonymized.local";
            user.Name = "Deleted User";
            user.Phone = null;
            user.Address = null;
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
        }
        
        await _db.SaveChangesAsync();
        
        // Audit
        await _auditService.LogAsync($"Retention policy applied: {staleUsers.Count} users anonymized");
    }
}
```

---

### Breach Notification (72 Hours)

```csharp
public class BreachIncidentResponse
{
    public async Task HandleBreachAsync(BreachDetails breach)
    {
        // 1. Contain
        await ContainBreachAsync(breach);
        
        // 2. Assess impact
        var affectedUsers = await IdentifyAffectedUsersAsync(breach);
        
        // 3. Notify authorities (72 hours from awareness)
        await NotifyDataProtectionAuthorityAsync(breach, affectedUsers);
        
        // 4. Notify affected users (without undue delay)
        if (breach.HighRisk)
            await NotifyAffectedUsersAsync(affectedUsers, breach);
        
        // 5. Document
        await DocumentIncidentAsync(breach, affectedUsers);
    }
}
```

---

### Encryption Requirements

| Data State | Requirement | Implementation |
|---|---|---|
| **At rest** | Encrypt sensitive data | SQL TDE, disk encryption, column-level encryption |
| **In transit** | HTTPS/TLS 1.2+ | Force HTTPS, HSTS headers |
| **In backups** | Encrypted backups | Backup encryption enabled |
| **In logs** | PII redaction | Structured logging filters |

```csharp
// ❌ BAD — PII in logs
_logger.LogInformation($"User {user.Email} logged in");

// ✅ GOOD — Redacted
_logger.LogInformation($"User {user.Id} logged in");
```

---

### Interview Answer Template

**Q: "How do you handle GDPR in your application?"**

**Strong answer:**
> "GDPR requirements ko product/legal team ke saath confirm karta hoon — kaunsa data personal hai, legal basis kya hai. Technical implementation mein: data export endpoint (JSON format), soft delete + anonymization for 'right to be forgotten', consent records with timestamp/version/IP, retention policy automation (2-year cutoff), encryption at rest (TDE) aur in transit (TLS 1.2+), PII redaction in logs, aur breach response plan. Recent project mein user export endpoint implement kiya tha jo complete data (orders, preferences, activity) structured JSON mein deta hai, aur scheduled job har month retention policy apply karta hai. Audit logs har data access track karte hain. Compliance organizational responsibility hai, lekin engineering implementation robust hona chahiye."

---

## 4. HIPAA Compliance for Engineers

### Q4.1 HIPAA kya hai aur developers ko kya karna chahiye?

**Short answer:** HIPAA (Health Insurance Portability and Accountability Act) — US healthcare mein Protected Health Information (PHI) ko protect karta hai. Technical safeguards: encryption, access controls, audit logging, session timeouts, backup/DR.

**Answer:**

**PHI (Protected Health Information) kya hai:**
- Medical records, diagnoses, prescriptions
- Lab results, treatment plans
- Insurance/billing info tied to patient
- Any health data linked to individual

---

### Technical Safeguards (HIPAA Security Rule)

| Safeguard | Requirement | Implementation |
|---|---|---|
| **Unique User IDs** | Each user must have unique identifier | No shared accounts; individual logins |
| **Emergency Access** | Break-glass access with audit trail | Admin override + detailed logging |
| **Automatic Logoff** | Idle session termination | 15-minute timeout (configurable) |
| **Encryption** | PHI encrypted at rest + in transit | AES-256 at rest, TLS 1.2+ in transit |
| **Audit Controls** | Log access to PHI | WHO accessed WHAT WHEN |
| **Integrity** | Protect data from alteration | Checksums, digital signatures |
| **Authentication** | Strong authentication mechanisms | MFA for PHI access |

---

### Access Control Implementation

```csharp
[Authorize(Policy = "RequiresMFA")]
[AuditLog(Resource = "PatientRecord")]
public class PatientRecordsController : ControllerBase
{
    [HttpGet("{patientId}")]
    public async Task<IActionResult> GetPatientRecord(string patientId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        // Check authorization
        if (!await _authService.CanAccessPatientAsync(userId, patientId))
        {
            await _auditService.LogUnauthorizedAccessAttemptAsync(userId, patientId);
            return Forbid();
        }
        
        var record = await _patientService.GetRecordAsync(patientId);
        
        // Audit log
        await _auditService.LogAccessAsync(new AccessLog
        {
            UserId = userId,
            ResourceType = "PatientRecord",
            ResourceId = patientId,
            Action = "Read",
            Timestamp = DateTime.UtcNow,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
        
        return Ok(record);
    }
}
```

---

### Audit Logging (Critical)

**What to log:**
- WHO (user ID, role)
- WHAT (resource type, resource ID, action)
- WHEN (timestamp with timezone)
- WHERE (IP address, device)
- RESULT (success/failure, error if any)

```csharp
public class AccessLog
{
    public string UserId { get; set; }
    public string UserRole { get; set; }
    public string ResourceType { get; set; } // "PatientRecord", "Prescription"
    public string ResourceId { get; set; }
    public string Action { get; set; } // "Read", "Create", "Update", "Delete"
    public DateTime Timestamp { get; set; }
    public string IpAddress { get; set; }
    public string DeviceInfo { get; set; }
    public bool Success { get; set; }
    public string FailureReason { get; set; }
}

// Retention: HIPAA requires minimum 6 years
```

---

### Encryption at Rest

```csharp
// SQL Server Transparent Data Encryption (TDE)
// Enable at database level — no code changes

// Column-level encryption (for extra sensitive fields)
public class PatientRecord
{
    public string PatientId { get; set; }
    public string Name { get; set; }
    
    [EncryptedColumn]
    public string SocialSecurityNumber { get; set; } // Extra protection
    
    [EncryptedColumn]
    public string DiagnosisNotes { get; set; }
}

// Custom encryption attribute implementation:
public class EncryptedColumnAttribute : Attribute { }

public class EncryptionService
{
    private readonly byte[] _key; // From Azure Key Vault
    
    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        // Prepend IV to ciphertext
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }
    
    public string Decrypt(string cipherText)
    {
        var fullCipher = Convert.FromBase64String(cipherText);
        
        using var aes = Aes.Create();
        aes.Key = _key;
        
        // Extract IV from beginning
        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];
        
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

---

### Session Management

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(15); // HIPAA requirement
            options.Cookie.HttpOnly = true;
            options.Cookie.Secure = true; // HTTPS only
            options.Cookie.SameSite = SameSiteMode.Strict;
        });
        
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
                options.SlidingExpiration = false; // Absolute timeout
            });
    }
}
```

---

### Business Associate Agreement (BAA)

**Vendor checklist:**
- AWS/Azure/Google Cloud: Execute BAA (they provide templates)
- SaaS providers (logging, monitoring, email): Confirm HIPAA compliance + BAA
- Payment processors: PCI-DSS + HIPAA if handling PHI

**Red flag:** "Cloud provider se sirf BAA sign karna enough hai" — application-level controls bhi implement karne hote hain.

---

### Backup & Disaster Recovery

```csharp
public class BackupPolicy
{
    // HIPAA requires exact copy retrievable for 6 years
    
    public async Task PerformBackupAsync()
    {
        // 1. Encrypted backup
        await TriggerDatabaseBackupAsync(encrypted: true);
        
        // 2. Store in geographically separate location
        await ReplicateToSecondaryRegionAsync();
        
        // 3. Test restore quarterly
        if (DateTime.UtcNow.Month % 3 == 0)
            await ScheduleRestoreTestAsync();
        
        // 4. Audit log
        await _auditService.LogAsync("Backup completed", resourceType: "Database");
    }
}
```

---

### Interview Answer Template

**Q: "How would you implement HIPAA compliance in a healthcare application?"**

**Strong answer:**
> "HIPAA compliance ka pehla step legal/compliance team se requirements confirm karna hai — kaunsa data PHI hai. Technical implementation mein: unique user IDs (no shared accounts), MFA for PHI access, AES-256 encryption at rest (SQL TDE), TLS 1.2+ in transit, comprehensive audit logging (WHO accessed WHAT WHEN with 6-year retention), 15-minute idle session timeout, role-based access control with least privilege, Business Associate Agreements with vendors, encrypted backups with quarterly restore testing. Recent project mein patient record access API implement kiya tha jo har access audit log karta hai — user ID, patient ID, timestamp, IP address, action — aur unsuccessful access attempts immediately alert trigger karte hain. Encryption keys Azure Key Vault mein rotate hoti hain quarterly. Compliance organizational responsibility hai, but engineering robust technical safeguards implement karti hai."

---

## 5. Testing Best Practices Summary

### Test Pyramid

```
        /\
       /  \  E2E (Few)
      /----\
     /      \  Integration (Some)
    /--------\
   /          \  Unit (Many)
  /____________\
```

**Distribution:**
- **70% Unit tests** — Fast, isolated, test single component
- **20% Integration tests** — Test boundaries (DB, HTTP, message queues)
- **10% E2E tests** — Critical user flows end-to-end

---

### Testing Checklist

| Type | Tool | What to Test |
|---|---|---|
| **Unit** | xUnit/NUnit + Moq | Business logic, algorithms, domain models |
| **Integration** | WebApplicationFactory | API endpoints, database queries, middleware |
| **Performance** | BenchmarkDotNet | Hot paths, allocation hotspots |
| **Load** | k6, JMeter, Artillery | Concurrency, throughput, SLOs |
| **Security** | OWASP ZAP, Snyk | Vulnerabilities, dependencies |
| **Mutation** | Stryker.NET | Test quality (do tests catch bugs?) |

---

## 6. Interview Questions

### Q6.1 BenchmarkDotNet ka use kab karein?

**Answer:** Hot path optimization ke liye — jab profiling se allocation hotspot ya CPU-heavy code path identify ho. Production-like workload ke saath benchmark karein, Release mode mein, `[MemoryDiagnoser]` se allocations track karein. Baseline compare karein (before/after). Load test se end-to-end impact validate karein.

---

### Q6.2 GDPR vs HIPAA — kya farq hai?

| Aspect | GDPR | HIPAA |
|---|---|---|
| **Geography** | EU (European residents) | US (Healthcare industry) |
| **Data type** | Personal data (broad) | Protected Health Information |
| **Breach notification** | 72 hours to authority | 60 days to individuals |
| **Consent** | Explicit consent required (many cases) | Consent or legal justification |
| **Penalties** | Up to €20M or 4% revenue | Up to $1.5M per violation |

**Both require:** Encryption, access controls, audit logging, data minimization, breach response plans.

---

### Q6.3 Encryption at rest vs in transit — dono zaroori kyun hain?

**Answer:**

- **At rest:** Protects against physical theft, unauthorized DB access, backup breaches
- **In transit:** Protects against network sniffing, man-in-the-middle attacks

**Implementation:**
- At rest: SQL TDE, disk encryption, Azure Storage encryption
- In transit: TLS 1.2+, certificate pinning, HSTS headers

**Pitfall:** "HTTPS hai to at-rest encryption nahin chahiye" — galat. Dono layers independent hain.

---

**Good luck!** Testing aur compliance robust engineering practices ka sign hai. 🚀
