using CommonNetFuncs.Web.Common;

namespace Web.Common.Tests;

public class ContentTypesTests
{
	#region GetContentType Tests

	[Fact]
	public void GetContentType_NullFileName_ThrowsArgumentException()
	{
		// Arrange
		string? fileName = null;

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => fileName!.GetContentType());
		ex.ParamName.ShouldBe("fileName");
	}

	[Fact]
	public void GetContentType_EmptyFileName_ThrowsArgumentException()
	{
		// Arrange
		string fileName = string.Empty;

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => fileName.GetContentType());
		ex.ParamName.ShouldBe("fileName");
	}

	[Theory]
	[InlineData("stream.ts", ContentTypes.TransportStream)]
	[InlineData("video.mpeg", ContentTypes.Mpeg)]
	[InlineData("video.mpg", ContentTypes.Mpeg)]
	[InlineData("movie.mp4", ContentTypes.Mp4)]
	[InlineData("clip.webm", ContentTypes.Webm)]
	[InlineData("video.flv", ContentTypes.Flv)]
	[InlineData("video.avi", ContentTypes.Avi)]
	[InlineData("audio.mp3", ContentTypes.Mp3)]
	[InlineData("sound.aac", ContentTypes.Aac)]
	[InlineData("page.html", ContentTypes.Html)]
	[InlineData("page.htm", ContentTypes.Html)]
	[InlineData("document.txt", ContentTypes.Text)]
	[InlineData("script.js", ContentTypes.Js)]
	[InlineData("style.css", ContentTypes.Css)]
	[InlineData("data.xml", ContentTypes.Xml)]
	[InlineData("spreadsheet.xlsx", ContentTypes.Xlsx)]
	[InlineData("oldspreadsheet.xls", ContentTypes.Xls)]
	[InlineData("document.docx", ContentTypes.Docx)]
	[InlineData("olddocument.doc", ContentTypes.Doc)]
	[InlineData("presentation.pptx", ContentTypes.Pptx)]
	[InlineData("oldpresentation.ppt", ContentTypes.Ppt)]
	[InlineData("document.pdf", ContentTypes.Pdf)]
	[InlineData("data.csv", ContentTypes.Csv)]
	[InlineData("archive.zip", ContentTypes.Zip)]
	[InlineData("upload.mszipupload", ContentTypes.Zip)]
	[InlineData("document.json", ContentTypes.Json)]
	[InlineData("data.mempack", ContentTypes.MemPack)]
	[InlineData("message.msgpack", ContentTypes.MsgPack)]
	[InlineData("image.png", ContentTypes.Png)]
	[InlineData("photo.jpg", ContentTypes.Jpeg)]
	[InlineData("picture.jpeg", ContentTypes.Jpeg)]
	[InlineData("animation.gif", ContentTypes.Gif)]
	[InlineData("bitmap.bmp", ContentTypes.Bmp)]
	[InlineData("image.tiff", ContentTypes.Tiff)]
	[InlineData("vector.svg", ContentTypes.Svg)]
	[InlineData("Document.JSON", ContentTypes.Json)]
	[InlineData("Image.PNG", ContentTypes.Png)]
	[InlineData("Video.MP4", ContentTypes.Mp4)]
	[InlineData("path/to/document.json", ContentTypes.Json)]
	[InlineData("C:\\Users\\test\\image.png", ContentTypes.Png)]
	[InlineData("/var/www/page.html", ContentTypes.Html)]
	public void GetContentType_ValidMediaFiles_ReturnsCorrectContentType(string fileName, string expectedContentType)
	{
		// Act
		string result = fileName.GetContentType();

		// Assert
		result.ShouldBe(expectedContentType);
	}

	[Theory]
	[InlineData("unknown.xyz")]
	[InlineData("file.unknown")]
	[InlineData("test.abc123")]
	public void GetContentType_UnknownExtension_ReturnsOctetStream(string fileName)
	{
		// Act
		string result = fileName.GetContentType();

		// Assert
		result.ShouldBe(ContentTypes.BinaryStream);
	}

	#endregion

	#region GetContentTypeByExtension Tests

	[Fact]
	public void GetContentTypeByExtension_NullExtension_ThrowsArgumentException()
	{
		// Arrange
		string? extension = null;

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => ContentTypes.GetContentTypeByExtension(extension!));
		ex.ParamName.ShouldBe("extension");
	}

	[Fact]
	public void GetContentTypeByExtension_EmptyExtension_ThrowsArgumentException()
	{
		// Arrange
		string extension = string.Empty;

		// Act & Assert
		ArgumentException ex = Should.Throw<ArgumentException>(() => ContentTypes.GetContentTypeByExtension(extension));
		ex.ParamName.ShouldBe("extension");
	}

	[Theory]
	[InlineData(".json", ContentTypes.Json)]
	[InlineData("json", ContentTypes.Json)]
	[InlineData(".mempack", ContentTypes.MemPack)]
	[InlineData(".msgpack", ContentTypes.MsgPack)]
	[InlineData(".png", ContentTypes.Png)]
	[InlineData(".jpg", ContentTypes.Jpeg)]
	[InlineData(".jpeg", ContentTypes.Jpeg)]
	[InlineData(".gif", ContentTypes.Gif)]
	[InlineData(".bmp", ContentTypes.Bmp)]
	[InlineData(".tiff", ContentTypes.Tiff)]
	[InlineData(".svg", ContentTypes.Svg)]
	[InlineData(".xlsx", ContentTypes.Xlsx)]
	[InlineData(".xls", ContentTypes.Xls)]
	[InlineData(".docx", ContentTypes.Docx)]
	[InlineData(".doc", ContentTypes.Doc)]
	[InlineData(".pptx", ContentTypes.Pptx)]
	[InlineData(".ppt", ContentTypes.Ppt)]
	[InlineData(".pdf", ContentTypes.Pdf)]
	[InlineData(".csv", ContentTypes.Csv)]
	[InlineData(".zip", ContentTypes.Zip)]
	[InlineData(".mszipupload", ContentTypes.Zip)]
	[InlineData(".ts", ContentTypes.TransportStream)]
	[InlineData(".mpeg", ContentTypes.Mpeg)]
	[InlineData(".mpg", ContentTypes.Mpeg)]
	[InlineData(".mp4", ContentTypes.Mp4)]
	[InlineData(".webm", ContentTypes.Webm)]
	[InlineData(".flv", ContentTypes.Flv)]
	[InlineData(".avi", ContentTypes.Avi)]
	[InlineData(".mp3", ContentTypes.Mp3)]
	[InlineData(".aac", ContentTypes.Aac)]
	[InlineData(".html", ContentTypes.Html)]
	[InlineData(".htm", ContentTypes.Html)]
	[InlineData(".html5", ContentTypes.Html)]
	[InlineData(".htmlx", ContentTypes.Html)]
	[InlineData(".txt", ContentTypes.Text)]
	[InlineData(".text", ContentTypes.Text)]
	[InlineData(".text123", ContentTypes.Text)]
	[InlineData(".js", ContentTypes.Js)]
	[InlineData(".javascript", ContentTypes.Js)]
	[InlineData(".javascript-minified", ContentTypes.Js)]
	[InlineData(".css", ContentTypes.Css)]
	[InlineData(".stylesheet", ContentTypes.Css)]
	[InlineData(".stylesheet-min", ContentTypes.Css)]
	[InlineData(".xml", ContentTypes.Xml)]
	[InlineData(".xhtml", ContentTypes.Xml)]
	[InlineData(".xhtml5", ContentTypes.Xml)]
	[InlineData(".binary", ContentTypes.BinaryStream)]
	[InlineData(".octet-stream", ContentTypes.BinaryStream)]
	[InlineData(".binary-data", ContentTypes.BinaryStream)]
	[InlineData(".JSON", ContentTypes.Json)]
	[InlineData(".PNG", ContentTypes.Png)]
	[InlineData(".PDF", ContentTypes.Pdf)]
	[InlineData(".Mp4", ContentTypes.Mp4)]
	public void GetContentTypeByExtension_ReturnsCorrectContentType(string extension, string expectedContentType)
	{
		// Act
		string result = ContentTypes.GetContentTypeByExtension(extension);

		// Assert
		result.ShouldBe(expectedContentType);
	}

	[Theory]
	[InlineData(".unknown")]
	[InlineData(".xyz")]
	[InlineData(".random123")]
	[InlineData(".test")]
	public void GetContentTypeByExtension_UnknownExtension_ReturnsOctetStream(string extension)
	{
		// Act
		string result = ContentTypes.GetContentTypeByExtension(extension);

		// Assert
		result.ShouldBe("application/octet-stream");
	}

	#endregion

	#region Constants Tests

	[Fact]
	public void ContentTypeConstants_HaveExpectedValues()
	{
		// Assert
		ContentTypes.Json.ShouldBe("application/json");
		ContentTypes.JsonProblem.ShouldBe("application/problem+json");
		ContentTypes.MemPack.ShouldBe("application/x-memorypack");
		ContentTypes.MsgPack.ShouldBe("application/x-msgpack");
		ContentTypes.UrlEncodedFormData.ShouldBe("application/x-www-form-urlencoded");
		ContentTypes.MultiPartFormData.ShouldBe("multipart/form-data");
		ContentTypes.Png.ShouldBe("image/png");
		ContentTypes.Jpeg.ShouldBe("image/jpeg");
		ContentTypes.Gif.ShouldBe("image/gif");
		ContentTypes.Bmp.ShouldBe("image/bmp");
		ContentTypes.Tiff.ShouldBe("image/tiff");
		ContentTypes.Svg.ShouldBe("image/svg+xml");
		ContentTypes.Xlsx.ShouldBe("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
		ContentTypes.Xls.ShouldBe("application/vnd.ms-excel");
		ContentTypes.Docx.ShouldBe("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
		ContentTypes.Doc.ShouldBe("application/msword");
		ContentTypes.Pptx.ShouldBe("application/vnd.openxmlformats-officedocument.presentationml.presentation");
		ContentTypes.Ppt.ShouldBe("application/vnd.ms-powerpoint");
		ContentTypes.Pdf.ShouldBe("application/pdf");
		ContentTypes.Csv.ShouldBe("text/csv");
		ContentTypes.Zip.ShouldBe("application/zip");
		ContentTypes.MsZipUpload.ShouldBe("application/x-zip-compressed");
		ContentTypes.TransportStream.ShouldBe("video/mp2t");
		ContentTypes.Mpeg.ShouldBe("video/mpeg");
		ContentTypes.Mp4.ShouldBe("video/mp4");
		ContentTypes.Webm.ShouldBe("video/webm");
		ContentTypes.Flv.ShouldBe("video/x-flv");
		ContentTypes.Avi.ShouldBe("video/x-msvideo");
		ContentTypes.Mp3.ShouldBe("audio/mpeg");
		ContentTypes.Aac.ShouldBe("audio/aac");
		ContentTypes.Html.ShouldBe("text/html");
		ContentTypes.Text.ShouldBe("text/plain");
		ContentTypes.Js.ShouldBe("text/javascript");
		ContentTypes.AppJs.ShouldBe("application/javascript");
		ContentTypes.Css.ShouldBe("text/css");
		ContentTypes.Xhtml.ShouldBe("application/xhtml+xml");
		ContentTypes.Xml.ShouldBe("application/xml");
		ContentTypes.BinaryStream.ShouldBe("application/octet-stream");
	}

	[Fact]
	public void FormDataTypes_ContainsExpectedValues()
	{
		// Assert
		ContentTypes.FormDataTypes.ShouldNotBeNull();
		ContentTypes.FormDataTypes.Length.ShouldBe(2);
		ContentTypes.FormDataTypes.ShouldContain(ContentTypes.UrlEncodedFormData);
		ContentTypes.FormDataTypes.ShouldContain(ContentTypes.MultiPartFormData);
	}

	#endregion
}

public class EncodingTypesTests
{
	[Fact]
	public void EncodingTypeConstants_HaveExpectedValues()
	{
		// Assert
		EncodingTypes.Identity.ShouldBe("identity");
		EncodingTypes.Brotli.ShouldBe("br");
		EncodingTypes.GZip.ShouldBe("gzip");
		EncodingTypes.Deflate.ShouldBe("deflate");
	}
}
