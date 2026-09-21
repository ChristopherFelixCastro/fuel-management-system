import React, { useEffect, useState } from 'react';
import {
  Truck,
  Plus,
  Search,
  Edit2,
  Power,
  Fuel,
  X
} from 'lucide-react';
import { Vehicle, Department, FuelType, CreateVehicleDto } from '../types';
import { VehicleService, DepartmentService, CatalogService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';
import { StatusBadge } from '../components/common/Badge';
import { ConfirmModal } from '../components/common/ConfirmModal';

export const Vehicles: React.FC = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole(['ADMINISTRADOR']);
  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [fuelTypes, setFuelTypes] = useState<FuelType[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [deptFilter, setDeptFilter] = useState('');
  const [fuelFilter, setFuelFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  // Modal / Form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<Vehicle | null>(null);
  const [formData, setFormData] = useState<CreateVehicleDto>({
    plate: '',
    assetNumber: '',
    brand: '',
    model: '',
    year: new Date().getFullYear(),
    departmentId: '',
    fuelTypeId: '',
    tankCapacity: 20,
    currentOdometer: 0,
  });

  // Feedback & confirm modal
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [vehicleToToggle, setVehicleToToggle] = useState<Vehicle | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [vehRes, deptRes, fuelRes] = await Promise.all([
        VehicleService.getAll(),
        DepartmentService.getAll(),
        CatalogService.getFuelTypes(),
      ]);
      setVehicles(vehRes.data);
      setDepartments(deptRes.data);
      setFuelTypes(fuelRes.data);
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cargar vehículos' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenCreate = () => {
    setEditingVehicle(null);
    setFormData({
      plate: '',
      assetNumber: '',
      brand: '',
      model: '',
      year: new Date().getFullYear(),
      departmentId: departments[0]?.id || '',
      fuelTypeId: fuelTypes[0]?.id || '',
      tankCapacity: 20,
      currentOdometer: 0,
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (veh: Vehicle) => {
    setEditingVehicle(veh);
    setFormData({
      plate: veh.plate,
      assetNumber: veh.assetNumber || '',
      brand: veh.brand || '',
      model: veh.model || '',
      year: veh.year || new Date().getFullYear(),
      departmentId: veh.departmentId,
      fuelTypeId: veh.fuelTypeId,
      tankCapacity: veh.tankCapacity,
      currentOdometer: veh.currentOdometer,
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setAlert(null);

    // Validaciones de contrato
    if (!formData.plate.trim()) {
      setAlert({ type: 'error', message: 'La placa es un campo obligatorio.' });
      return;
    }
    if (!formData.fuelTypeId) {
      setAlert({ type: 'error', message: 'El tipo de combustible es obligatorio (FUEL_TYPE_REQUIRED).' });
      return;
    }
    if (formData.tankCapacity <= 0) {
      setAlert({ type: 'error', message: 'La capacidad del tanque debe ser mayor a 0 galones (INVALID_TANK_CAPACITY).' });
      return;
    }
    if (formData.currentOdometer < 0) {
      setAlert({ type: 'error', message: 'El odómetro no puede ser negativo (ODOMETER_INVALID).' });
      return;
    }
    if (editingVehicle && formData.currentOdometer < editingVehicle.currentOdometer) {
      setAlert({
        type: 'error',
        message: `El odómetro no puede ser menor al kilometraje registrado anteriormente (${editingVehicle.currentOdometer} km).`
      });
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingVehicle) {
        await VehicleService.update(editingVehicle.id, formData);
        setAlert({ type: 'success', message: 'Vehículo actualizado correctamente.' });
      } else {
        await VehicleService.create(formData);
        setAlert({ type: 'success', message: 'Vehículo registrado exitosamente.' });
      }
      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al guardar vehículo' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmToggle = async () => {
    if (!vehicleToToggle) return;
    setIsSubmitting(true);
    try {
      await VehicleService.deactivate(vehicleToToggle.id);
      setAlert({
        type: 'success',
        message: 'Vehículo desactivado correctamente.',
      });
      setVehicleToToggle(null);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cambiar estado' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const filteredVehicles = vehicles.filter(v => {
    const term = searchTerm.toLowerCase();
    const matchSearch =
      v.plate.toLowerCase().includes(term) ||
      (v.assetNumber && v.assetNumber.toLowerCase().includes(term)) ||
      (v.brand && v.brand.toLowerCase().includes(term)) ||
      (v.model && v.model.toLowerCase().includes(term));
    const matchDept = !deptFilter || v.departmentId === deptFilter;
    const matchFuel = !fuelFilter || v.fuelTypeId === fuelFilter;
    const matchStatus =
      !statusFilter ||
      (statusFilter === 'active' && v.isActive) ||
      (statusFilter === 'inactive' && !v.isActive);

    return matchSearch && matchDept && matchFuel && matchStatus;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Gestión de Vehículos y Flota
          </h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Control de unidades móviles, tipos de combustible autorizados y odómetros
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={handleOpenCreate}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition self-start sm:self-auto"
          >
            <Plus className="w-4 h-4" />
            <span>Nuevo Vehículo</span>
          </button>
        )}
      </div>

      {alert && (
        <AlertBanner
          type={alert.type}
          message={alert.message}
          onClose={() => setAlert(null)}
        />
      )}

      {/* Filters Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex flex-col md:flex-row gap-3">
        <div className="flex-1 relative">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
          <input
            type="text"
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            placeholder="Buscar por placa, ficha, marca o modelo..."
            className="w-full text-xs pl-9 pr-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          />
        </div>

        <div className="w-full md:w-48">
          <select
            value={deptFilter}
            onChange={e => setDeptFilter(e.target.value)}
            className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          >
            <option value="">Todos los Departamentos</option>
            {departments.map(d => (
              <option key={d.id} value={d.id}>
                {d.name}
              </option>
            ))}
          </select>
        </div>

        <div className="w-full md:w-44">
          <select
            value={fuelFilter}
            onChange={e => setFuelFilter(e.target.value)}
            className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          >
            <option value="">Todos los Combustibles</option>
            {fuelTypes.map(f => (
              <option key={f.id} value={f.id}>
                {f.name}
              </option>
            ))}
          </select>
        </div>

        <div className="w-full md:w-36">
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
            className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          >
            <option value="">Todos</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
        {isLoading ? (
          <LoadingSpinner message="Cargando flota de vehículos..." />
        ) : filteredVehicles.length === 0 ? (
          <div className="p-8 text-center text-slate-500 text-sm">
            No se encontraron vehículos registrados con esos criterios.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Placa / Ficha</th>
                  <th className="py-3 px-4">Vehículo</th>
                  <th className="py-3 px-4">Departamento</th>
                  <th className="py-3 px-4">Combustible Exigido</th>
                  <th className="py-3 px-4 text-right">Capacidad Tanque</th>
                  <th className="py-3 px-4 text-right">Odómetro Actual</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  {isAdmin && <th className="py-3 px-4 text-right">Acciones</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredVehicles.map(v => (
                  <tr key={v.id} className="hover:bg-slate-50 transition">
                    <td className="py-3 px-4">
                      <div className="font-mono font-bold text-slate-900 flex items-center gap-1.5">
                        <Truck className="w-4 h-4 text-[#087e8b]" />
                        <span>{v.plate}</span>
                      </div>
                      {v.assetNumber && (
                        <span className="text-[10px] text-slate-500 font-mono">
                          Ficha: {v.assetNumber}
                        </span>
                      )}
                    </td>
                    <td className="py-3 px-4">
                      <div className="font-medium text-slate-800">
                        {v.brand} {v.model}
                      </div>
                      {v.year && <span className="text-xs text-slate-500">Año {v.year}</span>}
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-700">
                      {v.departmentName}
                    </td>
                    <td className="py-3 px-4 text-xs">
                      <span className="inline-flex items-center gap-1 font-semibold text-slate-800 bg-slate-100 px-2 py-0.5 rounded-md border border-slate-200">
                        <Fuel className="w-3 h-3 text-[#087e8b]" />
                        {v.fuelTypeName}
                      </span>
                    </td>
                    <td className="py-3 px-4 text-right font-mono font-medium text-slate-700">
                      {v.tankCapacity} gal
                    </td>
                    <td className="py-3 px-4 text-right font-mono text-slate-900 font-bold">
                      {v.currentOdometer.toLocaleString()} km
                    </td>
                    <td className="py-3 px-4 text-center">
                      <StatusBadge isActive={v.isActive} />
                    </td>
                    {isAdmin && (
                      <td className="py-3 px-4 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => handleOpenEdit(v)}
                            title="Editar vehículo"
                            className="p-1.5 text-slate-500 hover:text-[#087e8b] hover:bg-slate-100 rounded-lg transition"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => setVehicleToToggle(v)}
                            title="Desactivar vehículo"
                            className={`p-1.5 rounded-lg transition ${
                              v.isActive ? 'text-slate-500 hover:text-amber-600 hover:bg-amber-50' : 'hidden'
                            }`}
                          >
                            <Power className="w-4 h-4" />
                          </button>
                        </div>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Modal Crear / Editar */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-xs">
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full border border-slate-200 overflow-hidden animate-fade-in">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                {editingVehicle ? 'Editar Vehículo' : 'Registrar Nuevo Vehículo'}
              </h3>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-md"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Placa Única *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.plate}
                    onChange={e => setFormData({ ...formData, plate: e.target.value.toUpperCase() })}
                    placeholder="ej. L345678"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden uppercase font-mono font-bold"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Número de Activo / Ficha
                  </label>
                  <input
                    type="text"
                    value={formData.assetNumber || ''}
                    onChange={e => setFormData({ ...formData, assetNumber: e.target.value.toUpperCase() })}
                    placeholder="ej. CAM-05"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden uppercase"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="sm:col-span-1">
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Marca
                  </label>
                  <input
                    type="text"
                    value={formData.brand || ''}
                    onChange={e => setFormData({ ...formData, brand: e.target.value })}
                    placeholder="ej. Toyota"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>

                <div className="sm:col-span-1">
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Modelo
                  </label>
                  <input
                    type="text"
                    value={formData.model || ''}
                    onChange={e => setFormData({ ...formData, model: e.target.value })}
                    placeholder="ej. Hilux"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>

                <div className="sm:col-span-1">
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Año
                  </label>
                  <input
                    type="number"
                    min="1990"
                    max={new Date().getFullYear() + 1}
                    value={formData.year || ''}
                    onChange={e => setFormData({ ...formData, year: Number(e.target.value) })}
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Departamento Asignado *
                  </label>
                  <select
                    required
                    value={formData.departmentId}
                    onChange={e => setFormData({ ...formData, departmentId: e.target.value })}
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  >
                    <option value="">Seleccione departamento...</option>
                    {departments.map(d => (
                      <option key={d.id} value={d.id}>
                        {d.name}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Tipo de Combustible Requerido *
                  </label>
                  <select
                    required
                    value={formData.fuelTypeId}
                    onChange={e => setFormData({ ...formData, fuelTypeId: e.target.value })}
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden font-medium"
                  >
                    <option value="">Seleccione combustible...</option>
                    {fuelTypes.map(f => (
                      <option key={f.id} value={f.id}>
                        {f.name}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Capacidad del Tanque (Galones) *
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    min="0.1"
                    required
                    value={formData.tankCapacity}
                    onChange={e => setFormData({ ...formData, tankCapacity: parseFloat(e.target.value) || 0 })}
                    placeholder="ej. 25.0"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden font-mono"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Odómetro Actual (Km) *
                  </label>
                  <input
                    type="number"
                    step="0.1"
                    min="0"
                    required
                    value={formData.currentOdometer}
                    onChange={e => setFormData({ ...formData, currentOdometer: parseFloat(e.target.value) || 0 })}
                    placeholder="ej. 45200.0"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden font-mono font-bold"
                  />
                </div>
              </div>

              <p className="text-[11px] text-slate-500 bg-slate-50 p-2.5 rounded-md border border-slate-200">
                ℹ️ <strong>Regla del contrato:</strong> No existe asignación permanente empleado–vehículo. El conductor y el vehículo se asocian de forma dinámica en cada solicitud/ticket.
              </p>

              <div className="pt-4 border-t border-slate-100 flex justify-end gap-2">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="px-4 py-2 text-xs font-semibold text-slate-600 bg-slate-100 hover:bg-slate-200 rounded-lg transition"
                >
                  Cancelar
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="px-4 py-2 text-xs font-semibold text-white bg-[#087e8b] hover:bg-[#066570] rounded-lg transition shadow-xs disabled:opacity-50"
                >
                  {isSubmitting ? 'Guardando...' : editingVehicle ? 'Actualizar Vehículo' : 'Registrar Vehículo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Confirmar Toggle Estado */}
      <ConfirmModal
        isOpen={!!vehicleToToggle}
        title="¿Desactivar vehículo?"
        message={
          `¿Desea dar de baja operativa el vehículo con placa ${vehicleToToggle?.plate}? No estará disponible para nuevas solicitudes de combustible.`
        }
        confirmLabel="Sí, Desactivar"
        isDestructive
        isLoading={isSubmitting}
        onConfirm={handleConfirmToggle}
        onClose={() => setVehicleToToggle(null)}
      />
    </div>
  );
};
