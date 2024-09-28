Feature: DbQueryProfiler

Essential tests

Scenario: Insert Query
	Given the endpoint is /insert
	When executed
	Then profiled query should be
"""
INSERT INTO "Todos" ("Title")
VALUES (@p0)
RETURNING "Id";
"""


Scenario: Update Query
	Given the endpoint is /update
	When executed
	Then profiled query should be
"""
UPDATE "Todos" SET "Title" = @p0
WHERE "Id" = @p1
RETURNING 1;
"""

Scenario: Select Scalar
	Given the endpoint is /select/scalar
	When executed
	Then profiled query should be
"""
SELECT "t"."Id"
FROM "Todos" AS "t"
"""

Scenario: Select Single #1
	Given the endpoint is /select/single/1
	When executed
	Then profiled query should be
"""
SELECT "t"."Id", "t"."Title"
FROM "Todos" AS "t"
LIMIT 1
"""

Scenario: Select Single #2
	Given the endpoint is /select/single/2
	When executed
	Then profiled query should be
"""
SELECT "t"."Id", "t"."Title"
FROM "Todos" AS "t"
LIMIT 1 OFFSET @__p_0
"""

Scenario: Select All
	Given the endpoint is /select/all
	When executed
	Then profiled query should be
"""
SELECT "t"."Id", "t"."Title"
FROM "Todos" AS "t"
"""


Scenario: Delete Single
	Given the endpoint is /delete/single
	When executed
	Then profiled query should be
"""
DELETE FROM "Todos"
WHERE "Id" = @p0
RETURNING 1;
"""

Scenario: Delete All
	Given the endpoint is /delete/all
	When executed
	Then profiled query should be
"""
DELETE FROM "Todos" AS "t"
"""
