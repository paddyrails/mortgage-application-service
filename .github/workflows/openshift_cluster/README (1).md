# ROSA Infrastructure Workflows

GitHub Actions workflows for managing Red Hat OpenShift on AWS (ROSA) clusters.

## Workflows

| Workflow | Description |
|----------|-------------|
| `rosa-create-cluster.yml` | Create new ROSA cluster with optional Spot instances |
| `rosa-manage-machinepools.yml` | Create/scale/delete machine pools |
| `rosa-delete-cluster.yml` | Delete ROSA cluster |

## Prerequisites

### 1. AWS Account Setup

```bash
# Ensure you have ROSA enabled
aws configure  # Set up AWS CLI
rosa init      # Initialize ROSA
```

### 2. Get ROSA Token

1. Go to [Red Hat Hybrid Cloud Console](https://console.redhat.com/openshift/token/rosa)
2. Click "Load token"
3. Copy the token

### 3. GitHub Secrets Required

| Secret | Description | How to Get |
|--------|-------------|------------|
| `AWS_ACCESS_KEY_ID` | AWS access key | AWS IAM Console |
| `AWS_SECRET_ACCESS_KEY` | AWS secret key | AWS IAM Console |
| `ROSA_TOKEN` | Red Hat ROSA API token | [Red Hat Console](https://console.redhat.com/openshift/token/rosa) |

## Using Spot Instances

### Why Spot Instances?

| Feature | On-Demand | Spot |
|---------|-----------|------|
| Price | $0.192/hr (m5.xlarge) | ~$0.06-0.08/hr |
| Savings | Baseline | **60-70%** |
| Availability | Guaranteed | Can be interrupted |
| Best For | Production | Test/Dev, Fault-tolerant workloads |

### How Spot Works in ROSA

1. **Default pool cannot use Spot** - ROSA creates an on-demand default pool
2. **Create separate Spot pool** - Add a machine pool with `--use-spot-instances`
3. **Scale down default** - Set default pool to 0 replicas
4. **Your workloads run on Spot** - Kubernetes schedules pods on available nodes

### Spot Instance Commands

```bash
# Create Spot machine pool
rosa create machinepool \
  --cluster=mortgage-test \
  --name=spot-workers \
  --instance-type=m5.xlarge \
  --replicas=2 \
  --use-spot-instances \
  --spot-max-price=0.10  # Optional: max $0.10/hr, or omit for on-demand price cap

# Scale Spot pool
rosa edit machinepool \
  --cluster=mortgage-test \
  --replicas=3 \
  spot-workers

# Enable autoscaling on Spot pool
rosa edit machinepool \
  --cluster=mortgage-test \
  --enable-autoscaling \
  --min-replicas=1 \
  --max-replicas=5 \
  spot-workers

# Scale down default (on-demand) pool
rosa edit machinepool \
  --cluster=mortgage-test \
  --replicas=0 \
  worker
```

## Workflow Usage

### Create Cluster with Spot Instances

1. Go to **Actions** → **Create ROSA Cluster with Spot Instances**
2. Fill in inputs:
   - `cluster_name`: `mortgage-test`
   - `region`: `us-east-1`
   - `instance_type`: `m5.xlarge`
   - `worker_count`: `2`
   - `use_spot`: `true` ✅
   - `spot_max_price`: `on-demand` (or specific price like `0.08`)
3. Click **Run workflow**
4. Wait ~40 minutes for cluster creation

### Manage Machine Pools

Actions available:
- **create-spot-pool**: Add new Spot instance pool
- **scale-spot-pool**: Change replica count
- **enable-autoscaling**: Enable auto-scaling
- **delete-spot-pool**: Remove pool
- **list-pools**: View all pools

### Delete Cluster

1. Go to **Actions** → **Delete ROSA Cluster**
2. Enter cluster name
3. Confirm by typing cluster name again
4. Click **Run workflow**

## Cost Comparison

### Test Cluster Monthly Cost

| Configuration | Control Plane | Workers | Total |
|---------------|---------------|---------|-------|
| On-Demand (2x m5.xlarge) | ~$500 | ~$280 | **~$780** |
| Spot (2x m5.xlarge) | ~$500 | ~$90 | **~$590** |
| **Savings** | - | 68% | **24%** |

*Note: Control plane is always managed/fixed cost*

### Spot Pricing Reference (us-east-1)

| Instance | On-Demand | Spot (avg) | Savings |
|----------|-----------|------------|---------|
| m5.large | $0.096/hr | ~$0.03/hr | 69% |
| m5.xlarge | $0.192/hr | ~$0.06/hr | 69% |
| m5.2xlarge | $0.384/hr | ~$0.12/hr | 69% |

## Best Practices for Spot in Test/Dev

### 1. Use Spot for Non-Critical Workloads
```yaml
# In your deployment, add node selector for spot nodes
spec:
  template:
    spec:
      nodeSelector:
        node-type: spot
```

### 2. Enable Autoscaling
```bash
rosa edit machinepool \
  --cluster=mortgage-test \
  --enable-autoscaling \
  --min-replicas=1 \
  --max-replicas=4 \
  spot-workers
```

### 3. Mix Spot and On-Demand for Resilience
```bash
# Keep 1 on-demand node for critical workloads
rosa edit machinepool --cluster=mortgage-test --replicas=1 worker

# Use Spot for the rest
rosa create machinepool --cluster=mortgage-test --name=spot-workers \
  --replicas=2 --use-spot-instances --instance-type=m5.xlarge
```

### 4. Handle Spot Interruptions

Your applications should be:
- **Stateless** or use persistent storage
- **Replicated** (at least 2 replicas)
- **Quick to start** (fast container startup)

## Troubleshooting

### Spot Capacity Not Available
```bash
# Check available instance types
aws ec2 describe-spot-price-history \
  --instance-types m5.xlarge m5.large m6i.xlarge \
  --product-descriptions "Linux/UNIX" \
  --start-time $(date -u +%Y-%m-%dT%H:%M:%SZ) \
  --region us-east-1

# Try different instance types or availability zones
```

### Cluster Creation Failed
```bash
# View installation logs
rosa logs install --cluster=mortgage-test --watch
```

### Machine Pool Not Scaling
```bash
# Check machine pool status
rosa describe machinepool --cluster=mortgage-test spot-workers

# Check cluster events
oc get events -A --sort-by='.lastTimestamp'
```
