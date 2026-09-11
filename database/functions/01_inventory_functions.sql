
BEGIN;

 
-- FUNCIÓN:
-- fn_obtener_stock_disponible
--
-- Recibe:
--   p_estacion_id
--   p_tipo_combustible_id
--
-- Devuelve:
--   stock físico
--   stock reservado
--   stock disponible
--
-- Esta función NO modifica inventario.
 

CREATE OR REPLACE FUNCTION fn_obtener_stock_disponible(
    p_estacion_id UUID,
    p_tipo_combustible_id SMALLINT
)
RETURNS TABLE (
    stock_fisico NUMERIC,
    stock_reservado NUMERIC,
    stock_disponible NUMERIC
)
LANGUAGE sql
STABLE
AS $$
    SELECT
        v.stock_fisico,
        v.stock_reservado,
        v.stock_disponible
    FROM vw_stock_disponible_estacion_combustible v
    WHERE v.estacion_id = p_estacion_id
      AND v.tipo_combustible_id = p_tipo_combustible_id;
$$;

COMMIT;