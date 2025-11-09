from rest_framework import routers
from api.v1.user.viewsets import UserViewSet

router = routers.SimpleRouter(trailing_slash=False)

router.register(r"edit", UserViewSet, basename="user-edit")

urlpatterns = [
    *router.urls,
]

