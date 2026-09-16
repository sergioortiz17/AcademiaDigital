
| Servicio        | URL                                                            | Qué es                                                                                                           |
| ----------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------- |
| **Frontend**    | [http://localhost:4200](http://localhost:4200/)                | La app Angular real (login, inscripciones, calificaciones, admin, etc.)                                           |
| **Backend API** | [http://localhost:8000](http://localhost:8000/)                | API .NET real                                                                                                     |
| **Swagger**     | [http://localhost:8000/swagger](http://localhost:8000/swagger) | Documentación interactiva de la API (Swashbuckle)                                                                |
| **Scalar**      | [http://localhost:8000/scalar](http://localhost:8000/scalar)   | La otra UI de documentación de API (conviven ambas)                                                              |
| **DevTools**    | [http://localhost:8090](http://localhost:8090/)                | Panel de pruebas/testing (reset, seeds, comisiones, línea de tiempo de alumno, escenarios)                       |
| **Finance API** | [http://localhost:8091](http://localhost:8091/)                | El microservicio de Finance que Kiro viene armando por separado                                                   |
| **PostgreSQL**  | `localhost:5432` (no es navegador)                             | La base de datos — conectás con DBeaver/psql,`Database=AcademiaDigital`, user `postgres`, password `Admin1234!` |

Con las credenciales que ya tenés: `vn@vn.com` (Admin), `jaz@jaz.com` (Profesor), `vn2@vn.com` (Alumno), todas con password `Qwerty123.`.


docker compose -f docker-compose.finance.yml up --build

docker compose -f docker-compose.dev-tools.yml up --build
