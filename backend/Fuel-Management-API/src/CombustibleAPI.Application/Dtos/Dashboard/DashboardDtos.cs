namespace CombustibleAPI.Application.Dtos.Dashboard;

public class DashboardFilterDto
{
    public Guid? StationId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class DashboardSummaryDto
{
    public int PendingRequests { get; set; }
    public int ActiveTickets { get; set; }
    public int LowInventoryTanks { get; set; }
    public int DispatchesToday { get; set; }
    public decimal GallonsDispatchedToday { get; set; }

    public List<DashboardInventoryByFuelDto> InventoryByFuel { get; set; } = [];
    public List<DashboardAlertDto> RecentAlerts { get; set; } = [];
    public List<DashboardConsumptionDto> ConsumptionByDepartment { get; set; } = [];
    public List<DashboardConsumptionDto> ConsumptionByVehicle { get; set; } = [];
}

public class DashboardInventoryByFuelDto
{
    public short FuelTypeId { get; set; }
    public string FuelTypeName { get; set; } = default!;
    public decimal CurrentStock { get; set; }
    public decimal TotalCapacity { get; set; }
    public decimal Percentage { get; set; }
}

public class DashboardAlertDto
{
    public Guid TankId { get; set; }
    public string TankCode { get; set; } = default!;
    public string? TankName { get; set; }
    public string StationName { get; set; } = default!;
    public string FuelTypeName { get; set; } = default!;
    public decimal CurrentStock { get; set; }
    public decimal CriticalLevel { get; set; }
    public string Message { get; set; } = default!;
}

public class DashboardConsumptionDto
{
    public string Name { get; set; } = default!;
    public decimal Gallons { get; set; }
}