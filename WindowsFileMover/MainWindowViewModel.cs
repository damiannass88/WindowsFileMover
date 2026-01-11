using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Application = System.Windows.Application;

namespace WindowsFileMover;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    public ObservableCollection<FileItem> Files { get; } = new();

    private string? _sourceFolder;
    private string? _destinationFolder;

    public string SourceFolderLabel => $"Source: {(_sourceFolder ?? "(not set)")};";
    public string DestinationFolderLabel => $"Destination: {(_destinationFolder ?? "(not set)")}";

    private string _status = "Ready.";
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }

    private int _progressMax;
    public int ProgressMax { get => _progressMax; set { _progressMax = value; OnPropertyChanged(); } }

    private int _progressValue;
    public int ProgressValue { get => _progressValue; set { _progressValue = value; OnPropertyChanged(); } }

    private string _progressText = "";
    public string ProgressText { get => _progressText; set { _progressText = value; OnPropertyChanged(); } }

    // Filters
    private string _withWord = "";
    public string WithWord { get => _withWord; set { _withWord = value; OnPropertyChanged(); } }

    public bool ExtMp4 { get; set; } = true;
    public bool ExtMkv { get; set; } = true;
    public bool ExtAvi { get; set; } = true;
    public bool ExtMov { get; set; } = false;
    public bool ExtWmv { get; set; } = false;
    public bool ExtFlv { get; set; } = false;
    public bool ExtWebm { get; set; } = false;

    private string _customExtensions = "";
    public string CustomExtensions { get => _customExtensions; set { _customExtensions = value; OnPropertyChanged(); } }

    public bool KeepStructure { get; set; } = false;
    public bool AutoRenameOnConflict { get; set; } = true;

    // Commands
    public RelayCommand PickSourceCommand { get; }
    public RelayCommand PickDestinationCommand { get; }
    public RelayCommand SearchCommand { get; }
    public RelayCommand MoveCommand { get; }
    public RelayCommand SelectAllCommand { get; }
    public RelayCommand SelectNoneCommand { get; }
    public RelayCommand<FileItem> OpenFileCommand { get; }
    public RelayCommand ResetCommand { get; }

    private bool _isBusy;

    public MainWindowViewModel()
    {
        PickSourceCommand = new RelayCommand(PickSource);
        PickDestinationCommand = new RelayCommand(PickDestination);

        SearchCommand = new RelayCommand(async () => await SearchAsync(), () => !_isBusy);
        MoveCommand = new RelayCommand(async () => await MoveSelectedAsync());
        OpenFileCommand = new RelayCommand<FileItem>(OpenFile, file => !_isBusy && file != null);
        ResetCommand = new RelayCommand(Reset, () => !_isBusy);

        SelectAllCommand = new RelayCommand(() =>
        {
            foreach (var f in Files)
                f.IsSelected = true;
        });

        SelectNoneCommand = new RelayCommand(() =>
        {
            foreach (var f in Files)
                f.IsSelected = false;
        });
    }

    private void SetBusy(bool busy, string status)
    {
        _isBusy = busy;
        Status = status;
        SearchCommand.RaiseCanExecuteChanged();
        OpenFileCommand.RaiseCanExecuteChanged();
        ResetCommand.RaiseCanExecuteChanged();
    }

    private void PickSource()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select SOURCE folder (recursive search)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _sourceFolder = dlg.SelectedPath;
            OnPropertyChanged(nameof(SourceFolderLabel));
            Status = "Source set.";
        }
    }

    private void PickDestination()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select DESTINATION folder (move selected here)",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        if (dlg.ShowDialog() == DialogResult.OK)
        {
            _destinationFolder = dlg.SelectedPath;
            OnPropertyChanged(nameof(DestinationFolderLabel));
            Status = "Destination set.";
        }
    }

    private HashSet<string> BuildExtensionsSet()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string ext)
        {
            ext = ext.Trim();
            if (string.IsNullOrWhiteSpace(ext)) return;
            if (!ext.StartsWith(".")) ext = "." + ext;
            set.Add(ext);
        }

        if (ExtMp4) Add("mp4");
        if (ExtMkv) Add("mkv");
        if (ExtAvi) Add("avi");
        if (ExtMov) Add("mov");
        if (ExtWmv) Add("wmv");
        if (ExtFlv) Add("flv");
        if (ExtWebm) Add("webm");

        if (!string.IsNullOrWhiteSpace(CustomExtensions))
        {
            var parts = CustomExtensions
                .Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var p in parts)
                Add(p);
        }

        return set;
    }

    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(_sourceFolder) || !Directory.Exists(_sourceFolder))
        {
            System.Windows.MessageBox.Show("Set a valid Source folder first.", "Missing Source",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SetBusy(true, "Searching...");
        Files.Clear();
        ProgressValue = 0;
        ProgressMax = 1;
        ProgressText = "";

        var extSet = BuildExtensionsSet();
        var withWord = (WithWord ?? "").Trim();

        var results = await Task.Run(() =>
        {
            var found = new List<FileItem>();
            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };

            foreach (var path in Directory.EnumerateFiles(_sourceFolder!, "*", opts))
            {
                var ext = Path.GetExtension(path);
                if (extSet.Count > 0 && !extSet.Contains(ext)) continue;

                if (!string.IsNullOrEmpty(withWord))
                {
                    var name = Path.GetFileName(path);
                    if (!name.Contains(withWord, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                try
                {
                    var fi = new FileInfo(path);
                    found.Add(new FileItem
                    {
                        IsSelected = false,
                        MoveWithFolder = false,
                        Name = fi.Name,
                        FullPath = fi.FullName,
                        SizeBytes = fi.Length
                    });
                }
                catch
                {
                    // ignore unreadable files
                }
            }

            return found.OrderByDescending(f => f.SizeBytes).ToList();
        });

        ProgressMax = results.Count;
        ProgressValue = 0;

        foreach (var item in results)
        {
            Files.Add(item);
            ProgressValue++;
            if (ProgressValue % 50 == 0)
                ProgressText = $"Loaded: {ProgressValue}/{ProgressMax}";
        }

        ProgressText = $"Loaded: {ProgressValue}/{ProgressMax}";
        Status = $"Search done. Found: {Files.Count} file(s). (Select what you want to move)";
        SetBusy(false, Status);
    }

    private async Task MoveSelectedAsync()
    {
        if (_isBusy)
            return;

        if (string.IsNullOrWhiteSpace(_destinationFolder) || !Directory.Exists(_destinationFolder))
        {
            System.Windows.MessageBox.Show("Set a valid Destination folder first.", "Missing Destination",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var selected = Files.Where(f => f.IsSelected).ToList();
        if (selected.Count == 0)
        {
            System.Windows.MessageBox.Show("No files selected.", "Nothing to move",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SetBusy(true, "Moving selected files...");
        ProgressMax = selected.Count;
        ProgressValue = 0;
        ProgressText = "";

        var sourceRoot = _sourceFolder!;
        var destRoot = _destinationFolder!;

        var movedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var errors = new List<string>();

        await Task.Run(() =>
        {
            foreach (var item in selected)
            {
                try
                {
                    var src = item.FullPath;

                    var destFolder = destRoot;
                    if (item.MoveWithFolder)
                    {
                        var parent = Path.GetDirectoryName(src);
                        if (!string.IsNullOrEmpty(parent))
                            destFolder = Path.Combine(destRoot, Path.GetFileName(parent));
                    }
                    else if (KeepStructure)
                    {
                        var rel = Path.GetRelativePath(sourceRoot, Path.GetDirectoryName(src)!);
                        destFolder = Path.Combine(destRoot, rel);
                    }
                    Directory.CreateDirectory(destFolder);

                    var destPath = Path.Combine(destFolder, item.Name);

                    if (File.Exists(destPath))
                    {
                        if (item.MoveWithFolder)
                        {
                            // skip on conflict when moving with folder
                            errors.Add($"Skipped (exists): {destPath}");
                            continue;
                        }

                        if (AutoRenameOnConflict)
                            destPath = GetNonConflictingPath(destPath);
                        else
                            throw new IOException($"Destination exists: {destPath}");
                    }

                    File.Move(src, destPath);
                    movedPaths.Add(src);
                }
                catch (Exception ex)
                {
                    errors.Add($"{item.FullPath} -> {ex.Message}");
                }
                finally
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressValue++;
                        if (ProgressValue % 10 == 0)
                            ProgressText = $"Moved: {ProgressValue}/{ProgressMax}";
                    });
                }
            }
        });

        var toRemove = Files.Where(f => movedPaths.Contains(f.FullPath)).ToList();
        foreach (var r in toRemove)
            Files.Remove(r);

        ProgressText = $"Moved: {ProgressValue}/{ProgressMax}";
        SetBusy(false, errors.Count == 0 ? "Move completed." : $"Move completed with {errors.Count} error(s).");

        if (errors.Count > 0)
        {
            var preview = string.Join(Environment.NewLine, errors.Take(20));
            if (errors.Count > 20)
                preview += $"{Environment.NewLine}... (+{errors.Count - 20} more)";
            System.Windows.MessageBox.Show(preview, "Move errors", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenFile(FileItem? file)
    {
        if (file == null)
            return;

        try
        {
            var startInfo = new ProcessStartInfo(file.FullPath)
            {
                UseShellExecute = true
            };
            Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to open file: {ex.Message}", "Open file",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string GetNonConflictingPath(string destPath)
    {
        var dir = Path.GetDirectoryName(destPath)!;
        var name = Path.GetFileNameWithoutExtension(destPath);
        var ext = Path.GetExtension(destPath);

        for (int i = 1; i < 100000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate))
                return candidate;
        }

        throw new IOException("Could not generate a unique destination filename.");
    }

    private void Reset()
    {
        if (_isBusy)
            return;

        Files.Clear();
        _sourceFolder = null;
        _destinationFolder = null;
        OnPropertyChanged(nameof(SourceFolderLabel));
        OnPropertyChanged(nameof(DestinationFolderLabel));

        WithWord = string.Empty;

        ExtMp4 = true;
        ExtMkv = true;
        ExtAvi = true;
        ExtMov = false;
        ExtWmv = false;
        ExtFlv = false;
        ExtWebm = false;
        OnPropertyChanged(nameof(ExtMp4));
        OnPropertyChanged(nameof(ExtMkv));
        OnPropertyChanged(nameof(ExtAvi));
        OnPropertyChanged(nameof(ExtMov));
        OnPropertyChanged(nameof(ExtWmv));
        OnPropertyChanged(nameof(ExtFlv));
        OnPropertyChanged(nameof(ExtWebm));

        CustomExtensions = string.Empty;

        KeepStructure = false;
        AutoRenameOnConflict = true;
        OnPropertyChanged(nameof(KeepStructure));
        OnPropertyChanged(nameof(AutoRenameOnConflict));

        ProgressValue = 0;
        ProgressMax = 0;
        ProgressText = string.Empty;

        SetBusy(false, "Ready.");
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
