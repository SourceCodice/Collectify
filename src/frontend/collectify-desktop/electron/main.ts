import { app, BrowserWindow, shell } from "electron";
import path from "node:path";

const rendererUrl = process.env.COLLECTIFY_RENDERER_URL ?? "http://127.0.0.1:5173";
const appIconPath = app.isPackaged
  ? path.join(process.resourcesPath, "collectify-logo.png")
  : path.join(app.getAppPath(), "public", "collectify-logo.png");

function createMainWindow(): void {
  const window = new BrowserWindow({
    width: 1240,
    height: 820,
    minWidth: 960,
    minHeight: 680,
    title: "Collectify",
    backgroundColor: "#111214",
    icon: appIconPath,
    webPreferences: {
      preload: path.join(__dirname, "preload.js"),
      contextIsolation: true,
      nodeIntegration: false
    }
  });

  window.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url);
    return { action: "deny" };
  });

  if (app.isPackaged) {
    window.loadFile(path.join(__dirname, "../dist/index.html"));
    return;
  }

  window.loadURL(rendererUrl);
}

app.whenReady().then(() => {
  createMainWindow();

  app.on("activate", () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createMainWindow();
    }
  });
});

app.on("window-all-closed", () => {
  if (process.platform !== "darwin") {
    app.quit();
  }
});
