using Microsoft.Toolkit.Uwp.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;


//https://go.microsoft.com/fwlink/?LinkId=234236 上介绍了“用户控件”项模板

namespace NotepadUWP
{
    public sealed partial class TabContent : UserControl
    {
        //Properties
        public MainPage mainPage;
        public TabViewItem tab;     //标签页
        public bool isEdited = false;
        public bool StatusBarVisibility
        {
            get
            {
                if (this.StatusBar.Visibility == Visibility.Collapsed)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            set
            {
                if (value == true)
                {
                    this.StatusBar.Visibility = Visibility.Visible;
                }
                else
                {
                    this.StatusBar.Visibility = Visibility.Collapsed;
                }
            }

        }
        public bool IsWrap
        {
            get
            {
                if (this.TabTextBox.TextWrapping == TextWrapping.Wrap)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (value == true)
                {
                    this.TabTextBox.TextWrapping = TextWrapping.Wrap;
                }
                else
                {
                    this.TabTextBox.TextWrapping = TextWrapping.NoWrap;
                }
            }
        }
        public bool isAfterOpenFile = false; //textboxtextchanged用此属性判断是否为打开文件导致的文本变动
        public FontFamily TextBoxFontFamilty
        {
            get
            {
                return this.TabTextBox.FontFamily;
            }
            set
            {
                this.TabTextBox.FontFamily = value;
            }
        }
        public double TextBoxFontSize
        {
            get
            {
                return this.TabTextBox.FontSize;
            }
            set
            {
                if (value < 5 || value > 300)
                {
                    return;
                }
                this.TabTextBox.FontSize = value;
            }
        }
        public double ZoomScale = 100;
        public Windows.Storage.StorageFile file;
        private ArrayList findResult;
        private int findResultIndex;

        public TabContent()
        {
            this.InitializeComponent();

            //for sharing function
            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;

            
        }


        //Page Functions
        private async void OpenFileAsync()   //Open a file
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            //if Edited
            if (this.isEdited)
            {
                //Open a SaveOrNot Dialog
                ContentDialog saveDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("FileUnsavedDialogTitle"),
                    Content = resourceLoader.GetString("FileUnsavedDialogContent") + this.tab.Header.ToString().Substring(1) + "\"?",
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
                        //SaveFile
                        if (file == null)
                        {
                            FileSavePicker fileSavePicker = new FileSavePicker();
                            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                            fileSavePicker.FileTypeChoices.Add(resourceLoader.GetString("TextFile"), new List<string>() { ".txt" });
                            fileSavePicker.SuggestedFileName = this.tab.Header.ToString().Substring(1);
                            this.file = await fileSavePicker.PickSaveFileAsync();
                            //if noting chosen, return
                            if (file == null)
                            {
                                return;
                            }

                            //prevent upload until saved (onedrive, etc)
                            Windows.Storage.CachedFileManager.DeferUpdates(file);

                            //Write to file
                            await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

                            //let windows know we finished writing
                            await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                            this.isEdited = false;
                            //tab header changed
                            this.tab.Header = file.Name;

                            //Save complete, continue Opening a file
                            break;
                        }


                        //if not a new file
                        //prevent upload until saved (onedrive, etc)
                        Windows.Storage.CachedFileManager.DeferUpdates(file);

                        //Write to file
                        await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

                        //let windows know we finished writing
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                        this.isEdited = false;
                        this.tab.Header = file.Name;

                        break;

                    //if clicked not Save, continue
                    case ContentDialogResult.Secondary:
                        break;

                    //if clicked cancel - return
                    case ContentDialogResult.None:
                        return;
                }

            }

            //creating FileOpenPicker
            FileOpenPicker fileOpenPicker = new FileOpenPicker();
            fileOpenPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileOpenPicker.FileTypeFilter.Add(".txt");
            //fileOpenPicker.FileTypeFilter.Add(".*");

            //Display FilePicker
            Windows.Storage.StorageFile selectedFile = await fileOpenPicker.PickSingleFileAsync();
            //if nothing picked, return
            if (selectedFile == null)
            {
                return;
            }


            //set file properties
            this.file = selectedFile;

            //read text from file to textbox
            this.TabTextBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);

            //set tab header
            this.tab.Header = this.file.Name;
            this.isEdited = false;
            this.isAfterOpenFile = true;
        }
        public async void OpenFileNewTabAsync()  //OpenFile New Tab
        {
            this.tab.Header = this.file.Name;
            this.isEdited = false;
            this.isAfterOpenFile = true;
            this.TabTextBox.Text = await Windows.Storage.FileIO.ReadTextAsync(file);
        }
        public async void SaveFileAsync()     //Save File
        {
            //detact if is a new file
            if (file == null)
            {
                FileSavePicker fileSavePicker = new FileSavePicker();
                fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                fileSavePicker.FileTypeChoices.Add("文本文档", new List<string>() { ".txt" });
                fileSavePicker.SuggestedFileName = this.tab.Header.ToString().Substring(1);
                this.file = await fileSavePicker.PickSaveFileAsync();
                //if noting chosen, return
                if (file == null)
                {
                    return;
                }

                //prevent upload until saved (onedrive, etc)
                Windows.Storage.CachedFileManager.DeferUpdates(file);

                //Write to file
                await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

                //let windows know we finished writing
                await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                this.isEdited = false;
                //tab header changed
                this.tab.Header = file.Name;

                //Save complete, return
                return;
            }


            //if not a new file
            //prevent upload until saved (onedrive, etc)
            Windows.Storage.CachedFileManager.DeferUpdates(file);

            //Write to file
            await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

            //let windows know we finished writing
            await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            this.isEdited = false;
            this.tab.Header = file.Name;
        }
        public async void SaveAsAsync()     //Save As..
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            FileSavePicker fileSavePicker = new FileSavePicker();
            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            fileSavePicker.FileTypeChoices.Add(resourceLoader.GetString("TextFile"), new List<string>() { ".txt" });
            fileSavePicker.SuggestedFileName = this.tab.Header.ToString().Substring(1);
            this.file = await fileSavePicker.PickSaveFileAsync();
            //if noting chosen, return
            if (file == null)
            {
                return;
            }

            //prevent upload until saved (onedrive, etc)
            Windows.Storage.CachedFileManager.DeferUpdates(file);

            //Write to file
            await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

            //let windows know we finished writing
            await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

            this.isEdited = false;
            //tab header changed
            this.tab.Header = file.Name;
        }
        public async void NewFileAsync()        //New File
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            if (this.isEdited == true)
            {
                ContentDialog saveDialog = new ContentDialog
                {
                    Title = resourceLoader.GetString("FileUnsavedDialogTitle"),
                    Content = resourceLoader.GetString("FileUnsavedDialogContent") + this.tab.Header.ToString().Substring(1) + "\"?",
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
                        //SaveFile
                        if (file == null)
                        {
                            FileSavePicker fileSavePicker = new FileSavePicker();
                            fileSavePicker.SuggestedStartLocation = PickerLocationId.Desktop;
                            fileSavePicker.FileTypeChoices.Add(resourceLoader.GetString("TextFile"), new List<string>() { ".txt" });
                            fileSavePicker.SuggestedFileName = this.tab.Header.ToString().Substring(1);
                            this.file = await fileSavePicker.PickSaveFileAsync();
                            //if noting chosen, return
                            if (file == null)
                            {
                                return;
                            }

                            //prevent upload until saved (onedrive, etc)
                            Windows.Storage.CachedFileManager.DeferUpdates(file);

                            //Write to file
                            await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

                            //let windows know we finished writing
                            await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                            this.isEdited = false;
                            //tab header changed
                            this.tab.Header = file.Name;

                            //Save complete, continue
                            break;
                        }


                        //if not a new file
                        //prevent upload until saved (onedrive, etc)
                        Windows.Storage.CachedFileManager.DeferUpdates(file);

                        //Write to file
                        await Windows.Storage.FileIO.WriteTextAsync(file, this.TabTextBox.Text);

                        //let windows know we finished writing
                        await Windows.Storage.CachedFileManager.CompleteUpdatesAsync(file);

                        this.isEdited = false;
                        this.tab.Header = file.Name;

                        //Save Complete, continue
                        break;

                    //if clicked not Save, continue
                    case ContentDialogResult.Secondary:
                        break;

                    //if clicked cancel - return
                    case ContentDialogResult.None:
                        return;
                }

            }

            this.tab.Header = resourceLoader.GetString("NewTabHeader");
            this.isAfterOpenFile = true;
            this.TabTextBox.Text = "";
            this.isEdited = false;
            this.file = null;

        }
        private void WebSearchSelectedText()    //Web Search Selected Text
        {
            //if selected nothing
            if (this.TabTextBox.SelectedText.Length == 0)
            {
                return;
            }

            string URL = "https://github.com/search?&amp;q=TestString";
            XmlDocument searchEngineXml = new XmlDocument();
            searchEngineXml.Load("Resources/SearchEnginesURL.xml");
            XmlNode xmlNode = searchEngineXml.SelectSingleNode("SearchEngines");
            XmlNodeList xmlNodeList = xmlNode.ChildNodes;
            foreach (XmlNode node in xmlNodeList)
            {
                if (node.Attributes["Name"].Value == (string)mainPage.localSettings.Values["SearchEngine"])
                {
                    URL = node.Attributes["URL"].Value;
                    break;
                }
            }
            URL = URL.Replace("TestString", TabTextBox.SelectedText);
            Windows.System.Launcher.LaunchUriAsync(new Uri(URL));
        }
        private void AskCortana()   //Ask Cortana
        {
            //Launch Cortana
            Windows.System.Launcher.LaunchUriAsync(new Uri("ms-cortana://search/?q=" + System.Web.HttpUtility.UrlEncode(this.TabTextBox.SelectedText)));
        }
        private void UpdateTextBoxZoomScale()
        {
            //When initializing the page, this function runs which can cause an Exception
            try
            {
                this.StatusBarZoomScaleTextDisplayer.Text = this.ZoomScale.ToString() + "%";
                this.TabTextBox.FontSize = double.Parse(mainPage.localSettings.Values["FontSize"].ToString()) * (this.ZoomScale / 100);
            }
            catch
            {

            }
        }

        //TextBox Events
        private void TextChanged(object sender, TextChangedEventArgs e)     //文本改变的行为
        {
            //if is after open a file, return
            if (this.isAfterOpenFile)
            {
                this.isAfterOpenFile = false;
                return;
            }

            //if not, add a * on tab header
            //加上星号
            if (this.isEdited == false)
            {
                this.isEdited = true;
                this.tab.Header = "*" + this.tab.Header.ToString();
            }

            //Clear Find Input Textbox
            this.FindBarInputTextBox.Text = "";

            //update status bar
            //更新状态栏
            UpdateStatusBar();
        }
        private void TextBoxSelectionChanged(object sender, RoutedEventArgs e)  //指针变化
        {
            //UpdateStatusBar
            UpdateStatusBar();

            //RightClickMenu
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            //if something is selected
            if (this.TabTextBox.SelectionLength > 0)
            {
                //if selected text is too long
                string tempString = this.TabTextBox.SelectedText;
                if (tempString.Length > 18)
                {
                    tempString = tempString.Substring(0, 18) + "...";
                }

                this.RightClickMenuCopyButton.IsEnabled = true;
                this.RightClickMenuCutButton.IsEnabled = true;
                this.RightClickMenuSeparatorBetweenEditAndWeb.Visibility = Visibility.Visible;
                this.RightClickMenuWebSearchButton.Visibility = Visibility.Visible;
                this.RightClickMenuWebSearchButton.Text = resourceLoader.GetString("TabTextBoxRightClickMenuWebSearch0") + tempString + resourceLoader.GetString("TabTextBoxRightClickMenuWebSearch1");
                this.RightClickMenuAskCortanaButton.Visibility = Visibility.Visible;
                this.RightClickMenuAskCortanaButton.Text = resourceLoader.GetString("TabTextBoxRightClickMenuAskCortana0") + tempString + resourceLoader.GetString("TabTextBoxRightClickMenuAskCortana1");
            }

            //nothing selected
            else
            {
                this.RightClickMenuCopyButton.IsEnabled = false;
                this.RightClickMenuCutButton.IsEnabled = false;
                this.RightClickMenuSeparatorBetweenEditAndWeb.Visibility = Visibility.Collapsed;
                this.RightClickMenuWebSearchButton.Visibility = Visibility.Collapsed;
                this.RightClickMenuAskCortanaButton.Visibility = Visibility.Collapsed;
            }
        }
        private void UpdateStatusBar()  //更新状态栏
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();

            int lineCount = 1;
            int charPos = 1;
            int lastLineStartIndex = 0;
            foreach (char c in TabTextBox.Text)
            {
                if (charPos > TabTextBox.SelectionStart)
                {
                    break;
                }
                if (c == '\n' || c == '\r')
                {
                    lineCount++;
                    lastLineStartIndex = charPos;
                }
                charPos++;
            }
            StatusBarLineDisplayer.Text = resourceLoader.GetString("StatusBarLnCow0") + lineCount + resourceLoader.GetString("StatusBarLnCow1") 
                + (TabTextBox.SelectionStart - lastLineStartIndex + 1) + resourceLoader.GetString("StatusBarLnCow2");
            StatusBarCharacterCountDisplay.Text = TabTextBox.Text.Length.ToString() + resourceLoader.GetString("StatusBarCharCount");
        }
        private void TabTextBoxPointerWheelChaned(object sender, PointerRoutedEventArgs e)  //Mouse Wheel + Ctrl = ZoomScale Change
        {
            int wheelAction = e.GetCurrentPoint(this.TabTextBox).Properties.MouseWheelDelta;
            Windows.UI.Core.CoreVirtualKeyStates ctrl = Window.Current.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control);
            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                if (wheelAction > 0)
                {
                    if (this.ZoomScale >= 500)
                    {
                        return;
                    }
                    this.ZoomScale += 10;
                    this.StatusBarZoomScaleSlider.Value = this.ZoomScale;
                    UpdateTextBoxZoomScale();
                }
                else
                {
                    if (this.ZoomScale <= 10)
                    {
                        return;
                    }
                    this.ZoomScale -= 10;
                    this.StatusBarZoomScaleSlider.Value = this.ZoomScale;
                    UpdateTextBoxZoomScale();
                }
                e.Handled = true;
            }
        }
        private void TabTextBoxKeyDown(object sender, KeyRoutedEventArgs e)     //Make textbox accepts tab
        {
            if (e.Key == Windows.System.VirtualKey.Tab)
            {
                int selectionStart = this.TabTextBox.SelectionStart + 1;
                this.TabTextBox.Text = this.TabTextBox.Text.Insert(this.TabTextBox.SelectionStart, "\t");
                this.TabTextBox.SelectionStart = selectionStart;
                e.Handled = true;
            }
        }



        //AppBar Events
        private void OpenSetting(object sender, RoutedEventArgs e)  //AppBar设置按钮被点击
        {
            mainPage.OpenSettingPage();
        }
        private void AppBarUndoButtonClicked(object sender, RoutedEventArgs e)      //撤销
        {
            TabTextBox.Undo();
        }
        private void AppBarRedoButtonClicked(object sender, RoutedEventArgs e)      //重做
        {
            TabTextBox.Redo();
        }
        private void AppBarOpenFileButtonClicked(object sender, RoutedEventArgs e)  //打开
        {
            OpenFileAsync();
        }
        private void AppBarNewFileButtonClicked(object sender, RoutedEventArgs e)   //New File
        {
            NewFileAsync();
        }
        private void AppBarSaveAsButtonClicked(object sender, RoutedEventArgs e)    //Save As..
        {
            SaveAsAsync();
        }
        private void AppBarSaveButtonClicked(object sender, RoutedEventArgs e)      //保存
        {
            SaveFileAsync();
        }
        private void AppBarCopyButtonClicked(object sender, RoutedEventArgs e)      //复制
        {
            TabTextBox.CopySelectionToClipboard();
        }
        private void AppBarCutButtonClicked(object sender, RoutedEventArgs e)       //剪切
        {
            TabTextBox.CutSelectionToClipboard();
        }
        private void AppBarPasteButtonClicked(object sender, RoutedEventArgs e)     //粘贴
        {
            TabTextBox.PasteFromClipboard();
        }
        private void AppBarSelectAllButtonClicked(object sender, RoutedEventArgs e)     //全选
        {
            TabTextBox.SelectAll();
        }
        private void AppBarAddDateButtonClicked(object sender, RoutedEventArgs e)       //插入日期
        {
            TabTextBox.Text = TabTextBox.Text.Insert(TabTextBox.SelectionStart, DateTime.Now.Date.ToString());
        }
        private void AppBarAddTimeButtonClicked(object sender, RoutedEventArgs e)       //插入时间
        {
            TabTextBox.Text = TabTextBox.Text.Insert(TabTextBox.SelectionStart, DateTime.Now.TimeOfDay.ToString());
        }
        private void AppBarAddTabButtonClicked(object sender, RoutedEventArgs e)
        {
            int selectionStart = this.TabTextBox.SelectionStart + 1;
            this.TabTextBox.Text = this.TabTextBox.Text.Insert(this.TabTextBox.SelectionStart, "\t");
            this.TabTextBox.SelectionStart = selectionStart;
        }
        private void AppBarFindButtonClicked(object sender, RoutedEventArgs e)  //Find
        {
            if (this.FindBar.Visibility == Visibility.Collapsed)
            {
                this.FindBarInputTextBox.Text = "";
                this.FindBar.Visibility = Visibility.Visible;
            }
            else
            {
                this.FindBar.Visibility = Visibility.Collapsed;
            }
        }
        private void AppBarJumpToButtonFlyoutButtonClicked(object sender, RoutedEventArgs e)
        {
            int lineInput;
            bool isInputLegal = int.TryParse(this.AppBarJumpToButtonFlyoutTextBox.Text, out lineInput);

            //if input something wrong
            if (!isInputLegal || lineInput <= 0)
            {
                this.AppBarJumpToButtonFlyoutTextBox.Text = "";
                return;
            }


            int lineCounter = 0;
            int charCounter = 0;
            foreach (char c in this.TabTextBox.Text)
            {
                if (c == '\n' || c == '\r')
                {
                    lineCounter++;
                }
                if (lineCounter == lineInput)
                {
                    this.TabTextBox.SelectionStart = charCounter;
                    this.TabTextBox.Focus(FocusState.Programmatic);
                    return;
                }
                charCounter++;
            }

            //only 1 line
            if (lineCounter == 0)
            {
                this.TabTextBox.SelectionStart = 0;
                this.TabTextBox.Focus(FocusState.Programmatic);
                return;
            }

            //if input > total lines
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            ContentDialog contentDialog = new ContentDialog
            {
                CloseButtonText = resourceLoader.GetString("AppBarJumpToButtonFlyoutDialogCloseButtonText"),
                Content = resourceLoader.GetString("AppBarJumpToButtonFlyoutDialogContent"),
            };
            contentDialog.ShowAsync();
        }
        private void AppBarWebSearchButtonClicked(object sender, RoutedEventArgs e)     //网页搜索
        {
            WebSearchSelectedText();
        }
        private void AppBarShareButtonClicked(object sender, RoutedEventArgs e) //共享
        {
            if (this.file == null || this.isEdited == true)
            {
                Flyout flyout = new Flyout();
                flyout.Content = new TextBlock { Text = "你需要先保存文档才能共享。" };
                flyout.ShowAt(sender as FrameworkElement);
                return;
            }
            //Display Sharing UI
            DataTransferManager.ShowShareUI();
        }
        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args) //for sharing function
        {
            DataPackage requestData = new DataPackage();
            List<Windows.Storage.IStorageItem> items = new List<Windows.Storage.IStorageItem> { this.file };
            requestData.Properties.Title = (string)this.tab.Header;
            requestData.SetStorageItems(items);
            DataRequest request = args.Request;
            request.Data = requestData;
        }


        //RightClickMenu Events
        private void RightClickMenuUndoButtonClicked(object sender, RoutedEventArgs e)      //撤销
        {
            this.TabTextBox.Undo();
        }
        private void RightClickMenuCopyButtonClicked(object sender, RoutedEventArgs e)      //复制
        {
            this.TabTextBox.CopySelectionToClipboard();
        }
        private void RightClickMenuCutButtonClicked(object sender, RoutedEventArgs e)       //剪切
        {
            this.TabTextBox.CutSelectionToClipboard();
        }
        private void RightClickMenuPasteButtonClicked(object sender, RoutedEventArgs e)     //粘贴
        {
            this.TabTextBox.PasteFromClipboard();
        }
        private void RightClickMenuSelectAllButtonClicked(object sender, RoutedEventArgs e)     //全选
        {
            this.TabTextBox.SelectAll();
        }
        private void RightClickMenuWebSearchButtonClicked(object sender, RoutedEventArgs e)     //Web 搜索
        {
            WebSearchSelectedText();
        }
        private void RightClickMenuAskCortanaButtonClicked(object sender, RoutedEventArgs e)    //询问小娜
        {
            AskCortana();
        }


        //Debug
        private void DebugButtonClicked(object sender, RoutedEventArgs e)
        {

        }



        //StatusBar Events
        private void StatusBarZoomScaleSliderValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            this.ZoomScale = this.StatusBarZoomScaleSlider.Value;
            UpdateTextBoxZoomScale();
        }
        private void StatusBarZoomOutButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.ZoomScale <= 10)
            {
                return;
            }
            this.ZoomScale -= 10;
            this.StatusBarZoomScaleSlider.Value = this.ZoomScale;
            UpdateTextBoxZoomScale();
        }
        private void StatusBarZoomInButtonClicked(object sender, RoutedEventArgs e)
        {
            if (this.ZoomScale >= 500)
            {
                return;
            }
            this.ZoomScale += 10;
            this.StatusBarZoomScaleSlider.Value = this.ZoomScale;
            UpdateTextBoxZoomScale();
        }


        //FindBar Events
        private void FinBarCloseButtonClicked(object sender, RoutedEventArgs e)     //"X" Button Clicked
        {
            this.FindBar.Visibility = Visibility.Collapsed;
        }
        private void FindBarInputTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            //if input textbox has nothing
            if (this.FindBarInputTextBox.Text.Length == 0)
            {
                this.FindBarResultTextBox.Text = "没有任何结果";
                this.findResultIndex = 0;
                return;
            }

            //Find
            this.findResult = new System.Collections.ArrayList();
            int tempResult = this.TabTextBox.Text.IndexOf(this.FindBarInputTextBox.Text);
            int tempLastResultIndex = 0;
            while(tempResult != -1)
            {
                int result = tempLastResultIndex + tempResult;
                this.findResult.Add(result);
                tempLastResultIndex += tempResult + 1;
                tempResult = this.TabTextBox.Text.Substring(tempLastResultIndex).IndexOf(this.FindBarInputTextBox.Text);
            }
            this.findResultIndex = 0;

            //if found something
            if(this.findResult.Count != 0)
            {
                this.TabTextBox.SelectionStart = (int)findResult[findResultIndex];      //findResultIndex是当前选择项的index
                this.TabTextBox.SelectionLength = this.FindBarInputTextBox.Text.Length;
                this.FindBarResultTextBox.Text = "1/" + findResult.Count;
            }

            //if found nothing
            else
            {
                this.FindBarResultTextBox.Text = "没有任何结果";
            }
        }
        private void FindBarPreviousButtonClicked(object sender, RoutedEventArgs e)
        {
            //若findResult存在且findresultindex
            if (findResult != null && findResult.Count != 0 && findResultIndex > 0)
            {
                findResultIndex--;  //选择项加一
                this.TabTextBox.SelectionStart = (int)findResult[findResultIndex];
                this.TabTextBox.SelectionLength = this.FindBarInputTextBox.Text.Length;
                this.FindBarResultTextBox.Text = (findResultIndex + 1) + "/" + findResult.Count;
            }
        }
        private void FindBarNextButtonClicked(object sender, RoutedEventArgs e)
        {
            //若findResult存在且findresultindex
            if (findResult != null && findResult.Count != 0 && findResultIndex < findResult.Count - 1)
            {
                findResultIndex++;  //选择项减一
                this.TabTextBox.SelectionStart = (int)findResult[findResultIndex];
                this.TabTextBox.SelectionLength = this.FindBarInputTextBox.Text.Length;
                this.FindBarResultTextBox.Text = (findResultIndex + 1) + "/" + findResult.Count;
            }
        }

    }
}
