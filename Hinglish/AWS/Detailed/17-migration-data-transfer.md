> **AWS Detailed Guide** · [Index](README.md) · Part III

# Migration & Data Transfer

---

## 1. Migration Strategy

### The 7 Rs — Migration Strategies

Yeh woh framework hai jo reach karne layak hai jab bhi ek question start hota hai "hamare paas ek on-prem application hai aur usko AWS pe move karna hai." Strategy naam karna *aur* choice justify karna hi poora answer hai.

| Strategy | What it means | When it's right |
|---|---|---|
| **Retire** | Isko band kar do — koi use nahi karta | Hamesha pehle audit karo; ek estate ka surprising share dead weight hota hai |
| **Retain** | Abhi ke liye jahan hai wahin rehne do | Mainframe dependencies, ek pending vendor decision, ya kuch jo waise bhi replace ho raha hai |
| **Relocate** | Hypervisor level pe move karo bina change ke (VMware Cloud on AWS) | Ek large VMware estate ko jaldi data centre se exit karna hai |
| **Rehost** ("lift and shift") | As-is EC2 pe move karo, koi code change nahi | Speed aur ek hard deadline; *karna* sabse cheap, *chalana* sabse expensive. **AWS Application Migration Service (MGN)** use karo |
| **Replatform** ("lift and reshape") | App rakho, components ko managed services se swap karo — self-managed SQL Server → **RDS**, self-hosted queue → **SQS** | ✅ Usual sweet spot: rewrite ke bina real operational savings |
| **Repurchase** | Isko drop karo aur SaaS khareedo | Commodity functions — email, CRM, ticketing |
| **Refactor / Re-architect** | Cloud-native rewrite karo (containers, serverless, managed data stores) | Highest cost aur risk, highest long-term payoff — ek business driver se justify karo (scale, release velocity), kabhi "cloud-native better hai" se nahi |

**Honest senior answer:** "**Pehle rehost karo data centre se bahar nikalne ke liye, phir selectively replatform aur refactor karo ek baar yeh AWS pe real telemetry ke sath chal raha ho.** Migration ke dauran sab kuch refactor karne ki koshish yehi hai ki migrations ek saal slip ho jaate hain. Aur sirf rehosting rarely paisa bachata hai — yeh sirf bill move karta hai — isliye business case mein follow-on replatforming include hona chahiye."

Supporting tooling: **Migration Hub** (tools ke across progress track karne wala single dashboard), **Application Discovery Service** (plan karne se pehle on-prem estate aur uski dependencies ka inventory), aur TCO business case ke liye **Migration Evaluator**.

---

## 2. Database Migration

### Database Migration Service (DMS)

Databases migrate karta hai jabki **source online rehta hai** migration ke dauran.

- **Homogeneous** (SQL Server → RDS SQL Server, PostgreSQL → Aurora PostgreSQL) ek straight data move hai.
- **Heterogeneous** (Oracle/SQL Server → PostgreSQL/Aurora) ko pehle **Schema Conversion Tool (SCT)** chahiye schema, stored procedures, aur views convert karne ke liye — SCT *schema* convert karta hai, DMS *data* move karta hai. Un doono ko straight rakhna hi question hai.
- **Change Data Capture (CDC)** important part hai: DMS ek full load karta hai, phir **ongoing changes ko continuously replicate** karta hai, isliye tum dono databases ko sync mein rakhte ho aur ek short window mein cutover karte ho, ek long outage ke bajaye. CDC ko reverse mein bhi running rakhna tumhe ek rollback path bhi deta hai.
- Tumhare VPC mein ek replication instance pe chalta hai; migration ke alawa analytics ke liye S3/Kinesis/OpenSearch mein **ongoing replication** bhi karta hai.

---

## 3. Bulk & Hybrid Data Transfer

### Snow Family

**Physical devices jo AWS tumhe ship karta hai** offline data transfer ke liye — jab data ko network pe move karne mein bahut waqt lagega uska answer.

| Device | Capacity | Use for |
|---|---|---|
| **Snowcone** | ~8–14 TB | Small, rugged, edge collection; ek drone/vehicle se bhi ship ho sakta hai |
| **Snowball Edge** | ~80 TB usable (Storage Optimized) / compute-optimised variants | Workhorse: TB-to-PB migrations, plus disconnected edge site pe **local compute** (EC2/Lambda) |
| **Snowmobile** | Up to **100 PB** — ek 45-foot shipping container | Exabyte-scale data-centre evacuation |

**Reasoning jo dikhana hai, sirf naam nahi:** transfer time calculate karo. 1 Gbps link pe 100 TB, realistic utilisation pe, **ek hafte se zyada** saturated bandwidth hai jo tumhe production ke liye bhi chahiye — isliye ek Snowball jo days mein aata hai wins. Roughly 10 TB se kam, ya ek fat dedicated link ke sath, network transfer (Direct Connect pe DataSync) simpler hai. Data KMS se encrypted hota hai aur devices tamper-evident hote hain.

### Storage Gateway, DataSync & Transfer Family

**Storage Gateway** — ek hybrid appliance (usually on-prem ek VM) jo local systems ko ek familiar protocol deta hai jabki data actually AWS mein rehta hai:
| Type | Presents | Backed by | Use for |
|---|---|---|---|
| **S3 File Gateway** | **NFS / SMB** file shares | S3 objects | Existing apps aur users ko "files" likhne dena jo S3 mein land hote hain, local cache ke sath |
| **FSx File Gateway** | SMB | FSx for Windows | AWS mein Windows file shares tak low-latency on-prem access |
| **Volume Gateway** | **iSCSI** block volumes (cached or stored mode) | S3 + EBS snapshots | On-prem block storage backup karna; EC2 mein DR restore |
| **Tape Gateway** | Ek **virtual tape library** (VTL) | S3 Glacier | Existing backup software rakhte hue physical tape backup retire karna |

**DataSync** — on-prem (NFS/SMB/HDFS/object) aur AWS (S3, EFS, FSx) ke beech, ya AWS storage services ke beech managed, accelerated **online** transfer aur ongoing sync. Yeh parallelism, integrity validation, incremental sync, aur scheduling handle karta hai — repeated bulk transfer ke liye right tool, jahan Storage Gateway *continuous hybrid access* ke liye hai.

**Transfer Family** — S3 ya EFS ke saamne fully managed **SFTP/FTPS/FTP** endpoints. Yeh answer hai jab koi partner ya legacy system sirf SFTP bol sakta hai aur tum ek SFTP server run aur patch nahi karna chahte.

**Distinction jo state karna hai:** "**Storage Gateway** ek permanent hybrid foothold rakhta hai — on-prem systems NFS/SMB/iSCSI/tape use karte rehte hain jabki data AWS mein rehta hai. **DataSync** network ke over data move ya sync karne ke liye hai. **Snow Family** data ko physically move karne ke liye hai jab network kaam na kare. **Transfer Family** S3 ko legacy file-transfer protocols pe expose karne ke liye hai."

---

← [Cost & Performance](16-cost-performance.md) · [Index](README.md) · [Well-Architected & Resilience](18-well-architected-resilience.md) →
