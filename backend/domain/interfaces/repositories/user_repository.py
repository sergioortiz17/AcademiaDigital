from abc import ABC, abstractmethod
from typing import Optional
from domain.entities.user import User


class IUserRepository(ABC):
    """Interfaz para el repositorio de usuarios"""
    
    @abstractmethod
    def find_by_id(self, user_id: int) -> Optional[User]:
        """Busca un usuario por ID"""
        pass
    
    @abstractmethod
    def find_by_email(self, email: str) -> Optional[User]:
        """Busca un usuario por email"""
        pass
    
    @abstractmethod
    def authenticate(self, email: str, password: str) -> Optional[User]:
        """Autentica un usuario con email y contraseña"""
        pass
    
    @abstractmethod
    def create(self, user_data: dict) -> User:
        """Crea un nuevo usuario"""
        pass
    
    @abstractmethod
    def update(self, user: User) -> User:
        """Actualiza un usuario existente"""
        pass

