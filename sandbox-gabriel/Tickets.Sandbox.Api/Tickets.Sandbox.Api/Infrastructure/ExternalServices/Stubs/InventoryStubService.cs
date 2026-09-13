using System.Collections.Concurrent;
using Tickets.Sandbox.Api.Application.Contracts;

namespace Tickets.Sandbox.Api.Infrastructure.ExternalServices.Stubs;

/// <summary>
/// TODO-INTEGRACIÓN: Stub temporal que simula el módulo de Inventario (mantenido por José Enrique).
/// Maneja existencias y reservas atómicas de combustible asociadas a tickets.
/// </summary>
public class InventoryStubService : IInventoryService
{
    // Inventario físico por FuelTypeId
    private readonly ConcurrentDictionary<Guid, decimal> _physicalStock = new();
    
    // Reservas activas: TicketId -> (FuelTypeId, Quantity)
    private readonly ConcurrentDictionary<Guid, (Guid FuelTypeId, decimal Quantity)> _activeReservations = new();

    private readonly object _lock = new();

    public InventoryStubService()
    {
        // Sembrar stock físico por defecto para pruebas
        _physicalStock[MasterDataStubService.DefaultFuelTypeId] = 10000.00m;
        _physicalStock[MasterDataStubService.AlternativeFuelTypeId] = 5000.00m;
    }

    public void SetPhysicalStock(Guid fuelTypeId, decimal quantity)
    {
        _physicalStock[fuelTypeId] = quantity;
    }

    public Task<decimal> GetAvailableInventoryAsync(Guid fuelTypeId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _physicalStock.TryGetValue(fuelTypeId, out var physical);
            
            var reserved = _activeReservations.Values
                .Where(r => r.FuelTypeId == fuelTypeId)
                .Sum(r => r.Quantity);

            var available = Math.Max(0, physical - reserved);
            return Task.FromResult(available);
        }
    }

    public Task<bool> ReserveInventoryAsync(Guid fuelTypeId, decimal quantity, Guid ticketId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _physicalStock.TryGetValue(fuelTypeId, out var physical);
            
            var reserved = _activeReservations.Values
                .Where(r => r.FuelTypeId == fuelTypeId)
                .Sum(r => r.Quantity);

            var available = physical - reserved;
            if (available < quantity)
            {
                return Task.FromResult(false);
            }

            _activeReservations[ticketId] = (fuelTypeId, quantity);
            return Task.FromResult(true);
        }
    }

    public Task<bool> ReleaseReservationAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_activeReservations.TryRemove(ticketId, out _));
        }
    }
}
