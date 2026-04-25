using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Commands.GenerateSeats;

/// <summary>
/// Generates theater or banquet seats for a zone within a venue layout.
/// </summary>
public record GenerateSeatsCommand(
    Guid LayoutId,
    Guid ZoneId,
    /// <summary>"Theater" or "Banquet"</summary>
    string GenerationType,
    /// <summary>Rows (theater) or Tables (banquet)</summary>
    int RowsOrTables,
    /// <summary>Seats per row (theater) or Seats per table (banquet)</summary>
    int SeatsPerUnit,
    /// <summary>Start row label (theater, e.g. "A") or start table number (banquet, e.g. 1)</summary>
    string? StartLabel
) : ICommand<VenueLayoutDto>;
