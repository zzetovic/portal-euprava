import type { ReactNode } from 'react';

export function AuthLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-gray-50 flex flex-col items-center justify-start pt-12 px-4 sm:pt-20">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-text-primary">Portal eUprava</h1>
        </div>
        <div className="bg-surface rounded-lg shadow-md p-6 sm:p-8">
          {children}
        </div>
      </div>
    </div>
  );
}
