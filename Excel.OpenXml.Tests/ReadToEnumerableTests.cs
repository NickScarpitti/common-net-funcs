using System.Reflection;
using CommonNetFuncs.Excel.OpenXml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using xRetry.v3;
using static CommonNetFuncs.Excel.OpenXml.Common;

namespace Excel.OpenXml.Tests;

public sealed class ReadToEnumerableTests
{
	// ── Model types ──────────────────────────────────────────────────────────

	public class SimpleModel
	{
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	public class AllTypesModel
	{
		public string StringValue { get; set; } = string.Empty;
		public bool BoolValue { get; set; }
		public byte ByteValue { get; set; }
		public short ShortValue { get; set; }
		public int IntValue { get; set; }
		public long LongValue { get; set; }
		public float FloatValue { get; set; }
		public double DoubleValue { get; set; }
		public decimal DecimalValue { get; set; }
		public DateTime DateTimeValue { get; set; }
		public DateOnly DateOnlyValue { get; set; }
		public TimeOnly TimeOnlyValue { get; set; }
		public DateTimeOffset DateTimeOffsetValue { get; set; }
		public TimeSpan TimeSpanValue { get; set; }
		public Guid GuidValue { get; set; }
		public uint UintValue { get; set; } // hits the Convert.ChangeType fallback arm
	}

	public class NullableTypesModel
	{
		public int? NullableInt { get; set; }
		public DateOnly? NullableDateOnly { get; set; }
		public bool? NullableBool { get; set; }
	}

	public class ExcelColumnModel
	{
		[ExcelColumn("Full Name")]
		public string Name { get; set; } = string.Empty;
		public int Age { get; set; }
	}

	public class AttributePriorityModel
	{
		// "Points" header maps to PropA via attribute; the property name "PropA" also maps here
		[ExcelColumn("Points")]
		public int PropA { get; set; }
		public int OtherProp { get; set; }
	}

	public enum TestStatus { Active, Inactive }

	public class EnumModel
	{
		public TestStatus Status { get; set; }
	}

	// Uri is a reference type whose TypeCode is Object and which Convert.ChangeType cannot produce,
	// so assigning an invalid string exercises the exception-catch branch in SetExcelPropertyValue.
	public class ModelWithUri
	{
		public string Name { get; set; } = string.Empty;
		public Uri? Website { get; set; }
	}

	// ── Stream builder ───────────────────────────────────────────────────────

	/// <summary>
	/// Creates a valid in-memory .xlsx stream from a 2-D string array.
	/// Row 0 is the first worksheet row.  Empty strings produce no Cell element.
	/// </summary>
	private static MemoryStream BuildStream(string[,] data, string sheetName = "Sheet1", bool useSharedStrings = false)
	{
		// Create in a temporary MemoryStream; SpreadsheetDocument takes ownership and disposes it.
		// MemoryStream.ToArray() still works after disposal — it copies the underlying buffer.
		MemoryStream temp = new();
		using (SpreadsheetDocument doc = SpreadsheetDocument.Create(temp, SpreadsheetDocumentType.Workbook))
		{
			WorkbookPart wbp = doc.AddWorkbookPart();
			wbp.Workbook = new Workbook();
			WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
			SheetData sheetData = new();

			SharedStringTablePart? ssp = null;
			Dictionary<string, int>? ssCache = null;
			if (useSharedStrings)
			{
				ssp = wbp.AddNewPart<SharedStringTablePart>();
				ssp.SharedStringTable = new SharedStringTable();
				ssCache = [];
			}

			int rowCount = data.GetLength(0);
			int colCount = data.GetLength(1);
			for (int r = 0; r < rowCount; r++)
			{
				Row row = new() { RowIndex = (uint)(r + 1) };
				for (int c = 0; c < colCount; c++)
				{
					string val = data[r, c];
					if (string.IsNullOrEmpty(val)) continue;

					string cellRef = new CellReference((uint)(c + 1), (uint)(r + 1)).ToString();
					Cell cell = new() { CellReference = cellRef };

					if (useSharedStrings && ssp != null && ssCache != null)
					{
						if (!ssCache.TryGetValue(val, out int idx))
						{
							idx = ssCache.Count;
							ssp.SharedStringTable!.AppendChild(new SharedStringItem(new Text(val)));
							ssCache[val] = idx;
						}
						cell.CellValue = new CellValue(idx.ToString());
						cell.DataType = new EnumValue<CellValues>(CellValues.SharedString);
					}
					else
					{
						cell.CellValue = new CellValue(val);
					}
					row.Append(cell);
				}
				sheetData.Append(row);
			}

			wsp.Worksheet = new Worksheet(sheetData);
			Sheets sheets = wbp.Workbook.AppendChild(new Sheets());
			sheets.Append(new Sheet { Id = wbp.GetIdOfPart(wsp), SheetId = 1, Name = sheetName });
			wbp.Workbook.Save();
			ssp?.SharedStringTable?.Save();
		}
		return new MemoryStream(temp.ToArray());
	}

	// ── ExcelColumnAttribute ─────────────────────────────────────────────────

	[Fact]
	public void ExcelColumnAttribute_Constructor_SetsName()
	{
		ExcelColumnAttribute attr = new("My Column");
		attr.Name.ShouldBe("My Column");
	}

	// ── ReadExcelFileToEnumerable ─────────────────────────────────────────────

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_WithHeaders_ReturnsCorrectRows()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name", "Age" },
			{ "Alice", "30" },
			{ "Bob", "25" }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(2);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
		result[1].Name.ShouldBe("Bob");
		result[1].Age.ShouldBe(25);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_WithSharedStrings_ReturnsCorrectRows()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name", "Age" },
			{ "Alice", "30" }
		}, useSharedStrings: true);

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_EmptySheet_ReturnsEmpty()
	{
		using MemoryStream ms = BuildStream(new string[0, 0]);
		ms.ReadExcelFileToEnumerable<SimpleModel>().ShouldBeEmpty();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_SheetNotFound_ReturnsEmpty()
	{
		using MemoryStream ms = BuildStream(new[,] { { "Name" }, { "Alice" } }, sheetName: "Data");

		ms.ReadExcelFileToEnumerable<SimpleModel>(sheetName: "NonExistent").ShouldBeEmpty();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_NamedSheet_ReturnsRows()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name", "Age" },
			{ "Charlie", "40" }
		}, sheetName: "Employees");

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(sheetName: "Employees").ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Charlie");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_WithoutHeaders_PositionalMapping_ReturnsRows()
	{
		// Properties are mapped left-to-right by declaration order (Name → col A, Age → col B).
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Alice", "30" },
			{ "Bob", "25" }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(hasHeaders: false).ToList();

		result.Count.ShouldBe(2);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
		result[1].Name.ShouldBe("Bob");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_CaseInsensitiveHeaderMatch_ReturnsRows()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "NAME", "AGE" },
			{ "Alice", "30" }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_ExcelColumnAttribute_MapsCustomHeader()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Full Name", "Age" },
			{ "Dave", "35" }
		});

		List<ExcelColumnModel> result = ms.ReadExcelFileToEnumerable<ExcelColumnModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Dave");
		result[0].Age.ShouldBe(35);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_ExcelColumnAttribute_TakesPriorityOverPropertyName()
	{
		// "Points" maps to PropA via [ExcelColumn("Points")], not to OtherProp.
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Points", "OtherProp" },
			{ "99", "10" }
		});

		List<AttributePriorityModel> result = ms.ReadExcelFileToEnumerable<AttributePriorityModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].PropA.ShouldBe(99);
		result[0].OtherProp.ShouldBe(10);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_StartCellReference_SkipsEarlierRowsAndColumns()
	{
		// Column A and row 1 contain junk; real data starts at B2.
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Junk", "Junk", "Junk" },
			{ "Junk", "Name", "Age" },
			{ "Junk", "Eve",  "22"  },
			{ "Junk", "Frank","33"  }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(startCellReference: "B2").ToList();

		result.Count.ShouldBe(2);
		result[0].Name.ShouldBe("Eve");
		result[0].Age.ShouldBe(22);
		result[1].Name.ShouldBe("Frank");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_EndCellReference_StopsAtBoundRow()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name", "Age" },
			{ "Alice",   "30" },
			{ "Bob",     "25" },
			{ "Charlie", "40" } // row 4 — excluded by endCellReference "B3"
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(endCellReference: "B3").ToList();

		result.Count.ShouldBe(2);
		result[1].Name.ShouldBe("Bob");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_EndCellReference_RestrictsColumns()
	{
		// "Age" is in column C, which is beyond endCellReference "B2" — it should be excluded.
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Extra", "Age" },
			{ "Alice", "skip",  "30"  }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(endCellReference: "B2").ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(0); // column excluded
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_StopsAtFirstEmptyRow_WhenNoEndRef()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Age" },
			{ "Alice", "30"  },
			{ "",      ""    }, // empty row — iteration stops here
			{ "Bob",   "25"  }  // never reached
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_WithEndRef_EmptyRowWithinRange_IsSkippedNotStopped()
	{
		// When endCellReference is set, an empty row inside the range must NOT stop iteration.
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Age" },
			{ "Alice", "30"  },
			{ "",      ""    }, // empty row — skipped when endRef is present
			{ "Bob",   "25"  }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>(endCellReference: "B4").ToList();

		result.Count.ShouldBe(2);
		result[0].Name.ShouldBe("Alice");
		result[1].Name.ShouldBe("Bob");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_UnknownColumnHeader_IsSkippedSilently()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Age", "Extra"   },
			{ "Alice", "30",  "ignored" }
		});

		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_CellsWithNoCellReference_AreSkipped()
	{
		// Build a stream that contains cells without a CellReference attribute — defensive
		// null-check branches in both the header loop and the data loop must be hit.
		MemoryStream temp = new();
		using (SpreadsheetDocument doc = SpreadsheetDocument.Create(temp, SpreadsheetDocumentType.Workbook))
		{
			WorkbookPart wbp = doc.AddWorkbookPart();
			wbp.Workbook = new Workbook();
			WorksheetPart wsp = wbp.AddNewPart<WorksheetPart>();
			SheetData sheetData = new();

			// Header row: one cell with CellReference (valid header), one without (skipped).
			Row headerRow = new() { RowIndex = 1 };
			headerRow.Append(new Cell { CellValue = new CellValue("Junk") }); // no CellReference
			headerRow.Append(new Cell { CellReference = "A1", CellValue = new CellValue("Name") });
			sheetData.Append(headerRow);

			// Data row: one cell with CellReference (valid data), one without (skipped).
			Row dataRow = new() { RowIndex = 2 };
			dataRow.Append(new Cell { CellValue = new CellValue("Junk") }); // no CellReference
			dataRow.Append(new Cell { CellReference = "A2", CellValue = new CellValue("Alice") });
			sheetData.Append(dataRow);

			wsp.Worksheet = new Worksheet(sheetData);
			Sheets sheets = wbp.Workbook.AppendChild(new Sheets());
			sheets.Append(new Sheet { Id = wbp.GetIdOfPart(wsp), SheetId = 1, Name = "Sheet1" });
			wbp.Workbook.Save();
		}

		using MemoryStream ms = new(temp.ToArray());
		List<SimpleModel> result = ms.ReadExcelFileToEnumerable<SimpleModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_AllBasicTypes_ParsedCorrectly()
	{
		const string guidStr = "550e8400-e29b-41d4-a716-446655440000";
		using MemoryStream ms = BuildStream(new[,]
		{
			{
				"StringValue","BoolValue","ByteValue","ShortValue","IntValue","LongValue",
				"FloatValue","DoubleValue","DecimalValue","DateTimeValue","DateOnlyValue",
				"TimeOnlyValue","DateTimeOffsetValue","TimeSpanValue","GuidValue","UintValue"
			},
			{
				"hello","True","200","1000","42","9876543210",
				"3.5","2.5","99.99","2024-06-15T10:00:00","2024-06-15",
				"14:30:00","2024-06-15T10:00:00+00:00","01:30:00",guidStr,"123"
			}
		});

		List<AllTypesModel> result = ms.ReadExcelFileToEnumerable<AllTypesModel>().ToList();

		result.Count.ShouldBe(1);
		AllTypesModel item = result[0];
		item.StringValue.ShouldBe("hello");
		item.BoolValue.ShouldBeTrue();
		item.ByteValue.ShouldBe((byte)200);
		item.ShortValue.ShouldBe((short)1000);
		item.IntValue.ShouldBe(42);
		item.LongValue.ShouldBe(9876543210L);
		item.FloatValue.ShouldBe(3.5f);
		item.DoubleValue.ShouldBe(2.5);
		item.DecimalValue.ShouldBe(99.99m);
		item.DateTimeValue.ShouldBe(new DateTime(2024, 6, 15, 10, 0, 0));
		item.DateOnlyValue.ShouldBe(new DateOnly(2024, 6, 15));
		item.TimeOnlyValue.ShouldBe(new TimeOnly(14, 30, 0));
		item.DateTimeOffsetValue.ShouldBe(new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.Zero));
		item.TimeSpanValue.ShouldBe(new TimeSpan(1, 30, 0));
		item.GuidValue.ShouldBe(Guid.Parse(guidStr));
		item.UintValue.ShouldBe(123u);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_Boolean_FromOne_ReturnsTrue()
	{
		// "1" fails bool.TryParse, falls to `value == "1"` branch → true
		using MemoryStream ms = BuildStream(new[,] { { "BoolValue" }, { "1" } });

		List<AllTypesModel> result = ms.ReadExcelFileToEnumerable<AllTypesModel>().ToList();

		result[0].BoolValue.ShouldBeTrue();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_Boolean_FromZero_ReturnsFalse()
	{
		// "0" fails bool.TryParse, falls to `value == "1"` branch → false
		using MemoryStream ms = BuildStream(new[,] { { "BoolValue" }, { "0" } });

		List<AllTypesModel> result = ms.ReadExcelFileToEnumerable<AllTypesModel>().ToList();

		result[0].BoolValue.ShouldBeFalse();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_NullableProperty_InvalidValue_SetToNull()
	{
		// TryParse failure for int and DateOnly returns null; setter is still called (nullable property)
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "NullableInt", "NullableDateOnly" },
			{ "xyz",         "bad-date"         }
		});

		List<NullableTypesModel> result = ms.ReadExcelFileToEnumerable<NullableTypesModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].NullableInt.ShouldBeNull();
		result[0].NullableDateOnly.ShouldBeNull();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_NullableBool_InvalidValue_ReturnsFalse()
	{
		// bool never returns null from ConvertExcelCellValue; unrecognised string → false
		using MemoryStream ms = BuildStream(new[,] { { "NullableBool" }, { "notabool" } });

		List<NullableTypesModel> result = ms.ReadExcelFileToEnumerable<NullableTypesModel>().ToList();

		result[0].NullableBool.ShouldBe(false);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_NonNullableProperty_InvalidValue_KeepsDefault()
	{
		// TryParse failure for non-nullable types returns null; null guard prevents setter call
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "IntValue", "ByteValue", "GuidValue" },
			{ "xyz",      "xyz",       "not-a-guid" }
		});

		List<AllTypesModel> result = ms.ReadExcelFileToEnumerable<AllTypesModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].IntValue.ShouldBe(0);          // default
		result[0].ByteValue.ShouldBe((byte)0);    // default
		result[0].GuidValue.ShouldBe(Guid.Empty); // default
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_ConvertChangeType_Exception_SilentlySkipped()
	{
		// Uri is handled by the Convert.ChangeType fallback; invalid value throws InvalidCastException
		// which is caught silently — the property keeps its null default.
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Website"     },
			{ "Alice", "not-a-uri"   }
		});

		List<ModelWithUri> result = ms.ReadExcelFileToEnumerable<ModelWithUri>().ToList();

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Website.ShouldBeNull();
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_Enum_CaseInsensitive_Parses()
	{
		using MemoryStream ms = BuildStream(new[,] { { "Status" }, { "active" } });

		List<EnumModel> result = ms.ReadExcelFileToEnumerable<EnumModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Status.ShouldBe(TestStatus.Active);
	}

	[RetryFact(3)]
	public void ReadExcelFileToEnumerable_Enum_InvalidValue_KeepsDefault()
	{
		// Enum.TryParse fails → null → setter not called → default value
		using MemoryStream ms = BuildStream(new[,] { { "Status" }, { "NotAStatus" } });

		List<EnumModel> result = ms.ReadExcelFileToEnumerable<EnumModel>().ToList();

		result.Count.ShouldBe(1);
		result[0].Status.ShouldBe((TestStatus)0);
	}

	// ── ReadExcelFileToAsyncEnumerable ────────────────────────────────────────

	[RetryFact(3)]
	public async Task ReadExcelFileToAsyncEnumerable_BasicCase_ReturnsRows()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",  "Age" },
			{ "Alice", "30"  }
		});

		List<SimpleModel> result = [];
		await foreach (SimpleModel item in ms.ReadExcelFileToAsyncEnumerable<SimpleModel>())
			result.Add(item);

		result.Count.ShouldBe(1);
		result[0].Name.ShouldBe("Alice");
		result[0].Age.ShouldBe(30);
	}

	[RetryFact(3)]
	public async Task ReadExcelFileToAsyncEnumerable_Cancellation_ThrowsOperationCanceledException()
	{
		using MemoryStream ms = BuildStream(new[,]
		{
			{ "Name",    "Age" },
			{ "Alice",   "30"  },
			{ "Bob",     "25"  },
			{ "Charlie", "40"  }
		});

		using CancellationTokenSource cts = new();
		List<SimpleModel> received = [];

		await Should.ThrowAsync<OperationCanceledException>(async () =>
		{
			await foreach (SimpleModel item in ms.ReadExcelFileToAsyncEnumerable<SimpleModel>(cancellationToken: cts.Token))
			{
				received.Add(item);
				cts.Cancel(); // triggers ThrowIfCancellationRequested on the very next MoveNextAsync call
			}
		});

		received.Count.ShouldBe(1); // received exactly one item before the token was checked
	}
}
