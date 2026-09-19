import { BrowserRouter, NavLink, Navigate, Route, Routes } from "react-router-dom";
import InventorySummary from "./pages/InventorySummary";
import Receptions from "./pages/Receptions";
import Movements from "./pages/Movements";
import Transfers from "./pages/Transfers";
import Adjustments from "./pages/Adjustments";

export default function App() {
  return (
    <BrowserRouter>
      <nav>
        <NavLink to="/inventory">Resumen</NavLink>
        <NavLink to="/receptions">Recepciones</NavLink>
        <NavLink to="/movements">Movimientos</NavLink>
        <NavLink to="/transfers">Transferencias</NavLink>
        <NavLink to="/adjustments">Mermas y ajustes</NavLink>
      </nav>

      <Routes>
        <Route path="/" element={<Navigate to="/inventory" replace />} />
        <Route path="/inventory" element={<InventorySummary />} />
        <Route path="/receptions" element={<Receptions />} />
        <Route path="/movements" element={<Movements />} />
        <Route path="/transfers" element={<Transfers />} />
        <Route path="/adjustments" element={<Adjustments />} />
      </Routes>
    </BrowserRouter>
  );
}