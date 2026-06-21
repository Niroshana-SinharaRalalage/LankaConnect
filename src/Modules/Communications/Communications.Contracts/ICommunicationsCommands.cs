namespace LankaConnect.Modules.Communications.Contracts;

/// <summary>
/// Cross-module mutator API for the Communications module.
/// SCOPE: only operations invoked FROM other modules. All within-Communications
/// mutators (Create/Update/Delete EmailGroup, Create/Update Newsletter, etc.)
/// move into Communications.Application in Wave 5.4.c.1 and are NOT exposed
/// via this contract — they remain MediatR commands dispatched by the
/// controllers that already live inside the module.
/// </summary>
/// <remarks>
/// Wave 5.4.a (2026-06-13). The surface is intentionally minimal at launch —
/// concrete methods are added in Wave 5.4.d.1 when the audit of
/// <c>LankaConnect.Application</c> identifies which cross-module mutator paths
/// (if any) need to go through Contracts. Mirrors the Forms 5.3a pattern where
/// <c>IFormCommands</c> shipped with a single architect-known method
/// (<c>DeleteResponsesByEventAndUserAsync</c>) added at 5.3a time. If the 5.4.d.1
/// audit surfaces zero cross-module mutators, this interface is removed in
/// 5.4.d.3 along with the unused DI registration.
/// </remarks>
public interface ICommunicationsCommands
{
    // Concrete methods added in Wave 5.4.d.1 after audit. Empty marker interface
    // for now so consumers can take the dependency in 5.4.d.1 without
    // additional Contracts ripple.
}
