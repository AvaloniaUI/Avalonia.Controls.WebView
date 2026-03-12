export async function createIframeElement() {
    const iframe = document.createElement("iframe");
    return iframe;
}

export function goBack(iframe) {
    try {
        iframe.contentWindow.history.back();
        return true;
    } catch {
        return false;
    }
}

export function goForward(iframe) {
    try {
        iframe.contentWindow.history.forward();
        return true;
    } catch {
        return false;
    }
}

export function canGoBack(iframe) {
    try {
        return iframe.contentWindow.history.length > 0;
    } catch {
        return false;
    }
}

export function refresh(iframe) {
    try {
        iframe.contentWindow.location.reload();
        return true;
    } catch {
        return false;
    }
}

export function stop(iframe) {
    try {
        iframe.contentWindow.stop();
        return true;
    } catch {
        return false;
    }
}

export async function evalScript(iframe, script) {
    try {
        var result = iframe.contentWindow.eval(script);
        if (result instanceof Promise) {
            return await result;
        }
        return result;
    } catch {
        return null;
    }
}

export function getActualLocation(iframe) {
    try {
        return iframe.contentWindow.location.href;
    } catch {
        return null;
    }
}

export function subscribe(iframe, onload) {
    var onloadHandler = () => {
        onload(iframe.src);
    };
    iframe.addEventListener("load", onloadHandler);
    return () => iframe.removeEventListener("load", onloadHandler);
}

export function setBackground(iframe, color) {
    iframe.style.backgroundColor = color;
}

export function focusIframe(iframe) {
    iframe.focus();
}

export function blurIframe(iframe) {
    iframe.blur();
}

export function subscribeFocus(iframe, onFocus, onBlur) {
    var focusHandler = () => onFocus();
    var blurHandler = () => onBlur();
    iframe.addEventListener("focus", focusHandler);
    iframe.addEventListener("blur", blurHandler);
    return () => {
        iframe.removeEventListener("focus", focusHandler);
        iframe.removeEventListener("blur", blurHandler);
    };
}

export function subscribeMessages(iframe, onMessage) {
    var handler = (event) => {
        if (event.source === iframe.contentWindow) {
            var data = typeof event.data === "string" ? event.data : JSON.stringify(event.data);
            onMessage(data);
        }
    };
    window.addEventListener("message", handler);
    return () => window.removeEventListener("message", handler);
}

export function injectPostMessageBridge(iframe) {
    try {
        var script = iframe.contentDocument.createElement("script");
        script.textContent = `
            if (!window.chrome) window.chrome = {};
            if (!window.chrome.webview) window.chrome.webview = {};
            window.chrome.webview.postMessage = function(message) {
                window.parent.postMessage(message, '*');
            };
        `;
        iframe.contentDocument.head.appendChild(script);
        return true;
    } catch {
        return false;
    }
}

export function showPrintUI(iframe) {
    try {
        iframe.contentWindow.print();
        return true;
    } catch {
        return false;
    }
}

export function setSandbox(iframe, value) {
    if (value) {
        iframe.setAttribute("sandbox", value);
    } else {
        iframe.removeAttribute("sandbox");
    }
}

let authWindows = new Map();

export function openAuthWindow(windowId, url, redirectUri) {
    return new Promise((resolve, reject) => {
        const authWindow = window.open(url, '_blank', 'width=800,height=600');
        authWindows.set(windowId, authWindow);

        const checkWindow = setInterval(() => {
            try {
                if (authWindow.closed) {
                    clearInterval(checkWindow);
                    reject(new Error('Popup closed by user'));
                    return;
                }

                const currentUrl = authWindow.location.href;
                if (currentUrl.startsWith(redirectUri)) {
                    clearInterval(checkWindow);
                    authWindow.close();
                    authWindows.delete(windowId);
                    resolve(currentUrl);
                }
            } catch (e) {
                // Cross-origin access will throw error - ignore
            }
        }, 100);
    });
}


export function closeAuthWindow(windowId) {
    const window = authWindows.get(windowId);
    if (window) {
        window.close();
        authWindows.delete(windowId);
    }
}
