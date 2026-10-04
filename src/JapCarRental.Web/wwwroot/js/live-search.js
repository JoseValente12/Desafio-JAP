// Live search: as the user types, fetch the same page and swap only the results area.
// The server is unchanged; it already filters on ?search=. Without JavaScript the
// form still works with the Search button.
(() => {
  const form = document.querySelector('[data-live-search]');
  if (!form) return;

  const selector = form.dataset.target;
  const input = form.querySelector('input[name="search"]');
  let timer;
  let controller;

  async function run() {
    const url = new URL(form.action, location.href);
    const term = input.value.trim();
    if (term) url.searchParams.set('search', term); // a new search always starts at page 1

    // Cancel the previous request so a slow, old answer can never overwrite a newer one.
    controller?.abort();
    controller = new AbortController();

    try {
      const response = await fetch(url, { signal: controller.signal });
      if (!response.ok) return;

      const doc = new DOMParser().parseFromString(await response.text(), 'text/html');
      const fresh = doc.querySelector(selector);
      const current = document.querySelector(selector);
      if (fresh && current) {
        current.innerHTML = fresh.innerHTML;
        // Keep the address bar in sync so the search can be bookmarked or refreshed.
        history.replaceState(null, '', url.toString());
      }
    } catch (error) {
      if (error.name !== 'AbortError') form.submit(); // fall back to a normal page load
    }
  }

  // 250 ms after the last key press, to avoid one request per letter.
  input.addEventListener('input', () => {
    clearTimeout(timer);
    timer = setTimeout(run, 250);
  });

  form.addEventListener('submit', (event) => {
    event.preventDefault();
    clearTimeout(timer);
    run();
  });
})();