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
  selectedRole?: UserRole; // Phase 6A.0: Optional role selection (defaults to GeneralUser)
  preferredMetroAreaIds?: string[]; // Required: Minimum 1, Maximum 20 metro areas
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
  fullName: string; // Backend returns fullName, not firstName/lastName separately
  role: UserRole;
  pendingUpgradeRole?: UserRole | null; // Phase 6A.7: Pending role upgrade (returned from login)
  upgradeRequestedAt?: string | null; // Phase 6A.7: When upgrade was requested (returned from login)
  isEmailVerified?: boolean;
  createdAt?: string;
  profilePhotoUrl?: string | null; // User's profile photo URL from Azure Blob Storage
}

export interface LoginResponse {
  user: UserDto;
  accessToken: string;
  tokenExpiresAt: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  message: string;
}

// Enums
// Phase 6A.0: Updated role system for Event Organizer + Business Owner model (6 roles)
export enum UserRole {
  GeneralUser = 'GeneralUser',
  BusinessOwner = 'BusinessOwner',
  EventOrganizer = 'EventOrganizer',
  EventOrganizerAndBusinessOwner = 'EventOrganizerAndBusinessOwner',
  Admin = 'Admin',
  AdminManager = 'AdminManager',
}

// Error Response
export interface ApiErrorResponse {
  error: string;
  errors?: Record<string, string[]>; // Validation errors
}
