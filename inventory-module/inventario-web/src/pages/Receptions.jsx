import { useEffect, useState } from "react";
import { getReceptions, createReception, getErrorMessage } from "../api/inventoryApi";

const emptyForm = {
  supplierId: "",
  stationId: "",
  tankId: "",
  invoiceNumber: "",
  volume: "",
  rnc: "",
  notes: "",
};

export default function Receptions() {
  const [receptions, setReceptions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const load = () => {
    setLoading(true);
    getReceptions()
      .then((res) => {
        if (res.status !== 200 || !res.body.success) {
          setError(getErrorMessage(res));
          return;
        }
        setReceptions(res.body.data);
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
      const res = await createReception({
        ...form,
        volume: Number(form.volume),
        receivedAt: new Date().toISOString(),
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
      <h2>Recepciones</h2>
      <form onSubmit={handleSubmit}>
        <input placeholder="Supplier ID" value={form.supplierId} onChange={change("supplierId")} />
        <input placeholder="Station ID" value={form.stationId} onChange={change("stationId")} />
        <input placeholder="Tank ID" value={form.tankId} onChange={change("tankId")} />
        <input placeholder="Factura" value={form.invoiceNumber} onChange={change("invoiceNumber")} />
        <input placeholder="Volumen" type="number" value={form.volume} onChange={change("volume")} />
        <input placeholder="RNC" value={form.rnc} onChange={change("rnc")} />
        <input placeholder="Notas (opcional)" value={form.notes} onChange={change("notes")} />
        <button type="submit">Registrar recepción</button>
      </form>

      {error && <p>Error: {error}</p>}

      {loading ? (
        <p>Cargando...</p>
      ) : receptions.length === 0 ? (
        <p>No hay recepciones registradas.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Factura</th>
              <th>Tanque</th>
              <th>Volumen</th>
              <th>Fecha</th>
            </tr>
          </thead>
          <tbody>
            {receptions.map((r) => (
              <tr key={r.id}>
                <td>{r.invoiceNumber}</td>
                <td>{r.tankId}</td>
                <td>{r.volume}</td>
                <td>{new Date(r.receivedAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}