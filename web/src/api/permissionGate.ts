import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { rolesApi } from './roles';
import type { PermissionCode } from '../lib/permissions';

export function useEffectivePermissions() {
  const { isAuthenticated } = useAuth();
  return useQuery({
    queryKey: ['roles', 'effective'],
    queryFn: rolesApi.getEffectivePermissions,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });
}

export function useHasPermission(code: PermissionCode): boolean {
  const { data: permissions } = useEffectivePermissions();
  return permissions?.includes(code) ?? false;
}

export function useHasAnyPermission(codes: PermissionCode[]): boolean {
  const { data: permissions } = useEffectivePermissions();
  if (!permissions) return false;
  return codes.some((c) => permissions.includes(c));
}

export function useIsAdmin(): boolean {
  const { user } = useAuth();
  const { data: permissions } = useEffectivePermissions();
  if (permissions?.includes('system.admin')) return true;
  return user?.roles?.some((r) => r.toLowerCase() === 'admin') ?? false;
}
