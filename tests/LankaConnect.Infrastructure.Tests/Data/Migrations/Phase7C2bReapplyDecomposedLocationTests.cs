using System;
using System.Linq;
using FluentAssertions;
using LankaConnect.Infrastructure.Data.Migrations.Resources;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data.Migrations;

/// <summary>
/// Phase 7C.2b Chunk 1: transformation-logic invariants for the
/// <c>Phase7C2b_ReapplyDecomposedLocationInCommitmentTemplates</c> migration.
///
/// <para>
/// The migration's <c>Up()</c> runs
/// <c>UPDATE ... SET html_template = REPLACE(html_template, '{{EventLocation}}', &lt;DecomposedBlock&gt;)</c>
/// against each of the 3 active signup/volunteer commitment templates that still carry
/// the legacy flat token after the 2026-04-22 recovery migration restored them to
/// pre-rewrite bodies. These tests simulate the PostgreSQL <c>REPLACE</c> call in C#
/// against the actual embedded restore bodies and assert every invariant the
/// post-UPDATE guards in the migration will check at apply time:
/// row-count = 1, body no longer contains <c>{{EventLocation}}</c>, body contains
/// <c>{{LocationName}}</c>, body still contains <c>{{UserName}}</c>, body length ≥ 50000.
/// </para>
/// <para>
/// This is a unit-level TDD red/green loop — Testcontainers-level integration is a
/// separate follow-up. It catches:
/// (a) <see cref="EmailLocationBlockHtml.DecomposedBlock"/> drift that would break
/// every downstream template the same way,
/// (b) a restore-template body that no longer contains <c>{{EventLocation}}</c>
/// (migration would 0-row and <c>RAISE EXCEPTION</c> — tested here fails loudly
/// instead of at deploy time),
/// (c) the two cancellation templates lacking <c>{{EventLocation}}</c> — Chunk 1
/// migration skips them by design and this test documents that invariant.
/// </para>
/// </summary>
public class Phase7C2bReapplyDecomposedLocationTests
{
    private const string LegacyToken = "{{EventLocation}}";

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void ActiveTemplate_contains_legacy_token_before_migration(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        body.Should().Contain(LegacyToken,
            "the migration's WHERE clause matches on the legacy token — if the restore body no longer contains it the migration would RAISE EXCEPTION at apply time");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-cancellation")]
    [InlineData("template-volunteer-commitment-cancellation")]
    public void CancellationTemplate_does_not_contain_legacy_token(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        body.Should().NotContain(LegacyToken,
            "cancellation bodies were never part of the Phase 7C.2 rewrite scope — the Chunk 1 migration skips them and this invariant guards that decision");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void Replacing_legacy_token_with_DecomposedBlock_removes_all_occurrences(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        var after = body.Replace(LegacyToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().NotContain(LegacyToken,
            "every occurrence of the flat token must be replaced — otherwise the post-UPDATE guard 'body LIKE %{{EventLocation}}%' would RAISE EXCEPTION at apply time");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void After_migration_body_contains_LocationName_placeholder(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        var after = body.Replace(LegacyToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().Contain("{{LocationName}}",
            "migration's post-UPDATE guard asserts the new decomposed placeholder landed — mirrors the {{UserName}} survival check");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void After_migration_body_still_contains_UserName_greeting(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        var after = body.Replace(LegacyToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().Contain("{{UserName}}",
            "REPLACE against the unique {{EventLocation}} token must NOT touch the {{UserName}} greeting — tests the migration's greeting-survival guard");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void After_migration_body_length_at_least_50000_bytes(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        var after = body.Replace(LegacyToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Length.Should().BeGreaterOrEqualTo(50_000,
            "migration's final post-UPDATE guard asserts body length ≥ 50000 — protects against catastrophic truncation like the 2026-04-21 damage");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void After_migration_body_length_grows_by_block_size_times_token_count(string name)
    {
        var body = Phase7C2RecoveryTemplates.LoadHtml(name);

        var tokenCount = CountOccurrences(body, LegacyToken);
        var expectedDelta = (EmailLocationBlockHtml.DecomposedBlock.Length - LegacyToken.Length) * tokenCount;

        var after = body.Replace(LegacyToken, EmailLocationBlockHtml.DecomposedBlock);

        (after.Length - body.Length).Should().Be(expectedDelta,
            "length math must be exact — any off-by-one suggests REPLACE picked up substring matches that aren't really {{EventLocation}} tokens");
    }

    [Fact]
    public void DecomposedBlock_is_referenced_by_this_test_to_catch_compile_time_drift()
    {
        // Guard test: compile-pins the reference from Phase7C2b migration tests to
        // EmailLocationBlockHtml.DecomposedBlock. If the const ever moves or gets
        // renamed the build breaks here, which is louder than silently applying a
        // stale migration to staging.
        EmailLocationBlockHtml.DecomposedBlock.Should().NotBeNullOrEmpty();
        EmailLocationBlockHtml.DecomposedBlock.Should().Contain("{{LocationName}}");
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) != -1)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }
}
