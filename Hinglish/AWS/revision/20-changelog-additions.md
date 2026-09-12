> **AWS Quick Revision Notes** · [Index](README.md) · Part IV

# Changelog / Additions Summary (guide construction)
Guide 7 passes se assemble hui — yeh sections meta/changelog hain (study material nahi):
- **[new content]:** ECS/EKS/Fargate/Lambda decision table, Beanstalk vs ECS vs Lambda, S3 storage classes/lifecycle, EBS/EFS/S3, VPC ref architecture, RDS Multi-AZ/replica/Aurora, EventBridge, event-driven ref flow, Secrets Manager vs Parameter Store, least privilege/boundaries, CloudWatch vs X-Ray, cost optimization, Well-Architected, DR strategies.
- **[gaps]:** EC2 sizing/CPU credits, S3 lifecycle Terraform, Fargate/ECS/EKS reasoning, Multi-AZ vs Read Replica drill, VPC rapid-fire, Well-Architected rapid recall, **CloudFormation vs Terraform/CDKTF**.
- **[iam-core]:** IAM restructured to full syllabus order (overview/root/users/groups/policy types/password/MFA/access keys/security tools/hands-on/rapid-fire + instance profile/IMDS/PassRole/STS/Identity Center).
- **[services-core]:** filled ~60% missing service surface (EC2 depth, storage, S3, S3 security, LB/ASG, databases/analytics/caching, Kinesis/MQ, networking, Route 53, global edge, containers/Docker/ECS/ECR, observability, all security services, Organizations/billing).
- **[resume-aligned]:** restructured into 4 tiers + priority map, Azure→AWS translation, resume deep-dives, Terraform in practice, SQS/SNS detail, Step Functions, SSM, migration, Lambda versions/aliases, DAX, support plans, free tier, resilience testing.
- **[net-fundamentals]:** CIDR/subnetting ground-up (32-bit view, `/n` math, mask octets, **AWS 5 reserved IPs**, block-size trick, longest prefix match, RFC 1918) + overlapping-CIDR failure walkthrough (peering reject, TGW blackhole, **`local`-route killer**, partial overlap, PrivateLink fix, allocation registry, IPAM).
- **[topology]:** ekmatra pass jo syllabus ke bajaye ek **asli production stack** se driven hai — ek Windows container ECS-on-EC2 par, internal ALB ke peeche, ek aisi VPC mein jo app own nahi karti. **Client VPN** (chaar objects, do-gate authorization-rule-vs-SG distinction, split-tunnel ek NAT-cost decision, immutable client CIDR, hourly-floor pricing) · **inherited network / shared VPC** (teen operating models, RAM *subnet* sharing, cross-account SG referencing, free same-AZ inter-account traffic, tags-as-contract, isolation accepted not achieved) · **internal vs internet-facing ALB** (immutable `scheme`, aur ALB ke ≥2 AZs / `/27` / 8-free-IPs subnet requirements) · **interface endpoint AZ placement** (per AZ ek ENI, zonal DNS, provider-AZ matching, **AZ names per account different, AZ IDs nahi**) · **ECS dynamic host ports** (`32768–65535` SG rule vs unrelated NACL ephemeral trap; `bridge` per-service SG isolation impossible bana deta hai).
- **Contradictions fixed:** duplicate Lambda/DynamoDB blocks merged (copy-paste artifacts, no factual conflict); resource-policy vs AssumeRole cross-account clarified; S3 strong consistency (Dec 2020) corrected; per-partition DynamoDB figures flagged directional not guaranteed.

---

← [Cross-Cutting Reference](19-cross-cutting-reference.md) · [Index](README.md)
