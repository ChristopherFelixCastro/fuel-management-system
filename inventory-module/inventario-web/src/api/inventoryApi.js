const BASE_URL = import.meta.env.VITE_API_URL;

async function request(path, { role = "SUPERVISOR", ...options } = {}) {
  const response = await fetch(`${BASE_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-Mock-Role": role,
      ...options.headers,
    },
  });
  const body = await response.json();
  return { status: response.status, body };
}

export const getErrorMessage = ({ body }) =>
  body?.errors?.[0]?.detail || body?.message || body?.title || "Ocurrió un error.";

export const getInventory = () => request("/inventory");
export const getMovements = (query = "") => request(`/inventory-movements${query}`);

export const getReceptions = () => request("/receptions");
export const createReception = (data) =>
  request("/receptions", { method: "POST", body: JSON.stringify(data) });

export const getTransfers = () => request("/transfers");
export const createTransfer = (data) =>
  request("/transfers", { method: "POST", body: JSON.stringify(data) });

export const getAdjustments = () => request("/adjustments");
export const createAdjustment = (data) =>
  request("/adjustments", {
    method: "POST",
    role: "DESPACHADOR",
    body: JSON.stringify(data),
  });
export const approveAdjustment = (id) =>
  request(`/adjustments/${id}/approve`, { method: "POST" });
export const rejectAdjustment = (id, reason) =>
  request(`/adjustments/${id}/reject`, {
    method: "POST",
    body: JSON.stringify({ rejectionReason: reason }),
  });