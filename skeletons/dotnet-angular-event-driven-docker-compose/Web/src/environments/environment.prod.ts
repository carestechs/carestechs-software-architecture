// Production: same-origin — nginx serves the bundle and proxies /v1 to the API
// container (adrs/deployment/nginx-spa-proxy.md). No deploy-time injection needed.
export const environment = {
  apiBaseUrl: '',
};
