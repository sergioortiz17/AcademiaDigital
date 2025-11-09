variable "aws_region" {
  description = "Región de AWS"
  type        = string
  default     = "us-east-1"
}

variable "ami_id" {
  description = "ID de la AMI"
  type        = string
  default     = "ami-0157af9aea2eef346" // 0157af9aea2eef346" //04681163a08179f28
}

variable "instance_type" {
  description = "Tipo de instancia EC2"
  type        = string
  default     = "t3.small"
}

variable "instance_name" {
  description = "Nombre de la instancia EC2"
  type        = string
  default     = "sergio-ec2-01"
}

