import React, { ReactNode } from 'react';

interface SketchCardProps {
  children: ReactNode;
  className?: string;
  rotate?: boolean;
  shadow?: boolean;
}

export const SketchCard = ({ children, className = '', rotate = false, shadow = true }: SketchCardProps) => {
  return (
    <div className={`bg-surface p-6 rounded-2xl border border-outline/10 ${shadow ? 'premium-shadow' : ''} transition-all duration-300 hover:border-outline/20 ${className}`}>
      {children}
    </div>
  );
};

interface SketchButtonProps {
  children: ReactNode;
  onClick?: () => void;
  type?: 'button' | 'submit' | 'reset';
  className?: string;
  variant?: 'primary' | 'secondary' | 'error';
  icon?: string;
  isLoading?: boolean;
}

export const SketchButton = ({ 
  children, 
  onClick, 
  type = 'button', 
  className = '', 
  variant = 'primary',
  icon,
  isLoading
}: SketchButtonProps) => {
  const variants = {
    primary: 'bg-primary text-on-primary hover:bg-primary/90 shadow-md hover:shadow-lg',
    secondary: 'bg-surface-container-low text-primary hover:bg-surface-container-high border border-outline/10',
    error: 'bg-error text-on-error hover:bg-error/90 shadow-md'
  };

  return (
    <button
      type={type}
      onClick={onClick}
      disabled={isLoading}
      className={`py-3 px-6 rounded-xl transition-all duration-200 flex items-center justify-center gap-2 font-label-md active:scale-95 disabled:opacity-50 disabled:pointer-events-none ${variants[variant]} ${className}`}
    >
      {isLoading ? (
        <span className="material-symbols-outlined animate-spin">sync</span>
      ) : icon && (
        <span className="material-symbols-outlined">{icon}</span>
      )}
      {children}
    </button>
  );
};

interface SketchInputProps {
  label?: string;
  error?: string;
  [key: string]: any;
}

export const SketchInput = ({ label, error, ...props }: SketchInputProps) => {
  return (
    <div className="flex flex-col gap-1.5 w-full group">
      {label && <label className="font-label-md text-sm text-on-surface-variant transition-colors group-focus-within:text-primary">{label}</label>}
      <input
        {...props}
        className={`w-full p-3 bg-surface-container-lowest border border-outline/20 rounded-xl focus:border-primary focus:ring-2 focus:ring-primary/10 outline-none transition-all ${error ? 'border-error' : ''}`}
      />
      {error && <span className="text-xs text-error font-medium pl-1">{error}</span>}
    </div>
  );
};

export const SketchDivider = ({ className = '' }: { className?: string }) => (
  <div className={`h-[1px] bg-outline/10 w-full my-6 ${className}`} />
);

interface SketchAvatarProps {
  src?: string | null;
  alt?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
  rotate?: number;
  children?: ReactNode;
}

export const SketchAvatar = ({ 
  src, 
  alt, 
  size = 'md', 
  className = '',
  rotate = 0,
  children
}: SketchAvatarProps) => {
  const [imgSrc, setImgSrc] = React.useState<string | null>(null);
  const [imgError, setImgError] = React.useState(false);

  React.useEffect(() => {
    if (!src) {
      setImgSrc(null);
      setImgError(false);
      return;
    }

    if (src.startsWith('data:') || src.startsWith('blob:') || !src.includes('/api/')) {
      setImgSrc(src);
      setImgError(false);
      return;
    }

    let objectUrl: string | null = null;

    const fetchImage = async () => {
      try {
        const token = localStorage.getItem('token');
        const response = await fetch(src, {
          headers: {
            'Authorization': `Bearer ${token}`,
            'bypass-tunnel-reminder': 'true'
          }
        });
        
        if (!response.ok) throw new Error('Failed to load image');
        
        const blob = await response.blob();
        objectUrl = URL.createObjectURL(blob);
        setImgSrc(objectUrl);
        setImgError(false);
      } catch (err) {
        setImgError(true);
      }
    };

    fetchImage();

    return () => {
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [src]);

  const sizes = {
    sm: 'w-10 h-10 text-sm',
    md: 'w-20 h-20 text-2xl',
    lg: 'w-32 h-32 text-4xl',
    xl: 'w-40 h-40 text-5xl',
  };

  const fallbackUrl = '/default-avatar.png';
  const finalSrc = (!imgError && imgSrc) ? imgSrc : fallbackUrl;

  return (
    <div 
      className={`bg-surface-container-highest rounded-2xl flex items-center justify-center font-black text-primary overflow-hidden shrink-0 relative shadow-inner border border-outline/10 ${sizes[size]} ${className}`}
    >
      <img 
        src={finalSrc} 
        alt={alt || 'Avatar'} 
        className="w-full h-full object-cover"
        onError={() => setImgError(true)}
      />
      {children}
    </div>
  );
};
