import { Routes, Route, Navigate } from 'react-router-dom';
import { AdminLayout } from './AdminLayout';
import { RequestTypeListPage } from './RequestTypeListPage';
import { RequestTypeEditPage } from './RequestTypeEditPage';

export default function AdminRoutes() {
  return (
    <Routes>
      <Route element={<AdminLayout />}>
        <Route index element={<Navigate to="request-types" replace />} />
        <Route path="request-types" element={<RequestTypeListPage />} />
        <Route path="request-types/new" element={<RequestTypeEditPage />} />
        <Route path="request-types/:id" element={<RequestTypeEditPage />} />
        <Route path="users" element={<div>Users</div>} />
        <Route path="audit" element={<div>Audit</div>} />
      </Route>
    </Routes>
  );
}
