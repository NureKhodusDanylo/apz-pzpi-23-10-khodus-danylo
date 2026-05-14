import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import { useNavigate, Link } from 'react-router-dom';
import Layout from '../components/Layout';
import { orderAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchButton, SketchDivider } from '../components/common/SketchComponents';

const DashboardPage = () => {
  const navigate = useNavigate();
  const user = useAuthStore((state) => state.user);

  const { data: myOrders, isLoading } = useQuery({
    queryKey: ['orders', 'my-orders'],
    queryFn: async () => {
      const response = await orderAPI.getMyOrders();
      return response.data;
    },
  });

  const allOrders = myOrders || [];
  const activeOrders = allOrders.filter((o) =>
    ['AwaitingPayment', 'AwaitingConfirmation', 'Pending', 'Processing', 'EnRoute'].includes(o.status)
  );
  
  const recentOrders = [...allOrders]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  return (
    <Layout>
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-gutter">
        {/* Left Column: Welcome & Active Shipments */}
        <div className="lg:col-span-8 flex flex-col gap-8">
          {/* Welcome Section */}
          <section className="bg-surface-container p-6 md:p-8 sketch-border sketch-shadow relative transform -rotate-1">
            <div className="absolute top-2 right-4 text-outline-variant opacity-30">
              <span className="material-symbols-outlined text-4xl transform rotate-12">draw</span>
            </div>
            <h1 className="font-headline-lg text-headline-lg mb-2">Hello, {user?.userName}</h1>
            <p className="font-body-lg text-body-lg text-on-surface-variant">
              Ready to manage your fleet and track deliveries?
            </p>
          </section>

          {/* Active Orders Section */}
          <section>
            <div className="flex justify-between items-end mb-4 px-2">
              <h2 className="font-headline-md text-headline-md">Active Shipments</h2>
              <Link className="font-label-md text-label-md text-primary-container" to="/orders">View All</Link>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {isLoading ? (
                <div className="col-span-2 text-center py-12">
                   <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
                   <p className="mt-2 font-label-md">Locating robots...</p>
                </div>
              ) : activeOrders.length === 0 ? (
                <SketchCard className="col-span-2 text-center py-12 bg-surface-container-low" rotate>
                  <span className="material-symbols-outlined text-6xl opacity-20 mb-4">package_2</span>
                  <h3 className="font-headline-md text-lg mb-2">No active shipments</h3>
                  <p className="font-body-md text-on-surface-variant mb-6">Your robots are resting. Give them something to do!</p>
                  <SketchButton onClick={() => navigate('/orders/create')} icon="add" className="mx-auto">
                    New Order
                  </SketchButton>
                </SketchCard>
              ) : (
                activeOrders.slice(0, 4).map((order, idx) => (
                  <div 
                    key={order.id} 
                    className={`bg-surface-container-low p-5 sketch-border group hover:bg-surface-container transition-colors relative cursor-pointer ${idx % 2 === 1 ? 'transform rotate-1' : ''}`}
                    onClick={() => navigate('/orders')}
                  >
                    <div className="absolute -top-3 -right-3 bg-tertiary-fixed-dim text-tertiary-container text-xs font-bold px-2 py-1 sketch-border transform rotate-12">
                      {order.status}
                    </div>
                    <div className="w-full h-32 mb-4 sketch-border overflow-hidden bg-surface-variant flex items-center justify-center">
                      <span className="material-symbols-outlined text-6xl text-outline-variant opacity-50">
                        {order.status === 'EnRoute' ? 'local_shipping' : 'precision_manufacturing'}
                      </span>
                    </div>
                    <h3 className="font-headline-md text-lg font-bold mb-1">{order.name}</h3>
                    <p className="font-body-md text-sm text-on-surface-variant mb-4">To: {order.recipientName}</p>
                    <div className="flex items-center gap-2">
                      <span className="material-symbols-outlined text-sm">payments</span>
                      <span className="font-label-md text-xs text-on-surface-variant">${order.deliveryPrice}</span>
                    </div>
                  </div>
                ))
              )}
            </div>
          </section>

          {/* Quick Stats Section */}
          <section>
             <div className="flex justify-between items-end mb-4 px-2">
              <h2 className="font-headline-md text-headline-md">Fleet Status</h2>
            </div>
            <div className="flex flex-col gap-4">
               <div className="bg-surface p-4 sketch-border flex items-center justify-between hover:bg-surface-container-low transition-colors">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 sketch-border-thin flex items-center justify-center bg-secondary-fixed text-on-secondary-fixed">
                    <span className="material-symbols-outlined">smart_toy</span>
                  </div>
                  <div>
                    <h4 className="font-bold text-lg">Active Robots</h4>
                    <p className="text-sm text-on-surface-variant">Monitoring real-time telemetry</p>
                  </div>
                </div>
                <div className="text-2xl font-black px-4">12</div>
              </div>

              <div className="bg-surface p-4 sketch-border flex items-center justify-between hover:bg-surface-container-low transition-colors transform -rotate-1">
                <div className="flex items-center gap-4">
                  <div className="w-12 h-12 sketch-border-thin flex items-center justify-center bg-error-container text-on-error-container">
                    <span className="material-symbols-outlined">warning</span>
                  </div>
                  <div>
                    <h4 className="font-bold text-lg">System Alerts</h4>
                    <p className="text-sm text-on-surface-variant">2 robots require battery maintenance</p>
                  </div>
                </div>
                 <div className="text-2xl font-black px-4 text-error">2</div>
              </div>
            </div>
          </section>
        </div>

        {/* Right Column: Recent Activity (Task Style) */}
        <div className="lg:col-span-4 flex flex-col gap-6">
          <section className="bg-surface-container-lowest p-6 sketch-border h-full flex flex-col">
            <div className="flex justify-between items-center mb-6">
              <h2 className="font-headline-md text-xl font-bold flex items-center gap-2">
                <span className="material-symbols-outlined">history</span>
                Recent Activity
              </h2>
              <button 
                onClick={() => navigate('/orders/create')}
                className="w-8 h-8 flex items-center justify-center sketch-border-thin hover:bg-surface-variant transition-colors rounded-full"
              >
                <span className="material-symbols-outlined text-sm">add</span>
              </button>
            </div>

            <div className="flex flex-col gap-4 flex-grow">
              {recentOrders.length === 0 ? (
                <p className="text-center py-8 text-on-surface-variant font-body-md italic">No recent activity</p>
              ) : (
                recentOrders.map((order, idx) => (
                  <React.Fragment key={order.id}>
                    <div className="flex items-start gap-3 group cursor-pointer" onClick={() => navigate('/orders')}>
                       <div className="relative flex items-center justify-center mt-1">
                          <div className={`w-5 h-5 sketch-border-thin flex items-center justify-center ${order.status === 'Delivered' ? 'bg-primary-container' : 'bg-surface'}`}>
                            <span className={`material-symbols-outlined text-surface text-[16px] ${order.status === 'Delivered' ? 'opacity-100' : 'opacity-0'}`}>
                              check
                            </span>
                          </div>
                        </div>
                        <div>
                          <span className={`font-body-md text-base ${order.status === 'Delivered' ? 'line-through text-on-surface-variant' : ''}`}>
                            {order.name}
                          </span>
                          <p className={`font-label-md text-xs mt-1 ${order.status === 'Delivered' ? 'text-on-surface-variant' : 'text-error'}`}>
                            {order.status === 'Delivered' ? 'Completed' : order.status}
                          </p>
                        </div>
                    </div>
                    {idx < recentOrders.length - 1 && <div className="sketch-divider opacity-50" />}
                  </React.Fragment>
                ))
              )}
            </div>

            <SketchButton variant="secondary" className="mt-6 w-full text-sm py-2" onClick={() => navigate('/orders')}>
              View All Activity
            </SketchButton>
          </section>
        </div>
      </div>
    </Layout>
  );
};

export default DashboardPage;
