using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFileMover
{
    public sealed class FileItem
    {
        public bool IsSelected { get; set; }

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
    }
    }
