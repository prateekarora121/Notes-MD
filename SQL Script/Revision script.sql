--Create DATABASE Practice11;
---- 3. Create the custom schema
--CREATE SCHEMA Practice1;
GO

GO

-- 2. Switch to the newly created database context
USE Practice11;



DROP TABLE Practice1.practice
GO
Create Table Practice1.practice(
OrderID int IDENTITY(1,1) Primary key,
OrderNumber int Not null,
OrderDesc VARCHAR CHECK (LEN(OrderDesc)>1),

CONSTRAINT PK_ODEREID_PRIMARKEY UNIQUE (OrderID)
)


Create NONCLUSTERED INDEX NON_CLUSTERED_INDEX ON PRACTICE(OrderNumber);



insertinto 