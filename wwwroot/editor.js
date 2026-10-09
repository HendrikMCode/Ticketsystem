// Formatierung (Markdown) in Textfeldern, Syntax-Hervorhebung und „Code kopieren“.
//  - Werkzeugleiste: <button data-md-format="bold" data-md-ziel="#beschreibung-text">
//  - Tastenkürzel in <textarea data-markdown>: Strg+B fett, Strg+I kursiv, Strg+E Code, Strg+K Link
//  - Codeblöcke (pre code.language-xyz) werden mit highlight.js eingefärbt
(function () {
    'use strict';

    // ── Formatierung einfügen ──────────────────────────────────
    function aendern(textarea, neuerText, selStart, selEnde) {
        textarea.focus();
        textarea.setRangeText(neuerText, textarea.selectionStart, textarea.selectionEnd, 'select');
        var basis = textarea.selectionStart;
        textarea.setSelectionRange(basis + selStart, basis + selEnde);
        textarea.dispatchEvent(new Event('input', { bubbles: true }));   // Blazor + Auto-Speichern
    }

    function umschliessen(textarea, links, rechts, platzhalter) {
        var auswahl = textarea.value.slice(textarea.selectionStart, textarea.selectionEnd) || platzhalter;
        aendern(textarea, links + auswahl + rechts, links.length, links.length + auswahl.length);
    }

    function zeilenAnfang(textarea, praefixFn) {
        // Auswahl auf ganze Zeilen erweitern
        var v = textarea.value;
        var start = v.lastIndexOf('\n', textarea.selectionStart - 1) + 1;
        var ende = v.indexOf('\n', textarea.selectionEnd);
        if (ende < 0) ende = v.length;
        textarea.setSelectionRange(start, ende);
        var zeilen = v.slice(start, ende).split('\n');
        var neu = zeilen.map(function (z, i) { return praefixFn(i) + z; }).join('\n');
        aendern(textarea, neu, neu.length, neu.length);
    }

    function formatieren(textarea, art) {
        switch (art) {
            case 'bold': return umschliessen(textarea, '**', '**', 'fett');
            case 'italic': return umschliessen(textarea, '*', '*', 'kursiv');
            case 'strike': return umschliessen(textarea, '~~', '~~', 'durchgestrichen');
            case 'code': return umschliessen(textarea, '`', '`', 'code');
            case 'codeblock': {
                var auswahl = textarea.value.slice(textarea.selectionStart, textarea.selectionEnd);
                var vorher = textarea.value.slice(0, textarea.selectionStart);
                var nl = vorher.length && !vorher.endsWith('\n') ? '\n' : '';
                var text = nl + '```sql\n' + (auswahl || '') + '\n```\n';
                // Cursor auf die Sprache setzen, damit man sie direkt überschreiben kann
                return aendern(textarea, text, nl.length + 3, nl.length + 6);
            }
            case 'heading': return zeilenAnfang(textarea, function () { return '## '; });
            case 'list': return zeilenAnfang(textarea, function () { return '- '; });
            case 'numbered': return zeilenAnfang(textarea, function (i) { return (i + 1) + '. '; });
            case 'task': return zeilenAnfang(textarea, function () { return '- [ ] '; });
            case 'quote': return zeilenAnfang(textarea, function () { return '> '; });
            case 'link': {
                var t = textarea.value.slice(textarea.selectionStart, textarea.selectionEnd) || 'Linktext';
                var istUrl = /^https?:\/\//i.test(t);
                var neu = istUrl ? '[Linktext](' + t + ')' : '[' + t + '](https://)';
                return istUrl ? aendern(textarea, neu, 1, 9) : aendern(textarea, neu, t.length + 3, neu.length - 1);
            }
        }
    }

    document.addEventListener('click', function (e) {
        var knopf = e.target && e.target.closest ? e.target.closest('[data-md-format]') : null;
        if (!knopf) return;
        var textarea = document.querySelector(knopf.getAttribute('data-md-ziel'));
        if (textarea) { e.preventDefault(); formatieren(textarea, knopf.getAttribute('data-md-format')); }
    });

    document.addEventListener('keydown', function (e) {
        var textarea = e.target && e.target.closest ? e.target.closest('textarea[data-markdown]') : null;
        if (!textarea || !(e.ctrlKey || e.metaKey) || e.altKey) return;
        var art = { b: 'bold', i: 'italic', e: 'code', k: 'link' }[e.key.toLowerCase()];
        if (art && !e.shiftKey) { e.preventDefault(); formatieren(textarea, art); }
    });

    // Auswahlfelder (Ansprechpartner, Mandant, Umfeld): Pfeiltasten und Enter steuern die Vorschläge –
    // kein Springen des Cursors und kein Absenden des Formulars. Blazor bekommt die Taste trotzdem.
    document.addEventListener('keydown', function (e) {
        if (e.target && e.target.matches && e.target.matches('input[data-auswahl]')
            && (e.key === 'Enter' || e.key === 'ArrowUp' || e.key === 'ArrowDown')) {
            e.preventDefault();
        }
    });

    // ── Code kopieren ──────────────────────────────────────────
    document.addEventListener('click', function (e) {
        var knopf = e.target && e.target.closest ? e.target.closest('[data-code-kopieren]') : null;
        if (!knopf) return;
        var code = knopf.closest('.code-block');
        code = code ? code.querySelector('pre code') : null;
        if (!code || !navigator.clipboard) return;
        navigator.clipboard.writeText(code.textContent).then(function () {
            var alt = knopf.lastChild.textContent;
            knopf.lastChild.textContent = 'Kopiert';
            setTimeout(function () { knopf.lastChild.textContent = alt; }, 1500);
        });
    });

    // ── Syntax-Hervorhebung (highlight.js) ─────────────────────
    function einfaerben(wurzel) {
        if (!window.hljs || !wurzel.querySelectorAll) return;
        wurzel.querySelectorAll('pre code[class*="language-"]:not([data-hl])').forEach(function (el) {
            el.setAttribute('data-hl', '');
            var sprache = (el.className.match(/language-([\w+-]+)/) || [])[1];
            if (sprache && window.hljs.getLanguage(sprache)) {
                try { window.hljs.highlightElement(el); } catch (x) { }
            }
        });
    }

    function starten() {
        einfaerben(document);
        new MutationObserver(function (aenderungen) {
            aenderungen.forEach(function (a) {
                a.addedNodes.forEach(function (n) { if (n.nodeType === 1) einfaerben(n.parentNode || n); });
            });
        }).observe(document.body, { childList: true, subtree: true });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', starten); else starten();
})();
