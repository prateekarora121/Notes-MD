# SOTI Interview Questions and Answers

An interview-preparation guide based on the supplied PDF. It covers coding,
data structures and algorithms, C#/.NET, OOP, SQL, operating systems,
networking, puzzles, and HR topics.

> These are practice questions based on reported topics, not a confirmed or
> guaranteed SOTI interview paper. Personalize sample HR answers and verify
> current company and product information before using it in an interview.

## How to answer interview questions

1. Clarify assumptions and edge cases.
2. Explain the approach before writing code.
3. Walk through a small example.
4. State time and auxiliary-space complexity.
5. Mention trade-offs or alternatives when useful.

## 1. Coding questions

### 1.1 Reverse a string without extra space; check whether it is a palindrome

Use two pointers, one at each end. Swap characters while the pointers move
inward. A palindrome has matching characters at mirrored positions. C# strings
are immutable, so use a `char[]` for in-place reversal; creating that array
takes O(n) storage.

Example: `"level"` is a palindrome; reversing `"hello"` gives `"olleh"`.

```csharp
static void ReverseInPlace(char[] chars)
{
    int left = 0, right = chars.Length - 1;
    while (left < right)
    {
        (chars[left], chars[right]) = (chars[right], chars[left]);
        left++;
        right--;
    }
}

static bool IsPalindrome(string value)
{
    int left = 0, right = value.Length - 1;
    while (left < right)
    {
        if (value[left] != value[right]) return false;
        left++;
        right--;
    }
    return true;
}
```

Complexity: O(n) time; palindrome check uses O(1) auxiliary space. Reversal
uses O(n) space if a new character array must be created from a string.

### 1.2 Find the second-largest distinct element without sorting

Scan once and keep the largest and second-largest distinct values. Return no
value if the input has fewer than two distinct elements.

Example: `[4, 1, 9, 9, 6]` → `6`.

```csharp
static int? SecondLargestDistinct(int[] values)
{
    int? largest = null, second = null;
    foreach (int value in values)
    {
        if (largest is null || value > largest)
        {
            second = largest;
            largest = value;
        }
        else if (value < largest && (second is null || value > second))
        {
            second = value;
        }
    }
    return second;
}
```

Complexity: O(n) time and O(1) extra space.

### 1.3 Print the n-th Fibonacci number, then optimize it

The sequence begins `0, 1, 1, 2, 3, 5, 8...`; each term is the sum of the
previous two. Naive recursion repeats work. An iterative solution avoids both
repeated calculations and a deep call stack.

Example: `F(6) = 8` when `F(0) = 0` and `F(1) = 1`.

```csharp
static long Fibonacci(int n)
{
    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));
    if (n < 2) return n;

    long previousTwo = 0, previousOne = 1;
    for (int i = 2; i <= n; i++)
    {
        long current = previousOne + previousTwo;
        previousTwo = previousOne;
        previousOne = current;
    }
    return previousOne;
}
```

Complexity: naive recursion is approximately O(2^n); memoization is O(n) time
and O(n) space; iteration is O(n) time and O(1) space. `long` eventually
overflows; use `System.Numerics.BigInteger` for large exact results.

### 1.4 Rotate a square matrix 90 degrees clockwise; print matrix patterns

Transpose the square matrix, then reverse each row. Matrix-pattern questions
usually test nested loops and row/column boundaries.

Example: `[[1,2,3],[4,5,6],[7,8,9]]` becomes
`[[7,4,1],[8,5,2],[9,6,3]]`.

```csharp
static void RotateClockwise(int[][] matrix)
{
    int n = matrix.Length; // Assumes an n x n matrix.
    for (int row = 0; row < n; row++)
    {
        for (int column = row + 1; column < n; column++)
        {
            (matrix[row][column], matrix[column][row]) =
                (matrix[column][row], matrix[row][column]);
        }
    }

    foreach (int[] row in matrix)
        Array.Reverse(row);
}
```

Complexity: O(n²) time and O(1) auxiliary space for an already allocated
square matrix.

### 1.5 Two Sum: find indices of two values that add to a target

Keep a dictionary from each seen value to its index. For each value, look up
its complement before storing the current value, so an item cannot pair with
itself unless another matching value occurs at a different index.

Example: `[2, 7, 11, 15]`, target `9` → indices `[0, 1]`.

```csharp
static int[]? TwoSum(int[] values, int target)
{
    var indexByValue = new Dictionary<int, int>();
    for (int i = 0; i < values.Length; i++)
    {
        int needed = target - values[i];
        if (indexByValue.TryGetValue(needed, out int otherIndex))
            return new[] { otherIndex, i };
        indexByValue[values[i]] = i;
    }
    return null;
}
```

Complexity: O(n) average time and O(n) space.

### 1.6 Implement a queue using two stacks

Push enqueued items onto an input stack. When the output stack is empty,
transfer all items from the input stack to the output stack; this reverses
their order. Dequeue from the output stack.

```csharp
class QueueWithStacks<T>
{
    private readonly Stack<T> input = new();
    private readonly Stack<T> output = new();

    public void Enqueue(T item) => input.Push(item);

    public T Dequeue()
    {
        if (output.Count == 0)
        {
            while (input.Count > 0)
                output.Push(input.Pop());
        }

        if (output.Count == 0)
            throw new InvalidOperationException("Queue is empty.");

        return output.Pop();
    }
}
```

Complexity: enqueue O(1); dequeue amortized O(1), with O(n) worst case during
a transfer; O(n) storage.

### 1.7 Find the longest substring without repeating characters

Use a sliding window and store the last index of each character. When a
repeated character is inside the current window, move the window start just
past its previous occurrence.

Example: `"abcabcbb"` → `3` (`"abc"`); `"bbbbb"` → `1`.

```csharp
static int LengthOfLongestSubstring(string value)
{
    var lastIndex = new Dictionary<char, int>();
    int best = 0, start = 0;

    for (int i = 0; i < value.Length; i++)
    {
        if (lastIndex.TryGetValue(value[i], out int previous) && previous >= start)
            start = previous + 1;

        lastIndex[value[i]] = i;
        best = Math.Max(best, i - start + 1);
    }
    return best;
}
```

Complexity: O(n) average time and O(min(n, alphabet size)) space.

### 1.8A Maximum-sum subarray of fixed size k

Sum the first `k` values, then slide the window by adding the incoming value
and subtracting the value that left.

Example: `[2,1,5,1,3,2]`, `k = 3` → `9`, from `[5,1,3]`.

```csharp
static int MaxSumWindow(int[] values, int k)
{
    if (k <= 0 || k > values.Length)
        throw new ArgumentOutOfRangeException(nameof(k));

    int sum = 0;
    for (int i = 0; i < k; i++) sum += values[i];
    int best = sum;

    for (int i = k; i < values.Length; i++)
    {
        sum += values[i] - values[i - k];
        best = Math.Max(best, sum);
    }
    return best;
}
```

Complexity: O(n) time and O(1) extra space.

### 1.8B Maximum-sum subarray of any length (Kadane's algorithm)

At each position, either start a new subarray or extend the previous one.
Initialize from the first element so all-negative arrays work when a
non-empty subarray is required.

Example: `[-2,1,-3,4,-1,2,1,-5,4]` → `6`, from `[4,-1,2,1]`.

```csharp
static int MaxSubarraySum(int[] values)
{
    if (values.Length == 0)
        throw new ArgumentException("Array must not be empty.", nameof(values));

    int current = values[0], best = values[0];
    for (int i = 1; i < values.Length; i++)
    {
        current = Math.Max(values[i], current + values[i]);
        best = Math.Max(best, current);
    }
    return best;
}
```

Complexity: O(n) time and O(1) extra space.

### 1.9A Insert a value at a position in a linked list

Create a node. For insertion at the head, point it to the old head. Otherwise,
walk to the node immediately before the requested position, then update the
two links. Validate the position and handle an empty list.

Example: inserting `8` at index 1 into `3 → 5 → 9` gives
`3 → 8 → 5 → 9`.

Complexity: O(n) to find the position; O(1) pointer updates once there.

### 1.9B Reverse a singly linked list

Track `previous`, `current`, and `next`. Save `current.Next` before changing
the link. At the end, `previous` is the new head.

Example: `1 → 2 → 3 → null` becomes `3 → 2 → 1 → null`.

```csharp
class Node
{
    public int Value;
    public Node? Next;
    public Node(int value) => Value = value;
}

static Node? ReverseList(Node? head)
{
    Node? previous = null;
    Node? current = head;
    while (current is not null)
    {
        Node? next = current.Next;
        current.Next = previous;
        previous = current;
        current = next;
    }
    return previous;
}
```

Complexity: O(n) time and O(1) extra space.

### 1.10A Merge two sorted arrays or linked lists

Use one pointer per input. Append the smaller current item and advance its
pointer. When one input ends, append the remaining items from the other.
For linked lists, rewire nodes if an in-place merge is required.

Example: `[1,3,7]` + `[2,4,6]` → `[1,2,3,4,6,7]`.

Complexity: O(n + m) time; O(n + m) output space for a new array, or O(1)
auxiliary space when rewiring existing linked-list nodes.

### 1.10B Explain merge sort

Divide the collection into halves, recursively sort each half, and merge the
sorted halves. The recurrence `T(n) = 2T(n/2) + O(n)` gives O(n log n).

Example: `[4,1,3,2]` → sort `[4,1]` and `[3,2]`, then merge `[1,4]` and
`[2,3]` to get `[1,2,3,4]`.

Complexity: O(n log n) time in best, average, and worst cases; typically
O(n) auxiliary memory for arrays.

### 1.11 Count ways to climb n stairs using 1 or 2 steps

The last move to stair `n` comes from `n - 1` or `n - 2`, so
`ways(n) = ways(n - 1) + ways(n - 2)`. Use `ways(0) = 1` and `ways(1) = 1`.

Example: `n = 4` → 5 ways: `1+1+1+1`, `1+1+2`, `1+2+1`, `2+1+1`, `2+2`.

```csharp
static long ClimbStairs(int n)
{
    if (n < 0) throw new ArgumentOutOfRangeException(nameof(n));

    long twoBack = 1, oneBack = 1;
    for (int stair = 2; stair <= n; stair++)
    {
        long current = oneBack + twoBack;
        twoBack = oneBack;
        oneBack = current;
    }
    return n == 0 ? 1 : oneBack;
}
```

Complexity: O(n) time and O(1) extra space.

### 1.12A Find a path in a grid / shortest path in an unweighted graph

Use breadth-first search (BFS) with a queue. Mark a node visited when
enqueuing it so it is not queued repeatedly. Store parent pointers if the
actual path must be reconstructed. BFS finds the fewest edges when all edges
have equal cost.

For a grid, enqueue the start and explore valid unvisited neighbors
(up/down/left/right).

Complexity: O(V + E) for a graph; O(rows × columns) time and space for a grid.

### 1.12B Find the shortest path in a weighted graph

For non-negative edge weights, use Dijkstra's algorithm with a min-priority
queue. Track the best distance to each node and relax outgoing edges. BFS is
not sufficient when edge costs differ. For negative edges, consider
Bellman–Ford.

Example: `A→B` costs 2, `A→C` costs 5, and `B→C` costs 1. The shortest
`A→C` path is `A→B→C`, total cost 3.

Complexity: commonly O((V + E) log V) with a binary heap.

## 2. DSA and complexity

### Array vs. linked list

Arrays provide O(1) indexed access and good cache locality; middle insertion
usually shifts elements. Linked lists allow O(1) insertion/deletion when the
relevant node (and predecessor for a singly linked list) is already known,
but searching or indexing takes O(n). In C#, `List<T>` is a resizable array;
`LinkedList<T>` is doubly linked.

Use an array or `List<T>` for indexed data; consider a linked list when
node-based insertions/removals are frequent and the locations are already
known.

### How does a hash map work internally?

A hash function maps a key to a hash code and then to a bucket. Different keys
can map to the same bucket (a collision); implementations handle collisions
with techniques such as chaining or open addressing. As the load factor grows,
the table may resize and rehash entries.

Get/put/remove are typically O(1) on average and can be O(n) in the worst
case.

### Design a hash map from scratch

Use an array of buckets, each containing key-value entries. `Put` finds the
bucket and updates or adds an entry; `Get` searches the bucket; `Remove`
unlinks the matching entry. Resize when the load factor passes a chosen
threshold, then rehash entries. Define key equality, negative-hash handling,
null-key behavior, and concurrency requirements.

Avoid `Math.Abs(hash)` for normalization because the minimum integer value
cannot be represented as a positive `int`. One conceptual bucket calculation
is:

```csharp
int bucket = (key.GetHashCode() & 0x7fffffff) % buckets.Length;
```

This is only the bucket calculation, not a complete production hash map.

### Stack vs. queue, with real uses

A stack is LIFO (last in, first out), useful for undo, call stacks, and
expression parsing. A queue is FIFO (first in, first out), useful for job
scheduling, buffering, and BFS.

Example: push A, push B, pop returns B; enqueue A, enqueue B, dequeue returns
A.

### Detect a cycle in a linked list

Floyd's tortoise-and-hare algorithm advances `slow` by one node and `fast` by
two. If they meet, there is a cycle; if `fast` or `fast.Next` becomes null,
there is none.

```csharp
static bool HasCycle(Node? head)
{
    Node? slow = head, fast = head;
    while (fast?.Next is not null)
    {
        slow = slow!.Next;
        fast = fast.Next.Next;
        if (slow == fast) return true;
    }
    return false;
}
```

Complexity: O(n) time and O(1) extra space.

### Binary search, merge sort, and quicksort complexity

- **Binary search:** requires sorted input; O(log n) time and O(1) extra space
  iteratively.
- **Merge sort:** O(n log n) time in all cases; typically O(n) extra array
  memory.
- **Quicksort:** O(n log n) average time, O(n²) worst case for highly
  unbalanced partitions. Randomized or median-style pivot selection reduces
  the chance of poor partitions. Expected recursion stack is O(log n), worst
  case O(n).

### Recursion vs. iteration; what is stack overflow?

Recursion calls the same function on a smaller input and uses call-stack
frames. Iteration repeats work with loops and often uses less stack memory.
Excessively deep recursion or a missing terminating base case can cause a
stack overflow. Ensure progress toward a valid base case or use iteration for
deep inputs.

### BFS vs. DFS

BFS explores level by level using a queue and finds shortest paths in
unweighted graphs. DFS explores one branch deeply using recursion or an
explicit stack; it is useful for connected components, cycle detection, and
topological sorting with the appropriate directed-graph algorithm. Mark
visited nodes to avoid looping on cycles.

Both commonly run in O(V + E) time with adjacency lists and use O(V) visited
storage.

### What is dynamic programming?

Dynamic programming (DP) stores results for overlapping subproblems and uses
optimal substructure to avoid recomputation. Memoization is top-down recursion
with a cache; tabulation fills a table bottom-up. Define the state,
recurrence, base cases, and iteration order first.

Fibonacci and climbing stairs are simple examples. DP can reduce an
exponential recursive solution to polynomial time.

### Explain pointers; what is the C# equivalent?

A raw pointer stores a memory address. Risks include null pointers, invalid
pointer arithmetic, and dangling references. C# normally uses managed
references, with object lifetime handled by the garbage collector. Raw
pointers are supported only in `unsafe` contexts and should be used only when
necessary.

## 3. C# and .NET

### Value type vs. reference type

Value-type variables hold their value directly (or inline in their containing
object); examples include `int`, `bool`, and `struct`. Reference-type
variables hold a reference to an object; examples include classes, arrays,
and strings. Assignment copies a value-type value, while reference assignment
copies the reference, so two variables can refer to the same object.

Do not simplify this to “all value types are on the stack”; storage depends
on context and runtime implementation.

```csharp
int a = 5;
int b = a;
b = 9; // a remains 5

var x = new List<int> { 1 };
var y = x;
y.Add(2); // x also observes [1, 2]
```

### String vs. StringBuilder

`string` is immutable: operations that appear to modify it create a new
string. `StringBuilder` maintains a mutable buffer and is useful for repeated
concatenation, especially in loops. For a few concatenations, interpolation
or `+` is usually clearer and may be optimized by the compiler.

```csharp
var builder = new System.Text.StringBuilder();
for (int i = 0; i < 1000; i++)
    builder.Append(i).Append(',');
string result = builder.ToString();
```

### Array vs. List<T> vs. Dictionary<TKey, TValue>

An array has a fixed length and indexed access. `List<T>` is a resizable
ordered sequence with indexed access. `Dictionary<TKey, TValue>` maps unique
keys to values and typically provides average O(1) key lookup. Choose
according to whether the need is fixed-size indexed data, a growable
sequence, or key-based lookup.

### Abstract class vs. interface

An abstract class can contain state, constructors, abstract members, and
implemented members; a class can inherit from only one base class. An
interface defines a contract and a type can implement multiple interfaces.
Modern C# interfaces can also provide default implementations. Use an
abstract class for shared base identity/state and an interface for a
capability or contract.

### Delegates, events, and lambdas

A delegate is a type-safe reference to a compatible method or callable. An
event provides a publish/subscribe notification mechanism while controlling
who can raise it. A lambda is concise inline function syntax that can be
assigned to a compatible delegate or expression tree.

```csharp
Action<string> greet = name => Console.WriteLine($"Hello, {name}");
greet("SOTI");

public class Publisher
{
    public event EventHandler? Completed;
    public void Finish() => Completed?.Invoke(this, EventArgs.Empty);
}
```

### What do async and await do?

`async`/`await` makes asynchronous workflows easier to write. When awaiting
incomplete I/O, the method yields control instead of blocking the current
thread while the operation is pending. The compiler implements this with a
state machine. It does not automatically make CPU-bound work faster or
guarantee a new thread.

Prefer async all the way and avoid blocking with `.Result` or `.Wait()`, which
can cause deadlocks in some contexts and waste threads.

```csharp
public static async Task<string> GetTextAsync(HttpClient client, string url)
{
    using var response = await client.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
}
```

### Garbage collection and IDisposable

The .NET garbage collector reclaims memory occupied by unreachable managed
objects. Generations 0, 1, and 2 optimize collection based on object lifetime;
large objects are handled separately by the large object heap. GC does not
guarantee timely release of unmanaged resources such as file handles.
`IDisposable` provides deterministic cleanup; `using` ensures `Dispose` runs
even if an exception occurs.

Use `using` for streams, database connections, and other disposable
resources.

```csharp
using var stream = File.OpenRead(path);
// Work with stream; Dispose runs when the scope exits.
```

### Dependency injection lifetimes

- **Transient:** a new instance is created each time the service is requested.
- **Scoped:** one instance per scope (typically one per ASP.NET Core request).
- **Singleton:** one instance for the service provider's lifetime.

Do not inject a scoped service directly into a singleton; that can
accidentally extend the scoped service's lifetime. Singleton services should
also be safe for concurrent use when accessed by multiple requests.

### MVC vs. Web API

MVC commonly serves server-rendered views such as HTML. Web API endpoints
typically expose data and operations over HTTP, often returning JSON to
web/mobile clients. ASP.NET Core uses a shared framework and controller
capabilities can overlap; the distinction is primarily response style and
API purpose, not a completely separate runtime.

### REST principles and common HTTP status codes

REST emphasizes resources identified by URLs, standard HTTP methods,
stateless requests, and representations. `GET` reads; `POST` commonly
creates or triggers processing; `PUT` replaces; `PATCH` partially updates;
`DELETE` removes.

Common codes: `200 OK`, `201 Created`, `204 No Content`, `400 Bad Request`,
`401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `409 Conflict`, and
`500 Internal Server Error`. `401` means authentication is missing or
invalid; `403` means the server understood the request but refuses it.

## 4. OOP and output-prediction questions

### What are the four pillars of OOP?

- **Encapsulation:** bundles state and behavior and controls access.
- **Abstraction:** exposes essential behavior while hiding implementation
  details.
- **Inheritance:** derives a type from another type to reuse or extend
  behavior.
- **Polymorphism:** lets the same contract invoke different implementations,
  such as a virtual method overridden by derived classes.

Example: `EmailNotification` and `SmsNotification` can implement
`INotification`; calling `Send()` uses the appropriate implementation.

### Overloading vs. overriding

Overloading uses the same method name with different parameter lists and is
selected at compile time. Overriding replaces a virtual/abstract base member
in a derived class and dispatches according to the runtime object type.
Return type alone cannot distinguish overloads. In C#, `new` hides a base
member; it does not override it.

```csharp
class Base
{
    public virtual string Show() => "Base";
}

class Derived : Base
{
    public override string Show() => "Derived";
}

Base item = new Derived();
Console.WriteLine(item.Show()); // Derived
```

### How do you solve output-prediction questions?

Trace the program line by line. Identify static versus instance members,
constructor order, overload selection, virtual dispatch, method hiding (`new`),
and exception flow. Static constructors run at most once per type before the
type is first used, subject to runtime initialization rules.

For a hidden member, the variable's compile-time type determines which member
is called:

```csharp
class A { public void Show() => Console.WriteLine("A"); }
class B : A { public new void Show() => Console.WriteLine("B"); }

A value = new B();
value.Show(); // A: new hides; it does not override.
```

## 5. SQL

### Find the second-highest salary

This query returns the second-highest **distinct** salary, or `NULL` if no
lower salary exists. Clarify whether the question means the second distinct
salary or the second row and define the tie policy.

```sql
SELECT MAX(Salary) AS SecondHighestSalary
FROM Emp
WHERE Salary < (SELECT MAX(Salary) FROM Emp);
```

### Find duplicate names

Group by the columns that define duplication and filter groups with more than
one row. Grouping only by `Name` detects duplicate names, not necessarily
identical full employee rows.

```sql
SELECT Name, COUNT(*) AS Occurrences
FROM Emp
GROUP BY Name
HAVING COUNT(*) > 1;
```

### INNER JOIN vs. LEFT JOIN

`INNER JOIN` returns only rows with matching keys on both sides. `LEFT JOIN`
returns every row from the left table and matching right-side data where
available; unmatched right-side columns are `NULL`.

```sql
SELECT e.Name, d.DepartmentName
FROM Emp e
LEFT JOIN Department d ON d.DepartmentId = e.DepartmentId;
```

### WHERE vs. HAVING

`WHERE` filters individual rows before grouping. `HAVING` filters groups
after `GROUP BY`, commonly using aggregate conditions.

```sql
SELECT DepartmentId, COUNT(*) AS EmployeeCount
FROM Emp
WHERE IsActive = 1
GROUP BY DepartmentId
HAVING COUNT(*) >= 5;
```

### Clustered vs. non-clustered index

A clustered index defines how table rows are organized at the leaf level of
the index; a table can have only one clustered index. A non-clustered index
is a separate structure containing indexed keys and row locators (or the
clustered key). It can speed up reads but costs storage and adds work to
inserts, updates, and deletes. Exact physical behavior depends on the
database engine; this description is for SQL Server.

### SQL interview reminders

Be ready to explain `NULL` handling, `GROUP BY`, aggregate functions, joins,
indexes, and query plans. Use explicit `JOIN` syntax. Clarify what “second
highest” means and avoid `SELECT *` in production queries when only specific
columns are needed.

## 6. Operating systems and networking

### Process vs. thread

A process is an executing program with its own virtual address space and
resources. Threads are execution paths within a process; they share process
memory but have their own stack and execution state. Shared memory requires
synchronization to avoid race conditions.

### What is a deadlock?

A deadlock occurs when tasks wait indefinitely for resources held by one
another. The classic conditions are mutual exclusion, hold-and-wait, no
preemption, and circular wait. Prevention techniques include consistent lock
ordering, avoiding unnecessary nested locks, using timeouts where
appropriate, and reducing lock scope.

### TCP vs. UDP

TCP is connection-oriented and provides ordered, reliable byte-stream
delivery with retransmission and congestion control. UDP sends datagrams
without guaranteeing delivery, order, or duplicate protection. UDP can suit
low-latency applications that handle loss themselves, such as real-time media
or some DNS traffic.

### What happens when you type a URL?

The browser parses the URL, resolves the hostname through DNS (possibly from
caches), establishes a connection, sends an HTTP request, receives a response,
then parses HTML/CSS, fetches dependent resources, executes JavaScript, and
renders the page. HTTP/1.1 and HTTP/2 use TCP; HTTPS typically adds TLS.
HTTP/3 uses QUIC over UDP. Caches, proxies, CDNs, and connection reuse can
change the exact path, and DNS/TCP/TLS steps may be skipped when cached or
reused.

## 7. Puzzles and brain teasers

### Two uneven ropes each burn in 60 minutes. Measure 45 minutes.

Light rope A at both ends and rope B at one end simultaneously. Rope A
finishes in 30 minutes. At that moment, light the other end of rope B. Its
remaining portion now burns from both ends in 15 minutes. Total: 45 minutes.
This works even if the ropes burn unevenly, assuming each rope's total
one-ended burn time is 60 minutes.

### Eight balls; one is heavier. What is the fewest balance-scale weighings?

Two weighings are enough. First weigh three balls against three. If they
balance, the heavier ball is among the remaining two; weigh those against
each other. If the first weighing tips, the heavier ball is in the heavier
group of three; weigh two balls from that group. If they balance, the third
is heavier; otherwise, the heavier side identifies it.

### Three switches outside, one bulb inside; enter the room once

Turn switch 1 on for several minutes, then turn it off. Turn switch 2 on and
enter the room. If the bulb is lit, switch 2 controls it. If it is off but
warm, switch 1 controls it. If it is off and cold, switch 3 controls it. This
assumes an incandescent bulb that becomes detectably warm; it may not work
with some LED bulbs.

### 100 doors toggled on 100 passes: which remain open?

Door `n` is toggled once for each divisor of `n`. Divisors usually come in
pairs, so most doors are toggled an even number of times and end closed.
Perfect squares have one unpaired divisor (their square root), so they are
toggled an odd number of times. Doors 1, 4, 9, 16, 25, 36, 49, 64, 81, and
100 remain open.

### 25 horses, five per race: find the fastest three in seven races

Race five groups of five, then race the five group winners (six races total).
If the winner order is A1, B1, C1, D1, E1, eliminate horses that cannot be in
the overall top three. The remaining candidates are A1, A2, A3, B1, B2, and
C1. A1 is already fastest; race A2, A3, B1, B2, and C1 in race seven. The
first two in that race, together with A1, are the overall top three. This
assumes only race order is available and there are no ties.

### Attach a candle to a wall using a box of tacks and matches

Empty the tack box, pin the box to the wall, place the candle in it, and light
it. The box becomes the candle holder/platform. This puzzle tests whether you
can see the container as part of the solution, rather than only as packaging.

## 8. HR and managerial questions

> These are sample structures, not claims to repeat verbatim. Personalize
> them to your actual experience, motivation, availability, and compensation
> expectations. Do not claim facts you cannot support.

### Tell me about yourself

Suggested structure: summarize your experience and technical focus, name one
or two relevant responsibilities or outcomes, then explain what kind of role
you are seeking. The supplied PDF's sample describes a senior engineer
experienced with C#/.NET, microservices, SQL, AWS, integrations, CI/CD, and
production support; replace those details with your own.

### Why do you want to join SOTI?

Connect your experience to SOTI's enterprise mobility and device-management
products. Explain why reliability, security, and scale interest you, then
describe how your skills could contribute. Ask about the team's actual
technical challenges rather than relying on generic claims.

### What does SOTI do? What is MobiControl?

A concise answer: SOTI provides enterprise mobility management solutions.
SOTI MobiControl helps organizations manage, secure, and support mobile and
other endpoint devices across their lifecycle. Verify current product wording
and company information using SOTI's official sources before the interview.

### Will you relocate to the SOTI office?

Answer honestly. If flexible, clarify the role's location and office
expectations (including in-office days) before confirming relocation. If
relocation is not possible, say so clearly and ask whether the team supports
your location or hybrid work.

### What is your salary expectation?

Base the answer on the role's scope, experience, and the overall compensation
structure. The supplied PDF includes a sample target of ₹37 LPA; use that
number only if it still matches your current expectation. State whether you
are flexible and avoid presenting a number as non-negotiable unless it is.

### Describe a leadership or teamwork task

Use STAR: **Situation** (context), **Task** (your responsibility), **Action**
(what you personally did), and **Result** (measurable outcome). Choose a real
example and quantify impact only when you can substantiate it.

### Walk me through your project and your role

Explain the product and its users, the system architecture at a high level,
your individual responsibilities, and one important technical decision or
result. Be ready for follow-ups about architecture, data ownership, failure
handling, observability, testing, deployment rollback, and trade-offs.
Separate your own contribution from the team's delivery.

## 9. Final revision checklist

- [ ] Solve 20–30 easy/medium problems: arrays, strings, sliding window,
  linked lists, basic DP, BFS/DFS.
- [ ] Practice a timed three-hour session with three problems, using one
  language and no hints.
- [ ] Revisit Fibonacci, climbing stairs, subsets, and combinations that sum
  to a target.
- [ ] Implement a hash map, queue using two stacks, and linked-list reversal
  from memory.
- [ ] Revise OOP, C#/.NET fundamentals, async/await, DI lifetimes, GC, and
  `IDisposable`.
- [ ] Write SQL by hand: second-highest salary, duplicates, joins,
  `WHERE`/`HAVING`, and `GROUP BY`.
- [ ] Practice explaining the eight-ball, ropes, switches, 100 doors, horses,
  and candle puzzles aloud.
- [ ] Research SOTI and MobiControl; prepare concise, truthful HR answers.
- [ ] For every coding problem, state assumptions, edge cases, time
  complexity, and space complexity.

## Source and accuracy note

This guide is based on the user-provided *SOTI Interview Questions Answers
Explained* PDF. It preserves the practice topics and adds structure for
review. Some explanations highlight important assumptions, such as the bulb
type in the switch puzzle, the tie policy for SQL ranking, and the distinction
between C# method hiding and overriding. The PDF describes practice topics,
not a confirmed or leaked interview paper.
