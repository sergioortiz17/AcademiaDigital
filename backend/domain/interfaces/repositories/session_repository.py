from abc import ABC, abstractmethod
from typing import Optional
from domain.entities.session import Session
from domain.entities.user import User


class ISessionRepository(ABC):
    """Interfaz para el repositorio de sesiones"""
    
    @abstractmethod
    def find_by_token(self, token: str) -> Optional[Session]:
        """Busca una sesión por token"""
        pass
    
    @abstractmethod
    def find_by_user(self, user: User) -> Optional[Session]:
        """Busca una sesión por usuario"""
        pass
    
    @abstractmethod
    def create(self, user: User, token: str) -> Session:
        """Crea una nueva sesión"""
        pass
    
    @abstractmethod
    def delete(self, session: Session) -> None:
        """Elimina una sesión"""
        pass
    
    @abstractmethod
    def delete_by_user(self, user: User) -> None:
        """Elimina todas las sesiones de un usuario"""
        pass

