﻿using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Studio.Controls;
using Reclaimer.Plugins;

namespace Reclaimer.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, IMultiPanelHost
    {
        MultiPanel IMultiPanelHost.MultiPanel => MainPanel;

        DocumentTabControl IMultiPanelHost.DocumentContainer => docTab;

        public MainWindow()
        {
            InitializeComponent();

            Substrate.LoadPlugins();
        }

        private async void menuImport_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Halo Map Files|*.map",
                Multiselect = true,
                CheckFileExists = true
            };

            if (ofd.ShowDialog() != true)
                return;

            await Task.Run(async () =>
            {
                foreach (var fileName in ofd.FileNames)
                {
                    if (!File.Exists(fileName))
                        continue;

                    await Storage.ImportCacheFile(fileName);
                }

                MessageBox.Show("all done");
            });
        }

        private void menuTagViewer_Click(object sender, RoutedEventArgs e)
        {
            var tc = MainPanel.GetElementAtPath(Dock.Left) as Studio.Controls.UtilityTabControl;

            if (tc == null) tc = new Studio.Controls.UtilityTabControl();
            tc.Items.Add(new Controls.TagViewer());

            if (!MainPanel.GetChildren().Contains(tc))
                MainPanel.AddElement(tc, null, Dock.Left, new GridLength(400));
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            menu.Items.Clear();

            foreach (var plugin in Substrate.AllPlugins)
            {
                foreach (var item in plugin.MenuItems)
                    AddMenuItem(plugin, item);
            }
        }

        private void AddMenuItem(Plugin source, PluginMenuItem item)
        {
            var menuItem = GetMenuItem(item.Path);
            menuItem.Tag = item.Key;

            menuItem.Click -= GetHandler(source, item.Key); // incase the key is not unique
            menuItem.Click += GetHandler(source, item.Key);

            var root = GetRoot(menuItem);
            if (!menu.Items.Contains(root))
                menu.Items.Add(root);
        }

        private Dictionary<string, RoutedEventHandler> actionLookup = new Dictionary<string, RoutedEventHandler>();
        private RoutedEventHandler GetHandler(Plugin source, string key)
        {
            if (actionLookup.ContainsKey(key))
                return actionLookup[key];

            var action = new RoutedEventHandler((s, e) => source.OnMenuItemClick(key));
            actionLookup.Add(key, action);

            return action;
        }

        private Dictionary<string, MenuItem> menuLookup = new Dictionary<string, MenuItem>();
        private MenuItem GetMenuItem(string path)
        {
            if (menuLookup.ContainsKey(path))
                return menuLookup[path];

            var index = path.LastIndexOf('\\');
            var branch = index < 0 ? null : path.Substring(0, index);
            var leaf = index < 0 ? path : path.Substring(index + 1);

            var item = new MenuItem { Header = leaf };
            menuLookup.Add(path, item);

            if (branch == null)
                return item;

            var parent = GetMenuItem(branch);
            parent.Items.Add(item);

            return item;
        }

        private MenuItem GetRoot(MenuItem item)
        {
            var temp = item;
            while ((temp = temp.Parent as MenuItem) != null)
                item = temp;

            return item;
        }
    }
}
