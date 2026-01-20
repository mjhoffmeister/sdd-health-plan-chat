# Variables for Health Plan Chat Infrastructure

variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "centralus"
}

variable "environment" {
  description = "Environment name (e.g., demo, dev, prod)"
  type        = string
  default     = "demo"
}

variable "project_name" {
  description = "Project name used for resource naming"
  type        = string
  default     = "healthplanchat"
}

variable "github_repository" {
  description = "GitHub repository in format 'owner/repo'"
  type        = string
}

variable "github_environment" {
  description = "GitHub Actions environment name for federated credentials"
  type        = string
  default     = "demo"
}

variable "developer_principal_id" {
  description = "Azure AD Object ID of developer for local debugging access (optional)"
  type        = string
  default     = ""
}

# Local values for consistent naming
locals {
  resource_prefix = "${var.project_name}-${var.environment}"
  tags = {
    Project     = var.project_name
    Environment = var.environment
    ManagedBy   = "terraform"
  }
}
