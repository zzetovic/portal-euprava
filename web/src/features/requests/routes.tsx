import { Routes, Route } from 'react-router-dom';

function RequestListPage() {
  return <div>My Requests</div>;
}

function NewRequestPage() {
  return <div>New Request</div>;
}

export default function RequestRoutes() {
  return (
    <Routes>
      <Route index element={<RequestListPage />} />
      <Route path="/new" element={<NewRequestPage />} />
    </Routes>
  );
}
