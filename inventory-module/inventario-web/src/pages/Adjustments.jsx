import { useEffect, useState } from "react";
import {
  getAdjustments,
  createAdjustment,
  approveAdjustment,
  rejectAdjustment,
  getErrorMessage,
} from "../api/inventoryApi";

const emptyForm = { tankId: "", physicalQuantity: "", reason: "" };

export default function Adjustments() {
  const [adjustments, setAdjustments] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const load = () => {
    setLoading(true);
    getAdjustments()
      .then((res) => {
        if (res.status !== 200 || !res.body.success) {
          setError(getErrorMessage(res));
          return;
        }
        setAdjustments(res.body.data);
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
      const res = await createAdjustment({
        ...form,
        physicalQuantity: Number(form.physicalQuantity),
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

  const handleApprove = async (id) => {
    setError(null);
    const res = await approveAdjustment(id);
    if (res.status !== 200 || !res.body.success) {
      setError(getErrorMessage(res));
      return;
    }
    load();
  };

  const handleReject = async (id) => {
    setError(null);
    const reason = window.prompt("Motivo del rechazo:") || "";
    const res = await rejectAdjustment(id, reason);
    if (res.status !== 200 || !res.body.success) {
      setError(getErrorMessage(res));
      return;
    }
    load();
  };

  return (
    <div>
      <h2>Mermas y ajustes</h2>
      <form onSubmit={handleSubmit}>
        <input placeholder="Tank ID" value={form.tankId} onChange={change("tankId")} />
        <input placeholder="Cantidad física" type="number" value={form.physicalQuantity} onChange={change("physicalQuantity")} />
        <input placeholder="Motivo" value={form.reason} onChange={change("reason")} />
        <button type="submit">Reportar ajuste</button>
      </form>

      {error && <p>Error: {error}</p>}

      {loading ? (
        <p>Cargando...</p>
      ) : adjustments.length === 0 ? (
        <p>No hay ajustes reportados.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Tanque</th>
              <th>Cantidad física</th>
              <th>Motivo</th>
              <th>Estado</th>
              <th>Acciones</th>
            </tr>
          </thead>
          <tbody>
            {adjustments.map((a) => (
              <tr key={a.id}>
                <td>{a.tankId}</td>
                <td>{a.physicalQuantity}</td>
                <td>{a.reason}</td>
                <td>{a.status}</td>
                <td>
                  {a.status === "PENDIENTE" && (
                    <>
                      <button onClick={() => handleApprove(a.id)}>Aprobar</button>{" "}
                      <button onClick={() => handleReject(a.id)}>Rechazar</button>
                    </>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}