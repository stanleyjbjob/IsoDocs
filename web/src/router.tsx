import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './contexts/AuthContext';
import HomePage from './pages/HomePage';
import LoginPage from './pages/LoginPage';
import NotFoundPage from './pages/NotFoundPage';
import AdminLayout from './pages/admin/AdminLayout';
import RolesPage from './pages/admin/RolesPage';
import UsersPage from './pages/admin/UsersPage';
import FieldDefinitionsPage from './pages/admin/FieldDefinitionsPage';
import WorkflowTemplatesPage from './pages/admin/WorkflowTemplatesPage';
import CaseListPage from './pages/cases/CaseListPage';
import CaseCreatePage from './pages/cases/CaseCreatePage';
import CaseDetailPage from './pages/cases/CaseDetailPage';
import type { ReactNode } from 'react';

/**
 * 受保護路由：未登入時導向 /login。
 * MSAL 狀態由 issue #34 [2.1.2] 补上。
 */
function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return null;
  }
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <HomePage />
          </RequireAuth>
        }
      />
      {/* 案件模組 (issue #21 [5.5])：發起 / 清單 / 詳情 */}
      <Route
        path="/cases"
        element={
          <RequireAuth>
            <CaseListPage />
          </RequireAuth>
        }
      />
      <Route
        path="/cases/new"
        element={
          <RequireAuth>
            <CaseCreatePage />
          </RequireAuth>
        }
      />
      <Route
        path="/cases/:id"
        element={
          <RequireAuth>
            <CaseDetailPage />
          </RequireAuth>
        }
      />
      {/* 管理者區 (issue #8 [2.2.2])。AdminLayout 內部會再進一步檢查 isAdmin，非 admin 會看到 403。 */}
      <Route
        path="/admin"
        element={
          <RequireAuth>
            <AdminLayout />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/admin/roles" replace />} />
        <Route path="roles" element={<RolesPage />} />
        <Route path="users" element={<UsersPage />} />
        {/* 自訂欄位管理 (issue #11 [3.1.2]) */}
        <Route path="fields" element={<FieldDefinitionsPage />} />
        {/* 流程範本設計器 (issue #13 [3.2.2]) */}
        <Route path="workflow-templates" element={<WorkflowTemplatesPage />} />
      </Route>
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
