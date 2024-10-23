using System.Text.RegularExpressions;
using CommonNetFuncs.Core;
using DocumentFormat.OpenXml.Packaging;

namespace CommonNetFuncs.Word.OpenXml;

public static class Common
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public static bool ChangeUrlsInWordDoc(Stream fileStream, string newUrl, string urlToReplace, bool replaceAll = true)
    {
        bool success = false;
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
        try
        {
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    if (hyperlink.Uri.ToString().StrEq(urlToReplace))
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(newUrl), true, hyperlink.Id);
                    }

                    if (!replaceAll)
                    {
                        break;
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
        }
        finally
        {
            wordDoc.Dispose();
        }
        return success;
    }

    public static bool ChangeUrlsInWordDocRegex(Stream fileStream, string regexPattern, string replacementText, bool replaceAll = true)
    {
        bool success = false;
        using WordprocessingDocument wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
        try
        {
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                Regex regex = new(regexPattern);
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    string currentUri = hyperlink.Uri.ToString();
                    if (regex.Matches(currentUri).AnyFast())
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(Regex.Replace(currentUri, regexPattern, replacementText)), true, hyperlink.Id);
                    }

                    if (!replaceAll)
                    {
                        break;
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {ex.GetLocationOfException()}");
        }
        finally
        {
            wordDoc.Dispose();
        }
        return success;
    }
}
