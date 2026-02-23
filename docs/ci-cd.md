# CI/CD

Dos workflows de GitHub Actions en `.github/workflows/`.

---

## CI Pipeline: `ci.yml`

**Trigger**: push a `main` o pull request a `main`.
**Runner**: `ubuntu-latest`.

### Pasos

1. **Checkout** del codigo.
2. **Setup .NET 10** (`dotnet-version: "10.0.x"`).
3. **Restore** dependencias.
4. **Build** en modo Release.
5. **Tests con coverage**: `dotnet test` con coverlet que genera `coverage.cobertura.xml`.
6. **Coverage summary**: script PowerShell que parsea el XML y genera un summary en GitHub Actions (tabla con line coverage y branch coverage).
7. **Upload artifacts**: sube el reporte de cobertura y los resultados de tests como artefactos del workflow.

### Que pasa si falla

- Si el build falla → el workflow falla, el PR se bloquea (si hay branch protection).
- Si un test falla → el workflow falla.
- Si la cobertura baja → el summary se muestra pero no bloquea (no hay threshold configurado).

---

## Release Pipeline: `release.yml`

**Trigger**: push de un tag con formato `v*.*.*` (ej: `v1.5.0`).
**Runner**: matrix build (3 jobs paralelos).

### Matrix build

| OS | RID | Artefacto |
|---|---|---|
| `windows-latest` | `win-x64` | `PlatzDaemon-win-x64-v*.*.*.zip` |
| `ubuntu-latest` | `linux-x64` | `PlatzDaemon-linux-x64-v*.*.*.tar.gz` |
| `macos-latest` | `osx-arm64` | `PlatzDaemon-osx-arm64-v*.*.*.tar.gz` |

### Pasos del job `build` (por cada OS)

1. **Checkout**.
2. **Setup .NET 10**.
3. **Tests**: `dotnet test --configuration Release`. Si fallan, el release se cancela.
4. **Publish**: `dotnet publish PlatzDaemon.csproj` con:
   - `-c Release` — optimizaciones de produccion.
   - `-r {rid}` — runtime identifier para el OS target.
   - `--self-contained true` — incluye el runtime de .NET en el binario.
   - `-p:PublishSingleFile=true` — genera un unico ejecutable.
   - `-p:IncludeNativeLibrariesForSelfExtract=true` — extrae librerias nativas al ejecutar.
5. **Empaquetar**:
   - Windows: `Compress-Archive` → `.zip`.
   - Linux/macOS: `tar -czf` → `.tar.gz`.
6. **Upload artefacto**: sube el paquete como artefacto del workflow.

### Job `release` (despues de los 3 builds)

1. **Checkout** (para acceder a `CHANGELOG.md`).
2. **Download artifacts**: baja los 3 paquetes.
3. **Extraer notas del CHANGELOG**:
   ```bash
   version="${{ github.ref_name }}"
   version="${version#v}"  # Quita el prefijo "v"
   notes=$(awk "/^## \[${version}\]/{found=1; next} /^## \[/{if(found) exit} found{print}" CHANGELOG.md)
   ```
   Este `awk` extrae todo el contenido entre `## [1.5.0]` y el siguiente `## [` (la version anterior).
4. **Crear Release**: usa `softprops/action-gh-release@v2` para crear un GitHub Release con:
   - Titulo: "PlatzDaemon v1.5.0".
   - Body: las notas extraidas del CHANGELOG.
   - Files: los 3 paquetes (ZIP + tar.gz).

---

## Como publicar una nueva version

### 1. Actualizar version

En `PlatzDaemon.csproj`:

```xml
<Version>1.6.0</Version>
```

### 2. Actualizar CHANGELOG

En `CHANGELOG.md`, agregar una nueva seccion al principio:

```markdown
## [1.6.0] - 2026-MM-DD

### Added
- ...

### Changed
- ...

### Fixed
- ...
```

### 3. Commit y tag

```bash
git add -A
git commit -m "release: v1.6.0"
git tag v1.6.0
git push origin main
git push origin v1.6.0
```

### 4. Verificar

En unos minutos, el Release aparece en la pestaña **Releases** del repositorio con los binarios de las 3 plataformas y las notas del changelog.

---

## Release manual (alternativa)

Si se prefiere no usar el workflow:

1. Compilar manualmente:
   ```bash
   dotnet publish PlatzDaemon.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
   ```
2. Comprimir `./publish/` en un ZIP.
3. En GitHub: Releases > Draft a new release > subir el ZIP.

---

## Notas tecnicas

### Por que `PlatzDaemon.csproj` en el publish

El `release.yml` especifica `dotnet publish PlatzDaemon.csproj` explicitamente (en vez de `dotnet publish` a secas) porque el directorio contiene dos proyectos (app + tests). Sin especificar, `dotnet publish` intentaria publicar ambos.

### Permisos

El workflow necesita `permissions: contents: write` para crear releases y subir artefactos.

### CHANGELOG y awk

El script de extraccion del CHANGELOG busca la seccion con `## [{version}]` y lee hasta la siguiente seccion `## [`. Si no encuentra la version (ej: por typo), el body del release sera "Release v1.6.0" como fallback.

Para que funcione correctamente, las secciones del CHANGELOG deben seguir el formato exacto: `## [1.6.0] - 2026-MM-DD`.
