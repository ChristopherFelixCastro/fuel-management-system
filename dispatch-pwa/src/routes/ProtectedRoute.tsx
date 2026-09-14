import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
export function ProtectedRoute() { const { isAuthenticated, isLoading } = useAuth(); const location = useLocation(); if (isLoading) return <main className="page-status">Cargando sesión…</main>; return isAuthenticated ? <Outlet /> : <Navigate to="/login" replace state={{ from: location }} /> }
