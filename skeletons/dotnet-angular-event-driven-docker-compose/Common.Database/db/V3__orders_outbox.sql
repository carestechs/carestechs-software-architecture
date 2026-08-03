-- Transactional outbox (adrs/database/transactional-outbox.md): rows are written
-- in the same transaction as the business change; Outbox.Dispatch drains them.
CREATE TABLE orders.outbox_messages (
    id uuid PRIMARY KEY,
    queuename varchar(128) NOT NULL,
    payload text NOT NULL,
    correlationid varchar(64) NOT NULL,
    createdat timestamptz NOT NULL,
    dispatchedat timestamptz NULL
);
CREATE INDEX outbox_pending ON orders.outbox_messages (createdat) WHERE dispatchedat IS NULL;
