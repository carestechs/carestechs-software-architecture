-- productid is a plain uuid: cross-module references carry no FK
-- (adrs/dotnet/cross-module-by-id.md).
CREATE SCHEMA orders;
CREATE TABLE orders.orders (
    id uuid PRIMARY KEY,
    productid uuid NOT NULL,
    quantity int NOT NULL,
    status varchar(20) NOT NULL,
    createdat timestamptz NOT NULL,
    confirmedat timestamptz NULL
);
