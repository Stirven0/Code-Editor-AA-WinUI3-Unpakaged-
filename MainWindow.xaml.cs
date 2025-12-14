using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Code_Editor__AA___WinUI3___Unpakaged_
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(TitleBarGrid);
        }

        private void FlyoutAbout_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Acerca de",
                Content = "Code_Editor__AA___WinUI3___Unpakaged_, versión 1.0\n(c) 2025 Persona, todos los derechos reservados",
                CloseButtonText = "Aceptar",
                XamlRoot = Content.XamlRoot
            };

            _ = dialog.ShowAsync();
        }

        private void FlyoutExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Increment_Click(object sender, RoutedEventArgs e)
        {
            int number = Convert.ToInt32(NumberText.Text);
            number++;
            NumberText.Text = number.ToString();
        }
    }
}
