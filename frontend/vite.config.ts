/// <reference types="vitest/config" />
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    env: { TZ: "America/La_Paz" },
  },
  build: {
    outDir: "../backend/MarketApp.Api/wwwroot",
    emptyOutDir: true,
  },
  server: {
    open: true,
    proxy: {
      // Must match Urls in backend/MarketApp.Api/appsettings.json
      "/api": "http://localhost:5120",
    },
  },
});
