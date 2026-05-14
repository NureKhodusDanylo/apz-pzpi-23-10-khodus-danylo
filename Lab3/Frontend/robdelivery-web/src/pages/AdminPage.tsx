import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import Layout from '../components/Layout';
import { adminAPI } from '../lib/api';
import { SketchCard, SketchButton, SketchDivider } from '../components/common/SketchComponents';

const AdminPage = () => {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['admin', 'stats'],
    queryFn: async () => {
      const response = await adminAPI.getStats();
      return response.data;
    },
  });

  const { data: efficiency } = useQuery({
    queryKey: ['admin', 'efficiency'],
    queryFn: async () => {
      const response = await adminAPI.getRobotEfficiency();
      return response.data;
    },
  });

  const handleExport = async () => {
    try {
      const response = await adminAPI.exportDeliveryHistory();
      const url = window.URL.createObjectURL(new Blob([response.data]));
      const link = document.createElement('a');
      link.href = url;
      link.setAttribute('download', 'delivery-history.json');
      document.body.appendChild(link);
      link.click();
      link.remove();
    } catch (error) {
      alert('Failed to export delivery history');
    }
  };

  const handleBackup = async () => {
    try {
      await adminAPI.createBackup();
      alert('Backup created successfully');
    } catch (error) {
      alert('Failed to create backup');
    }
  };

  return (
    <Layout>
      <div className="flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform rotate-1 flex flex-col md:flex-row justify-between items-center gap-6">
          <div>
            <h1 className="font-headline-lg text-headline-lg mb-2">System Overview</h1>
            <p className="font-body-lg text-on-surface-variant italic">High-level diagnostics and network synchronization status.</p>
          </div>
          <div className="flex gap-4">
            <SketchButton variant="secondary" icon="download" onClick={handleExport}>Export Manifests</SketchButton>
            <SketchButton icon="cloud_sync" onClick={handleBackup}>System Backup</SketchButton>
          </div>
        </section>

        {isLoading ? (
           <div className="text-center py-24">
              <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
              <p className="mt-4 font-label-md">Aggregating system logs...</p>
           </div>
        ) : (
          <>
            {/* Main Stats Grid */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
              {[
                { label: 'Total Citizens', value: stats?.totalUsers, icon: 'group', color: 'text-primary-container' },
                { label: 'Total Shipments', value: stats?.totalOrders, icon: 'inventory_2', color: 'text-secondary' },
                { label: 'Active Fleet', value: stats?.totalRobots, icon: 'smart_toy', color: 'text-tertiary-container' },
                { label: 'Network Nodes', value: stats?.totalNodes, icon: 'hub', color: 'text-success' }
              ].map((stat, i) => (
                <SketchCard key={stat.label} rotate={i % 2 === 0} className="bg-surface-container-low">
                   <div className="flex flex-col items-center gap-2">
                      <span className={`material-symbols-outlined text-3xl ${stat.color}`}>{stat.icon}</span>
                      <h3 className="font-headline-md text-3xl font-black">{stat.value}</h3>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40 tracking-widest">{stat.label}</p>
                   </div>
                </SketchCard>
              ))}
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
              {/* Order Analytics */}
              <SketchCard rotate>
                <div className="flex items-center gap-3 mb-6">
                   <span className="material-symbols-outlined text-2xl">monitoring</span>
                   <h3 className="font-headline-md text-xl">Order Protocol Data</h3>
                </div>
                <div className="space-y-6">
                   <div className="flex justify-between items-center p-4 bg-surface-container-low sketch-border-thin">
                      <div className="flex items-center gap-3">
                         <span className="material-symbols-outlined text-warning">pending</span>
                         <span className="font-label-md text-sm">Active Missions</span>
                      </div>
                      <span className="text-xl font-black">{stats?.activeOrders}</span>
                   </div>
                   <div className="flex justify-between items-center p-4 bg-surface-container-low sketch-border-thin">
                      <div className="flex items-center gap-3">
                         <span className="material-symbols-outlined text-success">task_alt</span>
                         <span className="font-label-md text-sm">Successful Deliveries</span>
                      </div>
                      <span className="text-xl font-black">{stats?.completedOrders}</span>
                   </div>
                   <div className="flex justify-between items-center p-4 bg-surface-container-low sketch-border-thin">
                      <div className="flex items-center gap-3">
                         <span className="material-symbols-outlined text-error">cancel</span>
                         <span className="font-label-md text-sm">Aborted Requests</span>
                      </div>
                      <span className="text-xl font-black">{stats?.cancelledOrders}</span>
                   </div>
                </div>
              </SketchCard>

              {/* Robot Health */}
              <SketchCard rotate={false}>
                 <div className="flex items-center gap-3 mb-6">
                   <span className="material-symbols-outlined text-2xl">vital_signs</span>
                   <h3 className="font-headline-md text-xl">Fleet Health Parameters</h3>
                </div>
                <div className="grid grid-cols-2 gap-4">
                   {[
                     { label: 'Combat Ready', value: stats?.availableRobots, icon: 'bolt', color: 'text-success' },
                     { label: 'Engaged', value: stats?.busyRobots, icon: 'rocket_launch', color: 'text-primary-container' },
                     { label: 'Refueling', value: stats?.chargingRobots, icon: 'battery_charging_full', color: 'text-warning' },
                     { label: 'Avg Battery', value: `${stats?.averageBatteryLevel.toFixed(1)}%`, icon: 'battery_std', color: 'text-tertiary-container' }
                   ].map(s => (
                     <div key={s.label} className="p-4 bg-surface-container-low sketch-border-thin flex flex-col items-center gap-1">
                        <span className={`material-symbols-outlined text-xl ${s.color}`}>{s.icon}</span>
                        <span className="text-lg font-black">{s.value}</span>
                        <span className="text-[9px] uppercase font-black opacity-40">{s.label}</span>
                     </div>
                   ))}
                </div>
              </SketchCard>
            </div>

            {/* Revenue Analytics */}
            {stats && (
               <SketchCard shadow className="bg-primary-container text-surface p-12 overflow-hidden relative">
                  <div className="absolute -right-10 -top-10 opacity-10">
                     <span className="material-symbols-outlined text-[200px]">payments</span>
                  </div>
                  <div className="relative z-10">
                     <h3 className="font-headline-md text-2xl mb-8">Financial Ledger</h3>
                     <div className="grid grid-cols-1 md:grid-cols-3 gap-12">
                        <div className="flex flex-col border-l-2 border-surface/20 pl-6">
                           <span className="font-label-md text-[10px] uppercase opacity-60 font-black tracking-widest">Delivery Fees</span>
                           <span className="text-3xl font-black">${stats.deliveryRevenue.toFixed(2)}</span>
                        </div>
                        <div className="flex flex-col border-l-2 border-surface/20 pl-6">
                           <span className="font-label-md text-[10px] uppercase opacity-60 font-black tracking-widest">Cargo Value</span>
                           <span className="text-3xl font-black">${stats.productRevenue.toFixed(2)}</span>
                        </div>
                        <div className="flex flex-col border-l-2 border-surface pl-6">
                           <span className="font-label-md text-[10px] uppercase font-black tracking-widest">Gross Network Value</span>
                           <span className="text-4xl font-black text-secondary-fixed-dim">${stats.totalRevenue.toFixed(2)}</span>
                        </div>
                     </div>
                  </div>
               </SketchCard>
            )}

            {/* Efficiency Rankings */}
            {efficiency && efficiency.length > 0 && (
              <SketchCard rotate className="p-8">
                <h3 className="font-headline-md text-2xl mb-8">Fleet Performance Rankings</h3>
                <div className="space-y-4">
                  {efficiency
                    .sort((a, b) => b.efficiencyScore - a.efficiencyScore)
                    .slice(0, 5)
                    .map((robot, index) => (
                      <div key={robot.robotId} className="flex items-center gap-6 p-4 bg-surface-container-low sketch-border-thin transform hover:scale-[1.01] transition-transform">
                        <div className="w-12 h-12 flex items-center justify-center sketch-border font-black text-2xl bg-surface">
                          #{index + 1}
                        </div>
                        <div className="flex-1">
                          <div className="flex justify-between items-center mb-2">
                             <h4 className="font-headline-md text-lg">{robot.serialNumber}</h4>
                             <span className="font-black text-primary-container">{robot.efficiencyScore.toFixed(1)} Pts</span>
                          </div>
                          <div className="flex items-center gap-4 text-[10px] font-black uppercase opacity-40">
                             <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-xs">task_alt</span>
                                {robot.completedOrders} Missions
                             </span>
                             <span className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-xs">battery_std</span>
                                {robot.batteryLevel}% Reserves
                             </span>
                          </div>
                          <div className="mt-3 h-2 sketch-border-thin bg-surface-container overflow-hidden">
                             <div 
                              className="h-full bg-primary-container"
                              style={{ width: `${robot.efficiencyScore}%` }}
                             />
                          </div>
                        </div>
                      </div>
                    ))}
                </div>
              </SketchCard>
            )}
          </>
        )}
      </div>
    </Layout>
  );
};

export default AdminPage;
