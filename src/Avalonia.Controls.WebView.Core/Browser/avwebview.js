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

export async function eval(iframe, script) {
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
