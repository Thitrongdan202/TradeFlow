SELECT 'Products' as entity, count(*) as total FROM "Products"
UNION ALL
SELECT 'Customers', count(*) FROM "Customers"
UNION ALL
SELECT 'Suppliers', count(*) FROM "Suppliers"
UNION ALL
SELECT 'SalesOrders', count(*) FROM "SalesOrders"
UNION ALL
SELECT 'Invoices', count(*) FROM "Invoices";
