import { useEffect, useRef, useState } from 'react'
import {
  Html5Qrcode,
  Html5QrcodeSupportedFormats,
} from 'html5-qrcode'
import { useNavigate } from 'react-router-dom'

import { ticketsApi } from '../api/tickets.api'
import { useAuth } from '../contexts/AuthContext'
import { ApiError } from '../types/ticket'

const READER_ID = 'qr-reader'
const REJECTED_QR_COOLDOWN_MS = 3000

function getFriendlyError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'No fue posible validar el ticket. Intente nuevamente.'
  }

  switch (error.code) {
    case 'TICKET_CONSUMED':
      return 'Este ticket ya fue utilizado.'

    case 'TICKET_EXPIRED':
      return 'Este ticket está vencido.'

    case 'TICKET_ANULADO':
      return 'Este ticket fue anulado.'

    case 'TICKET_INEXISTENTE':
      return 'El ticket no existe.'

    case 'FIRMA_INVALIDA':
      return 'El código QR no es válido o fue alterado.'

    case 'FORBIDDEN':
      return 'Este ticket no corresponde a su estación.'

    default:
      return error.message
  }
}

export function ScanPage() {
  const navigate = useNavigate()

  const {
    session,
    refreshSession,
  } = useAuth()

  const scannerRef = useRef<Html5Qrcode | null>(null)
  const processingRef = useRef(false)

  const lastRejectedQrRef = useRef<{
    value: string
    rejectedAt: number
  } | null>(null)

  const [error, setError] = useState<string | null>(null)
  const [isProcessing, setIsProcessing] = useState(false)

  useEffect(() => {
    // Reiniciamos el bloqueo cada vez que se monta el escáner.
    processingRef.current = false
    lastRejectedQrRef.current = null

    const scanner = new Html5Qrcode(READER_ID, {
      verbose: false,
      formatsToSupport: [
        Html5QrcodeSupportedFormats.QR_CODE,
      ],
    })

    scannerRef.current = scanner

    let disposed = false

    async function startScanner() {
      try {
        await scanner.start(
          {
            facingMode: 'environment',
          },
          {
            fps: 15,
            qrbox: (
              viewfinderWidth,
              viewfinderHeight,
            ) => {
              const minEdge = Math.min(
                viewfinderWidth,
                viewfinderHeight,
              )

              const size = Math.floor(minEdge * 0.8)

              return {
                width: size,
                height: size,
              }
            },
            aspectRatio: 1.333333,
          },

          async (decodedText) => {
            // El componente ya fue desmontado.
            if (disposed) {
              return
            }

            // No intentamos validar sin una sesión activa.
            if (!session) {
              return
            }

            /*
             * Si este mismo QR acaba de ser rechazado,
             * evitamos bombardear el backend mientras
             * permanece frente a la cámara.
             */
            const lastRejected =
              lastRejectedQrRef.current

            if (
              lastRejected?.value === decodedText &&
              Date.now() - lastRejected.rejectedAt <
                REJECTED_QR_COOLDOWN_MS
            ) {
              return
            }

            /*
             * Evita procesar varios frames simultáneamente
             * mientras una validación está en curso.
             */
            if (processingRef.current) {
              return
            }

            processingRef.current = true
            setIsProcessing(true)
            setError(null)

            try {
              const ticket =
                await ticketsApi.validateQr(
                  decodedText,
                  session,
                  {
                    refreshSession,
                  },
                )

              /*
               * Si el QR anteriormente había sido
               * rechazado, limpiamos ese estado porque
               * ahora la validación fue exitosa.
               */
              lastRejectedQrRef.current = null

              try {
                if (scanner.isScanning) {
                  await scanner.stop()
                }
              } catch {
                // El escáner puede haberse detenido previamente.
              }

              if (disposed) {
                return
              }

              navigate('/ticket', {
                state: {
                  ticket,
                },
              })
            } catch (validationError) {
              console.error(
                '[QR] Error de validación:',
                validationError,
              )

              if (disposed) {
                return
              }

              /*
               * Registramos el QR rechazado para aplicar
               * el cooldown antes de permitir otro intento
               * con exactamente el mismo contenido.
               */
              lastRejectedQrRef.current = {
                value: decodedText,
                rejectedAt: Date.now(),
              }

              setError(
                getFriendlyError(validationError),
              )

              /*
               * Liberamos el procesamiento. Si el mismo
               * QR continúa visible, el cooldown evita
               * solicitudes continuas al backend.
               */
              processingRef.current = false
              setIsProcessing(false)
            }
          },

          () => {
            /*
             * Los frames sin un QR decodificable son
             * normales y no se muestran como error.
             */
          },
        )
      } catch (cameraError) {
        console.error(
          '[QR] Error al iniciar la cámara:',
          cameraError,
        )

        if (!disposed) {
          setError(
            'No fue posible acceder a la cámara. Verifique los permisos del navegador.',
          )
        }
      }
    }

    void startScanner()

    return () => {
      disposed = true

      const activeScanner = scannerRef.current

      if (activeScanner?.isScanning) {
        void activeScanner
          .stop()
          .catch(() => undefined)
      }

      scannerRef.current = null
    }
  }, [navigate, session, refreshSession])

  return (
    <main className="scanner-page">
      <header className="scanner-header">
        <button
          type="button"
          className="button-secondary"
          onClick={() => navigate('/')}
        >
          ← Volver
        </button>

        <div>
          <h1>Escanear ticket</h1>
          <p>
            Coloque el código QR dentro del recuadro.
          </p>
        </div>
      </header>

      <section className="scanner-card">
        {isProcessing && (
          <div className="scanner-status" style={{ marginBottom: '1rem' }}>
            <span className="spinner" />
            <span>Validando ticket...</span>
          </div>
        )}

        {error && (
          <div
            className="scanner-error"
            role="alert"
            style={{ marginBottom: '1rem' }}
          >
            <strong>No se puede continuar</strong>
            <span>{error}</span>
          </div>
        )}

        <div
          id={READER_ID}
          className="qr-reader"
        />

        <p className="scanner-help">
          La validación requiere conexión con el
          servidor. El despacho no puede realizarse
          sin conexión.
        </p>
      </section>
    </main>
  )
}