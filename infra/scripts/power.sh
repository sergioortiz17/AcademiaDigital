#!/usr/bin/env bash
# Encendido y apagado a demanda de la plataforma del ITSC, fuera del horario automático
# (unidad itsc/schedule). Útil para trabajar de noche o un domingo.
#
#   infra/scripts/power.sh status   estado de la base, la app y el monitoreo
#   infra/scripts/power.sh on       enciende base → app → monitoreo (tarda ~5-10 min)
#   infra/scripts/power.sh off      apaga app → monitoreo → base
#
# El horario sigue activo: lo que enciendas a mano se apaga en el próximo apagado
# programado (lunes a viernes 22:00, sábado 13:00). El domingo no hay apagados: lo que
# enciendas un domingo queda prendido hasta el lunes a las 22:00, salvo que corras "off".
#
# Requiere el entorno aislado (source infra/scripts/aws-env.sh + aws login). Antes de
# tocar nada verifica que la cuenta activa sea la del proyecto.
set -euo pipefail

ACCOUNT_ID="740227232235"
REGION="us-east-2"
NAME="academiadigital-itsc-prod"
INSTANCES="${INSTANCES:-1}" # instancias de Beanstalk al encender (igual que instance_count)

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
"$SCRIPT_DIR/assert-account.sh" "$ACCOUNT_ID" >/dev/null
export AWS_REGION="$REGION"

asg_name() {
  aws autoscaling describe-auto-scaling-groups \
    --query "AutoScalingGroups[?Tags[?Key=='elasticbeanstalk:environment-name' && Value=='$NAME']].AutoScalingGroupName | [0]" \
    --output text
}

monitoring_id() {
  aws ec2 describe-instances \
    --filters "Name=tag:Name,Values=$NAME-monitoring" "Name=instance-state-name,Values=pending,running,stopping,stopped" \
    --query 'Reservations[0].Instances[0].InstanceId' --output text
}

db_status() {
  aws rds describe-db-instances --db-instance-identifier "$NAME" \
    --query 'DBInstances[0].DBInstanceStatus' --output text
}

status() {
  local asg
  asg="$(asg_name)"
  echo "Base de datos : $(db_status)"
  echo "App (ASG)     : $(aws autoscaling describe-auto-scaling-groups --auto-scaling-group-names "$asg" \
    --query 'AutoScalingGroups[0].{deseadas:DesiredCapacity,en_servicio:length(Instances[?LifecycleState==`InService`])}' --output text) (deseadas / en servicio)"
  echo "Monitoreo     : $(aws ec2 describe-instances --instance-ids "$(monitoring_id)" \
    --query 'Reservations[0].Instances[0].State.Name' --output text)"
  echo "Entorno       : $(aws elasticbeanstalk describe-environments --environment-names "$NAME" \
    --query 'Environments[0].[Status,Health]' --output text)"
}

on() {
  # 1. Base primero: la app aplica migraciones al arrancar y necesita la base disponible.
  case "$(db_status)" in
    stopped)
      echo "Encendiendo la base (unos 5 minutos)..."
      aws rds start-db-instance --db-instance-identifier "$NAME" >/dev/null ;;
    available) echo "La base ya está encendida." ;;
    *) echo "La base está en estado $(db_status); se espera a que quede disponible." ;;
  esac
  aws rds wait db-instance-available --db-instance-identifier "$NAME"
  echo "Base disponible."

  # 2. App: el horario la apaga dejando el Auto Scaling en 0.
  echo "Encendiendo la app ($INSTANCES instancia/s)..."
  aws autoscaling update-auto-scaling-group --auto-scaling-group-name "$(asg_name)" \
    --min-size "$INSTANCES" --max-size "$INSTANCES" --desired-capacity "$INSTANCES"

  # 3. Monitoreo.
  echo "Encendiendo el monitoreo..."
  aws ec2 start-instances --instance-ids "$(monitoring_id)" >/dev/null

  echo "Listo. La app tarda unos minutos en quedar sana; revisar con: $0 status"
}

off() {
  echo "Apagando la app..."
  aws autoscaling update-auto-scaling-group --auto-scaling-group-name "$(asg_name)" \
    --min-size 0 --max-size 0 --desired-capacity 0
  echo "Apagando el monitoreo..."
  aws ec2 stop-instances --instance-ids "$(monitoring_id)" >/dev/null
  if [ "$(db_status)" = "available" ]; then
    echo "Apagando la base..."
    aws rds stop-db-instance --db-instance-identifier "$NAME" >/dev/null
  fi
  echo "Listo. Se vuelve a encender sola en el próximo encendido programado (lunes a sábado 06:40)."
}

case "${1:-}" in
  status) status ;;
  on)     on ;;
  off)    off ;;
  *)      echo "Uso: $0 {status|on|off}" >&2; exit 1 ;;
esac
