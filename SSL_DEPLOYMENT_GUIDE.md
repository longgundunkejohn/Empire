# Empire TCG SSL Deployment Guide

## Current Issue
The site is down because SSL certificates are not properly configured. The nginx configuration expects SSL certificates that don't exist yet.

## What Was Fixed
1. **nginx.conf** - Updated with correct domain names (empirecardgame.com and www.empirecardgame.com)
2. **docker-compose.yml** - Updated with correct email (oskarmelorm@gmail.com) and both domains
3. **SSL Certificate Setup** - Created automated scripts for certificate generation

## Deployment Steps

### Option 1: Improved Automated SSL Setup (Recommended)
Run the improved SSL setup script with better error handling:
```bash
chmod +x fix-ssl-deployment-v2.sh
./fix-ssl-deployment-v2.sh
```

This script will:
1. Check DNS configuration for both domains
2. Test challenge directory access (optional)
3. Start containers with HTTP-only access
4. Generate SSL certificates using Let's Encrypt
5. Switch to HTTPS configuration
6. Test both HTTP and HTTPS access for both domains
7. Provide detailed error diagnostics if anything fails

### Option 2: Original Automated SSL Setup
If you prefer the simpler version:
```bash
chmod +x fix-ssl-deployment.sh
./fix-ssl-deployment.sh
```

### Option 2: Manual SSL Setup
If you prefer manual control:

1. **Stop existing containers:**
   ```bash
   docker-compose down
   ```

2. **Create SSL directory:**
   ```bash
   mkdir -p nginx/ssl
   ```

3. **Temporarily use HTTP-only config:**
   ```bash
   cp nginx/nginx-temp.conf nginx/nginx-current.conf
   sed -i.bak 's|nginx.conf|nginx-current.conf|g' docker-compose.yml
   ```

4. **Start containers with HTTP only:**
   ```bash
   docker-compose up -d empire-app nginx
   ```

5. **Generate SSL certificates:**
   ```bash
   docker-compose run --rm certbot
   ```

6. **Switch to full HTTPS config:**
   ```bash
   cp nginx/nginx.conf nginx/nginx-current.conf
   docker-compose restart nginx
   ```

7. **Restore original docker-compose:**
   ```bash
   mv docker-compose.yml.bak docker-compose.yml
   ```

## Verification

After SSL setup, verify the site is working:

- **HTTP (should redirect to HTTPS):** http://empirecardgame.com
- **HTTPS:** https://empirecardgame.com
- **WWW HTTPS:** https://www.empirecardgame.com

## Certificate Renewal

SSL certificates expire every 90 days. You have several options for renewal:

### Option 1: Automated Renewal Script (Recommended)
Use the provided renewal script:
```bash
chmod +x renew-ssl-certificates.sh
./renew-ssl-certificates.sh
```

This script will:
- Check certificate expiration dates
- Only renew if certificates expire within 30 days
- Restart nginx after renewal
- Test HTTPS access after renewal
- Use `--force` flag to renew regardless of expiration date

### Option 2: Manual Renewal
```bash
docker-compose run --rm certbot renew
docker-compose restart nginx
```

### Option 3: Automatic Cron Job
Set up automatic renewal (recommended for production):
```bash
# Edit crontab
crontab -e

# Add this line to check for renewal daily at noon
0 12 * * * cd /root/EmpireRepo && /root/EmpireRepo/renew-ssl-certificates.sh >> /var/log/ssl-renewal.log 2>&1
```

### Test Renewal
To test the renewal process without actually renewing:
```bash
docker-compose run --rm certbot renew --dry-run
```

## Troubleshooting

### DNS Issues
Ensure your domain points to the server:
```bash
dig +short empirecardgame.com
dig +short www.empirecardgame.com
```
Both should return: `138.68.188.47`

### Certificate Generation Fails
Common causes:
- DNS not pointing to server
- Port 80 not accessible from internet
- Domain not properly configured
- Rate limiting from Let's Encrypt

Check logs:
```bash
docker-compose logs certbot
docker-compose logs nginx
```

### Site Still Down After SSL Setup
1. Check container status: `docker-compose ps`
2. Check nginx logs: `docker-compose logs nginx`
3. Check app logs: `docker-compose logs empire-tcg`
4. Verify certificates exist: `ls -la nginx/ssl/live/empirecardgame.com/`

## Configuration Details

### Domains Configured
- Primary: `empirecardgame.com`
- WWW: `www.empirecardgame.com`

### SSL Certificate Email
- `oskarmelorm@gmail.com`

### Server IP
- `138.68.188.47`

### Security Features
- HTTP to HTTPS redirect
- Modern SSL protocols (TLS 1.2, 1.3)
- Security headers
- Rate limiting for API endpoints
- WebSocket support for SignalR

## Next Steps After SSL Setup

1. **Test all functionality:**
   - User registration/login
   - Deck builder
   - Game lobby
   - Actual gameplay

2. **Monitor logs:**
   ```bash
   docker-compose logs -f
   ```

3. **Set up monitoring:**
   - SSL certificate expiration alerts
   - Site uptime monitoring
   - Application error monitoring

## Files Created/Modified

- `nginx/nginx.conf` - Updated with correct domains
- `docker-compose.yml` - Updated with correct email and domains
- `nginx/nginx-temp.conf` - Temporary HTTP-only configuration
- `fix-ssl-deployment.sh` - Automated SSL setup script
- `setup-ssl.sh` - Simple SSL setup script
- `SSL_DEPLOYMENT_GUIDE.md` - This guide

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Review container logs
3. Verify DNS configuration
4. Ensure ports 80 and 443 are open on the server
