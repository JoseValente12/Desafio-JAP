// Asks for confirmation before submitting any form that has a data-confirm message.
// Kept in a script file (and not an inline onsubmit) so the Content Security Policy
// can forbid inline scripts altogether.
document.addEventListener('submit', event => {
  const message = event.target.dataset?.confirm;
  if (message && !window.confirm(message)) {
    event.preventDefault();
  }
});