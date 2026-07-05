using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
namespace LankaConnect.Modules.Communications.Domain;

/// <summary>
/// Phase 7A: Repository interface for WhatsApp template registry.
/// </summary>
public interface IWhatsAppTemplateRepository : IRepository<WhatsAppTemplate>
{
    Task<WhatsAppTemplate?> GetByNameAsync(string templateName, CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplate>> GetApprovedTemplatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WhatsAppTemplate>> GetAllTemplatesAsync(CancellationToken ct = default);
}
