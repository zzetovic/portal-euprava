import { Routes, Route, Navigate } from 'react-router-dom';
import { OfficerLayout } from './OfficerLayout';
import { InboxPage } from './InboxPage';
import { OfficerRequestDetailPage } from './RequestDetailPage';

export default function OfficeRoutes() {
  return (
    <Routes>
      <Route element={<OfficerLayout />}>
        <Route index element={<Navigate to="inbox" replace />} />
        <Route path="inbox" element={<InboxPage />} />
        <Route path="requests/:id" element={<OfficerRequestDetailPage />} />
      </Route>
    </Routes>
  );
}
