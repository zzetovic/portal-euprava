import { Routes, Route } from 'react-router-dom';

function InboxPage() {
  return <div>Office Inbox</div>;
}

export default function OfficeRoutes() {
  return (
    <Routes>
      <Route path="/inbox" element={<InboxPage />} />
    </Routes>
  );
}
