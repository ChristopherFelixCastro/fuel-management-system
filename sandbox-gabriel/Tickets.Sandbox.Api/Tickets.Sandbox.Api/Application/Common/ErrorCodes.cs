namespace Tickets.Sandbox.Api.Application.Common;

public static class ErrorCodes
{
    // Códigos Globales / HTTP
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string Forbidden = "FORBIDDEN";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";
    public const string IdempotencyKeyReused = "IDEMPOTENCY_KEY_REUSED";
    public const string InternalError = "INTERNAL_ERROR";

    // Códigos Específicos del Módulo
    public const string VehicleFuelMismatch = "VEHICLE_FUEL_MISMATCH";
    public const string QuantityExceedsTankCapacity = "QUANTITY_EXCEEDS_TANK_CAPACITY";
    public const string DepartmentIncoherent = "DEPARTMENT_INCOHERENT";
    public const string RequestNotEditable = "REQUEST_NOT_EDITABLE";
    public const string RequestNotPending = "REQUEST_NOT_PENDING";
    public const string InsufficientAvailableInventory = "INSUFFICIENT_AVAILABLE_INVENTORY";
    public const string TicketAlreadyExists = "TICKET_ALREADY_EXISTS";
    public const string ExpirationInvalid = "EXPIRATION_INVALID";
    public const string TicketNotFound = "TICKET_NOT_FOUND";
    public const string TicketAccessDenied = "TICKET_ACCESS_DENIED";
    public const string PdfGenerationFailed = "PDF_GENERATION_FAILED";
    public const string TicketExpired = "TICKET_EXPIRED";
    public const string TicketConsumed = "TICKET_CONSUMED";
    public const string TicketCancelled = "TICKET_CANCELLED";
    public const string QrSignatureInvalid = "QR_SIGNATURE_INVALID";
    public const string IntegrationFailure = "INTEGRATION_FAILURE";
}
