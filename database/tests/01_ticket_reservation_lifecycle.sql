  
-- Proyecto: Fuel Management System
-- Archivo: 01_ticket_reservation_lifecycle.sql
-- Descripción: Prueba del ciclo de vida de una reserva
-- Base de datos: fuel_management_db
-- Motor: PostgreSQL
  

BEGIN;

  
-- ESTADO INICIAL
  

SELECT
    '1 - ESTADO INICIAL' AS prueba,
    estacion_codigo,
    combustible_codigo,
    stock_fisico,
    stock_reservado,
    stock_disponible
FROM vw_stock_disponible_estacion_combustible
WHERE estacion_codigo = 'EST-001'
  AND combustible_codigo = 'DIESEL';


  
-- PRUEBA 1: TICKET ENVIADO
  
-- ENVIADO continúa reservando combustible.
-- Esperado:
-- físico     = 800
-- reservado  = 50
-- disponible = 750
  

UPDATE ticket
SET estado = 'ENVIADO',
    fecha_envio = NOW()
WHERE numero_ticket = 'COM-2026-000001';


SELECT
    '2 - TICKET ENVIADO' AS prueba,
    estacion_codigo,
    combustible_codigo,
    stock_fisico,
    stock_reservado,
    stock_disponible
FROM vw_stock_disponible_estacion_combustible
WHERE estacion_codigo = 'EST-001'
  AND combustible_codigo = 'DIESEL';


  
-- PRUEBA 2: TICKET CONSUMIDO
  
-- Un ticket consumido deja de reservar.
--
-- IMPORTANTE:
-- Esta prueba solo cambia el estado del ticket.
-- Todavía NO estamos simulando un despacho completo,
-- por lo que stock_actual no debe bajar aquí.
--
-- Esperado:
-- físico     = 800
-- reservado  = 0
-- disponible = 800
  

UPDATE ticket
SET estado = 'CONSUMIDO'
WHERE numero_ticket = 'COM-2026-000001';


SELECT
    '3 - TICKET CONSUMIDO' AS prueba,
    estacion_codigo,
    combustible_codigo,
    stock_fisico,
    stock_reservado,
    stock_disponible
FROM vw_stock_disponible_estacion_combustible
WHERE estacion_codigo = 'EST-001'
  AND combustible_codigo = 'DIESEL';


  
-- PRUEBA 3: TICKET ANULADO
  
-- Primero restauramos el ticket a CREADO y luego lo anulamos.
-- Un ticket anulado tampoco reserva combustible.
  

UPDATE ticket
SET estado = 'CREADO',
    fecha_envio = NULL
WHERE numero_ticket = 'COM-2026-000001';


UPDATE ticket
SET
    estado = 'ANULADO',
    fecha_anulacion = NOW(),
    anulado_por_usuario_id = (
        SELECT id
        FROM usuario
        WHERE nombre_usuario = 'supervisor.test'
    ),
    motivo_anulacion = 'Anulación utilizada para prueba del ciclo de reserva.'
WHERE numero_ticket = 'COM-2026-000001';


SELECT
    '4 - TICKET ANULADO' AS prueba,
    estacion_codigo,
    combustible_codigo,
    stock_fisico,
    stock_reservado,
    stock_disponible
FROM vw_stock_disponible_estacion_combustible
WHERE estacion_codigo = 'EST-001'
  AND combustible_codigo = 'DIESEL';


  
-- PRUEBA 4: TICKET VENCIDO POR FECHA
  
-- VENCIDO no se guarda como estado físico en nuestra BD.
-- Se deriva mediante fecha_expiracion.
--
-- Restauramos el ticket a CREADO pero colocamos una fecha
-- de expiración anterior al momento actual.
  

UPDATE ticket
SET
    estado = 'CREADO',
    fecha_anulacion = NULL,
    anulado_por_usuario_id = NULL,
    motivo_anulacion = NULL,
    fecha_expiracion = NOW() - INTERVAL '1 hour'
WHERE numero_ticket = 'COM-2026-000001';


SELECT
    '5 - TICKET VENCIDO' AS prueba,
    estacion_codigo,
    combustible_codigo,
    stock_fisico,
    stock_reservado,
    stock_disponible
FROM vw_stock_disponible_estacion_combustible
WHERE estacion_codigo = 'EST-001'
  AND combustible_codigo = 'DIESEL';


  
-- DESHACER TODAS LAS MODIFICACIONES
  

ROLLBACK; 