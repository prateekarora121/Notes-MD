# MS SQL Server Interview Solutions

Companion to `sql_interview_problems.md`. Run the setup script there first.

---

## Problem 1 — Top 2 Salary Tiers per Department

```sql
WITH r AS (
    SELECT d.DeptName, e.EmpName, e.Salary,
           DENSE_RANK() OVER (PARTITION BY e.DeptID ORDER BY e.Salary DESC) AS rnk
    FROM Employees e
    JOIN Departments d ON d.DeptID = e.DeptID
)
SELECT DeptName, EmpName, Salary
FROM r
WHERE rnk <= 2
ORDER BY DeptName, Salary DESC;
```

**Why:** `DENSE_RANK` handles ties without skipping ranks (`RANK` would skip, `ROW_NUMBER` would drop tied people).

**Expected (9 rows):** Engineering: Alice, Bob · Finance: Leo, Mia, Ned · HR: Judy, Ken · Sales: Frank, Ivan

---

## Problem 2 — Earning More Than the Boss

```sql
SELECT e.EmpName, e.Salary, m.EmpName AS ManagerName, m.Salary AS ManagerSalary
FROM Employees e
JOIN Employees m ON e.ManagerID = m.EmpID
WHERE e.Salary > m.Salary;
```

**Expected:** `Ivan | 92000 | Grace | 90000`

---

## Problem 3 — Above Department Average

```sql
WITH x AS (
    SELECT e.EmpName, d.DeptName, e.Salary,
           AVG(e.Salary) OVER (PARTITION BY e.DeptID) AS DeptAvgSalary
    FROM Employees e
    JOIN Departments d ON d.DeptID = e.DeptID
)
SELECT EmpName, DeptName, Salary, DeptAvgSalary
FROM x
WHERE Salary > DeptAvgSalary;
```

**Why a CTE:** window functions can't be used directly in `WHERE`.

**Expected:** Alice (avg 137000), Bob (137000), Frank (100500), Judy (72500), Leo (120000)

---

## Problem 4 — Understaffed Departments

```sql
SELECT d.DeptName, COUNT(e.EmpID) AS Headcount
FROM Departments d
LEFT JOIN Employees e ON e.DeptID = d.DeptID
GROUP BY d.DeptID, d.DeptName
HAVING COUNT(e.EmpID) < 3;
```

**Gotcha:** `COUNT(e.EmpID)` counts only non-NULL matches, so Legal gets 0. `COUNT(*)` would wrongly give 1.

**Expected:** `HR | 2`, `Legal | 0`

---

## Problem 5 — Median Salary per Department

```sql
SELECT DISTINCT
       d.DeptName,
       PERCENTILE_CONT(0.5) WITHIN GROUP (ORDER BY e.Salary)
           OVER (PARTITION BY d.DeptID) AS MedianSalary
FROM Employees e
JOIN Departments d ON d.DeptID = e.DeptID;
```

**Alternative (no PERCENTILE_CONT):**

```sql
WITH n AS (
    SELECT DeptID, Salary,
           ROW_NUMBER() OVER (PARTITION BY DeptID ORDER BY Salary) AS rn,
           COUNT(*)     OVER (PARTITION BY DeptID)                 AS cnt
    FROM Employees
)
SELECT d.DeptName, AVG(n.Salary) AS MedianSalary
FROM n
JOIN Departments d ON d.DeptID = n.DeptID
WHERE n.rn IN ((n.cnt + 1) / 2, (n.cnt + 2) / 2)
GROUP BY d.DeptName;
```

**Expected:** Engineering 120000 · Sales 91000 · HR 72500 · Finance 110000

---

## Problem 6 — Org Chart (Recursive CTE)

```sql
WITH org AS (
    -- anchor: top of hierarchy
    SELECT EmpID, EmpName, ManagerID,
           0 AS Lvl,
           CAST(EmpName AS VARCHAR(500)) AS Path
    FROM Employees
    WHERE ManagerID IS NULL

    UNION ALL

    -- recursive member
    SELECT e.EmpID, e.EmpName, e.ManagerID,
           o.Lvl + 1,
           CAST(o.Path + ' > ' + e.EmpName AS VARCHAR(500))
    FROM Employees e
    JOIN org o ON e.ManagerID = o.EmpID
)
SELECT EmpID, EmpName, Lvl, Path
FROM org
ORDER BY Path;
-- OPTION (MAXRECURSION 100);  -- default is 100; use 0 for unlimited
```

**Gotcha:** anchor and recursive columns must have identical data types, hence the `CAST` on `Path`.

**Expected (14 rows):**

| EmpName | Lvl | Path |
|---|---|---|
| Alice | 0 | Alice |
| Bob | 1 | Alice > Bob |
| Carol | 2 | Alice > Bob > Carol |
| Eve | 3 | Alice > Bob > Carol > Eve |
| Dave | 2 | Alice > Bob > Dave |
| Frank | 1 | Alice > Frank |
| Grace | 2 | Alice > Frank > Grace |
| Ivan | 3 | Alice > Frank > Grace > Ivan |
| Heidi | 2 | Alice > Frank > Heidi |
| Judy | 1 | Alice > Judy |
| Ken | 2 | Alice > Judy > Ken |
| Leo | 1 | Alice > Leo |
| Mia | 2 | Alice > Leo > Mia |
| Ned | 2 | Alice > Leo > Ned |

*(Row order may differ slightly depending on collation.)*

---

## Problem 7 — Month-over-Month Revenue Growth

```sql
WITH m AS (
    SELECT DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1) AS MonthStart,
           SUM(oi.Quantity * p.Price) AS Revenue
    FROM Orders o
    JOIN OrderItems oi ON oi.OrderID  = o.OrderID
    JOIN Products   p  ON p.ProductID = oi.ProductID
    GROUP BY DATEFROMPARTS(YEAR(o.OrderDate), MONTH(o.OrderDate), 1)
),
l AS (
    SELECT MonthStart, Revenue,
           LAG(Revenue) OVER (ORDER BY MonthStart) AS PrevRevenue
    FROM m
)
SELECT MonthStart, Revenue, PrevRevenue,
       CAST(100.0 * (Revenue - PrevRevenue) / NULLIF(PrevRevenue, 0) AS DECIMAL(8,2)) AS MoMGrowthPct
FROM l
ORDER BY MonthStart;
```

**Expected:**

| MonthStart | Revenue | PrevRevenue | MoMGrowthPct |
|---|---|---|---|
| 2026-01-01 | 2100.00 | NULL | NULL |
| 2026-02-01 | 1750.00 | 2100.00 | -16.67 |
| 2026-03-01 | 2700.00 | 1750.00 | 54.29 |
| 2026-04-01 | 4500.00 | 2700.00 | 66.67 |

---

## Problem 8 — Top 2 Products per Category

```sql
WITH rev AS (
    SELECT p.Category, p.ProductName,
           SUM(oi.Quantity * p.Price) AS Revenue
    FROM OrderItems oi
    JOIN Products p ON p.ProductID = oi.ProductID
    GROUP BY p.Category, p.ProductName
),
r AS (
    SELECT *, DENSE_RANK() OVER (PARTITION BY Category ORDER BY Revenue DESC) AS RankInCategory
    FROM rev
)
SELECT Category, ProductName, Revenue, RankInCategory
FROM r
WHERE RankInCategory <= 2
ORDER BY Category, RankInCategory;
```

**Expected:**

| Category | ProductName | Revenue | Rank |
|---|---|---|---|
| Electronics | Laptop | 5000.00 | 1 |
| Electronics | Phone | 2800.00 | 2 |
| Furniture | Desk | 1200.00 | 1 |
| Furniture | Chair | 1050.00 | 2 |
| Stationery | Notebook | 400.00 | 1 |

---

## Problem 9 — Bought Every Electronics Product

```sql
SELECT c.CustomerName
FROM Customers c
JOIN Orders     o  ON o.CustomerID = c.CustomerID
JOIN OrderItems oi ON oi.OrderID   = o.OrderID
JOIN Products   p  ON p.ProductID  = oi.ProductID
WHERE p.Category = 'Electronics'
GROUP BY c.CustomerID, c.CustomerName
HAVING COUNT(DISTINCT p.ProductID) =
       (SELECT COUNT(*) FROM Products WHERE Category = 'Electronics');
```

**Why:** compare the customer's distinct-product count against the total in the category. `DISTINCT` is essential since a customer can buy the same product many times.

**Expected:** `Acme Corp` (Laptop, Phone, Headphones). Globex lacks Headphones; Initech only bought Laptops.

---

## Problem 10 — Average Days Between Orders

```sql
WITH g AS (
    SELECT CustomerID,
           DATEDIFF(DAY,
                    LAG(OrderDate) OVER (PARTITION BY CustomerID ORDER BY OrderDate),
                    OrderDate) AS GapDays
    FROM Orders
)
SELECT c.CustomerName,
       COUNT(g.GapDays) AS Gaps,
       CAST(AVG(g.GapDays * 1.0) AS DECIMAL(8,2)) AS AvgGapDays
FROM g
JOIN Customers c ON c.CustomerID = g.CustomerID
WHERE g.GapDays IS NOT NULL
GROUP BY c.CustomerID, c.CustomerName
ORDER BY c.CustomerName;
```

**Gotcha:** `AVG` over an `INT` column returns an `INT` (Acme would show 33). Multiply by `1.0` first.

**Expected:**

| CustomerName | Gaps | AvgGapDays |
|---|---|---|
| Acme Corp | 3 | 33.33 |
| Globex | 2 | 45.00 |
| Initech | 1 | 47.00 |

---

## Problem 11 — Revenue Pivot: Category × Month

```sql
SELECT Category,
       ISNULL([1], 0) AS Jan,
       ISNULL([2], 0) AS Feb,
       ISNULL([3], 0) AS Mar,
       ISNULL([4], 0) AS Apr
FROM (
    SELECT p.Category,
           MONTH(o.OrderDate)    AS Mth,
           oi.Quantity * p.Price AS Amt
    FROM Orders o
    JOIN OrderItems oi ON oi.OrderID  = o.OrderID
    JOIN Products   p  ON p.ProductID = oi.ProductID
    WHERE o.OrderDate >= '2026-01-01' AND o.OrderDate < '2026-05-01'
) s
PIVOT (SUM(Amt) FOR Mth IN ([1],[2],[3],[4])) pv
ORDER BY Category;
```

**Expected:**

| Category | Jan | Feb | Mar | Apr |
|---|---|---|---|---|
| Electronics | 1200 | 1700 | 1700 | 3800 |
| Furniture | 900 | 0 | 900 | 450 |
| Stationery | 0 | 50 | 100 | 250 |

**Portable alternative (conditional aggregation):**

```sql
SELECT p.Category,
       SUM(CASE WHEN MONTH(o.OrderDate)=1 THEN oi.Quantity*p.Price ELSE 0 END) AS Jan,
       SUM(CASE WHEN MONTH(o.OrderDate)=2 THEN oi.Quantity*p.Price ELSE 0 END) AS Feb,
       SUM(CASE WHEN MONTH(o.OrderDate)=3 THEN oi.Quantity*p.Price ELSE 0 END) AS Mar,
       SUM(CASE WHEN MONTH(o.OrderDate)=4 THEN oi.Quantity*p.Price ELSE 0 END) AS Apr
FROM Orders o
JOIN OrderItems oi ON oi.OrderID = o.OrderID
JOIN Products p ON p.ProductID = oi.ProductID
GROUP BY p.Category;
```

---

## Problem 12 — Consecutive Login Streaks (Gaps & Islands)

```sql
WITH d AS (
    SELECT DISTINCT UserID, LoginDate          -- remove duplicate logins
    FROM UserLogins
),
g AS (
    SELECT UserID, LoginDate,
           DATEADD(DAY,
                   -ROW_NUMBER() OVER (PARTITION BY UserID ORDER BY LoginDate),
                   LoginDate) AS grp            -- constant within a streak
    FROM d
)
SELECT UserID,
       MIN(LoginDate) AS StreakStart,
       MAX(LoginDate) AS StreakEnd,
       COUNT(*)       AS StreakDays
FROM g
GROUP BY UserID, grp
ORDER BY UserID, StreakStart;
```

**Trick:** subtracting the row number from the date gives the same value for every date in a consecutive run.

**Expected:**

| UserID | StreakStart | StreakEnd | StreakDays |
|---|---|---|---|
| 1 | 2026-09-01 | 2026-09-03 | 3 |
| 1 | 2026-09-05 | 2026-09-06 | 2 |
| 1 | 2026-09-09 | 2026-09-09 | 1 |
| 2 | 2026-09-10 | 2026-09-14 | 5 |
| 3 | 2026-09-01 | 2026-09-01 | 1 |
| 3 | 2026-09-03 | 2026-09-03 | 1 |
| 3 | 2026-09-05 | 2026-09-05 | 1 |

**Bonus: longest streak per user**

```sql
WITH d AS (SELECT DISTINCT UserID, LoginDate FROM UserLogins),
g AS (
    SELECT UserID, LoginDate,
           DATEADD(DAY, -ROW_NUMBER() OVER (PARTITION BY UserID ORDER BY LoginDate), LoginDate) AS grp
    FROM d
),
s AS (
    SELECT UserID, MIN(LoginDate) AS StreakStart, MAX(LoginDate) AS StreakEnd, COUNT(*) AS StreakDays
    FROM g GROUP BY UserID, grp
),
r AS (
    SELECT *, ROW_NUMBER() OVER (PARTITION BY UserID ORDER BY StreakDays DESC, StreakStart) AS rn
    FROM s
)
SELECT UserID, StreakStart, StreakEnd, StreakDays
FROM r WHERE rn = 1;
```

Result: User 1 → 3 days, User 2 → 5 days, User 3 → 1 day (earliest).

---

## Problem 13 — Sessionization

```sql
WITH p AS (
    SELECT UserID, EventID, EventTime,
           LAG(EventTime) OVER (PARTITION BY UserID ORDER BY EventTime, EventID) AS PrevTime
    FROM Events
),
f AS (
    SELECT *,
           CASE WHEN PrevTime IS NULL
                  OR DATEDIFF(SECOND, PrevTime, EventTime) > 1800
                THEN 1 ELSE 0 END AS NewSession
    FROM p
),
s AS (
    SELECT *,
           SUM(NewSession) OVER (PARTITION BY UserID
                                 ORDER BY EventTime, EventID
                                 ROWS UNBOUNDED PRECEDING) AS SessionNo
    FROM f
)
SELECT UserID, SessionNo,
       MIN(EventTime) AS SessionStart,
       MAX(EventTime) AS SessionEnd,
       COUNT(*)       AS EventCount
FROM s
GROUP BY UserID, SessionNo
ORDER BY UserID, SessionNo;
```

**Pattern:** flag session starts → running `SUM` of flags = session id. Use `DATEDIFF(SECOND, ...)` for precision; `DATEDIFF(MINUTE, ...)` counts minute-boundary crossings and can mislead. Always specify `ROWS UNBOUNDED PRECEDING` (the default `RANGE` frame is slower and treats equal timestamps as peers).

**Expected:**

| UserID | SessionNo | SessionStart | SessionEnd | EventCount |
|---|---|---|---|---|
| 1 | 1 | 10:00:00 | 10:25:00 | 3 |
| 1 | 2 | 11:30:00 | 11:45:00 | 2 |
| 1 | 3 | 14:00:00 | 14:00:00 | 1 |
| 2 | 1 | 09:00:00 | 09:00:00 | 1 |
| 2 | 2 | 09:50:00 | 09:55:00 | 2 |

---

## Problem 14 — Delete Duplicates, Keep Latest

```sql
WITH d AS (
    SELECT *,
           ROW_NUMBER() OVER (PARTITION BY Email
                              ORDER BY CreatedAt DESC, ContactID DESC) AS rn
    FROM ContactsRaw
)
DELETE FROM d
WHERE rn > 1;

SELECT * FROM ContactsRaw ORDER BY ContactID;
```

**Why it works:** SQL Server allows `DELETE` through a CTE over a single table, which removes the underlying rows.

**Tip:** wrap in `BEGIN TRAN ... ROLLBACK` first to preview the damage in real life.

**Expected remaining rows:** ContactID **2** (Anna B), **3** (Bob), **6** (Cara L). Deleted: 1, 4, 5.

---

## Problem 15 — Missing Invoice Numbers

```sql
WITH n AS (
    SELECT InvoiceNo,
           LEAD(InvoiceNo) OVER (ORDER BY InvoiceNo) AS NextNo
    FROM Invoices
)
SELECT InvoiceNo + 1 AS GapStart,
       NextNo - 1    AS GapEnd
FROM n
WHERE NextNo - InvoiceNo > 1;
```

**Expected:**

| GapStart | GapEnd |
|---|---|
| 4 | 5 |
| 8 | 9 |

---

## Quick Reference: Patterns Used

| Pattern | Problems |
|---|---|
| `DENSE_RANK` / top-N per group | 1, 8 |
| Self join | 2 |
| Window aggregate (`AVG OVER`) | 3 |
| `LEFT JOIN` + `COUNT(col)` | 4 |
| `PERCENTILE_CONT` / median | 5 |
| Recursive CTE | 6 |
| `LAG` / `LEAD` | 7, 10, 13, 15 |
| Relational division | 9 |
| `PIVOT` / conditional aggregation | 11 |
| Gaps & islands (date − row_number) | 12 |
| Flag + running sum (sessionization) | 13 |
| CTE `DELETE` with `ROW_NUMBER` | 14 |
