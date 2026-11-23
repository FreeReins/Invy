// Reads QR labels and product barcodes from the camera. The browser's own BarcodeDetector is used where it has one
// (Chrome on Android, Safari 17+); elsewhere zxing-wasm, bundled with the page and loaded the first time it's needed,
// with its .wasm served by Invy too. Nothing is fetched from a CDN.
import wasmUrl from 'zxing-wasm/reader/zxing_reader.wasm?url';

/** The codes worth reading: Invy's QR labels and the barcodes on products. */
const FORMATS = { qr_code: 'QRCode', ean_13: 'EAN13', ean_8: 'EAN8', upc_a: 'UPCA', upc_e: 'UPCE', code_128: 'Code128' };

/** How often a frame is read, and how long the same code is ignored after it was read. */
const EVERY_MS = 200;
const REPEAT_MS = 2500;

/** True when this browser can use a camera at all (it needs HTTPS, or localhost). */
export const canUseCamera = () => Boolean(navigator.mediaDevices?.getUserMedia) && window.isSecureContext;

/** A function that reads the codes in a video frame: the browser's BarcodeDetector, or zxing-wasm. */
async function makeReader() {
  if ('BarcodeDetector' in window) {
    const supported = await window.BarcodeDetector.getSupportedFormats();
    const formats = Object.keys(FORMATS).filter((format) => supported.includes(format));
    if (formats.includes('qr_code')) {
      const detector = new window.BarcodeDetector({ formats });
      return async (video) => (await detector.detect(video)).map((code) => code.rawValue);
    }
  }
  const { prepareZXingModule, readBarcodes } = await import('zxing-wasm/reader');
  prepareZXingModule({ overrides: { locateFile: (path, prefix) => (path.endsWith('.wasm') ? wasmUrl : prefix + path) } });
  const canvas = document.createElement('canvas');
  const context = canvas.getContext('2d', { willReadFrequently: true });
  const options = { formats: Object.values(FORMATS), tryHarder: true, maxNumberOfSymbols: 1 };
  return async (video) => {
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    context.drawImage(video, 0, 0);
    const results = await readBarcodes(context.getImageData(0, 0, canvas.width, canvas.height), options);
    return results.filter((result) => result.isValid !== false && result.text).map((result) => result.text);
  };
}

/**
 * Starts the back camera in `video` and calls `onCode(text)` for each code read, ignoring the same code read again
 * within a moment. Resolves to a function that stops the camera. Rejects with a readable message when the camera
 * can't be used.
 */
export async function startScanner(video, onCode) {
  let stream;
  try {
    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' }, audio: false });
  } catch (error) {
    throw new Error(error?.name === 'NotAllowedError'
      ? 'Invy wasn\'t allowed to use the camera. Allow it in the browser\'s site settings, or type the code instead.'
      : 'No camera could be started. Type the code instead.');
  }
  video.srcObject = stream;
  await video.play();
  const read = await makeReader();

  let stopped = false;
  let last = { text: null, at: 0 };
  async function tick() {
    if (stopped) return;
    try {
      if (video.readyState >= 2) {
        const text = (await read(video))[0];
        const now = Date.now();
        if (text && (text !== last.text || now - last.at > REPEAT_MS)) {
          last = { text, at: now };
          navigator.vibrate?.(40);
          onCode(text);
        }
      }
    } catch { /* an unreadable frame; try the next */ }
    if (!stopped) setTimeout(tick, EVERY_MS);
  }
  tick();

  return () => {
    stopped = true;
    for (const track of stream.getTracks()) track.stop();
    video.srcObject = null;
  };
}
