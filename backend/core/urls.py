from django.urls import path, include

urlpatterns = [
    # New Clean Architecture API v1 (prioridad)
    path("api/v1/users/", include(("api.v1.authentication.urls", "api-v1-auth"), namespace="api-v1-auth")),
    path("api/v1/users/", include(("api.v1.user.urls", "api-v1-user"), namespace="api-v1-user")),
    # Legacy API (mantener por compatibilidad - será deprecado)
    path("api/users/", include(("api.routers", "api"), namespace="api")),
]
