import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { MainLayout } from './components/layout/MainLayout';
import { ProtectedRoute } from './components/layout/ProtectedRoute';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { Users } from './pages/Users';
import { Employees } from './pages/Employees';
import { Vehicles } from './pages/Vehicles';
import { Departments } from './pages/Departments';
import { Closures } from './pages/Closures';
import { Reports } from './pages/Reports';
import { Alerts } from './pages/Alerts';
import { InventoryOperations } from './pages/InventoryOperations';
import { MyRequests } from './pages/MyRequests';
import { RequestsManagement } from './pages/RequestsManagement';
import { PublicTicket } from './pages/PublicTicket';
import { useAuth } from './context/AuthContext';
import { getDefaultRouteForRole } from './utils/navigation';

const HomeRedirect: React.FC = () => {
  const { user } = useAuth();
  return <Navigate to={getDefaultRouteForRole(user?.role)} replace />;
};

export const App: React.FC = () => {
  return (
    <Routes>
      {/* Ruta pública de autenticación */}
      <Route path="/login" element={<Login />} />

      {/* Ruta pública para visualización segura del ticket QR */}
      <Route path="/ticket/:token" element={<PublicTicket />} />

      {/* Rutas protegidas bajo el Layout principal */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<HomeRedirect />} />

        {/* Mis Solicitudes (Portal del Solicitante) */}
        <Route
          path="my-requests"
          element={
            <ProtectedRoute allowedRoles={['SOLICITANTE']}>
              <MyRequests />
            </ProtectedRoute>
          }
        />

        {/* Dashboard Ejecutivo */}
        <Route
          path="dashboard"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR', 'DESPACHADOR']}>
              <Dashboard />
            </ProtectedRoute>
          }
        />

        {/* Gestión de Solicitudes (Supervisor y Administrador) */}
        <Route
          path="requests"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR']}>
              <RequestsManagement />
            </ProtectedRoute>
          }
        />

        {/* CRUD Usuarios (Solo ADMINISTRADOR) */}
        <Route
          path="users"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR']}>
              <Users />
            </ProtectedRoute>
          }
        />

        {/* CRUD Empleados */}
        <Route
          path="employees"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR']}>
              <Employees />
            </ProtectedRoute>
          }
        />

        {/* CRUD Vehículos */}
        <Route
          path="vehicles"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR']}>
              <Vehicles />
            </ProtectedRoute>
          }
        />

        {/* CRUD Departamentos */}
        <Route
          path="departments"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR', 'AUDITOR']}>
              <Departments />
            </ProtectedRoute>
          }
        />

        <Route
          path="closures"
          element={
            <ProtectedRoute allowedRoles={['ADMINISTRADOR', 'SUPERVISOR', 'DESPACHADOR']}>
              <Closures />
            </ProtectedRoute>
          }
        />
        <Route
  path="reports"
  element={
    <ProtectedRoute
      allowedRoles={[
        'ADMINISTRADOR',
        'SUPERVISOR',
        'AUDITOR',
      ]}
    >
      <Reports />
    </ProtectedRoute>
  }
/>
<Route
  path="inventory"
  element={<ProtectedRoute allowedRoles={['ADMINISTRADOR','SUPERVISOR','AUDITOR']}><InventoryOperations /></ProtectedRoute>}
/>
<Route
  path="alerts"
  element={
    <ProtectedRoute
      allowedRoles={[
        'ADMINISTRADOR',
        'SUPERVISOR',
        'AUDITOR',
      ]}
    >
      <Alerts />
    </ProtectedRoute>
  }
/>
      </Route>

      {/* Fallback general */}
      <Route path="*" element={<HomeRedirect />} />
    </Routes>
  );
};
