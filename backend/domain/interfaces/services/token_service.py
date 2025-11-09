from abc import ABC, abstractmethod
from domain.entities.user import User


class ITokenService(ABC):
    """Interfaz para el servicio de tokens"""
    
    @abstractmethod
    def generate_token(self, user: User) -> str:
        """Genera un token JWT para un usuario"""
        pass
    
    @abstractmethod
    def validate_token(self, token: str) -> bool:
        """Valida un token JWT"""
        pass
    
    @abstractmethod
    def decode_token(self, token: str) -> dict:
        """Decodifica un token JWT"""
        pass

