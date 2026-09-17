using System;
using System.Collections.Generic;

namespace DataStructuresDemo
{
    // ============================================================
    // 1. HASHMAP (Dictionary in C#)
    // ============================================================
    public static class HashMapDemo
    {
        public static void Run()
        {
            Console.WriteLine("--- HashMap (Dictionary<TKey, TValue>) ---");

            // Creating and populating
            Dictionary<string, int> ages = new Dictionary<string, int>();
            ages.Add("Alice", 30);
            ages["Bob"] = 25;          // indexer also inserts/updates
            ages["Charlie"] = 35;

            // Accessing a value safely
            if (ages.TryGetValue("Alice", out int aliceAge))
                Console.WriteLine($"Alice is {aliceAge} years old");

            // Checking existence
            Console.WriteLine($"Contains 'Bob'? {ages.ContainsKey("Bob")}");

            // Updating a value
            ages["Bob"] = 26;

            // Removing a key
            ages.Remove("Charlie");

            // Traversing (key-value pairs)
            foreach (KeyValuePair<string, int> entry in ages)
                Console.WriteLine($"{entry.Key} -> {entry.Value}");

            // Traversing keys / values separately
            foreach (string key in ages.Keys) Console.WriteLine($"Key: {key}");
            foreach (int value in ages.Values) Console.WriteLine($"Value: {value}");

            Console.WriteLine();
        }
    }

    // Custom HashMap implementation (chaining for collision handling)
    // Common interview ask: "implement a hashmap from scratch"
    public class MyHashMap<TKey, TValue>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Next;
            public Node(TKey key, TValue value) { Key = key; Value = value; }
        }

        private readonly Node[] _buckets;
        private readonly int _capacity;

        public MyHashMap(int capacity = 16)
        {
            _capacity = capacity;
            _buckets = new Node[capacity];
        }

        private int GetBucketIndex(TKey key)
        {
            int hash = key.GetHashCode();
            return Math.Abs(hash) % _capacity;
        }

        public void Put(TKey key, TValue value)
        {
            int index = GetBucketIndex(key);
            Node current = _buckets[index];

            // Update if key already exists
            while (current != null)
            {
                if (current.Key.Equals(key))
                {
                    current.Value = value;
                    return;
                }
                current = current.Next;
            }

            // Insert new node at head of the bucket's chain
            Node newNode = new Node(key, value) { Next = _buckets[index] };
            _buckets[index] = newNode;
        }

        public bool TryGet(TKey key, out TValue value)
        {
            int index = GetBucketIndex(key);
            Node current = _buckets[index];
            while (current != null)
            {
                if (current.Key.Equals(key))
                {
                    value = current.Value;
                    return true;
                }
                current = current.Next;
            }
            value = default;
            return false;
        }

        public bool Remove(TKey key)
        {
            int index = GetBucketIndex(key);
            Node current = _buckets[index];
            Node previous = null;

            while (current != null)
            {
                if (current.Key.Equals(key))
                {
                    if (previous == null) _buckets[index] = current.Next;
                    else previous.Next = current.Next;
                    return true;
                }
                previous = current;
                current = current.Next;
            }
            return false;
        }
    }

    // ============================================================
    // 2. LINKED LIST (singly linked, custom implementation)
    // ============================================================
    public class LinkedListNode<T>
    {
        public T Data;
        public LinkedListNode<T> Next;
        public LinkedListNode(T data) { Data = data; }
    }

    public class MyLinkedList<T>
    {
        private LinkedListNode<T> _head;
        private LinkedListNode<T> _tail;
        public int Count { get; private set; }

        // Insert at the end
        public void AddLast(T value)
        {
            var node = new LinkedListNode<T>(value);
            if (_head == null)
            {
                _head = node;
                _tail = node;
            }
            else
            {
                _tail.Next = node;
                _tail = node;
            }
            Count++;
        }

        // Insert at the beginning
        public void AddFirst(T value)
        {
            var node = new LinkedListNode<T>(value) { Next = _head };
            _head = node;
            if (_tail == null) _tail = node;
            Count++;
        }

        // Delete the first node matching value
        public bool Remove(T value)
        {
            LinkedListNode<T> current = _head;
            LinkedListNode<T> previous = null;

            while (current != null)
            {
                if (EqualityComparer<T>.Default.Equals(current.Data, value))
                {
                    if (previous == null) _head = current.Next;       // removing head
                    else previous.Next = current.Next;

                    if (current == _tail) _tail = previous;           // removing tail
                    Count--;
                    return true;
                }
                previous = current;
                current = current.Next;
            }
            return false;
        }

        // Traverse forward and print every element
        public void Traverse()
        {
            LinkedListNode<T> current = _head;
            while (current != null)
            {
                Console.Write(current.Data + " -> ");
                current = current.Next;
            }
            Console.WriteLine("null");
        }

        // Reverse the linked list in place (classic interview question)
        public void Reverse()
        {
            LinkedListNode<T> prev = null;
            LinkedListNode<T> current = _head;
            _tail = _head;

            while (current != null)
            {
                LinkedListNode<T> nextTemp = current.Next;
                current.Next = prev;
                prev = current;
                current = nextTemp;
            }
            _head = prev;
        }
    }

    public static class LinkedListDemo
    {
        public static void Run()
        {
            Console.WriteLine("--- Custom Singly Linked List ---");
            var list = new MyLinkedList<int>();
            list.AddLast(10);
            list.AddLast(20);
            list.AddLast(30);
            list.AddFirst(5);

            Console.Write("Forward traversal: ");
            list.Traverse(); // 5 -> 10 -> 20 -> 30 -> null

            list.Remove(20);
            Console.Write("After removing 20: ");
            list.Traverse(); // 5 -> 10 -> 30 -> null

            list.Reverse();
            Console.Write("After reversing: ");
            list.Traverse(); // 30 -> 10 -> 5 -> null

            Console.WriteLine("\n--- Built-in LinkedList<T> ---");
            LinkedList<int> builtIn = new LinkedList<int>();
            builtIn.AddLast(1);
            builtIn.AddLast(2);
            builtIn.AddFirst(0);

            foreach (int val in builtIn) // traversal via foreach
                Console.Write(val + " ");
            Console.WriteLine();

            Console.WriteLine();
        }
    }

    // ============================================================
    // 3. QUEUE (FIFO — First In, First Out)
    // ============================================================
    public static class QueueDemo
    {
        public static void Run()
        {
            Console.WriteLine("--- Built-in Queue<T> ---");
            Queue<string> queue = new Queue<string>();
            queue.Enqueue("first");
            queue.Enqueue("second");
            queue.Enqueue("third");

            Console.WriteLine($"Peek: {queue.Peek()}"); // "first" (doesn't remove)
            Console.WriteLine($"Dequeue: {queue.Dequeue()}"); // "first" (removes & returns)

            Console.Write("Remaining queue: ");
            foreach (string item in queue) Console.Write(item + " "); // second third
            Console.WriteLine("\n");
        }
    }

    // Custom Queue implementation using a linked list internally
    public class MyQueue<T>
    {
        private class Node
        {
            public T Value;
            public Node Next;
            public Node(T value) { Value = value; }
        }

        private Node _front;
        private Node _rear;
        public int Count { get; private set; }

        public void Enqueue(T value)
        {
            var node = new Node(value);
            if (_rear == null)
            {
                _front = node;
                _rear = node;
            }
            else
            {
                _rear.Next = node;
                _rear = node;
            }
            Count++;
        }

        public T Dequeue()
        {
            if (_front == null) throw new InvalidOperationException("Queue is empty");
            T value = _front.Value;
            _front = _front.Next;
            if (_front == null) _rear = null;
            Count--;
            return value;
        }

        public T Peek()
        {
            if (_front == null) throw new InvalidOperationException("Queue is empty");
            return _front.Value;
        }

        public bool IsEmpty => _front == null;
    }

    // ============================================================
    // 4. STACK (LIFO — Last In, First Out)
    // ============================================================
    public static class StackDemo
    {
        public static void Run()
        {
            Console.WriteLine("--- Built-in Stack<T> ---");
            Stack<int> stack = new Stack<int>();
            stack.Push(1);
            stack.Push(2);
            stack.Push(3);

            Console.WriteLine($"Peek: {stack.Peek()}"); // 3 (doesn't remove)
            Console.WriteLine($"Pop: {stack.Pop()}");    // 3 (removes & returns)

            Console.Write("Remaining stack (top to bottom): ");
            foreach (int item in stack) Console.Write(item + " "); // 2 1
            Console.WriteLine("\n");
        }
    }

    // Custom Stack implementation using an array (with dynamic resizing)
    public class MyStack<T>
    {
        private T[] _items = new T[4];
        public int Count { get; private set; }

        public void Push(T value)
        {
            if (Count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length * 2); // grow when full
            }
            _items[Count] = value;
            Count++;
        }

        public T Pop()
        {
            if (Count == 0) throw new InvalidOperationException("Stack is empty");
            Count--;
            T value = _items[Count];
            _items[Count] = default; // clear reference
            return value;
        }

        public T Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Stack is empty");
            return _items[Count - 1];
        }

        public bool IsEmpty => Count == 0;
    }

    // ============================================================
    // MAIN — run all demos
    // ============================================================
    public static class Program
    {
        public static void Main(string[] args)
        {
            HashMapDemo.Run();

            // Custom hashmap usage
            Console.WriteLine("--- Custom MyHashMap<TKey, TValue> ---");
            var myMap = new MyHashMap<string, int>();
            myMap.Put("x", 100);
            myMap.Put("y", 200);
            if (myMap.TryGet("x", out int val)) Console.WriteLine($"x -> {val}");
            myMap.Remove("y");
            Console.WriteLine($"y still present? {myMap.TryGet("y", out _)}\n");

            LinkedListDemo.Run();
            QueueDemo.Run();

            // Custom queue usage
            Console.WriteLine("--- Custom MyQueue<T> ---");
            var myQueue = new MyQueue<int>();
            myQueue.Enqueue(1);
            myQueue.Enqueue(2);
            myQueue.Enqueue(3);
            Console.WriteLine($"Dequeue: {myQueue.Dequeue()}"); // 1
            Console.WriteLine($"Peek: {myQueue.Peek()}\n");      // 2

            StackDemo.Run();

            // Custom stack usage
            Console.WriteLine("--- Custom MyStack<T> ---");
            var myStack = new MyStack<int>();
            myStack.Push(1);
            myStack.Push(2);
            myStack.Push(3);
            Console.WriteLine($"Pop: {myStack.Pop()}");  // 3
            Console.WriteLine($"Peek: {myStack.Peek()}"); // 2
        }
    }
}
