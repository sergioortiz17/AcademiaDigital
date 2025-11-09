from rest_framework import viewsets, mixins, status
from rest_framework.response import Response
from rest_framework.permissions import AllowAny, IsAuthenticated
from rest_framework.decorators import action
from application.use_cases.authentication.login_use_case import LoginUseCase
from application.use_cases.authentication.register_use_case import RegisterUseCase
from application.use_cases.authentication.logout_use_case import LogoutUseCase
from domain.exceptions.authentication_exceptions import (
    InvalidCredentialsException,
    InactiveUserException,
    EmailAlreadyExistsException,
    SessionNotFoundException,
)
from infrastructure.persistence.django_orm.user_repository import DjangoUserRepository
from infrastructure.persistence.django_orm.session_repository import DjangoSessionRepository
from infrastructure.services.jwt_token_service import JwtTokenService
from api.v1.authentication.serializers import LoginSerializer, RegisterSerializer


class LoginViewSet(viewsets.GenericViewSet, mixins.CreateModelMixin):
    """ViewSet para login de usuarios"""
    
    permission_classes = (AllowAny,)
    serializer_class = LoginSerializer
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Dependency Injection
        self.login_use_case = LoginUseCase(
            user_repository=DjangoUserRepository(),
            session_repository=DjangoSessionRepository(),
            token_service=JwtTokenService()
        )
    
    def create(self, request, *args, **kwargs):
        """Endpoint para login"""
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        
        try:
            result = self.login_use_case.execute(
                email=serializer.validated_data['email'],
                password=serializer.validated_data['password']
            )
            return Response(result, status=status.HTTP_200_OK)
        except InvalidCredentialsException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_401_UNAUTHORIZED
            )
        except InactiveUserException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_403_FORBIDDEN
            )


class RegisterViewSet(viewsets.GenericViewSet, mixins.CreateModelMixin):
    """ViewSet para registro de usuarios"""
    
    permission_classes = (AllowAny,)
    serializer_class = RegisterSerializer
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Dependency Injection
        self.register_use_case = RegisterUseCase(
            user_repository=DjangoUserRepository()
        )
    
    def create(self, request, *args, **kwargs):
        """Endpoint para registro"""
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        
        try:
            result = self.register_use_case.execute(
                email=serializer.validated_data['email'],
                username=serializer.validated_data['username'],
                password=serializer.validated_data['password']
            )
            return Response(result, status=status.HTTP_201_CREATED)
        except EmailAlreadyExistsException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_409_CONFLICT
            )


class LogoutViewSet(viewsets.GenericViewSet, mixins.CreateModelMixin):
    """ViewSet para logout de usuarios"""
    
    permission_classes = (IsAuthenticated,)
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Dependency Injection
        self.logout_use_case = LogoutUseCase(
            session_repository=DjangoSessionRepository()
        )
        self.user_repository = DjangoUserRepository()
    
    def create(self, request, *args, **kwargs):
        """Endpoint para logout"""
        try:
            # Convertir Django User a Domain User
            django_user = request.user
            domain_user = self.user_repository.find_by_id(django_user.pk)
            
            if not domain_user:
                return Response(
                    {"success": False, "msg": "User not found"},
                    status=status.HTTP_404_NOT_FOUND
                )
            
            result = self.logout_use_case.execute(domain_user)
            return Response(result, status=status.HTTP_200_OK)
        except SessionNotFoundException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_404_NOT_FOUND
            )


class ActiveSessionViewSet(viewsets.GenericViewSet, mixins.CreateModelMixin):
    """ViewSet para verificar sesión activa"""
    
    permission_classes = (IsAuthenticated,)
    http_method_names = ["post"]
    
    def create(self, request, *args, **kwargs):
        """Endpoint para verificar sesión activa"""
        return Response({"success": True}, status=status.HTTP_200_OK)

