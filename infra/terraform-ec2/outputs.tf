output "instance_public_ip" {
  description = "IP public EC2"
  value       = aws_instance.ec2_basic.public_ip
}
