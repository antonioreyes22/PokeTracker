using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;


namespace PokeTracker.Services
{
    public class FileService
    {
        public string? savePath { get; set; }
        public string? fileName { get; set; }
        public event Action? StateChanged;

        public void SetSaveFile(string path, string name)
        {
            savePath = path;
            fileName = name;
            StateChanged?.Invoke();
        }

        public void ClearSaveFile()
        {
            savePath = null;
            fileName = null;
            StateChanged?.Invoke();
        }
    }
}
