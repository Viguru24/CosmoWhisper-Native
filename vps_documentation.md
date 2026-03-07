# CosmoWhisper — VPS Infrastructure Documentation

## 1. Server Metadata
| Property | Value |
|----------|-------|
| **Provider** | Hetzner Cloud CMS (Nuremberg, DE) |
| **Instance Type** | CX33 (8GB RAM, 80GB Disk) |
| **Public IP** | `46.224.29.175` |
| **Tailscale IP** | `100.79.71.113` |
| **Domain** | `api.cosmowhisper.com` |
| **OS** | Ubuntu 22.04 LTS |

---

## 2. Server Software Stack
The backend runs on a Node.js stack managed by PM2 and proxied via Nginx.

### **Node.js**
- **Version**: v20.x
- **Path**: `/usr/bin/node`
- **Source**: Installed via NodeSource binary distributions.

### **PM2 (Process Manager)**
- **Purpose**: Keeps the application running 24/7. Automatically restarts on crashes or server reboots.
- **Process List**: `pm2 list` (to see running apps)
- **Logs**: `pm2 logs cosmowhisper-api`
- **Location**: `/root/cosmowhisper-backend`

### **Nginx (Web Server & Reverse Proxy)**
- **Role**: Handles all incoming HTTPS traffic on port 443 and proxies it to internal port 5000.
- **Configuration**: `/etc/nginx/sites-available/api.cosmowhisper.com`
- **Active Site**: Symlinked to `/etc/nginx/sites-enabled/`
- **Status Check**: `systemctl status nginx`
- **Config Check**: `nginx -t`

### **SSL (Let's Encrypt)**
- **Domain**: `api.cosmowhisper.com`
- **Management**: Certbot
- **Auto-Renewal**: Managed by `certbot.timer`.

---

## 3. Firewall & Security (UFW)
The Uncomplicated Firewall (UFW) is active and follows a "deny by default" policy for incoming traffic.

### **Allowed Traffic:**
| Port | Protocol | Service | Description |
|------|----------|---------|-------------|
| 22 | TCP | SSH | Server management |
| 80 | TCP | HTTP | Redirects to HTTPS (Nginx) |
| 443 | TCP | HTTPS | Main API traffic (Nginx) |
| 5000 | TCP | API | Backend internal port (Deny from public internet) |

- **To Check Status**: `ufw status verbose`
- **To Allow a Port**: `ufw allow <port>/tcp`

---

## 4. Google Cloud Authentication
- **Project Name**: `cosmowhisper-app`
- **Role**: Google Cloud acts as the OAuth 2.0 Identity Provider.
- **Redirect URI**: `https://api.cosmowhisper.com/api/auth/google/callback`

---

## 5. Deployment & Maintenance
| Task | Command (run as root) |
|------|---------|
| **View Backend Logs** | `pm2 logs cosmowhisper-api` |
| **Restart Backend** | `pm2 restart cosmowhisper-api` |
| **Stop Backend** | `pm2 stop cosmowhisper-api` |
| **Build from Source** | `cd /root/cosmowhisper-backend && npm run build` |
| **Check API Health** | `curl -I https://api.cosmowhisper.com/` |

---

## 6. Known Configurations & Special Rules
- **Reverse Proxy**: Nginx forwards headers (Host, X-Real-IP, etc.) so the Node.js backend knows who is connecting.
- **Environment Variables**: Stored in `/root/cosmowhisper-backend/.env`. (Contains Google Client ID, Client Secret, JWT Secret, Admin Key, and Neon DB URL).

---

## 7. Security Management

### **Admin Key Rotation**
The `ADMIN_KEY` is used to protect administrative endpoints. It should be rotated every 90 days or if a breach is suspected.
1. Generate a new high-entropy key (e.g., `openssl rand -hex 16`).
2. Update the `ADMIN_KEY` in `/root/cosmowhisper-backend/.env`.
3. Restart the backend: `pm2 restart cosmowhisper-api`.
4. Update the Admin dashboard settings with the new key.

### **Rate Limiting**
Implemented via `express-rate-limit`:
- **Auth Routes**: 20 requests per hour.
- **AI Proxy**: 30 requests per minute.
- **Admin/License**: 500 requests per 15 minutes.

### **Firewall Lockdown**
Ensure only standard ports are exposed:
```bash
# Reset UFW
ufw default deny incoming
ufw default allow outgoing

# Allow required ports
ufw allow 22/tcp   # SSH
ufw allow 80/tcp   # HTTP (Nginx)
ufw allow 443/tcp  # HTTPS (Nginx)

# Enable
ufw enable
```
