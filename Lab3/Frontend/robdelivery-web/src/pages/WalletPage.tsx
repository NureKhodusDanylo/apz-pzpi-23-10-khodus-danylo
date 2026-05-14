import React from 'react';
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import Layout from '../components/Layout';
import { walletAPI } from '../lib/api';
import { useAuthStore } from '../store/authStore';
import { SketchCard, SketchButton, SketchInput } from '../components/common/SketchComponents';
import toast from 'react-hot-toast';

const WalletPage = () => {
  const user = useAuthStore((state) => state.user);
  const queryClient = useQueryClient();

  const { data: walletData, isLoading } = useQuery({
    queryKey: ['wallet-balance'],
    queryFn: async () => {
      const response = await walletAPI.getBalance();
      return response.data;
    },
  });

  const withdrawMutation = useMutation({
    mutationFn: walletAPI.withdraw,
    onSuccess: (response) => {
      queryClient.invalidateQueries({ queryKey: ['wallet-balance'] });
      toast.success(`Successfully withdrew $${response.data.withdrawnAmount?.toFixed(2) || '0.00'}`);
    },
    onError: (error: any) => {
      toast.error(error.response?.data?.message || 'Failed to process withdrawal');
    },
  });

  const handleWithdraw = (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!walletData || walletData.balance <= 0) {
      toast.error('No funds available to withdraw');
      return;
    }

    if (window.confirm(`Are you sure you want to withdraw all your funds ($${walletData.balance.toFixed(2)})?`)) {
      withdrawMutation.mutate(undefined as any);
    }
  };

  return (
    <Layout>
      <div className="flex flex-col gap-8 max-w-4xl mx-auto">
        <section className="bg-surface-container p-6 sketch-border sketch-shadow relative transform -rotate-1">
          <h1 className="font-headline-lg text-headline-lg mb-2">My Wallet</h1>
          <p className="font-body-lg text-on-surface-variant italic">Manage your virtual earnings and withdraw to your bank.</p>
        </section>

        {isLoading ? (
          <div className="text-center py-24">
            <span className="material-symbols-outlined animate-spin text-4xl">sync</span>
            <p className="mt-4 font-label-md">Loading ledger...</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <SketchCard className="bg-primary-container text-on-primary-container text-center flex flex-col items-center justify-center py-12">
              <span className="material-symbols-outlined text-6xl mb-4">account_balance_wallet</span>
              <h2 className="font-label-md font-black uppercase tracking-widest text-primary/60 mb-2">Available Balance</h2>
              <div className="text-6xl font-black mb-2">${walletData?.balance?.toFixed(2) || '0.00'}</div>
              <p className="text-xs font-bold italic opacity-70">Funds are safe in the RobDelivery Ledger</p>
            </SketchCard>

            <SketchCard rotate>
              <h3 className="font-headline-md text-2xl mb-6">Withdraw Funds</h3>
              <form onSubmit={handleWithdraw} className="space-y-6">
                <div className="bg-surface-container-low p-4 sketch-border-thin flex items-start gap-3">
                  <span className="material-symbols-outlined text-primary">info</span>
                  <div className="text-sm">
                    <p className="font-bold mb-1">Standard Payout</p>
                    <p className="text-on-surface-variant">Your entire balance will be transferred to your connected bank account in 2-3 business days.</p>
                  </div>
                </div>

                <SketchButton 
                  type="submit" 
                  className="w-full text-lg py-3"
                  disabled={withdrawMutation.isPending || !walletData || walletData.balance <= 0}
                  icon={withdrawMutation.isPending ? 'sync' : 'payments'}
                >
                  {withdrawMutation.isPending ? 'Processing...' : `Withdraw All ($${walletData?.balance?.toFixed(2) || '0.00'})`}
                </SketchButton>
              </form>
            </SketchCard>
          </div>
        )}
      </div>
    </Layout>
  );
};

export default WalletPage;
