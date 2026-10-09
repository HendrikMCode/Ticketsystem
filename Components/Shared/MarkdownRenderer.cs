using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Ticketsystem.Components.Shared;

/// <summary>
/// Schlanker, sicherer Markdown-Renderer für Beschreibungen und Schritte.
/// Unterstützt: Überschriften (#, ##, ###), **fett**, *kursiv*, ~~durchgestrichen~~, `Code`,
/// Codeblöcke mit Sprache (```sql), Listen (-, 1.), Checklisten (- [ ] / - [x]), Zitate (>),
/// Tabellen (| a | b |), Trennlinien (---), Links, automatische Links und Bilder ![Name](bilder/ID).
/// Es wird nie HTML aus dem Text übernommen – alles wird als Text bzw. eigenes Element ausgegeben.
/// Einfache Zeilenumbrüche bleiben erhalten (wie in GitHub-Kommentaren).
/// </summary>
public static class MarkdownRenderer
{
    public static RenderFragment Render(string? text) => builder =>
    {
        if (string.IsNullOrEmpty(text)) return;
        var zeilen = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        Bloecke(builder, zeilen, 0, zeilen.Length, new Kontext());
    };

    // ── Blockebene ─────────────────────────────────────────────
    private static readonly Regex Ueberschrift = new(@"^(#{1,3})\s+(.+?)\s*#*\s*$", RegexOptions.Compiled);
    private static readonly Regex Trennlinie = new(@"^\s{0,3}([-*_])(\s*\1){2,}\s*$", RegexOptions.Compiled);
    private static readonly Regex Listenpunkt = new(@"^(?<einzug>\s*)(?<marke>[-*+]|\d{1,3}[.)])\s+(?<text>.*)$", RegexOptions.Compiled);
    private static readonly Regex Aufgabe = new(@"^\[(?<x>[ xX])\]\s+(?<text>.*)$", RegexOptions.Compiled);
    private static readonly Regex TabellenTrenner = new(@"^\s*\|?\s*:?-{2,}:?\s*(\|\s*:?-{2,}:?\s*)*\|?\s*$", RegexOptions.Compiled);

    /// <summary>Zählt Codeblöcke, damit jeder einen eindeutigen Schlüssel bekommt.</summary>
    private sealed class Kontext { public int Codebloecke; }

    private static void Bloecke(RenderTreeBuilder b, string[] z, int start, int ende, Kontext k)
    {
        var i = start;
        while (i < ende)
        {
            var zeile = z[i];
            var getrimmt = zeile.TrimStart();

            if (string.IsNullOrWhiteSpace(zeile)) { i++; continue; }

            // Codeblock ```sprache … ```
            if (getrimmt.StartsWith("```") || getrimmt.StartsWith("~~~"))
            {
                var zaun = getrimmt[..3];
                var sprache = getrimmt[3..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                var inhalt = new List<string>();
                i++;
                while (i < ende && !z[i].TrimStart().StartsWith(zaun)) inhalt.Add(z[i++]);
                i++; // schließender Zaun
                Codeblock(b, string.Join("\n", inhalt), sprache, k);
                continue;
            }

            if (Trennlinie.IsMatch(zeile))
            {
                b.OpenElement(1, "hr");
                b.CloseElement();
                i++;
                continue;
            }

            var u = Ueberschrift.Match(zeile);
            if (u.Success)
            {
                var ebene = u.Groups[1].Value.Length;
                b.OpenElement(2, ebene switch { 1 => "h3", 2 => "h4", _ => "h5" });
                b.AddAttribute(3, "class", "md-h" + ebene);
                Inline(b, u.Groups[2].Value);
                b.CloseElement();
                i++;
                continue;
            }

            if (getrimmt.StartsWith('>'))
            {
                var inhalt = new List<string>();
                while (i < ende && z[i].TrimStart().StartsWith('>'))
                {
                    var t = z[i].TrimStart()[1..];
                    inhalt.Add(t.StartsWith(' ') ? t[1..] : t);
                    i++;
                }
                b.OpenElement(4, "blockquote");
                var innen = inhalt.ToArray();
                Bloecke(b, innen, 0, innen.Length, k);
                b.CloseElement();
                continue;
            }

            if (Listenpunkt.IsMatch(zeile))
            {
                var listenEnde = i;
                while (listenEnde < ende && (Listenpunkt.IsMatch(z[listenEnde])
                       || (!string.IsNullOrWhiteSpace(z[listenEnde]) && z[listenEnde].StartsWith("  ") && listenEnde > i)))
                    listenEnde++;
                Liste(b, z, i, listenEnde);
                i = listenEnde;
                continue;
            }

            // Tabelle: Kopfzeile | a | b | + Trennzeile |---|---|
            if (getrimmt.StartsWith('|') && i + 1 < ende && TabellenTrenner.IsMatch(z[i + 1]))
            {
                var tabEnde = i + 2;
                while (tabEnde < ende && z[tabEnde].TrimStart().StartsWith('|')) tabEnde++;
                Tabelle(b, z, i, tabEnde);
                i = tabEnde;
                continue;
            }

            // Absatz: bis Leerzeile oder Beginn eines anderen Blocks
            var absatz = new List<string>();
            while (i < ende && !string.IsNullOrWhiteSpace(z[i]) && (absatz.Count == 0 || !IstBlockAnfang(z, i, ende)))
                absatz.Add(z[i++]);
            b.OpenElement(5, "p");
            for (var zeileNr = 0; zeileNr < absatz.Count; zeileNr++)
            {
                if (zeileNr > 0) { b.OpenElement(6, "br"); b.CloseElement(); }
                Inline(b, absatz[zeileNr]);
            }
            b.CloseElement();
        }
    }

    private static bool IstBlockAnfang(string[] z, int i, int ende)
    {
        var t = z[i].TrimStart();
        return t.StartsWith("```") || t.StartsWith("~~~") || t.StartsWith('>') || Ueberschrift.IsMatch(z[i])
            || Trennlinie.IsMatch(z[i]) || Listenpunkt.IsMatch(z[i])
            || (t.StartsWith('|') && i + 1 < ende && TabellenTrenner.IsMatch(z[i + 1]));
    }

    private static void Codeblock(RenderTreeBuilder b, string code, string sprache, Kontext k)
    {
        var lang = Sprache(sprache);
        b.OpenElement(7, "div");
        // neuer Schlüssel bei geändertem Inhalt → Element wird neu erzeugt und neu eingefärbt
        b.SetKey($"{k.Codebloecke++}:{lang}:{code.GetHashCode()}");
        b.AddAttribute(8, "class", "code-block");

        b.OpenElement(9, "div");
        b.AddAttribute(10, "class", "code-head");
        b.OpenElement(11, "span");
        b.AddContent(12, string.IsNullOrEmpty(sprache) ? "Code" : sprache);
        b.CloseElement();
        b.OpenElement(13, "button");
        b.AddAttribute(14, "type", "button");
        b.AddAttribute(15, "class", "code-copy");
        b.AddAttribute(16, "data-code-kopieren", "");
        b.AddAttribute(17, "title", "Code kopieren");
        b.OpenElement(18, "md-icon");
        b.AddContent(19, "content_copy");
        b.CloseElement();
        b.AddContent(20, "Kopieren");
        b.CloseElement();
        b.CloseElement();

        b.OpenElement(21, "pre");
        b.OpenElement(22, "code");
        if (!string.IsNullOrEmpty(lang)) b.AddAttribute(23, "class", "language-" + lang);
        b.AddContent(24, code);
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    /// <summary>Gängige Kurzformen auf highlight.js-Namen abbilden.</summary>
    private static string Sprache(string s) => s.ToLowerInvariant() switch
    {
        "" => "",
        "c#" or "cs" or "csharp" => "csharp",
        "js" or "javascript" => "javascript",
        "ts" or "typescript" => "typescript",
        "sh" or "bash" or "shell" or "cmd" => "bash",
        "ps" or "ps1" or "powershell" => "powershell",
        "html" or "xml" or "razor" or "cshtml" => "xml",
        "yml" or "yaml" => "yaml",
        "tsql" or "sql" or "mssql" => "sql",
        var x => Regex.Replace(x, "[^a-z0-9+-]", ""),
    };

    private sealed record Punkt(int Einzug, bool Nummeriert, string Text, List<string> Folgezeilen);

    private static void Liste(RenderTreeBuilder b, string[] z, int start, int ende)
    {
        var punkte = new List<Punkt>();
        for (var i = start; i < ende; i++)
        {
            var m = Listenpunkt.Match(z[i]);
            if (m.Success)
                punkte.Add(new Punkt(m.Groups["einzug"].Value.Replace("\t", "    ").Length / 2,
                    char.IsDigit(m.Groups["marke"].Value[0]), m.Groups["text"].Value, new()));
            else if (punkte.Count > 0)
                punkte[^1].Folgezeilen.Add(z[i].Trim());
        }
        var pos = 0;
        while (pos < punkte.Count)
            ListeEbene(b, punkte, ref pos, punkte[pos].Einzug);
    }

    private static void ListeEbene(RenderTreeBuilder b, List<Punkt> punkte, ref int pos, int einzug)
    {
        var nummeriert = punkte[pos].Nummeriert;
        var istCheckliste = punkte.Skip(pos).TakeWhile(p => p.Einzug >= einzug).Where(p => p.Einzug == einzug).All(p => Aufgabe.IsMatch(p.Text));
        b.OpenElement(25, nummeriert ? "ol" : "ul");
        if (istCheckliste) b.AddAttribute(26, "class", "md-checkliste");

        while (pos < punkte.Count && punkte[pos].Einzug >= einzug)
        {
            var p = punkte[pos];
            // Wechsel zwischen „-“ und „1.“ auf gleicher Ebene beginnt eine neue Liste
            if (p.Einzug == einzug && p.Nummeriert != nummeriert) break;
            if (p.Einzug > einzug)
            {
                // tiefer eingerückt ohne Elternpunkt → als Unterliste des vorigen Punkts behandeln
                ListeEbene(b, punkte, ref pos, p.Einzug);
                continue;
            }
            b.OpenElement(27, "li");
            var a = Aufgabe.Match(p.Text);
            if (a.Success)
            {
                var erledigt = a.Groups["x"].Value != " ";
                b.AddAttribute(28, "class", erledigt ? "md-task erledigt" : "md-task");
                b.OpenElement(29, "md-icon");
                b.AddContent(30, erledigt ? "check_box" : "check_box_outline_blank");
                b.CloseElement();
                b.OpenElement(31, "span");
                Inline(b, a.Groups["text"].Value);
                b.CloseElement();
            }
            else
            {
                Inline(b, p.Text);
            }
            foreach (var f in p.Folgezeilen)
            {
                b.OpenElement(32, "br");
                b.CloseElement();
                Inline(b, f);
            }
            pos++;
            if (pos < punkte.Count && punkte[pos].Einzug > einzug)
                ListeEbene(b, punkte, ref pos, punkte[pos].Einzug);
            b.CloseElement();
        }
        b.CloseElement();
    }

    private static void Tabelle(RenderTreeBuilder b, string[] z, int start, int ende)
    {
        static List<string> Zellen(string zeile)
        {
            var t = zeile.Trim();
            if (t.StartsWith('|')) t = t[1..];
            if (t.EndsWith('|')) t = t[..^1];
            return t.Split('|').Select(c => c.Trim()).ToList();
        }
        var kopf = Zellen(z[start]);
        b.OpenElement(33, "div");
        b.AddAttribute(34, "class", "md-tabelle");
        b.OpenElement(35, "table");
        b.OpenElement(36, "thead");
        b.OpenElement(37, "tr");
        foreach (var k in kopf)
        {
            b.OpenElement(38, "th");
            Inline(b, k);
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(39, "tbody");
        for (var i = start + 2; i < ende; i++)
        {
            var zellen = Zellen(z[i]);
            b.OpenElement(40, "tr");
            for (var c = 0; c < kopf.Count; c++)
            {
                b.OpenElement(41, "td");
                if (c < zellen.Count) Inline(b, zellen[c]);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    // ── Zeilenebene ────────────────────────────────────────────
    private static readonly Regex InlineMuster = new(
        @"(?<code>`+)(?<codetext>.+?)\k<code>" +
        @"|!\[(?<ialt>[^\]\n]*)\]\((?<iurl>bilder/\d+)\)" +
        @"|\[(?<ltext>[^\]\n]+)\]\((?<lurl>[^)\s]+)\)" +
        @"|(?<auto>https?://[^\s<>()]+[^\s<>().,;:!?""'])" +
        @"|\*\*(?<b>.+?)\*\*" +
        @"|(?<![\w])__(?<b2>.+?)__(?![\w])" +
        @"|~~(?<s>.+?)~~" +
        @"|(?<![\w*])\*(?![\s*])(?<i>[^*\n]+?)(?<![\s])\*(?![\w*])" +
        @"|(?<![\w])_(?![\s_])(?<i2>[^_\n]+?)(?<![\s])_(?![\w])",
        RegexOptions.Compiled);

    private static void Inline(RenderTreeBuilder b, string text)
    {
        var pos = 0;
        foreach (Match m in InlineMuster.Matches(text))
        {
            if (m.Index > pos) b.AddContent(42, text[pos..m.Index]);
            pos = m.Index + m.Length;

            if (m.Groups["codetext"].Success)
            {
                b.OpenElement(43, "code");
                b.AddAttribute(44, "class", "md-code");
                b.AddContent(45, m.Groups["codetext"].Value.Trim());
                b.CloseElement();
            }
            else if (m.Groups["iurl"].Success)
            {
                var alt = string.IsNullOrWhiteSpace(m.Groups["ialt"].Value) ? "Bild" : m.Groups["ialt"].Value;
                b.OpenElement(46, "a");
                b.AddAttribute(47, "class", "text-bild");
                b.AddAttribute(48, "href", m.Groups["iurl"].Value);
                b.AddAttribute(49, "target", "_blank");
                b.AddAttribute(50, "rel", "noopener");
                b.AddAttribute(51, "title", alt + " – in neuem Tab öffnen");
                b.OpenElement(52, "img");
                b.AddAttribute(53, "src", m.Groups["iurl"].Value);
                b.AddAttribute(54, "alt", alt);
                b.AddAttribute(55, "loading", "lazy");
                b.CloseElement();
                b.CloseElement();
            }
            else if (m.Groups["lurl"].Success)
            {
                var url = m.Groups["lurl"].Value;
                if (SichereUrl(url)) Link(b, url, m.Groups["ltext"].Value);
                else b.AddContent(56, m.Value);
            }
            else if (m.Groups["auto"].Success)
            {
                Link(b, m.Groups["auto"].Value, m.Groups["auto"].Value);
            }
            else
            {
                var (tag, inhalt) =
                    m.Groups["b"].Success ? ("strong", m.Groups["b"].Value) :
                    m.Groups["b2"].Success ? ("strong", m.Groups["b2"].Value) :
                    m.Groups["s"].Success ? ("del", m.Groups["s"].Value) :
                    m.Groups["i"].Success ? ("em", m.Groups["i"].Value) :
                    ("em", m.Groups["i2"].Value);
                b.OpenElement(57, tag);
                Inline(b, inhalt);
                b.CloseElement();
            }
        }
        if (pos < text.Length) b.AddContent(58, text[pos..]);
    }

    private static void Link(RenderTreeBuilder b, string url, string text)
    {
        var extern_ = url.StartsWith("http", StringComparison.OrdinalIgnoreCase) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase);
        b.OpenElement(59, "a");
        b.AddAttribute(60, "class", "text-link");
        b.AddAttribute(61, "href", url);
        if (extern_)
        {
            b.AddAttribute(62, "target", "_blank");
            b.AddAttribute(63, "rel", "noopener noreferrer");
        }
        // Linktext nur formatieren, wenn er selbst keine Adresse enthält (sonst Endlosschleife)
        if (text.Contains("://")) b.AddContent(64, text);
        else Inline(b, text);
        b.CloseElement();
    }

    /// <summary>Nur http(s), mailto und Adressen innerhalb der App – kein javascript: o. Ä.</summary>
    private static bool SichereUrl(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
        || (!url.Contains(':') && !url.StartsWith("//"));
}
