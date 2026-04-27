document.addEventListener('click', event => {
    const link = event.target.closest?.('a[data-nav]');
    const id = link?.dataset.nav;
    if (!id) return;

    event.preventDefault();
    window.parent.postMessage({ type: 'mki:navigate', id }, '*');
});
