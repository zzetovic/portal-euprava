import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { apiClient } from '@/shared/api/client';

const USER_TYPE_MAP: Record<string, AuthUser['userType']> = {
  Citizen: 'citizen',
  JlsOfficer: 'jls_officer',
  JlsAdmin: 'jls_admin',
  citizen: 'citizen',
  jls_officer: 'jls_officer',
  jls_admin: 'jls_admin',
};

function normalizeUser(raw: Record<string, unknown>): AuthUser {
  return {
    ...raw,
    userType: USER_TYPE_MAP[raw.userType as string] ?? 'citizen',
  } as AuthUser;
}

interface AuthUser {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  userType: 'citizen' | 'jls_officer' | 'jls_admin';
  emailVerifiedAt: string | null;
  mustChangePassword: boolean;
  tenantId: string;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<{ mustChangePassword: boolean; userType: string }>;
  register: (data: RegisterData) => Promise<void>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
  setAccessToken: (token: string | null) => void;
}

interface RegisterData {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  oib?: string;
  phone?: string;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const setAccessToken = useCallback((token: string | null) => {
    if (token) {
      sessionStorage.setItem('access_token', token);
    } else {
      sessionStorage.removeItem('access_token');
    }
  }, []);

  const refreshUser = useCallback(async () => {
    try {
      const token = sessionStorage.getItem('access_token');
      if (!token) {
        setUser(null);
        return;
      }
      const { data } = await apiClient.get('/auth/me');
      setUser(normalizeUser(data as Record<string, unknown>));
    } catch {
      setUser(null);
      sessionStorage.removeItem('access_token');
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const { data } = await apiClient.post<{ accessToken: string; user: Record<string, unknown> }>('/auth/login', {
      email,
      password,
    });
    setAccessToken(data.accessToken);
    const user = normalizeUser(data.user);
    setUser(user);
    return { mustChangePassword: user.mustChangePassword, userType: user.userType };
  }, [setAccessToken]);

  const register = useCallback(async (regData: RegisterData) => {
    await apiClient.post('/auth/register', regData);
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiClient.post('/auth/logout');
    } finally {
      setAccessToken(null);
      setUser(null);
    }
  }, [setAccessToken]);

  useEffect(() => {
    const init = async () => {
      const token = sessionStorage.getItem('access_token');
      if (token) {
        await refreshUser();
      }
      setIsLoading(false);
    };
    init();
  }, [refreshUser]);

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: user !== null,
        isLoading,
        login,
        register,
        logout,
        refreshUser,
        setAccessToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
