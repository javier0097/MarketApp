import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: "../backend/MarketApp.Api/wwwroot",
    emptyOutDir: true,
  },
  server: {
    proxy: {
      // Must match applicationUrl in backend/MarketApp.Api/Properties/launchSettings.json
      "/api": "http://localhost:5120",
    },
  },
});
