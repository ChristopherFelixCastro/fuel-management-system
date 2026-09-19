import { useEffect, useState } from 'react'
import { useAuth } from '../contexts/AuthContext'
import { tanksApi } from '../api/tanks.api'
import type { Tank } from '../types/tank'

export function InventoryPage() {
  const { user, session, refreshSession } = useAuth(); const [tanks, setTanks] = useState<Tank[]>([]); const [error, setError] = useState('')
  useEffect(() => { if (session && user?.stationId) tanksApi.getStationTanks(user.stationId, session, { refreshSession }).then(setTanks).catch(e => setError(e.message)) }, [session, user?.stationId, refreshSession])
  return <main className="closure-page"><header className="closure-header"><h1>Existencias de mi estación</h1></header>{error && <p>{error}</p>}<section className="closure-card"><ul>{tanks.map(t => <li key={t.id}><strong>{t.codigo}</strong> — {t.nombre ?? 'Tanque'}: {t.stockActual} gal / capacidad {t.capacidadMaxima} gal</li>)}</ul></section></main>
}
