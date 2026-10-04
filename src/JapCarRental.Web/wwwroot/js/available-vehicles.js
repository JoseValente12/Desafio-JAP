// When the dates of a contract change, reload the vehicle dropdown with only the vehicles
// that are free in that period. ContractService validates again on the server, so this
// script is a convenience, not the rule.
(() => {
  const form = document.querySelector('[data-available-url]');
  if (!form) return;

  const start = form.querySelector('#StartDate');
  const end = form.querySelector('#EndDate');
  const select = form.querySelector('#VehicleId');
  const hint = form.querySelector('#vehicle-hint');
  const defaultHint = hint?.textContent || 'Só aparecem os veículos livres nas datas escolhidas.';
  let controller;

  function parseDate(str) {
    if (!str) return null;
    str = str.trim();

    // yyyy-mm-dd or yyyy/mm/dd
    let m = str.match(/^(\d{4})[-/](\d{1,2})[-/](\d{1,2})$/);
    if (m) {
      const [, year, month, day] = m.map(Number);
      const date = new Date(year, month - 1, day);
      if (date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day) {
        return date;
      }
    }

    // dd/mm/yyyy or dd-mm-yyyy
    m = str.match(/^(\d{1,2})[-/](\d{1,2})[-/](\d{4})$/);
    if (m) {
      const [, day, month, year] = m.map(Number);
      const date = new Date(year, month - 1, day);
      if (date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day) {
        return date;
      }
    }

    return null;
  }

  function toIsoDate(d) {
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  function setOptions(vehicles) {
    const previous = select.value;
    // Keep the first option ("Selecione um veículo") and replace the rest.
    while (select.options.length > 1) select.remove(1);

    for (const v of vehicles) {
      const option = new Option(v.description, v.id);
      option.selected = String(v.id) === previous; // keep the choice if still available
      select.add(option);
    }
    select.dispatchEvent(new CustomEvent('options-replaced'));
  }

  async function refresh() {
    const startDate = parseDate(start.value);
    const endDate = parseDate(end.value);

    if (!startDate || !endDate || endDate.getTime() <= startDate.getTime()) {
      setOptions([]);
      if (hint) hint.textContent = 'Escolha uma data de fim posterior à data de início.';
      return;
    }

    const url = new URL(form.dataset.availableUrl, location.href);
    url.searchParams.set('start', toIsoDate(startDate));
    url.searchParams.set('end', toIsoDate(endDate));

    // Cancel the previous request so an old, slow answer never overwrites a newer one.
    controller?.abort();
    controller = new AbortController();

    try {
      const response = await fetch(url, { signal: controller.signal });
      if (!response.ok) return;

      const vehicles = await response.json();
      setOptions(vehicles);
      if (hint) {
        hint.textContent = vehicles.length ? defaultHint : 'Nenhum veículo disponível nestas datas.';
      }
    } catch (error) {
      if (error.name !== 'AbortError' && hint) {
        hint.textContent = 'Não foi possível carregar os veículos.';
      }
    }
  }

  start.addEventListener('change', refresh);
  start.addEventListener('input', refresh);
  end.addEventListener('change', refresh);
  end.addEventListener('input', refresh);
})();