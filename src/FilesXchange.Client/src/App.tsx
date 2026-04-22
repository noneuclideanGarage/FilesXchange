import { Navigate, Route, Routes } from 'react-router-dom'
import './App.css'
import { DownloadPage } from './pages/DownloadPage'
import { UploadPage } from './pages/UploadPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<UploadPage />} />
      <Route path="/download/:token" element={<DownloadPage />} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
