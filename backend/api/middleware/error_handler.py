from rest_framework.views import exception_handler
from rest_framework.response import Response
from rest_framework import status
from domain.exceptions.authentication_exceptions import (
    AuthenticationException,
    InvalidCredentialsException,
    InactiveUserException,
    SessionNotFoundException,
    EmailAlreadyExistsException,
)


def custom_exception_handler(exc, context):
    """
    Manejo personalizado de excepciones para la API
    """
    # Llamar al handler por defecto de DRF
    response = exception_handler(exc, context)
    
    # Si es una excepción de dominio, formatear la respuesta
    if isinstance(exc, AuthenticationException):
        status_code = status.HTTP_400_BAD_REQUEST
        
        if isinstance(exc, InvalidCredentialsException):
            status_code = status.HTTP_401_UNAUTHORIZED
        elif isinstance(exc, InactiveUserException):
            status_code = status.HTTP_403_FORBIDDEN
        elif isinstance(exc, SessionNotFoundException):
            status_code = status.HTTP_404_NOT_FOUND
        elif isinstance(exc, EmailAlreadyExistsException):
            status_code = status.HTTP_409_CONFLICT
        
        custom_response_data = {
            "success": False,
            "msg": str(exc)
        }
        return Response(custom_response_data, status=status_code)
    
    # Si hay respuesta de DRF, formatearla
    if response is not None:
        custom_response_data = {
            "success": False,
            "msg": str(exc) if hasattr(exc, 'detail') and exc.detail else "An error occurred"
        }
        response.data = custom_response_data
    
    return response

