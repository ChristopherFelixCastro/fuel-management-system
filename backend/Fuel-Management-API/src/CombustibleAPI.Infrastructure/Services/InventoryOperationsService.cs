using CombustibleAPI.Application.Dtos.Inventory;
using CombustibleAPI.Application.Exceptions;
using CombustibleAPI.Application.Interfaces;
using CombustibleAPI.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;
using CombustibleAPI.Application.Dtos.Dispatches;

namespace CombustibleAPI.Infrastructure.Services;

/// <summary>Mutaciones de inventario delegadas exclusivamente a funciones PostgreSQL.</summary>
public sealed class InventoryOperationsService(AppDbContext context) : IInventoryOperationsService
{
    public Task<InventoryOperationResultDto> CreateReceptionAsync(CreateReceptionRequestDto r, Guid userId, CancellationToken ct) =>
        ExecuteIdAsync("SELECT fn_registrar_recepcion(@proveedor,@tanque,@usuario,@factura,@cantidad,@fecha,@obs)", c => {
            c.Parameters.AddWithValue("proveedor", r.ProveedorId); c.Parameters.AddWithValue("tanque", r.TanqueId); c.Parameters.AddWithValue("usuario", userId);
            c.Parameters.AddWithValue("factura", r.NumeroFactura); c.Parameters.AddWithValue("cantidad", r.Cantidad); c.Parameters.AddWithValue("fecha", r.FechaRecepcion); c.Parameters.AddWithValue("obs", (object?)r.Observaciones ?? DBNull.Value);
        }, "REGISTRADA", ct);

    public Task<InventoryOperationResultDto> CreateTransferAsync(CreateTransferRequestDto r, Guid userId, CancellationToken ct) =>
        ExecuteIdAsync("SELECT fn_registrar_transferencia(@origen,@destino,@usuario,@cantidad,@obs)", c => {
            c.Parameters.AddWithValue("origen", r.TanqueOrigenId); c.Parameters.AddWithValue("destino", r.TanqueDestinoId); c.Parameters.AddWithValue("usuario", userId); c.Parameters.AddWithValue("cantidad", r.Cantidad); c.Parameters.AddWithValue("obs", (object?)r.Observaciones ?? DBNull.Value);
        }, "REGISTRADA", ct);

    public async Task<InventoryOperationResultDto> ReportAdjustmentAsync(ReportAdjustmentRequestDto r, Guid userId, Guid stationId, CancellationToken ct)
    {
        if (!await context.Tanques.AsNoTracking().AnyAsync(x=>x.Id==r.TanqueId && x.EstacionId==stationId && x.Activo,ct)) throw ApiException.NoAutorizadoParaEstacion();
        var connection = (NpgsqlConnection)context.Database.GetDbConnection(); await context.Database.OpenConnectionAsync(ct);
        await using var cmd = new NpgsqlCommand("SELECT fn_reportar_ajuste(@tanque,@usuario,@conteo,@motivo,@obs)", connection);
        cmd.Parameters.AddWithValue("tanque", r.TanqueId); cmd.Parameters.AddWithValue("usuario", userId); cmd.Parameters.AddWithValue("conteo", r.ConteoFisico); cmd.Parameters.AddWithValue("motivo", r.Motivo); cmd.Parameters.AddWithValue("obs", (object?)r.Observaciones ?? DBNull.Value);
        try { var id = (Guid)(await cmd.ExecuteScalarAsync(ct) ?? throw ApiException.BusinessRule("AJUSTE_FALLIDO", "No se pudo reportar el ajuste.")); return new() { Id = id, Estado = "PENDIENTE" }; }
        catch (PostgresException ex) { throw ApiException.BusinessRule("AJUSTE_FALLIDO", ex.MessageText); }
        finally { await context.Database.CloseConnectionAsync(); }
    }

    public Task ApproveAdjustmentAsync(Guid id, Guid userId, CancellationToken ct) => ExecuteVoidAsync("SELECT fn_aprobar_ajuste(@id,@usuario)", id, userId, ct);
    public Task RejectAdjustmentAsync(Guid id, Guid userId, string reason, CancellationToken ct) => ExecuteVoidAsync("SELECT fn_rechazar_ajuste(@id,@usuario,@motivo)", id, userId, ct, reason);

    public async Task<PaginatedList<InventoryMovementDto>> GetMovementsAsync(InventoryQueryDto f, Guid? forcedStationId, CancellationToken ct)
    {
        var page=Math.Max(1,f.Page); var size=Math.Clamp(f.PageSize,1,100);
        var q=context.MovimientosInventario.AsNoTracking().Include(x=>x.Tanque).AsQueryable();
        var station=forcedStationId ?? f.EstacionId; if(station.HasValue)q=q.Where(x=>x.Tanque.EstacionId==station);
        if(f.TanqueId.HasValue)q=q.Where(x=>x.TanqueId==f.TanqueId); if(!string.IsNullOrWhiteSpace(f.Tipo))q=q.Where(x=>x.TipoMovimiento==f.Tipo);
        if(f.Desde.HasValue)q=q.Where(x=>x.FechaMovimiento>=f.Desde); if(f.Hasta.HasValue)q=q.Where(x=>x.FechaMovimiento<=f.Hasta);
        var total=await q.CountAsync(ct); var items=await q.OrderByDescending(x=>x.FechaMovimiento).Skip((page-1)*size).Take(size).Select(x=>new InventoryMovementDto{Id=x.Id,TanqueId=x.TanqueId,TanqueCodigo=x.Tanque.Codigo,EstacionId=x.Tanque.EstacionId,Tipo=x.TipoMovimiento,Cantidad=x.Cantidad,SaldoAnterior=x.SaldoAnterior,SaldoPosterior=x.SaldoPosterior,Fecha=x.FechaMovimiento,Observaciones=x.Observaciones}).ToListAsync(ct);
        return new(items,total,page,size);
    }

    public async Task<PaginatedList<AdjustmentDto>> GetAdjustmentsAsync(InventoryQueryDto f, Guid? forcedUserId, Guid? forcedStationId, CancellationToken ct)
    {
        var page=Math.Max(1,f.Page); var size=Math.Clamp(f.PageSize,1,100); var c=(NpgsqlConnection)context.Database.GetDbConnection(); await context.Database.OpenConnectionAsync(ct);
        var where=new StringBuilder(" WHERE 1=1"); var args=new List<NpgsqlParameter>();
        void Add(string clause,string name,object value){where.Append(clause);args.Add(new(name,value));}
        if(forcedUserId.HasValue)Add(" AND a.reportado_por_usuario_id=@user","user",forcedUserId.Value);
        var station=forcedStationId??f.EstacionId;if(station.HasValue)Add(" AND t.estacion_id=@station","station",station.Value);
        if(f.TanqueId.HasValue)Add(" AND a.tanque_id=@tank","tank",f.TanqueId.Value);if(!string.IsNullOrWhiteSpace(f.Estado))Add(" AND a.estado=@state","state",f.Estado!);
        await using var count=new NpgsqlCommand("SELECT COUNT(*) FROM ajuste_inventario a JOIN tanque t ON t.id=a.tanque_id"+where,c);count.Parameters.AddRange(args.ToArray());var total=Convert.ToInt32(await count.ExecuteScalarAsync(ct));
        var sql="SELECT a.id,a.tanque_id,t.codigo,t.estacion_id,a.reportado_por_usuario_id,a.conteo_fisico,a.tipo_ajuste,a.cantidad,a.motivo,a.estado,a.fecha_reporte,a.fecha_revision,a.motivo_rechazo,a.observaciones FROM ajuste_inventario a JOIN tanque t ON t.id=a.tanque_id"+where+" ORDER BY a.fecha_reporte DESC LIMIT @limit OFFSET @offset";
        await using var cmd=new NpgsqlCommand(sql,c);foreach(var p in args)cmd.Parameters.Add(new NpgsqlParameter(p.ParameterName,p.Value));cmd.Parameters.AddWithValue("limit",size);cmd.Parameters.AddWithValue("offset",(page-1)*size);var items=new List<AdjustmentDto>();await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))items.Add(new(){Id=r.GetGuid(0),TanqueId=r.GetGuid(1),TanqueCodigo=r.GetString(2),EstacionId=r.GetGuid(3),ReportadoPorUsuarioId=r.GetGuid(4),ConteoFisico=r.IsDBNull(5)?0:r.GetDecimal(5),Tipo=r.GetString(6),Cantidad=r.GetDecimal(7),Motivo=r.GetString(8),Estado=r.GetString(9),FechaReporte=r.GetDateTime(10),FechaRevision=r.IsDBNull(11)?null:r.GetDateTime(11),MotivoRechazo=r.IsDBNull(12)?null:r.GetString(12),Observaciones=r.IsDBNull(13)?null:r.GetString(13)});
        await context.Database.CloseConnectionAsync(); return new(items,total,page,size);
    }

    private async Task<InventoryOperationResultDto> ExecuteIdAsync(string sql, Action<NpgsqlCommand> bind, string state, CancellationToken ct) { var c=(NpgsqlConnection)context.Database.GetDbConnection(); await context.Database.OpenConnectionAsync(ct); await using var cmd=new NpgsqlCommand(sql,c); bind(cmd); try { var id=(Guid)(await cmd.ExecuteScalarAsync(ct) ?? throw ApiException.BusinessRule("INVENTORY_OPERATION_FAILED","La operación no devolvió identificador.")); return new(){Id=id,Estado=state}; } catch(PostgresException ex){ throw ApiException.BusinessRule("INVENTORY_OPERATION_FAILED",ex.MessageText); } finally { await context.Database.CloseConnectionAsync(); } }
    private async Task ExecuteVoidAsync(string sql, Guid id, Guid userId, CancellationToken ct, string? reason=null) { var c=(NpgsqlConnection)context.Database.GetDbConnection(); await context.Database.OpenConnectionAsync(ct); await using var cmd=new NpgsqlCommand(sql,c); cmd.Parameters.AddWithValue("id",id); cmd.Parameters.AddWithValue("usuario",userId); if(reason is not null)cmd.Parameters.AddWithValue("motivo",reason); try { await cmd.ExecuteNonQueryAsync(ct); } catch(PostgresException ex){throw ApiException.BusinessRule("INVENTORY_OPERATION_FAILED",ex.MessageText);} finally { await context.Database.CloseConnectionAsync(); } }
}
