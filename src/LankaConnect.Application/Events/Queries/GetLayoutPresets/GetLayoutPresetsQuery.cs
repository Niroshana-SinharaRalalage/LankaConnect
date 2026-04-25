using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Application.Events.Queries.GetLayoutPresets;

/// <summary>
/// Slice 6 Chunk S6.2: returns the static metadata list powering the preset-library
/// modal. No parameters — the preset list is immutable server-side code.
/// </summary>
public record GetLayoutPresetsQuery() : IQuery<IReadOnlyList<LayoutPresetDto>>;
