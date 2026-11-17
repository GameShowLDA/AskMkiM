(() => {
    const role = document.body.dataset.role;
    if (!role) return;
    const sent = new Set();
    async function recordVisibleReleases() {
        if (document.visibilityState !== 'visible') return;
        for (const release of document.querySelectorAll('details.release[open]')) {
            const version = release.dataset.version;
            if (!version || sent.has(version)) continue;
            sent.add(version);
            try {
                const response = await fetch('/api/release-notes/viewed', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ role, version })
                });
                if (!response.ok) sent.delete(version);
            } catch {
                sent.delete(version);
            }
        }
    }
    document.addEventListener('toggle', recordVisibleReleases, true);
    document.addEventListener('visibilitychange', recordVisibleReleases);
    requestAnimationFrame(recordVisibleReleases);
})();
