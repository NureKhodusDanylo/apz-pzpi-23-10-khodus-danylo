import React from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { SketchCard, SketchButton, SketchDivider } from './SketchComponents';

interface DiagnosticModalProps {
  isOpen: boolean;
  onClose: () => void;
  logs: string[];
  title?: string;
}

export const DiagnosticModal = ({ isOpen, onClose, logs, title = "Routing Diagnostics" }: DiagnosticModalProps) => {
  return (
    <AnimatePresence>
      {isOpen && (
        <div className="fixed inset-0 z-[200] flex items-center justify-center p-4 bg-primary-container/20 backdrop-blur-sm" onClick={onClose}>
          <motion.div
            initial={{ scale: 0.9, opacity: 0, rotate: -2 }}
            animate={{ scale: 1, opacity: 1, rotate: 0 }}
            exit={{ scale: 0.9, opacity: 0, rotate: 2 }}
            className="w-full max-w-3xl max-h-[80vh] flex flex-col"
            onClick={(e) => e.stopPropagation()}
          >
            <SketchCard shadow rotate className="bg-surface relative flex flex-col overflow-hidden">
              <button 
                className="absolute top-4 right-4 w-10 h-10 sketch-border-thin hover:rotate-12 transition-transform flex items-center justify-center bg-surface z-10"
                onClick={onClose}
              >
                <span className="material-symbols-outlined">close</span>
              </button>

              <div className="mb-6">
                <h2 className="font-headline-md text-2xl flex items-center gap-3">
                  <span className="material-symbols-outlined text-primary-container">terminal</span>
                  {title}
                </h2>
                <p className="font-body-sm italic text-on-surface-variant">Step-by-step trace of the routing algorithm decision process.</p>
              </div>

              <div className="flex-1 overflow-y-auto bg-surface-container-low p-4 sketch-border-thin font-mono text-xs space-y-2 custom-scrollbar">
                {logs.map((log, index) => {
                  const isError = log.includes('[ERROR]') || log.includes('FAILURE') || log.includes('CRITICAL');
                  const isSuccess = log.includes('[SUCCESS]') || log.includes('MATCH');
                  const isSystem = log.includes('[SYSTEM]');
                  const isPhase = log.includes('PHASE');
                  
                  return (
                    <div 
                      key={index} 
                      className={`py-1 border-b border-primary-container/5 last:border-0 ${
                        isError ? 'text-error font-bold' : 
                        isSuccess ? 'text-primary font-bold' : 
                        isSystem ? 'text-secondary-fixed-dim italic' :
                        isPhase ? 'bg-primary-container/10 text-primary-container p-2 mt-4 font-black text-[10px] uppercase tracking-widest' :
                        'text-on-surface-variant'
                      }`}
                    >
                      {log}
                    </div>
                  );
                })}
              </div>

              <div className="mt-6 flex justify-end">
                <SketchButton onClick={onClose}>Close Logs</SketchButton>
              </div>
            </SketchCard>
          </motion.div>
        </div>
      )}
    </AnimatePresence>
  );
};
