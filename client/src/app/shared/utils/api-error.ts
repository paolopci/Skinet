export type ValidationErrors = Record<string, string[]>;

export interface ValidationProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: ValidationErrors;
}

const isStringArray = (value: unknown): value is string[] =>
  Array.isArray(value) && value.every((item) => typeof item === 'string');

export const extractValidationErrorMap = (payload: unknown): ValidationErrors | null => {
  if (!payload || typeof payload !== 'object') {
    return null;
  }

  const errors = (payload as { errors?: unknown }).errors;
  if (!errors || typeof errors !== 'object') {
    return null;
  }

  const result: ValidationErrors = {};
  for (const [key, value] of Object.entries(errors)) {
    if (isStringArray(value)) {
      result[key] = value;
    }
  }

  return Object.keys(result).length > 0 ? result : null;
};

export const extractValidationErrors = (payload: unknown): string[] => {
  const map = extractValidationErrorMap(payload);
  return map ? Object.values(map).flat() : [];
};
