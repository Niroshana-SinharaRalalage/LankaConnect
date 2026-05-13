using System.Linq.Expressions;
using LankaConnect.BuildingBlocks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Per ADR-005 (Money DTO migration), <see cref="Money"/> persists as TWO
/// columns: <c>{prefix}_amount</c> + <c>{prefix}_currency</c>. The currency
/// is stored as its ISO 4217 code (3 chars); reconstruction goes through
/// <see cref="Currency.FromCode"/> which validates against the supported registry.
/// </summary>
/// <remarks>
/// <para>
/// Why two columns vs JSON-serialize the composite?
/// Two-column persistence enables WHERE clauses, aggregation, indexing on
/// the amount, and currency-filtered queries — all of which are expensive
/// or impossible against an opaque JSON blob.
/// </para>
/// <para>
/// Why use ComplexProperty / OwnsOne semantics? Money is a value object —
/// it doesn't have an identity, doesn't make sense to share between entities
/// (each entity holds its own Money instance), and is always loaded with
/// the owning entity. EF Core 8 supports both <c>OwnsOne</c> and complex
/// properties; <c>OwnsOne</c> is widely used + better tooling supported.
/// </para>
/// </remarks>
public static class MoneyConfigurationExtensions
{
    /// <summary>
    /// Maps a nullable <see cref="Money"/> property to a pair of columns
    /// (<paramref name="columnPrefix"/>_amount, <paramref name="columnPrefix"/>_currency).
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="propertyExpression">e.g. <c>e =&gt; e.Price</c></param>
    /// <param name="columnPrefix">e.g. <c>"price"</c> → columns <c>price_amount</c>, <c>price_currency</c></param>
    /// <param name="amountPrecision">Decimal precision for the amount column. Default 18,4 covers all supported currencies.</param>
    /// <param name="amountScale">Decimal scale for the amount column.</param>
    public static EntityTypeBuilder<TEntity> ConfigureMoney<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        Expression<Func<TEntity, Money?>> propertyExpression,
        string columnPrefix,
        int amountPrecision = 18,
        int amountScale = 4)
        where TEntity : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(propertyExpression);

        if (string.IsNullOrWhiteSpace(columnPrefix))
        {
            throw new ArgumentException("Column prefix must be non-empty.", nameof(columnPrefix));
        }

        return builder.OwnsOne(propertyExpression, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName($"{columnPrefix}_amount")
                .HasPrecision(amountPrecision, amountScale);

            money.Property(m => m.Currency)
                .HasColumnName($"{columnPrefix}_currency")
                .HasMaxLength(3)
                .HasConversion(
                    currency => currency.Code,
                    code => Currency.FromCode(code));
        });
    }
}
