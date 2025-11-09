from rest_framework import serializers
from application.use_cases.authentication.login_use_case import LoginUseCase
from application.use_cases.authentication.register_use_case import RegisterUseCase


class LoginSerializer(serializers.Serializer):
    """Serializer para login - solo validación de datos"""
    email = serializers.CharField(max_length=255, required=True)
    password = serializers.CharField(max_length=128, write_only=True, required=True)
    
    def validate_email(self, value):
        """Valida que el email no esté vacío"""
        if not value:
            raise serializers.ValidationError("Email is required to login")
        return value
    
    def validate_password(self, value):
        """Valida que la contraseña no esté vacía"""
        if not value:
            raise serializers.ValidationError("Password is required to log in.")
        return value


class RegisterSerializer(serializers.Serializer):
    """Serializer para registro - solo validación de datos"""
    email = serializers.EmailField(required=True)
    username = serializers.CharField(max_length=255, required=True)
    password = serializers.CharField(min_length=4, max_length=128, write_only=True, required=True)
    
    def validate_email(self, value):
        """Valida formato de email"""
        if not value:
            raise serializers.ValidationError("Email is required")
        return value
    
    def validate_username(self, value):
        """Valida que el username no esté vacío"""
        if not value:
            raise serializers.ValidationError("Username is required")
        return value

