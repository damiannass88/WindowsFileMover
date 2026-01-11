using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsFileMover
{
    public sealed class FileItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _moveWithFolder;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public bool MoveWithFolder
        {
            get => _moveWithFolder;
            set
            {
                if (_moveWithFolder == value) return;
                _moveWithFolder = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; init; } = "";
        public string FullPath { get; init; } = "";
        public long SizeBytes { get; init; }

        public string SizeHuman => HumanSize(SizeBytes);

        private static string HumanSize(long bytes)
        {
            string[] units = ["B", "KB", "MB", "GB", "TB"];
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return $"{size:0.##} {units[unit]}";
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
