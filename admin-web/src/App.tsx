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

export const App: React.FC = () => {
  return (
    <Routes>
      {/* Ruta pública de autenticación */}
      <Route path="/login" element={<Login />} />

      {/* Rutas protegidas bajo el Layout principal */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />

        {/* Dashboard Ejecutivo */}
        <Route path="dashboard" element={<Dashboard />} />

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
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
};
