import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

const isWatch = process.argv.includes('--watch')

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      '/api': 'http://127.0.0.1:5180'
    }
  },
  build: {
    outDir: isWatch ? '../Homeboard.Backend/Homeboard.API/wwwroot/dist' : 'dist',
    emptyOutDir: true,
    manifest: true,
    watch: isWatch ? { exclude: ['../Homeboard.Backend/**'] } : null,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/[hash].js',
        chunkFileNames: 'assets/[hash].js',
        assetFileNames: 'assets/[hash].[ext]'
      }
    }
  },
  resolve: {
    alias: {
      '@': '/src'
    }
  }
})
