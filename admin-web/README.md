# Portal Administrativo — Sistema de Gestión de Combustible

> **Responsable:** Angel  
> **Alcance:** Portal Web Administrativo, Layout General, Maestros (Usuarios, Empleados, Vehículos, Departamentos) y Dashboard Ejecutivo.  
> **Alineado con:** `SDP_Individual_Angel_Portal_Administrativo.html` y `Contrato_Tecnico_y_Cronograma_Integracion.html` (Baseline Sprint 1 y 2).

---

## 🚀 Cómo abrir y ejecutar en Visual Studio / VS Code

### Opción A: En Visual Studio 2022
1. Abre **Visual Studio 2022**.
2. Selecciona **"Abrir un proyecto o una solución"** y selecciona el archivo [`admin-web.esproj`](./admin-web.esproj).
3. Presiona el botón verde de inicio (o `F5` / `Ctrl+F5`) para iniciar el servidor de desarrollo (`npm run dev`).

### Opción B: En Visual Studio Code o Terminal
1. Abre la carpeta `admin-web` en VS Code.
2. Ejecuta en la terminal integrada:
   ```bash
   npm run dev
   ```
3. Accede en el navegador a: `http://localhost:5173`.

---

## 🔑 Credenciales Demo Rápidas

Para facilitar las pruebas de demostración y evaluación, la pantalla de Login cuenta con botones de acceso directo:

| Rol | Usuario | Contraseña | Permisos / Alcance |
| :--- | :--- | :--- | :--- |
| **ADMINISTRADOR** | `admin` | `Password123!` | Acceso total a Usuarios, Empleados, Vehículos, Departamentos y Dashboard. |
| **SUPERVISOR** | `supervisor` | `Password123!` | Dashboard, consulta y gestión de Empleados, Vehículos y Departamentos. |
| **DESPACHADOR** | `despachador` | `Password123!` | Asignado obligatoriamente a Estación Central Patio 1. Vista de Dashboard. |
| **SOLICITANTE** | `solicitante` | `Password123!` | Dashboard general y datos asociados a su departamento. |
| **AUDITOR** | `auditor` | `Password123!` | Dashboard de indicadores y consulta solo lectura. |

---

## 🔄 Conmutador Dual: Mock Demo vs API Real .NET 8

En la barra superior (**Header**) se incluye un interruptor interactivo:
* **Modo Mock Demo:** Permite realizar CRUD completo, probar validaciones, filtros y el dashboard inmediatamente sin requerir que la base de datos o el backend .NET 8 de Iván estén levantados (los cambios se guardan localmente).
* **Modo API Real .NET 8:** Consume directamente los endpoints bajo `/api/v1` de acuerdo al contrato REST (`https://localhost:7001/api/v1`).

---

## 🏛️ Estructura del Proyecto

```
admin-web/
├── admin-web.esproj           # Archivo de proyecto para Visual Studio 2022
├── index.html                 # Punto de entrada HTML
├── package.json               # Dependencias (React 18, Vite, TypeScript, Lucide, Tailwind)
├── tailwind.config.js         # Paleta institucional del SDP
├── tsconfig.json              # Configuración TypeScript
├── vite.config.ts             # Configuración Vite y alias @/
└── src/
    ├── main.tsx               # Montaje React + Router + AuthProvider
    ├── App.tsx                # Rutas y guardas RBAC
    ├── index.css              # Estilos globales y utilidades
    ├── types/                 # DTOs y tipos del contrato técnico
    │   └── index.ts
    ├── context/               # Manejo de sesión y autenticación
    │   └── AuthContext.tsx
    ├── services/              # Cliente HTTP Envelope y servicios
    │   ├── apiClient.ts       # Fetch, interceptores JWT y normalización de errores
    │   ├── api.ts            # Servicios tipados (Users, Employees, etc.)
    │   ├── mockStorage.ts    # Almacenamiento mock con reglas de negocio
    │   └── mockData.ts       # Datos semillas iniciales
    ├── components/
    │   ├── common/           # AlertBanner, Badge, ConfirmModal, LoadingSpinner
    │   └── layout/           # Sidebar, Header, MainLayout, ProtectedRoute
    └── pages/
        ├── Login.tsx          # Pantalla de acceso
        ├── Dashboard.tsx      # Dashboard ejecutivo (Inventario, Tickets, Consumos)
        ├── Users.tsx          # CRUD Usuarios (Rol único, estación despachador)
        ├── Employees.tsx      # CRUD Empleados
        ├── Vehicles.tsx       # CRUD Vehículos (Placa única, combustible, odómetro)
        └── Departments.tsx    # CRUD Departamentos (Soft delete)
```

---

## 📋 Reglas de Negocio Implementadas (Sección Angel)

1. **Usuarios:**
   - Un único rol por usuario.
   - Si el rol es `DESPACHADOR`, la estación asignada es obligatoria (`DISPATCHER_STATION_REQUIRED`).
   - No se expone password hash.
   - Protección para no desactivar al último Administrador activo (`LAST_ADMIN_PROTECTED`).
2. **Vehículos:**
   - Placa única en el sistema (`PLATE_ALREADY_EXISTS`).
   - Tipo de combustible obligatorio (`FUEL_TYPE_REQUIRED`).
   - Capacidad de tanque estrictamente positiva (`INVALID_TANK_CAPACITY`).
   - Odómetro no negativo y no regresivo (`ODOMETER_INVALID`).
   - Sin asignación permanente empleado-vehículo.
3. **Departamentos y Empleados:**
   - Código único para departamentos.
   - Empleado ≠ Usuario (un empleado no necesita tener cuenta obligatoriamente).
   - Soft delete mediante estado `isActive` (ninguna pantalla utiliza borrado físico destructivo).
4. **Dashboard Ejecutivo:**
   - Indicador de nivel crítico de inventario en tanques.
   - Solo lectura: no altera movimientos ni transacciones de inventario.
