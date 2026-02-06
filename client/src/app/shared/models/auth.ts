export type AuthUser = {
  email: string;
  firstName: string;
  lastName: string;
  token: string;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type AddressRequest = {
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string | null;
  city: string;
  postalCode: string;
  countryCode: string;
  region?: string | null;
};

export type RegisterRequest = {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
  address: AddressRequest;
};

export type RefreshRequest = {
  refreshToken: string;
};
