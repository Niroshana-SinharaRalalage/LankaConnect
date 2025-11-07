/**
 * Auth API Types
 * DTOs matching backend API contracts for authentication
 */

// Request DTOs
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

// Response DTOs
export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface UserDto {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  isEmailVerified: boolean;
  createdAt: string;
}

export interface LoginResponse {
  user: UserDto;
  tokens: AuthTokens;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  message: string;
}

// Enums
export enum UserRole {
  User = 'User',
  Admin = 'Admin',
  Moderator = 'Moderator',
}

// Error Response
export interface ApiErrorResponse {
  error: string;
  errors?: Record<string, string[]>; // Validation errors
}
