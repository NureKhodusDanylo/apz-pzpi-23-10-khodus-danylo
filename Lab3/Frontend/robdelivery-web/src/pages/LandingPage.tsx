import React from 'react';
import { motion } from 'framer-motion';
import { useNavigate, Link } from 'react-router-dom';
import { SketchCard, SketchButton, SketchDivider } from '../components/common/SketchComponents';

const LandingPage = () => {
  const navigate = useNavigate();

  const features = [
    {
      icon: 'smart_toy',
      title: 'Autonomous Robots',
      description: 'Advanced ground and aerial robots for fast delivery',
    },
    {
      icon: 'bolt',
      title: 'Lightning Fast',
      description: 'Get your packages delivered in record time',
    },
    {
      icon: 'shield',
      title: 'Secure & Safe',
      description: 'End-to-end encryption and secure delivery',
    },
    {
      icon: 'location_on',
      title: 'Real-Time Tracking',
      description: 'Track your delivery every step of the way',
    },
  ];

  return (
    <div className="min-h-screen bg-background selection:bg-tertiary-fixed-dim selection:text-tertiary-container overflow-x-hidden">
      {/* Navigation */}
      <nav className="fixed top-0 left-0 w-full z-50 bg-background/80 backdrop-blur-sm border-b-2 border-primary-container/20">
        <div className="max-w-[1200px] mx-auto px-6 py-4 flex justify-between items-center">
          <div className="flex items-center gap-3">
             <span className="material-symbols-outlined text-3xl text-primary-container">smart_toy</span>
             <span className="font-headline-md text-xl font-black text-primary-container">RobDelivery</span>
          </div>
          <div className="flex gap-4">
            <button onClick={() => navigate('/login')} className="font-label-md text-sm text-primary-container/70 hover:text-primary-container transition-colors">Login</button>
            <SketchButton onClick={() => navigate('/register')} className="py-2 px-4 text-sm">Get Started</SketchButton>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="pt-32 pb-20 px-6">
        <div className="max-w-[1200px] mx-auto grid grid-cols-1 lg:grid-cols-2 gap-12 items-center">
          <motion.div
            initial={{ opacity: 0, x: -50 }}
            animate={{ opacity: 1, x: 0 }}
            transition={{ duration: 0.8 }}
          >
            <h1 className="font-headline-lg text-5xl md:text-7xl mb-6 leading-tight">
              Future of Delivery <br />
              <span className="bg-tertiary-fixed-dim/20 px-2 rounded">Powered by Robots</span>
            </h1>
            <p className="font-body-lg text-body-lg text-on-surface-variant mb-10 max-w-lg">
              Experience autonomous last-mile delivery with our advanced robotic fleet. 
              Fast, secure, and eco-friendly. Built with graphite precision.
            </p>
            <div className="flex flex-wrap gap-6">
              <SketchButton onClick={() => navigate('/register')} icon="rocket_launch" className="px-8 py-4 text-lg">
                Start Delivery
              </SketchButton>
              <button className="font-label-md text-primary-container/60 hover:text-primary-container flex items-center gap-2 group transition-all">
                Learn More <span className="material-symbols-outlined group-hover:translate-x-2 transition-transform">arrow_right_alt</span>
              </button>
            </div>
          </motion.div>

          <motion.div
            className="relative"
            initial={{ opacity: 0, scale: 0.8, rotate: 5 }}
            animate={{ opacity: 1, scale: 1, rotate: 0 }}
            transition={{ delay: 0.2, duration: 1 }}
          >
            <SketchCard rotate shadow className="bg-surface-container flex items-center justify-center p-12 min-h-[400px]">
               <motion.div
                  animate={{
                    y: [0, -20, 0],
                    rotate: [0, 2, -2, 0]
                  }}
                  transition={{
                    duration: 5,
                    repeat: Infinity,
                    ease: 'easeInOut',
                  }}
                  className="flex items-center justify-center"
               >
                  <span className="material-symbols-outlined text-[200px] text-primary-container opacity-90" style={{ fontVariationSettings: "'FILL' 1" }}>
                    smart_toy
                  </span>
               </motion.div>
               <div className="absolute -bottom-6 -left-6 bg-tertiary-fixed-dim p-4 sketch-border transform -rotate-12">
                  <p className="font-label-md font-black text-tertiary-container">99.9% SUCCESS RATE</p>
               </div>
            </SketchCard>
          </motion.div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-20 px-6 bg-surface-container-low">
        <div className="max-w-[1200px] mx-auto">
          <div className="text-center mb-16">
            <h2 className="font-headline-md text-headline-lg mb-4">Why RobDelivery?</h2>
            <p className="font-body-md text-on-surface-variant">Advanced technology meets exceptional service.</p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8">
            {features.map((feature, index) => (
              <SketchCard 
                key={index} 
                rotate={index % 2 === 1} 
                className="hover:bg-surface-container transition-colors group cursor-default"
              >
                <span className="material-symbols-outlined text-4xl mb-4 text-primary-container group-hover:rotate-12 transition-transform">
                  {feature.icon}
                </span>
                <h3 className="font-headline-md text-xl mb-2">{feature.title}</h3>
                <p className="font-body-md text-sm text-on-surface-variant">{feature.description}</p>
              </SketchCard>
            ))}
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="py-32 px-6">
        <div className="max-w-[800px] mx-auto text-center">
          <SketchCard rotate shadow className="bg-primary-container text-surface p-12">
            <h2 className="font-headline-lg text-4xl mb-6">Ready to Experience the Future?</h2>
            <p className="font-body-lg mb-10 opacity-80 italic">Join thousands of satisfied customers today. First delivery is on the house!</p>
            <SketchButton variant="secondary" onClick={() => navigate('/register')} icon="trending_up" className="mx-auto">
              Get Started Now
            </SketchButton>
          </SketchCard>
        </div>
      </section>

      {/* Footer */}
      <footer className="py-12 border-t-2 border-primary-container/10">
        <div className="max-w-[1200px] mx-auto px-6 flex flex-col md:flex-row justify-between items-center gap-8 text-center md:text-left">
          <div>
            <div className="flex items-center justify-center md:justify-start gap-3 mb-2">
               <span className="material-symbols-outlined text-2xl text-primary-container">smart_toy</span>
               <span className="font-headline-md text-lg font-black text-primary-container">RobDelivery</span>
            </div>
            <p className="font-body-md text-sm text-on-surface-variant">Revolutionizing last-mile delivery with autonomous robots.</p>
          </div>
          <p className="font-label-md text-xs text-on-surface-variant">© 2026 RobDelivery. All rights reserved.</p>
        </div>
      </footer>
    </div>
  );
};

export default LandingPage;
