/**
 * Devvit Client Entry Point — Unity WebGL Loader
 *
 * This TypeScript file runs in the browser when a user opens the Reddit post.
 * Its job is to:
 *   1. Set up the HTML canvas and loading UI
 *   2. Load the Unity WebGL build via the Unity loader script
 *   3. Initialize the Unity instance with the correct build URLs and config
 *
 * How this fits into the Devvit architecture:
 *   - Devvit serves this script alongside the Unity build files from the same origin
 *   - `context` from @devvit/web/client provides read-only post/user data
 *     (e.g. context.postId, context.postData) without requiring any API calls
 *   - All Reddit API interactions (user data, leaderboard, etc.) happen in the
 *     server (src/server/index.ts) because they require authentication
 *
 * Note: The `context` object (from @devvit/web/client) is the CLIENT-SIDE context.
 * It's read-only and contains only what Devvit injects at page load. To fetch
 * authenticated data (like snoovatars), Unity calls the server's /api/* endpoints.
 * The server receives `context` server-side where it can access the Reddit API.
 */

// Type definitions for the Unity WebGL runtime API.
// These are provided by the Unity loader script loaded below.
type UnityBannerType = 'error' | 'warning' | 'info';

type UnityConfig = {
  arguments: string[];
  dataUrl: string;
  frameworkUrl: string;
  codeUrl: string;
  streamingAssetsUrl: string;
  companyName: string;
  productName: string;
  productVersion: string;
  showBanner: (msg: string, type: UnityBannerType) => void;
  matchWebGLToCanvasSize?: boolean;
  autoSyncPersistentDataPath?: boolean;
  devicePixelRatio?: number;
};

type UnityInstance = {
  SetFullscreen: (fullscreen: number) => void;
  SendMessage: (objectName: string, methodName: string, value?: string | number) => void;
  Quit: () => Promise<void>;
};

// The Unity loader function is defined by the loader script (loaderUrl below).
// It's not bundled here — it's loaded dynamically at runtime.
declare function createUnityInstance(
  canvas: HTMLCanvasElement,
  config: UnityConfig,
  onProgress?: (progress: number) => void
): Promise<UnityInstance>;

const canvas = document.querySelector<HTMLCanvasElement>("#unity-canvas");

if (!canvas) {
  throw new Error("Unity canvas element not found");
}

// Displays a temporary banner message during Unity initialization.
// Errors are shown permanently (red); warnings auto-dismiss after 5 seconds.
function unityShowBanner(msg: string, type: UnityBannerType): void {
  const warningBanner = document.querySelector<HTMLElement>("#unity-warning");

  if (!warningBanner) {
    console.error("Warning banner element not found");
    return;
  }

  const banner = warningBanner;

  function updateBannerVisibility(): void {
    banner.style.display = banner.children.length ? 'block' : 'none';
  }

  const div = document.createElement('div');
  div.innerHTML = msg;
  warningBanner.appendChild(div);

  if (type === 'error') {
    div.style.cssText = 'background: red; padding: 10px;';
  } else {
    if (type === 'warning') {
      div.style.cssText = 'background: yellow; padding: 10px;';
    }
    setTimeout(() => {
      warningBanner.removeChild(div);
      updateBannerVisibility();
    }, 5000);
  }
  updateBannerVisibility();
}

// Unity build file paths. These files must be placed in the public/Build/ folder.
// See the root README for the two-step build process that generates these files.
const buildUrl = "Build";
const loaderUrl = buildUrl + "/Ski-99.loader.js";
const config: UnityConfig = {
  arguments: [],
  dataUrl: buildUrl + "/Ski-99.data.unityweb",      // GZip-compressed game data
  frameworkUrl: buildUrl + "/Ski-99.framework.js",  // Uncompressed JS framework
  codeUrl: buildUrl + "/Ski-99.wasm.unityweb",       // GZip-compressed WebAssembly
  streamingAssetsUrl: "StreamingAssets",
  companyName: "DefaultCompany",
  productName: "Ski-99",
  productVersion: "0.1.0",
  showBanner: unityShowBanner,
};

// Mobile: fill the entire viewport so the game is fullscreen on small screens.
// Desktop: fix the canvas to the browser window.
if (/iPhone|iPad|iPod|Android/i.test(navigator.userAgent)) {
  // Prevent the user from zooming/scrolling — the game controls the entire touch surface
  const meta = document.createElement('meta');
  meta.name = 'viewport';
  meta.content = 'width=device-width, height=device-height, initial-scale=1.0, user-scalable=no, shrink-to-fit=yes';
  document.getElementsByTagName('head')[0]?.appendChild(meta);

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) {
    container.className = "unity-mobile";
  }
  canvas.className = "unity-mobile";
} else {
  // Desktop: stretch the canvas to fill the Reddit post iframe
  canvas.style.width = "100%";
  canvas.style.height = "100%";

  const container = document.querySelector<HTMLElement>("#unity-container");
  if (container) {
    container.style.width = "100%";
    container.style.height = "100%";
    container.style.position = "fixed";
    container.style.left = "0";
    container.style.top = "0";
    container.style.transform = "none";
  }
}

// Show the loading bar while Unity is loading
const loadingBar = document.querySelector<HTMLElement>("#unity-loading-bar");
if (loadingBar) {
  loadingBar.style.display = "block";
}

// Splash screen: shown immediately while Unity loads (WebGL can take several seconds).
// The splash image fades in once it has loaded so there's no flash of unstyled content.
// The entire splash overlay fades out on the first tap/click after Unity is ready.
const splashOverlay = document.querySelector<HTMLElement>("#splash-overlay");
if (splashOverlay) {
  const img = new Image();
  img.onload = () => {
    splashOverlay.classList.add("show");
  };
  img.onerror = () => {
    // If the splash image fails to load, still show the overlay with its background color
    splashOverlay.classList.add("show");
  };
  img.src = "TemplateData/splash-background.jpg";
}

// Dynamically load the Unity loader script, then initialize the Unity instance.
// We load the script dynamically (rather than as a <script> tag in HTML) so that
// we can use the Promise-based createUnityInstance() API and handle progress/errors.
const script = document.createElement("script");
script.src = loaderUrl;
script.onload = () => {
  createUnityInstance(canvas, config, (progress: number) => {
    // Update the loading progress bar (0.0 to 1.0)
    const progressBarContainer = document.querySelector<HTMLElement>("#unity-progress-bar-full-container");
    if (progressBarContainer) {
      progressBarContainer.style.width = 100 * progress + "%";
    }
  }).then((unityInstance: UnityInstance) => {
    // Unity is ready — hide the loading bar and show the "tap to start" prompt
    const loadingBar = document.querySelector<HTMLElement>("#unity-loading-bar");
    if (loadingBar) {
      loadingBar.style.display = "none";
    }

    const tapToStart = document.querySelector<HTMLElement>("#tap-to-start");
    if (tapToStart) {
      tapToStart.classList.add("show");
    }

    // Dismiss the splash screen on first tap/click, then pass pointer events
    // through to the Unity canvas underneath
    if (splashOverlay) {
      const handleTap = () => {
        splashOverlay.classList.add("fade-out");
        document.removeEventListener("click", handleTap);
        document.removeEventListener("touchstart", handleTap);
        // After the CSS fade animation completes, disable pointer interception
        setTimeout(() => {
          splashOverlay.style.pointerEvents = "none";
        }, 600);
      };
      document.addEventListener("click", handleTap);
      document.addEventListener("touchstart", handleTap);
    }

    const fullscreenButton = document.querySelector<HTMLElement>("#unity-fullscreen-button");
    if (fullscreenButton) {
      fullscreenButton.onclick = () => {
        unityInstance.SetFullscreen(1);
      };
    }
  }).catch((message: unknown) => {
    alert(message);
  });
};

document.body.appendChild(script);
