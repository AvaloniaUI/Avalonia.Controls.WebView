// An ES module, which needs a correct MIME type and a real origin.
const checks = document.getElementById("checks");
const results = {};

function report(name, value, ok = true) {
    results[name] = value;
    const item = document.createElement("li");
    item.className = ok ? "ok" : "failed";
    item.textContent = `${name}: ${value}`;
    checks.append(item);
}

report("origin", location.origin, location.origin !== "null");
report("secure context", window.isSecureContext, window.isSecureContext);

try {
    const visits = Number(localStorage.getItem("visits") ?? 0) + 1;
    localStorage.setItem("visits", String(visits));
    report("visits stored in localStorage", visits);
} catch (e) {
    report("localStorage", e.message, false);
}

try {
    const response = await fetch("data/info.json");
    const info = await response.json();
    report("fetch data/info.json", info.message);
} catch (e) {
    report("fetch data/info.json", e.message, false);
}

const missing = await fetch("missing.txt");
report("fetch missing.txt", missing.status, missing.status === 404);

window.localContentResults = results;
