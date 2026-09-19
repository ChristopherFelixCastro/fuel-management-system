import {
  BrowserRouter,
  Navigate,
  Route,
  Routes,
} from 'react-router-dom'

import { AuthProvider } from './contexts/AuthContext'
import { HomePage } from './pages/HomePage'
import { LoginPage } from './pages/LoginPage'
import { ScanPage } from './pages/ScanPage'
import { ProtectedRoute } from './routes/ProtectedRoute'
import { TicketPage } from './pages/TicketPage'
import { ClosurePage } from './pages/ClosurePage'
import { InventoryPage } from './pages/InventoryPage'
import { AdjustmentPage } from './pages/AdjustmentPage'
export function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route element={<ProtectedRoute />}>
            <Route path="/" element={<HomePage />} />
            <Route path="/scan" element={<ScanPage />} />
            <Route path="/ticket" element={<TicketPage />} />
            <Route path="/closure" element={<ClosurePage />} />
            <Route path="/inventory" element={<InventoryPage />} />
            <Route path="/adjustment" element={<AdjustmentPage />} />
          </Route>

          <Route
            path="*"
            element={<Navigate to="/" replace />}
          />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
