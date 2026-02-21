// Platz Daemon - Site JS
// Auto-scroll log console on page load
document.addEventListener('DOMContentLoaded', function () {
    const logConsole = document.getElementById('logConsole');
    if (logConsole) {
        logConsole.scrollTop = logConsole.scrollHeight;
    }
});
