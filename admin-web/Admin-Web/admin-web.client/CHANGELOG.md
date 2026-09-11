En este archivo se explica cómo Visual Studio creado el proyecto.

Se usaron las siguientes herramientas para generar este proyecto:
- create-vite

Los pasos siguientes se usaron para generar este proyecto:
- Cree un proyecto de React con create-vite: `npm init --yes vite@latest admin-web.client -- --template=react-ts  --no-rolldown --no-immediate`.
- Actualizar `vite.config.ts` para configurar el proxy y los certificados.
- Agregar `@type/node` para `vite.config.js` escribir.
- Actualice el componente `App` para capturar y mostrar información meteorológica.
- Crear archivo de proyecto (`admin-web.client.esproj`).
- Crear `launch.json` para habilitar la depuración.
- Agregue el proyecto a la solución.
- Actualice el punto de conexión de proxy para que sea el punto de conexión del servidor back-end.
- Agregar proyecto a la lista de proyectos de inicio.
- Escriba este archivo.
