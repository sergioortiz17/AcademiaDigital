# Despliegue de AcademiaDigital para el ITSC

Guía paso a paso para crear la infraestructura en la cuenta **740227232235**, región **us-east-2**.
Cada paso tiene tres partes: el `plan`, lo que tiene que mostrar y el `apply`.

**Regla:** si el `plan` no muestra lo esperado o aparece un `ERROR`, no ejecutar el `apply`.

## Antes de cada sesión

Abrir una terminal y cargar el entorno aislado:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital && source infra/scripts/aws-env.sh
ad-whoami
```

Esperado:

- El indicador a la derecha del prompt dice `[aws: academiadigital · us-east-2]`.
- `ad-whoami` muestra la cuenta `740227232235`.
- Si dice que las credenciales vencieron: `aws login --profile academiadigital`, elegir la sesión **production** y volver a correr `ad-whoami`.

Cada comando de abajo usa la ruta absoluta de su unidad y está unido al `cd` con `&&`: si el `cd` falla, Terragrunt no se ejecuta. Copiar **un bloque por vez**.

Si se interrumpe un comando con Ctrl+C, esperar a que diga `Releasing state lock` antes de seguir.

## Estado

| Paso | Unidad | Estado | Fecha | Resultado |
|---|---|---|---|---|
| 0 | Herramientas | ✅ Hecho | 10/11/2026 | Terraform 1.17.1, Terragrunt 1.2.1 |
| 1 | `shared/budget` | ✅ Hecho | 10/11/2026 | Bucket `academiadigital-tfstate-740227232235` y presupuesto de 100 USD |
| 2 | `itsc/dns-zone` | ✅ Hecho | 10/11/2026 | Zona `itsc.dryvn.org` (`Z00817471O5KPX8B390RA`) |
| 3 | Delegación en Cloudflare | ✅ Hecho | 10/11/2026 | 4 NS `itsc` verificados con `dig` |
| 4 | `shared/registry` | ✅ Hecho | 10/11/2026 | 6 recursos: 3 repositorios ECR y sus reglas de limpieza |
| 5 | `itsc/network` | ✅ Hecho | 10/11/2026 | 13 recursos: VPC `vpc-01b427966047ea349`, 4 subnets en us-east-2a y us-east-2b, sin NAT |
| 6 | `itsc/app-security` | ✅ Hecho | 10/11/2026 | 2 recursos: security group `sg-09bd941eb58fa0eb2`, solo salida |
| 7 | `itsc/storage` | ✅ Hecho | 10/11/2026 | 7 recursos: bucket `academiadigital-itsc-prod-files-740227232235`, privado, cifrado y versionado |
| 8 | `itsc/certificate` | ✅ Hecho | 10/11/2026 | 3 recursos: certificado `ISSUED` para `itsc.dryvn.org`, vence el 27/05/2027 |
| 9 | `itsc/email` | ✅ Hecho | 10/11/2026 | 8 recursos: dominio verificado en SES, DKIM y MAIL FROM en `SUCCESS` |
| 10 | `itsc/database` | ✅ Hecho | 14/11/2026 | 5 recursos: RDS PostgreSQL 16 `db.t4g.micro`, sin Multi-AZ, privada y cifrada (`academiadigital-itsc-prod.cxmuy04cq35c.us-east-2.rds.amazonaws.com`). Backups de 1 día: el plan Free rechazó 7 (`FreeTierRestrictionError`) |
| 11 | `itsc/secrets` | ✅ Hecho | 14/11/2026 | 4 recursos: clave JWT y 3 parámetros `SecureString` en SSM (`/academiadigital-itsc-prod/...`) |
| 11a | `shared/ec2-metadata-defaults` (IMDSv2, hop limit 2) | ✅ Hecho | 14/11/2026 | 1 recurso. `get-instance-metadata-defaults`: `HttpTokens: required`, `HttpPutResponseHopLimit: 2` |
| 11b | `itsc/monitoring` (Grafana, Loki, Prometheus) | ✅ Hecho | 15/11/2026 | 28 recursos: instancia `i-09df2b11d76c8c790` (10.20.0.10), Online en Session Manager |
| 12 | `itsc/app` | ✅ Hecho | 15/11/2026 | 12 recursos: entorno Beanstalk `e-pmmd5tb3ck` (1 instancia `t4g.small`), ALB con HTTPS y registro A `itsc.dryvn.org`. Muestra la app de ejemplo de AWS hasta el paso 18 |
| 13 | `itsc/schedule` | ⏸️ Pendiente | | |
| — | ~~OIDC de GitHub~~ | ❌ Descartado | 14/11/2026 | La SCP de AWS (`DenyIAMRestrictedActions`: `iam:*Provider*`) lo bloquea y no se puede modificar. Se reemplaza por CodePipeline |
| 14 | `shared/github-connection` | ✅ Hecho | 14/11/2026 | 1 recurso: conexión `academiadigital-github` |
| 15 | Confirmar la conexión en la consola | ✅ Hecho | 14/11/2026 | App "AWS Connector for GitHub" instalada solo en `AcademiaDigital` (instalación `182079371`). Estado `AVAILABLE` |
| 16 | `deploy/` en `main` (PR) | ✅ Hecho | 14/11/2026 | PR de `feat/almacenamiento-s3` (v1.0.0 a v1.2.0) mergeado |
| 17 | `itsc/pipeline` | ✅ Hecho | 14/11/2026 | 13 recursos: pipeline `academiadigital-itsc-prod-deploy`, CodeBuild arm64 y bucket de artefactos. Primer build OK en ~3 min: imágenes `05a7a780…98f522` en ECR (backend 96 MB, frontend 29 MB) |
| 18 | Activar el despliegue (`deploy_enabled = true`) | ✅ Hecho | 15/11/2026 | Etapa Deploy activa. Primera ejecución completa (Source, Build, Deploy) en 5 min 19 s: v1.3.0 (`47fb220`) en Beanstalk |
| 19 | `itsc/app`: `Email__Provider = "Ses"` | ✅ Hecho | 15/11/2026 | 1 cambio en el entorno (reinicio de 4 min 27 s). v1.4.0 (`143073f`) desplegada por el pipeline |
| 20 | `itsc/app`: permiso SES sobre `identity/*` | ✅ Hecho | 15/11/2026 | 1 cambio en la política `app-access`, sin reinicio. Correo de recuperación recibido en el Gmail verificado |

Pendientes fuera de Terraform:

- [ ] Confirmar el correo de verificación de AWS Budgets en `sergioortiz170992@gmail.com`.
- [ ] Pedir acceso a producción de SES en us-east-2 (hoy: sandbox, 200 correos por día, solo a direcciones verificadas).
- [ ] Ver en [settings.aws.com](https://settings.aws.com) el crédito restante y la fecha de fin del plan Free. Al terminar, AWS suspende la cuenta ([AWS](https://docs.aws.amazon.com/accounts/latest/reference/bcm-lite-free-tier.html)). Para producción, la universidad tiene que pasarla a pago antes de la fase 4.

---

## Fase 0: herramientas ✅

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra && tfenv install && tgenv install
terraform version && terragrunt --version
```

Esperado: `Terraform v1.17.1` y `terragrunt version v1.2.1`.

## Fase 1: protección ✅

### Paso 1. Presupuesto y bucket del estado (`shared/budget`)

Plan (la primera vez crea el bucket del estado; responder `y` cuando pregunte):

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/budget && terragrunt plan --backend-bootstrap
```

Esperado:

- Pregunta si crea el bucket `academiadigital-tfstate-740227232235` → `y`.
- `Plan: 1 to add, 0 to change, 0 to destroy.`
- Recurso `aws_budgets_budget.monthly` con límite de 100 USD y 3 avisos a `sergioortiz170992@gmail.com` (real 80 %, real 100 %, pronóstico 100 %).

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/budget && terragrunt apply
```

Esperado: `Apply complete! Resources: 1 added, 0 changed, 0 destroyed.`

Después: confirmar el correo de verificación de AWS en `sergioortiz170992@gmail.com`. Sin eso, el presupuesto no envía avisos.

## Fase 2: DNS ✅

### Paso 2. Hosted zone (`itsc/dns-zone`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/dns-zone && terragrunt plan
```

Esperado:

- `Plan: 1 to add, 0 to change, 0 to destroy.`
- Recurso `aws_route53_zone.this` con `name = "itsc.dryvn.org"`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/dns-zone && terragrunt apply
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/dns-zone && terragrunt output name_servers
```

Esperado: `Apply complete! Resources: 1 added` y 4 servidores. Los asignados son:

```text
ns-1209.awsdns-23.org
ns-1706.awsdns-21.co.uk
ns-476.awsdns-59.com
ns-576.awsdns-08.net
```

### Paso 3. Delegación en Cloudflare ✅

En Cloudflare → dominio `dryvn.org` → **DNS → Records**, 4 registros:

| Type | Name | Nameserver | TTL |
|---|---|---|---|
| NS | `itsc` | `ns-1209.awsdns-23.org` | Auto |
| NS | `itsc` | `ns-1706.awsdns-21.co.uk` | Auto |
| NS | `itsc` | `ns-476.awsdns-59.com` | Auto |
| NS | `itsc` | `ns-576.awsdns-08.net` | Auto |

Verificación:

```bash
dig NS itsc.dryvn.org +short
```

Esperado: los 4 servidores `awsdns`.

---

## Fase 3: base barata ✅

Costo: prácticamente cero.

### Paso 4. Repositorios de imágenes (`shared/registry`) ✅

Resultado (10/11/2026):

```text
backend  = 740227232235.dkr.ecr.us-east-2.amazonaws.com/academiadigital/backend
finance  = 740227232235.dkr.ecr.us-east-2.amazonaws.com/academiadigital/finance
frontend = 740227232235.dkr.ecr.us-east-2.amazonaws.com/academiadigital/frontend
```

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/registry && terragrunt plan
```

Esperado:

- `Plan: 6 to add, 0 to change, 0 to destroy.`
- 3 `aws_ecr_repository`: `academiadigital/backend`, `academiadigital/finance`, `academiadigital/frontend`.
- 3 `aws_ecr_lifecycle_policy` (conservan las últimas 15 imágenes).

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/registry && terragrunt apply
```

Esperado: `Apply complete! Resources: 6 added, 0 changed, 0 destroyed.`

### Paso 5. Red (`itsc/network`) ✅

Resultado (10/11/2026):

| Recurso | ID | Rango | Zona |
|---|---|---|---|
| VPC | `vpc-01b427966047ea349` | `10.20.0.0/16` | — |
| Subnet pública | `subnet-02a06a2c48d68d7a5` | `10.20.0.0/24` | us-east-2a |
| Subnet pública | `subnet-0f53e4241fe25a541` | `10.20.1.0/24` | us-east-2b |
| Subnet privada | `subnet-0846983043b79269d` | `10.20.10.0/24` | us-east-2a |
| Subnet privada | `subnet-0a722c22b380d5673` | `10.20.11.0/24` | us-east-2b |
| Internet Gateway | `igw-0ff34689b2ca904a6` | — | — |

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/network && terragrunt plan
```

Esperado:

- `Plan: 13 to add, 0 to change, 0 to destroy.`
- `aws_vpc.this` con `cidr_block = "10.20.0.0/16"`.
- `aws_internet_gateway.this` y `aws_default_security_group.this`.
- 2 `aws_subnet.public` (`10.20.0.0/24` y `10.20.1.0/24`) y 2 `aws_subnet.private` (`10.20.10.0/24` y `10.20.11.0/24`), en dos zonas distintas.
- 2 `aws_route_table` (pública con ruta `0.0.0.0/0` al Internet Gateway; privada sin ruta a internet) y 4 `aws_route_table_association`.
- **No** tiene que aparecer ningún `aws_nat_gateway`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/network && terragrunt apply
```

Esperado: `Apply complete! Resources: 13 added, 0 changed, 0 destroyed.`

### Paso 6. Security group de las instancias (`itsc/app-security`) ✅

Resultado (10/11/2026): `sg-09bd941eb58fa0eb2` (`academiadigital-itsc-prod-instances`) en la VPC `vpc-01b427966047ea349`, con una sola regla de salida (`sgr-059658193641f6d11`) y ninguna de ingreso.

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/app-security && terragrunt plan
```

Esperado:

- `Plan: 2 to add, 0 to change, 0 to destroy.`
- `aws_security_group.instances` con nombre `academiadigital-itsc-prod-instances`.
- `aws_vpc_security_group_egress_rule.all` (salida a internet).
- **No** tiene que aparecer ninguna regla de ingreso.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/app-security && terragrunt apply
```

Esperado: `Apply complete! Resources: 2 added, 0 changed, 0 destroyed.`

### Paso 7. Bucket de archivos (`itsc/storage`) ✅

Resultado (10/11/2026): `arn:aws:s3:::academiadigital-itsc-prod-files-740227232235`, con acceso público bloqueado, `BucketOwnerEnforced`, cifrado `AES256`, versionado, solo TLS y expiración de versiones anteriores a los 30 días. La regla de ciclo de vida tardó 1 minuto en crearse: es normal, el provider espera a que S3 la propague.

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/storage && terragrunt plan
```

Esperado:

- `Plan: 7 to add, 0 to change, 0 to destroy.`
- `aws_s3_bucket.files` con nombre `academiadigital-itsc-prod-files-740227232235`.
- Ownership controls, bloqueo de acceso público, versionado, cifrado, política de solo TLS y ciclo de vida.

Apply (la regla de ciclo de vida puede quedar alrededor de 1 minuto en `Still creating...`):

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/storage && terragrunt apply
```

Esperado: `Apply complete! Resources: 7 added, 0 changed, 0 destroyed.`

### Paso 8. Certificado HTTPS (`itsc/certificate`) ✅

Resultado (10/11/2026):

```text
arn:aws:acm:us-east-2:740227232235:certificate/d335ebee-237c-4daa-885a-26650f45be3a
Estado: ISSUED · Validación: SUCCESS · Vence: 27/05/2027
```

La validación tardó segundos. Mientras el certificado no esté asociado al ALB, ACM lo muestra como `INELIGIBLE` para renovación: es esperable, pasa a renovable automático cuando lo usa el ALB (paso 12).

Requiere la delegación del paso 3 funcionando.

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/certificate && terragrunt plan
```

Esperado:

- `Plan: 3 to add, 0 to change, 0 to destroy.`
- `aws_acm_certificate.this` con `domain_name = "itsc.dryvn.org"` y `validation_method = "DNS"`.
- `aws_route53_record.validation["itsc.dryvn.org"]` (CNAME de validación).
- `aws_acm_certificate_validation.this`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/certificate && terragrunt apply
```

Esperado:

- Queda varios minutos en `aws_acm_certificate_validation.this: Still creating...`. Es normal: ACM está comprobando el registro DNS. Suele tardar entre 1 y 5 minutos.
- `Apply complete! Resources: 3 added, 0 changed, 0 destroyed.`

### Paso 9. Correo (`itsc/email`) ✅

Resultado (10/11/2026):

| Verificación | Estado |
|---|---|
| Identidad `itsc.dryvn.org` | Verificada para envío |
| DKIM | `SUCCESS` |
| MAIL FROM `mail.itsc.dryvn.org` | `SUCCESS` |
| DMARC en DNS público | `v=DMARC1; p=none` |
| Cuenta SES | Sandbox: 200 correos cada 24 h, solo a direcciones verificadas |

ARN: `arn:aws:ses:us-east-2:740227232235:identity/itsc.dryvn.org`

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/email && terragrunt plan
```

Esperado:

- `Plan: 8 to add, 0 to change, 0 to destroy.`
- `aws_sesv2_email_identity.this` con `email_identity = "itsc.dryvn.org"`.
- 3 `aws_route53_record.dkim` (CNAME `..._domainkey.itsc.dryvn.org`).
- `aws_sesv2_email_identity_mail_from_attributes.this` con `mail.itsc.dryvn.org`.
- `aws_route53_record.mail_from_mx` (`10 feedback-smtp.us-east-2.amazonses.com`), `mail_from_spf` y `dmarc`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/email && terragrunt apply
```

Esperado: `Apply complete! Resources: 8 added, 0 changed, 0 destroyed.`

Después: la identidad tarda unos minutos en verificarse. En la consola, **SES → Identities → itsc.dryvn.org** tiene que pasar a **Verified**. La cuenta sigue en el sandbox de SES: solo puede enviar a direcciones verificadas hasta pedir acceso a producción.

---

## Fase 4: base de datos ⏸️

**No avanzar hasta:**

- Confirmar con Gabriel que la producción del ITSC puede vivir en esta cuenta.
- Tener listos los cambios de código del README (S3, migraciones, `/health`).

Costo: ~11 USD/mes con el horario de servicio (~23 USD siempre encendida).

### Paso 10. RDS PostgreSQL (`itsc/database`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/database && terragrunt plan
```

Esperado:

- `Plan: 5 to add, 0 to change, 0 to destroy.`
- `random_password.master`, `aws_db_subnet_group.this`, `aws_security_group.db`, 1 `aws_vpc_security_group_ingress_rule.postgres` (puerto 5432 desde el security group de las instancias).
- `aws_db_instance.this` con `engine = "postgres"`, `instance_class = "db.t4g.micro"`, `multi_az = true`, `publicly_accessible = false`, `storage_encrypted = true`, `deletion_protection = true`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/database && terragrunt apply
```

Esperado: tarda entre 10 y 20 minutos (Multi-AZ). `Apply complete! Resources: 5 added, 0 changed, 0 destroyed.`

### Paso 11. Secretos (`itsc/secrets`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/secrets && terragrunt plan
```

Esperado:

- `Plan: 4 to add, 0 to change, 0 to destroy.`
- `random_password.jwt` y 3 `aws_ssm_parameter` de tipo `SecureString`: `/academiadigital-itsc-prod/backend/db-connection`, `/academiadigital-itsc-prod/finance/db-connection` y `/academiadigital-itsc-prod/backend/jwt-secret`.
- Los valores aparecen como `(sensitive value)`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/secrets && terragrunt apply
```

Esperado: `Apply complete! Resources: 4 added, 0 changed, 0 destroyed.`

---

## Fase 5: aplicación ⏸️

**No avanzar hasta** tener las imágenes en ECR y el bundle de Beanstalk. Sin eso, Beanstalk solo muestra la aplicación de ejemplo de AWS.

Costo: ~40 USD/mes con el horario de servicio (ALB, 2 EC2 e IPs públicas).

### Paso 11a. Metadatos de EC2 (`shared/ec2-metadata-defaults`)

Ajuste de cuenta para us-east-2, sin costo. Toda instancia EC2 **nueva** arranca con IMDSv2 obligatorio y *hop limit* 2. Beanstalk no expone esa opción, y con el valor 1 los contenedores no reciben el token de IMDSv2: el backend no podría usar el rol para S3 (archivos), SES (correo) ni SSM ([AWS](https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/configuring-IMDS-new-instances.html)). No modifica instancias que ya existan.

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/ec2-metadata-defaults && terragrunt plan
```

Esperado: `Plan: 1 to add, 0 to change, 0 to destroy.` con `aws_ec2_instance_metadata_defaults.this`, `http_tokens = "required"` y `http_put_response_hop_limit = 2`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/ec2-metadata-defaults && terragrunt apply
```

Verificación:

```bash
aws ec2 get-instance-metadata-defaults --query 'AccountLevel'
```

Esperado: `HttpTokens: required` y `HttpPutResponseHopLimit: 2`.

### Paso 11b. Monitoreo (`itsc/monitoring`)

Una EC2 `t4g.small` con Grafana, Loki y Prometheus en Docker. Loki guarda los logs en S3 (90 días) y Prometheus las métricas en el disco (30 días). No abre puertos a internet: Loki (3100) y Prometheus (9090) solo aceptan a las instancias de la app, y Grafana escucha en `127.0.0.1` y se abre con Session Manager. Sigue el mismo horario que la plataforma. Suma una instancia del mismo tipo que las de la app, su disco y el almacenamiento de S3.

**Qué se guarda:** solo errores (5xx, excepciones, errores del navegador y de nginx) y la auditoría de accesos (login exitoso, login fallido con IP, logout). Los requests normales, Info y Warning no se escriben (`Observability__MinimumLevel=Error` en la unidad `app`) y Alloy los vuelve a filtrar. Para diagnosticar algo puntual se puede bajar a `Warning` o `Information` en `infra/live/itsc/app/terragrunt.hcl` y aplicar; conviene volver a `Error` después.

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/monitoring && terragrunt plan
```

Esperado: `Plan: 28 to add, 0 to change, 0 to destroy.`

- 6 del bucket `academiadigital-itsc-prod-monitoring-740227232235`: bucket, ownership, bloqueo público, cifrado, política de solo TLS y ciclo de vida.
- 6 `aws_s3_object` en `config/`: `docker-compose.yml`, `loki.yaml`, `prometheus.yml`, data sources, proveedor de dashboards y el dashboard.
- `random_password.grafana_admin` y `aws_ssm_parameter.grafana_admin` (`/academiadigital-itsc-prod/monitoring/grafana-admin-password`).
- 4 de red: security group `academiadigital-itsc-prod-monitoring`, 2 reglas de entrada (3100 y 9090) **con origen en el security group de la app**, ninguna desde `0.0.0.0/0`, y la regla de salida.
- `aws_instance.this` con `private_ip = "10.20.0.10"`, `http_tokens = "required"` y disco cifrado.
- 4 de IAM: rol, `AmazonSSMManagedInstanceCore`, política (S3 del bucket, el parámetro de Grafana y lectura de CloudWatch) y perfil de instancia.
- 5 del horario: rol, política y 3 schedules (`start`, `stop-weekday`, `stop-saturday`).

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/monitoring && terragrunt apply
```

Esperado: `Apply complete! Resources: 28 added, 0 changed, 0 destroyed.` La instancia tarda unos 3 a 5 minutos en instalar Docker y levantar los contenedores.

**Abrir Grafana** (requiere el [Session Manager plugin](https://docs.aws.amazon.com/systems-manager/latest/userguide/session-manager-working-with-install-plugin.html) instalado):

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/monitoring && \
aws ssm start-session --target "$(terragrunt output -raw instance_id)" \
  --document-name AWS-StartPortForwardingSession \
  --parameters 'portNumber=3000,localPortNumber=3000'
```

Con la sesión abierta, entrar a `http://localhost:3000` con el usuario `admin`. La contraseña está en SSM; leerla en la terminal y no pegarla en chats ni tickets:

```bash
aws ssm get-parameter --with-decryption --name /academiadigital-itsc-prod/monitoring/grafana-admin-password --query Parameter.Value --output text
```

El dashboard está en **Dashboards → AcademiaDigital → Accesos, errores e instancias**.

**Cambiar la configuración** (dashboards, Loki, Prometheus): editar `infra/modules/monitoring/files/`, aplicar esta unidad (solo actualiza objetos de S3) y reiniciar el servicio, que vuelve a sincronizar desde S3:

```bash
aws ssm send-command --instance-ids "$(cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/monitoring && terragrunt output -raw instance_id)" \
  --document-name AWS-RunShellScript --parameters 'commands=["systemctl restart monitoring"]'
```

El encendido diario también sincroniza la configuración.

### Paso 12. Elastic Beanstalk (`itsc/app`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/app && terragrunt plan
```

Esperado:

- `Plan: 12 to add, 0 to change, 0 to destroy.`
- IAM: `aws_iam_role.service`, `aws_iam_role.instance`, `aws_iam_instance_profile.instance`, `aws_iam_role_policy.instance_app` y 5 `aws_iam_role_policy_attachment`.
- `aws_elastic_beanstalk_application.this` y `aws_elastic_beanstalk_environment.this` con nombre `academiadigital-itsc-prod`.
- `aws_route53_record.app` (registro A de `itsc.dryvn.org` hacia el entorno).

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/app && terragrunt apply
```

Esperado: el entorno tarda entre 10 y 15 minutos. `Apply complete! Resources: 12 added, 0 changed, 0 destroyed.`

Verificar que las instancias de Beanstalk tomaron el *hop limit* 2 del paso 11a (si dice 1, los archivos y el correo van a fallar):

```bash
aws ec2 describe-instances \
  --filters "Name=tag:elasticbeanstalk:environment-name,Values=academiadigital-itsc-prod" "Name=instance-state-name,Values=running" \
  --query 'Reservations[].Instances[].{Id:InstanceId,Hop:MetadataOptions.HttpPutResponseHopLimit,Tokens:MetadataOptions.HttpTokens}' --output table
```

### Paso 13. Horario de servicio (`itsc/schedule`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/schedule && terragrunt plan
```

Esperado:

- `Plan: 9 to add, 0 to change, 0 to destroy.`
- `aws_scheduler_schedule_group.this`, `aws_iam_role.scheduler`, `aws_iam_role_policy.scheduler`.
- 6 `aws_scheduler_schedule`: `db-start`, `app-start`, `app-stop-weekday`, `db-stop-weekday`, `app-stop-saturday`, `db-stop-saturday`, todos con `schedule_expression_timezone = "America/Argentina/Cordoba"`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/schedule && terragrunt apply
```

Esperado: `Apply complete! Resources: 9 added, 0 changed, 0 destroyed.`

---

## Fase 6: CI/CD con CodePipeline ⏸️

**Por qué no GitHub Actions con OIDC:** la cuenta es un proyecto de *Sign up for AWS (new)*. AWS administra la organización y sus SCP, y `DenyIAMRestrictedActions` niega `iam:*Provider*` sin posibilidad de cambio ([AWS](https://docs.aws.amazon.com/accounts/latest/reference/scps-and-rcps-for-projects.html)). El apply del 14/11/2026 lo confirmó con `explicit deny in a service control policy`.

**Cómo funciona ahora:** AWS lee el repositorio por una conexión de CodeConnections (la app "AWS Connector for GitHub"), así que GitHub no guarda ninguna credencial de AWS.

```
push a main → CodePipeline → CodeBuild arm64 (deploy/buildspec.yml)
            → ECR (backend, finance, frontend con el SHA del commit)
            → Elastic Beanstalk (desde el paso 18)
```

Costo: CodeBuild incluye 100 minutos al mes en `arm1.small` y CodePipeline V2 incluye 100 minutos de acción al mes ([CodeBuild](https://aws.amazon.com/codebuild/pricing/), [CodePipeline](https://aws.amazon.com/codepipeline/pricing/)).

### Paso 14. Conexión con GitHub (`shared/github-connection`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/github-connection && terragrunt plan
```

Esperado: `Plan: 1 to add, 0 to change, 0 to destroy.` con `aws_codeconnections_connection.github`, `name = "academiadigital-github"` y `provider_type = "GitHub"`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/github-connection && terragrunt apply
```

Esperado: `Apply complete! Resources: 1 added` y la salida `status = "PENDING"`. Es normal: falta el paso 15.

### Paso 15. Confirmar la conexión en la consola (manual, una sola vez)

Terraform no puede hacer este paso, porque requiere autorizar en GitHub ([AWS](https://docs.aws.amazon.com/dtconsole/latest/userguide/connections-update.html)).

1. En la consola de AWS (us-east-2): **Developer Tools → Settings → Connections**.
2. Elegir `academiadigital-github` (estado *Pending*) → **Update pending connection**.
3. Se abre GitHub: **Install a new app** → cuenta `sergioortiz17` → **Only select repositories** → `AcademiaDigital` → **Install**.
4. De vuelta en AWS: **Connect**. El estado pasa a **Available**.

Verificación:

```bash
aws codeconnections list-connections --query 'Connections[].{Nombre:ConnectionName,Estado:ConnectionStatus}' --output table
```

Esperado: `academiadigital-github` en `AVAILABLE`.

### Paso 16. Llevar `deploy/` a `main`

CodeBuild lee `deploy/buildspec.yml` de la rama `main`. Si el pipeline se crea antes, la primera ejecución falla con `YAML_FILE_ERROR`. Antes del paso 17, mergear a `main` un PR con:

- `deploy/buildspec.yml`
- `deploy/scripts/build-and-push.sh`
- `deploy/eb/docker-compose.yml`

### Paso 17. Pipeline (`itsc/pipeline`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/pipeline && terragrunt plan
```

Esperado: `Plan: 13 to add, 0 to change, 0 to destroy.`

- 6 del bucket de artefactos `academiadigital-itsc-prod-pipeline-740227232235`: bucket, ownership, bloqueo público, cifrado, política de solo TLS y ciclo de vida de 30 días.
- `aws_cloudwatch_log_group.build` (`/aws/codebuild/academiadigital-itsc-prod-images`).
- `aws_codebuild_project.images` con `type = "ARM_CONTAINER"` y `privileged_mode = true`.
- 2 del rol de CodeBuild (`academiadigital-itsc-prod-codebuild` y su política).
- 2 del rol de CodePipeline (`academiadigital-itsc-prod-codepipeline` y su política).
- `aws_codepipeline.this` (`academiadigital-itsc-prod-deploy`) con 2 etapas, `Source` y `Build`, **sin** `Deploy`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/pipeline && terragrunt apply
```

Esperado: `Apply complete! Resources: 13 added, 0 changed, 0 destroyed.`

Al crearse, el pipeline ejecuta una vez sobre el último commit de `main`. El build tarda unos 15 a 20 minutos la primera vez, porque construye 3 imágenes sin caché. Seguimiento:

```bash
aws codepipeline get-pipeline-state --name academiadigital-itsc-prod-deploy \
  --query 'stageStates[].{Etapa:stageName,Estado:latestExecution.status}' --output table
```

Resultado esperado al terminar: las dos etapas en `Succeeded`, y en ECR una imagen por repositorio con el SHA del commit:

```bash
for r in backend finance frontend; do aws ecr list-images --repository-name academiadigital/$r --query 'imageIds[].imageTag' --output text; done
```

Logs del build: CloudWatch → `/aws/codebuild/academiadigital-itsc-prod-images`.

### Paso 18. Activar el despliegue (después de la fase 5)

Con el entorno de Beanstalk creado (paso 12), cambiar `deploy_enabled = false` a `true` en `/home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/pipeline/terragrunt.hcl`.

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/pipeline && terragrunt plan
```

Esperado: `Plan: 1 to add, 1 to change, 0 to destroy.` Agrega la política de Beanstalk al rol del pipeline y la etapa `Deploy` al pipeline.

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/pipeline && terragrunt apply
```

Desde ahí, cada push a `main` construye y despliega solo. Para reprocesar el último commit sin hacer push:

```bash
aws codepipeline start-pipeline-execution --name academiadigital-itsc-prod-deploy
```

---

## Escalar: pasar de 1 a 2 instancias (o activar Multi-AZ)

Arranca con 1 instancia de Beanstalk y la base sin Multi-AZ, para minimizar el costo. Con 1 instancia, una falla de la instancia deja la plataforma caída unos minutos hasta que Beanstalk la reemplaza (los datos están en RDS y S3, no se pierden); los despliegues no cortan el servicio.

**Más instancias:** cambiar `instance_count` en `/home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/scope.hcl` y aplicar **las dos** unidades que lo usan (si solo se aplica `app`, el encendido diario vuelve al valor anterior):

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/app && terragrunt plan
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/schedule && terragrunt plan
```

Esperado en cada plan: `0 to add, 1 to change, 0 to destroy` (`MinSize`/`MaxSize` en el entorno; las entradas del schedule de encendido). Beanstalk agrega la instancia sin recrear el entorno ni cortar el servicio. Requiere los archivos en S3 (v1.2.0), que ya está activo.

**Multi-AZ en la base:** cambiar `db_multi_az = true` en el mismo archivo y aplicar `itsc/database`. RDS crea la réplica sin recrear la base. El módulo usa `apply_immediately = false`, así que el cambio se aplica en la siguiente ventana de mantenimiento (sábados 13:00 UTC); para hacerlo antes, aplicarlo desde la consola de RDS con "Aplicar inmediatamente".

**Probar si alcanza con 1 instancia:** mirar en Grafana (paso 11b) el panel de CPU y memoria durante el uso real, por ejemplo en un período de inscripciones. Si la CPU queda sostenida por encima del 70 % o la memoria cerca del límite, conviene pasar a 2 instancias o a un tipo más grande (`instance_type`).

## Si algo sale mal

| Síntoma | Qué hacer |
|---|---|
| `ERROR: la cuenta activa es ... pero esta infraestructura es de la cuenta 740227232235` | La terminal no tiene el entorno correcto. `ad-off`, `source infra/scripts/aws-env.sh`, `ad-whoami` |
| `ExpiredToken` o credenciales vencidas | `aws login --profile academiadigital` |
| `explicit deny in a service control policy` | Lo bloquea una SCP administrada por AWS (cuenta de *Sign up for AWS (new)*), que no se puede modificar. No reintentar; buscar la acción en las [políticas publicadas](https://docs.aws.amazon.com/accounts/latest/reference/scps-and-rcps-for-projects.html) |
| El `plan` muestra `to destroy` o `to change` en algo que ya existía | No aplicar. Revisar qué cambió |
| `Error acquiring the state lock` | Otra ejecución está usando el estado. Esperar a que termine; no forzar el desbloqueo sin saber cuál es |
| El pipeline falla en `Source` | La conexión con GitHub no está en `AVAILABLE` (paso 15), o la app de GitHub no tiene acceso al repositorio |
| CodeBuild falla con `YAML_FILE_ERROR` | `deploy/buildspec.yml` no está en `main` (paso 16) |
| CodeBuild no arranca: `Cannot have more than 0 builds in queue` o similar | Cuota de builds concurrentes de la cuenta. Revisarla en **Service Quotas → AWS CodeBuild** y pedir un aumento |
| "¿Olvidaste tu contraseña?" dice "revisa tu correo" pero no llega nada | La pantalla responde igual aunque el envío falle. Ver si hubo envío: `aws cloudwatch get-metric-statistics --namespace AWS/SES --metric-name Send --period 60 --statistics Sum --start-time ... --end-time ...` (el contador `SentLast24Hours` de `get-account` se actualiza con demora). Sin envío: buscar en Loki `{service="backend"} \|= "email_send_failed"`. En sandbox el destinatario tiene que estar verificado (`aws sesv2 list-email-identities`), y el DNI y el email tienen que coincidir exactamente con los del usuario |
| El build falla en `docker push` con `tag invalid` / `already exists` | El tag es inmutable; el script ya lo saltea. Si pasa, revisar que `IMAGE_TAG` llegue con el SHA |
