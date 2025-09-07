import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000', // docker default
        changeOrigin: true,
        configure: (proxy)=> {
          proxy.on('error', ()=>{});
        },
        bypass: async (_req,_res,options) => {
          // On first call, detect which backend port responds. Cache result in globalThis.
          if (!globalThis.__apiTarget) {
            const tryPorts = [5000, 62509, 8080]; // 5000 (docker mapped), 62509 (local launch), 8080 (container internal)
            for (const p of tryPorts) {
              try {
                const controller = new AbortController();
                const t = setTimeout(()=> controller.abort(), 250);
                const resp = await fetch(`http://localhost:${p}/api/health`, { signal: controller.signal });
                clearTimeout(t);
                if (resp.ok) { globalThis.__apiTarget = `http://localhost:${p}`; break; }
              } catch(_) { /* ignore */ }
            }
            // Fallback if health not implemented: probe /api/devices
            if(!globalThis.__apiTarget) {
              for (const p of tryPorts) {
                try {
                  const controller = new AbortController();
                  const t = setTimeout(()=> controller.abort(), 300);
                  const resp = await fetch(`http://localhost:${p}/api/devices`, { signal: controller.signal });
                  clearTimeout(t);
                  if (resp.ok) { globalThis.__apiTarget = `http://localhost:${p}`; break; }
                } catch(_) { }
              }
            }
            if(!globalThis.__apiTarget) globalThis.__apiTarget = 'http://localhost:62509';
            // eslint-disable-next-line no-console
            console.log('[vite-proxy] API target resolved to', globalThis.__apiTarget);
          }
          options.target = globalThis.__apiTarget;
          return null; // continue proxying
        }
      }
    }
  }
})
