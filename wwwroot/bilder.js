// Bilder in Textfelder einfügen – per Strg+V, Drag & Drop oder Dateiauswahl.
// Funktioniert für jedes <textarea data-bilder="TICKET-ID">. Das Bild wird hochgeladen
// und als ![Name](bilder/ID) an der Cursorposition eingefügt.
(function () {
    'use strict';

    function bilderAus(liste) {
        return Array.from(liste || []).filter(function (f) {
            return f && f.type && f.type.indexOf('image/') === 0 && f.type.indexOf('svg') < 0;
        });
    }

    function hinweis(textarea, text) {
        var feld = textarea.closest('md-outlined-field') || textarea;
        var alt = feld.getAttribute('supporting-text');
        feld.setAttribute('supporting-text', text);
        return function () {
            if (alt === null) feld.removeAttribute('supporting-text'); else feld.setAttribute('supporting-text', alt);
        };
    }

    function einfuegen(textarea, text) {
        textarea.focus();
        var start = textarea.selectionStart != null ? textarea.selectionStart : textarea.value.length;
        var ende = textarea.selectionEnd != null ? textarea.selectionEnd : start;
        var vorher = textarea.value.slice(0, start);
        // Bilder bekommen eine eigene Zeile
        var praefix = vorher.length && !vorher.endsWith('\n') ? '\n' : '';
        textarea.setRangeText(praefix + text + '\n', start, ende, 'end');
        // Blazor über die Änderung informieren (löst Auto-Speichern aus)
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
    }

    async function hochladen(textarea, dateien) {
        var ticket = textarea.getAttribute('data-bilder');
        if (!ticket || ticket === '0') {
            alert('Bilder können erst eingefügt werden, wenn das Ticket angelegt ist (Betreff eintragen).');
            return;
        }
        var fertig = hinweis(textarea, dateien.length > 1 ? dateien.length + ' Bilder werden hochgeladen …' : 'Bild wird hochgeladen …');
        try {
            for (var i = 0; i < dateien.length; i++) {
                var datei = dateien[i];
                var name = datei.name && datei.name !== 'image.png' ? datei.name : 'Screenshot';
                var antwort = await fetch('api/bilder?ticket=' + encodeURIComponent(ticket) + '&name=' + encodeURIComponent(name), {
                    method: 'POST',
                    headers: { 'Content-Type': datei.type },
                    body: datei
                });
                if (!antwort.ok) throw new Error(await antwort.text());
                var ergebnis = await antwort.json();
                einfuegen(textarea, '![' + name.replace(/[\[\]]/g, '') + '](' + ergebnis.url + ')');
            }
        } catch (fehler) {
            alert('Bild konnte nicht hochgeladen werden: ' + (fehler && fehler.message ? fehler.message : fehler));
        } finally {
            fertig();
        }
    }

    function zielTextarea(e) {
        var el = e.target;
        return el && el.closest ? el.closest('textarea[data-bilder]') : null;
    }

    // Strg+V
    document.addEventListener('paste', function (e) {
        var textarea = zielTextarea(e);
        if (!textarea || !e.clipboardData) return;
        var dateien = bilderAus(e.clipboardData.files);
        if (!dateien.length) return;   // normaler Text → Browser macht es selbst
        e.preventDefault();
        hochladen(textarea, dateien);
    });

    // Drag & Drop von Bilddateien
    document.addEventListener('dragover', function (e) {
        if (zielTextarea(e) && e.dataTransfer && Array.from(e.dataTransfer.types || []).indexOf('Files') >= 0) {
            e.preventDefault();
            e.dataTransfer.dropEffect = 'copy';
        }
    });
    document.addEventListener('drop', function (e) {
        var textarea = zielTextarea(e);
        if (!textarea || !e.dataTransfer) return;
        var dateien = bilderAus(e.dataTransfer.files);
        if (!dateien.length) return;
        e.preventDefault();
        hochladen(textarea, dateien);
    });

    // Knopf „Bild einfügen“: <button data-bild-waehlen="CSS-Selektor des Textfelds">
    document.addEventListener('click', function (e) {
        var knopf = e.target && e.target.closest ? e.target.closest('[data-bild-waehlen]') : null;
        if (!knopf) return;
        var textarea = document.querySelector(knopf.getAttribute('data-bild-waehlen'));
        if (!textarea) return;
        var auswahl = document.createElement('input');
        auswahl.type = 'file';
        auswahl.accept = 'image/png,image/jpeg,image/gif,image/webp';
        auswahl.multiple = true;
        auswahl.addEventListener('change', function () {
            var dateien = bilderAus(auswahl.files);
            if (dateien.length) hochladen(textarea, dateien);
        });
        auswahl.click();
    });
})();
