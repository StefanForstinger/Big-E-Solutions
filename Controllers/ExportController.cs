using System.Security.Claims;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectPlanner.Data;
using ProjectPlanner.Models;

namespace ProjectPlanner.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExportController(AppDbContext db) => _db = db;

    // ── Hilfsmethode: Projekte laden ─────────────────────────────────────────
    private async Task<List<Project>> LoadProjects(string userId, string role)
    {
        var query = _db.Projects
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .OrderBy(p => p.StartDate);

        return role is "Admin" or "Teacher"
            ? await query.ToListAsync()
            : await query.Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)).ToListAsync();
    }

    // ── Excel Export ─────────────────────────────────────────────────────────
    [HttpGet("excel/{projectId:int}")]
    public async Task<IActionResult> ExportExcel(int projectId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role   = User.FindFirstValue(ClaimTypes.Role)!;

        var project = await _db.Projects
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return NotFound();
        if (project.OwnerId != userId && role is not ("Admin" or "Teacher") && !project.Members.Any(m => m.UserId == userId))
            return Forbid();

        using var wb = new XLWorkbook();

        // ── Sheet 1: Projektübersicht ──────────────────────────────────────
        var ws = wb.Worksheets.Add("Projektübersicht");

        // Titel
        ws.Range("A1:F1").Merge().Value = project.Name;
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 16;
        ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1A2E4A");
        ws.Cell("A1").Style.Font.FontColor = XLColor.White;
        ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        // Projektinfo
        var infoData = new[]
        {
            ("Beschreibung", project.Description),
            ("Startdatum",   project.StartDate.ToString("dd.MM.yyyy")),
            ("Enddatum",     project.EndDate.ToString("dd.MM.yyyy")),
            ("Eigentümer",   project.Owner?.FullName ?? "-"),
            ("Fortschritt",  project.Tasks.Any() ? $"{(int)project.Tasks.Average(t => t.Progress)} %" : "0 %"),
            ("Mitglieder",   string.Join(", ", project.Members.Select(m => m.User.FullName)))
        };

        int row = 2;
        foreach (var (label, value) in infoData)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF5FB");
            ws.Cell(row, 2).Value = value;
            ws.Range(row, 2, row, 6).Merge();
            row++;
        }

        row++;

        // ── Task-Tabelle ───────────────────────────────────────────────────
        string[] headers = { "Aufgabe", "Status", "Priorität", "Fortschritt (%)", "Startdatum", "Enddatum", "Zugewiesen an", "Meilenstein", "Notiz" };
        for (int col = 1; col <= headers.Length; col++)
        {
            var cell = ws.Cell(row, col);
            cell.Value = headers[col - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D9CDB");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }
        row++;

        int taskStartRow = row;
        foreach (var task in project.Tasks.OrderBy(t => t.StartDate))
        {
            ws.Cell(row, 1).Value = task.Title;
            ws.Cell(row, 2).Value = task.Status;
            ws.Cell(row, 3).Value = task.Priority;
            ws.Cell(row, 4).Value = task.Progress;
            ws.Cell(row, 5).Value = task.StartDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 6).Value = task.EndDate.ToString("dd.MM.yyyy");
            ws.Cell(row, 7).Value = task.Assignee?.FullName ?? "-";
            ws.Cell(row, 8).Value = task.IsMilestone ? "Ja" : "Nein";
            ws.Cell(row, 9).Value = task.Note ?? "";

            // Farbcodierung nach Status
            var fillColor = task.Status switch
            {
                "Done"       => XLColor.FromHtml("#D5F5E3"),
                "InProgress" => XLColor.FromHtml("#FEF9E7"),
                "Blocked"    => XLColor.FromHtml("#FDEDEC"),
                _            => XLColor.FromHtml("#EBF5FB")
            };
            ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = fillColor;
            ws.Range(row, 1, row, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            row++;
        }

        // Fortschritt-Spalte als Zahlenformat
        if (row > taskStartRow)
            ws.Range(taskStartRow, 4, row - 1, 4).Style.NumberFormat.Format = "0\"%\"";

        // Summenzeile
        ws.Cell(row, 1).Value = "Ø Fortschritt";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 4).FormulaA1 = $"=AVERAGE(D{taskStartRow}:D{row - 1})";
        ws.Cell(row, 4).Style.Font.Bold = true;
        ws.Cell(row, 4).Style.NumberFormat.Format = "0\"%\"";
        ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#D6EAF8");

        // Spaltenbreiten anpassen
        ws.Column(1).Width = 28;
        ws.Column(2).Width = 14;
        ws.Column(3).Width = 12;
        ws.Column(4).Width = 16;
        ws.Column(5).Width = 14;
        ws.Column(6).Width = 14;
        ws.Column(7).Width = 22;
        ws.Column(8).Width = 13;
        ws.Column(9).Width = 35;
        ws.SheetView.FreezeRows(1);

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Projekt_{SanitizeFileName(project.Name)}_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ── Excel Alle Projekte ───────────────────────────────────────────────────
    [HttpGet("excel")]
    public async Task<IActionResult> ExportAllExcel()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role     = User.FindFirstValue(ClaimTypes.Role)!;
        var projects = await LoadProjects(userId, role);

        using var wb = new XLWorkbook();

        // ── Übersichtsblatt ─────────────────────────────────────────────────
        var wsOverview = wb.Worksheets.Add("Alle Projekte");

        wsOverview.Range("A1:G1").Merge().Value = "Projektübersicht – " + DateTime.Now.ToString("dd.MM.yyyy");
        wsOverview.Cell("A1").Style.Font.Bold = true;
        wsOverview.Cell("A1").Style.Font.FontSize = 14;
        wsOverview.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1A2E4A");
        wsOverview.Cell("A1").Style.Font.FontColor = XLColor.White;

        string[] ovHeaders = { "Projekt", "Beschreibung", "Start", "Ende", "Fortschritt (%)", "Aufgaben", "Eigentümer" };
        for (int c = 1; c <= ovHeaders.Length; c++)
        {
            var cell = wsOverview.Cell(2, c);
            cell.Value = ovHeaders[c - 1];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D9CDB");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int ovRow = 3;
        foreach (var p in projects)
        {
            wsOverview.Cell(ovRow, 1).Value = p.Name;
            wsOverview.Cell(ovRow, 2).Value = p.Description;
            wsOverview.Cell(ovRow, 3).Value = p.StartDate.ToString("dd.MM.yyyy");
            wsOverview.Cell(ovRow, 4).Value = p.EndDate.ToString("dd.MM.yyyy");
            wsOverview.Cell(ovRow, 5).Value = p.Tasks.Any() ? (int)p.Tasks.Average(t => t.Progress) : 0;
            wsOverview.Cell(ovRow, 6).Value = p.Tasks.Count;
            wsOverview.Cell(ovRow, 7).Value = p.Owner?.FullName ?? "-";

            if (ovRow % 2 == 0)
                wsOverview.Range(ovRow, 1, ovRow, 7).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF5FB");
            ovRow++;
        }

        wsOverview.Range(3, 5, ovRow - 1, 5).Style.NumberFormat.Format = "0\"%\"";
        for (int c = 1; c <= 7; c++) wsOverview.Column(c).AdjustToContents();

        // ── Pro Projekt ein Tabellenblatt ────────────────────────────────────
        foreach (var project in projects)
        {
            var sheetName = SanitizeSheetName(project.Name);
            var ws = wb.Worksheets.Add(sheetName);

            ws.Range("A1:H1").Merge().Value = project.Name;
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 13;
            ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.FromHtml("#1A2E4A");
            ws.Cell("A1").Style.Font.FontColor = XLColor.White;

            string[] headers = { "Aufgabe", "Status", "Priorität", "Fortschritt (%)", "Startdatum", "Enddatum", "Zugewiesen an", "Meilenstein" };
            for (int c = 1; c <= headers.Length; c++)
            {
                var cell = ws.Cell(2, c);
                cell.Value = headers[c - 1];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2D9CDB");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int tRow = 3;
            foreach (var task in project.Tasks.OrderBy(t => t.StartDate))
            {
                ws.Cell(tRow, 1).Value = task.Title;
                ws.Cell(tRow, 2).Value = task.Status;
                ws.Cell(tRow, 3).Value = task.Priority;
                ws.Cell(tRow, 4).Value = task.Progress;
                ws.Cell(tRow, 5).Value = task.StartDate.ToString("dd.MM.yyyy");
                ws.Cell(tRow, 6).Value = task.EndDate.ToString("dd.MM.yyyy");
                ws.Cell(tRow, 7).Value = task.Assignee?.FullName ?? "-";
                ws.Cell(tRow, 8).Value = task.IsMilestone ? "Ja" : "Nein";

                if (tRow % 2 == 0)
                    ws.Range(tRow, 1, tRow, 8).Style.Fill.BackgroundColor = XLColor.FromHtml("#EBF5FB");
                tRow++;
            }

            if (tRow > 3)
                ws.Range(3, 4, tRow - 1, 4).Style.NumberFormat.Format = "0\"%\"";

            for (int c = 1; c <= 8; c++) ws.Column(c).AdjustToContents();
        }

        using var stream = new MemoryStream();
        wb.SaveAs(stream);

        var fileName = $"Alle_Projekte_{DateTime.Now:yyyyMMdd}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // ── PDF Export (einzelnes Projekt) ────────────────────────────────────────
    [HttpGet("pdf/{projectId:int}")]
    public async Task<IActionResult> ExportPdf(int projectId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role   = User.FindFirstValue(ClaimTypes.Role)!;

        var project = await _db.Projects
            .Include(p => p.Tasks).ThenInclude(t => t.Assignee)
            .Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null) return NotFound();
        if (project.OwnerId != userId && role is not ("Admin" or "Teacher") && !project.Members.Any(m => m.UserId == userId))
            return Forbid();

        try
        {
            var pdfBytes = BuildProjectPdf(new List<Project> { project }, singleProject: true);
            var fileName = $"Projekt_{SanitizeFileName(project.Name)}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.GetType().Name, message = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ── PDF Export (alle Projekte) ────────────────────────────────────────────
    [HttpGet("pdf")]
    public async Task<IActionResult> ExportAllPdf()
    {
        var userId   = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var role     = User.FindFirstValue(ClaimTypes.Role)!;
        var projects = await LoadProjects(userId, role);

        var pdfBytes = BuildProjectPdf(projects, singleProject: false);
        var fileName = $"Alle_Projekte_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    // ── PDF Bauhelfer (QuestPDF) ──────────────────────────────────────────────
    private static byte[] BuildProjectPdf(List<Project> projects, bool singleProject)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var title = singleProject && projects.Count == 1 ? projects[0].Name : "Projektübersicht";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                // ── Header ────────────────────────────────────────────────
                page.Header().Element(header =>
                {
                    header.Column(col =>
                    {
                        col.Item().Background("#1A2E4A").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Text(title).FontSize(16).Bold().FontColor("#FFFFFF");
                            row.AutoItem().AlignRight().Text($"Export: {DateTime.Now:dd.MM.yyyy}").FontSize(8).FontColor("#AAAAAA");
                        });
                    });
                });

                // ── Content ───────────────────────────────────────────────
                page.Content().PaddingTop(10).Column(col =>
                {
                    foreach (var project in projects)
                    {
                        int progress = project.Tasks.Any() ? (int)project.Tasks.Average(t => t.Progress) : 0;

                        // Projekttitel (bei mehreren)
                        if (!singleProject)
                        {
                            col.Item().PaddingTop(16).Text(project.Name)
                                .FontSize(13).Bold().FontColor("#1A2E4A");
                        }

                        // Info-Grid
                        col.Item().PaddingTop(6).Table(info =>
                        {
                            info.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1); c.RelativeColumn(3); });

                            void InfoCell(string label, string value)
                            {
                                info.Cell().Background("#EBF5FB").Padding(5).Text(label).Bold().FontSize(8);
                                info.Cell().Padding(5).Text(value).FontSize(8);
                            }

                            InfoCell("Beschreibung", project.Description.Length > 100 ? project.Description[..100] + "…" : project.Description);
                            InfoCell("Fortschritt", $"{progress} %");
                            InfoCell("Zeitraum", $"{project.StartDate:dd.MM.yyyy} – {project.EndDate:dd.MM.yyyy}");
                            InfoCell("Eigentümer", project.Owner?.FullName ?? "-");

                            var members = string.Join(", ", project.Members.Select(m => m.User.FullName));
                            if (!string.IsNullOrEmpty(members))
                            {
                                InfoCell("Mitglieder", members.Length > 100 ? members[..100] + "…" : members);
                                info.Cell().ColumnSpan(2).Text(""); // fill row
                            }
                        });

                        // Fortschrittsbalken
                        col.Item().PaddingTop(6).PaddingBottom(8).Height(10).Row(bar =>
                        {
                            if (progress > 0)
                                bar.RelativeItem(progress).Background("#2D9CDB");
                            if (progress < 100)
                                bar.RelativeItem(100 - progress).Background("#DDDDDD");
                        });

                        // Tasks-Tabelle
                        if (project.Tasks.Any())
                        {
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);  // Aufgabe
                                    c.RelativeColumn(1.2f); // Status
                                    c.RelativeColumn(1);  // Priorität
                                    c.RelativeColumn(1);  // Fortschritt
                                    c.RelativeColumn(1.1f); // Start
                                    c.RelativeColumn(1.1f); // Ende
                                    c.RelativeColumn(1.5f); // Zugewiesen
                                    c.RelativeColumn(0.8f); // Meilenstein
                                });

                                // Header
                                string[] headers = { "Aufgabe", "Status", "Priorität", "Fortschritt", "Start", "Ende", "Zugewiesen an", "Meilenstein" };
                                foreach (var h in headers)
                                {
                                    table.Header(th =>
                                    {
                                        // handled below via Cell
                                    });
                                }

                                // Header-Zellen manuell
                                foreach (var h in headers)
                                    table.Cell().Row(1).Background("#2D9CDB").Padding(5)
                                        .Text(h).Bold().FontSize(8).FontColor("#FFFFFF");

                                uint rowIdx = 2;
                                foreach (var task in project.Tasks.OrderBy(t => t.StartDate))
                                {
                                    var bg = task.Status switch
                                    {
                                        "Done"       => "#D5F5E3",
                                        "InProgress" => "#FEF9E7",
                                        "Blocked"    => "#FDEDEC",
                                        _            => rowIdx % 2 == 0 ? "#F5F7FA" : "#FFFFFF"
                                    };

                                    void TaskCell(string text)
                                        => table.Cell().Row(rowIdx).Background(bg).Padding(4).Text(text).FontSize(8);

                                    TaskCell(task.Title);
                                    TaskCell(task.Status);
                                    TaskCell(task.Priority);
                                    TaskCell($"{task.Progress} %");
                                    TaskCell(task.StartDate.ToString("dd.MM.yy"));
                                    TaskCell(task.EndDate.ToString("dd.MM.yy"));
                                    TaskCell(task.Assignee?.FullName ?? "-");
                                    TaskCell(task.IsMilestone ? "✓" : "");

                                    rowIdx++;
                                }
                            });
                        }
                        else
                        {
                            col.Item().PaddingTop(4).Text("Keine Aufgaben vorhanden.")
                                .FontSize(9).FontColor("#888888").Italic();
                        }

                        col.Item().PaddingTop(20).Text(""); // Abstand zwischen Projekten
                    }
                });

                // ── Footer ────────────────────────────────────────────────
                page.Footer().AlignRight().Text(txt =>
                {
                    txt.Span("Seite ").FontSize(8).FontColor("#888888");
                    txt.CurrentPageNumber().FontSize(8).FontColor("#888888");
                    txt.Span(" / ").FontSize(8).FontColor("#888888");
                    txt.TotalPages().FontSize(8).FontColor("#888888");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Hilfsmethoden ────────────────────────────────────────────────────────
    private static readonly char[] _invalidFileChars = System.IO.Path.GetInvalidFileNameChars();
    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c => _invalidFileChars.Contains(c) ? '_' : c)).Replace(' ', '_');

    private static string SanitizeSheetName(string name)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var safe    = string.Concat(name.Select(c => invalid.Contains(c) ? '_' : c));
        return safe.Length > 31 ? safe[..31] : safe;
    }
}
