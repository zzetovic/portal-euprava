import { Routes, Route } from 'react-router-dom';

function FinancePage() {
  return <div>Finance</div>;
}

export default function FinanceRoutes() {
  return (
    <Routes>
      <Route index element={<FinancePage />} />
    </Routes>
  );
}
