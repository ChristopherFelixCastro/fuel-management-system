import { useEffect, useState } from "react";
import { getTransfers, createTransfer, getErrorMessage } from "../api/inventoryApi";

const emptyForm = { sourceTankId: "", destinationTankId: "", quantity: "", notes: "" };

export default function Transfers() {
  const [transfers, setTransfers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const load = () => {
    setLoading(true);
    getTransfers()
      .then((res) => {
        if (res.status !== 200 || !res.body.success) {
          setError(getErrorMessage(res));
          return;
        }
        setTransfers(res.body.data);
      })
      .catch(() => setError("No se pudo conectar con el servidor."))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const change = (field) => (e) => setForm({ ...form, [field]: e.target.value });

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await createTransfer({
        ...form,
        quantity: Number(form.quantity),
        transferredAt: new Date().toISOString(),
      });
      if (res.status !== 201 || !res.body.success) {
        setError(getErrorMessage(res));
        return;
      }
      setForm(emptyForm);
      load();
    } catch {
      setError("No se pudo conectar con el servidor.");
    }
  };

  return (
    <div>
      <h2>Transferencias</h2>
      <form onSubmit={handleSubmit}>
        <input placeholder="Tanque origen ID" value={form.sourceTankId} onChange={change("sourceTankId")} />
        <input placeholder="Tanque destino ID" value={form.destinationTankId} onChange={change("destinationTankId")} />
        <input placeholder="Cantidad" type="number" value={form.quantity} onChange={change("quantity")} />
        <input placeholder="Notas (opcional)" value={form.notes} onChange={change("notes")} />
        <button type="submit">Registrar transferencia</button>
      </form>

      {error && <p>Error: {error}</p>}

      {loading ? (
        <p>Cargando...</p>
      ) : transfers.length === 0 ? (
        <p>No hay transferencias registradas.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Origen</th>
              <th>Destino</th>
              <th>Cantidad</th>
              <th>Fecha</th>
            </tr>
          </thead>
          <tbody>
            {transfers.map((t) => (
              <tr key={t.id}>
                <td>{t.sourceTankId}</td>
                <td>{t.destinationTankId}</td>
                <td>{t.quantity}</td>
                <td>{new Date(t.transferredAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}