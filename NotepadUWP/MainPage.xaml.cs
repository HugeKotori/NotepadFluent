using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Toolkit.Uwp;
using Microsoft.Toolkit.Uwp.UI.Controls;
using Windows.Storage;
using Windows.Graphics.Display;
using Windows.System.Profile;
using Windows.UI.ViewManagement;
using Windows.UI.Core.Preview;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Activation;
using Windows.UI.Core;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace NotepadUWP
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {

        public ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public MainPage()
        {
            this.InitializeComponent();
            Window.Current.SetTitleBar(TitleBar);
            DetactIsFirstRunORNot();
            AddTab(this,null);
            SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += AppClosingConfirm;
        }

        //Page Functions
        private async void AppClosingConfirm(object sender, SystemNavigationCloseRequestedPreviewEventArgs e)   //if file not saved, show dialog
        {
            e.Handled = true;
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            //checked if something is not saved
            int pageNotSavedCount = 0;
            string pageNotSavedNames = "";
            for (int tabCount = 0; tabCount < this.TabBar.Items.Count; tabCount++)
            {
                if (((TabViewItem)this.TabBar.Items[tabCount]).Content is TabContent)
                {
                    if (((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).isEdited == true)
                    {
                        pageNotSavedCount++;
                        pageNotSavedNames += ((TabViewItem)(this.TabBar.Items[tabCount])).Header.ToString().Substring(1) + "\n";
                    }
                }
            }

            //if all saved
            if (pageNotSavedCount == 0)
            {
                App.Current.Exit();
            }

            //if only the selected page not saved
            if (pageNotSavedCount == 1)
            {
                if(((TabViewItem)this.TabBar.SelectedItem).Content is TabContent &&
                    ((TabContent)((TabViewItem)this.TabBar.SelectedItem).Content).isEdited == true)
                {
                    ContentDialog saveDialog = new ContentDialog
                    {
                        Title = resourceLoader.GetString("FileUnsavedDialogTitle"),
                        Content = resourceLoader.GetString("FileUnsavedDialogContent") + ((TabViewItem)this.TabBar.SelectedItem).Header.ToString().Substring(1) + "\"?",
                        CloseButtonText = resourceLoader.GetString("FileUnsavedDialogCancel"),
                        PrimaryButtonText = resourceLoader.GetString("FileUnsavedDialogSave"),
                        SecondaryButtonText = resourceLoader.GetString("FileUnsavedDialogNotSave"),
                        DefaultButton = ContentDialogButton.Primary
                    };
                    ContentDialogResult dialogResult = await saveDialog.ShowAsync();
                    switch (dialogResult)
                    {
                        //if clicked Save
                        case ContentDialogResult.Primary:
                            {
                                if (((TabContent)((TabViewItem)this.TabBar.SelectedItem).Content).file == null)
                                {
                                    FileSavePicker fileSavePicker = new FileSavePicker();
                                    fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                                    fileSavePicker.FileTypeChoices.Add(resourceLoader.GetString("TextFile"), new List<string>() { ".txt" });
                                    fileSavePicker.SuggestedFileName = ((TabViewItem)this.TabBar.SelectedItem).Header.ToString().Substring(1);
                                    ((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file = await fileSavePicker.PickSaveFileAsync();
                                    //if noting chosen, return, not closing the tab
                                    if (((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file == null)
                                    {
                                        return;
                                    }

                                    //prevent upload until saved (onedrive, etc)
                                    Windows.Storage.CachedFileManager.DeferUpdates(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file);

                                    //Write to file
                                    await Windows.Storage.FileIO.WriteTextAsync(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file, ((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).TabTextBox.Text);

                                    //let windows know we finished writing
                                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file);

                                    //save complete
                                    //exit the app
                                    App.Current.Exit();
                                    return;
                                }
                                else
                                {
                                    //if not a new file
                                    //prevent upload until saved (onedrive, etc)
                                    Windows.Storage.CachedFileManager.DeferUpdates(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file);

                                    //Write to file
                                    await Windows.Storage.FileIO.WriteTextAsync(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file, ((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).TabTextBox.Text);

                                    //let windows know we finished writing
                                    await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(((TabContent)(((TabViewItem)this.TabBar.SelectedItem).Content)).file);
                                    
                                    //finishe saving, Exit
                                    Application.Current.Exit();
                                    return;

                                }

                            }

                        //if clicked not Save, close tab
                        case ContentDialogResult.Secondary:
                            {
                                //close the app
                                App.Current.Exit();
                                return;
                            }

                        //if clicked cancel - return
                        case ContentDialogResult.None:
                            {
                                return;
                            }
                    }
                }
            }

            //if something not saved
            e.Handled = true;
            ContentDialog closeDialog = new ContentDialog
            {
                Title = resourceLoader.GetString("MutipleFilesUnsavedDialogTitle"),
                Content = resourceLoader.GetString("MutipleFilesUnsavedDialogContent0") + "\n\n" + pageNotSavedNames + "\n" +  resourceLoader.GetString("MutipleFilesUnsavedDialogContent1"),
                CloseButtonText = resourceLoader.GetString("MutipleFilesUnsavedDialogCancel"),
                PrimaryButtonText = resourceLoader.GetString("MutipleFilesUnsavedDialogExitAnyway"),
                DefaultButton = ContentDialogButton.Close,
            };

            //display dialog
            ContentDialogResult result = await closeDialog.ShowAsync();

            //if clicked cancel
            if (result == ContentDialogResult.None)
            {
                return;
            }

            //if clicked still want to exit
            else
            {
                App.Current.Exit();
            }
        }
        private void DetactIsFirstRunORNot()        //若初次启动，初始化
        {
            //if local settings not set, set all

            if (localSettings.Values["StatusBarVisibility"] == null)
            {
                localSettings.Values["StatusBarVisibility"] = true;
            }

            if (localSettings.Values["IsWrap"] == null)
            {
                localSettings.Values["IsWrap"] = false;
            }

            if (localSettings.Values["SearchEngine"] == null)
            {
                localSettings.Values["SearchEngine"] = "ComboBoxBing";
            }

            if (localSettings.Values["FontFamily"] == null)
            {
                localSettings.Values["FontFamily"] = "Segoe UI";
            }

            if (localSettings.Values["FontSize"] == null)
            {
                localSettings.Values["FontSize"] = 14.0;
            }
        }
        public void FileActivated(FileActivatedEventArgs e)
        {
            foreach (Windows.Storage.StorageFile fileFromExplorer in e.Files)
            {
                //if only 1 page and it's a new page that not edited
                if (this.TabBar.Items.Count == 1 && 
                    (((TabViewItem)(this.TabBar.Items[0])).Content is TabContent) &&
                    ((TabContent)(((TabViewItem)(this.TabBar.Items[0])).Content)).file == null &&
                    ((TabContent)(((TabViewItem)(this.TabBar.Items[0])).Content)).isEdited == false)
                {
                    ((TabContent)(((TabViewItem)(this.TabBar.Items[0])).Content)).file = fileFromExplorer;
                    ((TabContent)(((TabViewItem)(this.TabBar.Items[0])).Content)).OpenFileNewTabAsync();
                    ApplySettings(this, null);
                    return;
                }

                bool isOpened = false;
                //if file opened
                for (int tabCount = 0; tabCount < this.TabBar.Items.Count; tabCount++)
                {
                    //if is a text tab
                    if (((TabViewItem)this.TabBar.Items[tabCount]).Content is TabContent)
                    {
                        if (((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).file != null &&
                            ((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).file.Path == fileFromExplorer.Path)
                        {
                            isOpened = true;
                            this.TabBar.SelectedItem = this.TabBar.Items[tabCount];
                        }
                    }
                }

                //if not opened, Add a Tab to open
                if (!isOpened)
                {
                    TabBar.Items.Add(new TabViewItem()
                    {
                        BorderThickness = new Thickness(0, 0, 1, 0),
                        Content = new TabContent()
                        {
                            mainPage = this,
                            file = fileFromExplorer,
                        },
                    });
                    TabBar.SelectedIndex = TabBar.Items.Count - 1;
                    ((TabContent)((TabViewItem)(TabBar.SelectedItem)).Content).tab = (TabViewItem)TabBar.SelectedItem;
                    ((TabContent)((TabViewItem)TabBar.SelectedItem).Content).OpenFileNewTabAsync();
                    ApplySettings(this,null);
                }
            }
        }
        private void NotepadAppMainWindowPreviewKeyDown(object sender, KeyRoutedEventArgs e)    //Ctrl+Tab swtich tab
        {
            var ctrlState = CoreWindow.GetForCurrentThread().GetKeyState(Windows.System.VirtualKey.Control);
            var isCtrlDown = ctrlState == CoreVirtualKeyStates.Down ||
                ctrlState == (CoreVirtualKeyStates.Down | CoreVirtualKeyStates.Locked);
            if (isCtrlDown && e.Key == Windows.System.VirtualKey.Tab)
            {
                if (this.TabBar.SelectedIndex == this.TabBar.Items.Count - 1)
                {
                    this.TabBar.SelectedIndex = 0;
                }
                else
                {
                    this.TabBar.SelectedIndex++;
                }
                e.Handled = true;
            }
        }




        //TabView Events
        private void AddTab(object sender, RoutedEventArgs e)   //新建标签页
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            TabBar.Items.Add(new TabViewItem()
            {
                Header = resourceLoader.GetString("NewTabHeader"),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Content = new TabContent()
                {
                    mainPage = this,
                },

            }
            );
            TabBar.SelectedIndex = TabBar.Items.Count - 1;
            ((TabContent)((TabViewItem)(TabBar.SelectedItem)).Content).tab = (TabViewItem)TabBar.SelectedItem;
            ApplySettings(this, null);
        }
        private async void TabClosingAsync(object sender, TabClosingEventArgs e)   //Closing A tab, Check if it's edited
        {
            //localization
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            //Cancel Closing
            e.Cancel = true;

            //Detact if it's a Setting Page
            if((e.Tab.Content) is SettingPage)
            {
                //remove this tab
                this.TabBar.Items.Remove(e.Tab);
                //If all tab closed, exit the app
                if (this.TabBar.Items.Count == 0)
                {
                    Application.Current.Exit();
                }
                //Setting Page Closed, return
                return;
            }

            //If file is edited, show dialog
            if (((TabContent)(e.Tab.Content)).isEdited == true)
            {
                ContentDialog saveDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("FileUnsavedDialogTitle"),
                    Content = resourceLoader.GetString("FileUnsavedDialogContent") + e.Tab.Header.ToString().Substring(1) + "\"?",
                    CloseButtonText = resourceLoader.GetString("FileUnsavedDialogCancel"),
                    PrimaryButtonText = resourceLoader.GetString("FileUnsavedDialogSave"),
                    SecondaryButtonText = resourceLoader.GetString("FileUnsavedDialogNotSave"),
                    DefaultButton = ContentDialogButton.Primary
                };
                ContentDialogResult result = await saveDialog.ShowAsync();
                switch (result)
                {
                    //if clicked Save
                    case ContentDialogResult.Primary:
                        {
                            if (((TabContent)(e.Tab.Content)).file == null)
                            {
                                FileSavePicker fileSavePicker = new FileSavePicker();
                                fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                                fileSavePicker.FileTypeChoices.Add(resourceLoader.GetString("TextFile"), new List<string>() { ".txt" });
                                fileSavePicker.SuggestedFileName = e.Tab.Header.ToString().Substring(1);
                                ((TabContent)(e.Tab.Content)).file = await fileSavePicker.PickSaveFileAsync();
                                //if noting chosen, return, not closing the tab
                                if (((TabContent)(e.Tab.Content)).file == null)
                                {
                                    return;
                                }

                                //prevent upload until saved (onedrive, etc)
                                Windows.Storage.CachedFileManager.DeferUpdates(((TabContent)(e.Tab.Content)).file);

                                //Write to file
                                await Windows.Storage.FileIO.WriteTextAsync(((TabContent)(e.Tab.Content)).file, ((TabContent)(e.Tab.Content)).TabTextBox.Text);

                                //let windows know we finished writing
                                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(((TabContent)(e.Tab.Content)).file);
                        
                                //save complete, remove the tab
                                this.TabBar.Items.Remove(e.Tab);

                                //save complete
                                //If all tab closed, exit the app
                                if (this.TabBar.Items.Count == 0)
                                {
                                    Application.Current.Exit();
                                }
                                return;
                            }
                            else
                            {
                                //if not a new file
                                //prevent upload until saved (onedrive, etc)
                                Windows.Storage.CachedFileManager.DeferUpdates(((TabContent)(e.Tab.Content)).file);

                                //Write to file
                                await Windows.Storage.FileIO.WriteTextAsync(((TabContent)(e.Tab.Content)).file, ((TabContent)(e.Tab.Content)).TabTextBox.Text);

                                //let windows know we finished writing
                                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(((TabContent)(e.Tab.Content)).file);
                                //save complete, remove the tab
                                this.TabBar.Items.Remove(e.Tab);
                                if (this.TabBar.Items.Count == 0)
                                {
                                    Application.Current.Exit();
                                }
                                return;
                            }

                        }

                    //if clicked not Save, close tab
                    case ContentDialogResult.Secondary:
                        {
                            //remove this tab
                            this.TabBar.Items.Remove(e.Tab);
                            //If all tab closed, exit the app
                            if (this.TabBar.Items.Count == 0)
                            {
                                Application.Current.Exit();
                            }
                            return;
                        }

                    //if clicked cancel - return
                    case ContentDialogResult.None:
                        {
                            return;
                        }
                }
            }

            //If file not edited. close the tab
            this.TabBar.Items.Remove(e.Tab);

            //If all tab closed, exit the app
            if(this.TabBar.Items.Count == 0)
            {
                 Application.Current.Exit();
            }

        }

        //Functions
        public void OpenSettingPage()       //打开设置页面
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            //判断是否已打开设置窗口
            for(int tabCount=0; tabCount<this.TabBar.Items.Count; tabCount++)
            {
                if(((TabViewItem)this.TabBar.Items[tabCount]).Content is SettingPage)
                {
                    this.TabBar.SelectedIndex = tabCount;
                    return;
                }
            }

            //Add the Setting Page to Tab Bar
            TabBar.Items.Add(new TabViewItem()
            {
                Header = resourceLoader.GetString("Settings"),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Content = new SettingPage()
                {
                    mainPage = this
                },

            }
            ) ;
            TabBar.SelectedIndex = TabBar.Items.Count - 1;

            //Initializing the Setting Page
            ((SettingPage)(((TabViewItem)(TabBar.SelectedItem)).Content)).SetPageControls();
            
        }
        public void ApplySettings(object sender, SelectionChangedEventArgs e)       //应用设置页面
        {
            //判断是否为文本窗口
            for(int tabCount = 0; tabCount < this.TabBar.Items.Count; tabCount++ )
            {
                if(((TabViewItem)this.TabBar.Items[tabCount]).Content is TabContent)
                {
                    ((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).StatusBarVisibility = (bool)localSettings.Values["StatusBarVisibility"];
                    ((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).IsWrap = (bool)localSettings.Values["IsWrap"];
                    ((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).TextBoxFontFamilty = new FontFamily(((string)localSettings.Values["FontFamily"]));
                    //((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).TextBoxFontSize = double.Parse(localSettings.Values["FontSize"].ToString());
                    ((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).TextBoxFontSize = double.Parse(localSettings.Values["FontSize"].ToString()) * (((TabContent)((TabViewItem)this.TabBar.Items[tabCount]).Content).ZoomScale)/100;
                }
            }
        }

    }
}
