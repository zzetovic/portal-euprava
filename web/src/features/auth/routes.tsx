import { Routes, Route } from 'react-router-dom';

function LoginPage() {
  return <div>Login</div>;
}

function RegisterPage() {
  return <div>Register</div>;
}

export default function AuthRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
    </Routes>
  );
}
