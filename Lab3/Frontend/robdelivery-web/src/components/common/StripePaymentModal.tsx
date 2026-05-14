import React, { useState } from 'react';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, CardElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { SketchButton, SketchCard } from './SketchComponents';

// The user's Stripe publishable key
const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PUBLIC_KEY || '');

const CARD_ELEMENT_OPTIONS = {
  style: {
    base: {
      color: '#32325d',
      fontFamily: '"Helvetica Neue", Helvetica, sans-serif',
      fontSmoothing: 'antialiased',
      fontSize: '16px',
      '::placeholder': {
        color: '#aab7c4'
      }
    },
    invalid: {
      color: '#fa755a',
      iconColor: '#fa755a'
    }
  }
};

interface PaymentFormProps {
  onSuccess: (paymentMethodId: string) => void;
  onCancel: () => void;
  isLoading: boolean;
  amountText?: string;
}

const PaymentForm = ({ onSuccess, onCancel, isLoading, amountText }: PaymentFormProps) => {
  const stripe = useStripe();
  const elements = useElements();
  const [error, setError] = useState<string | null>(null);
  const [processing, setProcessing] = useState(false);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    if (!stripe || !elements) {
      return;
    }

    setProcessing(true);
    setError(null);

    const cardElement = elements.getElement(CardElement);
    if (!cardElement) return;

    // We only create a PaymentMethod to send to the backend
    const { error: stripeError, paymentMethod } = await stripe.createPaymentMethod({
      type: 'card',
      card: cardElement,
    });

    if (stripeError) {
      setError(stripeError.message || 'An error occurred with your card details.');
      setProcessing(false);
    } else {
      // Pass the PM ID back up to trigger the API mutation
      onSuccess(paymentMethod.id);
      // Let the parent component unmount or redirect us. 
      // We don't set processing to false yet, since the parent API call is loading.
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <div>
        <label className="block text-sm font-label-md mb-2">Card Details</label>
        <div className="p-3 bg-background border-2 border-text rounded-md shadow-[2px_2px_0px_#272727] -rotate-1">
          <CardElement options={CARD_ELEMENT_OPTIONS} />
        </div>
      </div>
      
      {error && (
        <div className="text-red-500 text-sm font-label-md">
          {error}
        </div>
      )}

      <div className="flex gap-4 pt-2">
        <SketchButton
          type="button"
          onClick={onCancel}
          disabled={processing || isLoading}
          className="flex-1 py-3 bg-background text-text border-2 border-text hover:bg-gray-100"
        >
          Cancel
        </SketchButton>
        <SketchButton
          type="submit"
          disabled={!stripe || processing || isLoading}
          icon={(processing || isLoading) ? 'sync' : 'payment'}
          className="flex-1 py-3"
        >
          {(processing || isLoading) ? 'Processing...' : `Pay ${amountText || ''}`}
        </SketchButton>
      </div>
    </form>
  );
};

interface StripePaymentModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: (paymentMethodId: string) => void;
  isLoading: boolean;
  title?: string;
  amountText?: string;
}

export const StripePaymentModal = ({
  isOpen,
  onClose,
  onSuccess,
  isLoading,
  title = "Complete Payment",
  amountText
}: StripePaymentModalProps) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm">
      <div className="max-w-md w-full">
        <SketchCard className="p-6 relative rotate-1">
          <button 
            onClick={onClose}
            disabled={isLoading}
            className="absolute top-4 right-4 text-text/50 hover:text-text transition-colors"
          >
            <span className="material-symbols-outlined">close</span>
          </button>
          
          <h2 className="text-2xl font-sketch-bold mb-6">{title}</h2>
          
          <Elements stripe={stripePromise}>
            <PaymentForm 
              onSuccess={onSuccess} 
              onCancel={onClose} 
              isLoading={isLoading} 
              amountText={amountText}
            />
          </Elements>
        </SketchCard>
      </div>
    </div>
  );
};
