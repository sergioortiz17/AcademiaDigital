#!/bin/bash
set -ex  # Modo Debug 

exec > >(tee /var/log/user_data.log | logger -t user-data ) 2>&1

echo "Actualizando paquetes..."
sudo yum update -y

echo "Instalando Git..."
sudo yum install -y git
git --version

echo "Clonando repositorio..."
sudo -u ec2-user git clone https://github.com/sergioortiz17/AcademiaDigital.git /home/ec2-user/AcademiaDigital
ls -lah /home/ec2-user/AcademiaDigital

echo "Instalando Docker..."
sudo yum install -y docker
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker ec2-user

echo "Instalando Docker Compose..."
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose


#  IMPORTANTE: instalar buildx y compose manualmente
echo "=== Instalando Docker Buildx y Compose (versiones estables) ==="
sudo dnf remove -y docker-buildx-plugin docker-compose-plugin || true
sudo mkdir -p /usr/libexec/docker/cli-plugins

# Buildx
sudo curl -SL https://github.com/docker/buildx/releases/download/v0.17.1/buildx-v0.17.1.linux-amd64 \
  -o /usr/libexec/docker/cli-plugins/docker-buildx

# Compose
sudo curl -SL https://github.com/docker/compose/releases/download/v2.27.0/docker-compose-linux-x86_64 \
  -o /usr/libexec/docker/cli-plugins/docker-compose

sudo chmod +x /usr/libexec/docker/cli-plugins/docker-*

sudo systemctl restart docker

docker buildx version

sudo usermod -aG docker ec2-user


# === Obtener IP pública ===
TOKEN=$(curl -X PUT "http://169.254.169.254/latest/api/token" \
  -H "X-aws-ec2-metadata-token-ttl-seconds: 21600" -s)
INSTANCE_IP=$(curl -s -H "X-aws-ec2-metadata-token: $TOKEN" \
  http://169.254.169.254/latest/meta-data/public-ipv4)
echo "📡 IP pública detectada: $INSTANCE_IP"


echo "Creando archivo .env en el frontend con la IP de la instancia..."
cat <<EOF > /home/ec2-user/AcademiaDigital/frontend/.env
REACT_APP_API_URL=http://$INSTANCE_IP:8000/api/
REACT_APP_API_SERVER=http://$INSTANCE_IP:8000/api/
EOF

echo "Cambiando permisos del archivo .env..."
sudo chown ec2-user:ec2-user /home/ec2-user/AcademiaDigital/frontend/.env

echo "Creando archivo .env en el backend con la IP de la instancia..."
cat <<EOF > /home/ec2-user/AcademiaDigital/backend/.env
FRONTEND_URL="http://$INSTANCE_IP:3000"
FRONTEND_IP="$INSTANCE_IP"
EOF

echo "Cambiando permisos del archivo .env..."
sudo chown ec2-user:ec2-user /home/ec2-user/AcademiaDigital/backend/.env

echo "Exportar automáticamente todas las variables del .env en cada inicio de sesión"
echo "Cargando variables del .env globalmente..."
echo "set -o allexport; source /home/ec2-user/AcademiaDigital/backend/.env; set +o allexport" | sudo tee -a /etc/profile.d/custom_env.sh

echo "Dar permisos de ejecución"
sudo chmod +x /etc/profile.d/custom_env.sh

echo "Aplicar las variables inmediatamente sin reiniciar la sesión"
set -o allexport
source /home/ec2-user/AcademiaDigital/backend/.env
set +o allexport

echo "Finalizado el script de configuración de user_data"
