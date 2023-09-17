// Copyright 2023 ReWaffle LLC. All rights reserved.

using System;
using System.Collections.Generic;
using System.IO;
using static Naninovel.PathUtils;
using static Naninovel.Spreadsheet.Constants;

namespace Naninovel.Spreadsheet
{
    public class Processor
    {
        protected virtual string ScriptFolder { get; }
        protected virtual string TextFolder { get; }
        protected virtual string OutputFolder { get; }
        protected virtual Action<ProcessorProgress> OnProgress { get; }
        protected virtual ScriptExporter ScriptExporter { get; }
        protected virtual TextExporter TextExporter { get; }
        protected virtual SheetImporter SheetImporter { get; }

        public Processor (ProcessorOptions options)
        {
            OutputFolder = options.OutputFolder;
            ScriptFolder = options.ScriptFolder;
            TextFolder = options.TextFolder;
            OnProgress = options.OnProgress;
            ScriptExporter = new ScriptExporter(options.ScriptFolder, options.L10nFolder, options.OutputFolder, options.SourceLocale, options.Annotate);
            TextExporter = new TextExporter(options.L10nFolder, options.OutputFolder, options.SourceLocale, options.Annotate);
            SheetImporter = new SheetImporter(options.L10nFolder, options.SourceLocale);
        }

        public virtual void Export ()
        {
            ExportScripts();
            ExportText();
        }

        public virtual void Import ()
        {
            ImportScripts();
            ImportText();
        }

        protected virtual void ExportScripts ()
        {
            var paths = FormatPaths(Directory.GetFiles(ScriptFolder, ScriptPattern, SearchOption.AllDirectories));
            for (int i = 0; i < paths.Length; i++)
            {
                NotifyProgressChanged(paths, i);
                try { ScriptExporter.Export(paths[i]); }
                catch (Exception e) { throw new Error($"Failed to export script `{paths[i]}`: {e}"); }
            }
        }

        protected virtual void ExportText ()
        {
            var paths = FormatPaths(Directory.GetFiles(TextFolder, TextPattern, SearchOption.TopDirectoryOnly));
            for (int i = 0; i < paths.Length; i++)
            {
                NotifyProgressChanged(paths, i);
                try { TextExporter.Export(paths[i]); }
                catch (Exception e) { throw new Error($"Failed to export text `{paths[i]}`: {e}"); }
            }
        }

        protected virtual void ImportScripts ()
        {
            var folder = Combine(OutputFolder, ScriptFolderName);
            var paths = FormatPaths(Directory.GetFiles(folder, CsvPattern, SearchOption.AllDirectories));
            for (int i = 0; i < paths.Length; i++)
            {
                NotifyProgressChanged(paths, i);
                try { SheetImporter.Import(paths[i], $"{ScriptFolderName}/{Path.GetFileNameWithoutExtension(paths[i])}"); }
                catch (Exception e) { throw new Error($"Failed to import script `{paths[i]}`: {e}"); }
            }
        }

        protected virtual void ImportText ()
        {
            var folder = Combine(OutputFolder, TextFolderName);
            var paths = FormatPaths(Directory.GetFiles(folder, CsvPattern, SearchOption.TopDirectoryOnly));
            for (int i = 0; i < paths.Length; i++)
            {
                NotifyProgressChanged(paths, i);
                try { SheetImporter.Import(paths[i], Path.GetFileNameWithoutExtension(paths[i])); }
                catch (Exception e) { throw new Error($"Failed to import text `{paths[i]}`: {e}"); }
            }
        }

        protected virtual void NotifyProgressChanged (IReadOnlyList<string> paths, int index)
        {
            if (OnProgress is null) return;
            var info = $"Processing {Path.GetFileName(paths[index])}...";
            var progress = index / (float)paths.Count;
            OnProgress.Invoke(new ProcessorProgress(info, progress));
        }
    }
}
