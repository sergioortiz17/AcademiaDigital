# Importamos los modelos desde las apps antiguas para compatibilidad
# En el futuro, estos se moverán completamente aquí
from api.user.models import User
from api.authentication.models.active_session import ActiveSession

__all__ = ['User', 'ActiveSession']
