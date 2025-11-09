from rest_framework import viewsets, mixins, status
from rest_framework.response import Response
from rest_framework.permissions import IsAuthenticated
from application.use_cases.user.update_user_use_case import UpdateUserUseCase
from domain.exceptions.authentication_exceptions import AuthenticationException
from infrastructure.persistence.django_orm.user_repository import DjangoUserRepository
from api.v1.user.serializers import UserSerializer


class UserViewSet(viewsets.GenericViewSet, mixins.CreateModelMixin, mixins.UpdateModelMixin):
    """ViewSet para gestión de usuarios"""
    
    serializer_class = UserSerializer
    permission_classes = (IsAuthenticated,)
    
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        # Dependency Injection
        self.update_use_case = UpdateUserUseCase(
            user_repository=DjangoUserRepository()
        )
        self.user_repository = DjangoUserRepository()
    
    def create(self, request, *args, **kwargs):
        """Endpoint para actualizar usuario (mantiene compatibilidad con API anterior)"""
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        
        user_id = serializer.validated_data.get("userID")
        if not user_id:
            return Response(
                {"success": False, "msg": "Error updating user"},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        try:
            # Convertir Django User a Domain User
            django_user = request.user
            current_user = self.user_repository.find_by_id(django_user.pk)
            
            if not current_user:
                return Response(
                    {"success": False, "msg": "Error updating user"},
                    status=status.HTTP_404_NOT_FOUND
                )
            
            # Actualizar usuario
            user_data = {
                "email": serializer.validated_data.get("email"),
                "username": serializer.validated_data.get("username"),
            }
            user_data = {k: v for k, v in user_data.items() if v is not None}
            
            result = self.update_use_case.execute(
                user_id=user_id,
                current_user=current_user,
                user_data=user_data,
                is_superuser=django_user.is_superuser
            )
            
            return Response({"success": True, **result}, status=status.HTTP_200_OK)
        except AuthenticationException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_403_FORBIDDEN
            )
    
    def update(self, request, *args, **kwargs):
        """Endpoint para actualizar usuario"""
        serializer = self.get_serializer(data=request.data)
        serializer.is_valid(raise_exception=True)
        
        user_id = serializer.validated_data.get("userID") or serializer.validated_data.get("id")
        if not user_id:
            return Response(
                {"success": False, "msg": "Error updating user"},
                status=status.HTTP_400_BAD_REQUEST
            )
        
        try:
            # Convertir Django User a Domain User
            django_user = request.user
            current_user = self.user_repository.find_by_id(django_user.pk)
            
            if not current_user:
                return Response(
                    {"success": False, "msg": "Error updating user"},
                    status=status.HTTP_404_NOT_FOUND
                )
            
            # Actualizar usuario
            user_data = {
                "email": serializer.validated_data.get("email"),
                "username": serializer.validated_data.get("username"),
            }
            user_data = {k: v for k, v in user_data.items() if v is not None}
            
            result = self.update_use_case.execute(
                user_id=user_id,
                current_user=current_user,
                user_data=user_data,
                is_superuser=django_user.is_superuser
            )
            
            return Response(result, status=status.HTTP_200_OK)
        except AuthenticationException as e:
            return Response(
                {"success": False, "msg": str(e)},
                status=status.HTTP_403_FORBIDDEN
            )

