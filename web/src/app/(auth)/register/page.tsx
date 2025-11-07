import { RegisterForm } from '@/presentation/components/features/auth/RegisterForm';
import { Logo } from '@/presentation/components/atoms/Logo';

export default function RegisterPage() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-gradient-sri-lanka p-4">
      <div className="mb-8">
        <Logo size="lg" showText />
      </div>
      <RegisterForm />
    </div>
  );
}
