from domain.entities.user import User
from domain.interfaces.repositories.session_repository import ISessionRepository
from domain.exceptions.authentication_exceptions import SessionNotFoundException


class LogoutUseCase:
    """Caso de uso para logout de usuarios"""
    
    def __init__(self, session_repository: ISessionRepository):
        self.session_repository = session_repository
    
    def execute(self, user: User) -> dict:
        """
        Ejecuta el caso de uso de logout
        
        Args:
            user: Usuario a desloguear
        
        Returns:
            Dict con success y msg
        
        Raises:
            SessionNotFoundException: Si no se encuentra la sesión
        """
        # 1. Buscar sesión del usuario
        session = self.session_repository.find_by_user(user)
        
        if not session:
            raise SessionNotFoundException("Session not found")
        
        # 2. Eliminar sesión
        self.session_repository.delete(session)
        
        # 3. Retornar resultado
        return {
            "success": True,
            "msg": "Token revoked"
        }

