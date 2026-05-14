import React, { ReactNode } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';
import { SketchAvatar } from './common/SketchComponents';
import { BASE_URL } from '../lib/api';

interface LayoutProps {
  children: ReactNode;
}

const Layout = ({ children }: LayoutProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuthStore();
  
  const profilePhotoUrl = user?.profilePhotoUrl 
    ? (user.profilePhotoUrl.startsWith('http') 
        ? user.profilePhotoUrl 
        : `${BASE_URL}/${user.profilePhotoUrl.replace(/\\/g, '/')}`.replace(/([^:]\/)\/+/g, "$1"))
    : null;

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  const navItems = [
    { path: '/dashboard', icon: 'dashboard', label: 'Dashboard' },
    { path: '/orders', icon: 'package', label: 'Orders' },
    { path: '/orders/create', icon: 'add_circle', label: 'New Order' },
    { path: '/wallet', icon: 'account_balance_wallet', label: 'Wallet' },
    { path: '/friends', icon: 'group', label: 'Friends' },
    ...(user?.role === 'Admin' ? [{ path: '/robots', icon: 'smart_toy', label: 'Robots' }] : []),
    { path: '/map', icon: 'map', label: 'Live Map' },
    { path: '/profile', icon: 'person', label: 'Profile' },
  ];

  if (user?.role === 'Admin') {
    navItems.push({ path: '/admin', icon: 'shield', label: 'Admin Panel' });
    navItems.push({ path: '/admin/routing-test', icon: 'route', label: 'Routing Test' });
  }

  return (
    <div className="flex min-h-screen bg-background selection:bg-tertiary-fixed-dim selection:text-tertiary-container">
      {/* SideNavBar (Desktop) */}
      <aside className="hidden md:flex flex-col h-screen w-64 border-r-2 border-primary-container border-opacity-30 bg-[#FCFAF7] py-6 px-4 z-20 shrink-0 sticky top-0">
        <div className="flex items-center gap-3 mb-8 px-2">
          <div className="flex flex-col items-center gap-2 py-4 mb-4 border-b-2 border-primary-container border-opacity-10 w-full">
            <div className="relative">
              <SketchAvatar 
                src={profilePhotoUrl} 
                alt={user?.userName}
                size="md"
                rotate={3}
              />
              {user?.role === 'Admin' && (
                <div className="absolute -bottom-1 -right-1 bg-tertiary-fixed-dim text-[10px] font-bold px-1 sketch-border transform -rotate-12">ADMIN</div>
              )}
            </div>
            <div className="text-center mt-2">
              <h2 className="text-xl font-black text-primary-container leading-tight">{user?.userName}</h2>
              <p className="text-xs font-medium text-primary-container/60 italic uppercase tracking-wider">{user?.role}</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 flex flex-col gap-2">
          {navItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              className={`flex items-center gap-3 px-3 py-2 transition-all group ${
                location.pathname === item.path
                  ? 'text-primary-container font-black rotate-1 scale-95 bg-primary-container/5 sketch-border-thin'
                  : 'text-primary-container/70 hover:text-primary-container hover:bg-primary-container/5 rounded'
              }`}
            >
              <span className="material-symbols-outlined group-hover:rotate-12 transition-transform">
                {item.icon}
              </span>
              <span className="font-label-md font-bold">{item.label}</span>
            </Link>
          ))}
        </nav>

        <div className="mt-auto pt-4">
          <button
            onClick={handleLogout}
            className="w-full py-3 px-4 bg-primary-container text-surface font-label-md sketch-border hover:bg-primary-container/90 transition-colors flex items-center justify-center gap-2"
          >
            <span className="material-symbols-outlined">logout</span>
            Logout
          </button>
        </div>
      </aside>

      {/* Main Content */}
      <main className="flex-1 w-full overflow-x-hidden">
        <div className="max-w-[1200px] mx-auto px-4 md:px-margin py-8 md:py-12 pb-32 md:pb-12">
          {children}
        </div>
      </main>

      {/* BottomNavBar (Mobile) */}
      <nav className="md:hidden bg-[#FCFAF7] text-primary-container font-body-md text-[11px] font-bold uppercase tracking-wider fixed bottom-0 left-0 w-full z-50 flex justify-around items-center px-4 py-3 border-t-2 border-primary-container rounded-t-lg shadow-[0_-4px_0_0_rgba(47,49,50,0.1)]">
        {navItems.slice(0, 4).map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`flex flex-col items-center justify-center transition-transform hover:rotate-1 ${
              location.pathname === item.path
                ? 'bg-primary-container/10 rounded-lg border border-primary-container px-3 py-1 scale-105'
                : 'opacity-70'
            }`}
          >
            <span className="material-symbols-outlined mb-1">{item.icon}</span>
            <span>{item.label.split(' ')[0]}</span>
          </Link>
        ))}
      </nav>

    </div>

  );
};

export default Layout;
