<<<<<<< HEAD
﻿using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace CommonNetFuncs.Word.OpenXml;

public static class ChangeUrls
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="newUrl">URL to replace the URL in urlToReplace</param>
    /// <param name="urlToReplace">The URL to be replaced by the URL in newUrl</param>
    /// <param name="replaceAll">Optional: Replaces all instances of urlToReplace with newUrl when true, otherwise replaces only the first instance of urlToReplace found</param>
    /// <returns>True when no error is thrown</returns>
    public static bool ChangeUrlsInWordDoc(Stream fileStream, string newUrl, string urlToReplace, bool replaceAll = true)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    if (string.Equals(hyperlink.Uri.ToString(), urlToReplace, StringComparison.InvariantCultureIgnoreCase))
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(newUrl), true, hyperlink.Id);

                        if (!replaceAll)
                        {
                            break;
                        }
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
            wordDoc.Save();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDoc)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    ///  Looks for specific URLs in an existing word document and replaces it with the paired replacement URL
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="urlsToUpdate">Dictionary containing URL to find / Replacement URL pairs where the Key is the URL to be replaced, and the Value is the URL to used as the replacement</param>
    /// <returns>True when no error is thrown</returns>
    public static bool ChangeUrlsInWordDoc(Stream fileStream, Dictionary<string, string> urlsToUpdate)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    string? newUrl = urlsToUpdate.Where(x => string.Equals(x.Key, hyperlink.Uri.ToString(), StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();
                    if (newUrl != null)
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(newUrl), true, hyperlink.Id);
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDoc)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="regexPattern">Regex pattern used to match with the part of the URL that you want to replace</param>
    /// <param name="replacementText">Text to replace the matched regex text with in the URl</param>
    /// <param name="replaceAll">Replace all URLs that are matched with regexPattern</param>
    /// <returns>True when no error is thrown</returns>
    public static bool ChangeUrlsInWordDocRegex(Stream fileStream, string regexPattern, string replacementText, bool replaceAll = true, RegexOptions regexOptions = RegexOptions.IgnoreCase, TimeSpan? regexTimeout = null)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                regexTimeout ??= Regex.InfiniteMatchTimeout;
                Regex regex = new(regexPattern, regexOptions, (TimeSpan)regexTimeout);
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    string currentUri = hyperlink.Uri.ToString();
                    if (regex.Matches(currentUri).Count > 0)
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(Regex.Replace(currentUri, regexPattern, replacementText, regexOptions, (TimeSpan)regexTimeout)), true, hyperlink.Id);

                        if (!replaceAll)
                        {
                            break;
                        }
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDocRegex)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="urlsToUpdate">Dictionary containing regex pattern / replacement text pairs where the Key is the regex pattern to match, and the Value is the text to replace the text that matches the paired regex pattern</param>
    /// <param name="replaceAll">Replace all URLs that are matched with regexPattern</param>
    /// <returns>True when no error is thrown</returns>
    public static bool ChangeUrlsInWordDocRegex(Stream fileStream, Dictionary<string, string> urlsToUpdate, bool replaceAll = true, RegexOptions regexOptions = RegexOptions.IgnoreCase, TimeSpan? regexTimeout = null)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                regexTimeout ??= Regex.InfiniteMatchTimeout;
                foreach (KeyValuePair<string, string> item in urlsToUpdate)
                {
                    Regex regex = new(item.Key, regexOptions, (TimeSpan)regexTimeout);
                    foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                    {
                        string currentUri = hyperlink.Uri.ToString();
                        if (regex.Matches(currentUri).Count > 0)
                        {
                            mainPart.DeleteReferenceRelationship(hyperlink);
                            mainPart.AddHyperlinkRelationship(new Uri(Regex.Replace(currentUri, item.Key, item.Value, regexOptions, (TimeSpan)regexTimeout)), true, hyperlink.Id);

                            if (!replaceAll)
                            {
                                break;
                            }
                        }
                    }
                    //mainPart.Document.Save();
                }
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDocRegex)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }
}
=======
﻿using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace CommonNetFuncs.Word.OpenXml;

public static class ChangeUrls
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="newUrl">URL to replace the URL in urlToReplace</param>
    /// <param name="urlToReplace">The URL to be replaced by the URL in newUrl</param>
    /// <param name="replaceAll">Optional: Replaces all instances of urlToReplace with newUrl when true, otherwise replaces only the first instance of urlToReplace found</param>
    /// <returns><see langword="true"/> when no error is thrown</returns>
    public static bool ChangeUrlsInWordDoc(Stream fileStream, string newUrl, string urlToReplace, bool replaceAll = true)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    if (string.Equals(hyperlink.Uri.ToString(), urlToReplace, StringComparison.InvariantCultureIgnoreCase))
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(newUrl), true, hyperlink.Id);

                        if (!replaceAll)
                        {
                            break;
                        }
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
            wordDoc.Save();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDoc)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    ///  Looks for specific URLs in an existing word document and replaces it with the paired replacement URL
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="urlsToUpdate">Dictionary containing URL to find / Replacement URL pairs where the Key is the URL to be replaced, and the Value is the URL to used as the replacement</param>
    /// <returns><see langword="true"/> when no error is thrown</returns>
    public static bool ChangeUrlsInWordDoc(Stream fileStream, Dictionary<string, string> urlsToUpdate)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    string? newUrl = urlsToUpdate.Where(x => string.Equals(x.Key, hyperlink.Uri.ToString(), StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value).FirstOrDefault();
                    if (newUrl != null)
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(newUrl), true, hyperlink.Id);
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDoc)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="regexPattern">Regex pattern used to match with the part of the URL that you want to replace</param>
    /// <param name="replacementText">Text to replace the matched regex text with in the URl</param>
    /// <param name="replaceAll">Replace all URLs that are matched with regexPattern</param>
    /// <returns><see langword="true"/> when no error is thrown</returns>
    public static bool ChangeUrlsInWordDocRegex(Stream fileStream, string regexPattern, string replacementText, bool replaceAll = true, RegexOptions regexOptions = RegexOptions.IgnoreCase, TimeSpan? regexTimeout = null)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                regexTimeout ??= Regex.InfiniteMatchTimeout;
                Regex regex = new(regexPattern, regexOptions, (TimeSpan)regexTimeout);
                foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                {
                    string currentUri = hyperlink.Uri.ToString();
                    if (regex.Matches(currentUri).Count > 0)
                    {
                        mainPart.DeleteReferenceRelationship(hyperlink);
                        mainPart.AddHyperlinkRelationship(new Uri(Regex.Replace(currentUri, regexPattern, replacementText, regexOptions, (TimeSpan)regexTimeout)), true, hyperlink.Id);

                        if (!replaceAll)
                        {
                            break;
                        }
                    }
                }
                //mainPart.Document.Save();
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDocRegex)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }

    /// <summary>
    /// Looks for a specific URL in an existing word document and replaces it with the specified replacement URL. Can be configured to replace only the first instance or all instances of the URL to replace.
    /// </summary>
    /// <param name="fileStream">Stream containing the word document data</param>
    /// <param name="urlsToUpdate">Dictionary containing regex pattern / replacement text pairs where the Key is the regex pattern to match, and the Value is the text to replace the text that matches the paired regex pattern</param>
    /// <param name="replaceAll">Replace all URLs that are matched with regexPattern</param>
    /// <returns><see langword="true"/> when no error is thrown</returns>
    public static bool ChangeUrlsInWordDocRegex(Stream fileStream, Dictionary<string, string> urlsToUpdate, bool replaceAll = true, RegexOptions regexOptions = RegexOptions.IgnoreCase, TimeSpan? regexTimeout = null)
    {
        bool success = false;
        WordprocessingDocument? wordDoc = null;
        try
        {
            wordDoc = WordprocessingDocument.Open(fileStream, true, new() { AutoSave = true });
            MainDocumentPart? mainPart = wordDoc.MainDocumentPart;
            if (mainPart != null)
            {
                regexTimeout ??= Regex.InfiniteMatchTimeout;
                foreach (KeyValuePair<string, string> item in urlsToUpdate)
                {
                    Regex regex = new(item.Key, regexOptions, (TimeSpan)regexTimeout);
                    foreach (HyperlinkRelationship hyperlink in mainPart.HyperlinkRelationships.ToList())
                    {
                        string currentUri = hyperlink.Uri.ToString();
                        if (regex.Matches(currentUri).Count > 0)
                        {
                            mainPart.DeleteReferenceRelationship(hyperlink);
                            mainPart.AddHyperlinkRelationship(new Uri(Regex.Replace(currentUri, item.Key, item.Value, regexOptions, (TimeSpan)regexTimeout)), true, hyperlink.Id);

                            if (!replaceAll)
                            {
                                break;
                            }
                        }
                    }
                    //mainPart.Document.Save();
                }
                success = true;
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"Error in {nameof(ChangeUrls)}.{nameof(ChangeUrlsInWordDocRegex)}");
        }
        finally
        {
            wordDoc?.Dispose();
        }
        return success;
    }
}
>>>>>>> 270705e4f794428a4927e32ef23496c0001e47e7
