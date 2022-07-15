namespace CommonNetCoreFuncs.Tools;

public static class FileHelpers
{
    /// <summary>
    /// Simulates automatic Windows behavior of adding a number after the original file name when a file with the same name exists already
    /// </summary>
    /// <param name="originalFullFileName">Full path and file name</param>
    /// <returns></returns>
    public static string GetSafeSaveName(this string originalFullFileName)
    {
        string testPath = Path.GetFullPath(originalFullFileName);
        if (File.Exists(testPath))
        {
            //Update name
            string oldFileName = Path.GetFileName(originalFullFileName);
            string ext = Path.GetExtension(oldFileName);
            oldFileName = oldFileName.Replace(ext, null);
            string newFullFileName = originalFullFileName;
            int i = 0;
            while (File.Exists(testPath))
            {
                newFullFileName = originalFullFileName.Replace(oldFileName + ext, $"{oldFileName} ({i}){ext}");
                testPath = Path.GetFullPath(newFullFileName);
                i++;
            }
            return newFullFileName;
        }
        else
        {
            return originalFullFileName;
        }
    }
}
