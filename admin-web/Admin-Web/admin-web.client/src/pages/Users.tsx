import React, { useEffect, useState } from 'react';
import { 
  Plus, 
  Search, 
  Edit2, 
  Power, 
  Fuel, 
  X
} from 'lucide-react';
import { User, UserRole, Station, Employee, CreateUserDto, UpdateUserDto } from '../types';
import { UserService, CatalogService, EmployeeService } from '../services/api';
import { LoadingSpinner } from '../components/common/LoadingSpinner';
import { AlertBanner } from '../components/common/AlertBanner';
import { RoleBadge, StatusBadge } from '../components/common/Badge';
import { ConfirmModal } from '../components/common/ConfirmModal';

export const Users: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const [stations, setStations] = useState<Station[]>([]);
  const [employees, setEmployees] = useState<Employee[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('');
  const [statusFilter, setStatusFilter] = useState<string>('');

  // Modal / Form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [formData, setFormData] = useState<CreateUserDto>({
    fullName: '',
    email: '',
    username: '',
    password: '',
    role: 'SOLICITANTE',
    employeeId: null,
    stationId: null,
  });

  // Feedback & confirm modal
  const [alert, setAlert] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [userToToggle, setUserToToggle] = useState<User | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const loadData = async () => {
    setIsLoading(true);
    try {
      const [usersRes, stationsRes, empRes] = await Promise.all([
        UserService.getAll(),
        CatalogService.getStations(),
        EmployeeService.getAll(),
      ]);
      setUsers(usersRes.data);
      setStations(stationsRes.data);
      setEmployees(empRes.data);
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cargar usuarios' });
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleOpenCreateModal = () => {
    setEditingUser(null);
    setFormData({
      fullName: '',
      email: '',
      username: '',
      password: '',
      role: 'SOLICITANTE',
      employeeId: null,
      stationId: null,
    });
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (user: User) => {
    setEditingUser(user);
    setFormData({
      fullName: user.fullName,
      email: user.email,
      username: user.username,
      password: '', // En edición opcional cambiarla
      role: user.role,
      employeeId: user.employeeId || null,
      stationId: user.stationId || null,
    });
    setIsModalOpen(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setAlert(null);

    // Validación de contrato: DESPACHADOR exige estación activa
    if (formData.role === 'DESPACHADOR' && !formData.stationId) {
      setAlert({
        type: 'error',
        message: 'Para el rol DESPACHADOR la estación asignada es obligatoria (DISPATCHER_STATION_REQUIRED).'
      });
      return;
    }

    setIsSubmitting(true);
    try {
      if (editingUser) {
        const updatePayload: UpdateUserDto = {
          fullName: formData.fullName,
          email: formData.email,
          username: formData.username,
          role: formData.role,
          employeeId: formData.employeeId || null,
          stationId: formData.role === 'DESPACHADOR' ? formData.stationId : null,
        };
        if (formData.password) {
          updatePayload.password = formData.password;
        }
        await UserService.update(editingUser.id, updatePayload);
        setAlert({ type: 'success', message: 'Usuario actualizado correctamente.' });
      } else {
        await UserService.create({
          ...formData,
          stationId: formData.role === 'DESPACHADOR' ? formData.stationId : null,
        });
        setAlert({ type: 'success', message: 'Usuario registrado exitosamente.' });
      }
      setIsModalOpen(false);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al procesar la operación' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleConfirmToggle = async () => {
    if (!userToToggle) return;
    setIsSubmitting(true);
    try {
      await UserService.toggleActive(userToToggle.id);
      setAlert({
        type: 'success',
        message: `Usuario ${userToToggle.isActive ? 'desactivado' : 'activado'} correctamente.`,
      });
      setUserToToggle(null);
      loadData();
    } catch (err: any) {
      setAlert({ type: 'error', message: err.message || 'Error al cambiar estado' });
    } finally {
      setIsSubmitting(false);
    }
  };

  const filteredUsers = users.filter(user => {
    const matchSearch =
      user.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.username.toLowerCase().includes(searchTerm.toLowerCase()) ||
      user.email.toLowerCase().includes(searchTerm.toLowerCase());
    const matchRole = !roleFilter || user.role === roleFilter;
    const matchStatus =
      !statusFilter ||
      (statusFilter === 'active' && user.isActive) ||
      (statusFilter === 'inactive' && !user.isActive);

    return matchSearch && matchRole && matchStatus;
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 bg-white p-5 rounded-xl border border-slate-200 shadow-2xs">
        <div>
          <h1 className="text-2xl font-bold text-[#062d4f] tracking-tight">
            Gestión de Usuarios
          </h1>
          <p className="text-sm text-slate-500 mt-0.5">
            Administración de credenciales, roles únicos y estaciones asignadas
          </p>
        </div>

        <button
          onClick={handleOpenCreateModal}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#087e8b] hover:bg-[#066570] text-white text-xs font-semibold rounded-lg shadow-sm transition self-start sm:self-auto"
        >
          <Plus className="w-4 h-4" />
          <span>Nuevo Usuario</span>
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
      <div className="bg-white p-4 rounded-xl border border-slate-200 shadow-2xs flex flex-col md:flex-row gap-3">
        <div className="flex-1 relative">
          <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5" />
          <input
            type="text"
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            placeholder="Buscar por nombre, usuario o correo..."
            className="w-full text-xs pl-9 pr-3 py-2 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          />
        </div>

        <div className="w-full md:w-48">
          <select
            value={roleFilter}
            onChange={e => setRoleFilter(e.target.value)}
            className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
          >
            <option value="">Todos los Roles</option>
            <option value="ADMINISTRADOR">Administrador</option>
            <option value="SUPERVISOR">Supervisor</option>
            <option value="DESPACHADOR">Despachador</option>
            <option value="SOLICITANTE">Solicitante</option>
            <option value="AUDITOR">Auditor</option>
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

      {/* Users Table */}
      <div className="bg-white rounded-xl border border-slate-200 shadow-2xs overflow-hidden">
        {isLoading ? (
          <LoadingSpinner message="Cargando catálogo de usuarios..." />
        ) : filteredUsers.length === 0 ? (
          <div className="p-8 text-center text-slate-500 text-sm">
            No se encontraron usuarios con los criterios de búsqueda especificados.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead className="bg-[#e8f4f5] text-[#063e5b] text-xs uppercase font-semibold">
                <tr>
                  <th className="py-3 px-4">Usuario</th>
                  <th className="py-3 px-4">Nombre Completo</th>
                  <th className="py-3 px-4">Correo</th>
                  <th className="py-3 px-4">Rol Asignado</th>
                  <th className="py-3 px-4">Estación</th>
                  <th className="py-3 px-4 text-center">Estado</th>
                  <th className="py-3 px-4 text-right">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {filteredUsers.map(user => (
                  <tr key={user.id} className="hover:bg-slate-50 transition">
                    <td className="py-3 px-4 font-mono font-medium text-slate-900">
                      @{user.username}
                    </td>
                    <td className="py-3 px-4 font-medium text-slate-800">
                      {user.fullName}
                    </td>
                    <td className="py-3 px-4 text-slate-600 text-xs">
                      {user.email}
                    </td>
                    <td className="py-3 px-4">
                      <RoleBadge role={user.role} />
                    </td>
                    <td className="py-3 px-4 text-xs text-slate-600">
                      {user.role === 'DESPACHADOR' ? (
                        user.stationName ? (
                          <span className="inline-flex items-center gap-1 text-slate-800 font-medium">
                            <Fuel className="w-3 h-3 text-[#087e8b]" />
                            {user.stationName}
                          </span>
                        ) : (
                          <span className="text-rose-600 font-bold">Sin estación asignada</span>
                        )
                      ) : (
                        <span className="text-slate-400 italic">No aplica</span>
                      )}
                    </td>
                    <td className="py-3 px-4 text-center">
                      <StatusBadge isActive={user.isActive} />
                    </td>
                    <td className="py-3 px-4 text-right">
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => handleOpenEditModal(user)}
                          title="Editar usuario"
                          className="p-1.5 text-slate-500 hover:text-[#087e8b] hover:bg-slate-100 rounded-lg transition"
                        >
                          <Edit2 className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => setUserToToggle(user)}
                          title={user.isActive ? 'Desactivar cuenta' : 'Activar cuenta'}
                          className={`p-1.5 rounded-lg transition ${
                            user.isActive
                              ? 'text-slate-500 hover:text-amber-600 hover:bg-amber-50'
                              : 'text-slate-500 hover:text-emerald-600 hover:bg-emerald-50'
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
          <div className="bg-white rounded-xl shadow-2xl max-w-lg w-full border border-slate-200 overflow-hidden animate-fade-in">
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <h3 className="text-base font-bold text-slate-900">
                {editingUser ? 'Editar Usuario' : 'Registrar Nuevo Usuario'}
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
                  Nombre Completo *
                </label>
                <input
                  type="text"
                  required
                  value={formData.fullName}
                  onChange={e => setFormData({ ...formData, fullName: e.target.value })}
                  placeholder="ej. Juan Pérez"
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                />
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Nombre de Usuario *
                  </label>
                  <input
                    type="text"
                    required
                    value={formData.username}
                    onChange={e => setFormData({ ...formData, username: e.target.value })}
                    placeholder="ej. jperez"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Correo Electrónico *
                  </label>
                  <input
                    type="email"
                    required
                    value={formData.email}
                    onChange={e => setFormData({ ...formData, email: e.target.value })}
                    placeholder="ej. jperez@empresa.com"
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-semibold text-slate-700 mb-1">
                  {editingUser ? 'Nueva Contraseña (Opcional)' : 'Contraseña Inicial *'}
                </label>
                <input
                  type="password"
                  required={!editingUser}
                  value={formData.password}
                  onChange={e => setFormData({ ...formData, password: e.target.value })}
                  placeholder={editingUser ? 'Dejar en blanco para mantener actual' : '••••••••'}
                  className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                />
                <p className="text-[10px] text-slate-400 mt-1">
                  Mínimo 8 caracteres, incluye mayúsculas, minúsculas y números.
                </p>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Rol Único del Usuario *
                  </label>
                  <select
                    value={formData.role}
                    onChange={e => {
                      const newRole = e.target.value as UserRole;
                      setFormData({
                        ...formData,
                        role: newRole,
                        // Si cambia de despachador a otro rol, limpiar estación
                        stationId: newRole === 'DESPACHADOR' ? formData.stationId : null,
                      });
                    }}
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  >
                    <option value="ADMINISTRADOR">ADMINISTRADOR</option>
                    <option value="SUPERVISOR">SUPERVISOR</option>
                    <option value="DESPACHADOR">DESPACHADOR</option>
                    <option value="SOLICITANTE">SOLICITANTE</option>
                    <option value="AUDITOR">AUDITOR</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-700 mb-1">
                    Empleado Vinculado (Opcional)
                  </label>
                  <select
                    value={formData.employeeId || ''}
                    onChange={e => setFormData({ ...formData, employeeId: e.target.value || null })}
                    className="w-full text-xs py-2 px-3 border border-slate-300 rounded-lg focus:ring-2 focus:ring-[#087e8b] focus:outline-hidden"
                  >
                    <option value="">Ninguno / Cuenta Institucional</option>
                    {employees.map(emp => (
                      <option key={emp.id} value={emp.id}>
                        {emp.employeeNumber} - {emp.firstName} {emp.lastName}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Campo condicional obligatorio: Estación para DESPACHADOR */}
              {formData.role === 'DESPACHADOR' && (
                <div className="p-3 bg-amber-50 border border-amber-200 rounded-lg">
                  <div className="flex items-center gap-1.5 text-xs font-bold text-amber-800 mb-1">
                    <Fuel className="w-4 h-4 text-amber-600" />
                    <span>Estación de Asignación Obligatoria *</span>
                  </div>
                  <select
                    required
                    value={formData.stationId || ''}
                    onChange={e => setFormData({ ...formData, stationId: e.target.value })}
                    className="w-full text-xs py-2 px-3 border border-amber-300 rounded-lg focus:ring-2 focus:ring-amber-500 focus:outline-hidden bg-white"
                  >
                    <option value="">Seleccione una estación...</option>
                    {stations.map(st => (
                      <option key={st.id} value={st.id}>
                        {st.name} ({st.code})
                      </option>
                    ))}
                  </select>
                  <p className="text-[10px] text-amber-700 mt-1">
                    Regla de negocio: Un despachador solo puede operar despachos en su estación asignada.
                  </p>
                </div>
              )}

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
                  {isSubmitting ? 'Guardando...' : editingUser ? 'Actualizar Usuario' : 'Registrar'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal de confirmación para Activar/Desactivar */}
      <ConfirmModal
        isOpen={!!userToToggle}
        title={userToToggle?.isActive ? '¿Desactivar usuario?' : '¿Activar usuario?'}
        message={
          userToToggle?.isActive
            ? `¿Está seguro de desactivar la cuenta de ${userToToggle?.fullName}? El usuario no podrá iniciar sesión mientras permanezca inactivo (Soft delete).`
            : `¿Desea restaurar el acceso al sistema para ${userToToggle?.fullName}?`
        }
        confirmLabel={userToToggle?.isActive ? 'Sí, Desactivar' : 'Sí, Activar'}
        isDestructive={userToToggle?.isActive}
        isLoading={isSubmitting}
        onConfirm={handleConfirmToggle}
        onClose={() => setUserToToggle(null)}
      />
    </div>
  );
};
