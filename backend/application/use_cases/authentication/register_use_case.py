from typing import Dict
from domain.interfaces.repositories.user_repository import IUserRepository
from domain.exceptions.authentication_exceptions import EmailAlreadyExistsException


class RegisterUseCase:
    """Caso de uso para registro de usuarios"""
    
    def __init__(self, user_repository: IUserRepository):
        self.user_repository = user_repository
    
    def execute(self, email: str, username: str, password: str) -> Dict:
        """
        Ejecuta el caso de uso de registro
        
        Args:
            email: Email del usuario
            username: Nombre de usuario
            password: Contraseña del usuario
        
        Returns:
            Dict con success, userID y msg
        
        Raises:
            EmailAlreadyExistsException: Si el email ya existe
        """
        # 1. Verificar que el email no exista
        existing_user = self.user_repository.find_by_email(email)
        if existing_user:
            raise EmailAlreadyExistsException("Email already taken")
        
        # 2. Crear nuevo usuario
        user = self.user_repository.create({
            "email": email,
            "username": username,
            "password": password
        })
        
        # 3. Retornar resultado
        return {
            "success": True,
            "userID": user.id,
            "msg": "The user was successfully registered"
        }

