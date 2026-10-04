// Adds a filter box above every <select data-searchable>. The native <select> stays
// (it posts the id and works with the keyboard); the box only narrows its options.
(() => {
  // Lowercase and without accents, so "joao" matches "João".
  const plain = (text) => text.normalize('NFD').replace(/\p{Diacritic}/gu, '').toLowerCase();
  // Also drop separators, so "aa-11" matches the plate AA11BB.
  const compact = (text) => plain(text).replace(/[^a-z0-9]/g, '');

  function init(select) {
    const filter = document.createElement('input');
    filter.type = 'search';
    filter.className = 'jr-input jr-select-filter';
    filter.placeholder = select.dataset.searchPlaceholder || 'Pesquisar';
    filter.autocomplete = 'off';
    filter.setAttribute('aria-label', filter.placeholder);
    select.before(filter); // no name attribute, so it is never posted

    let all = [];

    // Snapshot of the options, skipping the placeholder (empty value).
    function capture() {
      all = [...select.options]
        .filter((o) => o.value !== '')
        .map((o) => ({ value: o.value, text: o.text }));
    }

    function render() {
      const term = plain(filter.value.trim());
      const compactTerm = compact(filter.value);
      const selected = select.value;

      // The current choice always stays in the list, so filtering never drops it silently.
      const matches = all.filter(
        (o) =>
          o.value === selected ||
          !term ||
          plain(o.text).includes(term) ||
          (compactTerm && compact(o.text).includes(compactTerm))
      );

      while (select.options.length > 1) select.remove(1); // keep the placeholder
      for (const o of matches) {
        const option = new Option(o.text, o.value);
        option.selected = o.value === selected;
        select.add(option);
      }

      // While searching, show the matches as a short list so the result is visible at once.
      select.size = term ? Math.min(matches.length + 1, 6) : 1;
    }

    function collapse() {
      filter.value = '';
      render();
    }

    filter.addEventListener('input', render);

    filter.addEventListener('keydown', (event) => {
      if (event.key !== 'Enter') return;
      event.preventDefault(); // Enter must not submit the form

      const first = [...select.options].find((o) => o.value !== '');
      if (first && filter.value.trim()) {
        select.value = first.value;
        select.dispatchEvent(new Event('change', { bubbles: true }));
        collapse();
      }
    });

    // Choosing an option with the mouse closes the list and clears the box.
    select.addEventListener('change', () => {
      if (filter.value) collapse();
    });

    // Another script (available-vehicles.js) replaced the options: take a new snapshot.
    select.addEventListener('options-replaced', () => {
      capture();
      render();
    });

    capture();
  }

  document.querySelectorAll('select[data-searchable]').forEach(init);
})();