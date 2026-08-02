// Production: the placeholder survives the bundle and is replaced at deploy
// time before the S3 upload (profile convention: sed + CloudFront invalidation)
export const environment = {
  apiBaseUrl: '__API_BASE_URL__',
};
