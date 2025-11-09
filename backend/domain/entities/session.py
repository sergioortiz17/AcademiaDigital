from dataclasses import dataclass
from datetime import datetime
from domain.entities.user import User


@dataclass
class Session:
    """Entidad de dominio Session"""
    id: int
    user: User
    token: str
    created_at: datetime
    
    def is_valid(self) -> bool:
        """Valida que la sesión sea válida"""
        return bool(self.token) and self.user.is_active
    
    def is_expired(self, expiration_days: int = 7) -> bool:
        """Verifica si la sesión ha expirado"""
        from datetime import timedelta
        expiration_date = self.created_at + timedelta(days=expiration_days)
        return datetime.utcnow() > expiration_date

