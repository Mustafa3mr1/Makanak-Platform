import { create } from 'zustand';
import type { AuthResponseDto, UserProfileDto } from '@/shared/api/types';

interface AuthState {
  token: string | null;
  expiresAt: string | null;
  user: UserProfileDto | null;
  isAuthenticated: boolean;
  setAuth: (response: AuthResponseDto) => void;
  logout: () => void;
  updateUser: (updatedProfile: Partial<UserProfileDto>) => void;
}

const STORAGE_KEY = 'makanak_auth';

// Load stored session if still valid
const getInitialState = (): { token: string | null; expiresAt: string | null; user: UserProfileDto | null } => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { token: null, expiresAt: null, user: null };
    const parsed = JSON.parse(raw);
    if (parsed.expiresAt && new Date(parsed.expiresAt).getTime() <= Date.now()) {
      localStorage.removeItem(STORAGE_KEY);
      return { token: null, expiresAt: null, user: null };
    }
    return parsed;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return { token: null, expiresAt: null, user: null };
  }
};

const initialSession = getInitialState();

export const useAuthStore = create<AuthState>((set) => ({
  token: initialSession.token,
  expiresAt: initialSession.expiresAt,
  user: initialSession.user,
  isAuthenticated: !!initialSession.token,

  setAuth: (response: AuthResponseDto) => {
    const session = {
      token: response.token || null,
      expiresAt: response.expiresAt || null,
      user: response.user || null,
    };
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    } catch (e) {
      console.error('Failed to persist auth state', e);
    }
    set({
      token: session.token,
      expiresAt: session.expiresAt,
      user: session.user,
      isAuthenticated: !!session.token,
    });
  },

  logout: () => {
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch (e) {
      console.error('Failed to clear auth storage', e);
    }
    set({
      token: null,
      expiresAt: null,
      user: null,
      isAuthenticated: false,
    });
  },

  updateUser: (updatedProfile: Partial<UserProfileDto>) => {
    set((state) => {
      if (!state.user) return state;
      const newUser = { ...state.user, ...updatedProfile };
      const session = {
        token: state.token,
        expiresAt: state.expiresAt,
        user: newUser,
      };
      try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
      } catch (e) {
        console.error('Failed to update auth storage', e);
      }
      return { user: newUser };
    });
  },
}));
