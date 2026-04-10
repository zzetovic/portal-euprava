import { useTranslation } from 'react-i18next';

type RequestStatus = 'draft' | 'submitted' | 'processing_registry' | 'received_in_registry' | 'rejected_by_officer';

const statusClasses: Record<RequestStatus, string> = {
  draft: 'bg-gray-100 text-gray-700',
  submitted: 'bg-blue-100 text-blue-700',
  processing_registry: 'bg-blue-100 text-blue-700',
  received_in_registry: 'bg-green-100 text-success',
  rejected_by_officer: 'bg-red-50 text-burgundy',
};

interface StatusBadgeProps {
  status: RequestStatus;
  className?: string;
}

export function StatusBadge({ status, className = '' }: StatusBadgeProps) {
  const { t } = useTranslation();

  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${statusClasses[status]} ${className}`}
    >
      {t(`status.${status}`)}
    </span>
  );
}
