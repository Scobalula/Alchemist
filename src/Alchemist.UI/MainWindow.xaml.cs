using Alchemist.Scripting;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using RedFox.UI;
using RedFox.Zenith;
using RedFox.Zenith.LicenseStorages;
using RedFox.Zenith.LicenseVerifiers;
using Supabase.Postgrest;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms.Design;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Client = Supabase.Client;

namespace Alchemist.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainViewModel ViewModel { get; set; } = new((message) => { }, string.Empty);

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;

            DataContext = ViewModel;

            InitializeComponent();
        }

        private void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"Alchemist hit an unhandled exception and must close:\n\n{e.ExceptionObject}\n\nThe current project has been saved to a temp file in Alchemist's folder.", "Alchemist | Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

            try
            {
                MainViewModel.SaveProject(ViewModel, "Backup.aprj");
            }
            catch { }
        }

        private void TextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectionStart = 0;
            ((TextBox)sender).SelectionLength = ((TextBox)sender).Text.Length;
        }

        public static IEnumerable<T> GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            Stack<DependencyObject> objs = [];

            objs.Push(depObj);

            while(objs.Count > 0)
            {
                var obj = objs.Pop();
                var count = VisualTreeHelper.GetChildrenCount(obj);

                for (int i = 0; i < count; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);

                    Trace.WriteLine(child);

                    if (child is T rVal)
                        yield return rVal;

                    objs.Push(child);
                }
            }
        }

        private void ListViewDrop(object sender, DragEventArgs e)
        {
            //Animation? poseAnim = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // First check, let's see if we have aprj
                if (files.Length > 0 && Path.GetExtension(files[0]).Equals(".aprj", StringComparison.CurrentCultureIgnoreCase))
                {
                    ViewModel.LoadProjectFile(files[0]);

                    foreach (var l in GetChildOfType<ListViewItem>(MainAnimationList))
                    {
                        Trace.WriteLine(l);
                    }
                }
                else
                {
                    if (sender is ListView listView && listView.SelectedItems.Count > 0)
                    {
                        foreach (var animation in listView.SelectedItems.Cast<Animation>())
                        {
                            foreach (var file in files)
                            {
                                animation.Layers.Add(new(file, animation));
                                //ViewModel.AnimationLayers.Add(layer);
                            }
                        }
                    }
                    else
                    {
                        using var modifier = new MVVMItemListModifier<Animation>(ViewModel.Animations);

                        foreach (var file in files)
                        {
                            var anim = new Animation(file);
                            modifier.Add(anim);
                        }
                    }
                }
            }
        }

        private void ModelListDrop(object sender, DragEventArgs e)
        {
            //Animation? poseAnim = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);

                using var modifier = new MVVMItemListModifier<Part>(ViewModel.Parts);

                foreach (var file in files)
                {
                    var model = new Part(ViewModel, file);
                    modifier.Add(model);
                }
            }
        }

        /// <inheritdoc/>
        private void ListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListBox)
                return;

            foreach (var anim in e.RemovedItems.OfType<AnimationLayer>())
                ViewModel.SelectedLayers.Remove(anim);
            foreach (var anim in e.AddedItems.OfType<AnimationLayer>())
                ViewModel.SelectedLayers.Add(anim);

            e.Handled = true;
        }

        /// <inheritdoc/>
        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ListView)
                return;

            foreach (var anim in e.RemovedItems.OfType<Animation>())
                ViewModel.SelectedAnimations.Remove(anim);
            foreach (var anim in e.AddedItems.OfType<Animation>())
                ViewModel.SelectedAnimations.Add(anim);
            foreach (var model in e.RemovedItems.OfType<Part>())
                ViewModel.SelectedParts.Remove(model);
            foreach (var model in e.AddedItems.OfType<Part>())
                ViewModel.SelectedParts.Add(model);

            e.Handled = true;
        }

        private void DialogHostDialogClosing(object sender, DialogClosingEventArgs eventArgs)
        {
            if (eventArgs.Parameter is string str)
            {
                switch (str)
                {
                    case "AcceptPrefix":
                        if (!string.IsNullOrWhiteSpace(PrefixBox.Text))
                            foreach (var anim in ViewModel.SelectedAnimations)
                                anim.OutputName += PrefixBox.Text.Trim();
                        break;
                    case "AcceptSuffix":
                        if (!string.IsNullOrWhiteSpace(SuffixBox.Text))
                            foreach (var anim in ViewModel.SelectedAnimations)
                                anim.OutputName = SuffixBox.Text.Trim() + anim.OutputName;
                        break;
                    case "Run":
                        foreach (var script in ScriptsListBox.SelectedItems.Cast<Script>())
                            script.Run(ViewModel);
                        break;
                    default:
                        break;
                }
            }
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            // https://stackoverflow.com/a/978352
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        private void WindowKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch(e.Key)
                {
                    case Key.S:
                        ViewModel.SaveProjectCommand.Execute((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift);
                        break;
                    case Key.C:
                        ViewModel.Clipboard.Clear();
                        ViewModel.Clipboard.AddRange(MainAnimationList.SelectedItems.OfType<Animation>());
                        break;
                    case Key.V:
                        ViewModel.Animations.AddRange(ViewModel.Clipboard.Select(x => x.Clone()));
                        break;
                    case Key.Z:
                        ViewModel.Undo();
                        break;
                    case Key.Y:
                        ViewModel.Redo();
                        break;
                    case Key.Q:
                        ViewModel.AutoAdjustColumns(ActualWidth);
                        break;
                    case Key.W:
                        ViewModel.AutoAdjustColumns(ActualWidth);
                        break;
                    case Key.X:
                        foreach (var b in FindVisualChildren<ListView>(this))
                            b.SelectedItems.Clear();
                        foreach (var b in FindVisualChildren<ListBox>(this))
                            b.SelectedItems.Clear();
                        break;
                }
            }
            else if (e.Key == Key.Delete)
            {
                ViewModel.RemoveAnimationsCommand.Execute(null);
            }
        }
    }
}