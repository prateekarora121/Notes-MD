# SOTI

please do give this repo a STAR⭐ GUYSS!!! 

## Interview preparation

[SOTI Interview Questions and Answers](SOTI-Interview-Questions-and-Answers.md) — coding, DSA, C#/.NET, OOP, SQL, operating systems, networking, puzzles, and HR.

### Easy Problems

1. Get a string as input, check its palindrome or not, without using string handling functions.

[Python](Easy/palindrome.py) | [C#](Easy/Palindrome.cs)

2. Given a string containing just the characters '(' and ')', return the length of the longest valid (well-formed) parentheses substring

[Python](Easy/validParanthesis.py) | [C#](Easy/LongestValidParentheses.cs)

3. Given a number N. Find the minimum number of squares of any number that sums equal to N. For Example: If N = 100, N can be expressed as (10*10) and also as (5*5 + 5*5 + 5*5 + 5*5) but the output will be 1 as the minimum number of square is 1, i.e (10*10).

[Python](Easy/minSquares.py) | [C#](Easy/MinSquares.cs)

4. after creating list of numbers, remove all duplicates and reverse the list

[Python](Easy/removeDup.py) | [C#](Easy/RemoveDuplicates.cs)

5. Joseph is learning digital logic subject which will be for his next semester. He usually tries to solve unit assignment problems before the lecture. Today he got one tricky question. The problem statement is “A positive integer has been given as an input. Convert decimal value to binary representation. Toggle all bits of it after the most significant bit including the most significant bit. Print the positive integer value after toggling all bits”.

Constrains-1<=N<=100

[Python](Easy/joseph.py) | [C#](Easy/Joseph.cs)

### Medium Problems

1. A valid IP address consists of exactly four integers separated by single dots. Each integer is between 0 and 255 (inclusive) and cannot have leading zeros.
 For example, "0.1.2.201" and "192.168.1.1" are valid IP addresses, but "0.011.255.245", "192.168.1.312" and "192.168@1.1" are invalid IP addresses.
Given a string s containing only digits, return all possible valid IP addresses that can be formed by inserting dots into s. You are not allowed to reorder or remove any digits in s. You may return the valid IP addresses in any order.

Example 1:

Input: s = "25525511135"
Output: ["255.255.11.135","255.255.111.35"]
Example 2:

Input: s = "0000"
Output: ["0.0.0.0"]
Example 3:

Input: s = "101023"
Output: ["1.0.10.23","1.0.102.3","10.1.0.23","10.10.2.3","101.0.2.3"]

[Python](Medium/validIP.py) | [C#](Medium/RestoreIpAddresses.cs)

2. The count-and-say sequence is a sequence of digit strings defined by the recursive formula:
countAndSay(1) = "1"
countAndSay(n) is the run-length encoding of countAndSay(n - 1).
Run-length encoding (RLE) is a string compression method that works by replacing consecutive identical characters (repeated 2 or more times) with the concatenation of the character and the number marking the count of the characters (length of the run). For example, to compress the string "3322251" we replace "33" with "23", replace "222" with "32", replace "5" with "15" and replace "1" with "11". Thus the compressed string becomes "23321511".

Given a positive integer n, return the nth element of the count-and-say sequence.

 

Example 1:

Input: n = 4

Output: "1211"


Example 2:

Input: n = 1

Output: "1"

[Python](Medium/count_and_say.py) | [C#](Medium/CountAndSay.cs)

### Hard Problems

1. Given a password, return the minimum edits needed to make it 6-20
   characters long, contain a lowercase letter, an uppercase letter, and
   a digit, and contain no three identical consecutive characters.

[Python](Hard/strongPass.py) | [C#](Hard/StrongPasswordChecker.cs)

2. Given a string containing just the characters '(' and ')', return the
   length of the longest valid (well-formed) parentheses substring.

[Original](Hard/Longest%20Valid%20Paranthesis) | [C#](Hard/LongestValidParentheses.cs)

### Build the C# solutions

The C# files are in one class-library project and expose a solution class
for each problem. From this folder, run:

```text
dotnet build SOTI.CSharp.csproj
```
