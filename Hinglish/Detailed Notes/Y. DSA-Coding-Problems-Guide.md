# DSA & Coding Problems Guide — C# Solutions 🆕 LATEST

> **Added:** 2026-11-11  
> **Purpose:** Section 18 missing parts from Interview Prep Master — C# algorithm implementations, BFS, topological sort, LRU cache, puzzle problems.

---

## Table of Contents

1. [Algorithm Interview Approach](#1-algorithm-interview-approach)
2. [Core C# Algorithms](#2-core-c-algorithms)
3. [BFS — Open the Lock Problem](#3-bfs--open-the-lock-problem)
4. [Topological Sort — Course Schedule II](#4-topological-sort--course-schedule-ii)
5. [LRU Cache Implementation](#5-lru-cache-implementation)
6. [Puzzle Problems](#6-puzzle-problems)
7. [Quick Drills — Complexity Cheat Sheet](#7-quick-drills--complexity-cheat-sheet)
8. [Interview Tips](#8-interview-tips)

---

## 1. Algorithm Interview Approach

### The UPER Framework

**U — Understand**
- Restate problem in your own words
- Ask about input constraints (size, range, type)
- Ask about edge cases (empty, null, single item, duplicates)
- Confirm expected output format

**P — Plan**
- Discuss brute-force approach first (shows you can solve it)
- Choose appropriate data structure
- Explain chosen algorithm
- State time/space complexity

**E — Execute**
- Write clean code with clear variable names
- Handle edge cases explicitly
- Add comments for complex logic

**R — Review**
- Walk through code with example input
- Check edge cases
- Confirm complexity analysis

---

### Common Pitfalls to Avoid

| Pitfall | Explanation | Fix |
|---|---|---|
| **Sorted assumption** | Input may not be sorted | Ask or sort explicitly |
| **Duplicate handling** | Duplicates allowed? Distinct required? | Clarify requirements |
| **Empty input** | `null`, empty array, empty string | Guard clause at start |
| **Integer overflow** | Large numbers, multiplication | Use `long` or check bounds |
| **Off-by-one** | Loop boundaries, array indices | Test with examples |
| **Stable ordering** | Does order matter in result? | Use stable sort if needed |

---

## 2. Core C# Algorithms

### Problem 1: Palindrome Check

**Problem:** Check if a string is a palindrome (ignoring non-alphanumeric characters and case).

```csharp
public static bool IsPalindrome(string value)
{
    if (value is null) throw new ArgumentNullException(nameof(value));
    
    int left = 0, right = value.Length - 1;
    
    while (left < right)
    {
        // Skip non-alphanumeric from left
        while (left < right && !char.IsLetterOrDigit(value[left]))
            left++;
        
        // Skip non-alphanumeric from right
        while (left < right && !char.IsLetterOrDigit(value[right]))
            right--;
        
        // Compare characters (case-insensitive)
        if (char.ToLowerInvariant(value[left]) != char.ToLowerInvariant(value[right]))
            return false;
        
        left++;
        right--;
    }
    
    return true;
}

// Tests:
IsPalindrome("A man, a plan, a canal: Panama") // true
IsPalindrome("race a car") // false
IsPalindrome("") // true
IsPalindrome("a") // true
```

**Complexity:** O(n) time, O(1) space  
**Key insight:** Two pointers from both ends, skip non-alphanumeric.

---

### Problem 2: Two Sum

**Problem:** Given an array of integers and a target, return indices of two numbers that add up to target.

```csharp
public static int[] TwoSum(int[] nums, int target)
{
    if (nums == null || nums.Length < 2)
        throw new ArgumentException("Invalid input");
    
    var seen = new Dictionary<int, int>(); // value -> index
    
    for (int i = 0; i < nums.Length; i++)
    {
        int complement = target - nums[i];
        
        if (seen.ContainsKey(complement))
            return new[] { seen[complement], i };
        
        // Store current number and its index
        seen[nums[i]] = i;
    }
    
    throw new InvalidOperationException("No solution found");
}

// Tests:
TwoSum(new[] { 2, 7, 11, 15 }, 9) // [0, 1]
TwoSum(new[] { 3, 2, 4 }, 6) // [1, 2]
TwoSum(new[] { 3, 3 }, 6) // [0, 1]
```

**Complexity:** O(n) time, O(n) space  
**Key insight:** Use hash map to store complements; single pass.

---

### Problem 3: Fibonacci (Efficient)

**Problem:** Return nth Fibonacci number.

```csharp
public static long Fibonacci(int n)
{
    if (n < 0) throw new ArgumentException("n must be non-negative");
    if (n <= 1) return n;
    
    long prev = 0, curr = 1;
    
    for (int i = 2; i <= n; i++)
    {
        long next = prev + curr;
        prev = curr;
        curr = next;
    }
    
    return curr;
}

// Recursive (exponential — avoid):
public static long FibonacciRecursive(int n)
{
    if (n <= 1) return n;
    return FibonacciRecursive(n - 1) + FibonacciRecursive(n - 2); // O(2^n) — BAD
}

// Tests:
Fibonacci(0) // 0
Fibonacci(1) // 1
Fibonacci(10) // 55
```

**Complexity:** O(n) time, O(1) space (iterative)  
**Key insight:** Keep only last two values; no need for array.

---

### Problem 4: Reverse String

**Problem:** Reverse a character array in-place.

```csharp
public static void ReverseString(char[] s)
{
    if (s == null) return;
    
    int left = 0, right = s.Length - 1;
    
    while (left < right)
    {
        // Swap
        (s[left], s[right]) = (s[right], s[left]);
        left++;
        right--;
    }
}

// Tests:
var arr = new[] { 'h', 'e', 'l', 'l', 'o' };
ReverseString(arr);
// arr is now ['o', 'l', 'l', 'e', 'h']
```

**Complexity:** O(n) time, O(1) space  
**Key insight:** Two pointers swap from ends.

---

### Problem 5: Maximum Subarray (Kadane's Algorithm)

**Problem:** Find contiguous subarray with largest sum.

```csharp
public static int MaxSubArray(int[] nums)
{
    if (nums == null || nums.Length == 0)
        throw new ArgumentException("Array cannot be empty");
    
    int maxSoFar = nums[0];
    int maxEndingHere = nums[0];
    
    for (int i = 1; i < nums.Length; i++)
    {
        // Either extend existing subarray or start new one
        maxEndingHere = Math.Max(nums[i], maxEndingHere + nums[i]);
        maxSoFar = Math.Max(maxSoFar, maxEndingHere);
    }
    
    return maxSoFar;
}

// Tests:
MaxSubArray(new[] { -2, 1, -3, 4, -1, 2, 1, -5, 4 }) // 6 ([4,-1,2,1])
MaxSubArray(new[] { 1 }) // 1
MaxSubArray(new[] { 5, 4, -1, 7, 8 }) // 23
```

**Complexity:** O(n) time, O(1) space  
**Key insight:** At each position, decide whether to extend current sum or start fresh.

---

## 3. BFS — Open the Lock Problem

### Problem Statement

You have a lock with 4 circular wheels, each with digits 0-9. Start at "0000". You can:
- Rotate any wheel up or down by 1 (9 → 0, 0 → 9 wrap around)
- Some combinations are "deadends" (can't pass through them)

Find minimum number of turns to reach target, or -1 if impossible.

**Example:**
```
target = "0202"
deadends = ["0201", "0101", "0102", "1212", "2002"]
Output: 6
Path: "0000" → "1000" → "1100" → "1200" → "1201" → "1202" → "0202"
```

---

### Solution — BFS (Shortest Path in Unweighted Graph)

```csharp
public class Solution
{
    public int OpenLock(string[] deadends, string target)
    {
        var dead = new HashSet<string>(deadends);
        
        // Edge case: start is blocked
        if (dead.Contains("0000")) return -1;
        if (target == "0000") return 0;
        
        var queue = new Queue<string>();
        var visited = new HashSet<string>();
        
        queue.Enqueue("0000");
        visited.Add("0000");
        int turns = 0;
        
        while (queue.Count > 0)
        {
            int size = queue.Count;
            
            // Process all nodes at current level
            for (int i = 0; i < size; i++)
            {
                string current = queue.Dequeue();
                
                // Try all 8 possible moves (4 wheels × 2 directions)
                foreach (string neighbor in GetNeighbors(current))
                {
                    if (neighbor == target)
                        return turns + 1;
                    
                    if (!visited.Contains(neighbor) && !dead.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            turns++;
        }
        
        return -1; // Target unreachable
    }
    
    private IEnumerable<string> GetNeighbors(string state)
    {
        var chars = state.ToCharArray();
        
        for (int i = 0; i < 4; i++)
        {
            char original = chars[i];
            
            // Turn up
            chars[i] = original == '9' ? '0' : (char)(original + 1);
            yield return new string(chars);
            
            // Turn down
            chars[i] = original == '0' ? '9' : (char)(original - 1);
            yield return new string(chars);
            
            // Restore
            chars[i] = original;
        }
    }
}

// Tests:
var solution = new Solution();
solution.OpenLock(new[] { "0201", "0101", "0102", "1212", "2002" }, "0202") // 6
solution.OpenLock(new[] { "8888" }, "0009") // 1
solution.OpenLock(new[] { "0000" }, "8888") // -1
```

**Complexity:** O(10^4) time (10,000 possible states), O(10^4) space  
**Key insight:** Each state is a graph node; BFS guarantees shortest path in unweighted graph.

---

### Why BFS, Not DFS?

| Algorithm | Guarantee | Use Case |
|---|---|---|
| **BFS** | Shortest path (unweighted graph) | Open the Lock, level-order traversal |
| **DFS** | Explores deeply first (no shortest path guarantee) | Connected components, topological sort |

**BFS** explores level-by-level → first time target reached = shortest path.

---

## 4. Topological Sort — Course Schedule II

### Problem Statement

There are `numCourses` courses labeled `0` to `numCourses-1`. Some courses have prerequisites.

Given prerequisites array where `[a, b]` means "to take course `a`, you must first take course `b`", return the ordering of courses to finish all. If impossible (cycle), return empty array.

**Example:**
```
numCourses = 4
prerequisites = [[1,0], [2,0], [3,1], [3,2]]
Output: [0, 1, 2, 3] or [0, 2, 1, 3] (both valid)
```

---

### Solution — Kahn's Algorithm (BFS Topological Sort)

```csharp
public class Solution
{
    public int[] FindOrder(int numCourses, int[][] prerequisites)
    {
        // Build adjacency list and in-degree count
        var graph = new List<int>[numCourses];
        var inDegree = new int[numCourses];
        
        for (int i = 0; i < numCourses; i++)
            graph[i] = new List<int>();
        
        foreach (var prereq in prerequisites)
        {
            int course = prereq[0];
            int prerequisite = prereq[1];
            
            graph[prerequisite].Add(course); // prerequisite → course edge
            inDegree[course]++;
        }
        
        // Queue of courses with no prerequisites
        var queue = new Queue<int>();
        for (int i = 0; i < numCourses; i++)
        {
            if (inDegree[i] == 0)
                queue.Enqueue(i);
        }
        
        var result = new List<int>();
        
        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            result.Add(current);
            
            // Process outgoing edges
            foreach (int neighbor in graph[current])
            {
                inDegree[neighbor]--;
                
                if (inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }
        
        // If not all courses processed → cycle exists
        return result.Count == numCourses ? result.ToArray() : Array.Empty<int>();
    }
}

// Tests:
var solution = new Solution();
solution.FindOrder(4, new[] { new[] { 1, 0 }, new[] { 2, 0 }, new[] { 3, 1 }, new[] { 3, 2 } })
// [0, 1, 2, 3] or [0, 2, 1, 3]

solution.FindOrder(2, new[] { new[] { 1, 0 }, new[] { 0, 1 } })
// [] (cycle detected)
```

**Complexity:** O(V + E) time, O(V + E) space  
**Key insight:** Kahn's algorithm — process nodes with in-degree 0, decrement neighbors' in-degree.

---

### Topological Sort — DFS Alternative

```csharp
public class SolutionDFS
{
    public int[] FindOrder(int numCourses, int[][] prerequisites)
    {
        var graph = new List<int>[numCourses];
        for (int i = 0; i < numCourses; i++)
            graph[i] = new List<int>();
        
        foreach (var prereq in prerequisites)
            graph[prereq[1]].Add(prereq[0]);
        
        var visited = new int[numCourses]; // 0=unvisited, 1=visiting, 2=visited
        var result = new Stack<int>();
        
        for (int i = 0; i < numCourses; i++)
        {
            if (visited[i] == 0)
            {
                if (HasCycle(i, graph, visited, result))
                    return Array.Empty<int>();
            }
        }
        
        return result.ToArray();
    }
    
    private bool HasCycle(int node, List<int>[] graph, int[] visited, Stack<int> result)
    {
        if (visited[node] == 1) return true; // Back edge → cycle
        if (visited[node] == 2) return false; // Already processed
        
        visited[node] = 1; // Mark as visiting
        
        foreach (int neighbor in graph[node])
        {
            if (HasCycle(neighbor, graph, visited, result))
                return true;
        }
        
        visited[node] = 2; // Mark as visited
        result.Push(node); // Post-order → topological order reversed
        
        return false;
    }
}
```

**Complexity:** O(V + E) time, O(V + E) space  
**Key insight:** DFS post-order gives reverse topological order.

---

## 5. LRU Cache Implementation

### Problem Statement

Design a data structure for Least Recently Used (LRU) cache with:
- `Get(key)` — return value if exists, else -1 (mark as recently used)
- `Put(key, value)` — insert/update (mark as recently used); if capacity exceeded, evict least recently used

Both operations must be **O(1)**.

---

### Solution — Dictionary + Doubly Linked List

```csharp
public class LRUCache
{
    private class Node
    {
        public int Key;
        public int Value;
        public Node Prev;
        public Node Next;
    }
    
    private readonly int _capacity;
    private readonly Dictionary<int, Node> _cache; // key -> node
    private readonly Node _head; // Dummy head (most recent)
    private readonly Node _tail; // Dummy tail (least recent)
    
    public LRUCache(int capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<int, Node>(capacity);
        
        // Dummy head and tail to simplify edge cases
        _head = new Node();
        _tail = new Node();
        _head.Next = _tail;
        _tail.Prev = _head;
    }
    
    public int Get(int key)
    {
        if (!_cache.ContainsKey(key))
            return -1;
        
        var node = _cache[key];
        MoveToHead(node); // Mark as recently used
        return node.Value;
    }
    
    public void Put(int key, int value)
    {
        if (_cache.ContainsKey(key))
        {
            // Update existing
            var node = _cache[key];
            node.Value = value;
            MoveToHead(node);
        }
        else
        {
            // Insert new
            var newNode = new Node { Key = key, Value = value };
            _cache[key] = newNode;
            AddToHead(newNode);
            
            // Evict if over capacity
            if (_cache.Count > _capacity)
            {
                var lru = _tail.Prev; // Least recently used
                RemoveNode(lru);
                _cache.Remove(lru.Key);
            }
        }
    }
    
    private void AddToHead(Node node)
    {
        node.Next = _head.Next;
        node.Prev = _head;
        _head.Next.Prev = node;
        _head.Next = node;
    }
    
    private void RemoveNode(Node node)
    {
        node.Prev.Next = node.Next;
        node.Next.Prev = node.Prev;
    }
    
    private void MoveToHead(Node node)
    {
        RemoveNode(node);
        AddToHead(node);
    }
}

// Tests:
var cache = new LRUCache(2);
cache.Put(1, 1);
cache.Put(2, 2);
cache.Get(1);       // returns 1
cache.Put(3, 3);    // evicts key 2
cache.Get(2);       // returns -1 (not found)
cache.Put(4, 4);    // evicts key 1
cache.Get(1);       // returns -1 (not found)
cache.Get(3);       // returns 3
cache.Get(4);       // returns 4
```

**Complexity:** O(1) for both Get and Put  
**Key insight:** Dictionary for O(1) lookup; doubly linked list for O(1) eviction (head = recent, tail = LRU).

---

### Alternative — LinkedList<T> (Simpler but Less Efficient)

```csharp
public class LRUCacheSimple
{
    private readonly int _capacity;
    private readonly Dictionary<int, LinkedListNode<(int key, int value)>> _cache;
    private readonly LinkedList<(int key, int value)> _list;
    
    public LRUCacheSimple(int capacity)
    {
        _capacity = capacity;
        _cache = new Dictionary<int, LinkedListNode<(int, int)>>(capacity);
        _list = new LinkedList<(int, int)>();
    }
    
    public int Get(int key)
    {
        if (!_cache.ContainsKey(key))
            return -1;
        
        var node = _cache[key];
        _list.Remove(node);
        _list.AddFirst(node);
        return node.Value.value;
    }
    
    public void Put(int key, int value)
    {
        if (_cache.ContainsKey(key))
        {
            var node = _cache[key];
            _list.Remove(node);
        }
        else if (_cache.Count >= _capacity)
        {
            var lru = _list.Last;
            _cache.Remove(lru.Value.key);
            _list.RemoveLast();
        }
        
        var newNode = _list.AddFirst((key, value));
        _cache[key] = newNode;
    }
}
```

**Trade-off:** Simpler code, but still O(1) both operations.

---

## 6. Puzzle Problems

### Puzzle 1: Two Ropes — 45 Minutes

**Problem:** You have two ropes that each take exactly 60 minutes to burn completely. The ropes burn unevenly (not uniform rate). How do you measure exactly 45 minutes?

**Solution:**
1. Light Rope A from both ends, Rope B from one end
2. Rope A will burn out in 30 minutes (both ends meet in middle)
3. When Rope A finishes, light the other end of Rope B
4. Rope B now has 30 minutes of burn left, but burning from both ends → finishes in 15 minutes
5. Total: 30 + 15 = **45 minutes**

**Key insight:** Burning from both ends halves the time.

---

### Puzzle 2: 8 Balls — Find the Heavier One

**Problem:** You have 8 identical-looking balls. One is slightly heavier. You have a balance scale. Find the heavier ball in minimum weighings.

**Solution (2 weighings):**

**Weighing 1:** Divide into 3 groups: `{1,2,3}`, `{4,5,6}`, `{7,8}`
- Weigh `{1,2,3}` vs `{4,5,6}`

**Case A — Left heavier:**
- Heavy ball is in `{1,2,3}`
- **Weighing 2:** Weigh `1` vs `2`
  - If equal → `3` is heavy
  - Else → heavier side is the answer

**Case B — Right heavier:**
- Heavy ball is in `{4,5,6}` (same logic as Case A)

**Case C — Balance:**
- Heavy ball is in `{7,8}`
- **Weighing 2:** Weigh `7` vs `8` → heavier is the answer

**Answer: 2 weighings minimum**

---

### Puzzle 3: 25 Horses — Find Top 3

**Problem:** 25 horses, race track with 5 lanes. No stopwatch. Find the top 3 fastest horses in minimum races.

**Solution (7 races):**

**Races 1-5:** Divide into 5 groups, race each group. Call winners A1, B1, C1, D1, E1.

**Race 6:** Race the 5 winners (`A1 vs B1 vs C1 vs D1 vs E1`). Assume result: `A1 > B1 > C1 > D1 > E1`.

**Analysis after Race 6:**
- **A1** is overall fastest (confirmed top 1)
- **A2, A3** (from A's group) might be 2nd or 3rd
- **B1, B2** (from B's group) might be 2nd or 3rd
- **C1** (from C's group) might be 3rd
- **D1, D2, D3, E1, E2, E3, C2, C3** cannot be top 3

**Race 7:** Race `{A2, A3, B1, B2, C1}` (5 candidates for 2nd and 3rd place).
- Top 2 from this race are overall 2nd and 3rd.

**Answer: 7 races**

**Key insight:** Eliminate groups whose best performers are slower than the 3rd best known horse.

---

## 7. Quick Drills — Complexity Cheat Sheet

| Problem | Approach | Time | Space |
|---|---|---|---|
| **Reverse string** | Two pointers | O(n) | O(1) |
| **Palindrome** | Two pointers | O(n) | O(1) |
| **Two Sum** | Hash map | O(n) | O(n) |
| **Fibonacci** | Iterative (rolling 2 vars) | O(n) | O(1) |
| **Max subarray** | Kadane's algorithm | O(n) | O(1) |
| **Merge sorted arrays** | Two pointers | O(m+n) | O(1) extra |
| **Binary search** | Divide & conquer | O(log n) | O(1) |
| **Climbing stairs** | DP (`ways[n] = ways[n-1] + ways[n-2]`) | O(n) | O(1) |
| **BFS (shortest path)** | Queue | O(V+E) | O(V) |
| **DFS (graph traversal)** | Stack/recursion | O(V+E) | O(V) |
| **Topological sort** | Kahn's (BFS) or DFS | O(V+E) | O(V+E) |
| **LRU Cache** | Dict + Doubly linked list | O(1) get/put | O(capacity) |
| **Dijkstra** | Priority queue (non-negative weights) | O((V+E) log V) | O(V) |

---

## 8. Interview Tips

### Before You Code

1. **Restate the problem** — "So we need to find..."
2. **Ask clarifying questions:**
   - Input size / constraints?
   - Edge cases (null, empty, single element, duplicates)?
   - Output format?
3. **Discuss brute-force** — Shows you can solve it, even if slow
4. **Optimize** — Better data structure? Different algorithm?
5. **State complexity** — "This will be O(n) time, O(1) space"

### While Coding

- **Clear variable names** — `left`, `right`, not `i`, `j`
- **Handle edge cases explicitly** — Guard clauses at top
- **Comment complex logic** — Especially bit manipulation, tricky conditions
- **Compile in your head** — Catch syntax errors before running

### After Coding

- **Walk through example** — Trace code with input
- **Check edge cases:**
  - Empty input?
  - Single element?
  - All same elements?
  - Negative numbers?
  - Overflow?
- **Confirm complexity** — Re-check your O(n) claim

### Communication Tips

- **Think aloud** — "I'm choosing a hash map because..."
- **Ask for hints if stuck** — "Should I consider sorting first?"
- **Don't panic** — Breathe, restate problem, try simpler case
- **Iterate** — "Let me optimize this from O(n²) to O(n)"

---

## Summary — Interview Ready Approach

**Formula: Understand → Plan → Execute → Review**

1. **Understand** — Restate, ask constraints, clarify edge cases
2. **Plan** — Brute-force first, then optimize; state complexity
3. **Execute** — Clean code, handle edges, comment tricky parts
4. **Review** — Walk through with example, check edge cases

**Red Flags to Avoid:**
- ❌ Starting to code without discussing approach
- ❌ Assuming input is sorted without asking
- ❌ Ignoring edge cases (null, empty, single element)
- ❌ Not stating complexity before/after coding
- ❌ Silent coding (not explaining thought process)

**Green Flags to Show:**
- ✅ Asking clarifying questions upfront
- ✅ Discussing brute-force before optimal solution
- ✅ Explaining chosen data structure / algorithm
- ✅ Handling edge cases explicitly
- ✅ Stating time/space complexity
- ✅ Walking through code with example

**Good luck!** 🚀 Practice on LeetCode, CodeSignal, or HackerRank regularly.
