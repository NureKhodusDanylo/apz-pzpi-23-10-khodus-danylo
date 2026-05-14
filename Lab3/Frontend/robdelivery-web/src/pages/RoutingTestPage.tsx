import React, { useState } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { useQuery, useMutation } from '@tanstack/react-query';
import Layout from '../components/Layout';
import { robotAPI, nodeAPI } from '../lib/api';
import { SketchCard, SketchButton, SketchDivider } from '../components/common/SketchComponents';
import { toast } from 'react-hot-toast';

const RoutingTestPage = () => {
  const [robotId, setRobotId] = useState<number>(0);
  const [pickupNodeId, setPickupNodeId] = useState<number>(0);
  const [dropoffNodeId, setDropoffNodeId] = useState<number>(0);
  const [weight, setWeight] = useState<number>(1.0);

  const { data: robots } = useQuery({
    queryKey: ['robots'],
    queryFn: () => robotAPI.getAllRobots().then(res => res.data),
  });

  const { data: nodes } = useQuery({
    queryKey: ['nodes'],
    queryFn: () => nodeAPI.getAllNodes().then(res => res.data),
  });

  const testMutation = useMutation({
    mutationFn: (data: any) => robotAPI.testRouting(data).then(res => res.data),
    onSuccess: (data) => {
      if (data.success) {
        toast.success('Protocol analysis complete: Positive');
      } else {
        toast.error('Protocol analysis complete: Negative');
      }
    },
    onError: () => {
      toast.error('System failure: Check network integrity');
    }
  });

  const handleExecute = () => {
    if (!robotId || !pickupNodeId || !dropoffNodeId) {
      toast.error('Incomplete data for execution');
      return;
    }
    testMutation.mutate({
      robotId,
      pickupNodeId,
      dropoffNodeId,
      packageWeight: weight
    });
  };

  return (
    <Layout>
      <div className="flex flex-col gap-8 pb-20">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform -rotate-1">
          <h1 className="font-headline-lg text-headline-lg mb-2">Routing Diagnostic Terminal</h1>
          <p className="font-body-lg text-on-surface-variant italic">Full-spectrum simulation of route calculation and energy depletion.</p>
        </section>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
          {/* Configuration Panel */}
          <div className="lg:col-span-1 space-y-8">
            <SketchCard rotate>
              <h3 className="font-headline-md text-xl mb-6 flex items-center gap-2">
                <span className="material-symbols-outlined">settings_suggest</span>
                Parameters
              </h3>
              
              <div className="space-y-6">
                <div>
                  <label className="block font-black text-[10px] uppercase opacity-60 mb-2 tracking-widest">Active Fleet Unit</label>
                  <select 
                    value={robotId} 
                    onChange={e => setRobotId(Number(e.target.value))}
                    className="w-full p-3 bg-surface sketch-border font-black text-sm outline-none"
                  >
                    <option value={0}>Select Unit...</option>
                    {robots?.map(r => (
                      <option key={r.id} value={r.id}>{r.serialNumber} ({r.type} - {r.batteryLevel}%)</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-black text-[10px] uppercase opacity-60 mb-2 tracking-widest">Pickup Node</label>
                  <select 
                    value={pickupNodeId} 
                    onChange={e => setPickupNodeId(Number(e.target.value))}
                    className="w-full p-3 bg-surface sketch-border font-black text-sm outline-none"
                  >
                    <option value={0}>Select Node...</option>
                    {nodes?.map(n => (
                      <option key={n.id} value={n.id}>{n.name}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-black text-[10px] uppercase opacity-60 mb-2 tracking-widest">Dropoff Node</label>
                  <select 
                    value={dropoffNodeId} 
                    onChange={e => setDropoffNodeId(Number(e.target.value))}
                    className="w-full p-3 bg-surface sketch-border font-black text-sm outline-none"
                  >
                    <option value={0}>Select Node...</option>
                    {nodes?.map(n => (
                      <option key={n.id} value={n.id}>{n.name}</option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-black text-[10px] uppercase opacity-60 mb-2 tracking-widest">Payload Weight (kg)</label>
                  <input 
                    type="number"
                    step="0.1"
                    min="0"
                    value={weight}
                    onChange={e => setWeight(Number(e.target.value))}
                    className="w-full p-3 bg-surface sketch-border font-black text-sm outline-none"
                  />
                </div>

                <SketchButton 
                  className="w-full" 
                  onClick={handleExecute}
                  disabled={testMutation.isPending}
                >
                  {testMutation.isPending ? 'Simulating...' : 'Execute Simulation'}
                </SketchButton>
              </div>
            </SketchCard>

            {testMutation.data && (
              <motion.div 
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
              >
                <SketchCard className={testMutation.data.success ? 'bg-success/10' : 'bg-error/10'}>
                   <h3 className="font-headline-md text-xl mb-4 flex items-center gap-2">
                    <span className="material-symbols-outlined">
                      {testMutation.data.success ? 'check_circle' : 'cancel'}
                    </span>
                    Simulation Result
                  </h3>
                  <div className="space-y-3 font-black uppercase text-[11px] tracking-wider">
                    <div className="flex justify-between">
                      <span className="opacity-60">Status:</span>
                      <span className={testMutation.data.success ? 'text-success' : 'text-error'}>
                        {testMutation.data.success ? 'FEASIBLE' : 'INFEASIBLE'}
                      </span>
                    </div>
                    <div className="flex justify-between">
                      <span className="opacity-60">Total Distance:</span>
                      <span>{testMutation.data.totalDistanceMeters.toFixed(1)} m</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="opacity-60">Est. Battery Cost:</span>
                      <span className={testMutation.data.estimatedBatteryUsagePercent > 50 ? 'text-warning' : ''}>
                        {testMutation.data.estimatedBatteryUsagePercent.toFixed(1)}%
                      </span>
                    </div>
                  </div>
                </SketchCard>
              </motion.div>
            )}
          </div>

          {/* Diagnostic Logs */}
          <div className="lg:col-span-2">
            <SketchCard rotate={false} className="h-full flex flex-col bg-surface-container-highest">
              <div className="flex items-center justify-between mb-4">
                <h3 className="font-headline-md text-xl flex items-center gap-2">
                  <span className="material-symbols-outlined">terminal</span>
                  System Execution Logs
                </h3>
                {testMutation.isPending && (
                  <span className="material-symbols-outlined animate-spin">sync</span>
                )}
              </div>
              
              <div className="flex-1 bg-on-surface text-surface p-6 font-mono text-xs overflow-y-auto max-h-[600px] sketch-border-thin shadow-inner custom-scrollbar">
                <AnimatePresence mode="popLayout">
                  {!testMutation.data && !testMutation.isPending && (
                    <p className="opacity-40 italic">Waiting for simulation trigger...</p>
                  )}
                  {testMutation.isPending && (
                    <motion.p 
                      initial={{ opacity: 0 }}
                      animate={{ opacity: [0, 1, 0] }}
                      transition={{ repeat: Infinity, duration: 1.5 }}
                      className="text-primary-container font-bold"
                    >
                      {"> ANALYZING SPATIAL DATA..."}
                    </motion.p>
                  )}
                  {testMutation.data?.logs.map((log: string, idx: number) => (
                    <motion.div 
                      key={idx}
                      initial={{ opacity: 0, x: -10 }}
                      animate={{ opacity: 1, x: 0 }}
                      transition={{ delay: idx * 0.05 }}
                      className="mb-1 border-l-2 border-primary-container/20 pl-3 py-1 hover:bg-surface/10 transition-colors"
                    >
                      <span className="opacity-40 mr-2">[{idx.toString().padStart(3, '0')}]</span>
                      <span className={log.includes('Distance:') ? 'text-primary-container' : log.includes('ERROR') || log.includes('too far') ? 'text-error' : ''}>
                        {log}
                      </span>
                    </motion.div>
                  ))}
                </AnimatePresence>
              </div>

              {testMutation.data?.route.length > 0 && (
                <div className="mt-6">
                  <h4 className="font-black text-[10px] uppercase opacity-60 mb-3 tracking-widest">Spatial Segments</h4>
                  <div className="flex flex-wrap gap-3">
                    {testMutation.data.route.map((seg: any, i: number) => (
                      <div key={i} className="px-3 py-1 bg-surface sketch-border-thin text-[10px] font-black flex items-center gap-2">
                        <span>{seg.fromNodeName}</span>
                        <span className="material-symbols-outlined text-xs">arrow_forward</span>
                        <span>{seg.toNodeName}</span>
                        <span className="opacity-40">({seg.distanceMeters.toFixed(0)}m)</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </SketchCard>
          </div>
        </div>
      </div>
    </Layout>
  );
};

export default RoutingTestPage;
