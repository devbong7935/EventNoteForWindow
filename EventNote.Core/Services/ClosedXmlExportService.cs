using ClosedXML.Excel;
using EventNote.Core.Models;
using EventNote.Core.Text;

namespace EventNote.Core.Services;

/// <summary>ClosedXML 로 .xlsx 를 만든다. PC 에 엑셀이 설치돼 있지 않아도 동작한다.</summary>
public sealed class ClosedXmlExportService : IExcelExportService
{
    private const string HeaderColor = "#5A6472";
    private const string SubHeaderColor = "#E8ECF1";
    private const string TotalColor = "#FFF2CC";
    private const string MoneyFormat = "#,##0";

    /// <summary>식권을 쓰지 않는 행사에서는 식권 열을 아예 만들지 않는다.</summary>
    private static string[] ColumnsFor(CeremonyEvent ceremony) => ceremony.UsesMealTickets
        ? new[] { "No", "이름", "금액", "한글표기", "관계", "소속", "식권", "비고" }
        : new[] { "No", "이름", "금액", "한글표기", "관계", "소속", "비고" };

    public Task ExportAsync(CeremonyEvent ceremonyEvent, string filePath, CancellationToken cancellationToken = default)
        => ExportAsync(new[] { ceremonyEvent }, filePath, cancellationToken);

    public Task ExportAsync(IEnumerable<CeremonyEvent> events, string filePath, CancellationToken cancellationToken = default)
    {
        var snapshot = events.ToList();
        if (snapshot.Count == 0) throw new InvalidOperationException("내보낼 행사가 없습니다.");

        return Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ceremony in snapshot)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var sheet = workbook.Worksheets.Add(UniqueSheetName(ceremony, usedNames));
                WriteSheet(sheet, ceremony);
            }

            workbook.SaveAs(filePath);
        }, cancellationToken);
    }

    public string SuggestFileName(CeremonyEvent ceremonyEvent)
    {
        var name = $"{ceremonyEvent.EventDate:yyyyMMdd}_{ceremonyEvent.DisplayTitle}_하객명부.xlsx";
        return string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
    }

    private static void WriteSheet(IXLWorksheet sheet, CeremonyEvent ceremony)
    {
        var columns = ColumnsFor(ceremony);
        var width = columns.Length;

        // 식권 열이 빠지면 그 뒤 열이 한 칸씩 당겨진다.
        var ticketColumn = ceremony.UsesMealTickets ? 7 : 0;
        var noteColumn = ceremony.UsesMealTickets ? 8 : 7;

        var row = 1;

        // 제목
        var titleRange = sheet.Range(row, 1, row, width).Merge();
        titleRange.Value = ceremony.DisplayTitle;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 16;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Row(row).Height = 26;
        row++;

        // 행사 정보
        var info = $"종류: {ceremony.Category.ToDisplayName()}    " +
                   $"일자: {ceremony.DateDisplay}    " +
                   $"주최: {Or(ceremony.HostName, "-")}    " +
                   $"장소: {Or(ceremony.Venue, "-")}";
        var infoRange = sheet.Range(row, 1, row, width).Merge();
        infoRange.Value = info;
        infoRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        infoRange.Style.Font.FontColor = XLColor.FromHtml("#666666");
        row += 2;

        // 표 머리글
        var headerRow = row;
        for (var i = 0; i < width; i++) sheet.Cell(row, i + 1).Value = columns[i];
        var headerRange = sheet.Range(row, 1, row, width);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(HeaderColor);
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        // 하객 목록
        var firstDataRow = row;
        var no = 1;
        foreach (var guest in ceremony.Guests)
        {
            sheet.Cell(row, 1).Value = no++;
            sheet.Cell(row, 2).Value = guest.Name;
            sheet.Cell(row, 3).Value = (double)guest.Amount;
            sheet.Cell(row, 4).Value = KoreanCurrency.ToHangul(guest.Amount);
            sheet.Cell(row, 5).Value = guest.Relation;
            sheet.Cell(row, 6).Value = guest.Affiliation;
            if (ticketColumn > 0) sheet.Cell(row, ticketColumn).Value = guest.MealTickets;
            sheet.Cell(row, noteColumn).Value = guest.Note;
            row++;
        }

        var lastDataRow = Math.Max(row - 1, firstDataRow);
        if (ceremony.Guests.Count > 0)
        {
            var data = sheet.Range(firstDataRow, 1, lastDataRow, width);
            data.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            data.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sheet.Range(firstDataRow, 3, lastDataRow, 3).Style.NumberFormat.Format = MoneyFormat;
            sheet.Range(firstDataRow, 1, lastDataRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            if (ticketColumn > 0)
            {
                sheet.Range(firstDataRow, ticketColumn, lastDataRow, ticketColumn)
                    .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }

        // 합계 줄
        sheet.Cell(row, 1).Value = "합계";
        sheet.Range(row, 1, row, 2).Merge();
        sheet.Cell(row, 3).Value = (double)ceremony.TotalAmount;
        sheet.Cell(row, 3).Style.NumberFormat.Format = MoneyFormat;
        sheet.Cell(row, 4).Value = KoreanCurrency.ToHangul(ceremony.TotalAmount);
        sheet.Cell(row, 5).Value = $"{ceremony.Guests.Count} 명";
        if (ticketColumn > 0) sheet.Cell(row, ticketColumn).Value = ceremony.TotalTickets;
        var totalRange = sheet.Range(row, 1, row, width);
        totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml(TotalColor);
        totalRange.Style.Font.Bold = true;
        totalRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        totalRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row += 3;

        WriteSummary(sheet, ceremony, ref row);

        sheet.SheetView.FreezeRows(headerRow);
        sheet.Columns(1, width).AdjustToContents();
        sheet.Column(4).Width = Math.Max(sheet.Column(4).Width, 16);
        sheet.Column(noteColumn).Width = Math.Max(sheet.Column(noteColumn).Width, 20);
    }

    private static void WriteSummary(IXLWorksheet sheet, CeremonyEvent ceremony, ref int row)
    {
        sheet.Cell(row, 1).Value = "분류별 집계";
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 12;
        row++;

        var headers = ceremony.UsesMealTickets
            ? new[] { "분류", "인원수", "금액합계", "식권합계" }
            : new[] { "분류", "인원수", "금액합계" };
        for (var i = 0; i < headers.Length; i++) sheet.Cell(row, i + 1).Value = headers[i];
        var headerRange = sheet.Range(row, 1, row, headers.Length);
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml(SubHeaderColor);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row++;

        var first = row;
        foreach (var line in EventSummary.ByRelation(ceremony.Guests))
        {
            sheet.Cell(row, 1).Value = line.Relation;
            sheet.Cell(row, 2).Value = line.Count;
            sheet.Cell(row, 3).Value = (double)line.Amount;
            if (ceremony.UsesMealTickets) sheet.Cell(row, 4).Value = line.Tickets;
            row++;
        }

        if (row > first)
        {
            var body = sheet.Range(first, 1, row - 1, headers.Length);
            body.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
            body.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            sheet.Range(first, 3, row - 1, 3).Style.NumberFormat.Format = MoneyFormat;
        }

        row++;
        WriteTotalLine(sheet, ref row, "전체 금액", $"{ceremony.TotalAmount:#,##0} 원", "#FADBD8");
        WriteTotalLine(sheet, ref row, "전체 인원", $"{ceremony.Guests.Count:#,##0} 명", "#D6EAF8");

        // 식권을 안 쓰는 행사면 식권 · 식대 차감 줄은 뺀다. 화면 타일과 같은 규칙이다.
        if (ceremony.UsesMealTickets)
        {
            WriteTotalLine(sheet, ref row, "전체 식권", $"{ceremony.TotalTickets:#,##0} 장", "#D5F5E3");
            WriteTotalLine(sheet, ref row, "총 (식대 차감)", $"{ceremony.NetAmount:#,##0} 원", "#FDEBD0");
        }
    }

    private static void WriteTotalLine(IXLWorksheet sheet, ref int row, string label, string value, string color)
    {
        sheet.Cell(row, 1).Value = label;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        var valueRange = sheet.Range(row, 2, row, 4).Merge();
        valueRange.Value = value;
        valueRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

        var line = sheet.Range(row, 1, row, 4);
        line.Style.Fill.BackgroundColor = XLColor.FromHtml(color);
        line.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        row++;
    }

    private static string UniqueSheetName(CeremonyEvent ceremony, HashSet<string> used)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var name = new string(ceremony.DisplayTitle.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        if (string.IsNullOrEmpty(name)) name = "행사";
        if (name.Length > 28) name = name[..28];

        var candidate = name;
        var index = 2;
        while (!used.Add(candidate)) candidate = $"{name}({index++})";
        return candidate;
    }

    private static string Or(string value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
