import { Routes, Route } from 'react-router-dom';

function AdminDashboardPage() {
  return <div>Admin Dashboard</div>;
}

export default function AdminRoutes() {
  return (
    <Routes>
      <Route index element={<AdminDashboardPage />} />
    </Routes>
  );
}
