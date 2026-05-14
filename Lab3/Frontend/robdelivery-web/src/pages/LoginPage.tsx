import React from 'react';
import { useState } from 'react';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { GoogleLogin } from '@react-oauth/google';
import toast from 'react-hot-toast';
import { authAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchButton, SketchInput, SketchDivider } from '../components/common/SketchComponents';

const LoginPage = () => {
  const navigate = useNavigate();
  const setAuth = useAuthStore((state) => state.setAuth);

  const [formData, setFormData] = useState({
    email: '',
    password: '',
  });

  const handleAuthSuccess = async (token: string) => {
    // Fetch user profile with the new token
    localStorage.setItem('token', token);
    try {
      const profileRes = await authAPI.getCurrentUser();
      setAuth(profileRes.data, token);
      navigate('/dashboard');
    } catch {
      toast.error('Failed to load user profile');
    }
  };

  const loginMutation = useMutation({
    mutationFn: authAPI.login,
    onSuccess: async (response) => {
      const { status, token, googleId, email, userName } = response.data;

      if (status === 'NeedsAdditionalInfo') {
        // Google user not registered → redirect to complete profile
        toast('Please complete your registration', { icon: '📋' });
        navigate('/complete-profile', {
          state: { googleId, email, userName },
        });
        return;
      }

      if (status === 'Success' && token) {
        await handleAuthSuccess(token);
      }
    },
    onError: (error: any) => {
      console.error('Login error:', error.response?.data);
      const message = error.response?.data?.message || error.response?.data?.status || 'Login failed. Please check your credentials.';
      toast.error(message);
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    loginMutation.mutate(formData);
  };

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({
      ...prev,
      [e.target.name]: e.target.value,
    }));
  };

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <motion.div
        initial={{ opacity: 0, y: 20 }}
        animate={{ opacity: 1, y: 0 }}
        className="w-full max-w-md"
      >
        <SketchCard rotate shadow className="relative overflow-hidden">
          <div className="absolute top-2 right-4 text-outline-variant opacity-20">
            <span className="material-symbols-outlined text-6xl transform rotate-12">login</span>
          </div>

          <div className="text-center mb-8">
            <div className="flex items-center justify-center gap-3 mb-4">
              <span className="material-symbols-outlined text-4xl text-primary-container">smart_toy</span>
              <h1 className="text-3xl font-black text-primary-container">RobDelivery</h1>
            </div>
            <p className="font-body-lg text-on-surface-variant">Welcome Back! Let's get shipping.</p>
          </div>

          <form onSubmit={handleSubmit} className="flex flex-col gap-6">
            <SketchInput
              label="Email Address"
              name="email"
              type="email"
              placeholder="alex@example.com"
              required
              value={formData.email}
              onChange={handleChange}
              disabled={loginMutation.isPending}
            />

            <SketchInput
              label="Password"
              name="password"
              type="password"
              placeholder="••••••••"
              required
              value={formData.password}
              onChange={handleChange}
              disabled={loginMutation.isPending}
            />

            <SketchButton
              type="submit"
              className="w-full mt-2"
              icon={loginMutation.isPending ? 'sync' : 'login'}
            >
              {loginMutation.isPending ? 'Authenticating...' : 'Sign In'}
            </SketchButton>

            <div className="flex flex-col items-center gap-4 mt-2">
              <div className="w-full flex items-center gap-2 text-on-surface-variant/40">
                <div className="h-[1px] flex-1 bg-current opacity-20" />
                <span className="text-[10px] uppercase font-bold tracking-widest">or continue with</span>
                <div className="h-[1px] flex-1 bg-current opacity-20" />
              </div>
              
              <div className="w-full flex justify-center">
                <GoogleLogin
                  onSuccess={(credentialResponse) => {
                    if (credentialResponse.credential) {
                      loginMutation.mutate({ googleJwtToken: credentialResponse.credential });
                    }
                  }}
                  onError={() => {
                    toast.error('Google Login Failed');
                  }}
                  useOneTap
                  theme="outline"
                  shape="square"
                />
              </div>
            </div>
          </form>

          <SketchDivider className="my-8" />

          <div className="text-center">
            <p className="font-body-md text-on-surface-variant mb-4">
              Don't have an account?{' '}
              <Link to="/register" className="text-primary-container font-black">
                Join the Team
              </Link>
            </p>
            <Link to="/" className="font-label-md text-xs uppercase tracking-widest text-primary-container/60 hover:text-primary-container transition-colors">
              ← Back to Mission Control
            </Link>
          </div>
        </SketchCard>
      </motion.div>
    </div>
  );
};

export default LoginPage;
