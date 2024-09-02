﻿using Microsoft.Win32;
using Reclaimer.Blam.HaloInfinite;
using Reclaimer.Controls.Editors;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Reclaimer.Plugins
{
    public class InfiniteModuleViewerPlugin : Plugin
    {
        private const string OpenKey = "ModuleViewer.OpenModule";
        private const string OpenPath = "File\\Open Module (Infinite)";
        private const string BrowseFileFilter = "Halo Module Files|*.module";
        private const string ModuleFileExtension = "module";

        private const string HaloInfiniteTagKeyRegex = @"Blam\.HaloInfinite\w+\..{2,}";

        internal static InfiniteModuleViewerSettings Settings;

        public override string Name => "Module Viewer (Infinite)";

        private PluginContextItem ExtractBinaryContextItem => new PluginContextItem("ExtractBinary", "Extract Tag Binary", OnContextItemClick);

        public override void Initialise() => Settings = LoadSettings<InfiniteModuleViewerSettings>();
        public override void Suspend() => SaveSettings(Settings);

        public override IEnumerable<PluginMenuItem> GetMenuItems()
        {
            yield return new PluginMenuItem(OpenKey, OpenPath, OnMenuItemClick);
        }

        public override IEnumerable<PluginContextItem> GetContextItems(OpenFileArgs context)
        {
            yield return ExtractBinaryContextItem;
        }

        private void OnMenuItemClick(string key)
        {
            if (key != OpenKey)
                return;

            var ofd = new OpenFileDialog
            {
                Filter = BrowseFileFilter,
                Multiselect = true,
                CheckFileExists = true
            };

            if (!string.IsNullOrEmpty(Settings.ModuleFolder))
                ofd.InitialDirectory = Settings.ModuleFolder;

            if (ofd.ShowDialog() != true)
                return;

            foreach (var fileName in ofd.FileNames)
                OpenPhysicalFile(fileName);
        }

        private void OnContextItemClick(string key, OpenFileArgs context)
        {
            var item = context.File.OfType<ModuleItem>().First();

            var sfd = new SaveFileDialog
            {
                OverwritePrompt = true,
                FileName = item.FileName,
                Filter = "Binary Files|*.bin",
                FilterIndex = 1,
                AddExtension = true
            };

            if (sfd.ShowDialog() != true)
                return;

            using (var tagReader = item.CreateReader())
            using (var fs = File.Open(sfd.FileName, FileMode.Create))
                tagReader.BaseStream.CopyTo(fs);
        }

        public override bool SupportsFileExtension(string extension) => extension.ToLowerInvariant() == ModuleFileExtension;

        public override void OpenPhysicalFile(string fileName)
        {
            var tabId = $"{Key}::{fileName}";
            if (Substrate.ShowTabById(tabId))
                return;

            LogOutput($"Loading module file: {fileName}");

            try
            {
                var mv = new Controls.InfiniteModuleViewer();
                mv.TabModel.ContentId = tabId;
                mv.LoadModule(fileName);
                Substrate.AddTool(mv.TabModel, Substrate.GetHostWindow(), Dock.Left, new GridLength(400));
                Substrate.AddRecentFile(fileName);

                LogOutput($"Loaded module file: {fileName}");
            }
            catch (Exception ex)
            {
                var message = $"Unable to load {Path.GetFileName(fileName)}:{Environment.NewLine}{ex.Message}";
                Substrate.ShowErrorMessage(message);
                Substrate.LogError(message, ex);
            }
        }
    }

    internal class InfiniteModuleViewerSettings
    {
        [Editor(typeof(BrowseFolderEditor), typeof(BrowseFolderEditor))]
        [DisplayName("Modules Folder")]
        public string ModuleFolder { get; set; }

        [DisplayName("Hierarchy View")]
        public bool HierarchyView { get; set; }
    }
}
