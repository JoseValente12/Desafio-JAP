// Live search: as the user types or changes a filter, fetch the same page and swap only the
// results area. The server is unchanged; it already filters on the query string
// (?search=&status=&from=&to=). Without JavaScript the form still works with the Search button.
(() => {
  const form = document.querySelector('[data-live-search]');
  if (!form) return;

  const selector = form.dataset.target;
  let timer;
  let controller;

  async function run() {
    const url = new URL(form.action, location.href);

    // Send every field that has a value, so the search and all the filters travel together.
    // The page number is not a field of the form, so any change starts again at page 1.
    for (const [name, value] of new FormData(form)) {
      const text = String(value).trim();
      if (text) url.searchParams.set(name, text);
    }

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

  const fromInput = form.querySelector('#from');
  const toInput = form.querySelector('#to');

  function syncDateBounds() {
    if (fromInput && toInput) {
      if (fromInput.value) {
        toInput.min = fromInput.value;
      } else {
        toInput.removeAttribute('min');
      }
      if (toInput.value) {
        fromInput.max = toInput.value;
      } else {
        fromInput.removeAttribute('max');
      }
    }
  }

  function schedule() {
    syncDateBounds();
    clearTimeout(timer);
    timer = setTimeout(run, 250); // 250 ms after the last change, to avoid one request per letter
  }

  // "input" covers typing and picking a date; "change" covers the select in browsers
  // that do not fire "input" for it. The delay above merges the two into one request.
  form.addEventListener('input', schedule);
  form.addEventListener('change', schedule);

  syncDateBounds();

  form.addEventListener('submit', (event) => {
    event.preventDefault();
    clearTimeout(timer);
    run();
  });
})();