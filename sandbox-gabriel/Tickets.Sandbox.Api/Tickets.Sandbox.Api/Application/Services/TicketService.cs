using Microsoft.EntityFrameworkCore;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.DTOs.Tickets;
using Tickets.Sandbox.Api.Application.Interfaces;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Domain.Exceptions;
using Tickets.Sandbox.Api.Infrastructure.Persistence;

namespace Tickets.Sandbox.Api.Application.Services;

public class TicketService : ITicketService
{
    private readonly AppDbContext _context;
    private readonly IMasterDataService _masterDataService;
    private readonly IInventoryService _inventoryService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly IAuditService _auditService;

    public TicketService(
        AppDbContext context,
        IMasterDataService masterDataService,
        IInventoryService inventoryService,
        IQrCodeService qrCodeService,
        IPdfGeneratorService pdfGeneratorService,
        IAuditService auditService)
    {
        _context = context;
        _masterDataService = masterDataService;
        _inventoryService = inventoryService;
        _qrCodeService = qrCodeService;
        _pdfGeneratorService = pdfGeneratorService;
        _auditService = auditService;
    }

    public async Task<(List<TicketResponseDto> Items, int TotalCount)> GetTicketsAsync(
        TicketFilterDto filter,
        string? userRole = null,
        Guid? userEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Tickets
            .Include(t => t.Request)
            .AsNoTracking()
            .AsQueryable();

        // Filtro por rol: SOLICITANTE solo ve sus tickets
        if (userRole == "SOLICITANTE" && userEmployeeId.HasValue)
        {
            query = query.Where(t => t.Request.EmployeeId == userEmployeeId.Value);
        }
        else if (filter.EmployeeId.HasValue)
        {
            query = query.Where(t => t.Request.EmployeeId == filter.EmployeeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Number))
            query = query.Where(t => t.Number.Contains(filter.Number));

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(t => t.Request.VehicleId == filter.VehicleId.Value);

        if (filter.From.HasValue)
            query = query.Where(t => t.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(t => t.CreatedAt <= filter.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var tickets = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<TicketResponseDto>();
        foreach (var t in tickets)
        {
            dtos.Add(await MapToResponseDtoAsync(t, cancellationToken));
        }

        return (dtos, totalCount);
    }

    public async Task<TicketResponseDto> GetTicketByIdAsync(
        Guid id,
        string? userRole = null,
        Guid? userEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Request)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket == null)
            throw new BusinessRuleViolationException(ErrorCodes.TicketNotFound, "El ticket no fue encontrado.", nameof(id));

        if (userRole == "SOLICITANTE" && userEmployeeId.HasValue && ticket.Request.EmployeeId != userEmployeeId.Value)
        {
            throw new BusinessRuleViolationException(ErrorCodes.TicketAccessDenied, "No tiene permisos para acceder a este ticket.");
        }

        return await MapToResponseDtoAsync(ticket, cancellationToken);
    }

    public async Task<byte[]> GetTicketPdfBytesAsync(
        Guid id,
        string? userRole = null,
        Guid? userEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Request)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket == null)
            throw new BusinessRuleViolationException(ErrorCodes.TicketNotFound, "El ticket no fue encontrado.", nameof(id));

        if (userRole == "SOLICITANTE" && userEmployeeId.HasValue && ticket.Request.EmployeeId != userEmployeeId.Value)
        {
            throw new BusinessRuleViolationException(ErrorCodes.TicketAccessDenied, "No tiene permisos para descargar el PDF de este ticket.");
        }

        try
        {
            var emp = await _masterDataService.GetEmployeeByIdAsync(ticket.Request.EmployeeId, cancellationToken);
            var veh = await _masterDataService.GetVehicleByIdAsync(ticket.Request.VehicleId, cancellationToken);
            var dept = await _masterDataService.GetDepartmentByIdAsync(ticket.Request.DepartmentId, cancellationToken);
            var fuel = await _masterDataService.GetFuelTypeByIdAsync(ticket.Request.FuelTypeId, cancellationToken);

            // Reconstruir payload de presentación para el QR del PDF
            var qrPayload = $"{ticket.Id}:{ticket.QrTokenHash}:{ticket.Signature}";
            var qrBytes = _qrCodeService.GenerateQrImagePng(qrPayload);

            var pdfModel = new TicketPdfModel(
                ticket.Number,
                ticket.Status.ToString(),
                emp?.FullName ?? "N/A",
                emp?.EmployeeNumber ?? "N/A",
                veh?.Plate ?? "N/A",
                dept?.Name ?? "N/A",
                fuel?.Name ?? "N/A",
                ticket.AuthorizedQuantity,
                ticket.CreatedAt,
                ticket.ExpiresAt,
                qrBytes
            );

            return _pdfGeneratorService.GenerateTicketPdf(pdfModel);
        }
        catch (BusinessRuleViolationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BusinessRuleViolationException(ErrorCodes.PdfGenerationFailed, $"Error al generar el documento PDF del ticket: {ex.Message}");
        }
    }

    public async Task<ValidateTicketResponseDto> ValidateTicketQrAsync(
        ValidateTicketRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        // 1. Parsear payload compacto del QR
        var parsed = _qrCodeService.ParsePayload(dto.QrPayload);
        if (parsed == null)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), "UNKNOWN", "Payload QR con formato corrupto o inválido", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.QrSignatureInvalid, "El código QR presentado tiene un formato inválido.", nameof(dto.QrPayload));
        }

        var (ticketId, rawToken, signature) = parsed.Value;

        // 2. Validar firma criptográfica HMAC-SHA256
        var isSignatureValid = _qrCodeService.VerifySignature(ticketId, rawToken, signature);
        if (!isSignatureValid)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticketId.ToString(), "Firma criptográfica HMAC alterada o inválida", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.QrSignatureInvalid, "La firma de seguridad del código QR no es válida.", nameof(dto.QrPayload));
        }

        // 3. Consultar estado en la base de datos (única fuente de verdad)
        var ticket = await _context.Tickets
            .Include(t => t.Request)
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket == null)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticketId.ToString(), "Ticket inexistente en la base de datos", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.TicketNotFound, "El ticket no existe en el sistema.", nameof(ticketId));
        }

        // 4. Validar token hash contra el persistido
        var computedHash = _qrCodeService.HashToken(rawToken);
        if (ticket.QrTokenHash != computedHash)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticket.Id.ToString(), "Hash de token no coincide con el registro persistido", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.QrSignatureInvalid, "El token de autenticación del ticket es inválido.");
        }

        // 5. Validaciones de estado con códigos de error exactos
        if (ticket.Status == TicketStatus.CONSUMIDO)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticket.Id.ToString(), "Intento de validación de ticket ya consumido", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.TicketConsumed, "El ticket ya fue consumido anteriormente.", nameof(ticketId));
        }

        if (ticket.Status == TicketStatus.ANULADO)
        {
            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticket.Id.ToString(), "Intento de validación de ticket anulado", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.TicketCancelled, "El ticket ha sido anulado y no es válido para despacho.", nameof(ticketId));
        }

        // 6. Validar expiración
        if (ticket.Status == TicketStatus.VENCIDO || DateTime.UtcNow > ticket.ExpiresAt)
        {
            if (ticket.Status != TicketStatus.VENCIDO)
            {
                ticket.Status = TicketStatus.VENCIDO;
                await _context.SaveChangesAsync(cancellationToken);
                await _inventoryService.ReleaseReservationAsync(ticket.Id, cancellationToken);
            }

            await _auditService.LogEventAsync("QR_VALIDATION_FAILED", nameof(Ticket), ticket.Id.ToString(), "Intento de validación de ticket vencido", dto.StationId, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.TicketExpired, "El ticket se encuentra vencido.", nameof(ticketId));
        }

        if (ticket.Status != TicketStatus.ACTIVO)
        {
            throw new BusinessRuleViolationException(ErrorCodes.BusinessRuleViolation, "El ticket no se encuentra en estado ACTIVO.");
        }

        // 7. Cargar datos para respuesta a la PWA de Despacho
        var emp = await _masterDataService.GetEmployeeByIdAsync(ticket.Request.EmployeeId, cancellationToken);
        var veh = await _masterDataService.GetVehicleByIdAsync(ticket.Request.VehicleId, cancellationToken);
        var fuel = await _masterDataService.GetFuelTypeByIdAsync(ticket.Request.FuelTypeId, cancellationToken);

        await _auditService.LogEventAsync("QR_VALIDATION_SUCCESS", nameof(Ticket), ticket.Id.ToString(), $"Ticket validado correctamente en estación {dto.StationId}", dto.StationId, cancellationToken);

        return new ValidateTicketResponseDto
        {
            TicketId = ticket.Id,
            Number = ticket.Number,
            Employee = new TicketEmployeeDto(emp?.FullName ?? "N/A", emp?.EmployeeNumber ?? "N/A"),
            Vehicle = new TicketVehicleDto(veh?.Id, veh?.Plate ?? "N/A"),
            FuelType = fuel?.Name ?? "N/A",
            AuthorizedQuantity = ticket.AuthorizedQuantity,
            ExpiresAt = ticket.ExpiresAt,
            Status = ticket.Status.ToString(),
            CanDispatch = true,
            ValidationId = Guid.NewGuid()
        };
    }

    public async Task<TicketResponseDto> CancelTicketAsync(
        Guid id,
        CancelTicketRequestDto dto,
        string? supervisorId = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Request)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket == null)
            throw new BusinessRuleViolationException(ErrorCodes.TicketNotFound, "El ticket no fue encontrado.", nameof(id));

        if (ticket.Status == TicketStatus.CONSUMIDO)
            throw new BusinessRuleViolationException(ErrorCodes.TicketConsumed, "No se puede anular un ticket que ya fue consumido.", nameof(id));

        if (ticket.Status == TicketStatus.ANULADO)
            throw new BusinessRuleViolationException(ErrorCodes.TicketCancelled, "El ticket ya se encuentra anulado.", nameof(id));

        // Anular ticket y liberar reserva
        ticket.Status = TicketStatus.ANULADO;
        ticket.CancelledAt = DateTime.UtcNow;
        ticket.CancellationReason = dto.Reason;
        ticket.CancelledBy = supervisorId ?? "SUPERVISOR";

        ticket.Request.Status = RequestStatus.CANCELADA;
        ticket.Request.CancelledAt = DateTime.UtcNow;
        ticket.Request.RejectionReason = $"[Ticket Anulado]: {dto.Reason}";

        await _inventoryService.ReleaseReservationAsync(ticket.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogEventAsync("TICKET_CANCELLED", nameof(Ticket), ticket.Id.ToString(), $"Ticket anulado. Motivo: {dto.Reason}", supervisorId, cancellationToken);

        return await MapToResponseDtoAsync(ticket, cancellationToken);
    }

    public async Task<TicketResponseDto> ConsumeTicketAsync(
        Guid id,
        ConsumeTicketRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Request)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (ticket == null)
            throw new BusinessRuleViolationException(ErrorCodes.TicketNotFound, "El ticket no fue encontrado.", nameof(id));

        if (ticket.Status == TicketStatus.CONSUMIDO)
            throw new BusinessRuleViolationException(ErrorCodes.TicketConsumed, "El ticket ya fue consumido.", nameof(id));

        if (ticket.Status == TicketStatus.ANULADO)
            throw new BusinessRuleViolationException(ErrorCodes.TicketCancelled, "El ticket está anulado.", nameof(id));

        if (ticket.Status == TicketStatus.VENCIDO || DateTime.UtcNow > ticket.ExpiresAt)
        {
            ticket.Status = TicketStatus.VENCIDO;
            await _context.SaveChangesAsync(cancellationToken);
            await _inventoryService.ReleaseReservationAsync(ticket.Id, cancellationToken);
            throw new BusinessRuleViolationException(ErrorCodes.TicketExpired, "El ticket se encuentra vencido.", nameof(id));
        }

        if (ticket.Status != TicketStatus.ACTIVO)
            throw new BusinessRuleViolationException(ErrorCodes.BusinessRuleViolation, "El ticket no está activo.");

        // Transición a CONSUMIDO
        ticket.Status = TicketStatus.CONSUMIDO;
        ticket.ConsumedAt = DateTime.UtcNow;
        ticket.ConsumedByStationId = dto.StationId;

        // Liberar la reserva activa (el módulo de despacho descuenta el físico)
        await _inventoryService.ReleaseReservationAsync(ticket.Id, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogEventAsync("TICKET_CONSUMED", nameof(Ticket), ticket.Id.ToString(), $"Ticket consumido en estación {dto.StationId}", dto.DispatcherId, cancellationToken);

        return await MapToResponseDtoAsync(ticket, cancellationToken);
    }

    private async Task<TicketResponseDto> MapToResponseDtoAsync(Ticket ticket, CancellationToken cancellationToken)
    {
        var emp = await _masterDataService.GetEmployeeByIdAsync(ticket.Request.EmployeeId, cancellationToken);
        var veh = await _masterDataService.GetVehicleByIdAsync(ticket.Request.VehicleId, cancellationToken);
        var fuel = await _masterDataService.GetFuelTypeByIdAsync(ticket.Request.FuelTypeId, cancellationToken);

        return new TicketResponseDto
        {
            Id = ticket.Id,
            Number = ticket.Number,
            RequestId = ticket.RequestId,
            Employee = new TicketEmployeeDto(emp?.FullName ?? "N/A", emp?.EmployeeNumber ?? "N/A"),
            Vehicle = new TicketVehicleDto(veh?.Id, veh?.Plate ?? "N/A"),
            FuelType = fuel?.Name ?? "N/A",
            AuthorizedQuantity = ticket.AuthorizedQuantity,
            ExpiresAt = ticket.ExpiresAt,
            Status = ticket.Status.ToString(),
            QrToken = $"TKT-{ticket.Id.ToString()[..8].ToUpper()}", // Token de presentación seguro (nunca secretos)
            CreatedAt = ticket.CreatedAt
        };
    }
}
