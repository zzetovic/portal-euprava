import { createContext, useContext, useState, type ReactNode } from 'react';

interface AuthState {
  isAuthenticated: boolean;
  accessToken: string | null;
}

interface AuthContextType extends AuthState {
  setAccessToken: (token: string | null) => void;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [accessToken, setAccessToken] = useState<string | null>(null);

  const value: AuthContextType = {
    isAuthenticated: accessToken !== null,
    accessToken,
    setAccessToken,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
