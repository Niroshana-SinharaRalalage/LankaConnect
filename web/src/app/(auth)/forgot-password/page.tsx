import { ForgotPasswordForm } from '@/presentation/components/features/auth/ForgotPasswordForm';
import { Logo } from '@/presentation/components/atoms/Logo';

export default function ForgotPasswordPage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gradient-sri-lanka p-4">
      <div className="mb-8">
        <Logo size="lg" showText />
      </div>
      <ForgotPasswordForm />
    </div>
  );
}
