import React from 'react';
import { motion } from 'framer-motion';
import { useQuery } from '@tanstack/react-query';
import Layout from '../components/Layout';
import { robotAPI } from '../lib/api';
import { SketchCard, SketchDivider } from '../components/common/SketchComponents';

const RobotsPage = () => {
  const { data: robots, isLoading } = useQuery({
    queryKey: ['robots'],
    queryFn: async () => {
      const response = await robotAPI.getAllRobots();
      return response.data;
    },
  });

  const getBatteryColor = (level: number) => {
    if (level >= 70) return 'bg-success';
    if (level >= 30) return 'bg-warning';
    return 'bg-error';
  };

  const getStatusInfo = (status: string) => {
    const info: Record<string, { color: string; icon: string }> = {
      Idle: { color: 'bg-success text-surface', icon: 'bolt' },
      Delivering: { color: 'bg-primary-container text-surface', icon: 'local_shipping' },
      Charging: { color: 'bg-warning text-on-warning', icon: 'battery_charging_full' },
      Maintenance: { color: 'bg-error text-surface', icon: 'build' },
    };
    return info[status] || { color: 'bg-surface-variant text-on-surface-variant', icon: 'smart_toy' };
  };

  return (
    <Layout>
      <div className="flex flex-col gap-8">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform rotate-1">
          <h1 className="font-headline-lg text-headline-lg mb-2">Robot Fleet</h1>
          <p className="font-body-lg text-on-surface-variant italic">Real-time telemetric data for our autonomous delivery grid.</p>
        </section>

        {isLoading ? (
          <div className="text-center py-24">
              <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
              <p className="mt-4 font-label-md">Syncing with fleet headquarters...</p>
          </div>
        ) : !robots || robots.length === 0 ? (
          <SketchCard className="text-center py-20 bg-surface-container-low" rotate>
            <span className="material-symbols-outlined text-6xl opacity-20 mb-4">smart_toy</span>
            <h3 className="font-headline-md text-xl">No robots connected</h3>
            <p className="font-body-md text-on-surface-variant">The fleet is currently offline or undergoing deep cycle maintenance.</p>
          </SketchCard>
        ) : (
          <>
            {/* Stats Overview */}
            <div className="grid grid-cols-2 lg:grid-cols-4 gap-6">
              {[
                { label: 'Available', status: 'Idle', icon: 'bolt', color: 'text-success' },
                { label: 'En Route', status: 'Delivering', icon: 'rocket_launch', color: 'text-primary-container' },
                { label: 'Charging', status: 'Charging', icon: 'battery_charging_full', color: 'text-warning' },
                { label: 'Repair', status: 'Maintenance', icon: 'build', color: 'text-error' }
              ].map((stat, i) => (
                <SketchCard key={stat.label} rotate={i % 2 === 0} className="bg-surface-container-low">
                   <div className="flex flex-col items-center gap-2">
                      <span className={`material-symbols-outlined text-3xl ${stat.color}`}>{stat.icon}</span>
                      <h3 className="font-headline-md text-2xl font-black">
                        {robots.filter((r) => r.status === stat.status).length}
                      </h3>
                      <p className="font-label-md text-[10px] uppercase font-black opacity-40 tracking-widest">{stat.label}</p>
                   </div>
                </SketchCard>
              ))}
            </div>

            {/* Robots Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-8">
              {robots.map((robot, index) => {
                const status = getStatusInfo(robot.status);
                return (
                  <SketchCard 
                    key={robot.id} 
                    rotate={index % 2 === 1}
                    className="group"
                  >
                    <div className="flex justify-between items-start mb-6">
                       <div className="relative">
                          <span className="material-symbols-outlined text-5xl text-primary-container">smart_toy</span>
                          <motion.div
                            className={`absolute -top-1 -right-1 w-4 h-4 sketch-border-thin ${status.color.split(' ')[0]}`}
                            animate={{ scale: [1, 1.3, 1] }}
                            transition={{ duration: 2, repeat: Infinity }}
                          />
                       </div>
                       <div className={`${status.color} px-3 py-1 sketch-border-thin text-[10px] font-black uppercase flex items-center gap-2 transform rotate-3`}>
                          <span className="material-symbols-outlined text-xs">{status.icon}</span>
                          {robot.status}
                       </div>
                    </div>

                    <h3 className="font-headline-md text-xl font-bold mb-1 tracking-tight">{robot.serialNumber}</h3>
                    <p className="font-label-md text-xs text-on-surface-variant italic mb-6">{robot.type} Class Unit</p>

                    <div className="space-y-6">
                       <div className="flex flex-col gap-2">
                          <div className="flex justify-between font-label-md text-[10px] font-black uppercase opacity-40">
                             <span>Energy Reserves</span>
                             <span>{robot.batteryLevel}%</span>
                          </div>
                          <div className="h-4 sketch-border-thin bg-surface-container-low overflow-hidden">
                             <motion.div
                                className={`h-full ${getBatteryColor(robot.batteryLevel)}`}
                                initial={{ width: 0 }}
                                animate={{ width: `${robot.batteryLevel}%` }}
                                transition={{ duration: 1, delay: index * 0.1 }}
                             />
                          </div>
                       </div>

                       <SketchDivider />

                       <div className="grid grid-cols-2 gap-4">
                          <div className="flex flex-col gap-1">
                             <span className="font-label-md text-[9px] uppercase opacity-40 font-black">Anchor Node</span>
                             <div className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">location_on</span>
                                <span className="font-body-md text-xs font-bold truncate">{robot.currentNode?.name || 'Unknown'}</span>
                             </div>
                          </div>
                          <div className="flex flex-col gap-1">
                             <span className="font-label-md text-[9px] uppercase opacity-40 font-black">Coordinates</span>
                             <div className="flex items-center gap-1">
                                <span className="material-symbols-outlined text-sm">gps_fixed</span>
                                <span className="font-body-md text-[10px] font-bold">
                                   {robot.currentLatitude?.toFixed(4)}, {robot.currentLongitude?.toFixed(4)}
                                </span>
                             </div>
                          </div>
                       </div>
                    </div>
                  </SketchCard>
                );
              })}
            </div>
          </>
        )}
      </div>
    </Layout>
  );
};

export default RobotsPage;
