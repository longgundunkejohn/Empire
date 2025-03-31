#!/bin/bash

cd /root/EmpireRepo || exit

# Renew certs if needed
docker-compose run --rm certbot renew

# Reload nginx to apply renewed certs
docker-compose exec nginx nginx -s reload

