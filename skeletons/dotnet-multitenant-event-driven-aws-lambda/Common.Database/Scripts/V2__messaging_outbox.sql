-- Transactional outbox rows live next to the state they describe
-- (adrs/database/transactional-outbox.md).
CREATE TABLE messaging.outbox (
    id uuid PRIMARY KEY,
    queuename varchar(100) NOT NULL,
    payload text NOT NULL,
    correlationid varchar(64) NOT NULL,
    createdat timestamptz NOT NULL,
    dispatchedat timestamptz NULL
);
