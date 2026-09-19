import { authenticatedFetch } from './authenticated-fetch'
import type { AuthSession } from '../types/auth'

const API_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5080'
export async function reportAdjustment(session: AuthSession, refreshSession: () => Promise<AuthSession | null>, payload: { tanqueId: string; conteoFisico: number; motivo: string; observaciones?: string }) {
  const response = await authenticatedFetch({ session, refreshSession, input: `${API_URL}/adjustments`, init: { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(payload) } })
  if (!response.ok) throw new Error((await response.json().catch(() => null))?.error?.message ?? 'No fue posible reportar el ajuste.')
  return (await response.json()).data as { id: string; estado: string }
}

export interface AdjustmentStatus { id:string; tanqueCodigo:string; conteoFisico:number; motivo:string; estado:'PENDIENTE'|'APROBADO'|'RECHAZADO'; fechaReporte:string; motivoRechazo?:string|null }
export async function getMyAdjustments(session: AuthSession, refreshSession: () => Promise<AuthSession | null>): Promise<AdjustmentStatus[]> {
  const response=await authenticatedFetch({session,refreshSession,input:`${API_URL}/adjustments/mine?page=1&pageSize=50`})
  if(!response.ok)throw new Error('No fue posible consultar tus reportes.')
  return ((await response.json()).data.items ?? []) as AdjustmentStatus[]
}
