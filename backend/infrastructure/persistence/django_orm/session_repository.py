from typing import Optional
from datetime import datetime
from domain.entities.session import Session
from domain.entities.user import User
from domain.interfaces.repositories.session_repository import ISessionRepository
# Importamos desde el modelo antiguo para compatibilidad
from api.authentication.models.active_session import ActiveSession as ActiveSessionModel


class DjangoSessionRepository(ISessionRepository):
    """Implementación de ISessionRepository usando Django ORM"""
    
    def find_by_token(self, token: str) -> Optional[Session]:
        """Busca una sesión por token"""
        try:
            django_session = ActiveSessionModel.objects.get(token=token)
            return self._to_domain_entity(django_session)
        except ActiveSessionModel.DoesNotExist:
            return None
    
    def find_by_user(self, user: User) -> Optional[Session]:
        """Busca una sesión por usuario"""
        try:
            django_session = ActiveSessionModel.objects.get(user_id=user.id)
            return self._to_domain_entity(django_session)
        except ActiveSessionModel.DoesNotExist:
            return None
    
    def create(self, user: User, token: str) -> Session:
        """Crea una nueva sesión"""
        # Eliminar sesión existente si existe
        self.delete_by_user(user)
        
        django_session = ActiveSessionModel.objects.create(
            user_id=user.id,
            token=token
        )
        return self._to_domain_entity(django_session)
    
    def delete(self, session: Session) -> None:
        """Elimina una sesión"""
        try:
            django_session = ActiveSessionModel.objects.get(pk=session.id)
            django_session.delete()
        except ActiveSessionModel.DoesNotExist:
            pass
    
    def delete_by_user(self, user: User) -> None:
        """Elimina todas las sesiones de un usuario"""
        ActiveSessionModel.objects.filter(user_id=user.id).delete()
    
    def _to_domain_entity(self, django_session: ActiveSessionModel) -> Session:
        """Convierte un modelo Django a entidad de dominio"""
        from api.user.models import User as UserModel
        from infrastructure.persistence.django_orm.user_repository import DjangoUserRepository
        
        try:
            django_user = UserModel.objects.get(pk=django_session.user_id)
            user_repo = DjangoUserRepository()
            user = user_repo._to_domain_entity(django_user)
        except UserModel.DoesNotExist:
            raise ValueError(f"User {django_session.user_id} not found")
        
        return Session(
            id=django_session.pk,
            user=user,
            token=django_session.token,
            created_at=django_session.date
        )

