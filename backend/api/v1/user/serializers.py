from rest_framework import serializers


class UserSerializer(serializers.Serializer):
    """Serializer para usuario - solo validación de datos"""
    id = serializers.IntegerField(read_only=True)
    username = serializers.CharField(max_length=255, required=False)
    email = serializers.EmailField(required=False)
    userID = serializers.IntegerField(required=False)
    date = serializers.DateTimeField(read_only=True)

