using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Code_Editor__AA___WinUI3___Unpakaged_
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(TitleBarGrid);

            // Crear una pestaña inicial
            CreateNewTab();
        }

        private class Document
        {
            public string? FilePath { get; set; }
            public TextBox? TextBox { get; set; }
            public bool IsDirty { get; set; }
        }

        #region UI eventos (archivo)
        private void NewFile_Click(object sender, RoutedEventArgs e) => CreateNewTab();

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
                picker.FileTypeFilter.Add("*");
                StorageFile? file = await picker.PickSingleFileAsync();
                if (file is null) return;

                string text = await FileIO.ReadTextAsync(file);
                CreateNewTab(text, file.Path);

                // marcar como limpio
                var doc = GetCurrentDocument();
                if (doc is not null) { doc.IsDirty = false; UpdateTabHeader(GetSelectedTabItem()); }
            }
            catch (Exception ex)
            {
                _ = new ContentDialog { Title = "Error", Content = ex.Message, CloseButtonText = "OK", XamlRoot = Content.XamlRoot }.ShowAsync();
            }
        }

        private async void SaveFile_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentDocument();
            if (doc is null) return;

            if (string.IsNullOrEmpty(doc.FilePath))
            {
                await SaveFileAsAsync(doc);
                return;
            }

            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(doc.FilePath);
                await FileIO.WriteTextAsync(file, doc.TextBox!.Text);
                doc.IsDirty = false;
                UpdateTabHeader(GetSelectedTabItem());
            }
            catch (Exception ex)
            {
                _ = new ContentDialog { Title = "Error al guardar", Content = ex.Message, CloseButtonText = "OK", XamlRoot = Content.XamlRoot }.ShowAsync();
            }
        }

        private async void SaveAsFile_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentDocument();
            if (doc is null) return;
            await SaveFileAsAsync(doc);
        }

        private async Task SaveFileAsAsync(Document doc)
        {
            try
            {
                var picker = new FileSavePicker();
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
                picker.SuggestedFileName = Path.GetFileName(doc.FilePath ?? "SinTitulo");
                picker.FileTypeChoices.Add("All", new[] { "*" });

                StorageFile? file = await picker.PickSaveFileAsync();
                if (file is null) return;

                await FileIO.WriteTextAsync(file, doc.TextBox!.Text);
                doc.FilePath = file.Path;
                doc.IsDirty = false;
                UpdateTabHeader(GetSelectedTabItem());
            }
            catch (Exception ex)
            {
                _ = new ContentDialog { Title = "Error al guardar", Content = ex.Message, CloseButtonText = "OK", XamlRoot = Content.XamlRoot }.ShowAsync();
            }
        }
        #endregion

        #region Tab / editor management
        private void EditorTabView_AddButtonClick(TabView sender, object args) => CreateNewTab();

        private void EditorTabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            if (args.Tab is TabViewItem tab)
            {
                CloseTab(tab);
            }
        }

        private void CreateNewTab(string? content = null, string? path = null)
        {
            var tb = new TextBox
            {
                AcceptsReturn = true,
                //AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap,
                //HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                //VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas, 'Courier New', monospace"),
                FontSize = 14,
                Text = content ?? string.Empty,
                IsSpellCheckEnabled = false
            };
            tb.TextChanged += EditorTextBox_TextChanged;

            var doc = new Document { FilePath = path, TextBox = tb, IsDirty = false };
            var header = GetFileName(path) ?? "Sin título";

            var tab = new TabViewItem
            {
                Header = header,
                Content = tb,
                Tag = doc,
                IsClosable = true
            };

            EditorTabView.TabItems.Add(tab);
            EditorTabView.SelectedItem = tab;
        }

        private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            var tab = GetTabFromTextBox(tb);
            if (tab is null) return;

            var doc = (Document?)tab.Tag;
            if (doc is null) return;

            if (!doc.IsDirty)
            {
                doc.IsDirty = true;
                UpdateTabHeader(tab);
            }
        }

        private TabViewItem? GetTabFromTextBox(TextBox tb)
        {
            foreach (var item in EditorTabView.TabItems)
            {
                if (item is TabViewItem t && t.Content == tb) return t;
            }
            return null;
        }

        private TabViewItem? GetSelectedTabItem() => EditorTabView.SelectedItem as TabViewItem;

        private Document? GetCurrentDocument()
        {
            var tab = GetSelectedTabItem();
            return tab?.Tag as Document;
        }

        private void UpdateTabHeader(TabViewItem? tab)
        {
            if (tab is null) return;
            var doc = tab.Tag as Document;
            var name = GetFileName(doc?.FilePath) ?? "Sin título";
            tab.Header = doc?.IsDirty == true ? $"{name} *" : name;
        }

        private void CloseTab(TabViewItem tab)
        {
            var doc = tab.Tag as Document;
            if (doc?.IsDirty == true)
            {
                // Simple confirmación
                var dlg = new ContentDialog
                {
                    Title = "Cambios no guardados",
                    Content = $"El archivo \"{GetFileName(doc.FilePath) ?? "Sin título"}\" tiene cambios. ¿Guardar antes de cerrar?",
                    PrimaryButtonText = "Guardar",
                    SecondaryButtonText = "Cerrar sin guardar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = Content.XamlRoot
                };

                var result = dlg.ShowAsync();
                result.AsTask().ContinueWith(async t =>
                {
                    var r = t.Result;
                    if (r == ContentDialogResult.Primary)
                    {
                        await EnqueueAsync(async () =>
                        {
                            await SaveFileAsAsync(doc);
                            EditorTabView.TabItems.Remove(tab);
                        });
                    }
                    else if (r == ContentDialogResult.Secondary)
                    {
                        await EnqueueAsync(() => { EditorTabView.TabItems.Remove(tab); return Task.CompletedTask; });
                    }
                });
            }
            else
            {
                EditorTabView.TabItems.Remove(tab);
            }
        }

        // Método auxiliar para ejecutar código en el DispatcherQueue de forma asíncrona
        private static Task EnqueueAsync(Func<Task> func)
        {
            var tcs = new TaskCompletionSource<object?>();
            Windows.System.DispatcherQueue.GetForCurrentThread().TryEnqueue(async () =>
            {
                try
                {
                    await func();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });
            return tcs.Task;
        }
        #endregion

        #region Helpers / util
        private static string? GetFileName(string? path) => string.IsNullOrEmpty(path) ? null : Path.GetFileName(path);

        // Edición simple: copiar/pegar del TextBox seleccionado
        private void EditCopy_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentDocument();
            doc?.TextBox?.CopySelectionToClipboard();
        }

        private void EditPaste_Click(object sender, RoutedEventArgs e)
        {
            var doc = GetCurrentDocument();
            doc?.TextBox?.PasteFromClipboard();
        }

        // Mantengo los métodos existentes para el menú 'Acerca de' y 'Salir'
        private void FlyoutAbout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Acerca de",
                Content = "Code Editor, versión 1.0\n(c) 2025 Persona",
                CloseButtonText = "Aceptar",
                XamlRoot = Content.XamlRoot
            };

            _ = dialog.ShowAsync();
        }

        private void FlyoutExit_Click(object sender, RoutedEventArgs e) => Close();
        #endregion
    }
}