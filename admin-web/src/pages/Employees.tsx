import React, { useEffect, useState } from 'react';
import {
  Plus,
  Search,
  Edit2,
  Power,
  Building2,
  Phone,
  Mail,
  X
} from 'lucide-react';
import { Employee, Department, CreateEmployeeDto } from '../types';
import { EmployeeService, DepartmentService } from '../services/api';
import { useAuth } from '../context/AuthContext';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';
import { StatusBadge } from '../components/common/Badge';
import { ConfirmModal } from '../components/common/ConfirmModal';

export const Employees: React.FC = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole(['ADMINISTRADOR']);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [deptFilter, setDeptFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');

  // Modal / Form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);
  const [formData, setFormData] = useState<CreateEmployeeDto>({
    employeeNumber: '',
    firstName: '',
    lastName: '',
    documentId: '',
    email: '',
    phone: '',
    departmentId: '',
  });

  // Feedback & confirm modal
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [empToToggle, setEmpToToggle] = useState<Employee | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [empRes, deptRes] = await Promise.all([
        EmployeeService.getAll(),
        DepartmentService.getAll(),
      ]);
      setEmployees(empRes.data);
      setDepartments(deptRes.data);
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cargar empleados' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenCreate = () => {
    setEditingEmployee(null);
    setFormData({
      employeeNumber: '',
      firstName: '',
      lastName: '',
      documentId: '',
      email: '',
      phone: '',
      departmentId: departments[0]?.id || '',
    });
    setIsModalOpen(true);
  };

  const handleOpenEdit = (emp: Employee) => {
    setEditingEmployee(emp);
    setFormData({
      employeeNumber: emp.employeeNumber,
      firstName: emp.firstName,
      lastName: emp.lastName,
      documentId: emp.documentId || '',
      email: emp.email || '',
      phone: emp.phone || '',
      departmentId: emp.departmentId,
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setAlert(null);

    if (!formData.departmentId) {
      setAlert({ type: 'error', message: 'Debe seleccionar un departamento para el empleado.' });
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingEmployee) {
        await EmployeeService.update(editingEmployee.id, formData);
        setAlert({ type: 'success', message: 'Empleado actualizado correctamente.' });
      } else {
        await EmployeeService.create(formData);
        setAlert({ type: 'success', message: 'Empleado registrado exitosamente.' });
      }
      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al guardar datos del empleado' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmToggle = async () => {
    if (!empToToggle) return;
    setIsSubmitting(true);
    try {
      await EmployeeService.deactivate(empToToggle.id);
      setAlert({
        type: 'success',
        message: 'Empleado desactivado correctamente.',
      });
      setEmpToToggle(null);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cambiar estado' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const filteredEmployees = employees.filter(emp => {
    const term = searchTerm.toLowerCase();
    const matchSearch =
      emp.firstName.toLowerCase().includes(term) ||
      emp.lastName.toLowerCase().includes(term) ||
      emp.employeeNumber.toLowerCase().includes(term) ||
      (emp.documentId && emp.documentId.toLowerCase().includes(term));
    const matchDept = !deptFilter || emp.departmentId === deptFilter;
    const matchStatus =
      !statusFilter ||
      (statusFilter === 'active' && emp.isActive) ||
      (statusFilter === 'inactive' && !emp.isActive);

    return matchSearch && matchDept && matchStatus;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Gestión de Empleados
          </h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Registro de personal operativo, choferes y solicitantes autorizados
          </p>
        </div>

        {isAdmin && (
          <button
            onClick={handleOpenCreate}
            className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition self-start sm:self-auto"
          >
            <Plus className="w-4 h-4" />
            <span>Nuevo Empleado</span>
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
            placeholder="Buscar por código, nombre, apellido o cédula..."
            className="w-full text-xs pl-9 pr-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          />
        </div>

        <div className="w-full md:w-56">
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

        <div className="w-full md:w-40">
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
          <LoadingSpinner message="Cargando nómina de empleados..." />
        ) : filteredEmployees.length === 0 ? (
          <div className="p-8 text-center text-slate-500 text-sm">
            No se encontraron empleados con los filtros aplicados.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Ficha / Código</th>
                  <th className="py-3 px-4">Nombre y Apellidos</th>
                  <th className="py-3 px-4">Documento / Cédula</th>
                  <th className="py-3 px-4">Departamento</th>
                  <th className="py-3 px-4">Contacto</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  {isAdmin && <th className="py-3 px-4 text-right">Acciones</th>}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredEmployees.map(emp => (
                  <tr key={emp.id} className="hover:bg-slate-50 transition">
                    <td className="py-3 px-4 font-mono font-bold text-slate-900">
                      {emp.employeeNumber}
                    </td>
                    <td className="py-3 px-4 font-medium text-slate-800">
                      {emp.firstName} {emp.lastName}
                    </td>
                    <td className="py-3 px-4 text-xs font-mono text-slate-600">
                      {emp.documentId || '—'}
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-700">
                      <div className="flex items-center gap-1.5">
                        <Building2 className="w-3.5 h-3.5 text-[#087e8b]" />
                        <span>{emp.departmentName}</span>
                      </div>
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-600">
                      <div className="space-y-0.5">
                        {emp.email && (
                          <div className="flex items-center gap-1 text-[11px]">
                            <Mail className="w-3 h-3 text-slate-400" />
                            <span>{emp.email}</span>
                          </div>
                        )}
                        {emp.phone && (
                          <div className="flex items-center gap-1 text-[11px]">
                            <Phone className="w-3 h-3 text-slate-400" />
                            <span>{emp.phone}</span>
                          </div>
                        )}
                      </div>
                    </td>
                    <td className="py-3 px-4 text-center">
                      <StatusBadge isActive={emp.isActive} />
                    </td>
                    {isAdmin && (
                      <td className="py-3 px-4 text-right">
                        <div className="flex items-center justify-end gap-1">
                          <button
                            onClick={() => handleOpenEdit(emp)}
                            title="Editar empleado"
                            className="p-1.5 text-slate-500 hover:text-[#087e8b] hover:bg-slate-100 rounded-lg transition"
                          >
                            <Edit2 className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => setEmpToToggle(emp)}
                            title="Desactivar empleado"
                            className={`p-1.5 rounded-lg transition ${
                              emp.isActive ? 'text-slate-500 hover:text-amber-600 hover:bg-amber-50' : 'hidden'
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
                {editingEmployee ? 'Editar Empleado' : 'Registrar Nuevo Empleado'}
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
                    Número de Ficha / Empleado *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.employeeNumber}
                    onChange={e => setFormData({ ...formData, employeeNumber: e.target.value })}
                    placeholder="ej. EMP-105"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden uppercase"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Cédula / Documento
                  </label>
                  <input
                    type="text"
                    value={formData.documentId || ''}
                    onChange={e => setFormData({ ...formData, documentId: e.target.value })}
                    placeholder="ej. 402-0000000-0"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Nombres *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.firstName}
                    onChange={e => setFormData({ ...formData, firstName: e.target.value })}
                    placeholder="ej. Juan Antonio"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Apellidos *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.lastName}
                    onChange={e => setFormData({ ...formData, lastName: e.target.value })}
                    placeholder="ej. Rodríguez"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  Departamento de Adscripción *
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
                      {d.code} - {d.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Correo Electrónico
                  </label>
                  <input
                    type="email"
                    value={formData.email || ''}
                    onChange={e => setFormData({ ...formData, email: e.target.value })}
                    placeholder="ej. jrodriguez@empresa.com"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Teléfono de Contacto
                  </label>
                  <input
                    type="tel"
                    value={formData.phone || ''}
                    onChange={e => setFormData({ ...formData, phone: e.target.value })}
                    placeholder="ej. 809-555-0199"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>
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
                  {isSubmitting ? 'Guardando...' : editingEmployee ? 'Actualizar Empleado' : 'Guardar Empleado'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal de confirmación para Activar/Desactivar */}
      <ConfirmModal
        isOpen={!!empToToggle}
        title="¿Desactivar empleado?"
        message={
          `¿Desea desactivar a ${empToToggle?.firstName} ${empToToggle?.lastName}? No podrá ser seleccionado en nuevas solicitudes de combustible.`
        }
        confirmLabel="Sí, Desactivar"
        isDestructive
        isLoading={isSubmitting}
        onConfirm={handleConfirmToggle}
        onClose={() => setEmpToToggle(null)}
      />
    </div>
  );
};
