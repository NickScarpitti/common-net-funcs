using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace CommonNetCoreFuncs.Tools
{
    public static class ClosedXmlCommonHelpers
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

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

        public static bool IsCellEmpty(this IXLCell cell)
        {
            if (string.IsNullOrWhiteSpace(cell.Value.ToString()))
            {
                return true;
            }
            return false;
        }

        public static bool WriteExcelFile(IXLWorkbook wb, string path)
        {
            try
            {
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
                {
                    wb.SaveAs(fs);
                }
                wb.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "");
                return false;
            }
        }

        public static string GetSafeDate(string dateFormat)
        {
            return DateTime.Today.ToString(dateFormat).Replace("/", "-");
        }

        public static IXLStyle GetStyle(Styles style, IXLWorkbook wb, bool cellLocked = false, string htmlColor = null, IXLFont font = null, XLAlignmentHorizontalValues? alignment = null)
        {
            IXLStyle cellStyle = CreateEmptyStyle();
            switch (style)
            {
                case Styles.Header:
                    cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center; //Center
                    cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                    cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                    cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                    cellStyle.Border.TopBorder = XLBorderStyleValues.Thin;
                    cellStyle.Fill.BackgroundColor = XLColor.LightGray; //XLColor.FromArgb(140, 140, 140);
                    cellStyle.Font = GetFont(Fonts.Header, wb);
                    break;

                case Styles.Body:
                    cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                    cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                    cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                    cellStyle.Fill.BackgroundColor = XLColor.NoColor; //NPOI.HSSF.Util.HSSFColor.COLOR_NORMAL;
                    cellStyle.Font = GetFont(Fonts.Default, wb);
                    break;

                case Styles.Error:
                    cellStyle.Fill.BackgroundColor = XLColor.Red; //NPOI.HSSF.Util.HSSFColor.Red.Index;
                    cellStyle.Fill.PatternType = XLFillPatternValues.Solid; //FillPattern.SolidForeground;
                    break;

                case Styles.Custom:
                    IXLStyle xStyle = wb.Style;
                    if (alignment != null) { xStyle.Alignment.Horizontal = (XLAlignmentHorizontalValues)alignment; }
                    xStyle.Fill.BackgroundColor = XLColor.FromHtml(htmlColor);
                    xStyle.Fill.PatternType = XLFillPatternValues.Solid;
                    if (font != null) { xStyle.Font = font; }
                    cellStyle = xStyle;
                    break;

                default:
                    break;
            }
            cellStyle.Protection.Locked = cellLocked;
            return cellStyle;
        }

        /// <summary>
        /// Creates new instance of a IXLStyle object with reflection to avoid using the same reference to the existing workbook style
        /// </summary>
        /// <returns>Empty IXLStyle object</returns>
        private static IXLStyle CreateEmptyStyle()
        {
            var t = typeof(ClosedXML.Excel.XLConstants).Assembly.GetType("ClosedXML.Excel.XLStyle");
            MethodInfo m = t?.GetMethod("CreateEmptyStyle", BindingFlags.Static | BindingFlags.NonPublic);
            var o = m?.Invoke(null, null);
            return o as IXLStyle;
        }

        public static IXLFont GetFont(Fonts font, IXLWorkbook wb)
        {
            IXLFont cellFont = wb.Style.Font;
            switch (font)
            {
                case Fonts.Default:
                    cellFont.Bold = false;
                    cellFont.FontSize = 10;
                    cellFont.FontName = "Calibri";
                    break;

                case Fonts.Header:
                    cellFont.Bold = true;
                    cellFont.FontSize = 10;
                    cellFont.FontName = "Calibri";
                    break;

                default:
                    break;
            }
            return cellFont;
        }

        public static bool ExportFromTable<T>(IXLWorkbook wb, IXLWorksheet ws, List<T> data)
        {
            try
            {
                if (data != null)
                {
                    if (data.Count > 0)
                    {
                        IXLStyle headerStyle = GetStyle(Styles.Header, wb);
                        IXLStyle bodyStyle = GetStyle(Styles.Body, wb);

                        int x = 1;
                        int y = 1;

                        var header = data[0];
                        var props = header.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            IXLCell c = ws.Cell(y, x);
                            c.Value = prop.Name.ToString();
                            c.Style = headerStyle;
                            c.Style.Fill.BackgroundColor = headerStyle.Fill.BackgroundColor;
                            x++;
                        }
                        x = 1;
                        y++;

                        foreach (var item in data)
                        {
                            var props2 = item.GetType().GetProperties();
                            foreach (var prop in props2)
                            {
                                var val = prop.GetValue(item) ?? string.Empty;
                                IXLCell c = ws.Cell(y, x);
                                c.Value = val.ToString();
                                c.Style = bodyStyle;
                                x++;
                            }
                            x = 1;
                            y++;
                        }

                        ws.Range(1, 1, 1, props.Length - 1).SetAutoFilter();

                        foreach (var prop in props)
                        {
                            ws.Column(x).AdjustToContents();
                            x++;
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "");
                return false;
            }
        }

        public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, IXLWorkbook wb)
        {
            MemoryStream tempStream = new();
            SaveOptions options = new()
            {
                EvaluateFormulasBeforeSaving = true,
                ValidatePackage = true,
                GenerateCalculationChain = true
            };

            wb.SaveAs(tempStream, options);
            await tempStream.FlushAsync();
            tempStream.Seek(0, SeekOrigin.Begin);
            await tempStream.CopyToAsync(memoryStream);
            await tempStream.DisposeAsync();
            await memoryStream.FlushAsync();
            memoryStream.Seek(0, SeekOrigin.Begin);
        }

        //Corrupts excel file as is
        //public static void AddImages(this IXLWorkbook wb, List<byte[]> imageData, List<string> cellNames)
        //{
        //    if (wb != null && imageData.Count > 0 && cellNames.Count > 0 && imageData.Count == cellNames.Count)
        //    {
        //        IXLWorksheet ws = null;
        //        for (int i = 0; i < imageData.Count; i++)
        //        {
        //            if (imageData[i].Length > 0 && wb != null && cellNames[i] != null)
        //            {
        //                IXLCell cell = wb.Cell(cellNames[i]);
        //                IXLRange range = wb.Range(cellNames[i]);
        //                if (cell != null && range != null)
        //                {
        //                    if (ws == null)
        //                    {
        //                        ws = cell.Worksheet;
        //                    }

        //                    int imgWidth;
        //                    int imgHeight;
        //                    IXLPicture picture;
        //                    using (MemoryStream ms = new(imageData[i]))
        //                    {
        //                        using Image img = Image.FromStream(ms);
        //                        imgWidth = img.Width;
        //                        imgHeight = img.Height;
        //                        picture = ws.AddPicture(ms);
        //                    }

        //                    double imgAspect = (double)imgWidth / imgHeight;
        //                    double rangeWidth = ws.GetRangeWidthInPx(range.FirstColumn().ColumnNumber(), range.LastColumn().ColumnNumber());
        //                    double rangeHeight = ws.GetRangeHeightInPx(range.FirstRow().RowNumber(), range.LastRow().RowNumber());
        //                    double rangeAspect = (double)rangeWidth / rangeHeight;

        //                    double scale;

        //                    if (rangeAspect < imgAspect)
        //                    {
        //                        scale = (rangeWidth - 3.0) / imgWidth; //Set to width of cell -3px
        //                    }
        //                    else
        //                    {
        //                        scale = (rangeHeight - 3.0) / imgHeight; //Set to width of cell -3px
        //                    }
        //                    int resizeWidth = (int)Math.Round(imgWidth * scale, 0, MidpointRounding.ToZero);
        //                    int resizeHeight = (int)Math.Round(imgHeight * scale, 0, MidpointRounding.ToZero);
        //                    int xMargin = (int)Math.Round((rangeWidth - resizeWidth) / 2.0, 0, MidpointRounding.ToZero);
        //                    int yMargin = (int)Math.Round((rangeHeight - resizeHeight) / 2.0, 0, MidpointRounding.ToZero);

        //                    picture.Scale(scale);
        //                    picture.MoveTo(ws.Cell(range.FirstRow().RowNumber(), range.FirstColumn().ColumnNumber()), new Point(xMargin, yMargin));
        //                }
        //            }
        //        }
        //    }
        //}

        public static double GetRangeWidthInPx(this IXLWorksheet ws, int startCol, int endCol)
        {
            if (startCol > endCol)
            {
                int endTemp = startCol;
                startCol = endCol;
                endCol = endTemp;
            }

            double totalWidth = 0;
            for (int i = startCol; i < endCol + 1; i++)
            {
                totalWidth += (ws.Column(i).Width - 1) * 7 + 12;
            }
            return totalWidth;
        }

        public static double GetRangeHeightInPx(this IXLWorksheet ws, int startRow, int endRow)
        {
            if (startRow > endRow)
            {
                int endTemp = startRow;
                startRow = endRow;
                endRow = endTemp;
            }

            double totaHeight = 0;
            for (int i = startRow; i < endRow + 1; i++)
            {
                totaHeight += ws.Row(i).Height;
            }
            return totaHeight;
        }
    }
}