import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.VITE_GO_API_URL ?? 'http://localhost:8088',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, '/api/v1'),
      },
      '/go-api': {
        target: process.env.VITE_GO_API_URL ?? 'http://localhost:8088',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/go-api/, ''),
      },
      '/ruby-api': {
        target: process.env.VITE_RUBY_REGISTRY_URL ?? 'http://localhost:4567',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/ruby-api/, ''),
      },
    },
  },
});
