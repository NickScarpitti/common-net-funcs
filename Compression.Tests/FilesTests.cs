using System.IO.Compression;
using static CommonNetFuncs.Compression.Files;
using static CommonNetFuncs.Compression.Streams;

namespace Compression.Tests;

public sealed class FilesTests
{
	private readonly Fixture fixture;

	public FilesTests()
	{
		fixture = new Fixture();
	}

	[Fact]
	public async Task ZipFile_Should_Create_Zip_With_Single_File()
	{
		// Arrange
		string fileName = fixture.Create<string>();
		MemoryStream fileStream = new(fixture.CreateMany<byte>(100).ToArray());
		MemoryStream zipFileStream = new();

		// Act
		await (fileStream, fileName).ZipFile(zipFileStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		zipFileStream.Length.ShouldBeGreaterThan(0);
		await using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Read);
		archive.Entries.Count.ShouldBe(1);
		archive.Entries[0].Name.ShouldBe(fileName);
	}

	[Fact]
	public async Task ZipFile_NullStream_Should_Create_Zip_With_Single_File()
	{
		// Arrange
		string fileName = fixture.Create<string>();
		await using MemoryStream fileStream = new(fixture.CreateMany<byte>(100).ToArray());

		// Act
		await using MemoryStream zipFileStream = await (fileStream, fileName).ZipFile(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		zipFileStream.Length.ShouldBeGreaterThan(0);
		await using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Read);
		archive.Entries.Count.ShouldBe(1);
		archive.Entries[0].Name.ShouldBe(fileName);
	}

	[Fact]
	public async Task ZipFiles_Should_Create_Zip_With_Multiple_Files()
	{
		// Arrange
		List<(Stream?, string)> files =
		[
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()), "file1.txt"),
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 1)).ToArray()), "file2.txt"),
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 2)).ToArray()), "file3.txt")
		];
		await using MemoryStream zipFileStream = new();

		// Act
		await files.ZipFiles(zipFileStream, cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		zipFileStream.Length.ShouldBeGreaterThan(0);
		await using ZipArchive archive = new(zipFileStream, ZipArchiveMode.Read);
		archive.Entries.Count.ShouldBe(files.Count);
		foreach ((Stream?, string fileName) file in files)
		{
			archive.Entries.ShouldContain(x => x.Name == file.fileName);
		}
	}

	[Fact]
	public async Task ZipFiles_Null_Stream_Should_Create_Zip_With_Multiple_Files()
	{
		// Arrange
		List<(Stream?, string)> files =
		[
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()), "file1.txt"),
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 1)).ToArray()), "file2.txt"),
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 2)).ToArray()), "file3.txt")
		];

		// Act
		await using MemoryStream? zipFileStream = await files.ZipFiles(cancellationToken: TestContext.Current.CancellationToken);

		// Assert
		zipFileStream?.Length.ShouldBeGreaterThan(0);
		await using ZipArchive archive = new(zipFileStream ?? new(), ZipArchiveMode.Read);
		archive.Entries.Count.ShouldBe(files.Count);
		foreach ((Stream?, string fileName) file in files)
		{
			archive.Entries.ShouldContain(x => x.Name == file.fileName);
		}
	}

	[Fact]
	public async Task AddFilesToZip_Should_Add_Files_To_Archive()
	{
		// Arrange
		List<(Stream?, string)> files =
		[
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray()), "file1.txt"),
			(new MemoryStream(Enumerable.Range(0, 100).Select(i => (byte)(i + 1)).ToArray()), "file2.txt")
		];
		await using MemoryStream memoryStream = new();
		await using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
		{
			// Act
			await files.AddFilesToZip(archive, cancellationToken: TestContext.Current.CancellationToken);
		}

		// Reopen the ZipArchive in Read mode to verify its contents
		await using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);

		// Assert
		readArchive.Entries.Count.ShouldBe(files.Count);
		foreach ((Stream?, string fileName) file in files)
		{
			readArchive.Entries.ShouldContain(x => x.Name == file.fileName);
		}
	}

	[Fact]
	public async Task AddFileToZip_Should_Add_Single_File_To_Archive()
	{
		// Arrange
		string fileName = fixture.Create<string>();
		await using MemoryStream fileStream = new(Enumerable.Range(0, 100).Select(i => (byte)i).ToArray());
		await using MemoryStream memoryStream = new();

		// Act
		await using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
		{
			await fileStream.AddFileToZip(archive, fileName, cancellationToken: TestContext.Current.CancellationToken);
		}

		// Assert
		await using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);
		readArchive.Entries.Count.ShouldBe(1);
		readArchive.Entries[0].Name.ShouldBe(fileName);
	}

	[Fact]
	public async Task AddFileToZip_Should_Handle_Null_Stream_Gracefully()
	{
		// Arrange
		Stream? fileStream = null;
		string fileName = fixture.Create<string>();
		await using MemoryStream memoryStream = new();

		// Act
		await using (ZipArchive archive = new(memoryStream, ZipArchiveMode.Create, true))
		{
			await fileStream.AddFileToZip(archive, fileName, cancellationToken: TestContext.Current.CancellationToken);
		}

		// Assert
		await using ZipArchive readArchive = new(memoryStream, ZipArchiveMode.Read, true);
		readArchive.Entries.Count.ShouldBe(0);
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task CompressFile_Should_Create_Compressed_File(ECompressionType compressionType)
	{
		// Arrange
		string tempInput = Path.GetTempFileName();
		string tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cmp");
		try
		{
			byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
			await File.WriteAllBytesAsync(tempInput, data, TestContext.Current.CancellationToken);

			// Act
			await CompressFile(tempInput, tempOutput, compressionType, TestContext.Current.CancellationToken);

			// Assert
			File.Exists(tempOutput).ShouldBeTrue();
			new FileInfo(tempOutput).Length.ShouldBeGreaterThan(0);
		}
		finally
		{
			if (File.Exists(tempInput))
			{
				File.Delete(tempInput);
			}

			if (File.Exists(tempOutput))
			{
				File.Delete(tempOutput);
			}
		}
	}

	[Fact]
	public async Task CompressFile_Should_Throw_If_Input_File_Does_Not_Exist()
	{
		// Arrange
		string nonExistent = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
		string output = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cmp");

		// Act & Assert
		FileNotFoundException ex = await Should.ThrowAsync<FileNotFoundException>(() => CompressFile(nonExistent, output, ECompressionType.Gzip));
		ex.Message.ShouldContain("Input file not found");
	}

	[Fact]
	public async Task CompressFile_Should_Create_Output_Directory_If_Not_Exists()
	{
		// Arrange
		string tempInput = Path.GetTempFileName();
		string outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string tempOutput = Path.Combine(outputDir, "out.cmp");
		try
		{
			await File.WriteAllTextAsync(tempInput, "test data", TestContext.Current.CancellationToken);

			// Act
			await CompressFile(tempInput, tempOutput, ECompressionType.Gzip, TestContext.Current.CancellationToken);

			// Assert
			File.Exists(tempOutput).ShouldBeTrue();
			Directory.Exists(outputDir).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempInput))
			{
				File.Delete(tempInput);
			}

			if (File.Exists(tempOutput))
			{
				File.Delete(tempOutput);
			}

			if (Directory.Exists(outputDir))
			{
				Directory.Delete(outputDir, true);
			}
		}
	}

	[Theory]
	[InlineData(ECompressionType.Gzip)]
	[InlineData(ECompressionType.Brotli)]
	[InlineData(ECompressionType.Deflate)]
	[InlineData(ECompressionType.ZLib)]
	public async Task DecompressFile_Should_Restore_Original_Data(ECompressionType compressionType)
	{
		// Arrange
		string tempInput = Path.GetTempFileName();
		string tempCompressed = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cmp");
		string tempOutput = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.out");
		try
		{
			byte[] data = Enumerable.Range(0, 1000).Select(i => (byte)i).ToArray();
			await File.WriteAllBytesAsync(tempInput, data, TestContext.Current.CancellationToken);

			await CompressFile(tempInput, tempCompressed, compressionType, TestContext.Current.CancellationToken);

			// Act
			await DecompressFile(tempCompressed, tempOutput, compressionType, TestContext.Current.CancellationToken);

			// Assert
			File.Exists(tempOutput).ShouldBeTrue();
			byte[] decompressed = await File.ReadAllBytesAsync(tempOutput, TestContext.Current.CancellationToken);
			decompressed.ShouldBe(data);
		}
		finally
		{
			if (File.Exists(tempInput))
			{
				File.Delete(tempInput);
			}

			if (File.Exists(tempCompressed))
			{
				File.Delete(tempCompressed);
			}

			if (File.Exists(tempOutput))
			{
				File.Delete(tempOutput);
			}
		}
	}

	[Fact]
	public async Task DecompressFile_Should_Throw_If_Compressed_File_Does_Not_Exist()
	{
		// Arrange
		string nonExistent = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cmp");
		string output = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.out");

		// Act & Assert
		FileNotFoundException ex = await Should.ThrowAsync<FileNotFoundException>(() => DecompressFile(nonExistent, output, ECompressionType.Gzip));
		ex.Message.ShouldContain("Compressed file not found");
	}

	[Fact]
	public async Task DecompressFile_Should_Create_Output_Directory_If_Not_Exists()
	{
		// Arrange
		string tempInput = Path.GetTempFileName();
		string tempCompressed = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.cmp");
		string outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string tempOutput = Path.Combine(outputDir, "out.txt");
		try
		{
			await File.WriteAllTextAsync(tempInput, "test data", TestContext.Current.CancellationToken);
			await CompressFile(tempInput, tempCompressed, ECompressionType.Gzip, TestContext.Current.CancellationToken);

			// Act
			await DecompressFile(tempCompressed, tempOutput, ECompressionType.Gzip, TestContext.Current.CancellationToken);

			// Assert
			File.Exists(tempOutput).ShouldBeTrue();
			Directory.Exists(outputDir).ShouldBeTrue();
		}
		finally
		{
			if (File.Exists(tempInput))
			{
				File.Delete(tempInput);
			}

			if (File.Exists(tempCompressed))
			{
				File.Delete(tempCompressed);
			}

			if (File.Exists(tempOutput))
			{
				File.Delete(tempOutput);
			}

			if (Directory.Exists(outputDir))
			{
				Directory.Delete(outputDir, true);
			}
		}
	}
}
