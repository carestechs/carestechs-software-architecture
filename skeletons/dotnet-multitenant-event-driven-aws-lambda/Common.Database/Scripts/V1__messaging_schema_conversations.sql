-- DbUp-applied, embedded in the assembly (adrs/deployment/dbup-migrations.md).
-- Each module owns a schema (adrs/database/schema-per-module.md); the DbUp
-- journal stays in public. Lowercase identifiers (adrs/database/lowercase-naming.md).
CREATE SCHEMA IF NOT EXISTS messaging;

CREATE TABLE messaging.conversations (
    id uuid PRIMARY KEY,
    contactname varchar(200) NOT NULL,
    status varchar(20) NOT NULL,
    createdat timestamptz NOT NULL
);
