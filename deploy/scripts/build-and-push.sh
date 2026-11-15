#!/usr/bin/env bash
# Construye las imágenes de backend, finance y frontend para arm64, las sube a ECR con el
# SHA del commit como tag y arma el bundle de Elastic Beanstalk en ./bundle.
#
# Lo ejecuta CodeBuild (deploy/buildspec.yml). Requiere:
#   ECR_REGISTRY  <cuenta>.dkr.ecr.<región>.amazonaws.com
#   ECR_PREFIX    academiadigital
#   IMAGE_TAG     SHA del commit
set -euo pipefail

: "${ECR_REGISTRY:?Falta ECR_REGISTRY}"
: "${ECR_PREFIX:?Falta ECR_PREFIX}"
: "${IMAGE_TAG:?Falta IMAGE_TAG}"

REGION="${AWS_REGION:-${AWS_DEFAULT_REGION:?Falta la región}}"

echo "Registro: $ECR_REGISTRY  Prefijo: $ECR_PREFIX  Tag: $IMAGE_TAG"
aws ecr get-login-password --region "$REGION" |
  docker login --username AWS --password-stdin "$ECR_REGISTRY"

# build_push <nombre> <contexto> <dockerfile>
build_push() {
  local name="$1" context="$2" dockerfile="$3"
  local image="$ECR_REGISTRY/$ECR_PREFIX/$name:$IMAGE_TAG"

  # Los tags de ECR son inmutables: si este commit ya se subió (reintento), no se rehace.
  if aws ecr describe-images --region "$REGION" \
       --repository-name "$ECR_PREFIX/$name" \
       --image-ids imageTag="$IMAGE_TAG" >/dev/null 2>&1; then
    echo "== $name: $image ya existe, se omite"
    return
  fi

  echo "== $name: construyendo $image"
  docker build --platform linux/arm64 -t "$image" -f "$dockerfile" "$context"
  docker push "$image"
}

build_push backend  .                backend-dotnet/Dockerfile
build_push finance  services         services/AcademiaDigital.Finance/Dockerfile
build_push frontend frontend-angular frontend-angular/Dockerfile

# Bundle de Beanstalk: la plantilla con las imágenes de este commit.
rm -rf bundle && mkdir -p bundle
sed -e "s|__ECR_REGISTRY__|$ECR_REGISTRY|g" \
    -e "s|__ECR_PREFIX__|$ECR_PREFIX|g" \
    -e "s|__IMAGE_TAG__|$IMAGE_TAG|g" \
    deploy/eb/docker-compose.yml > bundle/docker-compose.yml

if grep -Eq '__ECR_REGISTRY__|__ECR_PREFIX__|__IMAGE_TAG__' bundle/docker-compose.yml; then
  echo "ERROR: quedaron marcadores sin reemplazar en bundle/docker-compose.yml" >&2
  exit 1
fi

echo "Bundle listo:"
cat bundle/docker-compose.yml
