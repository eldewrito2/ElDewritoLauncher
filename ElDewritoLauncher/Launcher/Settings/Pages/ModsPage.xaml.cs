using EDLauncher.Core.Install;
using EDLauncher.Utility;
using InstallerLib.Utility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace EDLauncher.Launcher.Settings.Pages
{
    public partial class ModsPage : UserControl
    {
        private string _directory = "";
        private bool _isEnumerating = false;
        private FileSystemWatcher? _directoryWatcher;
        public ObservableModList _mods = new ObservableModList();

        public ModsPage()
        {
            InitializeComponent();

            Loaded += ModsPage_Loaded;
            Unloaded += ModsPage_Unloaded;

            ModsCollectionView = CollectionViewSource.GetDefaultView(_mods);
            ModsCollectionView.SortDescriptions.Add(new SortDescription(nameof(ModListItemViewModel.Name), ListSortDirection.Ascending));
            ModsCollectionView.CollectionChanged += ModsCollectionView_CollectionChanged;
            AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeader_Click));
        }

        public ICollectionView ModsCollectionView { get; }

        private void ModsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _directory = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, InstallDirectory.ModsFolderName)).FullName;
            StartEnumerateMods();
            StartWatchingDirectory();
        }

        private void ModsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            StopWatchingDirectory();

            // We don't need to keep it memory as it will be refreshed when the page is next loaded anyway
            _mods.Clear();
        }

        private void StartWatchingDirectory()
        {
            _directoryWatcher = new FileSystemWatcher(_directory, "*.pak");
            _directoryWatcher.NotifyFilter =
                NotifyFilters.Attributes |
                NotifyFilters.CreationTime |
                NotifyFilters.DirectoryName |
                NotifyFilters.FileName |
                NotifyFilters.LastAccess |
                NotifyFilters.LastWrite |
                NotifyFilters.Security |
                NotifyFilters.Size;
            _directoryWatcher.Deleted += OnFileDeleted;
            _directoryWatcher.Renamed += OnFileRenamed;
            _directoryWatcher.Changed += OnFileChanged;
            _directoryWatcher.EnableRaisingEvents = true;
        }

        private void StopWatchingDirectory()
        {
            Debug.Assert(_directoryWatcher != null);
            _directoryWatcher.Dispose();
            _directoryWatcher = null;
        }

        private async void StartEnumerateMods()
        {
            // Prevent re-enumerating when switching fast between tabs. It might also
            // make sense to cancel it when the page is unloaded for the future
            if(_isEnumerating)
            {
                return;
            }

            try
            {
                _isEnumerating = true;
                loadingSpinner.Visibility = Visibility.Visible;
                _mods.Clear();

                List<ModMetadata> modInfos = await Task.Run(() => ModHelper.EnumerateModsAsync(_directory));
                modInfos.ForEach(info =>
                {
                    _mods.Add(new ModListItemViewModel(info));
                });
            }
            finally
            {
                _isEnumerating = false;
                loadingSpinner.Visibility = Visibility.Collapsed;
            }
        }

        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            var modInfo = await ModHelper.ReadModAsync(new FileInfo(e.FullPath));
            Dispatcher.Invoke(() =>
            {
                ModListItemViewModel? item = _mods.FirstOrDefault(x => x.FullPath == e.FullPath);
                if (item != null)
                {
                    var index = _mods.IndexOf(item);
                    _mods.Remove(item);
                }
                _mods.Add(new ModListItemViewModel(modInfo));
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ModListItemViewModel? item = _mods.FirstOrDefault(x => x.FullPath == e.OldFullPath);
                if (item != null)
                {
                    _mods.Remove(item);
                    item.FullPath = e.FullPath;
                    item.FileName = Path.GetFileName(e.FullPath);
                    _mods.Add(item);
                }
            });
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                ModListItemViewModel? item = _mods.FirstOrDefault(x => x.FullPath == e.FullPath);
                if (item != null)
                {
                    int selectedIndex = modList.SelectedIndex;
                    _mods.Remove(item);
                    modList.SelectedIndex = selectedIndex;
                }
            });
        }

        private async void DeleteCommand_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (sender == modList)
            {
                await HandleDeleteMod();
            }
        }

        private async Task HandleDeleteMod()
        {
            var items = modList.SelectedItems.Cast<ModListItemViewModel>().ToList();
            if (items.Count == 0)
                return;

            MessageBoxResult decision = MessageBox.Show("Are you sure you want to delete the selected mods?", $"Deleting {items.Count} mods", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (decision == MessageBoxResult.Yes)
            {
                foreach (ModListItemViewModel item in items)
                {
                    await FileUtility.RetryOnFileAccessErrorAsync(() => File.Delete(item.FullPath), 3, 100);
                }
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && header.Column != null)
            {
                string propertyName = ((Binding)header.Column.DisplayMemberBinding).Path.Path.Split('.').Last();
                SortDescription existingDesc = ModsCollectionView.SortDescriptions.FirstOrDefault(x => x.PropertyName == propertyName);
                var direction = ListSortDirection.Ascending;
                if (existingDesc != default)
                {
                    direction = existingDesc.Direction == ListSortDirection.Ascending
                        ? ListSortDirection.Descending
                        : ListSortDirection.Ascending;
                }

                ModsCollectionView.SortDescriptions.Clear();
                ModsCollectionView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            }
        }

        private void tbSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textbox = (TextBox)sender;
            string searchTerm = textbox.Text;
            if (ModsCollectionView != null)
            {
                ModsCollectionView.Filter = (o) =>
                {
                    var item = (ModListItemViewModel)o;
                    string combinedText = $"{item.Name} {item.Author} {item.FileName}";
                    return combinedText.Contains(searchTerm, StringComparison.InvariantCultureIgnoreCase);
                };
            }
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            using var _ = Process.Start("explorer.exe", _directory);
        }

        private void ModsCollectionView_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateStatusText();
        }

        private void modList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            List<ModListItemViewModel> items = modList.Items.Cast<ModListItemViewModel>().ToList();
            List<ModListItemViewModel> selectedItems = modList.SelectedItems.Cast<ModListItemViewModel>().ToList();

            int totalCount = items.Count;
            long totalSize = items.Sum(x => x.Size);
            int selectedCount = selectedItems.Count;
            long selectedSize = selectedItems.Sum(x => x.Size);

            string newStatusText = "0 mods";
            if (totalCount > 0)
            {
                newStatusText = $"{totalCount} {(totalCount != 1 ? "mods" : "mod")} {FormatUtils.FormatSize(totalSize)}";
                newStatusText += $" | {selectedCount} {(selectedCount != 1 ? "mods" : "mod")} selected {FormatUtils.FormatSize(selectedSize)}";
            }
            txtStatus.Text = newStatusText;
        }

        class ModHelper
        {
            public static async Task<List<ModMetadata>> EnumerateModsAsync(string directory)
            {
                var mods = new List<ModMetadata>();
                var readTasks = new List<Task<ModMetadata>>();
                foreach (var fileInfo in new DirectoryInfo(directory).GetFiles("*.pak"))
                {
                    try
                    {
                        readTasks.Add(ReadModAsync(fileInfo));
                    }
                    catch(Exception ex)
                    {
                        App.ServiceProvider.GetRequiredService<ILogger<ModHelper>>().LogError(ex, $"Failed to read mod package '{fileInfo.FullName}'");
                    }
                }

                foreach(var mod in await Task.WhenAll(readTasks))
                    mods.Add(mod);

                return mods;
            }

            public async static Task<ModMetadata> ReadModAsync(FileInfo fileInfo)
            {
                ModMetadata? metadata = null;
                await FileUtility.RetryOnFileAccessErrorAsync(() => metadata = ReadMod(fileInfo), 10, 50);
                Debug.Assert(metadata != null);
                return metadata;
            }

            private static ModMetadata ReadMod(FileInfo fileInfo)
            {
                using var fs = File.OpenRead(fileInfo.FullName);
                var item = ReadModMetadata(fs);
                item.FullPath = fileInfo.FullName;
                item.Size = fileInfo.Length;
                return item;
            }

            private static ModMetadata ReadModMetadata(Stream stream)
            {
                var reader = new BinaryReader(stream);
                reader.BaseStream.Position = 0x2C;
                int metadataOffset = reader.ReadInt32();
                reader.BaseStream.Position = metadataOffset;
                reader.BaseStream.Position += 4;
                long offset = reader.ReadUInt32();
                reader.BaseStream.Position = offset + 4;
                string name = Encoding.Unicode.GetString(reader.ReadBytes(32)).Trim('\0');
                reader.BaseStream.Position = offset + 0x44;
                string author = Encoding.ASCII.GetString(reader.ReadBytes(32)).Trim('\0');
                reader.BaseStream.Position += 512 + 8;
                int major = reader.ReadUInt16();
                int minor = reader.ReadUInt16();
                return new ModMetadata()
                {
                    Name = name,
                    Author = author,
                    Version = $"{major}.{minor}"
                };
            }
        }

        public class ModListItemViewModel
        {
            public string FullPath { get; set; }
            public string FileName { get; set; }
            public string Name { get; set; }
            public string Author { get; set; }
            public string Version { get; set; }
            public long Size { get; set; }

            public ModListItemViewModel(ModMetadata info)
            {
                FullPath = info.FullPath;
                FileName = Path.GetFileName(info.FullPath);
                Name = info.Name;
                Author = info.Author;
                Version = info.Version;
                Size = info.Size;
            }
        }

        public class ModMetadata
        {
            public string FullPath { get; set; } = "";
            public string Name { get; set; } = "";
            public string Author { get; set; } = "";
            public string Version { get; set; } = "";
            public long Size { get; set; } = 0;
        }

        public class ObservableModList : IEnumerable<ModListItemViewModel>, INotifyCollectionChanged
        {
            private ObservableCollection<ModListItemViewModel> _items = new ObservableCollection<ModListItemViewModel>();

            public event NotifyCollectionChangedEventHandler? CollectionChanged
            {
                add => _items.CollectionChanged += value;
                remove => _items.CollectionChanged -= value;
            }

            public void Add(ModListItemViewModel item)
            {
                if(_items.Any(x => x.FullPath == item.FullPath))
                {
                    return;
                }

                _items.Add(item);
            }

            public bool Remove(ModListItemViewModel item)
            {
                return _items.Remove(item);
            }

            public void Clear()
            {
                _items.Clear();
            }

            public int IndexOf(ModListItemViewModel item)
            {
                return _items.IndexOf(item);
            }

            public int Count => _items.Count;

            public IEnumerator<ModListItemViewModel> GetEnumerator()
            {
                return _items.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _items.GetEnumerator();
            }
        }

        private async void miModDelete_Click(object sender, RoutedEventArgs e)
        {
            await HandleDeleteMod();
        }

        private void miModOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var items = modList.SelectedItems.Cast<ModListItemViewModel>().ToList();
            if (items.Count == 0)
                return;

            if(items.Count == 1)
            {
                Process.Start("explorer.exe", $"/select,\"{items[0].FullPath}\"");
            }
            else
            {
                Process.Start("explorer.exe", _directory);
            }
        }
    }
}
