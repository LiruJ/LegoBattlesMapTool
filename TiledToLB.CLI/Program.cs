using CommandLine;
using TiledToLB.CLI.CommandLine;
using TiledToLB.Core.Processors;

var parseResult = Parser.Default.ParseArguments<ExportOptions, ExportLBZOptions, UnpackOptions, ImportFromRomOptions, CreateNewOptions, UpgradeExistingOptions>(args);

await parseResult.WithParsedAsync<ExportOptions>(async options => await ExporterProcessor.ProcessMapAsync(options.WorkspaceDirectoryPath, options.InputMapName, options.OutputMapName, !options.SkipCompression, options.Silent));
await parseResult.WithParsedAsync<ExportLBZOptions>(async options => await ExporterProcessor.ProcessAndPackLBZAsync(options.WorkspaceDirectoryPath, options.InputMapName, options.OutputMapName, options.Silent));
await parseResult.WithParsedAsync<CreateNewOptions>(async options => await CreateNewProcessor.CreateNewAsync(options.WorkspaceDirectoryPath, options.MapName, options.TilesetName, options.CreatorName, options.Silent));
await parseResult.WithParsedAsync<UnpackOptions>(async options => await UnpackerProcessor.UnpackRomAsync(options.InputFilePath, options.OutputDirectoryPath, options.Overwrite, options.Silent));
await parseResult.WithParsedAsync<ImportFromRomOptions>(async options => await ImporterProcessor.ImportMapFromRomAsync(options.InputFilePath, options.WorkspaceDirectoryPath, options.RomMapName, options.OutputMapName, options.Silent));
await parseResult.WithParsedAsync<UpgradeExistingOptions>(async options => await UpgradeProcessor.UpgradeExistingAsync(options.WorkspaceDirectoryPath, options.MapName, options.Silent));

await parseResult.WithNotParsedAsync((errors) =>
{
	foreach (Error error in errors)
	{
		// At some point, maybe certain errors need specific handling. But the default console output is already pretty good.
		switch (error.Tag)
		{
            // No real error, does not need any output.
            case ErrorType.HelpRequestedError:
            case ErrorType.HelpVerbRequestedError:
            case ErrorType.VersionRequestedError:
                break;
            case ErrorType.BadFormatTokenError:
			case ErrorType.MissingValueOptionError:
			case ErrorType.UnknownOptionError:
			case ErrorType.MissingRequiredOptionError:
			case ErrorType.MutuallyExclusiveSetError:
			case ErrorType.BadFormatConversionError:
			case ErrorType.SequenceOutOfRangeError:
			case ErrorType.RepeatedOptionError:
			case ErrorType.NoVerbSelectedError:
			case ErrorType.BadVerbSelectedError:
			case ErrorType.SetValueExceptionError:
			case ErrorType.InvalidAttributeConfigurationError:
			case ErrorType.MissingGroupOptionError:
			case ErrorType.GroupOptionAmbiguityError:
			case ErrorType.MultipleDefaultVerbsError:
			default:
				break;
		}
	}
	return Task.CompletedTask;
});