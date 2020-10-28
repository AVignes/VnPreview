# VnPreview

## Description

A collection of Azure functions to serve static preview apps.


This is an alternative to [Azure static web apps](https://docs.microsoft.com/en-us/azure/static-web-apps/overview)
which is currently in preview (28.10-20) and has some limitations.
 

![illustration](https://github.com/vendanor/VnPreview/blob/master/illustration.png?raw=true)

- User enters the following url: `https://myapp-278-b2e7537.preview.domain.com/`
- nginx redirects to `https://myapp-278-b2e7537.preview-azure.domain.com/`
- Your Azure function app `my-preview-app` has a wildcard binding to `*.preview-azure.domain.com` 
- Azure function proxy redirect to azure function `GetPreviewContent`. restOfPath (example `/no/find` or `/assets/style.css`) is passed 
  by azure function proxy to function `GetPreviewContent` as query parameter `restOfPath`.
- `GetPreviewContent` proxy requests to correct Azure static storage: `https://mystaticstore.z6.web.core.windows.net/myapp/278/b2e7537`

Static resources in Azure static storage are deployed by Github action build script on PR changes (see [VnPreviewActions](https://github.com/vendanor/VnPreviewActions)).

## Azure setup

Create a Azure function app `my-preview-app`
Setup proxies:

```json
{
  "$schema": "http://json.schemastore.org/proxies",
  "proxies": {
    "rootProxy": {
      "matchCondition": {
        "route": "/",
        "methods": [
          "GET",
          "HEAD"
        ]
      },
      "backendUri": "https://my-preview-app.azurewebsites.net/api/GetPreviewContent?restOfPath=index.html"
    },
    "contentProxy": {
      "matchCondition": {
        "route": "/{*restOfPath}",
        "methods": [
          "GET",
          "HEAD"
        ]
      },
      "backendUri": "https://my-preview-app.azurewebsites.net/api/GetPreviewContent?restOfPath={restOfPath}"
    }
  }
}
```

Add custom domain: `*.preview-azure.domain.com` (no ssl needed, handled by nginx)

Add the followin app settings to azure function app:
PREVIEW_BASE_URL = preview-azure.domain.com
STATIC_BASE_URL = https://mystaticstorageaccount.z6.web.etc.net

## Ngnix reverse proxy setup

DNS:
- Set up Azure static functions with *.preview-azure.domain.com as CNAME
- Set up *.preview.domain.com with nginx server

Configure certbot:

```
sudo certbot certonly   --agree-tos   --email dev@domain.com   --manual   --preferred-challenges=dns   -d *.preview.domain.com   --server https://acme-v02.api.letsencrypt.org/directory
```

```
upstream previewaz {
    server x.preview-azure.domain.com:443 weight=50 fail_timeout=30s;
}

server {
    server_name ~^(?<subdomain>.+)\.preview\.domain\.com$;
    location / {
        proxy_pass https://previewaz;
        proxy_ssl_verify off;
        proxy_set_header Host $subdomain.preview-azure.domain.com;
    }

    listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/preview.domain.com/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/preview.domain.com/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
}
```
