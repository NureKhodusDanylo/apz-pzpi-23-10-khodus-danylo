import React, { ReactNode } from 'react';

interface SketchCardProps {
  children: ReactNode;
  className?: string;
  rotate?: boolean;
  shadow?: boolean;
}

export const SketchCard = ({ children, className = '', rotate = false, shadow = true }: SketchCardProps) => {
  return (
    <div className={`bg-surface-container p-6 sketch-border ${shadow ? 'sketch-shadow' : ''} ${rotate ? 'transform -rotate-1' : ''} ${className}`}>
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
}

export const SketchButton = ({ 
  children, 
  onClick, 
  type = 'button', 
  className = '', 
  variant = 'primary',
  icon 
}: SketchButtonProps) => {
  const variants = {
    primary: 'bg-primary-container text-surface hover:bg-primary-container/90',
    secondary: 'bg-surface text-primary-container hover:bg-surface-variant',
    error: 'bg-error-container text-on-error-container hover:bg-error-container/90 border-error'
  };

  return (
    <button
      type={type}
      onClick={onClick}
      className={`py-3 px-6 sketch-border transition-colors flex items-center justify-center gap-2 font-label-md ${variants[variant]} ${className}`}
    >
      {icon && <span className="material-symbols-outlined">{icon}</span>}
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
    <div className="flex flex-col gap-1 w-full">
      {label && <label className="font-label-md text-sm text-primary-container/70">{label}</label>}
      <input
        {...props}
        className={`w-full p-3 bg-surface sketch-border-thin focus:sketch-border focus:outline-none transition-all ${error ? 'border-error' : ''}`}
      />
      {error && <span className="text-xs text-error font-label-md">{error}</span>}
    </div>
  );
};

export const SketchDivider = ({ className = '' }: { className?: string }) => (
  <div className={`sketch-divider opacity-50 my-4 ${className}`} />
);

interface SketchAvatarProps {
  src?: string | null;
  alt?: string;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  className?: string;
  rotate?: number;
}

export const SketchAvatar = ({ 
  src, 
  alt, 
  size = 'md', 
  className = '',
  rotate = 0,
  children
}: SketchAvatarProps & { children?: ReactNode }) => {
  const [imgSrc, setImgSrc] = React.useState<string | null>(null);
  const [imgError, setImgError] = React.useState(false);

  React.useEffect(() => {
    if (!src) {
      setImgSrc(null);
      setImgError(false);
      return;
    }

    // If it's a base64 preview or already a blob/public asset, use it directly
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
        console.error('Avatar load error:', err);
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
      className={`bg-surface-variant sketch-border flex items-center justify-center font-black text-primary-container overflow-hidden shrink-0 relative ${sizes[size]} ${className}`}
      style={{ transform: rotate ? `rotate(${rotate}deg)` : undefined }}
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
