import { Result, Button } from 'antd';
import { useNavigate } from 'react-router-dom';

export default function NotFoundPage() {
  const navigate = useNavigate();
  return (
    <Result
      status="404"
      title="404"
      subTitle="找不到您要的頁面"
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          回首頁
        </Button>
      }
    />
  );
}
