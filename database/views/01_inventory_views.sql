

BEGIN;

 
-- 1. VISTA: vw_stock_fisico_estacion_combustible
 
-- Calcula el stock físico actual agrupado por estación
-- y tipo de combustible, considerando solo tanques activos.
 

CREATE OR REPLACE VIEW vw_stock_fisico_estacion_combustible AS
SELECT
    t.estacion_id,
    e.codigo AS estacion_codigo,
    e.nombre AS estacion_nombre,
    t.tipo_combustible_id,
    tc.codigo AS combustible_codigo,
    tc.nombre AS combustible_nombre,
    SUM(t.stock_actual) AS stock_fisico
FROM tanque t
INNER JOIN estacion e
    ON e.id = t.estacion_id
INNER JOIN tipo_combustible tc
    ON tc.id = t.tipo_combustible_id
WHERE t.activo = TRUE
GROUP BY
    t.estacion_id,
    e.codigo,
    e.nombre,
    t.tipo_combustible_id,
    tc.codigo,
    tc.nombre;


 
-- 2. VISTA: vw_stock_reservado_estacion_combustible
 
-- Calcula el combustible reservado mediante tickets
-- activos que todavía no han vencido.
--
-- Un ticket activo se considera reserva cuando:
-- estado = CREADO o ENVIADO
-- y fecha_expiracion > NOW()
 

CREATE OR REPLACE VIEW vw_stock_reservado_estacion_combustible AS
SELECT
    tk.estacion_id,
    e.codigo AS estacion_codigo,
    e.nombre AS estacion_nombre,
    tk.tipo_combustible_id,
    tc.codigo AS combustible_codigo,
    tc.nombre AS combustible_nombre,
    SUM(tk.cantidad_autorizada) AS stock_reservado
FROM ticket tk
INNER JOIN estacion e
    ON e.id = tk.estacion_id
INNER JOIN tipo_combustible tc
    ON tc.id = tk.tipo_combustible_id
WHERE tk.estado IN ('CREADO', 'ENVIADO')
  AND tk.fecha_expiracion > NOW()
GROUP BY
    tk.estacion_id,
    e.codigo,
    e.nombre,
    tk.tipo_combustible_id,
    tc.codigo,
    tc.nombre;


 
-- 3. VISTA: vw_stock_disponible_estacion_combustible
 
-- Calcula:
--
-- Stock disponible = Stock físico - Stock reservado
--
-- LEFT JOIN permite que aparezca stock físico aunque todavía
-- no existan tickets reservando ese combustible.
 

CREATE OR REPLACE VIEW vw_stock_disponible_estacion_combustible AS
SELECT
    sf.estacion_id,
    sf.estacion_codigo,
    sf.estacion_nombre,
    sf.tipo_combustible_id,
    sf.combustible_codigo,
    sf.combustible_nombre,
    sf.stock_fisico,
    COALESCE(sr.stock_reservado, 0) AS stock_reservado,
    sf.stock_fisico - COALESCE(sr.stock_reservado, 0) AS stock_disponible
FROM vw_stock_fisico_estacion_combustible sf
LEFT JOIN vw_stock_reservado_estacion_combustible sr
    ON sr.estacion_id = sf.estacion_id
   AND sr.tipo_combustible_id = sf.tipo_combustible_id;

COMMIT;