import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Toaster } from 'react-hot-toast';
import { GoogleOAuthProvider } from '@react-oauth/google';
import { useAuthStore } from './store/authStore';

// Pages
import LandingPage from './pages/LandingPage';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import DashboardPage from './pages/DashboardPage';
import OrdersPage from './pages/OrdersPage';
import CreateOrderPage from './pages/CreateOrderPage';
import RobotsPage from './pages/RobotsPage';
import AdminPage from './pages/AdminPage';
import ProfilePage from './pages/ProfilePage';
import MapPage from './pages/MapPage';
import CompleteProfilePage from './pages/CompleteProfilePage';
import FriendsPage from './pages/FriendsPage';
import WalletPage from './pages/WalletPage';
import RoutingTestPage from './pages/RoutingTestPage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" />;
}

function AdminRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user } = useAuthStore();
  return isAuthenticated && user?.role === 'Admin' ? <>{children}</> : <Navigate to="/dashboard" />;
}
function App() {
  return (
    <GoogleOAuthProvider clientId="277199733405-bn8d717l6h3s39qgu5g9fe15lrf5v18n.apps.googleusercontent.com">
      <QueryClientProvider client={queryClient}>
        <Toaster position="bottom-right" toastOptions={{
          className: 'sketch-border font-black text-xs',
          style: {
            borderRadius: '0px',
            background: 'var(--md-sys-color-surface-container-high)',
            color: 'var(--md-sys-color-on-surface)',
          }
        }} />
        <Router>
          <Routes>
            <Route path="/" element={<LandingPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/register" element={<RegisterPage />} />
            <Route path="/complete-profile" element={<CompleteProfilePage />} />

            <Route
              path="/dashboard"
              element={
                <ProtectedRoute>
                  <DashboardPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/orders"
              element={
                <ProtectedRoute>
                  <OrdersPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/orders/create"
              element={
                <ProtectedRoute>
                  <CreateOrderPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/robots"
              element={
                <AdminRoute>
                  <RobotsPage />
                </AdminRoute>
              }
            />

            <Route
              path="/profile"
              element={
                <ProtectedRoute>
                  <ProfilePage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/map"
              element={
                <ProtectedRoute>
                  <MapPage />
                </ProtectedRoute>
              }
            />
            
            <Route
              path="/friends"
              element={
                <ProtectedRoute>
                  <FriendsPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/wallet"
              element={
                <ProtectedRoute>
                  <WalletPage />
                </ProtectedRoute>
              }
            />

            <Route
              path="/admin"
              element={
                <AdminRoute>
                  <AdminPage />
                </AdminRoute>
              }
            />
            
            <Route
              path="/admin/routing-test"
              element={
                <AdminRoute>
                  <RoutingTestPage />
                </AdminRoute>
              }
            />
          </Routes>
        </Router>
      </QueryClientProvider>
    </GoogleOAuthProvider>
  );
}

export default App;
