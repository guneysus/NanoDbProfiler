Feature: DbQueryProfiler

Essential tests

Scenario: Insert Query
    When /insert
    Then query should be
        """
        INSERT INTO "Todos" ("Title")
        VALUES (@p0)
        RETURNING "Id";
        """

Scenario: Update Query
    When /update
    Then query should be
        """
        UPDATE "Todos" SET "Title" = @p0
        WHERE "Id" = @p1
        RETURNING 1;
        """

Scenario: Select Scalar
    When /select/scalar
    Then query should be
        """
        SELECT "t"."Id"
        FROM "Todos" AS "t"
        """

Scenario: Select Single #1
    When /select/single/1
    Then query should be
        """
        SELECT "t"."Id", "t"."Title"
        FROM "Todos" AS "t"
        LIMIT 1
        """

Scenario: Select Single #2
    When /select/single/2
    Then query should be
        """
        SELECT "t"."Id", "t"."Title"
        FROM "Todos" AS "t"
        LIMIT 1 OFFSET @__p_0
        """

Scenario: Select All
    When /select/all
    Then query should be
        """
        SELECT "t"."Id", "t"."Title"
        FROM "Todos" AS "t"
        """

Scenario: Delete Single
    When /delete/single
    Then query should be
        """
        DELETE FROM "Todos"
        WHERE "Id" = @p0
        RETURNING 1;
        """

Scenario: Delete All
    When /delete/all
    Then query should be
        """
        DELETE FROM "Todos" AS "t"
        """