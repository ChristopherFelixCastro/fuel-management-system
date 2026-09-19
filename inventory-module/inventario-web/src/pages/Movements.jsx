import { useEffect, useState } from "react";
import { getMovements, getErrorMessage } from "../api/inventoryApi";

const TYPES = [
  "",
  "RECEPCION",
  "DESPACHO",
  "TRANSFERENCIA_ENTRADA",
  "TRANSFERENCIA_SALIDA",
  "MERMA",
  "AJUSTE_POSITIVO",
  "AJUSTE_NEGATIVO",
];

export default function Movements() {
  const [movements, setMovements] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [tankId, setTankId] = useState("");
  const [type, setType] = useState("");

  const load = () => {
    setLoading(true);
    setError(null);
    const params = new URLSearchParams();
    if (tankId) params.set("tankId", tankId);
    if (type) params.set("type", type);
    const query = params.toString() ? `?${params.toString()}` : "";

    getMovements(query)
      .then((res) => {
        if (res.status !== 200 || !res.body.success) {
          setError(getErrorMessage(res));
          return;
        }
        setMovements(res.body.data);
      })
      .catch(() => setError("No se pudo conectar con el servidor."))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  return (
    <div>
      <h2>Movimientos de inventario</h2>
      <form
        onSubmit={(e) => {
          e.preventDefault();
          load();
        }}
      >
        <input placeholder="Tank ID (opcional)" value={tankId} onChange={(e) => setTankId(e.target.value)} />
        <select value={type} onChange={(e) => setType(e.target.value)}>
          {TYPES.map((t) => (
            <option key={t} value={t}>
              {t === "" ? "Todos los tipos" : t}
            </option>
          ))}
        </select>
        <button type="submit">Filtrar</button>
      </form>

      {error && <p>Error: {error}</p>}

      {loading ? (
        <p>Cargando...</p>
      ) : movements.length === 0 ? (
        <p>No hay movimientos para mostrar.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Tipo</th>
              <th>Tanque</th>
              <th>Cantidad</th>
              <th>Saldo anterior</th>
              <th>Saldo nuevo</th>
              <th>Fecha</th>
            </tr>
          </thead>
          <tbody>
            {movements.map((m) => (
              <tr key={m.id}>
                <td>{m.type}</td>
                <td>{m.tankId}</td>
                <td>{m.quantity}</td>
                <td>{m.previousBalance}</td>
                <td>{m.newBalance}</td>
                <td>{new Date(m.occurredAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}