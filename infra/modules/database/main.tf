# RDS PostgreSQL en subnets privadas, accesible solo desde los security groups indicados.
# La instancia aloja las dos bases: la del monolito y la de Finance (Finance crea la suya
# al arrancar, igual que en docker-compose.finance.yml).

# Sin ; ' " / @ ni espacios: rompen la connection string de Npgsql o RDS no los acepta.
resource "random_password" "master" {
  length           = 32
  special          = true
  override_special = "!#%*-_+"
}

resource "aws_db_subnet_group" "this" {
  name       = var.name
  subnet_ids = var.subnet_ids

  tags = { Name = var.name }
}

resource "aws_security_group" "db" {
  name        = "${var.name}-db"
  description = "PostgreSQL: solo desde la app"
  vpc_id      = var.vpc_id

  tags = { Name = "${var.name}-db" }
}

# count y no for_each: los IDs de los security groups se conocen recién al aplicar.
resource "aws_vpc_security_group_ingress_rule" "postgres" {
  count = length(var.allowed_security_group_ids)

  security_group_id            = aws_security_group.db.id
  referenced_security_group_id = var.allowed_security_group_ids[count.index]
  ip_protocol                  = "tcp"
  from_port                    = 5432
  to_port                      = 5432
  description                  = "PostgreSQL desde la app"
}

resource "aws_db_instance" "this" {
  identifier     = var.name
  engine         = "postgres"
  engine_version = var.engine_version
  instance_class = var.instance_class

  allocated_storage     = var.allocated_storage_gb
  max_allocated_storage = var.max_allocated_storage_gb
  storage_type          = "gp3"
  storage_encrypted     = true

  db_name  = var.db_name
  username = var.master_username
  password = random_password.master.result

  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false
  multi_az               = var.multi_az

  backup_retention_period    = var.backup_retention_days
  backup_window              = var.backup_window
  maintenance_window         = var.maintenance_window
  auto_minor_version_upgrade = true
  copy_tags_to_snapshot      = true

  deletion_protection       = var.deletion_protection
  skip_final_snapshot       = false
  final_snapshot_identifier = "${var.name}-final"

  # Los cambios se aplican en la ventana de mantenimiento, no en horario de clases.
  apply_immediately = false

  tags = { Name = var.name }
}
