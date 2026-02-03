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
  street: string;
  city: string;
  state: string;
  postalCode: string;
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
