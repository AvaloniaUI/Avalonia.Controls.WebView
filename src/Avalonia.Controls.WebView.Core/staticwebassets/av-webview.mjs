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
