// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Hide external login providers note on the Identity login page when none are configured
(function () {
  try {
    var path = (location && location.pathname || '').toLowerCase();
    if (path.indexOf('/identity/account/login') === -1) return;
    document.body && document.body.setAttribute('data-route-login', 'true');
    var headers = document.querySelectorAll('h3');
    for (var i = 0; i < headers.length; i++) {
      var h = headers[i];
      var text = (h.textContent || '').trim().toLowerCase();
      if (text.indexOf('use another service to log in') !== -1) {
        var section = h.closest('section');
        if (section) section.style.display = 'none';
      }
    }
  } catch (_) { /* no-op */ }
})();