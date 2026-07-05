using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Wave 5.1.a-α.2 (2026-06-27) — pure model-snapshot rebaseline.
    ///
    /// EventPass.Price (Money) was decomposed into scalar PriceAmount (decimal)
    /// + PriceCurrency (Currency enum, HasConversion&lt;string&gt;) + [NotMapped]
    /// facade. Same change for PassPurchase.TotalPrice. EF Core recognises that
    /// the new direct Property mappings hit the SAME DB columns as the prior
    /// ComplexProperty/OwnsOne mapping (price_amount, price_currency,
    /// total_price_amount, total_price_currency) — confirmed by the scaffolder
    /// producing ZERO column add/drop/alter operations. The only scaffolded
    /// output was reference_values.created_at timestamp churn from .HasData
    /// seed-snapshot reflection, which is unrelated to this migration's intent.
    ///
    /// Empty Up()/Down() per the [[empty-up-snapshot-rebaseline]] precedent.
    /// The model snapshot delta (.Designer.cs + AppDbContextModelSnapshot.cs)
    /// captures the new entity shape so subsequent migrations diff correctly.
    ///
    /// Idempotent SQL: single __EFMigrationsHistory row insert, no DDL. Verified
    /// pre-commit via `dotnet ef migrations script --idempotent`.
    /// </summary>
    public partial class Wave5_1a_alpha_2_ScalarPriceMoneyOnEventPassPassPurchase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: model-snapshot rebaseline only.
        }
    }
}
