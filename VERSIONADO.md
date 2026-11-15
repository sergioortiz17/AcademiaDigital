# Versionado

AcademiaDigital usa [versionado semántico 2.0.0](https://semver.org/lang/es/) con **una sola versión para toda la plataforma**: frontend, backend y Finance se despliegan juntos (un pipeline, un bundle de Beanstalk), así que comparten número.

## Dónde está la versión

| Archivo | Para qué |
|---|---|
| `VERSION` | **Fuente de verdad** |
| `frontend-angular/package.json` y `package-lock.json` | Versión del frontend |
| `backend-dotnet/src/Directory.Build.props` | Versión de los ensamblados del backend |
| `services/AcademiaDigital.Finance/src/Directory.Build.props` | Versión de los ensamblados de Finance |

No se editan a mano. Se cambian todos juntos con:

```bash
tools/version.sh set 1.1.0
```

Y se verifica con `tools/version.sh check`. CodeBuild corre ese chequeo en cada build: si los archivos no coinciden, el despliegue se corta antes de subir imágenes.

## Qué número subir

Formato `MAJOR.MINOR.PATCH`:

| Cambio | Se sube | Ejemplos en esta plataforma |
|---|---|---|
| Rompe algo que ya funcionaba para quien usa la plataforma o la API | **MAJOR** (`2.0.0`) | Cambiar o quitar un endpoint que usa el frontend u otra integración; una migración que obliga a recargar datos |
| Funcionalidad nueva compatible | **MINOR** (`1.1.0`) | Módulo de Cooperadora, un reporte nuevo, un endpoint nuevo |
| Corrección compatible | **PATCH** (`1.0.1`) | Un bug de calificaciones, un error de estilos, un texto mal escrito |

Los cambios que no llegan a producción (documentación, tests, infraestructura sin efecto en la aplicación) no necesitan subir la versión.

## Cómo se publica una versión

1. En la rama del cambio: `tools/version.sh set X.Y.Z`.
2. Agregar la sección `## [X.Y.Z] - AAAA-MM-DD` en `CHANGELOG.md` con lo que cambió (Agregado, Cambiado, Corregido, Eliminado).
3. Abrir el PR a `main`. El título lleva la versión: `release: vX.Y.Z — <resumen>`.
4. Después del merge, crear el tag sobre el commit de `main`:

   ```bash
   git switch main && git pull
   git tag -a vX.Y.Z -m "AcademiaDigital vX.Y.Z"
   git push origin vX.Y.Z
   ```

5. Opcional: crear el *release* en GitHub desde ese tag, copiando la sección del CHANGELOG.

## Trazabilidad en producción

- Las imágenes de ECR se etiquetan con el **SHA del commit** (tags inmutables).
- Cada imagen lleva las etiquetas OCI `org.opencontainers.image.version` (la versión SemVer) y `org.opencontainers.image.revision` (el commit).

Para saber qué versión tiene una imagen:

```bash
docker inspect --format '{{ index .Config.Labels "org.opencontainers.image.version" }}' <imagen>
```
