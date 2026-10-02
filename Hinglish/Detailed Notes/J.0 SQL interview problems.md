# MS SQL Server Interview Problems (Medium → Hard)

15 problems on one shared dataset. Run the setup script once, then solve each problem.
Solutions are in `sql_interview_solutions.md` (try first, no peeking!).

**Topics covered:** window functions, self joins, recursive CTEs, PIVOT, gaps & islands,
sessionization, relational division, de-duplication, running comparisons, NULL/integer-division gotchas.

---

## 1. Setup Script

```sql
CREATE DATABASE InterviewDB;
GO
USE InterviewDB;
GO

-- ========== HR ==========
CREATE TABLE Departments (
    DeptID   INT PRIMARY KEY,
    DeptName VARCHAR(50) NOT NULL
);

CREATE TABLE Employees (
    EmpID     INT PRIMARY KEY,
    EmpName   VARCHAR(50) NOT NULL,
    DeptID    INT NULL REFERENCES Departments(DeptID),
    ManagerID INT NULL REFERENCES Employees(EmpID),
    Salary    DECIMAL(10,2) NOT NULL,
    HireDate  DATE NOT NULL
);

INSERT INTO Departments VALUES
(1,'Engineering'),(2,'Sales'),(3,'HR'),(4,'Finance'),(5,'Legal');

INSERT INTO Employees VALUES
(1 ,'Alice', 1, NULL, 200000, '2015-03-01'),
(2 ,'Bob',   1, 1,    150000, '2016-06-15'),
(3 ,'Carol', 1, 2,    120000, '2018-01-10'),
(4 ,'Dave',  1, 2,    120000, '2019-07-23'),
(5 ,'Eve',   1, 3,     95000, '2021-02-14'),
(6 ,'Frank', 2, 1,    130000, '2017-09-09'),
(7 ,'Grace', 2, 6,     90000, '2019-11-30'),
(8 ,'Heidi', 2, 6,     90000, '2020-05-05'),
(9 ,'Ivan',  2, 7,     92000, '2022-08-19'),
(10,'Judy',  3, 1,     85000, '2018-04-04'),
(11,'Ken',   3, 10,    60000, '2021-10-10'),
(12,'Leo',   4, 1,    140000, '2016-01-20'),
(13,'Mia',   4, 12,   110000, '2020-12-01'),
(14,'Ned',   4, 12,   110000, '2023-03-15');

-- ========== E-COMMERCE ==========
CREATE TABLE Customers (
    CustomerID   INT PRIMARY KEY,
    CustomerName VARCHAR(50) NOT NULL,
    Country      VARCHAR(50) NOT NULL
);

CREATE TABLE Products (
    ProductID   INT PRIMARY KEY,
    ProductName VARCHAR(50) NOT NULL,
    Category    VARCHAR(50) NOT NULL,
    Price       DECIMAL(10,2) NOT NULL
);

CREATE TABLE Orders (
    OrderID    INT PRIMARY KEY,
    CustomerID INT NOT NULL REFERENCES Customers(CustomerID),
    OrderDate  DATE NOT NULL
);

CREATE TABLE OrderItems (
    OrderItemID INT PRIMARY KEY,
    OrderID     INT NOT NULL REFERENCES Orders(OrderID),
    ProductID   INT NOT NULL REFERENCES Products(ProductID),
    Quantity    INT NOT NULL
);

INSERT INTO Customers VALUES
(1,'Acme Corp','USA'),(2,'Globex','UK'),(3,'Initech','USA'),
(4,'Umbrella','Germany'),(5,'Hooli','USA');

INSERT INTO Products VALUES
(1,'Laptop','Electronics',1000),
(2,'Phone','Electronics',700),
(3,'Headphones','Electronics',100),
(4,'Desk','Furniture',300),
(5,'Chair','Furniture',150),
(6,'Notebook','Stationery',5);

INSERT INTO Orders VALUES
(101,1,'2026-01-05'),(102,1,'2026-02-10'),(103,2,'2026-01-20'),
(104,3,'2026-02-14'),(105,1,'2026-03-03'),(106,2,'2026-03-18'),
(107,4,'2026-03-25'),(108,3,'2026-04-02'),(109,1,'2026-04-15'),
(110,2,'2026-04-20'),(111,5,'2026-04-28');

INSERT INTO OrderItems VALUES
(1,101,1,1),(2,101,3,2),(3,102,2,1),(4,102,6,10),(5,103,4,2),
(6,103,5,2),(7,104,1,1),(8,105,3,3),(9,105,4,1),(10,106,2,2),
(11,107,5,4),(12,107,6,20),(13,108,1,2),(14,109,2,1),(15,109,3,1),
(16,110,1,1),(17,110,4,1),(18,110,5,1),(19,111,6,50);

-- ========== ACTIVITY / LOGS ==========
CREATE TABLE UserLogins (          -- no PK on purpose: duplicates exist
    UserID    INT  NOT NULL,
    LoginDate DATE NOT NULL
);

INSERT INTO UserLogins VALUES
(1,'2026-09-01'),(1,'2026-09-02'),(1,'2026-09-02'),(1,'2026-09-03'),
(1,'2026-09-05'),(1,'2026-09-06'),(1,'2026-09-09'),
(2,'2026-09-10'),(2,'2026-09-11'),(2,'2026-09-12'),(2,'2026-09-13'),(2,'2026-09-14'),
(3,'2026-09-01'),(3,'2026-09-03'),(3,'2026-09-05');

CREATE TABLE Events (
    EventID   INT PRIMARY KEY,
    UserID    INT NOT NULL,
    EventTime DATETIME2(0) NOT NULL
);

INSERT INTO Events VALUES
(1,1,'2026-09-15 10:00:00'),(2,1,'2026-09-15 10:10:00'),(3,1,'2026-09-15 10:25:00'),
(4,1,'2026-09-15 11:30:00'),(5,1,'2026-09-15 11:45:00'),(6,1,'2026-09-15 14:00:00'),
(7,2,'2026-09-15 09:00:00'),(8,2,'2026-09-15 09:50:00'),(9,2,'2026-09-15 09:55:00');

-- ========== DATA CLEANING ==========
CREATE TABLE ContactsRaw (
    ContactID INT PRIMARY KEY,
    Email     VARCHAR(100) NOT NULL,
    FullName  VARCHAR(100) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL
);

INSERT INTO ContactsRaw VALUES
(1,'a@x.com','Anna',   '2026-01-01 09:00:00'),
(2,'a@x.com','Anna B', '2026-02-01 09:00:00'),
(3,'b@x.com','Bob',    '2026-01-15 09:00:00'),
(4,'c@x.com','Cara',   '2026-01-20 09:00:00'),
(5,'c@x.com','Cara',   '2026-01-20 09:00:00'),
(6,'c@x.com','Cara L', '2026-03-01 09:00:00');

CREATE TABLE Invoices (InvoiceNo INT PRIMARY KEY);
INSERT INTO Invoices VALUES (1),(2),(3),(6),(7),(10);
```

### Table cheat-sheet

| Table | Columns |
|---|---|
| Departments | DeptID, DeptName |
| Employees | EmpID, EmpName, DeptID, ManagerID, Salary, HireDate |
| Customers | CustomerID, CustomerName, Country |
| Products | ProductID, ProductName, Category, Price |
| Orders | OrderID, CustomerID, OrderDate |
| OrderItems | OrderItemID, OrderID, ProductID, Quantity |
| UserLogins | UserID, LoginDate (duplicates possible) |
| Events | EventID, UserID, EventTime |
| ContactsRaw | ContactID, Email, FullName, CreatedAt |
| Invoices | InvoiceNo |

*Revenue of an order line = `Quantity * Price`.*

---

## 2. Problems

### Problem 1 — Top 2 Salary Tiers per Department ⭐⭐
Return every employee whose salary is in the **top 2 distinct salaries** of their department.
If several employees share a salary, all of them qualify.

**Output:** `DeptName, EmpName, Salary`

---

### Problem 2 — Earning More Than the Boss ⭐⭐
Find employees who earn **more than their direct manager**.

**Output:** `EmpName, Salary, ManagerName, ManagerSalary`

---

### Problem 3 — Above Department Average ⭐⭐
List employees whose salary is **strictly above the average salary of their own department**.
Do it with a window function (no correlated subquery).

**Output:** `EmpName, DeptName, Salary, DeptAvgSalary`

---

### Problem 4 — Understaffed Departments ⭐⭐
List departments with **fewer than 3 employees**, including departments with **zero** employees.

**Output:** `DeptName, Headcount`

---

### Problem 5 — Median Salary per Department ⭐⭐⭐
Compute the **median** salary for each department (for an even count, average the two middle values).

**Output:** `DeptName, MedianSalary`

---

### Problem 6 — Org Chart (Recursive CTE) ⭐⭐⭐⭐
Starting from the top of the hierarchy (no manager), output every employee with their
**level** (CEO = 0) and the **full reporting path** (e.g. `Alice > Bob > Carol`).

**Output:** `EmpID, EmpName, Lvl, Path`

---

### Problem 7 — Month-over-Month Revenue Growth ⭐⭐⭐
For each month, compute total revenue, previous month's revenue, and the **% growth**
(rounded to 2 decimals). The first month has `NULL` growth.

**Output:** `MonthStart, Revenue, PrevRevenue, MoMGrowthPct`

---

### Problem 8 — Top 2 Products per Category ⭐⭐⭐
For each product category, return the **top 2 products by total revenue**.
Categories with only one sold product return just that one.

**Output:** `Category, ProductName, Revenue, RankInCategory`

---

### Problem 9 — Bought Every Electronics Product ⭐⭐⭐
Find customers who have purchased **every product** in the `Electronics` category
(relational division). The query must still work if new Electronics products are added.

**Output:** `CustomerName`

---

### Problem 10 — Average Days Between Orders ⭐⭐⭐
For each customer with **at least 2 orders**, compute the number of gaps and the
**average number of days between consecutive orders** (2 decimals).
Watch out for integer division.

**Output:** `CustomerName, Gaps, AvgGapDays`

---

### Problem 11 — Revenue Pivot: Category × Month ⭐⭐⭐
Produce one row per category with a column per month (Jan–Apr 2026) showing revenue.
Show `0` instead of `NULL`.

**Output:** `Category, Jan, Feb, Mar, Apr`

---

### Problem 12 — Consecutive Login Streaks (Gaps & Islands) ⭐⭐⭐⭐
Using `UserLogins` (note the duplicate row), list every **streak of consecutive login days**
per user.
**Bonus:** return only each user's longest streak.

**Output:** `UserID, StreakStart, StreakEnd, StreakDays`

---

### Problem 13 — Sessionization ⭐⭐⭐⭐
Group `Events` into sessions: a new session starts when the gap from the user's previous
event is **more than 30 minutes**. Return one row per session.

**Output:** `UserID, SessionNo, SessionStart, SessionEnd, EventCount`

---

### Problem 14 — Delete Duplicates, Keep Latest ⭐⭐⭐
`ContactsRaw` has duplicate emails. Delete duplicates so that **one row per email remains**:
the one with the latest `CreatedAt` (break ties with the higher `ContactID`).
Then `SELECT` the remaining rows.

---

### Problem 15 — Missing Invoice Numbers ⭐⭐⭐
`Invoices` should be sequential, but some numbers are missing. Return each **missing range**.

**Output:** `GapStart, GapEnd`

---

## 3. Self-Check Tips

- Run each answer, then compare to the expected output in the solutions file.
- Think about **ties**, **NULLs**, **empty groups**, and **duplicate rows** before submitting.
- In an interview, state your approach out loud first, then write the query.
