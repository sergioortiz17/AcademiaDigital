import jwt
from rest_framework import authentication, exceptions
from django.conf import settings
from infrastructure.persistence.django_orm.user_repository import DjangoUserRepository
from infrastructure.persistence.django_orm.session_repository import DjangoSessionRepository
from infrastructure.services.jwt_token_service import JwtTokenService
from domain.exceptions.authentication_exceptions import SessionNotFoundException


class ActiveSessionAuthentication(authentication.BaseAuthentication):
    """Backend de autenticación usando sesiones activas con JWT"""

    auth_error_message = {"success": False, "msg": "User is not logged on."}

    def __init__(self):
        self.user_repository = DjangoUserRepository()
        self.session_repository = DjangoSessionRepository()
        self.token_service = JwtTokenService()

    def authenticate(self, request):
        """
        Autentica al usuario usando el token JWT en el header Authorization
        """
        request.user = None

        auth_header = authentication.get_authorization_header(request)

        if not auth_header:
            return None

        token = auth_header.decode("utf-8")

        return self._authenticate_credentials(token)

    def _authenticate_credentials(self, token):
        """
        Autentica las credenciales del token
        """
        # 1. Validar token JWT
        if not self.token_service.validate_token(token):
            raise exceptions.AuthenticationFailed(self.auth_error_message)

        # 2. Buscar sesión en la base de datos
        try:
            session = self.session_repository.find_by_token(token)
            if not session:
                raise exceptions.AuthenticationFailed(self.auth_error_message)
        except SessionNotFoundException:
            raise exceptions.AuthenticationFailed(self.auth_error_message)

        # 3. Verificar que el usuario existe y está activo
        user = session.user
        if not user or not user.is_active:
            msg = {"success": False, "msg": "This user has been deactivated."}
            raise exceptions.AuthenticationFailed(msg)

        # 4. Obtener el usuario de Django para compatibilidad
        from api.user.models import User as UserModel
        try:
            django_user = UserModel.objects.get(pk=user.id)
        except UserModel.DoesNotExist:
            msg = {"success": False, "msg": "No user matching this token was found."}
            raise exceptions.AuthenticationFailed(msg)

        return (django_user, token)

