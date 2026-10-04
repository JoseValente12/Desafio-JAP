// Asks for confirmation before submitting a form that has data-confirm="message".
// It uses the <dialog> in the layout; if the browser cannot show it, it falls back to confirm().
document.addEventListener('submit', (event) => {
  const form = event.target;
  const message = form.dataset?.confirm;

  // No message: nothing to confirm. "confirmed" marks the second pass, after the user said OK.
  if (!message || form.dataset.confirmed === 'true') return;

  event.preventDefault();

  const dialog = document.getElementById('confirm-dialog');
  const submitter = event.submitter;

  const proceed = () => {
    form.dataset.confirmed = 'true';
    form.requestSubmit(submitter); // runs the normal submit again, anti-forgery token included
  };

  if (!dialog || typeof dialog.showModal !== 'function') {
    if (window.confirm(message)) proceed();
    return;
  }

  dialog.querySelector('[data-confirm-message]').textContent = message;
  dialog.returnValue = ''; // Esc or "Voltar" leave it empty, so nothing is submitted
  dialog.addEventListener('close', () => {
    if (dialog.returnValue === 'ok') proceed();
  }, { once: true });
  dialog.showModal();
});