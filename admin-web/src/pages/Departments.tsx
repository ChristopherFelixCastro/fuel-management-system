import React, { useEffect, useState } from 'react';
import {
  Building2,
  Plus,
  Search,
  Edit2,
  Power,
  X
} from 'lucide-react';
import { Department, CreateDepartmentDto } from '../types';
import { DepartmentService } from '../services/api';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';
import { StatusBadge } from '../components/common/Badge';
import { ConfirmModal } from '../components/common/ConfirmModal';

export const Departments: React.FC = () => {
  const [departments, setDepartments] = useState<Department[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  // Modal / Form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);
  const [formData, setFormData] = useState<CreateDepartmentDto>({
    code: '',
    name: '',
    description: '',
  });

  // Feedback & confirm modal
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [deptToToggle, setDeptToToggle] = useState<Department | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const res = await DepartmentService.getAll();
      setDepartments(res.data);
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cargar departamentos' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenCreate = () => {
    setEditingDepartment(null);
    setFormData({
      code: '',
      name: '',
      description: '',
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (dept: Department) => {
    setEditingDepartment(dept);
    setFormData({
      code: dept.code,
      name: dept.name,
      description: dept.description || '',
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setAlert(null);

    if (!formData.code.trim() || !formData.name.trim()) {
      setAlert({ type: 'error', message: 'Código y Nombre son campos obligatorios.' });
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingDepartment) {
        await DepartmentService.update(editingDepartment.id, formData);
        setAlert({ type: 'success', message: 'Departamento actualizado correctamente.' });
      } else {
        await DepartmentService.create(formData);
        setAlert({ type: 'success', message: 'Departamento registrado exitosamente.' });
      }
      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al guardar departamento' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmToggle = async () => {
    if (!deptToToggle) return;
    setIsSubmitting(true);
    try {
      await DepartmentService.deactivate(deptToToggle.id);
      setAlert({
        type: 'success',
        message: 'Departamento desactivado correctamente.',
      });
      setDeptToToggle(null);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cambiar estado' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const filteredDepartments = departments.filter(d => {
    const term = searchTerm.toLowerCase();
    const matchSearch =
      d.name.toLowerCase().includes(term) ||
      d.code.toLowerCase().includes(term) ||
      (d.description && d.description.toLowerCase().includes(term));
    const matchStatus =
      !statusFilter ||
      (statusFilter === 'active' && d.isActive) ||
      (statusFilter === 'inactive' && !d.isActive);

    return matchSearch && matchStatus;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Gestión de Departamentos
          </h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Estructura organizativa para asignación de presupuesto y consumo de combustible
          </p>
        </div>

        <button
          onClick={handleOpenCreate}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          <span>Nuevo Departamento</span>
        </button>
      </div>

      {alert && (
        <AlertBanner
          type={alert.type}
          message={alert.message}
          onClose={() => setAlert(null)}
        />
      )}

      {/* Filters Bar */}
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex flex-col sm:flex-row gap-3">
        <div className="flex-1 relative">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
          <input
            type="text"
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            placeholder="Buscar por código, nombre o descripción..."
            className="w-full text-xs pl-9 pr-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          />
        </div>

        <div className="w-full sm:w-44">
          <select
            value={statusFilter}
            onChange={e => setStatusFilter(e.target.value)}
            className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          >
            <option value="">Todos los Estados</option>
            <option value="active">Activos</option>
            <option value="inactive">Inactivos</option>
          </select>
        </div>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
        {isLoading ? (
          <LoadingSpinner message="Cargando departamentos..." />
        ) : filteredDepartments.length === 0 ? (
          <div className="p-8 text-center text-slate-500 text-sm">
            No se encontraron departamentos con esos criterios.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Código</th>
                  <th className="py-3 px-4">Nombre del Departamento</th>
                  <th className="py-3 px-4">Descripción / Función</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  <th className="py-3 px-4 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredDepartments.map(dept => (
                  <tr key={dept.id} className="hover:bg-slate-50 transition">
                    <td className="py-3 px-4 font-mono font-bold text-slate-900">
                      {dept.code}
                    </td>
                    <td className="py-3 px-4 font-semibold text-slate-800">
                      <div className="flex items-center gap-2">
                        <Building2 className="w-4 h-4 text-[#087e8b]" />
                        <span>{dept.name}</span>
                      </div>
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-600 max-w-md truncate">
                      {dept.description || <span className="text-slate-400 italic">Sin descripción</span>}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <StatusBadge isActive={dept.isActive} />
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => handleOpenEdit(dept)}
                          title="Editar departamento"
                          className="p-1.5 text-slate-500 hover:text-[#087e8b] hover:bg-slate-100 rounded-lg transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => setDeptToToggle(dept)}
                          title="Desactivar departamento"
                          className={`p-1.5 rounded-lg transition ${
                            dept.isActive ? 'text-slate-500 hover:text-amber-600 hover:bg-amber-50' : 'hidden'
                          }`}
                        >
                          <Power className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
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
          <div className="bg-white rounded-xl shadow-2xl max-w-md w-full border border-slate-200 overflow-hidden animate-fade-in">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                {editingDepartment ? 'Editar Departamento' : 'Registrar Nuevo Departamento'}
              </h3>
              <button
                onClick={() => setIsModalOpen(false)}
                className="text-slate-400 hover:text-slate-600 p-1 rounded-md"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <form onSubmit={handleSubmit} className="p-6 space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Código Único *
                </label>
                <input
                  type="text"
                  required
                  value={formData.code}
                  onChange={e => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                  placeholder="ej. DEP-LOG"
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden uppercase font-mono font-bold"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Nombre del Departamento *
                </label>
                <input
                  type="text"
                  required
                  value={formData.name}
                  onChange={e => setFormData({ ...formData, name: e.target.value })}
                  placeholder="ej. Transporte y Logística"
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                />
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Descripción / Finalidad
                </label>
                <textarea
                  rows={3}
                  value={formData.description || ''}
                  onChange={e => setFormData({ ...formData, description: e.target.value })}
                  placeholder="Detalles sobre las funciones o áreas cubiertas por este departamento..."
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden resize-none"
                />
              </div>

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
                  {isSubmitting ? 'Guardando...' : editingDepartment ? 'Actualizar' : 'Registrar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Confirmar Soft Delete */}
      <ConfirmModal
        isOpen={!!deptToToggle}
        title="¿Desactivar departamento?"
        message={
          `¿Desea desactivar el departamento ${deptToToggle?.name}? Las entidades asociadas mantendrán su historial, pero no se permitirá asignar este departamento a nuevas solicitudes (Soft delete).`
        }
        confirmLabel="Sí, Desactivar"
        isDestructive
        isLoading={isSubmitting}
        onConfirm={handleConfirmToggle}
        onClose={() => setDeptToToggle(null)}
      />
    </div>
  );
};
