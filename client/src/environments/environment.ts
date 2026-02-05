export const environment = {
  prodction: true,
  apiUrl: 'api/',
  // Loaded at runtime from window.__env (see env.js in assets)
  stripePublicKey:
    (window as { __env?: { STRIPE_PUBLIC_KEY?: string } }).__env?.STRIPE_PUBLIC_KEY ??
    '',
};
