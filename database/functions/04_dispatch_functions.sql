-- =========================================================
-- Proyecto: Fuel Management System
-- Archivo: 04_dispatch_functions.sql
-- Descripción: Registro atómico de despacho de combustible
-- Base de datos: fuel_management_db
-- Motor: PostgreSQL
-- =========================================================

BEGIN;

CREATE OR REPLACE FUNCTION fn_registrar_despacho(
    p_ticket_id UUID,
    p_despachador_usuario_id UUID,
    p_tanque_id UUID,
    p_cantidad_despachada NUMERIC(10,2),
    p_odometro_registrado NUMERIC(12,2),
    p_observaciones VARCHAR(500) DEFAULT NULL,
    p_direccion_ip VARCHAR(45) DEFAULT NULL
)
RETURNS UUID
LANGUAGE plpgsql
VOLATILE
AS $$
DECLARE
    v_despacho_id UUID;

    v_ticket_estado VARCHAR(25);
    v_ticket_expiracion TIMESTAMPTZ;
    v_ticket_cantidad NUMERIC(10,2);
    v_ticket_estacion UUID;
    v_ticket_combustible SMALLINT;
    v_solicitud_id UUID;

    v_vehiculo_id UUID;
    v_odometro_actual NUMERIC(12,2);

    v_tanque_estacion UUID;
    v_tanque_combustible SMALLINT;
    v_stock_actual NUMERIC(12,2);
    v_tanque_activo BOOLEAN;

    v_usuario_estacion UUID;
    v_usuario_activo BOOLEAN;
    v_rol_usuario VARCHAR(30);

    v_saldo_posterior NUMERIC(12,2);
BEGIN

    
    -- 1. VALIDAR DESPACHADOR
    

    SELECT
        u.estacion_id,
        u.activo,
        r.nombre
    INTO
        v_usuario_estacion,
        v_usuario_activo,
        v_rol_usuario
    FROM usuario u
    INNER JOIN rol r
        ON r.id = u.rol_id
    WHERE u.id = p_despachador_usuario_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'DESPACHADOR_INEXISTENTE';
    END IF;

    IF v_usuario_activo = FALSE THEN
        RAISE EXCEPTION 'DESPACHADOR_INACTIVO';
    END IF;

    IF v_rol_usuario <> 'DESPACHADOR' THEN
        RAISE EXCEPTION 'USUARIO_NO_ES_DESPACHADOR';
    END IF;

    IF v_usuario_estacion IS NULL THEN
        RAISE EXCEPTION 'DESPACHADOR_SIN_ESTACION';
    END IF;


    
    -- 2. BLOQUEAR Y VALIDAR TICKET
    
    -- FOR UPDATE evita que dos despachos puedan consumir
    -- simultáneamente el mismo ticket.
    

    SELECT
        tk.estado,
        tk.fecha_expiracion,
        tk.cantidad_autorizada,
        tk.estacion_id,
        tk.tipo_combustible_id,
        tk.solicitud_id
    INTO
        v_ticket_estado,
        v_ticket_expiracion,
        v_ticket_cantidad,
        v_ticket_estacion,
        v_ticket_combustible,
        v_solicitud_id
    FROM ticket tk
    WHERE tk.id = p_ticket_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TICKET_INEXISTENTE';
    END IF;

    IF v_ticket_estado = 'CONSUMIDO' THEN
        RAISE EXCEPTION 'TICKET_CONSUMIDO';
    END IF;

    IF v_ticket_estado = 'ANULADO' THEN
        RAISE EXCEPTION 'TICKET_ANULADO';
    END IF;

    IF v_ticket_estado NOT IN ('CREADO', 'ENVIADO') THEN
        RAISE EXCEPTION 'TICKET_ESTADO_INVALIDO';
    END IF;

    IF v_ticket_expiracion <= NOW() THEN
        RAISE EXCEPTION 'TICKET_VENCIDO';
    END IF;


    
    -- 3. VALIDAR ESTACIÓN DEL DESPACHADOR
    

    IF v_usuario_estacion <> v_ticket_estacion THEN
        RAISE EXCEPTION 'DESPACHADOR_ESTACION_INVALIDA';
    END IF;


    
    -- 4. VALIDAR CANTIDAD
    

    IF p_cantidad_despachada IS NULL
       OR p_cantidad_despachada <= 0 THEN
        RAISE EXCEPTION 'CANTIDAD_DESPACHO_INVALIDA';
    END IF;

    IF p_cantidad_despachada <> v_ticket_cantidad THEN
        RAISE EXCEPTION
            'CANTIDAD_DEBE_SER_IGUAL_A_AUTORIZADA';
    END IF;


    
    -- 5. BLOQUEAR Y VALIDAR TANQUE
    

    SELECT
        t.estacion_id,
        t.tipo_combustible_id,
        t.stock_actual,
        t.activo
    INTO
        v_tanque_estacion,
        v_tanque_combustible,
        v_stock_actual,
        v_tanque_activo
    FROM tanque t
    WHERE t.id = p_tanque_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'TANQUE_INEXISTENTE';
    END IF;

    IF v_tanque_activo = FALSE THEN
        RAISE EXCEPTION 'TANQUE_INACTIVO';
    END IF;

    IF v_tanque_estacion <> v_ticket_estacion THEN
        RAISE EXCEPTION 'TANQUE_ESTACION_INVALIDA';
    END IF;

    IF v_tanque_combustible <> v_ticket_combustible THEN
        RAISE EXCEPTION 'COMBUSTIBLE_TANQUE_INVALIDO';
    END IF;

    IF v_stock_actual < p_cantidad_despachada THEN
        RAISE EXCEPTION 'STOCK_FISICO_INSUFICIENTE';
    END IF;


    
    -- 6. OBTENER VEHÍCULO DE LA SOLICITUD
    

    SELECT
        s.vehiculo_id
    INTO
        v_vehiculo_id
    FROM solicitud s
    WHERE s.id = v_solicitud_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'SOLICITUD_INEXISTENTE';
    END IF;


    
    -- 7. BLOQUEAR Y VALIDAR ODOMETRO
    

    SELECT
        v.odometro_actual
    INTO
        v_odometro_actual
    FROM vehiculo v
    WHERE v.id = v_vehiculo_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'VEHICULO_INEXISTENTE';
    END IF;

    IF p_odometro_registrado IS NULL
       OR p_odometro_registrado < 0 THEN
        RAISE EXCEPTION 'ODOMETRO_INVALIDO';
    END IF;

    IF p_odometro_registrado < v_odometro_actual THEN
        RAISE EXCEPTION 'ODOMETRO_MENOR_AL_REGISTRADO';
    END IF;


    
    -- 8. CALCULAR NUEVO SALDO
    

    v_saldo_posterior :=
        v_stock_actual - p_cantidad_despachada;


    
    -- 9. CREAR DESPACHO
    

    INSERT INTO despacho (
        ticket_id,
        despachador_usuario_id,
        tanque_id,
        cantidad_despachada,
        odometro_registrado,
        fecha_despacho,
        observaciones,
        direccion_ip
    )
    VALUES (
        p_ticket_id,
        p_despachador_usuario_id,
        p_tanque_id,
        p_cantidad_despachada,
        p_odometro_registrado,
        NOW(),
        p_observaciones,
        p_direccion_ip
    )
    RETURNING id
    INTO v_despacho_id;


    
    -- 10. ACTUALIZAR STOCK FÍSICO
    

    UPDATE tanque
    SET
        stock_actual = v_saldo_posterior,
        fecha_actualizacion = NOW()
    WHERE id = p_tanque_id;


    
    -- 11. ACTUALIZAR ODOMETRO
    

    UPDATE vehiculo
    SET
        odometro_actual = p_odometro_registrado,
        fecha_actualizacion = NOW()
    WHERE id = v_vehiculo_id;


    
    -- 12. CONSUMIR TICKET
    

    UPDATE ticket
    SET estado = 'CONSUMIDO'
    WHERE id = p_ticket_id;


    
    -- 13. REGISTRAR MOVIMIENTO DE INVENTARIO
    

    INSERT INTO movimiento_inventario (
        tanque_id,
        registrado_por_usuario_id,
        tipo_movimiento,
        cantidad,
        saldo_anterior,
        saldo_posterior,
        despacho_id,
        fecha_movimiento,
        observaciones
    )
    VALUES (
        p_tanque_id,
        p_despachador_usuario_id,
        'DESPACHO',
        p_cantidad_despachada,
        v_stock_actual,
        v_saldo_posterior,
        v_despacho_id,
        NOW(),
        p_observaciones
    );


    
    -- 14. DEVOLVER ID DEL DESPACHO
    

    RETURN v_despacho_id;

END;
$$;

COMMIT;