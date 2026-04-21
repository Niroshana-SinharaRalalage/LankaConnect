namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Phase 7D.1 Step 15: Column-label bundle for sign-up list exports (CSV + Excel).
/// Allows the same export pipeline to produce "Items" headers (default) or
/// "Volunteers" headers without forking the service implementation.
/// </summary>
public sealed record SignUpExportLabels(
    string ItemDescription,
    string RequestedQuantity,
    string RemainingQuantity,
    string ContactName,
    string ContactEmail,
    string ContactPhone,
    string QuantityCommitted)
{
    public static SignUpExportLabels ForItems() => new(
        ItemDescription: "Item Description",
        RequestedQuantity: "Requested Quantity",
        RemainingQuantity: "Remaining Quantity",
        ContactName: "Contact Name",
        ContactEmail: "Contact Email",
        ContactPhone: "Contact Phone",
        QuantityCommitted: "Quantity Committed");

    public static SignUpExportLabels ForVolunteers() => new(
        ItemDescription: "Volunteer Role",
        RequestedQuantity: "Volunteers Needed",
        RemainingQuantity: "Volunteers Remaining",
        ContactName: "Volunteer Name",
        ContactEmail: "Volunteer Email",
        ContactPhone: "Volunteer Phone",
        QuantityCommitted: "Committed");
}
