from dataclasses import dataclass
from typing import Optional
from datetime import datetime


@dataclass
class User:
    """Entidad de dominio User"""
    id: int
    email: str
    username: str
    is_active: bool
    date_joined: Optional[datetime] = None
    
    def __post_init__(self):
        if not self.email:
            raise ValueError("Email is required")
        if not self.username:
            raise ValueError("Username is required")
    
    def is_valid(self) -> bool:
        """Valida que el usuario sea válido"""
        return self.is_active and bool(self.email)

