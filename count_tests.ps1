$projects = @('FastMap.Tests', 'DeepClone.Tests', 'Hangfire.Tests', 'Images.Tests', 'Office.Common.Tests', 'Excel.OpenXml.Tests', 'Excel.Npoi.Tests', 'Excel.ClosedXml.Tests', 'Media.Ffmpeg.Tests', 'Sql.Common.Tests', 'Web.Jwt.Tests', 'Web.Ftp.Tests', 'Web.Common.Tests', 'Web.Interface.Tests', 'Web.Middleware.Tests', 'Web.Aws.S3.Tests', 'Word.OpenXml.Tests', 'Compression.Tests')

foreach ($proj in $projects) {
    $facts = (Get-ChildItem -Path $proj -Filter *.cs -Recurse | Select-String -Pattern '^\s*\[Fact\]' | Measure-Object).Count
    $theories = (Get-ChildItem -Path $proj -Filter *.cs -Recurse | Select-String -Pattern '^\s*\[Theory\]' | Measure-Object).Count
    $total = $facts + $theories
    Write-Output "$proj,$facts,$theories,$total"
}
