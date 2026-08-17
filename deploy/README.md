# Deployment

This directory contains everything needed to run the E-Commerce API in a containerized environment.

## Docker

From the repository root:

```bash
# Build the image
docker build -t ecommerce-api .

# Run API + SQL Server
docker compose up -d

# View logs
docker compose logs -f api

# Stop
docker compose down

# Stop and remove the database volume
docker compose down -v
```

- API: http://localhost:8080 (health: http://localhost:8080/health, metrics: http://localhost:9090/metrics)
- SQL Server: localhost:1433 (SA password: `Ecommerce!Dev2026` — change for real deployments)

The container runs as a non-root user. Configuration is supplied through environment variables (see `docker-compose.yml`).

## Kubernetes

Requires a cluster with an ingress controller (e.g. nginx-ingress).

```bash
# Create namespace and resources
kubectl apply -f deploy/k8s/namespace.yaml
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/secret.yaml
kubectl apply -f deploy/k8s/sqlserver.yaml
kubectl apply -f deploy/k8s/deployment.yaml
kubectl apply -f deploy/k8s/service.yaml
kubectl apply -f deploy/k8s/ingress.yaml

# Or apply everything at once
kubectl apply -f deploy/k8s/

# Check status
kubectl -n ecommerce get pods
kubectl -n ecommerce get svc
```

`secret.yaml` uses `stringData` with development placeholders. For production, create the Secret imperatively to avoid committing real values:

```bash
kubectl -n ecommerce create secret generic ecommerce-secrets \
  --from-literal=ConnectionStrings__DefaultConnection='Server=...' \
  --from-literal=Jwt__Key='...' \
  --from-literal=Stripe__SecretKey='...' \
  --from-literal=Stripe__WebhookSecret='...'
```

The Ingress routes `ecommerce.local` to the API over TLS. Add an entry to `/etc/hosts` (or use a real DNS name) and provision the `ecommerce-tls` certificate secret.