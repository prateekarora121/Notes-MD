# Railway-Oriented Programming — Result Pattern Guide 🆕 LATEST

> **Added:** 2026-11-11 (Q2.6a from Interview Prep Master)  
> **Purpose:** Modern C# pattern for handling expected failures without exceptions — cleaner code, better performance, explicit error handling.

---

## Table of Contents

1. [The Problem with Exceptions for Business Logic](#1-the-problem-with-exceptions-for-business-logic)
2. [Railway-Oriented Programming Concept](#2-railway-oriented-programming-concept)
3. [Result Pattern Implementation](#3-result-pattern-implementation)
4. [Practical Examples](#4-practical-examples)
5. [Integration with ASP.NET Core](#5-integration-with-aspnet-core)
6. [MediatR Pipeline Integration](#6-mediatr-pipeline-integration)
7. [When to Use Result vs Exceptions](#7-when-to-use-result-vs-exceptions)
8. [Interview Questions](#8-interview-questions)

---

## 1. The Problem with Exceptions for Business Logic

### Exception-Based Approach (❌ Avoid for Expected Failures)

```csharp
public async Task<Booking> CreateBookingAsync(BookingRequest request)
{
    if (request == null)
        throw new ArgumentNullException(nameof(request));
    
    if (string.IsNullOrEmpty(request.CustomerId))
        throw new ValidationException("Customer ID is required");
    
    if (request.CheckInDate < DateTime.UtcNow)
        throw new BusinessRuleException("Check-in date cannot be in the past");
    
    var room = await _roomRepository.GetByIdAsync(request.RoomId);
    if (room == null)
        throw new NotFoundException("Room not found");
    
    if (!room.IsAvailable(request.CheckInDate, request.CheckOutDate))
        throw new ConflictException("Room is not available for selected dates");
    
    var booking = new Booking(request);
    await _repository.AddAsync(booking);
    return booking;
}

// Caller code:
try
{
    var booking = await _service.CreateBookingAsync(request);
    return Ok(booking);
}
catch (ValidationException ex)
{
    return BadRequest(ex.Message);
}
catch (NotFoundException ex)
{
    return NotFound(ex.Message);
}
catch (ConflictException ex)
{
    return Conflict(ex.Message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error");
    return StatusCode(500, "Internal server error");
}
```

### Problems with This Approach

| Problem | Explanation |
|---|---|
| **Performance** | Stack trace allocation for every validation failure (~30-50% overhead) |
| **Hidden control flow** | Exceptions can be thrown deep in call stack — hard to trace |
| **Maintenance** | Try-catch everywhere; easy to miss exception types |
| **Unclear intent** | Is this "exceptional" or expected validation? |
| **Testing difficulty** | Testing exception paths verbose and brittle |

**Key insight:** Expected business failures (validation, not found, conflict) are NOT exceptional — they're normal flow.

---

## 2. Railway-Oriented Programming Concept

### The Railway Metaphor

```
Success Track:  ────────────────────────────────────► Success
                    ↓ failure
                    ↓
Failure Track:  ──────────────────────────────────────► Failure
```

**Idea:**
- Functions return `Result<T>` instead of `T` or throwing
- Success stays on success track
- First failure switches to failure track
- Once on failure track, operations short-circuit (skip remaining steps)

**Benefits:**
- Explicit success/failure paths in method signature
- No hidden control flow
- Composable with LINQ-style chaining
- Fast (no stack trace allocation)

---

## 3. Result Pattern Implementation

### Basic Result Type

```csharp
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error { get; }

    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && !string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Success result cannot have error");
        if (!isSuccess && string.IsNullOrEmpty(error))
            throw new InvalidOperationException("Failure result must have error");
        
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new Result(true, string.Empty);
    public static Result Failure(string error) => new Result(false, error);
}

public class Result<T> : Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, string error) 
        : base(isSuccess, error)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new Result<T>(value, true, string.Empty);
    public static new Result<T> Failure(string error) => new Result<T>(default, false, error);
}
```

### Richer Result with Error Details

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public Error Error { get; }

    private Result(bool isSuccess, T value, Error error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, Error.None);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "Null value was provided");
}

// Domain-specific errors:
public static class BookingErrors
{
    public static Error NotFound(string bookingId) =>
        new("Booking.NotFound", $"Booking with ID {bookingId} was not found");
    
    public static Error RoomNotAvailable(string roomId) =>
        new("Booking.RoomNotAvailable", $"Room {roomId} is not available");
    
    public static Error InvalidDates =>
        new("Booking.InvalidDates", "Check-out date must be after check-in date");
}
```

---

## 4. Practical Examples

### Example 1: Booking Service with Result Pattern (✅ Recommended)

```csharp
public async Task<Result<Booking>> CreateBookingAsync(BookingRequest request)
{
    // Validation
    if (request == null)
        return Result<Booking>.Failure("Request cannot be null");
    
    if (string.IsNullOrEmpty(request.CustomerId))
        return Result<Booking>.Failure("Customer ID is required");
    
    if (request.CheckInDate < DateTime.UtcNow)
        return Result<Booking>.Failure("Check-in date cannot be in the past");
    
    if (request.CheckOutDate <= request.CheckInDate)
        return Result<Booking>.Failure("Check-out must be after check-in");
    
    // Business logic
    var room = await _roomRepository.GetByIdAsync(request.RoomId);
    if (room == null)
        return Result<Booking>.Failure($"Room {request.RoomId} not found");
    
    if (!room.IsAvailable(request.CheckInDate, request.CheckOutDate))
        return Result<Booking>.Failure("Room is not available for selected dates");
    
    // Success path
    var booking = new Booking(request);
    await _repository.AddAsync(booking);
    
    return Result<Booking>.Success(booking);
}

// Caller code:
var result = await _service.CreateBookingAsync(request);

if (result.IsFailure)
    return BadRequest(result.Error);

return Ok(result.Value);
```

**Benefits visible:**
- No try-catch needed
- Clear success/failure paths
- Fast (no stack traces)
- Easy to test
- Explicit in method signature

---

### Example 2: Chaining Operations (Railway Style)

```csharp
public async Task<Result<Order>> PlaceOrderAsync(PlaceOrderRequest request)
{
    var validationResult = ValidateRequest(request);
    if (validationResult.IsFailure)
        return Result<Order>.Failure(validationResult.Error);
    
    var customerResult = await GetCustomerAsync(request.CustomerId);
    if (customerResult.IsFailure)
        return Result<Order>.Failure(customerResult.Error);
    
    var inventoryResult = await ReserveInventoryAsync(request.Items);
    if (inventoryResult.IsFailure)
        return Result<Order>.Failure(inventoryResult.Error);
    
    var paymentResult = await ProcessPaymentAsync(customerResult.Value, request.PaymentInfo);
    if (paymentResult.IsFailure)
    {
        await ReleaseInventoryAsync(inventoryResult.Value); // Compensating action
        return Result<Order>.Failure(paymentResult.Error);
    }
    
    var order = new Order(customerResult.Value, inventoryResult.Value, paymentResult.Value);
    await _repository.AddAsync(order);
    
    return Result<Order>.Success(order);
}
```

**With extension methods (more elegant):**

```csharp
public static class ResultExtensions
{
    public static async Task<Result<TOut>> Bind<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<Result<TOut>>> func)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.Error);
        
        return await func(result.Value);
    }
    
    public static async Task<Result<TOut>> Map<TIn, TOut>(
        this Result<TIn> result,
        Func<TIn, Task<TOut>> func)
    {
        if (result.IsFailure)
            return Result<TOut>.Failure(result.Error);
        
        var value = await func(result.Value);
        return Result<TOut>.Success(value);
    }
}

// Usage:
public async Task<Result<Order>> PlaceOrderAsync(PlaceOrderRequest request)
{
    return await ValidateRequest(request)
        .Bind(_ => GetCustomerAsync(request.CustomerId))
        .Bind(customer => ReserveInventoryAsync(request.Items)
            .Map(inventory => (customer, inventory)))
        .Bind(async tuple => await ProcessPaymentAsync(tuple.customer, request.PaymentInfo)
            .Map(payment => new Order(tuple.customer, tuple.inventory, payment)))
        .Bind(order => SaveOrderAsync(order));
}
```

---

## 5. Integration with ASP.NET Core

### Minimal APIs with Typed Results (.NET 8+)

```csharp
app.MapPost("/bookings", async (
    BookingRequest request,
    IBookingService service) =>
{
    var result = await service.CreateBookingAsync(request);
    
    return result.IsSuccess
        ? Results.Ok(result.Value)
        : Results.BadRequest(result.Error);
});

// Even better — extension method:
app.MapPost("/bookings", async (
    BookingRequest request,
    IBookingService service) =>
{
    var result = await service.CreateBookingAsync(request);
    return result.ToHttpResult();
});

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return Results.Ok(result.Value);
        
        // Map error codes to HTTP status codes
        return result.Error.Code switch
        {
            "NotFound" => Results.NotFound(result.Error.Message),
            "Validation" => Results.BadRequest(result.Error.Message),
            "Conflict" => Results.Conflict(result.Error.Message),
            _ => Results.Problem(result.Error.Message)
        };
    }
}
```

### Controller Action with Result

```csharp
[ApiController]
[Route("api/[controller]")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _service;

    public BookingsController(IBookingService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request)
    {
        var result = await _service.CreateBookingAsync(request);
        
        if (result.IsFailure)
            return BadRequest(new ProblemDetails
            {
                Title = "Booking creation failed",
                Detail = result.Error,
                Status = 400
            });
        
        return CreatedAtAction(
            nameof(GetBooking),
            new { id = result.Value.Id },
            result.Value);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(string id)
    {
        var result = await _service.GetBookingAsync(id);
        
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound();
    }
}
```

---

## 6. MediatR Pipeline Integration

### Command Handler with Result

```csharp
public record CreateBookingCommand(BookingRequest Request) : IRequest<Result<Booking>>;

public class CreateBookingCommandHandler 
    : IRequestHandler<CreateBookingCommand, Result<Booking>>
{
    private readonly IBookingRepository _repository;

    public CreateBookingCommandHandler(IBookingRepository repository) 
        => _repository = repository;

    public async Task<Result<Booking>> Handle(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;
        
        // Validation
        if (string.IsNullOrEmpty(request.CustomerId))
            return Result<Booking>.Failure("Customer ID is required");
        
        // Business logic...
        var booking = new Booking(request);
        await _repository.AddAsync(booking, cancellationToken);
        
        return Result<Booking>.Success(booking);
    }
}
```

### Pipeline Behavior for Validation

```csharp
public class ValidationBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            // Return Result.Failure if TResponse is Result<T>
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod(nameof(Result<object>.Failure));
                
                return (TResponse)failureMethod.Invoke(null, new object[] { errorMessage });
            }
        }

        return await next();
    }
}
```

---

## 7. When to Use Result vs Exceptions

### Use Result Pattern For:

✅ **Expected business failures**
- Validation errors
- Business rule violations
- Not found scenarios
- Conflict/duplicate scenarios
- Authorization failures (domain level)

✅ **Control flow you want explicit**
- Multi-step workflows
- Complex domain logic
- Public API contracts

### Use Exceptions For:

✅ **Infrastructure failures**
- Database connection failures
- Network timeouts
- File system errors
- External service unavailability

✅ **Programming errors (bugs)**
- Null reference where shouldn't happen
- Index out of bounds
- Invalid cast
- Contract violations (preconditions)

✅ **Unrecoverable states**
- Out of memory
- Stack overflow
- Configuration errors at startup

### Comparison Table

| Scenario | Use | Reason |
|---|---|---|
| User enters invalid email | Result | Expected validation failure |
| Room not available for booking | Result | Normal business scenario |
| Payment declined | Result | Expected outcome in payment flow |
| Database connection timeout | Exception | Infrastructure issue, caller can't handle |
| Null argument where required | Exception | Programming error (contract violation) |
| User not authorized for resource | Result (domain) / Exception (framework) | Context-dependent |

---

## 8. Interview Questions

### Q8.1 Railway-Oriented Programming kya hai?

**Short answer:** Expected business failures ko `Result<T>` return karke handle karna instead of exceptions — success/failure paths explicit hote hain, performance better hoti hai, control flow visible hota hai.

**Answer:**
Exceptions expensive hain (stack trace allocation) aur hidden control flow banate hain. Railway pattern mein:
- Method `Result<T>` return karta hai (not `T` or throw)
- Success path continues normally
- First failure short-circuits remaining operations
- Caller explicitly checks `IsSuccess` / `IsFailure`

Benefits:
- **Performance:** 30-50% faster than exception-based validation
- **Predictability:** Method signature mein failure possibility visible
- **Cleaner code:** No try-catch everywhere
- **Easier testing:** Simple assertions on result state

**Follow-ups:**
- **vs OneOf/Either types?** Similar concept; Result semantics specific to success/failure; Either more general (left/right). Choice depends on team preference.
- **Overhead?** Result object allocation negligible compared to exception stack trace.

---

### Q8.2 Exception vs Result — kab kya use karein?

**Answer:**

**Rule of thumb:**
- **Expected business outcomes** → Result
- **Infrastructure failures / programming errors** → Exception

**Example:**
```csharp
// Good: Expected validation failure
public Result<User> CreateUser(string email)
{
    if (!IsValidEmail(email))
        return Result<User>.Failure("Invalid email format");
    // ...
}

// Good: Infrastructure failure
public async Task<User> GetUserFromDatabase(string id)
{
    try
    {
        return await _db.Users.FindAsync(id); // May throw SqlException
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error");
        throw; // Let framework handle infrastructure failure
    }
}
```

**Pitfall:** Mixing approaches inconsistently. Team ko ek pattern choose karna chahiye aur document karna chahiye.

---

### Q8.3 MediatR ke saath Result pattern kaise integrate karein?

**Answer:**

Command/Query handlers `IRequest<Result<T>>` return karte hain:

```csharp
public record CreateOrderCommand(OrderRequest Request) : IRequest<Result<Order>>;

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, Result<Order>>
{
    public async Task<Result<Order>> Handle(CreateOrderCommand cmd, CancellationToken ct)
    {
        // Validation
        if (cmd.Request.Items.Count == 0)
            return Result<Order>.Failure("Order must have at least one item");
        
        // Business logic
        var order = new Order(cmd.Request);
        await _repo.AddAsync(order, ct);
        return Result<Order>.Success(order);
    }
}

// Controller:
[HttpPost]
public async Task<IActionResult> CreateOrder(OrderRequest request)
{
    var result = await _mediator.Send(new CreateOrderCommand(request));
    return result.IsSuccess
        ? Ok(result.Value)
        : BadRequest(result.Error);
}
```

**Pipeline behavior** validation results ko automatically Result mein convert kar sakta hai (Q6 mein example).

---

### Q8.4 Result pattern libraries kaunsi hain?

**Answer:**

| Library | Features | When to Use |
|---|---|---|
| **CSharpFunctionalExtensions** | `Result<T>`, `Maybe<T>`, LINQ extensions, railway methods | Full functional programming toolkit |
| **LanguageExt** | Comprehensive FP library (Either, Option, Try, etc.) | Serious functional programming |
| **.NET 8+ Minimal APIs** | `Results<Ok<T>, BadRequest>` built-in | Simple API projects, no extra dependencies |
| **FluentResults** | Rich error details, reasons, custom metadata | Complex error scenarios |
| **Custom implementation** | Full control, zero dependencies | Team wants specific semantics |

**Recommendation:**
- Simple projects: Custom implementation (Q3 example)
- Functional style: CSharpFunctionalExtensions
- Minimal APIs: Built-in typed results

---

### Q8.5 Performance difference kitna hai?

**Answer:**

**Benchmark (typical):**

| Approach | Mean Time | Allocated Memory |
|---|---|---|
| Exception (validation failure) | 150 ns | 120 B |
| Result (validation failure) | 5 ns | 32 B |

**Stack trace allocation costly hai:**
- Exception: Stack trace capture, string allocation, stack walk
- Result: Simple object allocation

**Production impact:**
- High-throughput validation (1000s requests/sec): 30-50% CPU reduction
- Memory pressure reduction: Fewer Gen 0 collections

**Pitfall:** Over-optimizing rare paths. Focus on hot paths (validation, business rules). Infrastructure exceptions (DB failures) are fine — rare enough.

---

### Q8.6 Async operations ke saath chaining kaise karein?

**Answer:**

Extension methods se railway-style composition:

```csharp
public static async Task<Result<TOut>> Bind<TIn, TOut>(
    this Task<Result<TIn>> resultTask,
    Func<TIn, Task<Result<TOut>>> func)
{
    var result = await resultTask;
    if (result.IsFailure)
        return Result<TOut>.Failure(result.Error);
    
    return await func(result.Value);
}

// Usage:
var result = await ValidateRequestAsync(request)
    .Bind(validated => GetCustomerAsync(validated.CustomerId))
    .Bind(customer => ProcessOrderAsync(customer, request));

if (result.IsFailure)
    return BadRequest(result.Error);

return Ok(result.Value);
```

**Benefits:** Clear pipeline, early exit on failure, composable operations.

---

## Summary — Interview Ready Answer

**Railway-Oriented Programming formula:**

1. **Concept:** Expected failures ko explicit Result mein wrap karo instead of throwing exceptions
2. **Benefits:** Performance (no stack trace), predictability (method signature mein visible), cleaner code (no try-catch)
3. **Usage:** Validation, business rules, not-found, conflicts → Result; Infrastructure failures → Exception
4. **Integration:** MediatR handlers, ASP.NET Core endpoints, extension methods for chaining

**Example script:**
> "Result pattern use karta hoon expected business failures ke liye — validation, not-found, conflicts. Method `Result<T>` return karta hai instead of throwing. Yeh 30-50% faster hai because stack trace allocate nahin hota. Recent payment API integration mein, validation failures ko Result se handle kiya, infrastructure errors (DB timeout) ko exceptions se. ASP.NET Core mein `.ToHttpResult()` extension method se Result ko appropriate HTTP status mein map karta hoon. MediatR handlers bhi `IRequest<Result<T>>` implement karte hain, pipeline behavior validation automatically Result mein convert kar deti hai."

**Good luck!** 🚀
