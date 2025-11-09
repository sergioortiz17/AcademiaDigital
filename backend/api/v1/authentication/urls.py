from django.urls import path, include
from rest_framework import routers
from api.v1.authentication.viewsets import (
    LoginViewSet,
    RegisterViewSet,
    LogoutViewSet,
    ActiveSessionViewSet,
)

router = routers.SimpleRouter(trailing_slash=False)

router.register(r"register", RegisterViewSet, basename="register")
router.register(r"login", LoginViewSet, basename="login")
router.register(r"checkSession", ActiveSessionViewSet, basename="check-session")
router.register(r"logout", LogoutViewSet, basename="logout")

urlpatterns = [
    *router.urls,
]

