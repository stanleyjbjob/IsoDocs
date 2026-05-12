import { useState, useEffect } from 'react';
import { delegationsApi } from '../api/delegations';
import type { DelegationDto, CreateDelegationRequest } from '../api/delegations';

type FormState = {
  delegateUserId: string;
  startAt: string;
  endAt: string;
  note: string;
};

const emptyForm: FormState = {
  delegateUserId: '',
  startAt: '',
  endAt: '',
  note: '',
};

export default function DelegationPage() {
  const [delegations, setDelegations] = useState<DelegationDto[]>([]);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [loading, setLoading] = useState(false);
  const [loadingList, setLoadingList] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const fetchDelegations = async () => {
    setLoadingList(true);
    setError(null);
    try {
      const res = await delegationsApi.list();
      setDelegations(res.data);
    } catch {
      setError('無法載入代理設定清單，請稍後再試。');
    } finally {
      setLoadingList(false);
    }
  };

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError(null);
    setSuccess(null);

    const payload: CreateDelegationRequest = {
      delegateUserId: form.delegateUserId,
      startAt: new Date(form.startAt).toISOString(),
      endAt: new Date(form.endAt).toISOString(),
      note: form.note || undefined,
    };

    try {
      await delegationsApi.create(payload);
      setSuccess('代理設定已建立。');
      setForm(emptyForm);
      await fetchDelegations();
    } catch {
      setError('建立代理設定失敗，請確認輸入資料正確。');
    } finally {
      setLoading(false);
    }
  };

  const handleRevoke = async (id: string) => {
    if (!confirm('確定要撤銷此代理設定？')) return;
    setError(null);
    setSuccess(null);
    try {
      await delegationsApi.revoke(id);
      setSuccess('代理設定已撤銷。');
      setDelegations((prev) => prev.filter((d) => d.id !== id));
    } catch {
      setError('撤銷代理設定失敗。');
    }
  };

  useEffect(() => {
    fetchDelegations();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const formatDate = (iso: string) =>
    new Date(iso).toLocaleString('zh-TW', { dateStyle: 'medium', timeStyle: 'short' });

  return (
    <div style={{ maxWidth: 800, margin: '0 auto', padding: '2rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '1.5rem' }}>
        代理設定
      </h1>

      {error && (
        <div
          role="alert"
          style={{
            padding: '0.75rem 1rem',
            marginBottom: '1rem',
            background: '#fee2e2',
            color: '#991b1b',
            borderRadius: '0.375rem',
          }}
        >
          {error}
        </div>
      )}
      {success && (
        <div
          role="status"
          style={{
            padding: '0.75rem 1rem',
            marginBottom: '1rem',
            background: '#dcfce7',
            color: '#166534',
            borderRadius: '0.375rem',
          }}
        >
          {success}
        </div>
      )}

      {/* 建立代理設定表單 */}
      <section
        style={{
          padding: '1.5rem',
          border: '1px solid #e5e7eb',
          borderRadius: '0.5rem',
          marginBottom: '2rem',
        }}
      >
        <h2 style={{ fontSize: '1.125rem', fontWeight: '600', marginBottom: '1rem' }}>
          新增代理設定
        </h2>
        <form onSubmit={handleCreate}>
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: '500' }}>
              代理人 ID（User ID）
            </label>
            <input
              type="text"
              value={form.delegateUserId}
              onChange={(e) => setForm({ ...form, delegateUserId: e.target.value })}
              required
              placeholder="00000000-0000-0000-0000-000000000000"
              style={{
                width: '100%',
                padding: '0.5rem 0.75rem',
                border: '1px solid #d1d5db',
                borderRadius: '0.375rem',
                boxSizing: 'border-box',
              }}
            />
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1rem' }}>
            <div>
              <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: '500' }}>
                代理開始時間
              </label>
              <input
                type="datetime-local"
                value={form.startAt}
                onChange={(e) => setForm({ ...form, startAt: e.target.value })}
                required
                style={{
                  width: '100%',
                  padding: '0.5rem 0.75rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '0.375rem',
                  boxSizing: 'border-box',
                }}
              />
            </div>
            <div>
              <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: '500' }}>
                代理結束時間
              </label>
              <input
                type="datetime-local"
                value={form.endAt}
                onChange={(e) => setForm({ ...form, endAt: e.target.value })}
                required
                style={{
                  width: '100%',
                  padding: '0.5rem 0.75rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '0.375rem',
                  boxSizing: 'border-box',
                }}
              />
            </div>
          </div>

          <div style={{ marginBottom: '1rem' }}>
            <label style={{ display: 'block', marginBottom: '0.25rem', fontWeight: '500' }}>
              備註（選填）
            </label>
            <input
              type="text"
              value={form.note}
              onChange={(e) => setForm({ ...form, note: e.target.value })}
              maxLength={512}
              placeholder="例如：出差、休假"
              style={{
                width: '100%',
                padding: '0.5rem 0.75rem',
                border: '1px solid #d1d5db',
                borderRadius: '0.375rem',
                boxSizing: 'border-box',
              }}
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            style={{
              padding: '0.5rem 1.5rem',
              background: loading ? '#9ca3af' : '#2563eb',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: loading ? 'not-allowed' : 'pointer',
              fontWeight: '500',
            }}
          >
            {loading ? '建立中…' : '建立代理設定'}
          </button>
        </form>
      </section>

      {/* 代理設定清單 */}
      <section>
        <h2 style={{ fontSize: '1.125rem', fontWeight: '600', marginBottom: '1rem' }}>
          我的代理設定
        </h2>
        {loadingList ? (
          <p style={{ color: '#6b7280' }}>載入中…</p>
        ) : delegations.length === 0 ? (
          <p style={{ color: '#6b7280' }}>目前沒有代理設定。</p>
        ) : (
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              fontSize: '0.875rem',
            }}
          >
            <thead>
              <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
                <th style={{ padding: '0.5rem 0.75rem' }}>代理人 ID</th>
                <th style={{ padding: '0.5rem 0.75rem' }}>開始時間</th>
                <th style={{ padding: '0.5rem 0.75rem' }}>結束時間</th>
                <th style={{ padding: '0.5rem 0.75rem' }}>備註</th>
                <th style={{ padding: '0.5rem 0.75rem' }}>狀態</th>
                <th style={{ padding: '0.5rem 0.75rem' }}>操作</th>
              </tr>
            </thead>
            <tbody>
              {delegations.map((d) => (
                <tr key={d.id} style={{ borderBottom: '1px solid #f3f4f6' }}>
                  <td
                    style={{
                      padding: '0.5rem 0.75rem',
                      fontFamily: 'monospace',
                      fontSize: '0.75rem',
                    }}
                  >
                    {d.delegateUserId.slice(0, 8)}…
                  </td>
                  <td style={{ padding: '0.5rem 0.75rem' }}>{formatDate(d.startAt)}</td>
                  <td style={{ padding: '0.5rem 0.75rem' }}>{formatDate(d.endAt)}</td>
                  <td style={{ padding: '0.5rem 0.75rem' }}>{d.note ?? '—'}</td>
                  <td style={{ padding: '0.5rem 0.75rem' }}>
                    {d.isRevoked ? (
                      <span style={{ color: '#6b7280' }}>已撤銷</span>
                    ) : d.isCurrentlyEffective ? (
                      <span style={{ color: '#16a34a', fontWeight: '500' }}>生效中</span>
                    ) : (
                      <span style={{ color: '#ca8a04' }}>未生效</span>
                    )}
                  </td>
                  <td style={{ padding: '0.5rem 0.75rem' }}>
                    {!d.isRevoked && (
                      <button
                        onClick={() => handleRevoke(d.id)}
                        style={{
                          padding: '0.25rem 0.75rem',
                          background: '#fee2e2',
                          color: '#991b1b',
                          border: '1px solid #fca5a5',
                          borderRadius: '0.25rem',
                          cursor: 'pointer',
                          fontSize: '0.75rem',
                        }}
                      >
                        撤銷
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}
