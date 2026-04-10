import { useEffect, useState, useCallback, createContext, useContext, type ReactNode } from 'react';
import { generateId } from '../utils/uuid';

type ToastType = 'success' | 'error' | 'warning';

interface ToastMessage {
  id: string;
  type: ToastType;
  message: string;
}

interface ToastContextType {
  toast: (type: ToastType, message: string) => void;
}

const ToastContext = createContext<ToastContextType | null>(null);

export function useToast(): ToastContextType {
  const ctx = useContext(ToastContext);
  if (!ctx) throw new Error('useToast must be used within ToastProvider');
  return ctx;
}

const typeClasses: Record<ToastType, string> = {
  success: 'bg-success text-white',
  error: 'bg-error text-white',
  warning: 'bg-warning text-white',
};

const icons: Record<ToastType, string> = {
  success: 'M5 13l4 4L19 7',
  error: 'M6 18L18 6M6 6l12 12',
  warning: 'M12 9v2m0 4h.01M12 3l9.5 16.5H2.5L12 3z',
};

function ToastItem({ item, onDismiss }: { item: ToastMessage; onDismiss: (id: string) => void }) {
  useEffect(() => {
    const timer = setTimeout(() => onDismiss(item.id), 5000);
    return () => clearTimeout(timer);
  }, [item.id, onDismiss]);

  return (
    <div
      className={`flex items-center gap-2 px-4 py-3 rounded-lg shadow-lg text-sm font-medium animate-slide-in ${typeClasses[item.type]}`}
      role="alert"
    >
      <svg className="w-5 h-5 flex-shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={icons[item.type]} />
      </svg>
      <span className="flex-1">{item.message}</span>
      <button onClick={() => onDismiss(item.id)} className="p-0.5 hover:opacity-80">
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  );
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  const dismiss = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const addToast = useCallback((type: ToastType, message: string) => {
    const id = generateId();
    setToasts((prev) => [...prev, { id, type, message }]);
  }, []);

  return (
    <ToastContext.Provider value={{ toast: addToast }}>
      {children}
      <div className="fixed top-4 right-4 z-[60] flex flex-col gap-2 max-sm:left-4 max-sm:right-4">
        {toasts.map((item) => (
          <ToastItem key={item.id} item={item} onDismiss={dismiss} />
        ))}
      </div>
    </ToastContext.Provider>
  );
}
