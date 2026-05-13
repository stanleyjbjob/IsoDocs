import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { setCustomVersionNumber } from '../../api/casesApi';

const VERSION_PATTERN = /^[A-Za-z0-9]([A-Za-z0-9.\-_]{0,19})?$/;

export default function CaseVersionNumberPage() {
  const { id: caseId = '' } = useParams<{ id: string }>();
  const [value, setValue] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [savedVersion, setSavedVersion] = useState<string | null>(null);

  const validate = (v: string): string | null => {
    if (!v.trim()) return '版號不可空白。';
    if (!VERSION_PATTERN.test(v)) return '僅允許英數字、點、連字號、底線，最多 20 字元。';
    return null;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const validationError = validate(value);
    if (validationError) {
      setError(validationError);
      return;
    }
    setError(null);
    setSaving(true);
    try {
      const result = await setCustomVersionNumber(caseId, { versionNumber: value });
      setSavedVersion(result.versionNumber);
    } catch {
      setError('儲存失敗，請稍後再試。');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div style={{ maxWidth: 480, margin: '2rem auto', padding: '1.5rem', border: '1px solid #e0e0e0', borderRadius: 8 }}>
      <h2 style={{ marginBottom: '1rem', fontSize: '1.125rem', fontWeight: 600 }}>文件版號設定</h2>
      {savedVersion && (
        <div style={{ marginBottom: '1rem', padding: '.75rem 1rem', background: '#f0fff4', border: '1px solid #9ae6b4', borderRadius: 4 }}>
          <span style={{ color: '#276749' }}>✓ 版號已設定為 <strong>{savedVersion}</strong></span>
        </div>
      )}
      <form onSubmit={handleSubmit}>
        <label htmlFor="version-input" style={{ display: 'block', marginBottom: '.5rem', fontWeight: 500 }}>
          版號
        </label>
        <input
          id="version-input"
          type="text"
          value={value}
          onChange={e => setValue(e.target.value)}
          placeholder="例：v1.0、REV-01、A"
          maxLength={20}
          style={{
            display: 'block',
            width: '100%',
            padding: '.5rem .75rem',
            border: error ? '1px solid #e53e3e' : '1px solid #ccc',
            borderRadius: 4,
            fontSize: '1rem',
            boxSizing: 'border-box' as const,
          }}
        />
        {error && <p style={{ color: '#e53e3e', marginTop: '.25rem', fontSize: '.875rem' }}>{error}</p>}
        <p style={{ color: '#718096', fontSize: '.75rem', marginTop: '.25rem' }}>
          英數字、點（.）、連字號（-）、底線（_），最多 20 字元。
        </p>
        <button
          type="submit"
          disabled={saving}
          style={{
            marginTop: '1rem',
            padding: '.5rem 1.25rem',
            background: '#3182ce',
            color: '#fff',
            border: 'none',
            borderRadius: 4,
            fontSize: '1rem',
            cursor: saving ? 'not-allowed' : 'pointer',
            opacity: saving ? 0.7 : 1,
          }}
        >
          {saving ? '儲存中…' : '儲存版號'}
        </button>
      </form>
    </div>
  );
}
