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
| 10 a 11 | Fase 4: base de datos | ⏸️ En espera | | |
| 12 a 13 | Fase 5: aplicación | ⏸️ En espera | | |
| 14 a 15 | Fase 6: CI | ⏸️ En espera | | |

Pendientes fuera de Terraform:

- [ ] Confirmar el correo de verificación de AWS Budgets en `sergioortiz170992@gmail.com`.
- [ ] Pedir acceso a producción de SES en us-east-2 (hoy: sandbox, 200 correos por día, solo a direcciones verificadas).
- [ ] Confirmar con Gabriel que la producción del ITSC puede vivir en esta cuenta, antes de la fase 4.

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

## Fase 6: CI ⏸️

**Bloqueo conocido:** la política de la organización niega listar proveedores OIDC de IAM. Es probable que el paso 14 falle con `AccessDenied`. Si pasa, no reintentar: hay que pedirle a Gabriel que lo habilite.

### Paso 14. Proveedor OIDC de GitHub (`shared/github-oidc-provider`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/github-oidc-provider && terragrunt plan
```

Esperado: `Plan: 1 to add, 0 to change, 0 to destroy.` con `aws_iam_openid_connect_provider.github` (`token.actions.githubusercontent.com`).

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/shared/github-oidc-provider && terragrunt apply
```

Esperado: `Apply complete! Resources: 1 added, 0 changed, 0 destroyed.`

### Paso 15. Rol de GitHub Actions (`itsc/github-oidc`)

Plan:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/github-oidc && terragrunt plan
```

Esperado:

- `Plan: 3 to add, 0 to change, 0 to destroy.`
- `aws_iam_role.github_actions` con nombre `academiadigital-itsc-prod-github-actions`; la condición de confianza solo admite `repo:sergioortiz17/AcademiaDigital:ref:refs/heads/main`.
- `aws_iam_role_policy_attachment.beanstalk` y `aws_iam_role_policy.github_actions`.

Apply:

```bash
cd /home/sergio/Videos/ITSC/3erAnio/TESIS/AcademiaDigital/infra/live/itsc/github-oidc && terragrunt apply
```

Esperado: `Apply complete! Resources: 3 added, 0 changed, 0 destroyed.`

---

## Si algo sale mal

| Síntoma | Qué hacer |
|---|---|
| `ERROR: la cuenta activa es ... pero esta infraestructura es de la cuenta 740227232235` | La terminal no tiene el entorno correcto. `ad-off`, `source infra/scripts/aws-env.sh`, `ad-whoami` |
| `ExpiredToken` o credenciales vencidas | `aws login --profile academiadigital` |
| `explicit deny in a service control policy` | La organización no permite esa acción o región. No reintentar; anotar la acción y consultarlo con Gabriel |
| El `plan` muestra `to destroy` o `to change` en algo que ya existía | No aplicar. Revisar qué cambió |
| `Error acquiring the state lock` | Otra ejecución está usando el estado. Esperar a que termine; no forzar el desbloqueo sin saber cuál es |
