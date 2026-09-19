import { useEffect, useState } from "react";
import { getInventory, getErrorMessage } from "../api/inventoryApi";

export default function InventorySummary() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    getInventory()
      .then((res) => {
        if (res.status !== 200 || !res.body.success) {
          setError(getErrorMessage(res));
          return;
        }
        setItems(res.body.data);
      })
      .catch(() => setError("No se pudo conectar con el servidor."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <p>Cargando inventario...</p>;
  if (error) return <p>Error: {error}</p>;
  if (items.length === 0) return <p>No hay tanques registrados.</p>;

  return (
    <div>
      <h2>Resumen de inventario</h2>
      <table>
        <thead>
          <tr>
            <th>Tanque</th>
            <th>Físico</th>
            <th>Reservado</th>
            <th>Disponible</th>
            <th>Nivel crítico</th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.tankId}>
              <td>{item.tankId}</td>
              <td>{item.physicalQuantity}</td>
              <td>{item.reservedQuantity}</td>
              <td>{item.availableQuantity}</td>
              <td>
                {item.criticalLevel}
                {item.physicalQuantity <= item.criticalLevel ? " ⚠ crítico" : ""}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}