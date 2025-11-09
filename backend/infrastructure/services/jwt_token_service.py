import jwt
from datetime import datetime, timedelta
from django.conf import settings
from domain.entities.user import User
from domain.interfaces.services.token_service import ITokenService


class JwtTokenService(ITokenService):
    """Implementación de ITokenService usando JWT"""
    
    def __init__(self, secret_key: str = None, expiration_days: int = 7):
        self.secret_key = secret_key or settings.SECRET_KEY
        self.expiration_days = expiration_days
    
    def generate_token(self, user: User) -> str:
        """Genera un token JWT para un usuario"""
        payload = {
            "id": user.id,
            "exp": datetime.utcnow() + timedelta(days=self.expiration_days)
        }
        return jwt.encode(payload, self.secret_key, algorithm="HS256")
    
    def validate_token(self, token: str) -> bool:
        """Valida un token JWT"""
        try:
            jwt.decode(token, self.secret_key, algorithms=["HS256"])
            return True
        except jwt.ExpiredSignatureError:
            return False
        except jwt.InvalidTokenError:
            return False
    
    def decode_token(self, token: str) -> dict:
        """Decodifica un token JWT"""
        return jwt.decode(token, self.secret_key, algorithms=["HS256"])

