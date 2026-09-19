import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { tanksApi } from '../api/tanks.api'
import { getMyAdjustments, reportAdjustment, type AdjustmentStatus } from '../api/adjustments.api'
import type { Tank } from '../types/tank'

export function AdjustmentPage() {
 const { user, session, refreshSession }=useAuth(); const [tanks,setTanks]=useState<Tank[]>([]); const [tankId,setTankId]=useState(''); const [count,setCount]=useState(''); const [reason,setReason]=useState(''); const [message,setMessage]=useState('')
 const [reports,setReports]=useState<AdjustmentStatus[]>([])
 const loadReports=()=>{if(session)getMyAdjustments(session,refreshSession).then(setReports).catch(e=>setMessage(e.message))}
 useEffect(()=>{if(session&&user?.stationId)tanksApi.getStationTanks(user.stationId,session,{refreshSession}).then(setTanks).catch(e=>setMessage(e.message))},[session,user?.stationId,refreshSession])
 useEffect(loadReports,[session,refreshSession])
 async function submit(e:FormEvent){e.preventDefault();if(!session)return;try{const r=await reportAdjustment(session,refreshSession,{tanqueId:tankId,conteoFisico:Number(count),motivo:reason});setMessage(`Reporte ${r.estado}. No se modificó el stock.`);loadReports()}catch(e){setMessage(e instanceof Error?e.message:'Error') }}
 return <main className="closure-page"><header className="closure-header"><h1>Reportar ajuste</h1></header><form className="closure-card" onSubmit={submit}><select required value={tankId} onChange={e=>setTankId(e.target.value)}><option value="">Seleccione tanque</option>{tanks.map(t=><option key={t.id} value={t.id}>{t.codigo}</option>)}</select><input required min="0" type="number" step="0.01" value={count} onChange={e=>setCount(e.target.value)} placeholder="Conteo físico"/><textarea required value={reason} onChange={e=>setReason(e.target.value)} placeholder="Motivo"/><button>Enviar para aprobación</button>{message&&<p>{message}</p>}</form><section className="closure-card"><h2>Mis reportes</h2>{reports.map(r=><article key={r.id}><strong>{r.tanqueCodigo}</strong> · {r.conteoFisico} gal · <b>{r.estado}</b><p>{r.motivo}</p>{r.motivoRechazo&&<p>Rechazo: {r.motivoRechazo}</p>}</article>)}</section></main>
}
