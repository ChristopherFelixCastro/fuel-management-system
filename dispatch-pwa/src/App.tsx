import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { ProtectedRoute } from './routes/ProtectedRoute'
export function App() { return <AuthProvider><BrowserRouter><Routes><Route path="/login" element={<LoginPage />} /><Route element={<ProtectedRoute />}><Route path="/" element={<HomePage />} /></Route><Route path="*" element={<Navigate to="/" replace />} /></Routes></BrowserRouter></AuthProvider> }
