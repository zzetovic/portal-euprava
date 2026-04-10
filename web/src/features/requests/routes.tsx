import { Routes, Route } from 'react-router-dom';
import { RequestListPage } from './RequestListPage';
import { SelectTypePage } from './SelectTypePage';
import { PreflightPage } from './PreflightPage';
import { FormPage } from './FormPage';
import { ReviewPage } from './ReviewPage';
import { SubmittedPage } from './SubmittedPage';
import { RequestDetailPage } from './RequestDetailPage';

export default function RequestRoutes() {
  return (
    <Routes>
      <Route index element={<RequestListPage />} />
      <Route path="new" element={<SelectTypePage />} />
      <Route path="new/:code" element={<PreflightPage />} />
      <Route path=":id" element={<RequestDetailPage />} />
      <Route path=":id/edit" element={<FormPage />} />
      <Route path=":id/review" element={<ReviewPage />} />
      <Route path=":id/submitted" element={<SubmittedPage />} />
    </Routes>
  );
}
