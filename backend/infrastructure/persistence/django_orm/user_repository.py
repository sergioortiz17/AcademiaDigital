from typing import Optional
from django.contrib.auth import authenticate as django_authenticate
from domain.entities.user import User
from domain.interfaces.repositories.user_repository import IUserRepository
# Importamos desde el modelo antiguo para compatibilidad
from api.user.models import User as UserModel


class DjangoUserRepository(IUserRepository):
    """Implementación de IUserRepository usando Django ORM"""
    
    def find_by_id(self, user_id: int) -> Optional[User]:
        """Busca un usuario por ID"""
        try:
            django_user = UserModel.objects.get(pk=user_id)
            return self._to_domain_entity(django_user)
        except UserModel.DoesNotExist:
            return None
    
    def find_by_email(self, email: str) -> Optional[User]:
        """Busca un usuario por email"""
        try:
            django_user = UserModel.objects.get(email=email)
            return self._to_domain_entity(django_user)
        except UserModel.DoesNotExist:
            return None
    
    def authenticate(self, email: str, password: str) -> Optional[User]:
        """Autentica un usuario con email y contraseña"""
        django_user = django_authenticate(username=email, password=password)
        if django_user:
            return self._to_domain_entity(django_user)
        return None
    
    def create(self, user_data: dict) -> User:
        """Crea un nuevo usuario"""
        django_user = UserModel.objects.create_user(
            username=user_data["username"],
            email=user_data["email"],
            password=user_data["password"]
        )
        return self._to_domain_entity(django_user)
    
    def update(self, user: User) -> User:
        """Actualiza un usuario existente"""
        django_user = UserModel.objects.get(pk=user.id)
        django_user.username = user.username
        django_user.email = user.email
        django_user.is_active = user.is_active
        django_user.save()
        return self._to_domain_entity(django_user)
    
    def _to_domain_entity(self, django_user: UserModel) -> User:
        """Convierte un modelo Django a entidad de dominio"""
        return User(
            id=django_user.pk,
            email=django_user.email,
            username=django_user.username,
            is_active=django_user.is_active,
            date_joined=django_user.date
        )

