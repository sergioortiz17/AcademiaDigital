from typing import Dict
from domain.entities.user import User
from domain.interfaces.repositories.user_repository import IUserRepository
from domain.exceptions.authentication_exceptions import AuthenticationException


class UnauthorizedUserUpdateException(AuthenticationException):
    """Excepción cuando el usuario no está autorizado para actualizar"""
    def __init__(self, message: str = "Error updating user"):
        self.message = message
        super().__init__(self.message)


class UpdateUserUseCase:
    """Caso de uso para actualizar usuarios"""
    
    def __init__(self, user_repository: IUserRepository):
        self.user_repository = user_repository
    
    def execute(self, user_id: int, current_user: User, user_data: dict, is_superuser: bool = False) -> Dict:
        """
        Ejecuta el caso de uso de actualización de usuario
        
        Args:
            user_id: ID del usuario a actualizar
            current_user: Usuario actual (quien hace la petición)
            user_data: Datos a actualizar
            is_superuser: Si el usuario actual es superuser
        
        Returns:
            Dict con los datos actualizados del usuario
        
        Raises:
            UnauthorizedUserUpdateException: Si el usuario no está autorizado
        """
        # 1. Verificar permisos
        if current_user.id != user_id and not is_superuser:
            raise UnauthorizedUserUpdateException("Error updating user")
        
        # 2. Buscar usuario
        user = self.user_repository.find_by_id(user_id)
        if not user:
            raise UnauthorizedUserUpdateException("Error updating user")
        
        # 3. Actualizar datos
        if "email" in user_data:
            user.email = user_data["email"]
        if "username" in user_data:
            user.username = user_data["username"]
        
        # 4. Guardar cambios
        updated_user = self.user_repository.update(user)
        
        # 5. Retornar resultado
        return {
            "id": updated_user.id,
            "username": updated_user.username,
            "email": updated_user.email,
            "date": updated_user.date_joined
        }

