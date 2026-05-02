# AcademiaDigital — Frontend Angular

![Angular](https://img.shields.io/badge/Angular-21-DD0031?style=for-the-badge&logo=angular&logoColor=white)
![TypeScript](https://img.shields.io/badge/TypeScript-5-3178C6?style=for-the-badge&logo=typescript&logoColor=white)
![NgRx](https://img.shields.io/badge/NgRx-Store-BA2BD2?style=for-the-badge&logo=reactivex&logoColor=white)

Aplicación Angular 21 del proyecto AcademiaDigital. Usa **NgRx** para gestión de estado, **ngx-translate** para internacionalización y **Bootstrap 5** para estilos.

---

## Requisitos previos

| Herramienta | Versión mínima | Verificar |
|-------------|---------------|-----------|
| Node.js | 20 LTS | `node -v` |
| npm | 11 | `npm -v` |
| Angular CLI | 21 | `ng version` |

> Si no tenés Angular CLI instalado: `npm install -g @angular/cli`

---

## Instalación

```bash
# Desde la raíz del repositorio
cd frontend-angular

# Instalar dependencias
npm install
```

---

## Levantar en desarrollo

```bash
npm start
```

El servidor de desarrollo queda corriendo en **http://localhost:4200**.

El archivo [proxy.conf.json](proxy.conf.json) redirige automáticamente todas las llamadas a `/api/*` hacia el backend en `http://localhost:5073`, por lo que no hace falta configurar CORS manualmente durante el desarrollo.

---

## Variables de entorno

Los archivos de entorno viven en [src/environments/](src/environments/):

| Archivo | Uso |
|---------|-----|
| `environment.ts` | Desarrollo (por defecto con `npm start`) |
| `environment.prod.ts` | Producción (`ng build --configuration production`) |

No se requiere ninguna variable de entorno adicional para correr localmente — el proxy se encarga del enrutamiento al backend.

---

## Comandos útiles

```bash
# Servidor de desarrollo (con proxy al backend)
npm start

# Build de producción
npm run build

# Build en modo watch (reconstruye al guardar)
npm run watch

# Correr tests unitarios
npm test
```

---

## Estructura del proyecto

```
src/
├── app/
│   ├── core/          # Servicios singleton, guards, interceptors
│   ├── features/      # Módulos de funcionalidad (alumnos, carreras, etc.)
│   ├── shared/        # Componentes, pipes y directivas reutilizables
│   └── store/         # NgRx: actions, reducers, effects, selectors
├── assets/
│   └── i18n/          # Archivos de traducción (es.json, en.json)
├── environments/      # Configuraciones por entorno
└── styles.scss        # Estilos globales
```

---

## Troubleshooting

### `ng` o `npm start` da "command not found"

- Verificá que Node.js esté instalado: `node -v`
- Si Node está pero `ng` no: `npm install -g @angular/cli`
- Si `ng` sigue sin encontrarse después de instalarlo, cerrá y volvé a abrir la terminal para que el PATH se actualice

### `npm install` falla con errores de permisos

No uses `sudo npm install`. En su lugar, corregí los permisos de npm:
```bash
mkdir -p ~/.npm-global
npm config set prefix ~/.npm-global
echo 'export PATH="$PATH:$HOME/.npm-global/bin"' >> ~/.bashrc
source ~/.bashrc
```

### La app carga pero las llamadas a `/api` fallan

- Verificá que el backend esté corriendo en **http://localhost:5073** (ver [../backend-dotnet/README.md](../backend-dotnet/README.md))
- El proxy solo funciona con `npm start` — si abrís el HTML directamente en el browser, el proxy no aplica

### Error de versión de Node

El proyecto usa Angular 21 que requiere Node 20+. Verificá con `node -v`. Si tenés una versión anterior, actualizá con [nvm](https://github.com/nvm-sh/nvm):
```bash
nvm install 20
nvm use 20
```

---

## Flujo esperado para desarrollo local

1. Levantar el backend .NET (ver [../backend-dotnet/README.md](../backend-dotnet/README.md))
2. Asegurarse de que el backend corra en el puerto **5073**
3. Ejecutar `npm start` en esta carpeta
4. Abrir **http://localhost:4200**
