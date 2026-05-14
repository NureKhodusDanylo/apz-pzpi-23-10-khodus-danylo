---
title: Rob
emoji: 😻
colorFrom: blue
colorTo: red
sdk: docker
pinned: false
license: mit
---

Check out the configuration reference at https://huggingface.co/docs/hub/spaces-config-reference

## Kubernetes Deployment

The application manifests for Kubernetes are located in the `k8s/` directory.

### Prerequisites
1. A running Kubernetes cluster.
2. `kubectl` configured to access the cluster.

### Deployment Steps
1. **Namespace**:
   ```bash
   kubectl apply -f k8s/namespace.yaml
   ```
2. **Secrets**:
   Update `k8s/secrets.yaml` with your actual keys and apply:
   ```bash
   kubectl apply -f k8s/secrets.yaml
   ```
3. **Configuration & Persistence**:
   ```bash
   kubectl apply -f k8s/configmap.yaml
   ```
   ```bash
   kubectl apply -f k8s/pvc.yaml
   ```
4. **App & Service**:
   Update the `image` field in `k8s/deployment.yaml` and apply:
   ```bash
   kubectl apply -f k8s/deployment.yaml
   ```
   ```bash
   kubectl apply -f k8s/service.yaml
   ```
5. **Ingress** (Optional):
   Update `k8s/ingress.yaml` with your domain and apply:
   ```bash
   kubectl apply -f k8s/ingress.yaml
   ```
