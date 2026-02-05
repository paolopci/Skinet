export const environment = {
  prodction: false,
  apiUrl: 'https://localhost:5001/api/',
  // Loaded at runtime from window.__env (see env.js in assets)
  stripePublicKey:
    (window as { __env?: { STRIPE_PUBLIC_KEY?: string } }).__env?.STRIPE_PUBLIC_KEY ??
    '',
};
