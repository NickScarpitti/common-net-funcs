using System.Reflection;
using ClosedXML.Excel;
using CommonNetFuncs.Excel.Common;

namespace CommonNetFuncs.Excel.ClosedXml;

///// <summary>
///// Methods to make reading and writing to an excel file easier using NPOI
///// </summary>
public static class Common
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Checks if cell is empty
    /// </summary>
    /// <param name="cell">Cell to check if empty</param>
    /// <returns>True if cell is empty</returns>
    public static bool IsCellEmpty(this IXLCell cell)
    {
        return string.IsNullOrWhiteSpace(cell.Value.ToString());
    }

    /// <summary>
    /// Writes an excel file to the specified path
    /// </summary>
    /// <param name="wb">Workbook to write to disk</param>
    /// <param name="path">Path to save the workbook to</param>
    /// <returns>True if write was successful</returns>
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
            logger.Error(ex, "{msg}", $"{nameof(Common)}.{nameof(WriteExcelFile)} Error");
            return false;
        }
    }

    /// <summary>
    /// Get cell style based on enum EStyle options
    /// </summary>
    /// <param name="style">Cell style to add to retrieve</param>
    /// <param name="wb">Workbook to add cell style to</param>
    /// <param name="cellLocked">Whether or not to lock the cells this style applies to</param>
    /// <param name="htmlColor">Background color in HTML format</param>
    /// <param name="font">Font to use for the cells this style applies to</param>
    /// <param name="alignment">Text alignment for the cells this style applies to</param>
    /// <returns>IXLStyle object containing all of the styling associated with the input EStyle option</returns>
    public static IXLStyle? GetStyle(EStyle style, IXLWorkbook wb, bool cellLocked = false, string? htmlColor = null, IXLFont? font = null, XLAlignmentHorizontalValues? alignment = null)
    {
        IXLStyle? cellStyle = CreateEmptyStyle();
        if (cellStyle == null)
        {
            return null;
        }
        switch (style)
        {
            case EStyle.Header:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.TopBorder = XLBorderStyleValues.Thin;
                cellStyle.Fill.BackgroundColor = XLColor.LightGray;
                cellStyle.Font = GetFont(EFont.Header, wb);
                break;

            case EStyle.Body:
                cellStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cellStyle.Border.BottomBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.LeftBorder = XLBorderStyleValues.Thin;
                cellStyle.Border.RightBorder = XLBorderStyleValues.Thin;
                cellStyle.Fill.BackgroundColor = XLColor.NoColor;
                cellStyle.Font = GetFont(EFont.Default, wb);
                break;

            case EStyle.Error:
                cellStyle.Fill.BackgroundColor = XLColor.Red;
                cellStyle.Fill.PatternType = XLFillPatternValues.Solid;
                break;

            case EStyle.Custom:
                IXLStyle xStyle = wb.Style;
                if (alignment != null)
                {
                    xStyle.Alignment.Horizontal = (XLAlignmentHorizontalValues)alignment;
                }
                if (!string.IsNullOrWhiteSpace(htmlColor))
                {
                    xStyle.Fill.BackgroundColor = XLColor.FromHtml(htmlColor);
                }
                xStyle.Fill.PatternType = XLFillPatternValues.Solid;
                if (font != null)
                {
                    xStyle.Font = font;
                }
                cellStyle = xStyle;
                break;
        }
        cellStyle.Protection.Locked = cellLocked;
        return cellStyle;
    }

    /// <summary>
    /// Creates new instance of a IXLStyle object with reflection to avoid using the same reference to the existing workbook style
    /// </summary>
    /// <returns>Empty IXLStyle object</returns>
    private static IXLStyle? CreateEmptyStyle()
    {
        Type type = typeof(XLConstants).Assembly.GetType("ClosedXML.Excel.XLStyle")!;
        MethodInfo methodInfo = type.GetMethod("CreateEmptyStyle", BindingFlags.Static | BindingFlags.NonPublic)!;
        return methodInfo?.Invoke(null, null) as IXLStyle;
    }

    /// <summary>
    /// Get font styling based on EFont option
    /// </summary>
    /// <param name="font">Font type to get</param>
    /// <param name="wb">Workbook to add font to</param>
    /// <returns>IXLFont object containing all of the styling associated with the input EFont option</returns>
    public static IXLFont GetFont(EFont font, IXLWorkbook wb)
    {
        IXLFont cellFont = wb.Style.Font;
        switch (font)
        {
            case EFont.Default:
                cellFont.Bold = false;
                cellFont.FontSize = 10;
                cellFont.FontName = "Calibri";
                break;

            case EFont.Header:
                cellFont.Bold = true;
                cellFont.FontSize = 10;
                cellFont.FontName = "Calibri";
                break;
        }
        return cellFont;
    }

    /// <summary>
    /// Writes excel file to a MemoryStream object
    /// </summary>
    /// <param name="memoryStream">Memory stream to write workbook data to</param>
    /// <param name="wb">Workbook to read into memory stream</param>
    public static async Task WriteFileToMemoryStreamAsync(this MemoryStream memoryStream, IXLWorkbook wb, CancellationToken cancellationToken = default)
    {
        await using MemoryStream tempStream = new();
        SaveOptions options = new()
        {
            EvaluateFormulasBeforeSaving = true,
            ValidatePackage = true,
            GenerateCalculationChain = true
        };

        wb.SaveAs(tempStream, options);
        await tempStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        tempStream.Position = 0;
        await tempStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
        await tempStream.DisposeAsync().ConfigureAwait(false);
        await memoryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        memoryStream.Position = 0;
    }
}
