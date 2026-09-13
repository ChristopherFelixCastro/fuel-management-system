using Microsoft.EntityFrameworkCore;
using Tickets.Sandbox.Api.Application.Common;
using Tickets.Sandbox.Api.Application.Contracts;
using Tickets.Sandbox.Api.Application.DTOs.Requests;
using Tickets.Sandbox.Api.Application.Interfaces;
using Tickets.Sandbox.Api.Domain.Entities;
using Tickets.Sandbox.Api.Domain.Enums;
using Tickets.Sandbox.Api.Domain.Exceptions;
using Tickets.Sandbox.Api.Infrastructure.Persistence;

namespace Tickets.Sandbox.Api.Application.Services;

public class RequestService : IRequestService
{
    private readonly AppDbContext _context;
    private readonly IMasterDataService _masterDataService;
    private readonly IInventoryService _inventoryService;
    private readonly ITicketSequenceService _sequenceService;
    private readonly IQrCodeService _qrCodeService;
    private readonly IPdfGeneratorService _pdfGeneratorService;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;

    public RequestService(
        AppDbContext context,
        IMasterDataService masterDataService,
        IInventoryService inventoryService,
        ITicketSequenceService sequenceService,
        IQrCodeService qrCodeService,
        IPdfGeneratorService pdfGeneratorService,
        INotificationService notificationService,
        IAuditService auditService)
    {
        _context = context;
        _masterDataService = masterDataService;
        _inventoryService = inventoryService;
        _sequenceService = sequenceService;
        _qrCodeService = qrCodeService;
        _pdfGeneratorService = pdfGeneratorService;
        _notificationService = notificationService;
        _auditService = auditService;
    }

    public async Task<RequestResponseDto> CreateRequestAsync(CreateRequestDto dto, string? userId = null, CancellationToken cancellationToken = default)
    {
        // 1. Validar existencia de entidades maestras
        var employee = await _masterDataService.GetEmployeeByIdAsync(dto.EmployeeId, cancellationToken);
        if (employee == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "El empleado especificado no existe.", nameof(dto.EmployeeId));

        var vehicle = await _masterDataService.GetVehicleByIdAsync(dto.VehicleId, cancellationToken);
        if (vehicle == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "El vehículo especificado no existe.", nameof(dto.VehicleId));

        var department = await _masterDataService.GetDepartmentByIdAsync(dto.DepartmentId, cancellationToken);
        if (department == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "El departamento especificado no existe.", nameof(dto.DepartmentId));

        var fuelType = await _masterDataService.GetFuelTypeByIdAsync(dto.FuelTypeId, cancellationToken);
        if (fuelType == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "El tipo de combustible especificado no existe.", nameof(dto.FuelTypeId));

        // 2. Regla RN-01/02: Coherencia Empleado <-> Departamento
        if (employee.DepartmentId != dto.DepartmentId)
        {
            throw new BusinessRuleViolationException(ErrorCodes.DepartmentIncoherent, "El empleado no pertenece al departamento indicado.", nameof(dto.DepartmentId));
        }

        // 3. Regla RN-01: Tipo de combustible debe coincidir con el del vehículo
        if (vehicle.FuelTypeId != dto.FuelTypeId)
        {
            throw new BusinessRuleViolationException(ErrorCodes.VehicleFuelMismatch, "El tipo de combustible no coincide con el asignado al vehículo.", nameof(dto.FuelTypeId));
        }

        // 4. Regla RN-01: Cantidad solicitada no debe superar la capacidad del tanque
        if (dto.RequestedQuantity > vehicle.TankCapacity)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.QuantityExceedsTankCapacity,
                $"La cantidad solicitada ({dto.RequestedQuantity:N2}) supera la capacidad máxima del tanque del vehículo ({vehicle.TankCapacity:N2}).",
                nameof(dto.RequestedQuantity));
        }

        // 5. Crear entidad Request
        var request = new Request
        {
            EmployeeId = dto.EmployeeId,
            VehicleId = dto.VehicleId,
            DepartmentId = dto.DepartmentId,
            FuelTypeId = dto.FuelTypeId,
            RequestedQuantity = dto.RequestedQuantity,
            RequestedFor = dto.RequestedFor,
            Source = dto.Source,
            Notes = dto.Notes,
            Status = RequestStatus.PENDIENTE,
            CreatedBy = userId ?? "SOLICITANTE"
        };

        _context.Requests.Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogEventAsync("REQUEST_CREATED", nameof(Request), request.Id.ToString(), $"Solicitud creada por cantidad {request.RequestedQuantity}", userId, cancellationToken);

        return await MapToResponseDtoAsync(request, cancellationToken);
    }

    public async Task<(List<RequestResponseDto> Items, int TotalCount)> GetRequestsAsync(
        RequestFilterDto filter,
        string? userRole = null,
        Guid? userEmployeeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Requests
            .Include(r => r.Ticket)
            .AsNoTracking()
            .AsQueryable();

        // Filtro por rol: SOLICITANTE solo ve sus solicitudes
        if (userRole == "SOLICITANTE" && userEmployeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == userEmployeeId.Value);
        }
        else if (filter.EmployeeId.HasValue)
        {
            query = query.Where(r => r.EmployeeId == filter.EmployeeId.Value);
        }

        if (filter.Status.HasValue)
            query = query.Where(r => r.Status == filter.Status.Value);

        if (filter.VehicleId.HasValue)
            query = query.Where(r => r.VehicleId == filter.VehicleId.Value);

        if (filter.DepartmentId.HasValue)
            query = query.Where(r => r.DepartmentId == filter.DepartmentId.Value);

        if (filter.From.HasValue)
            query = query.Where(r => r.CreatedAt >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(r => r.CreatedAt <= filter.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = new List<RequestResponseDto>();
        foreach (var req in requests)
        {
            dtos.Add(await MapToResponseDtoAsync(req, cancellationToken));
        }

        return (dtos, totalCount);
    }

    public async Task<RequestResponseDto?> GetRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _context.Requests
            .Include(r => r.Ticket)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return request == null ? null : await MapToResponseDtoAsync(request, cancellationToken);
    }

    public async Task<RequestResponseDto> UpdateRequestAsync(
        Guid id,
        UpdateRequestDto dto,
        string? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Requests
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "La solicitud no fue encontrada.", nameof(id));

        // Solo editable en BORRADOR o PENDIENTE
        if (request.Status != RequestStatus.BORRADOR && request.Status != RequestStatus.PENDIENTE)
        {
            throw new BusinessRuleViolationException(ErrorCodes.RequestNotEditable, "La solicitud no se puede editar en su estado actual.");
        }

        if (dto.RequestedQuantity.HasValue)
        {
            var vehicle = await _masterDataService.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle != null && dto.RequestedQuantity.Value > vehicle.TankCapacity)
            {
                throw new BusinessRuleViolationException(ErrorCodes.QuantityExceedsTankCapacity, "La cantidad solicitada supera la capacidad del tanque del vehículo.", nameof(dto.RequestedQuantity));
            }
            request.RequestedQuantity = dto.RequestedQuantity.Value;
        }

        if (dto.FuelTypeId.HasValue)
        {
            var vehicle = await _masterDataService.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
            if (vehicle != null && vehicle.FuelTypeId != dto.FuelTypeId.Value)
            {
                throw new BusinessRuleViolationException(ErrorCodes.VehicleFuelMismatch, "El tipo de combustible no coincide con el del vehículo.", nameof(dto.FuelTypeId));
            }
            request.FuelTypeId = dto.FuelTypeId.Value;
        }

        if (dto.RequestedFor.HasValue)
            request.RequestedFor = dto.RequestedFor.Value;

        if (dto.Notes != null)
            request.Notes = dto.Notes;

        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogEventAsync("REQUEST_UPDATED", nameof(Request), request.Id.ToString(), "Solicitud modificada", userId, cancellationToken);

        return await MapToResponseDtoAsync(request, cancellationToken);
    }

    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, SemaphoreSlim> _requestLocks = new();

    public async Task<RequestResponseDto> ApproveRequestAsync(
        Guid id,
        ApproveRequestDto dto,
        string? supervisorId = null,
        CancellationToken cancellationToken = default)
    {
        var requestLock = _requestLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        await requestLock.WaitAsync(cancellationToken);
        try
        {
            var request = await _context.Requests
                .Include(r => r.Ticket)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (request == null)
                throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "La solicitud no fue encontrada.", nameof(id));

            // 1. Debe estar en estado PENDIENTE
            if (request.Status != RequestStatus.PENDIENTE)
            {
                throw new BusinessRuleViolationException(ErrorCodes.ConcurrencyConflict, "Conflicto de concurrencia: la solicitud ya no se encuentra en estado PENDIENTE.");
            }

            // 2. Unicidad: no debe existir un ticket previo
            if (request.Ticket != null)
            {
                throw new BusinessRuleViolationException(ErrorCodes.TicketAlreadyExists, "Ya existe un ticket emitido para esta solicitud.");
            }

        // 3. Cantidad autorizada no puede superar la solicitada ni la capacidad del tanque
        if (dto.AuthorizedQuantity > request.RequestedQuantity)
        {
            throw new BusinessRuleViolationException(ErrorCodes.QuantityExceedsTankCapacity, "La cantidad autorizada no puede superar la cantidad solicitada.", nameof(dto.AuthorizedQuantity));
        }

        var vehicle = await _masterDataService.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
        if (vehicle != null && dto.AuthorizedQuantity > vehicle.TankCapacity)
        {
            throw new BusinessRuleViolationException(ErrorCodes.QuantityExceedsTankCapacity, "La cantidad autorizada supera la capacidad del tanque del vehículo.", nameof(dto.AuthorizedQuantity));
        }

        // 4. Vigencia válida
        if (dto.ExpiresAt <= DateTime.UtcNow)
        {
            throw new BusinessRuleViolationException(ErrorCodes.ExpirationInvalid, "La fecha de vencimiento debe ser posterior a la fecha y hora actual.", nameof(dto.ExpiresAt));
        }

        // 5. Disponibilidad real en Inventario (existencia física - reservas activas)
        var availableInventory = await _inventoryService.GetAvailableInventoryAsync(request.FuelTypeId, cancellationToken);
        if (availableInventory < dto.AuthorizedQuantity)
        {
            throw new BusinessRuleViolationException(
                ErrorCodes.InsufficientAvailableInventory,
                $"Inventario disponible insuficiente ({availableInventory:N2} galones disponibles vs {dto.AuthorizedQuantity:N2} solicitados).");
        }

        // 6. Transacción atómica: Reserva + Generación de Consecutivo + Emisión de Ticket + Aprobación
        var ticketId = Guid.NewGuid();

        var reserved = await _inventoryService.ReserveInventoryAsync(request.FuelTypeId, dto.AuthorizedQuantity, ticketId, cancellationToken);
        if (!reserved)
        {
            throw new BusinessRuleViolationException(ErrorCodes.InsufficientAvailableInventory, "No se pudo realizar la reserva de inventario para el ticket.");
        }

        var year = DateTime.UtcNow.Year;
        var ticketNumber = await _sequenceService.NextTicketNumberAsync(year, cancellationToken);
        var qrSecurity = _qrCodeService.GenerateQrSecurityData(ticketId);

        var ticket = new Ticket
        {
            Id = ticketId,
            Number = ticketNumber,
            RequestId = request.Id,
            QrTokenHash = qrSecurity.TokenHash,
            Signature = qrSecurity.Signature,
            AuthorizedQuantity = dto.AuthorizedQuantity,
            ExpiresAt = dto.ExpiresAt,
            Status = TicketStatus.ACTIVO,
            CreatedAt = DateTime.UtcNow
        };

        request.Status = RequestStatus.APROBADA;
        request.ApprovedAt = DateTime.UtcNow;
        request.AuthorApprovedBy = supervisorId ?? "SUPERVISOR";
        if (!string.IsNullOrWhiteSpace(dto.Notes))
        {
            request.Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"[Aprobación]: {dto.Notes}"
                : $"{request.Notes}\n[Aprobación]: {dto.Notes}";
        }

        _context.Tickets.Add(ticket);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Liberar la reserva en caso de colisión concurrente
            await _inventoryService.ReleaseReservationAsync(ticketId, cancellationToken);
            throw new BusinessRuleViolationException(
                ErrorCodes.ConcurrencyConflict,
                "Conflicto de concurrencia: la solicitud fue aprobada o modificada simultáneamente por otro proceso.");
        }

        // 7. Generación de PDF y Notificación Resiliente
        try
        {
            var emp = await _masterDataService.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken);
            var dept = await _masterDataService.GetDepartmentByIdAsync(request.DepartmentId, cancellationToken);
            var fuel = await _masterDataService.GetFuelTypeByIdAsync(request.FuelTypeId, cancellationToken);

            var qrBytes = _qrCodeService.GenerateQrImagePng(qrSecurity.QrPayload);

            var pdfModel = new TicketPdfModel(
                ticket.Number,
                ticket.Status.ToString(),
                emp?.FullName ?? "N/A",
                emp?.EmployeeNumber ?? "N/A",
                vehicle?.Plate ?? "N/A",
                dept?.Name ?? "N/A",
                fuel?.Name ?? "N/A",
                ticket.AuthorizedQuantity,
                ticket.CreatedAt,
                ticket.ExpiresAt,
                qrBytes
            );

            var pdfBytes = _pdfGeneratorService.GenerateTicketPdf(pdfModel);

            // Disparar notificaciones (no bloqueante / resiliente)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.NotifyTicketIssuedAsync(ticket, request, pdfBytes, CancellationToken.None);
                }
                catch
                {
                    // Manejado dentro de NotificationService
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Error en generación auxiliar no cancela la transacción del ticket emitido
            await _auditService.LogEventAsync(ErrorCodes.IntegrationFailure, nameof(Ticket), ticket.Id.ToString(), $"Fallo auxiliar al preparar notificación/PDF: {ex.Message}", "SYSTEM", cancellationToken);
        }

        await _auditService.LogEventAsync("REQUEST_APPROVED", nameof(Request), request.Id.ToString(), $"Solicitud aprobada y Ticket {ticket.Number} emitido", supervisorId, cancellationToken);

        return await MapToResponseDtoAsync(request, cancellationToken);
    }
    finally
    {
        requestLock.Release();
    }
}

    public async Task<RequestResponseDto> RejectRequestAsync(
        Guid id,
        RejectRequestDto dto,
        string? supervisorId = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Requests
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "La solicitud no fue encontrada.", nameof(id));

        if (request.Status != RequestStatus.PENDIENTE)
        {
            throw new BusinessRuleViolationException(ErrorCodes.RequestNotPending, "Solo las solicitudes en estado PENDIENTE pueden ser rechazadas.");
        }

        request.Status = RequestStatus.RECHAZADA;
        request.RejectionReason = dto.Reason;
        request.RejectedAt = DateTime.UtcNow;
        request.AuthorApprovedBy = supervisorId ?? "SUPERVISOR";
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogEventAsync("REQUEST_REJECTED", nameof(Request), request.Id.ToString(), $"Solicitud rechazada. Motivo: {dto.Reason}", supervisorId, cancellationToken);

        return await MapToResponseDtoAsync(request, cancellationToken);
    }

    public async Task<RequestResponseDto> CancelRequestAsync(
        Guid id,
        CancelRequestDto? dto = null,
        string? userId = null,
        string? userRole = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Requests
            .Include(r => r.Ticket)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (request == null)
            throw new BusinessRuleViolationException(ErrorCodes.ResourceNotFound, "La solicitud no fue encontrada.", nameof(id));

        // Solo se puede cancelar ANTES de tener ticket y si está en BORRADOR o PENDIENTE
        if (request.Ticket != null || (request.Status != RequestStatus.PENDIENTE && request.Status != RequestStatus.BORRADOR))
        {
            throw new BusinessRuleViolationException(ErrorCodes.RequestNotEditable, "No se puede cancelar una solicitud que ya tiene un ticket emitido o ha sido procesada.");
        }

        request.Status = RequestStatus.CANCELADA;
        request.CancelledAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(dto?.Reason))
        {
            request.RejectionReason = $"[Cancelada]: {dto.Reason}";
        }
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogEventAsync("REQUEST_CANCELLED", nameof(Request), request.Id.ToString(), "Solicitud cancelada por el usuario", userId, cancellationToken);

        return await MapToResponseDtoAsync(request, cancellationToken);
    }

    private async Task<RequestResponseDto> MapToResponseDtoAsync(Request request, CancellationToken cancellationToken)
    {
        var emp = await _masterDataService.GetEmployeeByIdAsync(request.EmployeeId, cancellationToken);
        var veh = await _masterDataService.GetVehicleByIdAsync(request.VehicleId, cancellationToken);
        var dept = await _masterDataService.GetDepartmentByIdAsync(request.DepartmentId, cancellationToken);
        var fuel = await _masterDataService.GetFuelTypeByIdAsync(request.FuelTypeId, cancellationToken);

        return new RequestResponseDto
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            EmployeeName = emp?.FullName,
            EmployeeNumber = emp?.EmployeeNumber,
            VehicleId = request.VehicleId,
            VehiclePlate = veh?.Plate,
            DepartmentId = request.DepartmentId,
            DepartmentName = dept?.Name,
            FuelTypeId = request.FuelTypeId,
            FuelTypeName = fuel?.Name,
            RequestedQuantity = request.RequestedQuantity,
            RequestedFor = request.RequestedFor,
            Source = request.Source.ToString(),
            Notes = request.Notes,
            Status = request.Status.ToString(),
            AuthorApprovedBy = request.AuthorApprovedBy,
            RejectionReason = request.RejectionReason,
            CreatedAt = request.CreatedAt,
            ApprovedAt = request.ApprovedAt,
            RejectedAt = request.RejectedAt,
            CancelledAt = request.CancelledAt,
            TicketId = request.Ticket?.Id,
            TicketNumber = request.Ticket?.Number
        };
    }
}
