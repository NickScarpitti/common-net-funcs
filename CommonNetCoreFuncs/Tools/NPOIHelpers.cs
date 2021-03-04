using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CommonNetCoreFuncs.Tools
{
    public static class NPOIHelpers
    {
        public enum Styles
        {
            Header,
            Body,
            Error,
            Custom
        }
        public enum Fonts
        {
            Default,
            Header,
            BigWhiteHeader
        }

        public static bool IsCellEmpty(this ICell cell)
        {
            if (string.IsNullOrWhiteSpace(cell.GetStringValue()))
            {
                return true;
            }
            return false;
        }
        public static ICell GetCellFromReference(this ISheet ws, string cellName, int colOffset = 0, int rowOffset = 0)
        {
            try
            {
                CellReference cr = new CellReference(cellName);
                IRow row = ws.GetRow(cr.Row + rowOffset);
                ICell cell = row.GetCell(cr.Col + colOffset);
                if (cell == null)
                {
                    cell = row.CreateCell(cr.Col + colOffset);
                }
                return cell;
            }
            catch (Exception)
            {
                return null;
            }

        }
        public static ICell GetCellFromCoordinates(this ISheet ws, int x, int y, int colOffset = 0, int rowOffset = 0)
        {
            try
            {
                IRow row = ws.GetRow(y + rowOffset);
                if (row == null)
                {
                    row = ws.CreateRow(y + rowOffset);
                }
                ICell cell = row.GetCell(x + colOffset);
                if (cell == null)
                {
                    cell = row.CreateCell(x + colOffset);
                }
                return cell;
            }
            catch (Exception)
            {
                return null;
            }

        }
        public static ICell GetCellFromName(this XSSFWorkbook wb, string cellName, int colOffset = 0, int rowOffset = 0)
        {
            try
            {
                IName name = wb.GetName(cellName);
                CellReference[] crs = new AreaReference(name.RefersToFormula).GetAllReferencedCells();
                ISheet ws = null;
                int rowNum = -1;
                int colNum = -1;
                for (int i = 0; i < crs.Length; i++)
                {
                    if (ws == null)
                    {
                        ws = wb.GetSheet(crs[i].SheetName);
                    }

                    if (rowNum == -1 || rowNum > crs[i].Row)
                    {
                        rowNum = crs[i].Row;
                    }

                    if (colNum == -1 || colNum > crs[i].Col)
                    {
                        colNum = crs[i].Col;
                    }
                }

                if (ws != null && colNum > -1 && rowNum > -1)
                {
                    IRow row = ws.GetRow(rowNum + rowOffset);
                    if (row == null)
                    {
                        row = ws.CreateRow(rowNum + rowOffset);
                    }
                    ICell cell = row.GetCell(colNum + colOffset);
                    if (cell == null)
                    {
                        cell = row.CreateCell(colNum + colOffset);
                    }
                    return cell;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }

        }
        public static void ClearAllFromName(this XSSFWorkbook wb, string cellName)
        {
            IName name = wb.GetName(cellName);
            CellReference[] crs = new AreaReference(name.RefersToFormula).GetAllReferencedCells();
            ISheet ws = wb.GetSheet(crs[0].SheetName);

            if (ws == null || crs.Length == 0 || name == null)
            {
                return;
            }

            for (int i = 0; i < crs.Length; i++)
            {
                IRow row = ws.GetRow(crs[i].Row);
                if (row != null)
                {
                    ICell cell = row.GetCell(crs[i].Col);
                    if (cell != null)
                    {
                        row.RemoveCell(cell);
                    }
                }
            }
        }
        public static ICell CreateCell(this IRow row, int col)
        {
            return row.CreateCell(col);
        }
        

        public static string MakeExportNameUnique(string savePath, string fileName, string extension)
        {
            int i = 0;
            string outputName = fileName;
            while (File.Exists(Path.Combine(savePath, outputName)))
            {
                outputName = $"{fileName.Left(fileName.Length - extension.Length)} ({i}).{extension}";
                i++;
            }
            return outputName;
        }

        public static bool WriteExcelFile(XSSFWorkbook wb, string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    wb.Write(fs);
                }
                wb.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static string GetSafeDate(string dateFormat) { return DateTime.Today.ToString(dateFormat).Replace("/", "-"); }
   
        /// <exception cref="Exception">Ignore.</exception>
        public static ICellStyle GetStyle(Styles style, XSSFWorkbook wb, bool cellLocked = false, string htmlColor = null, IFont font = null, NPOI.SS.UserModel.HorizontalAlignment? alignment = null)
        {
            ICellStyle cellStyle = wb.CreateCellStyle();
            switch (style)
            {
                case Styles.Header:
                    cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Grey25Percent.Index;
                    cellStyle.FillPattern = FillPattern.SolidForeground;
                    cellStyle.SetFont(GetFont(Fonts.Header, wb));
                    break;
                case Styles.Body:
                    cellStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;
                    cellStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;
                    cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.COLOR_NORMAL;
                    cellStyle.SetFont(GetFont(Fonts.Default, wb));
                    break;
                case Styles.Error:
                    cellStyle.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
                    cellStyle.FillPattern = FillPattern.SolidForeground;
                    break;
                case Styles.Custom:
                    XSSFCellStyle xStyle = (XSSFCellStyle)wb.CreateCellStyle();
                    if (alignment != null) { xStyle.Alignment = (NPOI.SS.UserModel.HorizontalAlignment)alignment; }
                    byte[] rgb = new byte[] { ColorTranslator.FromHtml(htmlColor).R, ColorTranslator.FromHtml(htmlColor).G, ColorTranslator.FromHtml(htmlColor).B };
                    xStyle.SetFillForegroundColor(new XSSFColor(rgb));
                    xStyle.FillPattern = FillPattern.SolidForeground;
                    if (font != null) { xStyle.SetFont(font); }
                    cellStyle = xStyle;
                    break;
                default:
                    break;
            }
            cellStyle.IsLocked = cellLocked;
            return cellStyle;
        }
        public static IFont GetFont(Fonts font, XSSFWorkbook wb)
        {
            IFont cellFont = wb.CreateFont();
            switch (font)
            {
                case Fonts.Default:
                    cellFont.IsBold = false;
                    cellFont.FontHeightInPoints = 10;
                    cellFont.FontName = "Calibri";
                    break;
                case Fonts.Header:
                    cellFont.IsBold = true;
                    cellFont.FontHeightInPoints = 10;
                    cellFont.FontName = "Calibri";
                    break;
                default:
                    break;
            }
            return cellFont;
        }
        public static bool ExportFromTable<T>(XSSFWorkbook wb, ISheet ws, List<T> data)
        {
            try
            {
                if (data != null)
                {
                    if (data.Count > 0)
                    {
                        ICellStyle headerStyle = GetStyle(Styles.Header, wb);
                        ICellStyle bodyStyle = GetStyle(Styles.Body, wb);

                        int x = 0;
                        int y = 0;

                        var header = data[0];
                        var props = header.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            ICell c = ws.GetCellFromCoordinates(x, y);
                            c.SetCellValue(prop.Name.ToString());
                            c.CellStyle = headerStyle;
                            x++;
                        }
                        x = 0;
                        y++;

                        foreach (var item in data)
                        {
                            var props2 = item.GetType().GetProperties();
                            foreach (var prop in props2)
                            {
                                var val = prop.GetValue(item) ?? string.Empty;
                                ICell c = ws.GetCellFromCoordinates(x, y);
                                c.SetCellValue(val.ToString());
                                c.CellStyle = bodyStyle;
                                x++;
                            }
                            x = 0;
                            y++;
                        }

                        ws.SetAutoFilter(new CellRangeAddress(0, 0, 0, props.Length - 1));

                        foreach (var prop in props)
                        {
                            ws.AutoSizeColumn(x);
                            x++;
                        }
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static string GetStringValue(this ICell c)
        {
            return c.CellType switch
            {
                CellType.Unknown => string.Empty,
                CellType.Numeric => c.NumericCellValue.ToString(),
                CellType.String => c.StringCellValue,
                CellType.Formula => c.CachedFormulaResultType switch
                {
                    CellType.Unknown => string.Empty,
                    CellType.Numeric => c.NumericCellValue.ToString(),
                    CellType.String => c.StringCellValue,
                    CellType.Blank => string.Empty,
                    CellType.Boolean => c.BooleanCellValue.ToString(),
                    CellType.Error => c.ErrorCellValue.ToString(),
                    _ => string.Empty,
                },
                CellType.Blank => string.Empty,
                CellType.Boolean => c.BooleanCellValue.ToString(),
                CellType.Error => c.ErrorCellValue.ToString(),
                _ => string.Empty,
            };
        }
    }
}
