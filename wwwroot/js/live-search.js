(function () {
    const input = document.getElementById('global-search');
    const resultsBox = document.getElementById('search-results');
    if (!input || !resultsBox) return;

    let debounceTimer = null;

    function hideResults() {
        resultsBox.style.display = 'none';
        resultsBox.innerHTML = '';
    }

    function showResults(items) {
        if (!items.length) {
            resultsBox.innerHTML = '<div class="search-result-item text-muted">' +
                (input.dataset.noResults || 'Brak wyników') + '</div>';
        } else {
            resultsBox.innerHTML = items.map(function (b) {
                const price = b.cena != null ? b.cena.toFixed(2) + ' zł' : '';
                return '<a class="search-result-item" href="/Shop/Details/' + b.id + '">' +
                    '<strong>' + escapeHtml(b.tytul) + '</strong>' +
                    '<span>' + escapeHtml(b.autor) + (price ? ' · ' + price : '') + '</span></a>';
            }).join('');
        }
        resultsBox.style.display = 'block';
    }

    function escapeHtml(text) {
        const d = document.createElement('div');
        d.textContent = text || '';
        return d.innerHTML;
    }

    input.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        const q = input.value.trim();
        if (q.length < 2) { hideResults(); return; }

        debounceTimer = setTimeout(function () {
            fetch('/api/books?q=' + encodeURIComponent(q))
                .then(function (r) { return r.json(); })
                .then(function (data) { showResults(data.slice(0, 8)); })
                .catch(function () { hideResults(); });
        }, 300);
    });

    document.addEventListener('click', function (e) {
        if (!input.contains(e.target) && !resultsBox.contains(e.target))
            hideResults();
    });

    input.addEventListener('focus', function () {
        if (resultsBox.innerHTML) resultsBox.style.display = 'block';
    });
})();
