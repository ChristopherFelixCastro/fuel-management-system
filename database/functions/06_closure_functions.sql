BEGIN;

CREATE OR REPLACE FUNCTION fn_calcular_cierre_diario(
    p_tanque_id UUID,
    p_fecha DATE
)
RETURNS TABLE (
    stock_inicial NUMERIC,
    total_recepciones NUMERIC,
    total_transferencias_entrada NUMERIC,
    total_transferencias_salida NUMERIC,
    total_despachos NUMERIC,
    total_ajustes_positivos NUMERIC,
    total_ajustes_negativos NUMERIC,
    stock_teorico_final NUMERIC
)
LANGUAGE plpgsql
STABLE
AS $$
BEGIN

    RETURN QUERY

    WITH movimientos_dia AS (
        SELECT *
        FROM movimiento_inventario
        WHERE tanque_id = p_tanque_id
          AND fecha_movimiento >= p_fecha::TIMESTAMP
          AND fecha_movimiento < (p_fecha + 1)::TIMESTAMP
    ),

    primer_movimiento AS (
        SELECT saldo_anterior
        FROM movimientos_dia
        ORDER BY fecha_movimiento ASC
        LIMIT 1
    ),

    resumen AS (
        SELECT
            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'RECEPCION'
                ), 0
            ) AS recepciones,

            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'TRANSFERENCIA_ENTRADA'
                ), 0
            ) AS transferencias_entrada,

            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'TRANSFERENCIA_SALIDA'
                ), 0
            ) AS transferencias_salida,

            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'DESPACHO'
                ), 0
            ) AS despachos,

            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'AJUSTE_POSITIVO'
                ), 0
            ) AS ajustes_positivos,

            COALESCE(
                SUM(cantidad) FILTER (
                    WHERE tipo_movimiento = 'AJUSTE_NEGATIVO'
                ), 0
            ) AS ajustes_negativos

        FROM movimientos_dia
    )

    SELECT
        COALESCE(
            (SELECT saldo_anterior FROM primer_movimiento),
            (
                SELECT stock_actual
                FROM tanque
                WHERE id = p_tanque_id
            )
        )::NUMERIC,

        r.recepciones::NUMERIC,
        r.transferencias_entrada::NUMERIC,
        r.transferencias_salida::NUMERIC,
        r.despachos::NUMERIC,
        r.ajustes_positivos::NUMERIC,
        r.ajustes_negativos::NUMERIC,

        (
            COALESCE(
                (SELECT saldo_anterior FROM primer_movimiento),
                (
                    SELECT stock_actual
                    FROM tanque
                    WHERE id = p_tanque_id
                )
            )
            + r.recepciones
            + r.transferencias_entrada
            - r.transferencias_salida
            - r.despachos
            + r.ajustes_positivos
            - r.ajustes_negativos
        )::NUMERIC

    FROM resumen r;

END;
$$;

COMMIT;