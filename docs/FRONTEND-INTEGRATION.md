# ?? Integración Frontend con Sistema de Huellas Dactilares

## ?? Índice
1. [Arquitectura](#arquitectura)
2. [Configuración](#configuración)
3. [Servicio de Configuración](#servicio-de-configuración)
4. [Componentes UI](#componentes-ui)
5. [Gestión de Estado](#gestión-de-estado)
6. [SignalR - Notificaciones en Tiempo Real](#signalr-notificaciones-en-tiempo-real)
7. [Ejemplos Prácticos](#ejemplos-prácticos)

---

## ??? Arquitectura

### **Flujo de Comunicación**

```
????????????????????????????????????????????????????????????????
?                    FLUJO DE DATOS                             ?
????????????????????????????????????????????????????????????????

Frontend React/Vue/Angular
    ?
    ? 1. Usuario interactúa con UI
    ?
API Service (Axios/Fetch)
    ?
    ? 2. HTTP Request
    ?
Backend API (.NET)
    ?
    ? 3. Valida y procesa
    ?
Futronic Service API
    ?
    ? 4. Actualiza configuración
    ?
SignalR Hub
    ?
    ? 5. Notificaciones en tiempo real
    ?
Frontend (actualización automática)
```

---

## ?? Configuración

### **1. Variables de Entorno**

#### `.env` (Desarrollo)
```env
VITE_BACKEND_API_URL=http://localhost:5001/api
VITE_FUTRONIC_SERVICE_URL=http://localhost:5000/api/fingerprint
VITE_SIGNALR_HUB_URL=http://localhost:5000/hubs/fingerprint
VITE_API_TIMEOUT=30000
```

#### `.env.production`
```env
VITE_BACKEND_API_URL=https://api.yourcompany.com/api
VITE_FUTRONIC_SERVICE_URL=https://futronic.yourcompany.com/api/fingerprint
VITE_SIGNALR_HUB_URL=https://futronic.yourcompany.com/hubs/fingerprint
VITE_API_TIMEOUT=30000
```

### **2. Instalación de Dependencias**

#### React/Vite
```bash
npm install axios @microsoft/signalr
# O con yarn
yarn add axios @microsoft/signalr
```

#### Tipos TypeScript (opcional)
```bash
npm install --save-dev @types/node
```

---

## ?? Servicio de Configuración

### **TypeScript Service (`services/fingerprintConfig.service.ts`)**

```typescript
import axios, { AxiosInstance } from 'axios';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

// ============================================
// TIPOS Y INTERFACES
// ============================================

export interface FingerprintConfiguration {
  threshold: number;              // 0-100
  timeout: number;                // milisegundos
  maxRotation: number;            // 0-199
  detectFakeFinger: boolean;
  templatePath: string;
  overwriteExisting: boolean;
  captureMode?: string;
  showImage?: boolean;
  maxFramesInTemplate?: number;
  disableMIDT?: boolean;
  minQuality?: number;
}

export interface ConfigurationValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errorCode?: string;
}

export interface SignalRNotification {
  eventType: string;
  message: string;
  data: any;
  dni?: string;
  timestamp: string;
}

// ============================================
// SERVICIO DE CONFIGURACIÓN
// ============================================

class FingerprintConfigService {
  private backendApi: AxiosInstance;
  private futronicApi: AxiosInstance;
  private signalRConnection: HubConnection | null = null;
  private eventHandlers: Map<string, Function[]> = new Map();

  constructor() {
    // Cliente HTTP para tu backend
    this.backendApi = axios.create({
      baseURL: import.meta.env.VITE_BACKEND_API_URL,
      timeout: import.meta.env.VITE_API_TIMEOUT || 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Cliente HTTP directo para Futronic Service (opcional)
    this.futronicApi = axios.create({
      baseURL: import.meta.env.VITE_FUTRONIC_SERVICE_URL,
      timeout: import.meta.env.VITE_API_TIMEOUT || 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Interceptores para manejo de errores y autenticación
    this.setupInterceptors();
  }

  private setupInterceptors() {
    // Agregar token de autenticación a cada request
    this.backendApi.interceptors.request.use(
      (config) => {
        const token = localStorage.getItem('auth_token');
        if (token) {
          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
      (error) => Promise.reject(error)
    );

    // Manejo global de errores
    this.backendApi.interceptors.response.use(
      (response) => response,
      (error) => {
        if (error.response?.status === 401) {
          // Redirigir a login
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  // ============================================
  // MÉTODOS DE CONFIGURACIÓN
  // ============================================

  /**
   * Obtener configuración actual
   */
  async getConfiguration(): Promise<FingerprintConfiguration> {
    try {
      const response = await this.backendApi.get<ApiResponse<FingerprintConfiguration>>(
        '/fingerprint/config'
      );
      
      if (response.data.success) {
        return response.data.data;
      }
      
      throw new Error(response.data.message || 'Error al obtener configuración');
    } catch (error) {
      console.error('? Error al obtener configuración:', error);
      throw error;
    }
  }

  /**
   * Actualizar configuración completa
   */
  async updateConfiguration(
    config: FingerprintConfiguration
  ): Promise<FingerprintConfiguration> {
    try {
      console.log('?? Actualizando configuración...', config);
      
      const response = await this.backendApi.put<ApiResponse<FingerprintConfiguration>>(
        '/fingerprint/config',
        config
      );
      
      if (response.data.success) {
        console.log('? Configuración actualizada exitosamente');
        return response.data.data;
      }
      
      throw new Error(response.data.message || 'Error al actualizar configuración');
    } catch (error: any) {
      console.error('? Error al actualizar configuración:', error);
      
      if (error.response?.data?.message) {
        throw new Error(error.response.data.message);
      }
      
      throw error;
    }
  }

  /**
   * Actualizar campos específicos (PATCH)
   */
  async updatePartialConfiguration(
    updates: Partial<FingerprintConfiguration>
  ): Promise<FingerprintConfiguration> {
    try {
      console.log('?? Actualización parcial:', updates);
      
      const response = await this.backendApi.patch<ApiResponse<FingerprintConfiguration>>(
        '/fingerprint/config',
        updates
      );
      
      if (response.data.success) {
        console.log('? Actualización parcial exitosa');
        return response.data.data;
      }
      
      throw new Error(response.data.message || 'Error en actualización parcial');
    } catch (error) {
      console.error('? Error en actualización parcial:', error);
      throw error;
    }
  }

  /**
   * Validar configuración sin guardar
   */
  async validateConfiguration(
    config: FingerprintConfiguration
  ): Promise<ConfigurationValidationResult> {
    try {
      const response = await this.backendApi.post<ApiResponse<ConfigurationValidationResult>>(
        '/fingerprint/config/validate',
        config
      );
      
      if (response.data.success) {
        return response.data.data;
      }
      
      throw new Error('Error al validar configuración');
    } catch (error) {
      console.error('? Error al validar:', error);
      throw error;
    }
  }

  /**
   * Restaurar a valores por defecto
   */
  async resetConfiguration(): Promise<FingerprintConfiguration> {
    try {
      console.warn('?? Restaurando configuración a valores por defecto...');
      
      const response = await this.backendApi.post<ApiResponse<FingerprintConfiguration>>(
        '/fingerprint/config/reset'
      );
      
      if (response.data.success) {
        console.log('? Configuración restaurada');
        return response.data.data;
      }
      
      throw new Error('Error al restaurar configuración');
    } catch (error) {
      console.error('? Error al restaurar:', error);
      throw error;
    }
  }

  // ============================================
  // SIGNALR - NOTIFICACIONES EN TIEMPO REAL
  // ============================================

  /**
   * Conectar a SignalR Hub
   */
  async connectToSignalR(dni?: string): Promise<void> {
    if (this.signalRConnection) {
      console.warn('?? Ya existe una conexión SignalR activa');
      return;
    }

    const hubUrl = import.meta.env.VITE_SIGNALR_HUB_URL;
    
    this.signalRConnection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .configureLogging(LogLevel.Information)
      .withAutomaticReconnect()
      .build();

    // Configurar listeners
    this.signalRConnection.on('ReceiveProgress', (notification: SignalRNotification) => {
      console.log('?? Notificación SignalR recibida:', notification);
      this.emitEvent(notification.eventType, notification);
    });

    this.signalRConnection.onreconnecting(() => {
      console.log('?? Reconectando a SignalR...');
    });

    this.signalRConnection.onreconnected(() => {
      console.log('? Reconectado a SignalR');
    });

    this.signalRConnection.onclose(() => {
      console.log('? Conexión SignalR cerrada');
    });

    try {
      await this.signalRConnection.start();
      console.log('? Conectado a SignalR Hub');

      // Suscribirse a notificaciones de un DNI específico
      if (dni) {
        await this.subscribeToNotifications(dni);
      }
    } catch (error) {
      console.error('? Error al conectar a SignalR:', error);
      throw error;
    }
  }

  /**
   * Suscribirse a notificaciones de un DNI
   */
  async subscribeToNotifications(dni: string): Promise<void> {
    if (!this.signalRConnection) {
      throw new Error('No hay conexión SignalR activa');
    }

    try {
      await this.signalRConnection.invoke('SubscribeToNotifications', dni);
      console.log(`? Suscrito a notificaciones del DNI: ${dni}`);
    } catch (error) {
      console.error('? Error al suscribirse:', error);
      throw error;
    }
  }

  /**
   * Desuscribirse de notificaciones de un DNI
   */
  async unsubscribeFromNotifications(dni: string): Promise<void> {
    if (!this.signalRConnection) {
      return;
    }

    try {
      await this.signalRConnection.invoke('UnsubscribeFromNotifications', dni);
      console.log(`?? Desuscrito de notificaciones del DNI: ${dni}`);
    } catch (error) {
      console.error('? Error al desuscribirse:', error);
    }
  }

  /**
   * Desconectar de SignalR
   */
  async disconnectFromSignalR(): Promise<void> {
    if (this.signalRConnection) {
      await this.signalRConnection.stop();
      this.signalRConnection = null;
      console.log('?? Desconectado de SignalR');
    }
  }

  // ============================================
  // EVENT EMITTER
  // ============================================

  /**
   * Registrar listener para eventos SignalR
   */
  on(eventType: string, handler: Function): void {
    if (!this.eventHandlers.has(eventType)) {
      this.eventHandlers.set(eventType, []);
    }
    this.eventHandlers.get(eventType)!.push(handler);
  }

  /**
   * Remover listener
   */
  off(eventType: string, handler: Function): void {
    const handlers = this.eventHandlers.get(eventType);
    if (handlers) {
      const index = handlers.indexOf(handler);
      if (index > -1) {
        handlers.splice(index, 1);
      }
    }
  }

  /**
   * Emitir evento a todos los listeners
   */
  private emitEvent(eventType: string, data: any): void {
    const handlers = this.eventHandlers.get(eventType);
    if (handlers) {
      handlers.forEach(handler => handler(data));
    }
  }
}

// Exportar instancia singleton
export const fingerprintConfigService = new FingerprintConfigService();
```

---

## ?? Componentes UI

### **React Component (`components/FingerprintConfigPanel.tsx`)**

```tsx
import React, { useState, useEffect } from 'react';
import { fingerprintConfigService, FingerprintConfiguration } from '../services/fingerprintConfig.service';

export const FingerprintConfigPanel: React.FC = () => {
  const [config, setConfig] = useState<FingerprintConfiguration | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<string[]>([]);

  // Cargar configuración al montar el componente
  useEffect(() => {
    loadConfiguration();
  }, []);

  const loadConfiguration = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await fingerprintConfigService.getConfiguration();
      setConfig(data);
    } catch (err: any) {
      setError(err.message || 'Error al cargar configuración');
    } finally {
      setLoading(false);
    }
  };

  const handleSave = async () => {
    if (!config) return;

    try {
      setLoading(true);
      setError(null);
      setValidationErrors([]);

      // 1. Validar primero
      const validation = await fingerprintConfigService.validateConfiguration(config);
      
      if (!validation.isValid) {
        setValidationErrors(validation.errors);
        return;
      }

      // 2. Actualizar si es válido
      const updatedConfig = await fingerprintConfigService.updateConfiguration(config);
      setConfig(updatedConfig);
      
      // Mostrar mensaje de éxito
      alert('? Configuración actualizada exitosamente');
    } catch (err: any) {
      setError(err.message || 'Error al guardar configuración');
    } finally {
      setLoading(false);
    }
  };

  const handleReset = async () => {
    if (!confirm('¿Restaurar configuración a valores por defecto?')) {
      return;
    }

    try {
      setLoading(true);
      setError(null);
      const defaultConfig = await fingerprintConfigService.resetConfiguration();
      setConfig(defaultConfig);
      alert('? Configuración restaurada a valores por defecto');
    } catch (err: any) {
      setError(err.message || 'Error al restaurar configuración');
    } finally {
      setLoading(false);
    }
  };

  if (loading && !config) {
    return <div className="loading">Cargando configuración...</div>;
  }

  if (error && !config) {
    return (
      <div className="error">
        <p>? {error}</p>
        <button onClick={loadConfiguration}>Reintentar</button>
      </div>
    );
  }

  return (
    <div className="fingerprint-config-panel">
      <h2>?? Configuración de Huellas Dactilares</h2>

      {error && <div className="alert alert-error">? {error}</div>}
      
      {validationErrors.length > 0 && (
        <div className="alert alert-warning">
          <strong>?? Errores de validación:</strong>
          <ul>
            {validationErrors.map((err, idx) => (
              <li key={idx}>{err}</li>
            ))}
          </ul>
        </div>
      )}

      {config && (
        <form onSubmit={(e) => { e.preventDefault(); handleSave(); }}>
          {/* Threshold */}
          <div className="form-group">
            <label>
              Umbral de Coincidencia (Threshold)
              <span className="help-text">0-100 (más alto = más estricto)</span>
            </label>
            <input
              type="number"
              min="0"
              max="100"
              value={config.threshold}
              onChange={(e) => setConfig({ ...config, threshold: parseInt(e.target.value) })}
              disabled={loading}
            />
          </div>

          {/* Timeout */}
          <div className="form-group">
            <label>
              Timeout (ms)
              <span className="help-text">Tiempo máximo de captura</span>
            </label>
            <input
              type="number"
              min="5000"
              max="60000"
              step="1000"
              value={config.timeout}
              onChange={(e) => setConfig({ ...config, timeout: parseInt(e.target.value) })}
              disabled={loading}
            />
          </div>

          {/* MaxRotation */}
          <div className="form-group">
            <label>
              Rotación Máxima
              <span className="help-text">0-199 (más alto = menos tolerante)</span>
            </label>
            <input
              type="number"
              min="0"
              max="199"
              value={config.maxRotation}
              onChange={(e) => setConfig({ ...config, maxRotation: parseInt(e.target.value) })}
              disabled={loading}
            />
          </div>

          {/* DetectFakeFinger */}
          <div className="form-group">
            <label>
              <input
                type="checkbox"
                checked={config.detectFakeFinger}
                onChange={(e) => setConfig({ ...config, detectFakeFinger: e.target.checked })}
                disabled={loading}
              />
              Detectar Dedos Falsos (Liveness Detection)
            </label>
          </div>

          {/* OverwriteExisting */}
          <div className="form-group">
            <label>
              <input
                type="checkbox"
                checked={config.overwriteExisting}
                onChange={(e) => setConfig({ ...config, overwriteExisting: e.target.checked })}
                disabled={loading}
              />
              Sobrescribir Huellas Existentes
            </label>
          </div>

          {/* Botones */}
          <div className="form-actions">
            <button type="submit" disabled={loading} className="btn btn-primary">
              {loading ? 'Guardando...' : '?? Guardar Cambios'}
            </button>
            <button
              type="button"
              onClick={handleReset}
              disabled={loading}
              className="btn btn-secondary"
            >
              ?? Restaurar Por Defecto
            </button>
            <button
              type="button"
              onClick={loadConfiguration}
              disabled={loading}
              className="btn btn-outline"
            >
              ? Recargar
            </button>
          </div>
        </form>
      )}
    </div>
  );
};
```

### **Componente de Registro con SignalR**

```tsx
import React, { useState, useEffect } from 'react';
import { fingerprintConfigService, SignalRNotification } from '../services/fingerprintConfig.service';

interface FingerprintRegistrationProps {
  dni: string;
  onComplete?: (success: boolean) => void;
}

export const FingerprintRegistration: React.FC<FingerprintRegistrationProps> = ({ dni, onComplete }) => {
  const [currentSample, setCurrentSample] = useState(0);
  const [totalSamples, setTotalSamples] = useState(5);
  const [progress, setProgress] = useState(0);
  const [quality, setQuality] = useState(0);
  const [imageBase64, setImageBase64] = useState<string | null>(null);
  const [status, setStatus] = useState<string>('Preparando...');

  useEffect(() => {
    // Conectar a SignalR al montar el componente
    const setupSignalR = async () => {
      try {
        await fingerprintConfigService.connectToSignalR(dni);
        
        // Registrar listeners para eventos
        fingerprintConfigService.on('operation_started', handleOperationStarted);
        fingerprintConfigService.on('sample_started', handleSampleStarted);
        fingerprintConfigService.on('sample_captured', handleSampleCaptured);
        fingerprintConfigService.on('operation_completed', handleOperationCompleted);
        fingerprintConfigService.on('error', handleError);
        
        setStatus('? Conectado - Esperando inicio de captura...');
      } catch (error) {
        console.error('Error al conectar SignalR:', error);
        setStatus('? Error de conexión');
      }
    };

    setupSignalR();

    // Cleanup al desmontar
    return () => {
      fingerprintConfigService.off('operation_started', handleOperationStarted);
      fingerprintConfigService.off('sample_started', handleSampleStarted);
      fingerprintConfigService.off('sample_captured', handleSampleCaptured);
      fingerprintConfigService.off('operation_completed', handleOperationCompleted);
      fingerprintConfigService.off('error', handleError);
      
      fingerprintConfigService.unsubscribeFromNotifications(dni);
      fingerprintConfigService.disconnectFromSignalR();
    };
  }, [dni]);

  const handleOperationStarted = (notification: SignalRNotification) => {
    console.log('?? Operación iniciada:', notification);
    setStatus('?? Iniciando captura de huella...');
  };

  const handleSampleStarted = (notification: SignalRNotification) => {
    const { currentSample, totalSamples } = notification.data;
    console.log(`?? Muestra ${currentSample}/${totalSamples} iniciada`);
    setCurrentSample(currentSample);
    setTotalSamples(totalSamples);
    setStatus(`Capturando muestra ${currentSample}/${totalSamples}...`);
  };

  const handleSampleCaptured = (notification: SignalRNotification) => {
    const { currentSample, totalSamples, quality, progress, imageBase64 } = notification.data;
    
    console.log(`? Muestra ${currentSample}/${totalSamples} capturada - Calidad: ${quality.toFixed(2)}`);
    
    setCurrentSample(currentSample);
    setTotalSamples(totalSamples);
    setQuality(quality);
    setProgress(progress);
    
    if (imageBase64) {
      setImageBase64(`data:image/bmp;base64,${imageBase64}`);
    }
    
    setStatus(`? Muestra ${currentSample}/${totalSamples} capturada (Calidad: ${quality.toFixed(2)})`);
  };

  const handleOperationCompleted = (notification: SignalRNotification) => {
    const { samplesCollected, averageQuality } = notification.data;
    
    console.log('?? Registro completado exitosamente');
    console.log(`   Muestras: ${samplesCollected}, Calidad promedio: ${averageQuality.toFixed(2)}`);
    
    setStatus(`?? ¡Registro completado! (${samplesCollected} muestras, calidad promedio: ${averageQuality.toFixed(2)})`);
    setProgress(100);
    
    if (onComplete) {
      onComplete(true);
    }
  };

  const handleError = (notification: SignalRNotification) => {
    console.error('? Error en registro:', notification.message);
    setStatus(`? Error: ${notification.message}`);
    
    if (onComplete) {
      onComplete(false);
    }
  };

  return (
    <div className="fingerprint-registration">
      <h3>Registro de Huella - DNI: {dni}</h3>
      
      <div className="status-bar">
        <p className="status-text">{status}</p>
        
        {totalSamples > 0 && (
          <div className="progress-container">
            <div className="progress-bar">
              <div 
                className="progress-fill" 
                style={{ width: `${progress}%` }}
              />
            </div>
            <span className="progress-text">
              {currentSample}/{totalSamples} ({progress}%)
            </span>
          </div>
        )}
        
        {quality > 0 && (
          <div className="quality-indicator">
            <span>Calidad: {quality.toFixed(2)}</span>
            <div className={`quality-bar quality-${getQualityLevel(quality)}`}>
              <div style={{ width: `${quality}%` }} />
            </div>
          </div>
        )}
      </div>

      {imageBase64 && (
        <div className="fingerprint-preview">
          <h4>Vista Previa de Huella</h4>
          <img src={imageBase64} alt="Huella capturada" />
        </div>
      )}
    </div>
  );
};

function getQualityLevel(quality: number): string {
  if (quality >= 80) return 'high';
  if (quality >= 50) return 'medium';
  return 'low';
}
```

---

## ?? Estilos CSS

```css
/* FingerprintConfigPanel.css */
.fingerprint-config-panel {
  max-width: 800px;
  margin: 0 auto;
  padding: 2rem;
  background: #fff;
  border-radius: 8px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 600;
  color: #333;
}

.help-text {
  display: block;
  font-size: 0.85rem;
  color: #666;
  font-weight: normal;
  margin-top: 0.25rem;
}

.form-group input[type="number"],
.form-group input[type="text"] {
  width: 100%;
  padding: 0.75rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 1rem;
}

.form-group input[type="number"]:disabled,
.form-group input[type="text"]:disabled {
  background-color: #f5f5f5;
  cursor: not-allowed;
}

.alert {
  padding: 1rem;
  border-radius: 4px;
  margin-bottom: 1rem;
}

.alert-error {
  background-color: #fee;
  border: 1px solid #fcc;
  color: #c00;
}

.alert-warning {
  background-color: #ffc;
  border: 1px solid #ff9;
  color: #880;
}

.form-actions {
  display: flex;
  gap: 1rem;
  margin-top: 2rem;
}

.btn {
  padding: 0.75rem 1.5rem;
  border: none;
  border-radius: 4px;
  font-size: 1rem;
  cursor: pointer;
  transition: all 0.2s;
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-primary {
  background-color: #007bff;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background-color: #0056b3;
}

.btn-secondary {
  background-color: #6c757d;
  color: white;
}

.btn-secondary:hover:not(:disabled) {
  background-color: #545b62;
}

.btn-outline {
  background-color: transparent;
  border: 1px solid #007bff;
  color: #007bff;
}

.btn-outline:hover:not(:disabled) {
  background-color: #007bff;
  color: white;
}

/* FingerprintRegistration.css */
.fingerprint-registration {
  padding: 2rem;
  background: #f9f9f9;
  border-radius: 8px;
}

.status-bar {
  margin: 1.5rem 0;
}

.status-text {
  font-size: 1.1rem;
  font-weight: 600;
  color: #333;
  margin-bottom: 1rem;
}

.progress-container {
  margin: 1rem 0;
}

.progress-bar {
  width: 100%;
  height: 24px;
  background-color: #e0e0e0;
  border-radius: 12px;
  overflow: hidden;
  position: relative;
}

.progress-fill {
  height: 100%;
  background: linear-gradient(90deg, #4CAF50, #8BC34A);
  transition: width 0.3s ease;
}

.progress-text {
  display: block;
  text-align: center;
  margin-top: 0.5rem;
  font-weight: 600;
  color: #666;
}

.quality-indicator {
  margin-top: 1rem;
}

.quality-bar {
  height: 8px;
  background-color: #e0e0e0;
  border-radius: 4px;
  overflow: hidden;
  margin-top: 0.5rem;
}

.quality-bar div {
  height: 100%;
  transition: width 0.3s ease;
}

.quality-high div { background-color: #4CAF50; }
.quality-medium div { background-color: #FFC107; }
.quality-low div { background-color: #F44336; }

.fingerprint-preview {
  margin-top: 2rem;
  text-align: center;
}

.fingerprint-preview img {
  max-width: 100%;
  height: auto;
  border: 2px solid #ddd;
  border-radius: 8px;
  background: white;
  padding: 1rem;
}
```

---

## ?? Ejemplos Adicionales

### **Vue 3 Composition API**

```typescript
// composables/useFingerprintConfig.ts
import { ref, onMounted } from 'vue';
import { fingerprintConfigService, FingerprintConfiguration } from '@/services/fingerprintConfig.service';

export function useFingerprintConfig() {
  const config = ref<FingerprintConfiguration | null>(null);
  const loading = ref(false);
  const error = ref<string | null>(null);

  const loadConfig = async () => {
    try {
      loading.value = true;
      error.value = null;
      config.value = await fingerprintConfigService.getConfiguration();
    } catch (err: any) {
      error.value = err.message;
    } finally {
      loading.value = false;
    }
  };

  const saveConfig = async (newConfig: FingerprintConfiguration) => {
    try {
      loading.value = true;
      error.value = null;
      config.value = await fingerprintConfigService.updateConfiguration(newConfig);
      return true;
    } catch (err: any) {
      error.value = err.message;
      return false;
    } finally {
      loading.value = false;
    }
  };

  onMounted(() => {
    loadConfig();
  });

  return {
    config,
    loading,
    error,
    loadConfig,
    saveConfig,
  };
}
```

### **Angular Service**

```typescript
// services/fingerprint-config.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class FingerprintConfigService {
  private configSubject = new BehaviorSubject<FingerprintConfiguration | null>(null);
  public config$ = this.configSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadConfiguration();
  }

  private loadConfiguration(): void {
    this.getConfiguration().subscribe(
      config => this.configSubject.next(config),
      error => console.error('Error al cargar configuración:', error)
    );
  }

  getConfiguration(): Observable<FingerprintConfiguration> {
    return this.http.get<ApiResponse<FingerprintConfiguration>>(
      `${environment.apiUrl}/fingerprint/config`
    ).pipe(
      tap(response => console.log('? Configuración obtenida:', response.data)),
      map(response => response.data)
    );
  }

  updateConfiguration(config: FingerprintConfiguration): Observable<FingerprintConfiguration> {
    return this.http.put<ApiResponse<FingerprintConfiguration>>(
      `${environment.apiUrl}/fingerprint/config`,
      config
    ).pipe(
      tap(response => {
        console.log('? Configuración actualizada:', response.data);
        this.configSubject.next(response.data);
      }),
      map(response => response.data)
    );
  }
}
```

---

## ?? Manejo de Errores en Frontend

```typescript
// utils/errorHandler.ts
export function handleFingerprintApiError(error: any): string {
  if (error.response?.data?.errorCode) {
    const errorMessages: Record<string, string> = {
      'UPDATE_CONFIG_FAILED': 'La configuración enviada no es válida',
      'GET_CONFIG_ERROR': 'Error al obtener la configuración del servicio',
      'VALIDATION_ERROR': 'Error al validar la configuración',
      'DEVICE_NOT_CONNECTED': 'El dispositivo de huellas no está conectado',
      'CAPTURE_FAILED': 'Error al capturar la huella',
    };
    
    return errorMessages[error.response.data.errorCode] || error.response.data.message;
  }
  
  if (error.message) {
    return error.message;
  }
  
  return 'Error desconocido al comunicarse con el servicio de huellas';
}

// Uso:
try {
  await fingerprintConfigService.updateConfiguration(config);
} catch (error) {
  const userMessage = handleFingerprintApiError(error);
  toast.error(userMessage); // Usando tu librería de notificaciones
}
```

---

## ? Checklist de Integración

### **Setup Inicial**
- [ ] Instalar dependencias (`axios`, `@microsoft/signalr`)
- [ ] Configurar variables de entorno (`.env`)
- [ ] Crear servicio de configuración (`fingerprintConfig.service.ts`)
- [ ] Configurar interceptores de autenticación

### **Componentes UI**
- [ ] Panel de configuración con formulario
- [ ] Componente de registro con progreso en tiempo real
- [ ] Indicadores visuales de calidad
- [ ] Manejo de errores y validaciones

### **SignalR**
- [ ] Conexión automática al montar componentes
- [ ] Suscripción a eventos por DNI
- [ ] Listeners para todos los eventos
- [ ] Cleanup al desmontar componentes

### **Testing**
- [ ] Probar obtención de configuración
- [ ] Probar actualización completa y parcial
- [ ] Probar validación antes de guardar
- [ ] Probar notificaciones en tiempo real
- [ ] Probar reconexión automática de SignalR

---

## ?? Soporte

Para problemas técnicos:
1. Revisar consola del navegador (errores de red, SignalR)
2. Verificar variables de entorno
3. Probar endpoint directamente con Postman/Insomnia
4. Revisar logs del backend

---

**¿Necesitas más ejemplos o aclaraciones?** ¡Consulta la [documentación completa de la API](./API-REFERENCE.md)!
