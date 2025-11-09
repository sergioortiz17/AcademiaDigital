from typing import Dict
from datetime import datetime
from domain.entities.user import User
from domain.entities.session import Session
from domain.interfaces.repositories.user_repository import IUserRepository
from domain.interfaces.repositories.session_repository import ISessionRepository
from domain.interfaces.services.token_service import ITokenService
from domain.exceptions.authentication_exceptions import (
    InvalidCredentialsException,
    InactiveUserException
)


class LoginUseCase:
    """Caso de uso para login de usuarios"""
    
    def __init__(
        self,
        user_repository: IUserRepository,
        session_repository: ISessionRepository,
        token_service: ITokenService
    ):
        self.user_repository = user_repository
        self.session_repository = session_repository
        self.token_service = token_service
    
    def execute(self, email: str, password: str) -> Dict:
        """
        Ejecuta el caso de uso de login
        
        Args:
            email: Email del usuario
            password: Contraseña del usuario
        
        Returns:
            Dict con success, token y user
        
        Raises:
            InvalidCredentialsException: Si las credenciales son inválidas
            InactiveUserException: Si el usuario está inactivo
        """
        # 1. Autenticar usuario
        user = self.user_repository.authenticate(email, password)
        if not user:
            raise InvalidCredentialsException("Invalid email or password")
        
        # 2. Verificar que el usuario esté activo
        if not user.is_active:
            raise InactiveUserException("User account is not active")
        
        # 3. Buscar o crear sesión
        session = self.session_repository.find_by_user(user)
        
        if session and session.is_valid() and not session.is_expired():
            # Reutilizar sesión existente
            token = session.token
        else:
            # Crear nueva sesión
            token = self.token_service.generate_token(user)
            session = self.session_repository.create(user, token)
        
        # 4. Retornar resultado
        return {
            "success": True,
            "token": token,
            "user": {
                "_id": user.id,
                "username": user.username,
                "email": user.email
            }
        }

